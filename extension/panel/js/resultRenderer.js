// Draws the answer of an API. The shape comes from the database (displayType + displayConfig):
//   list     - rows with a title, subtitle, value and badge
//   stats    - big number tiles (good for a few key figures)
//   keyvalue - label / value rows
import { h } from "./dom.js";
import { getPath } from "./pathUtil.js";
import { formatValue, toneFor } from "./format.js";

const RAW_JSON_LIMIT = 20000;

export function renderResult(source, response) {
  const config = source.displayConfig || {};
  let body;

  switch (source.displayType) {
    case "stats":
      body = renderStats(response.data, config);
      break;
    case "keyvalue":
      body = renderKeyValue(response.data, config);
      break;
    default:
      body = renderList(response.data, config);
  }

  return h("div", { class: "result" }, body, renderRaw(response.data));
}

// ---------- list ----------
function renderList(data, config) {
  let items = getPath(data, config.itemsPath);

  // Some APIs return an object like { USD: 1, INR: 83 }. "entries" turns it into rows.
  if (config.entries && items && typeof items === "object" && !Array.isArray(items)) {
    items = Object.entries(items).map(([key, value]) => ({ key, value }));
  }

  if (!Array.isArray(items)) return emptyState("The API answered, but not with a list.");
  if (config.limit) items = items.slice(0, config.limit);
  if (items.length === 0) return emptyState("No results.");

  const rows = items.map((item) => renderListRow(item, config));
  return h("ul", { class: "list" }, rows);
}

function renderListRow(item, config) {
  const titleText = formatValue(getPath(item, config.title));
  const href = config.link ? safeUrl(getPath(item, config.link)) : null;
  const title = href
    ? h("a", { class: "row-title", href, target: "_blank", rel: "noopener noreferrer", text: titleText })
    : h("span", { class: "row-title", text: titleText });

  const left = h("div", { class: "row-main" }, title);
  if (config.subtitle) {
    left.append(h("span", { class: "row-sub", text: formatValue(getPath(item, config.subtitle), config.subtitleFormat) }));
  }

  const right = h("div", { class: "row-side" });
  if (config.value) {
    right.append(h("span", { class: "row-value", text: formatValue(getPath(item, config.value.path), config.value.format, config.value.suffix) }));
  }
  if (config.badge) {
    const raw = getPath(item, config.badge.path);
    right.append(
      h("span", {
        class: "badge badge-" + toneFor(raw, config.badge.tone),
        text: formatValue(raw, config.badge.format, config.badge.suffix)
      })
    );
  }

  return h("li", { class: "row" }, left, right);
}

// ---------- stats ----------
function renderStats(data, config) {
  const wrap = h("div", {});
  wrap.append(renderHeading(data, config));
  const tiles = (config.fields || []).map((field) =>
    h(
      "div",
      { class: "tile" },
      h("div", { class: "tile-label", text: field.label }),
      h("div", { class: "tile-value", text: formatValue(getPath(data, field.path), field.format, field.suffix) })
    )
  );
  wrap.append(h("div", { class: "tiles" }, tiles));
  return wrap;
}

// ---------- keyvalue ----------
function renderKeyValue(data, config) {
  const wrap = h("div", {});
  wrap.append(renderHeading(data, config));
  const rows = (config.fields || []).map((field) =>
    h(
      "div",
      { class: "kv-row" },
      h("div", { class: "kv-key", text: field.label }),
      h("div", { class: "kv-value", text: formatValue(getPath(data, field.path), field.format, field.suffix) })
    )
  );
  wrap.append(h("div", { class: "kv" }, rows));
  return wrap;
}

function renderHeading(data, config) {
  if (!config.heading) return h("span", {});
  const title = getPath(data, config.heading.title);
  const subtitle = config.heading.subtitle ? getPath(data, config.heading.subtitle) : null;
  return h(
    "div",
    { class: "heading" },
    h("div", { class: "heading-title", text: formatValue(title) }),
    subtitle ? h("div", { class: "heading-sub", text: String(subtitle) }) : null
  );
}

// ---------- helpers ----------
function emptyState(message) {
  return h("div", { class: "empty", text: message });
}

// Only allow normal web links (blocks javascript: and similar).
function safeUrl(value) {
  if (typeof value !== "string") return null;
  try {
    const url = new URL(value);
    return url.protocol === "https:" || url.protocol === "http:" ? url.href : null;
  } catch (e) {
    return null;
  }
}

// Developers (you!) can open the raw answer to see exactly what the API returned.
function renderRaw(data) {
  let text = JSON.stringify(data, null, 2) || "";
  if (text.length > RAW_JSON_LIMIT) text = text.slice(0, RAW_JSON_LIMIT) + "\n... (cut)";
  return h("details", { class: "raw" }, h("summary", { text: "Raw response" }), h("pre", { text }));
}
