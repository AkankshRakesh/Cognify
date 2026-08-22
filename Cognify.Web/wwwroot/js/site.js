// Lightweight ambient interaction for the hero mock exam card:
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
