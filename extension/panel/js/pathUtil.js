// Reads a value from a JSON object using a simple path like "company.name" or "capital[0]".
// The paths come from the database (display_config_json), so the UI never needs code changes
// when you add a new API.

export function getPath(obj, path) {
  if (path === "" || path === undefined || path === null) return obj;
  const tokens = String(path).match(/[^.[\]]+/g) || [];
  let current = obj;
  for (const token of tokens) {
    if (current === undefined || current === null) return undefined;
    current = current[token];
  }
  return current;
}
