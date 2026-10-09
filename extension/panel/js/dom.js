// A tiny helper to build HTML elements safely.
// It always uses textContent (never innerHTML), so text coming from an API can not inject HTML or scripts.
//
//   h("div", { class: "card" }, h("span", { text: "Hello" }))

export function h(tag, props = {}, ...children) {
  const el = document.createElement(tag);
  for (const [name, value] of Object.entries(props)) {
    if (value === undefined || value === null || value === false) continue;
    if (name === "class") el.className = value;
    else if (name === "text") el.textContent = value;
    else if (name.startsWith("on") && typeof value === "function") {
      el.addEventListener(name.slice(2).toLowerCase(), value);
    } else if (name === "dataset") Object.assign(el.dataset, value);
    else el.setAttribute(name, value === true ? "" : String(value));
  }
  for (const child of children.flat()) {
    if (child === undefined || child === null || child === false) continue;
    el.append(child.nodeType ? child : document.createTextNode(String(child)));
  }
  return el;
}

export function clear(el) {
  while (el.firstChild) el.removeChild(el.firstChild);
}
