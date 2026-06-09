import {
  TokenResponse,
  SignInRequest,
  SignUpRequest,
} from "../types/auth/auth.contracts";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5152";

export const authService = {
  getTokens(): TokenResponse | null {
    if (typeof window === "undefined") return null;
    const tokenStr = localStorage.getItem("vaultmind_tokens");
    if (!tokenStr) return null;
    try {
      return JSON.parse(tokenStr) as TokenResponse;
    } catch {
      return null;
    }
  },

  setTokens(tokens: TokenResponse | null): void {
    if (typeof window === "undefined") return;
    if (tokens) {
      localStorage.setItem("vaultmind_tokens", JSON.stringify(tokens));
    } else {
      localStorage.removeItem("vaultmind_tokens");
    }
  },

  async requestToken(refreshToken?: string): Promise<TokenResponse> {
    const response = await fetch(`${API_URL}/api/auth/token`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ RefreshToken: refreshToken || null }),
    });

    if (!response.ok) {
      throw new Error(`Token request failed: ${response.status}`);
    }

    const tokens = (await response.json()) as TokenResponse;
    this.setTokens(tokens);
    return tokens;
  },

  async signUp(request: SignUpRequest): Promise<TokenResponse> {
    const currentTokens = this.getTokens();
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
    };
    if (currentTokens?.AccessToken) {
      headers["Authorization"] = `Bearer ${currentTokens.AccessToken}`;
    }

    const response = await fetch(`${API_URL}/api/auth/signup`, {
      method: "POST",
      headers,
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const err = (await response
        .json()
        .catch(() => ({ Error: "Signup failed" }))) as { Error?: string };
      throw new Error(err.Error || "Signup failed");
    }

    const tokens = (await response.json()) as TokenResponse;
    this.setTokens(tokens);
    return tokens;
  },

  async signIn(request: SignInRequest): Promise<TokenResponse> {
    const currentTokens = this.getTokens();
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
    };
    if (currentTokens?.AccessToken) {
      headers["Authorization"] = `Bearer ${currentTokens.AccessToken}`;
    }

    const response = await fetch(`${API_URL}/api/auth/signin`, {
      method: "POST",
      headers,
      body: JSON.stringify({
        Email: request.Email,
        Password: request.Password,
        AnonymousToken: currentTokens?.AccessToken || null,
      }),
    });

    if (!response.ok) {
      const err = (await response
        .json()
        .catch(() => ({ Error: "Signin failed" }))) as { Error?: string };
      throw new Error(err.Error || "Signin failed");
    }

    const tokens = (await response.json()) as TokenResponse;
    this.setTokens(tokens);
    return tokens;
  },
};
