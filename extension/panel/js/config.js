// Where is the backend? Stored in chrome.storage.local so the user can change it in Settings.

export const DEFAULT_API_BASE = "https://localhost:5081";
const STORAGE_KEY = "apiBaseUrl";

function clean(url) {
  return String(url || "").trim().replace(/\/+$/, "");
}

export async function getApiBase() {
  const stored = await chrome.storage.local.get(STORAGE_KEY);
  return clean(stored[STORAGE_KEY]) || DEFAULT_API_BASE;
}

export async function setApiBase(url) {
  const value = clean(url);
  if (!/^https?:\/\/.+/i.test(value)) {
    throw new Error("Please enter a full URL, for example https://localhost:5081");
  }
  await chrome.storage.local.set({ [STORAGE_KEY]: value });
  return value;
}

// Remember which tab was open last.
export async function getLastTab() {
  const stored = await chrome.storage.local.get("lastTabKey");
  return stored.lastTabKey || null;
}

export function setLastTab(key) {
  return chrome.storage.local.set({ lastTabKey: key });
}
