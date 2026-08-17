export type UserRole = 'SuperAdmin' | 'Admin' | 'Seller' | 'Customer';

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  role: UserRole;
  userId: string;
  email: string;
  sellerId?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface OtpRequest {
  email: string;
  purpose: 'PasswordReset' | 'EmailVerification';
}

export interface ResetPasswordRequest {
  email: string;
  otpCode: string;
  newPassword: string;
}
