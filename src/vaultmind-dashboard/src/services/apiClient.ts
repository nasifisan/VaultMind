import { authService } from "./authService.service";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5152";

let isRefreshing = false;
let refreshSubscribers: ((token: string) => void)[] = [];

function subscribeTokenRefresh(cb: (token: string) => void) {
  refreshSubscribers.push(cb);
}

function onRefreshed(token: string) {
  refreshSubscribers.forEach((cb) => cb(token));
  refreshSubscribers = [];
}

/**
 * Custom fetch wrapper that automatically:
 * 1. Attaches standard Authorization Bearer header if a token is present in localStorage.
 * 2. Intercepts 401 Unauthorized responses.
 * 3. Attempts to refresh the access token using the stored refresh token.
 * 4. Queues concurrent requests during a refresh operation and retries them on success.
 * 5. Falls back to generating a new anonymous session if the refresh fails, and retries.
 */
export async function apiFetch(
  endpoint: string,
  options: RequestInit = {}
): Promise<Response> {
  const url = endpoint.startsWith("http") ? endpoint : `${API_URL}${endpoint}`;

  // Get current tokens
  let tokens = authService.getTokens();

  // Attach bearer token if available
  const headers = new Headers(options.headers || {});
  if (tokens?.AccessToken && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${tokens.AccessToken}`);
  }
  options.headers = headers;

  const response = await fetch(url, options);

  // If unauthorized (401), trigger token refresh flow
  if (response.status === 401) {
    if (isRefreshing) {
      // Queue requests if we are already in the middle of a refresh
      return new Promise<Response>((resolve) => {
        subscribeTokenRefresh((newToken) => {
          const newHeaders = new Headers(options.headers || {});
          newHeaders.set("Authorization", `Bearer ${newToken}`);
          options.headers = newHeaders;
          resolve(fetch(url, options));
        });
      });
    }

    isRefreshing = true;

    try {
      const currentRefreshToken = tokens?.RefreshToken;
      if (!currentRefreshToken) {
        throw new Error("No refresh token available");
      }

      // 1. Try to refresh the token
      const newTokens = await authService.requestToken(currentRefreshToken);
      isRefreshing = false;
      onRefreshed(newTokens.AccessToken);

      // Retry the original request with new token
      const newHeaders = new Headers(options.headers || {});
      newHeaders.set("Authorization", `Bearer ${newTokens.AccessToken}`);
      options.headers = newHeaders;
      return fetch(url, options);
    } catch (refreshErr) {
      console.warn("Refresh token invalid or expired. Requesting a new anonymous session...", refreshErr);

      try {
        // 2. If refresh fails, call /token to get a new anonymous session
        const newAnonymousTokens = await authService.requestToken();
        isRefreshing = false;
        onRefreshed(newAnonymousTokens.AccessToken);

        // Retry the original request with new anonymous token
        const newHeaders = new Headers(options.headers || {});
        newHeaders.set("Authorization", `Bearer ${newAnonymousTokens.AccessToken}`);
        options.headers = newHeaders;
        return fetch(url, options);
      } catch (anonErr) {
        isRefreshing = false;
        refreshSubscribers = [];
        throw new Error(`Authentication failed: ${anonErr instanceof Error ? anonErr.message : "Unable to acquire session"}`);
      }
    }
  }

  return response;
}

