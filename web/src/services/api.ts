let csrf = "";
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message);
  }
}
export async function refreshCsrf() {
  const r = await fetch("/api/auth/csrf", { credentials: "include" });
  if (!r.ok) throw new Error("Cannot connect to the API.");
  csrf = (await r.json()).token;
}
export async function api<T>(
  url: string,
  method = "GET",
  data?: unknown,
): Promise<T> {
  if (method !== "GET" && !csrf) await refreshCsrf();
  const r = await fetch("/api" + url, {
    method,
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": csrf },
    body: data === undefined ? undefined : JSON.stringify(data),
  });
  if (!r.ok) {
    const body = await r.json().catch(() => ({}));
    if (r.status === 401) window.dispatchEvent(new Event("session-expired"));
    throw new ApiError(
      body.message ||
        (r.status === 403
          ? "Access denied."
          : r.status === 429
            ? "Too many attempts. Please try again later."
            : "Request failed."),
      r.status,
    );
  }
  if (r.status === 204) return undefined as T;
  return r.json();
}

/** Exchanges the HttpOnly refresh cookie for a new short-lived access cookie. */
export async function refreshSession(): Promise<boolean> {
  try {
    await refreshCsrf();
    const r = await fetch("/api/auth/refresh", {
      method: "POST",
      credentials: "include",
      headers: { "X-CSRF-TOKEN": csrf },
    });
    if (!r.ok) return false;
    await refreshCsrf();
    return true;
  } catch {
    return false;
  }
}
