/*
 * deck-zoom.js — click-to-zoom for diagrams in the Marp HTML deck.
 *
 * Injected into deck.html by build.sh after Marp runs, so it survives rebuilds
 * without patching Marp's template.
 *
 * Any <img> pointing at assets/*.svg becomes clickable. Clicking opens a
 * full-window overlay that inlines the SVG and hands it to panzoom, so zooming
 * transforms the vector and stays sharp at any magnification.
 *
 * Falls back to an <img> wrapper when fetch() is unavailable (file:// origin).
 * Swallows keys while open so the deck does not change slide underneath.
 */
(function () {
  "use strict";

  if (typeof panzoom !== "function") return;

  var SEL = 'img[src$=".svg"]';
  var pz = null, overlay = null, lastFocus = null;

  var css = `
  .dz-hit { cursor: zoom-in; transition: filter .12s ease, transform .12s ease; }
  .dz-hit:hover { filter: drop-shadow(0 0 0 #1a4d7a) brightness(.97); transform: scale(1.006); }
  .dz-badge { position: absolute; pointer-events: none; opacity: 0; transition: opacity .12s;
              background: rgba(26,77,122,.92); color: #fff; font: 600 11px -apple-system, system-ui, sans-serif;
              padding: 3px 8px; border-radius: 4px; letter-spacing: .3px; z-index: 5; }
  .dz-hit:hover + .dz-badge, .dz-badge.on { opacity: 1; }

  .dz-ov { position: fixed; inset: 0; z-index: 99999; background: #fff;
           display: grid; grid-template-rows: auto 1fr; animation: dz-in .14s ease; }
  @keyframes dz-in { from { opacity: 0 } to { opacity: 1 } }
  .dz-top { display: flex; align-items: center; gap: 14px; padding: 10px 16px;
            border-bottom: 1px solid #dde3e9; background: #f6f8fa;
            font: 14px -apple-system, system-ui, sans-serif; color: #1a1a1a; }
  .dz-top b { font-size: 15px; color: #1a4d7a; }
  .dz-top .dz-sp { flex: 1; }
  .dz-top code { font: 12px ui-monospace, Menlo, monospace; color: #667; }
  .dz-stage { position: relative; overflow: hidden; cursor: grab; background: #fff; }
  .dz-stage:active { cursor: grabbing; }
  .dz-stage svg, .dz-stage > div { display: block; }
  .dz-bar { position: absolute; left: 50%; transform: translateX(-50%); bottom: 20px;
            background: rgba(255,255,255,.97); border: 1px solid #d5dde5; border-radius: 10px;
            padding: 7px 10px; display: flex; gap: 6px; align-items: center;
            box-shadow: 0 4px 20px rgba(20,40,60,.16); }
  .dz-bar button { font: 13px -apple-system, system-ui, sans-serif; background: #fff; color: #1a1a1a;
                   border: 1px solid #cfd8e0; border-radius: 6px; padding: 5px 11px; cursor: pointer; }
  .dz-bar button:hover { background: #eef4fa; border-color: #9fb8cd; }
  .dz-bar .dz-z { min-width: 58px; text-align: center; color: #667;
                  font: 12px ui-monospace, Menlo, monospace; }
  .dz-bar .dz-sep { width: 1px; height: 20px; background: #d5dde5; margin: 0 3px; }
  .dz-hint { position: absolute; right: 18px; bottom: 22px; color: #7a8894;
             font: 12px -apple-system, system-ui, sans-serif; text-align: right; line-height: 1.7; }
  .dz-hint kbd { background: #f0f3f6; border: 1px solid #d5dde5; border-bottom-width: 2px;
                 border-radius: 4px; padding: 1px 5px; font: 11px ui-monospace, Menlo, monospace; }
  `;
  var st = document.createElement("style");
  st.textContent = css;
  document.head.appendChild(st);

  function label(src) {
    var f = src.split("/").pop();
    return ({
      "L2-layered-stack.svg": "Layered stack",
      "L4-connection.svg": "Connection / transports",
      "L3-apdu-sequence.svg": "APDU sequence",
      "L3b-fido2-ctap-sequence.svg": "FIDO2 / CTAP-HID sequence",
      "L4-discovery.svg": "Device discovery",
      "observability-before-after.svg": "Observability: v1 → v2",
      "L0-context.svg": "System context",
      "L1-assembly-deps.svg": "Assembly dependencies"
    })[f] || f;
  }

  function paint() {
    if (!pz || !overlay) return;
    overlay.querySelector(".dz-z").textContent =
      Math.round(pz.getTransform().scale * 100) + "%";
  }

  // Insets reserve room for the floating chrome so fit() never tucks diagram
  // content under the toolbar. Bottom is largest: toolbar + hint live there.
  var INSET = { top: 24, right: 32, bottom: 96, left: 32 };

  function fit() {
    if (!pz || !overlay) return;
    var stage = overlay.querySelector(".dz-stage");
    var svg = stage.querySelector("svg");
    var r = stage.getBoundingClientRect();
    var w, h;
    if (svg && svg.viewBox && svg.viewBox.baseVal && svg.viewBox.baseVal.width) {
      var vb = svg.viewBox.baseVal; w = vb.width; h = vb.height;
    } else {
      var im = stage.querySelector("img");
      w = (im && im.naturalWidth) || r.width;
      h = (im && im.naturalHeight) || r.height;
    }
    var availW = Math.max(40, r.width - INSET.left - INSET.right);
    var availH = Math.max(40, r.height - INSET.top - INSET.bottom);
    var s = Math.min(availW / w, availH / h);
    // The element is sized to the viewBox, so the browser already maps viewBox
    // (x, y) to the element origin. Do not subtract vb.x/vb.y again — doing so
    // double-corrects and shifts diagrams with a negative viewBox origin.
    pz.zoomAbs(0, 0, 1); pz.moveTo(0, 0); pz.zoomAbs(0, 0, s);
    pz.moveTo(INSET.left + (availW - w * s) / 2, INSET.top + (availH - h * s) / 2);
    paint();
  }

  function step(k) {
    if (!pz || !overlay) return;
    var r = overlay.querySelector(".dz-stage").getBoundingClientRect();
    pz.smoothZoom(r.width / 2, r.height / 2, k);
    setTimeout(paint, 320);
  }

  function close() {
    if (!overlay) return;
    if (pz) { pz.dispose(); pz = null; }
    overlay.remove(); overlay = null;
    document.removeEventListener("keydown", keys, true);
    if (lastFocus && lastFocus.focus) lastFocus.focus();
  }

  function keys(e) {
    if (!overlay) return;
    var k = e.key.toLowerCase();
    var handled = true;
    if (k === "escape") close();
    else if (k === "+" || k === "=") step(1.45);
    else if (k === "-" || k === "_") step(1 / 1.45);
    else if (k === "0") fit();
    else if (k === "1") {
      var r = overlay.querySelector(".dz-stage").getBoundingClientRect();
      pz.smoothZoomAbs(r.width / 2, r.height / 2, 1); setTimeout(paint, 320);
    } else handled = false;
    // Always stop propagation so Marp's slide navigation never fires behind the overlay.
    e.stopPropagation();
    if (handled) e.preventDefault();
  }

  async function open(src) {
    if (overlay) return;
    lastFocus = document.activeElement;

    overlay = document.createElement("div");
    overlay.className = "dz-ov";
    overlay.innerHTML =
      '<div class="dz-top"><b>' + label(src) + "</b><code>" + src + "</code>" +
      '<span class="dz-sp"></span>' +
      '<button class="dz-x" style="font:13px -apple-system,system-ui,sans-serif;background:#fff;' +
      'border:1px solid #cfd8e0;border-radius:6px;padding:5px 12px;cursor:pointer">Close &nbsp;Esc</button></div>' +
      '<div class="dz-stage"></div>' +
      '<div class="dz-hint">scroll to zoom &nbsp;·&nbsp; drag to pan</div>' +
      '<div class="dz-bar"><button class="dz-out" title="Zoom out  −">−</button>' +
      '<div class="dz-z">100%</div>' +
      '<button class="dz-in" title="Zoom in  +">+</button><div class="dz-sep"></div>' +
      '<button class="dz-fit" title="Fit to window  0">fit</button>' +
      '<button class="dz-one" title="Actual size  1">1:1</button></div>';
    document.body.appendChild(overlay);

    var stage = overlay.querySelector(".dz-stage");
    var target;
    try {
      var txt = await (await fetch(src)).text();
      stage.innerHTML = txt;
      target = stage.querySelector("svg");
      if (!target) throw new Error("no svg");
      // Size the SVG to its intrinsic viewBox in px. If we left it at 100% the SVG
      // would already self-fit via preserveAspectRatio, and fit()'s scale would then
      // shrink an already-fitted image (the 32%-underfill bug).
      var vb0 = target.viewBox && target.viewBox.baseVal;
      target.removeAttribute("width"); target.removeAttribute("height");
      if (vb0 && vb0.width) {
        target.style.width = vb0.width + "px";
        target.style.height = vb0.height + "px";
      }
      target.style.maxWidth = "none";
      target.style.transformOrigin = "0 0";
    } catch (err) {
      var wrap = document.createElement("div");
      var im = document.createElement("img");
      im.src = src; im.style.cssText = "max-width:none;height:auto";
      wrap.appendChild(im); stage.innerHTML = ""; stage.appendChild(wrap);
      await new Promise(function (r) { im.complete ? r() : (im.onload = r, im.onerror = r); });
      im.style.width = im.naturalWidth + "px";
      target = wrap;
    }

    pz = panzoom(target, {
      maxZoom: 60, minZoom: 0.05, smoothScroll: false,
      zoomDoubleClickSpeed: 2.2, beforeWheel: function () { return false; },
      filterKey: function () { return true; }
    });
    pz.on("zoom", paint); pz.on("pan", paint);
    fit();

    overlay.querySelector(".dz-x").onclick = close;
    overlay.querySelector(".dz-in").onclick = function () { step(1.45); };
    overlay.querySelector(".dz-out").onclick = function () { step(1 / 1.45); };
    overlay.querySelector(".dz-fit").onclick = fit;
    overlay.querySelector(".dz-one").onclick = function () {
      var r = stage.getBoundingClientRect();
      pz.smoothZoomAbs(r.width / 2, r.height / 2, 1); setTimeout(paint, 320);
    };
    document.addEventListener("keydown", keys, true);
    window.addEventListener("resize", fit);
  }

  function wire(root) {
    (root || document).querySelectorAll(SEL).forEach(function (img) {
      if (img.dataset.dz) return;
      if (/^data:|^https?:/.test(img.getAttribute("src") || "")) return; // skip emoji/CDN
      img.dataset.dz = "1";
      img.classList.add("dz-hit");
      img.title = "Click to zoom";
      var b = document.createElement("span");
      b.className = "dz-badge";
      b.textContent = "⤢ click to zoom";
      img.insertAdjacentElement("afterend", b);
      var r = img.getBoundingClientRect();
      img.addEventListener("click", function (e) {
        e.preventDefault(); e.stopPropagation();
        open(img.getAttribute("src"));
      });
    });
  }

  if (document.readyState === "loading")
    document.addEventListener("DOMContentLoaded", function () { wire(); });
  else wire();

  // Marp's bespoke player swaps slide DOM in some modes; re-wire on mutation.
  new MutationObserver(function () { wire(); })
    .observe(document.body, { childList: true, subtree: true });
})();
