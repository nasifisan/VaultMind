export interface User {
  Id: string;
  Email: string;
  Name: string;
}

export interface TokenResponse {
  AccessToken: string;
  RefreshToken: string;
  ExpiresAt: number;
  Exp: number;
}

export interface SignInRequest {
  Email: string;
  Password: string;
  AnonymousToken?: string | null;
}

export interface SignUpRequest {
  Email: string;
  Password: string;
  Name: string;
}
