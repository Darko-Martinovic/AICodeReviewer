const SAFE_DEFAULT = "/dashboard";

export function safeReturnUrl(raw: string | null, origin = window.location.origin): string {
  if (!raw) {
    return SAFE_DEFAULT;
  }
  try {
    const url = new URL(raw, origin);
    if (url.origin !== origin) {
      return SAFE_DEFAULT;
    }
    return url.pathname + url.search + url.hash;
  } catch {
    return SAFE_DEFAULT;
  }
}

export async function signIn(email: string, password: string): Promise<void> {
  const response = await fetch("/api/session", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "same-origin",
    body: JSON.stringify({ email, password }),
  });

  if (response.status === 401) {
    throw new Error("The email or password is wrong.");
  }
  if (!response.ok) {
    throw new Error(`Sign-in failed (${response.status}).`);
  }

  const params = new URLSearchParams(window.location.search);
  window.location.assign(safeReturnUrl(params.get("returnUrl")));
}
