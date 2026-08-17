import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

// Backend returns this when login succeeds
interface LoginSuccessResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: { id: string; email: string; firstName: string; lastName: string };
  action?: never;
  mfaToken?: never;
}

// Backend returns this for Admin/SuperAdmin (MFA required)
interface MfaRequiredResponse {
  action: 'MFA_REQUIRED';
  mfaToken: string;
  accessToken?: never;
  refreshToken?: never;
}

export type LoginResponse = LoginSuccessResponse | MfaRequiredResponse;

interface TokenPayload {
  sub: string;
  email: string;
  given_name: string;
  family_name: string;
  roles: string[];
  [key: string]: unknown;
}

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/auth';

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/login`, { email, password });
  }

  verifyMfa(mfaToken: string, otpCode: string): Observable<LoginSuccessResponse> {
    return this.http.post<LoginSuccessResponse>(`${this.base}/mfa/verify`, { mfaToken, otpCode });
  }

  parseToken(token: string): TokenPayload {
    const base64 = token.split('.')[1];
    const decoded = atob(base64.replace(/-/g, '+').replace(/_/g, '/'));
    const payload = JSON.parse(decoded) as Record<string, unknown>;

    const rawRoles =
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      ?? payload['role']    // .NET JwtSecurityTokenHandler maps ClaimTypes.Role → "role"
      ?? payload['roles']
      ?? [];

    const roles = Array.isArray(rawRoles) ? rawRoles as string[] : [rawRoles as string];

    return {
      sub: payload['sub'] as string,
      email: payload['email'] as string,
      given_name: payload['given_name'] as string,
      family_name: payload['family_name'] as string,
      roles,
      ...payload,
    };
  }
}
