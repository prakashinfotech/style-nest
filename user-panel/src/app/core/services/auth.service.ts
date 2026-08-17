import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthTokens, User } from '../models/user.model';

interface ApiUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

interface ApiAuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: ApiUser;
}

interface ApiRefreshResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}

interface AuthResult { user: User; tokens: AuthTokens; }

/** ENH-AUTH-001 — Facebook callback response shapes. */
interface SocialCallbackResponse {
  action: string;         // "NEW_ACCOUNT" | "MERGE_REQUIRED"
  auth?: ApiAuthResponse; // present when action === "NEW_ACCOUNT"
  mergeToken?: string;    // present when action === "MERGE_REQUIRED"
}

export interface FacebookCallbackResult {
  action: 'NEW_ACCOUNT' | 'MERGE_REQUIRED';
  authResult?: AuthResult;
  mergeToken?: string;
}

// ClaimTypes.Role serialized in a .NET JWT
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.authApiUrl;

  private decodeRoles(accessToken: string): string[] {
    try {
      const payload = accessToken.split('.')[1];
      if (!payload) return [];
      const padded = payload.replaceAll('-', '+').replaceAll('_', '/');
      const json = JSON.parse(atob(padded)) as Record<string, unknown>;
      const raw = json[ROLE_CLAIM];
      if (raw === null || raw === undefined) return [];
      return Array.isArray(raw) ? (raw as string[]) : [raw as string];
    } catch {
      return [];
    }
  }

  private buildUser(apiUser: ApiUser, accessToken: string): User {
    return {
      id:          apiUser.id,
      email:       apiUser.email,
      firstName:   apiUser.firstName,
      lastName:    apiUser.lastName,
      phoneNumber: null,
      roles:       this.decodeRoles(accessToken),
    };
  }

  private toAuthResult(res: ApiAuthResponse): AuthResult {
    return {
      user:   this.buildUser(res.user, res.accessToken),
      tokens: {
        accessToken:  res.accessToken,
        refreshToken: res.refreshToken,
        expiresIn:    0,
      },
    };
  }

  login(email: string, password: string): Observable<AuthResult> {
    return this.http
      .post<ApiAuthResponse>(`${this.base}/auth/login`, { email, password })
      .pipe(map((res) => this.toAuthResult(res)));
  }

  register(
    firstName: string,
    lastName: string,
    email: string,
    password: string,
  ): Observable<AuthResult> {
    return this.http
      .post<ApiAuthResponse>(`${this.base}/auth/register`, {
        firstName, lastName, email, password, confirmPassword: password,
      })
      .pipe(map((res) => this.toAuthResult(res)));
  }

  refreshToken(refreshToken: string): Observable<AuthTokens> {
    return this.http
      .post<ApiRefreshResponse>(`${this.base}/auth/refresh`, { refreshToken })
      .pipe(
        map((res) => ({
          accessToken:  res.accessToken,
          refreshToken: res.refreshToken,
          expiresIn:    0,
        })),
      );
  }

  logout(refreshToken: string): Observable<void> {
    return this.http.post<void>(`${this.base}/auth/logout`, { refreshToken });
  }

  /** ENH-AUTH-001 — Returns the Facebook OAuth 2.0 authorization URL from the backend. */
  getFacebookLoginUrl(redirectUri: string): Observable<string> {
    return this.http
      .get<{ url: string }>(`${this.base}/auth/facebook/url`, { params: { redirectUri } })
      .pipe(map((r) => r.url));
  }

  /** ENH-AUTH-001 — Exchanges the FB code for a JWT or returns a merge token. */
  facebookCallback(code: string, redirectUri: string): Observable<FacebookCallbackResult> {
    return this.http
      .post<SocialCallbackResponse>(`${this.base}/auth/facebook/callback`, { code, redirectUri })
      .pipe(
        map((res) => {
          if (res.action === 'NEW_ACCOUNT' && res.auth) {
            return { action: 'NEW_ACCOUNT' as const, authResult: this.toAuthResult(res.auth) };
          }
          return { action: 'MERGE_REQUIRED' as const, mergeToken: res.mergeToken };
        }),
      );
  }

  /** ENH-AUTH-001 — Confirms account merge with password; returns full auth result. */
  mergeConfirm(mergeToken: string, password: string): Observable<AuthResult> {
    return this.http
      .post<ApiAuthResponse>(`${this.base}/auth/merge/confirm`, { mergeToken, password })
      .pipe(map((res) => this.toAuthResult(res)));
  }

  /** ENH-AUTH-002 — Returns the Apple OAuth 2.0 authorization URL from the backend. */
  getAppleLoginUrl(redirectUri: string): Observable<string> {
    return this.http
      .get<{ url: string }>(`${this.base}/auth/apple/url`, { params: { redirectUri } })
      .pipe(map((r) => r.url));
  }

  /** ENH-AUTH-002 — Validates the Apple id_token JWT; returns auth or merge token. */
  appleCallback(idToken: string): Observable<FacebookCallbackResult> {
    return this.http
      .post<SocialCallbackResponse>(`${this.base}/auth/apple/callback`, { idToken })
      .pipe(
        map((res) => {
          if (res.action === 'NEW_ACCOUNT' && res.auth) {
            return { action: 'NEW_ACCOUNT' as const, authResult: this.toAuthResult(res.auth) };
          }
          return { action: 'MERGE_REQUIRED' as const, mergeToken: res.mergeToken };
        }),
      );
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${this.base}/auth/forgot-password`, { email });
  }

  verifyOtp(email: string, otp: string): Observable<void> {
    return this.http.post<void>(`${this.base}/auth/verify-otp`, { email, otp });
  }

  resetPassword(email: string, otp: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.base}/auth/reset-password`, { email, otp, newPassword });
  }
}
