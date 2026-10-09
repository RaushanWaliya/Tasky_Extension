// Tasky content script. Runs only on the websites listed in manifest.json.
//
// It adds ONE small button (the "tab") on the right edge of the page:
//   * drag it up or down to move it (the position is remembered),
//   * click it to open or close Tasky's side panel.
//
// It does not touch the website's layout in any way. The panel is Chrome's native side panel,
// which Chrome itself fits next to the page.
(() => {
  "use strict";

  if (window.__taskyLoaded) return;
  window.__taskyLoaded = true;

  const POSITION_KEY = "taskyLauncherTop";
  const DRAG_THRESHOLD_PX = 4;
  const LAUNCHER_HEIGHT = 48;

  const launcher = document.createElement("button");
  launcher.id = "tasky-launcher";
  launcher.type = "button";
  launcher.title = "Tasky: click to open, drag to move";
  launcher.setAttribute("aria-label", "Open Tasky panel");
  launcher.setAttribute("aria-expanded", "false");
  document.documentElement.appendChild(launcher);

  // ---------- Position ----------
  function clampTop(top) {
    const max = Math.max(0, window.innerHeight - LAUNCHER_HEIGHT);
    return Math.min(Math.max(0, top), max);
  }

  function setLauncherTop(top) {
    launcher.style.top = clampTop(top) + "px";
  }

  function restoreLauncherPosition() {
    try {
      chrome.storage.local.get(POSITION_KEY).then((stored) => {
        if (typeof stored[POSITION_KEY] === "number") setLauncherTop(stored[POSITION_KEY]);
      });
    } catch (e) {
      // Storage not available: keep the default position.
    }
  }

  function saveLauncherPosition() {
    try {
      chrome.storage.local.set({ [POSITION_KEY]: launcher.getBoundingClientRect().top });
    } catch (e) {
      // Ignore.
    }
  }

  // ---------- Drag up and down along the right edge ----------
  let dragStartY = 0;
  let dragStartTop = 0;
  let pointerIsDown = false;
  let didDrag = false;

  launcher.addEventListener("pointerdown", (event) => {
    if (event.button !== 0) return;
    pointerIsDown = true;
    didDrag = false;
    dragStartY = event.clientY;
    dragStartTop = launcher.getBoundingClientRect().top;
    launcher.setPointerCapture(event.pointerId);
  });

  launcher.addEventListener("pointermove", (event) => {
    if (!pointerIsDown) return;
    const moved = event.clientY - dragStartY;
    if (!didDrag && Math.abs(moved) < DRAG_THRESHOLD_PX) return;
    didDrag = true;
    launcher.classList.add("tasky-dragging");
    setLauncherTop(dragStartTop + moved);
  });

  function endDrag(event) {
    if (!pointerIsDown) return;
    pointerIsDown = false;
    launcher.classList.remove("tasky-dragging");
    if (launcher.hasPointerCapture(event.pointerId)) launcher.releasePointerCapture(event.pointerId);
    if (didDrag) saveLauncherPosition();
  }
  launcher.addEventListener("pointerup", endDrag);
  launcher.addEventListener("pointercancel", endDrag);

  // A drag ends with a "click" event too. Ignore that one, so dragging never opens the panel.
  // IMPORTANT: no async code here. Chrome only lets the background open the side panel
  // when the message is sent straight from the user's click.
  launcher.addEventListener("click", () => {
    if (didDrag) {
      didDrag = false;
      return;
    }
    try {
      chrome.runtime.sendMessage({ type: "TASKY_LAUNCHER_CLICK" }).catch(() => {});
    } catch (e) {
      // The extension was reloaded; this page needs a refresh.
    }
  });

  window.addEventListener("resize", () => {
    setLauncherTop(launcher.getBoundingClientRect().top);
  });

  // ---------- The background tells us if the panel is open (to colour the tab) ----------
  chrome.runtime.onMessage.addListener((message) => {
    if (!message || message.type !== "TASKY_STATE") return;
    launcher.classList.toggle("tasky-active", Boolean(message.open));
    launcher.setAttribute("aria-expanded", message.open ? "true" : "false");
    launcher.setAttribute("aria-label", message.open ? "Close Tasky panel" : "Open Tasky panel");
  });

  restoreLauncherPosition();
})();
