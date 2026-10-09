// The only file that talks to the backend. Everything else calls these small functions.
import { getApiBase } from "./config.js";

const REQUEST_TIMEOUT_MS = 30000;

export class ApiError extends Error {
  constructor(message, { kind = "server", status = 0, code = "", errors = null } = {}) {
    super(message);
    this.name = "ApiError";
    this.kind = kind; // "offline" | "invalid" | "upstream" | "server"
    this.status = status;
    this.code = code;
    this.errors = errors; // field name -> message (only for "invalid")
  }
}

async function request(path, { method = "GET", body } = {}) {
  const base = await getApiBase();
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  let response;
  try {
    response = await fetch(base + path, {
      method,
      headers: body ? { "Content-Type": "application/json" } : undefined,
      body: body ? JSON.stringify(body) : undefined,
      signal: controller.signal
    });
  } catch (err) {
    const timedOut = err && err.name === "AbortError";
    throw new ApiError(
      timedOut ? "The backend took too long to answer." : "Cannot reach the backend at " + base + ".",
      { kind: "offline" }
    );
  } finally {
    clearTimeout(timer);
  }

  let data = null;
  try {
    data = await response.json();
  } catch (e) {
    data = null;
  }

  if (!response.ok) {
    const kind =
      response.status === 400 ? "invalid" : response.status === 502 ? "upstream" : "server";
    throw new ApiError((data && data.message) || "Request failed (HTTP " + response.status + ").", {
      kind,
      status: response.status,
      code: data && data.code,
      errors: data && data.errors
    });
  }
  return data;
}

export const api = {
  health: () => request("/api/health"),
  getSources: () => request("/api/sources"),
  execute: (key, inputs) =>
    request("/api/sources/" + encodeURIComponent(key) + "/execute", {
      method: "POST",
      body: { inputs }
    })
};
