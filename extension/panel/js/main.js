// Tasky panel: the screen the user sees inside the docked side panel.
//
// Flow:  start -> check backend -> load tabs from backend -> show first tab
//        click a tab -> show its form -> Run -> backend calls the external API -> show result
import { api, ApiError } from "./apiClient.js";
import { getApiBase, setApiBase, getLastTab, setLastTab } from "./config.js";
import { h, clear } from "./dom.js";
import { buildForm } from "./formRenderer.js";
import { renderResult } from "./resultRenderer.js";
import { timeLabel } from "./format.js";

// ---------- Page elements ----------
const els = {
  hostLabel: document.getElementById("hostLabel"),
  status: document.getElementById("status"),
  statusText: document.getElementById("statusText"),
  settingsBtn: document.getElementById("settingsBtn"),
  closeBtn: document.getElementById("closeBtn"),
  settings: document.getElementById("settings"),
  apiBaseInput: document.getElementById("apiBaseInput"),
  saveSettingsBtn: document.getElementById("saveSettingsBtn"),
  tabs: document.getElementById("tabs"),
  content: document.getElementById("content"),
  footer: document.getElementById("footer")
};

// ---------- State ----------
const state = {
  sources: [],
  activeKey: null,
  savedValues: new Map(), // tab key -> last typed values
  results: new Map() // tab key -> last successful response
};

// ---------- Which browser tab is this panel for? ----------
// The background gives every tab its own panel address: panel.html?tabId=123
const tabId = Number(new URLSearchParams(location.search).get("tabId")) || null;

// ---------- Start ----------
init();

async function init() {
  wireStaticEvents();
  showHostName();
  keepConnected();
  els.apiBaseInput.value = await getApiBase();
  await checkHealth();
  await loadSources();
}

function wireStaticEvents() {
  els.closeBtn.addEventListener("click", closePanel);
  els.settingsBtn.addEventListener("click", () => {
    els.settings.hidden = !els.settings.hidden;
  });
  els.saveSettingsBtn.addEventListener("click", saveSettings);
  els.apiBaseInput.addEventListener("keydown", (event) => {
    if (event.key === "Enter") saveSettings();
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closePanel();
  });

}

async function showHostName() {
  if (tabId === null) return;
  try {
    const tab = await chrome.tabs.get(tabId);
    els.hostLabel.textContent = new URL(tab.url).hostname;
  } catch (e) {
    // The tab is gone or its address is not readable. Keep the default text.
  }
}

// Tell the background "this panel is open" (it uses this to decide open versus close on the
// next click). A small heartbeat keeps the connection alive, and we reconnect if it drops.
function keepConnected() {
  if (tabId === null) return;
  const port = chrome.runtime.connect({ name: "tasky-panel:" + tabId });
  const beat = setInterval(() => {
    try { port.postMessage({ type: "ping" }); } catch (e) { clearInterval(beat); }
  }, 20000);
  port.onDisconnect.addListener(() => {
    clearInterval(beat);
    setTimeout(keepConnected, 1000);
  });
}

function closePanel() {
  chrome.runtime.sendMessage({ type: "TASKY_CLOSE_PANEL", tabId });
}

async function saveSettings() {
  try {
    await setApiBase(els.apiBaseInput.value);
    els.settings.hidden = true;
    await checkHealth();
    await loadSources();
  } catch (err) {
    els.apiBaseInput.value = await getApiBase();
    showMessage("bad", err.message);
  }
}

// ---------- Backend connection ----------
async function checkHealth() {
  setStatus("unknown", "Checking");
  try {
    const health = await api.health();
    setStatus(health.database ? "ok" : "warn", health.database ? "Connected" : "No database");
  } catch (err) {
    setStatus(err.status === 503 ? "warn" : "bad", err.status === 503 ? "No database" : "Offline");
  }
}

function setStatus(stateName, text) {
  els.status.dataset.state = stateName;
  els.statusText.textContent = text;
}

// ---------- Tabs ----------
async function loadSources() {
  clear(els.tabs);
  clear(els.content);
  els.content.append(h("div", { class: "loading" }, h("div", { class: "spinner" }), h("span", { text: "Loading APIs..." })));

  try {
    state.sources = await api.getSources();
  } catch (err) {
    state.sources = [];
    clear(els.content);
    els.content.append(await offlineCard(err));
    return;
  }

  if (state.sources.length === 0) {
    clear(els.content);
    els.content.append(h("div", { class: "empty", text: "No APIs are configured yet. Add rows to the api_sources table." }));
    return;
  }

  renderTabs();
  const last = await getLastTab();
  const first = state.sources.find((s) => s.key === last) || state.sources[0];
  selectTab(first.key);
}

