type Session = {
  token: string;
  isAdmin: boolean;
};

export function getSession(): Session | null {
  const token = sessionStorage.getItem("token");

  if (!token) return null;

  try {
    const encodedPayload = token.split(".")[1];
    if (!encodedPayload) return null;

    const base64 = encodedPayload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, "=");
    const payload = JSON.parse(atob(padded));

    if (
      typeof payload.exp !== "number" ||
      payload.exp * 1000 <= Date.now()
    ) {
      return null;
    }

    const role =
      payload[
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
      ];

    return {
      token,
      isAdmin: Array.isArray(role)
        ? role.includes("Admin")
        : role === "Admin",
    };
  } catch {
    return null;
  }
}
