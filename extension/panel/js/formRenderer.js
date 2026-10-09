// Builds the input form of a tab from the "inputs" definition stored in the database.
import { h } from "./dom.js";

// source.inputs example:
// [{ name: "city", label: "City", type: "text", required: true, default: "Delhi" }]
export function buildForm(source, savedValues, onSubmit) {
  const fields = Array.isArray(source.inputs) ? source.inputs : [];
  const controls = new Map();
  const errorEls = new Map();

  const rows = fields.map((field) => {
    const initial = savedValues && savedValues[field.name] !== undefined ? savedValues[field.name] : field.default || "";
    const control = createControl(field, initial);
    const error = h("div", { class: "field-error", hidden: true });
    controls.set(field.name, control);
    errorEls.set(field.name, error);

    return h(
      "label",
      { class: "field" },
      h("span", { class: "field-label", text: field.label || field.name }),
      control,
      error
    );
  });

  const runLabel = source.httpMethod === "POST" ? "Execute" : "Run";
  const runBtn = h("button", { class: "btn btn-primary", type: "submit", text: runLabel });

  const form = h("form", { class: "form", novalidate: true }, h("div", { class: "form-grid" }, rows), runBtn);
  form.addEventListener("submit", (event) => {
    event.preventDefault();
    onSubmit(readValues());
  });

  function readValues() {
    const values = {};
    controls.forEach((control, name) => {
      values[name] = control.value;
    });
    return values;
  }

  function showErrors(errors) {
    errorEls.forEach((el, name) => {
      const message = errors && errors[name];
      el.textContent = message || "";
      el.hidden = !message;
    });
  }

  function setBusy(busy) {
    runBtn.disabled = busy;
    runBtn.textContent = busy ? "Working..." : runLabel;
  }

  return { element: form, readValues, showErrors, setBusy, hasFields: fields.length > 0 };
}

function createControl(field, initial) {
  if (field.type === "select") {
    const select = h("select", { class: "input", name: field.name });
    (field.options || []).forEach((option) => select.append(h("option", { value: option, text: option })));
    select.value = initial;
    return select;
  }
  if (field.type === "textarea") {
    const area = h("textarea", { class: "input", name: field.name, rows: 3 });
    area.value = initial;
    return area;
  }
  const input = h("input", {
    class: "input",
    name: field.name,
    type: "text",
    inputmode: field.type === "number" ? "decimal" : "text",
    autocomplete: "off",
    spellcheck: "false"
  });
  input.value = initial;
  return input;
}