function renderTabs() {
  clear(els.tabs);
  state.sources.forEach((source) => {
    els.tabs.append(
      h(
        "button",
        {
          class: "tab",
          type: "button",
          role: "tab",
          dataset: { key: source.key },
          title: source.description,
          onclick: () => selectTab(source.key)
        },
        h("span", { class: "tab-icon", text: source.icon }),
        h("span", { class: "tab-title", text: source.title })
      )
    );
  });
}

function markActiveTab() {
  els.tabs.querySelectorAll(".tab").forEach((tab) => {
    const active = tab.dataset.key === state.activeKey;
    tab.classList.toggle("active", active);
    tab.setAttribute("aria-selected", active ? "true" : "false");
    if (active) tab.scrollIntoView({ block: "nearest", inline: "nearest" });
  });
}

// ---------- One tab ----------
function selectTab(key) {
  const source = state.sources.find((s) => s.key === key);
  if (!source) return;

  state.activeKey = key;
  setLastTab(key);
  markActiveTab();

  clear(els.content);
  clear(els.footer);

  const resultBox = h("div", { class: "result-box" });
  const message = h("div", { class: "messages" });

  const form = buildForm(source, state.savedValues.get(key), (values) => run(source, form, resultBox, message, values));

  els.content.append(
    h(
      "div",
      { class: "tab-head" },
      h("div", {},
        h("h2", { class: "tab-name", text: source.icon + "  " + source.title }),
        h("p", { class: "tab-desc", text: source.description })
      ),
      h("button", {
        class: "icon-btn refresh",
        type: "button",
        title: "Refresh",
        "aria-label": "Refresh",
        text: "↻",
        onclick: () => run(source, form, resultBox, message, form.readValues())
      })
    ),
    form.hasFields ? form.element : h("span", {}),
    message,
    resultBox
  );

  const earlier = state.results.get(key);
  if (earlier) {
    showResult(source, resultBox, earlier);
  } else if (source.httpMethod === "GET") {
    // Reading data is safe, so GET tabs load straight away with the default inputs.
    run(source, form, resultBox, message, form.readValues());
  } else {
    // POST tabs change something on the other side, so the user must press Execute.
    resultBox.append(h("div", { class: "empty", text: "Fill in the form and press Execute." }));
  }
}

async function run(source, form, resultBox, message, values) {
  state.savedValues.set(source.key, values);
  clear(message);
  form.showErrors(null);
  form.setBusy(true);
  clear(resultBox);
  resultBox.append(h("div", { class: "loading" }, h("div", { class: "spinner" }), h("span", { text: "Calling " + source.title + " API..." })));

  try {
    const response = await api.execute(source.key, values);
    state.results.set(source.key, response);
    setStatus("ok", "Connected");
    if (state.activeKey === source.key) showResult(source, resultBox, response);
  } catch (err) {
    if (state.activeKey !== source.key) return;
    clear(resultBox);
    if (err instanceof ApiError && err.kind === "invalid") {
      form.showErrors(err.errors);
      message.append(h("div", { class: "notice notice-warn", text: err.message }));
    } else if (err instanceof ApiError && err.kind === "offline") {
      setStatus("bad", "Offline");
      resultBox.append(await offlineCard(err));
    } else {
      message.append(h("div", { class: "notice notice-bad", text: err.message }));
      resultBox.append(h("div", { class: "empty", text: "No data to show." }));
    }
  } finally {
    form.setBusy(false);
  }
}

function showResult(source, resultBox, response) {
  clear(resultBox);
  resultBox.append(renderResult(source, response));

  clear(els.footer);
  els.footer.append(
    h("span", { text: "Source: " + response.upstreamHost }),
    h("span", { text: response.durationMs + " ms" }),
    h("span", { class: response.fromCache ? "pill pill-cache" : "pill pill-live", text: response.fromCache ? "cached" : "live" }),
    h("span", { text: timeLabel(response.fetchedAtUtc) })
  );
}

// ---------- Messages ----------
async function offlineCard(err) {
  const base = await getApiBase();
  return h(
    "div",
    { class: "offline" },
    h("div", { class: "offline-title", text: "Backend is not reachable" }),
    h("p", { text: err.message }),
    h("p", { class: "hint" }, "Start the ASP.NET Core API (", h("code", { text: "dotnet run" }), ") and check the URL in settings. Current URL: ", h("code", { text: base })),
    h("button", { class: "btn", type: "button", text: "Try again", onclick: async () => { await checkHealth(); await loadSources(); } })
  );
}

function showMessage(kind, text) {
  els.content.prepend(h("div", { class: "notice notice-" + kind, text }));
}
