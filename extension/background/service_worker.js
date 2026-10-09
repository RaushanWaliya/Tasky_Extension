// Tasky background service worker.
//
// Tasky uses Chrome's NATIVE side panel (the same way Apollo does it). Chrome itself makes the
// web page narrower, so the website's layout, CSS and media queries behave exactly as if the
// browser window had been resized. Tasky never touches the website's own layout.
//
// This file does three things:
//   1. Enables the side panel ONLY on the allowed websites (per tab).
//   2. Opens / closes the panel when the user clicks the launcher tab, the toolbar icon
//      or presses Alt+Shift+T.
//   3. Remembers which tabs have the panel open (the panel reports in through a port).

const ALLOWED_HOSTS = ["bumblebot.tech", "nexthink.com"]; // keep in sync with manifest.json
const PANEL_PATH = "panel/panel.html";

// Tabs whose panel is currently open. Filled by the panel itself (see onConnect below).
const openTabs = new Set();

// ---------- 1. Only allowed websites get a side panel ----------
function isAllowedUrl(url) {
  try {
    const parsed = new URL(url);
    return (
      parsed.protocol === "https:" &&
      ALLOWED_HOSTS.some((host) => parsed.hostname === host || parsed.hostname.endsWith("." + host))
    );
  } catch (e) {
    return false; // no url (tab not one of our websites) or not a valid url
  }
}

// Each tab gets its own panel address, so the panel knows which tab it belongs to.
function panelPathFor(tabId) {
  return PANEL_PATH + "?tabId=" + tabId;
}

async function applyToTab(tab) {
  if (!tab || tab.id === undefined) return;
  try {
    await chrome.sidePanel.setOptions({
      tabId: tab.id,
      path: panelPathFor(tab.id),
      enabled: isAllowedUrl(tab.url)
    });
  } catch (e) {
    // The tab was closed in the meantime. Nothing to do.
  }
}

async function setup() {
  // Everywhere else: no side panel. The toolbar icon opens it only through our own code.
  await chrome.sidePanel.setOptions({ enabled: false });
  await chrome.sidePanel.setPanelBehavior({ openPanelOnActionClick: false });
  const tabs = await chrome.tabs.query({});
  tabs.forEach(applyToTab);
}

chrome.runtime.onInstalled.addListener(setup);
chrome.runtime.onStartup.addListener(setup);

chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  if (changeInfo.url || changeInfo.status === "loading" || changeInfo.status === "complete") {
    applyToTab(tab);
  }
});

chrome.tabs.onRemoved.addListener((tabId) => openTabs.delete(tabId));

// ---------- 2. Open and close ----------
// IMPORTANT: chrome.sidePanel.open() only works inside the user's click. So it must be called
// directly in the event handler, with no "await" before it.
function togglePanel(tabId) {
  if (tabId === undefined) return;
  if (openTabs.has(tabId)) {
    closePanel(tabId);
  } else {
    chrome.sidePanel.open({ tabId }).catch((err) => {
      // Not one of our websites, or Chrome refused (for example it was not a real click).
      console.warn("Tasky: the side panel did not open:", err && err.message);
    });
  }
}

function closePanel(tabId) {
  if (typeof chrome.sidePanel.close === "function") {
    chrome.sidePanel.close({ tabId }).catch(() => {});
    return;
  }
  // Older Chrome without sidePanel.close(): switching the panel off and on closes it.
  chrome.sidePanel
    .setOptions({ tabId, enabled: false })
    .then(() => chrome.sidePanel.setOptions({ tabId, path: panelPathFor(tabId), enabled: true }))
    .catch(() => {});
}

chrome.action.onClicked.addListener((tab) => togglePanel(tab.id));

chrome.commands.onCommand.addListener((command, tab) => {
  if (command === "toggle-panel" && tab) togglePanel(tab.id);
});

chrome.runtime.onMessage.addListener((message, sender) => {
  if (!message) return;
  if (message.type === "TASKY_LAUNCHER_CLICK" && sender.tab) togglePanel(sender.tab.id);
  if (message.type === "TASKY_CLOSE_PANEL" && typeof message.tabId === "number") closePanel(message.tabId);
});

// ---------- 3. Know which panels are open ----------
function tellTab(tabId, open) {
  chrome.tabs.sendMessage(tabId, { type: "TASKY_STATE", open }).catch(() => {});
}

chrome.runtime.onConnect.addListener((port) => {
  const match = /^tasky-panel:(\d+)$/.exec(port.name);
  if (!match) return;
  const tabId = Number(match[1]);

  openTabs.add(tabId);
  tellTab(tabId, true);

  port.onMessage.addListener(() => {
    // Heartbeat from the panel; receiving it keeps this worker awake.
  });
  port.onDisconnect.addListener(() => {
    openTabs.delete(tabId);
    tellTab(tabId, false);
  });
});
