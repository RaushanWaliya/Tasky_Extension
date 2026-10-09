// Turns raw values from an API into readable text.

const numberFormat = new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 });
const number4Format = new Intl.NumberFormat(undefined, { maximumFractionDigits: 4 });

export function formatValue(value, format, suffix) {
  if (value === undefined || value === null || value === "") return "—";

  let text;
  switch (format) {
    case "number":
      text = typeof value === "number" ? numberFormat.format(value) : String(value);
      break;
    case "number4":
      text = typeof value === "number" ? number4Format.format(value) : String(value);
      break;
    case "percent":
      text = typeof value === "number" ? (value > 0 ? "+" : "") + value.toFixed(2) + "%" : String(value);
      break;
    case "bool":
      text = value ? "Yes" : "No";
      break;
    case "done":
      text = value ? "Done" : "Pending";
      break;
    case "date": {
      const date = new Date(value);
      text = isNaN(date.getTime())
        ? String(value)
        : date.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
      break;
    }
    case "upper":
      text = String(value).toUpperCase();
      break;
    case "user":
      text = "User #" + value;
      break;
    default:
      text = typeof value === "object" ? JSON.stringify(value) : String(value);
  }
  return suffix ? text + suffix : text;
}

// Which colour should a badge get? "signed" = green/red by sign, "bool" = green/amber.
export function toneFor(value, tone) {
  if (tone === "signed" && typeof value === "number") return value > 0 ? "good" : value < 0 ? "bad" : "neutral";
  if (tone === "bool") return value ? "good" : "warn";
  return "neutral";
}

export function timeLabel(isoString) {
  const date = new Date(isoString);
  return isNaN(date.getTime()) ? "" : date.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit", second: "2-digit" });
}
