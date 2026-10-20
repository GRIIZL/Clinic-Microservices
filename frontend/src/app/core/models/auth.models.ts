export type UserRole = 'Patient' | 'Doctor' | 'Receptionist' | 'Admin';

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string;
}

export interface CurrentUser {
  accountId: string;
  email: string;
  role: UserRole;
}
