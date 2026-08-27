// ==========================================================================
// PAGE LOADER - shows a full-screen gif overlay the instant the user
// navigates away, and hides it once the destination page has finished
// loading. Runs in EVERY browser (unlike the crossfade below, which native
// View Transition browsers skip) because this covers the server/network
// gap that a crossfade alone doesn't - the loader is the thing visible
// while Cognify.Web is actually processing the request.
// ==========================================================================
(function () {
  const loader = document.getElementById("page-loader");
  if (!loader) return;

  function showLoader() {
    loader.classList.add("active");
  }
  function hideLoader() {
    loader.classList.remove("active");
  }

  function isSameOriginNavigableLink(anchor) {
    if (!anchor || !anchor.href) return false;
    if (anchor.target && anchor.target !== "_self") return false;
    if (anchor.hasAttribute("download")) return false;
    if (anchor.getAttribute("href")?.startsWith("#")) return false;
    try {
      const url = new URL(anchor.href, window.location.href);
      return url.origin === window.location.origin;
    } catch {
      return false;
    }
  }

  document.addEventListener("click", function (e) {
    const anchor = e.target.closest("a");
    if (!isSameOriginNavigableLink(anchor)) return;
    if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return; // let "open in new tab" etc. through untouched

    e.preventDefault();
    showLoader();
    // Let the loader paint on next frame before actually navigating
    requestAnimationFrame(() => { window.location.href = anchor.href; });
  });

  document.addEventListener("submit", function (e) {
    const form = e.target;
    if (form.tagName !== "FORM") return;
    try {
      const url = new URL(form.action || window.location.href, window.location.href);
      if (url.origin !== window.location.origin) return;
    } catch { return; }

    showLoader();
    // form submits natively; no preventDefault needed here
  });

  // Hide once the new page has fully loaded
  window.addEventListener("load", hideLoader);

  // Hide immediately if the page was restored from the back/forward cache
  window.addEventListener("pageshow", function (e) {
    if (e.persisted) hideLoader();
  });
})();


// (Chrome/Edge 126+ get the CSS-only crossfade in site.css and never touch this).
// Intercepts same-origin link clicks and form gets, fades #page-content out,
// then performs the real navigation so it doesn't feel like a hard cut.
// ==========================================================================
(function () {
  const supportsNativeViewTransitions =
    "startViewTransition" in document || CSS.supports("navigation", "auto");

  if (supportsNativeViewTransitions) return; // let the browser handle it natively

  const content = document.getElementById("page-content");
  if (!content) return;

  const FADE_MS = 220;

  function isSameOriginNavigableLink(anchor) {
    if (!anchor || !anchor.href) return false;
    if (anchor.target && anchor.target !== "_self") return false;
    if (anchor.hasAttribute("download")) return false;
    if (anchor.getAttribute("href")?.startsWith("#")) return false;
    try {
      const url = new URL(anchor.href, window.location.href);
      return url.origin === window.location.origin;
    } catch {
      return false;
    }
  }

  document.addEventListener("click", function (e) {
    const anchor = e.target.closest("a");
    if (!isSameOriginNavigableLink(anchor)) return;

    e.preventDefault();
    content.classList.add("is-leaving");
    setTimeout(() => { window.location.href = anchor.href; }, FADE_MS);
  });

  // Also fade out on same-origin form submissions (e.g. the Logout button)
  document.addEventListener("submit", function (e) {
    const form = e.target;
    if (form.tagName !== "FORM") return;
    try {
      const url = new URL(form.action || window.location.href, window.location.href);
      if (url.origin !== window.location.origin) return;
    } catch { return; }

    e.preventDefault();
    content.classList.add("is-leaving");
    setTimeout(() => form.submit(), FADE_MS);
  });
})();


// cycles the "selected" option and confidence pill to imply a live exam,
// purely decorative (no real exam logic lives here).
document.addEventListener("DOMContentLoaded", function () {
  const opts = document.querySelectorAll(".exam-card .exam-opt");
  const pills = document.querySelectorAll(".exam-card .confidence-pill");

  if (opts.length) {
    let idx = Array.from(opts).findIndex(o => o.classList.contains("selected"));
    setInterval(() => {
      opts.forEach(o => o.classList.remove("selected"));
      idx = (idx + 1) % opts.length;
      opts[idx].classList.add("selected");
    }, 3200);
  }

  if (pills.length) {
    let pIdx = Array.from(pills).findIndex(p => p.classList.contains("active"));
    setInterval(() => {
      pills.forEach(p => p.classList.remove("active"));
      pIdx = (pIdx + 1) % pills.length;
      pills[pIdx].classList.add("active");
    }, 3200);
  }
});
