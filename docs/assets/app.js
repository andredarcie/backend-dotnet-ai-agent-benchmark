/* ==================================================================
   app.js — renders the dynamic bits (leaderboard, rubric, diagrams),
   drives the PT/EN toggle and the theme, from:
     window.__BENCHMARK__  (generated → data/data.js)
     window.CONTENT        (authored → content.js)
   ================================================================== */
(function () {
  "use strict";

  var DATA = window.__BENCHMARK__ || { leaderboard: [], runs: [], generatedAtUtc: "", modelCount: 0, runCount: 0 };
  var C = window.CONTENT;
  var root = document.documentElement;
  var lang = root.getAttribute("lang") === "pt" ? "pt" : "en";
  var critSort = "weight";

  var AUTO_VAR = { FullAuto: "--auto-full", SemiOracle: "--auto-semi", ProxyReview: "--auto-proxy" };
  var AUTO_CLS = { FullAuto: "dg-full", SemiOracle: "dg-semi", ProxyReview: "dg-proxy" };

  function t(k) {
    var d = C.ui[lang];
    if (d && d[k] != null) return d[k];
    return (C.ui.en[k] != null) ? C.ui.en[k] : k;
  }
  function esc(s) {
    return String(s == null ? "" : s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
  }
  function fx(n, d) { return Number(n).toFixed(d == null ? 1 : d); }

  /* ---------- SVG helpers ---------- */
  function svg(vb, inner, cls) {
    return '<svg class="dg ' + (cls || "") + '" viewBox="' + vb + '" role="img" preserveAspectRatio="xMidYMid meet">' + inner + "</svg>";
  }
  function box(x, y, w, h, cls, r) {
    return '<rect x="' + x + '" y="' + y + '" width="' + w + '" height="' + h + '" rx="' + (r == null ? 10 : r) + '" class="' + (cls || "dg-box") + '" stroke-width="1.4"/>';
  }
  function tx(x, y, s, cls, size, anchor) {
    return '<text x="' + x + '" y="' + y + '" text-anchor="' + (anchor || "start") + '" font-size="' + (size || 12) + '" class="' + (cls || "") + '">' + esc(s) + "</text>";
  }
  function aRight(x1, x2, y, cls) {
    return '<line x1="' + x1 + '" y1="' + y + '" x2="' + (x2 - 6) + '" y2="' + y + '" class="' + (cls || "dg-line") + '" stroke-width="1.6"/>' +
      '<path d="M' + x2 + ' ' + y + ' l-7 -4 l0 8 z" class="' + (cls === "dg-line-accent" ? "dg-fill-accent" : "dg-arrow") + '"/>';
  }
  function aDown(x, y1, y2, cls) {
    return '<line x1="' + x + '" y1="' + y1 + '" x2="' + x + '" y2="' + (y2 - 6) + '" class="' + (cls || "dg-line") + '" stroke-width="1.6"/>' +
      '<path d="M' + x + ' ' + y2 + ' l-4 -7 l8 0 z" class="' + (cls === "dg-line-accent" ? "dg-fill-accent" : "dg-arrow") + '"/>';
  }

  /* =====================================================================
     DIAGRAMS
     ===================================================================== */

  // horizontal 4-step pipeline for the "how it works" section
  function dgPipelineWide() {
    var steps = [
      [t("hero.pipeline.prompt"), t("hero.pipeline.promptSub")],
      [t("hero.pipeline.model"), t("hero.pipeline.modelSub")],
      [t("hero.pipeline.eval"), t("hero.pipeline.evalSub")],
      [t("hero.pipeline.score"), t("hero.pipeline.scoreSub")]
    ];
    var i = "", x = 6, W = 190, gap = 200, y = 18, H = 66;
    for (var n = 0; n < steps.length; n++) {
      var accent = (n === 3);
      i += '<rect x="' + x + '" y="' + y + '" width="' + W + '" height="' + H + '" rx="13" class="dg-box" stroke-width="1.4" ' + (accent ? 'style="stroke:var(--accent)"' : "") + '/>';
      i += '<rect x="' + x + '" y="' + (y + 14) + '" width="4" height="' + (H - 28) + '" rx="2" class="' + (accent ? "dg-fill-accent" : "dg-muted-fill") + '"/>';
      i += tx(x + 20, y + 29, steps[n][0], "dg-strong", 13.5);
      i += tx(x + 20, y + 49, steps[n][1], "dg-muted", 9.5);
      if (n < 3) i += aRight(x + W, x + gap, y + H / 2, "dg-line-accent");
      x += gap;
    }
    return svg("0 0 800 104", i);
  }

  // domain 1:N + event
  function dgDomain() {
    var i = "";
    // CreditCard entity
    i += box(16, 14, 196, 158, "dg-box");
    i += '<rect x="16" y="14" width="196" height="30" rx="10" class="dg-box2"/>';
    i += tx(28, 34, t("task.card"), "dg-strong", 13);
    i += tx(196, 34, "1", "dg-accent", 13, "end");
    var cf = ["id · PK", "cardholderName *", "cardNumber  🔒", "creditLimit ≥ 0", "brand?  createdAt"];
    for (var a = 0; a < cf.length; a++) i += tx(28, 66 + a * 21, cf[a], (a === 2 ? "dg-accent" : "dg-muted"), 11.5);
    // Transaction entity
    i += box(272, 14, 196, 176, "dg-box");
    i += '<rect x="272" y="14" width="196" height="30" rx="10" class="dg-box2"/>';
    i += tx(284, 34, t("task.tx"), "dg-strong", 13);
    i += tx(452, 34, "N", "dg-accent", 13, "end");
    var tf = ["id · PK", "creditCardId · FK", "amount > 0", "merchant *", "category?", "createdAt"];
    for (var b = 0; b < tf.length; b++) i += tx(284, 66 + b * 21, tf[b], (b === 1 ? "dg-accent" : "dg-muted"), 11.5);
    // relation 1 --< N
    i += '<line x1="212" y1="60" x2="272" y2="60" class="dg-line-accent" stroke-width="1.6"/>';
    i += '<path d="M272 60 l-12 -6 M272 60 l-12 6 M272 60 l-12 0" class="dg-line-accent" stroke-width="1.4" fill="none"/>';
    i += tx(242, 51, t("task.oneToMany"), "dg-accent", 11, "middle");
    i += tx(242, 74, "FK", "dg-muted", 10, "middle");
    // event flow
    var y = 224;
    i += '<rect x="16" y="' + y + '" width="132" height="42" rx="10" class="dg-box" stroke-width="1.4"/>';
    i += tx(82, y + 19, "POST", "dg-strong", 11.5, "middle");
    i += tx(82, y + 33, "/api/transactions", "dg-muted", 9.5, "middle");
    i += aRight(148, 196, y + 21, "dg-line-accent");
    i += '<rect x="196" y="' + y + '" width="96" height="42" rx="10" class="dg-box" style="stroke:var(--accent)" stroke-width="1.4"/>';
    i += tx(244, y + 26, "201", "dg-accent", 15, "middle");
    i += aRight(292, 344, y + 21, "dg-line-accent");
    i += '<rect x="344" y="' + y + '" width="124" height="42" rx="10" class="dg-box2" stroke-width="1.4"/>';
    i += tx(406, y + 19, "Kafka", "dg-strong", 11.5, "middle");
    i += tx(406, y + 33, "topic: transactions", "dg-muted", 9, "middle");
    i += tx(406, y + 58, "key = id", "dg-muted", 9.5, "middle");
    return svg("0 0 484 296", i);
  }

  // functional: HTTP -> rule -> persist -> event
  function dgRequestFlow() {
    var nodes = [["HTTP", "request"], ["rule", "amount>0, FK"], ["persist", "Postgres"], ["event", "Kafka"]];
    var i = "", x = 8, W = 104, gap = 118, y = 18, H = 54;
    for (var n = 0; n < nodes.length; n++) {
      var accent = (n === nodes.length - 1);
      i += '<rect x="' + x + '" y="' + y + '" width="' + W + '" height="' + H + '" rx="11" class="dg-box" stroke-width="1.4" ' + (accent ? 'style="stroke:var(--accent)"' : "") + '/>';
      i += tx(x + W / 2, y + 24, nodes[n][0], "dg-strong", 13, "middle");
      i += tx(x + W / 2, y + 41, nodes[n][1], "dg-muted", 10.5, "middle");
      if (n < nodes.length - 1) i += aRight(x + W, x + gap, y + H / 2, "dg-line-accent");
      x += gap;
    }
    i += tx(8, y + H + 22, "✓ 100% acceptance · dotnet test", "dg-full", 11);
    return svg("0 0 480 108", i);
  }

  // architecture: nested layers, deps inward
  function dgArch() {
    var i = "";
    i += box(24, 16, 412, 176, "dg-box2");
    i += tx(40, 36, "Presentation · Controllers", "dg-muted", 11.5);
    i += box(70, 48, 320, 132, "dg-box");
    i += tx(86, 68, "Application · use cases", "dg-muted", 11.5);
    i += '<rect x="128" y="82" width="204" height="82" rx="12" class="dg-box" style="stroke:var(--accent)" stroke-width="1.6"/>';
    i += tx(230, 112, "Domain", "dg-strong", 15, "middle");
    i += tx(230, 132, "entities · rules", "dg-muted", 10.5, "middle");
    // inward arrows
    i += aDown(230, 48, 80, "dg-line-accent");
    i += '<line x1="360" y1="150" x2="336" y2="140" class="dg-line-accent" stroke-width="1.5"/><path d="M332 138 l10 1 l-4 8 z" class="dg-fill-accent"/>';
    i += tx(348, 176, "Infrastructure →", "dg-muted", 10.5, "end");
    i += tx(468, 100, "deps → inward", "dg-accent", 10.5, "end");
    return svg("0 0 480 206", i);
  }

  // REST verb/status matrix
  function dgRest() {
    var rows = [
      ["POST /…", "201", "dg-full", "+ Location"],
      ["GET /{id}", "200 · 404", "dg-full", ""],
      ["PUT /{id}", "204 · 404", "dg-full", ""],
      ["DELETE /{id}", "204 · 404", "dg-full", ""],
      ["invalid body", "400", "dg-proxy", "problem+json"]
    ];
    var i = "", y = 12, rh = 34;
    for (var r = 0; r < rows.length; r++) {
      var row = rows[r];
      i += '<rect x="8" y="' + y + '" width="464" height="' + (rh - 6) + '" rx="8" class="dg-box2" stroke-width="1"/>';
      i += tx(22, y + 19, row[0], "dg-strong", 12.5);
      i += '<rect x="250" y="' + (y + 5) + '" width="96" height="' + (rh - 16) + '" rx="9" fill="none" class="' + row[2] + '" stroke-width="1.4" fill-opacity="0.12"/>';
      i += tx(298, y + 19, row[1], row[2] + "-t", 12, "middle");
      if (row[3]) i += tx(360, y + 19, row[3], "dg-muted", 10.5);
      y += rh;
    }
    return svg("0 0 480 " + (y + 4), i);
  }

  // transactional outbox
  function dgOutbox() {
    var i = "";
    // API
    i += box(12, 20, 96, 44, "dg-box");
    i += tx(60, 46, "API", "dg-strong", 13, "middle");
    // Postgres w/ tx boundary
    i += '<rect x="168" y="10" width="184" height="96" rx="12" class="dg-box2" stroke-dasharray="5 4" stroke-width="1.4"/>';
    i += tx(176, 26, "one DB transaction", "dg-muted", 10);
    i += box(180, 32, 160, 30, "dg-box", 8);
    i += tx(192, 51, "transactions row", "dg-strong", 11);
    i += box(180, 68, 160, 30, "dg-box", 8);
    i += tx(192, 87, "outbox row", "dg-accent", 11);
    i += aRight(108, 168, 42, "dg-line-accent");
    i += tx(138, 34, "write", "dg-muted", 9.5, "middle");
    // dispatcher
    i += aDown(260, 106, 138, "dg-line");
    i += box(180, 138, 160, 36, "dg-box");
    i += tx(260, 161, "outbox dispatcher", "dg-strong", 11.5, "middle");
    // kafka
    i += aRight(340, 400, 156, "dg-line-accent");
    i += box(400, 138, 76, 36, "dg-box", 8);
    i += tx(438, 161, "Kafka", "dg-accent", 12, "middle");
    // consumer
    i += aDown(260, 174, 206, "dg-line");
    i += box(120, 206, 280, 40, "dg-box", 10);
    i += tx(260, 224, "idempotent consumer · dedupe by id", "dg-strong", 11, "middle");
    i += tx(260, 239, "commit offset AFTER processing", "dg-muted", 9.5, "middle");
    // DLQ
    i += '<line x1="400" y1="226" x2="452" y2="226" class="dg-line" stroke-width="1.4" stroke-dasharray="4 3"/>';
    i += tx(430, 220, "DLQ", "dg-proxy-t", 10, "middle");
    return svg("0 0 484 258", i);
  }

  // PAN masking (PCI)
  function dgPan() {
    var i = "";
    i += tx(16, 30, "PAN input", "dg-muted", 11);
    i += box(16, 38, 300, 34, "dg-box");
    i += tx(30, 60, "4111 1111 1111 1111", "dg-strong", 14);
    i += aDown(60, 74, 104, "dg-line-accent");
    i += tx(74, 96, "encrypt / tokenize / truncate", "dg-accent", 10.5);
    i += tx(16, 128, "stored", "dg-muted", 11);
    i += box(16, 136, 300, 34, "dg-box", 10);
    i += tx(30, 158, "•••• •••• •••• 1111", "dg-accent", 14);
    // never stored
    i += box(340, 60, 130, 90, "dg-box2");
    i += tx(405, 82, "CVV · PIN", "dg-proxy-t", 12, "middle");
    i += tx(405, 100, "track data", "dg-proxy-t", 12, "middle");
    i += '<line x1="356" y1="112" x2="454" y2="112" class="dg-proxy" stroke-width="1.6"/>';
    i += tx(405, 134, "never stored", "dg-muted", 10.5, "middle");
    return svg("0 0 484 186", i);
  }

  // resilience: retry + circuit breaker
  function dgResilience() {
    var i = "";
    i += box(12, 30, 96, 44, "dg-box");
    i += tx(60, 56, "request", "dg-strong", 12, "middle");
    i += aRight(108, 168, 52, "dg-line-accent");
    i += box(168, 30, 120, 44, "dg-box", 10);
    i += tx(228, 50, "timeout +", "dg-strong", 11.5, "middle");
    i += tx(228, 65, "retry (backoff)", "dg-muted", 10, "middle");
    i += aRight(288, 344, 52, "dg-line-accent");
    i += box(344, 30, 124, 44, "dg-box");
    i += tx(406, 56, "DB / Kafka", "dg-strong", 12, "middle");
    // retry loop
    i += '<path d="M228 30 q0 -20 -40 -20 q-40 0 -40 20" class="dg-line-accent" fill="none" stroke-width="1.4"/><path d="M148 30 l-3 -7 l7 2 z" class="dg-fill-accent"/>';
    // breaker states
    var st = [["closed", "dg-full-t"], ["open", "dg-proxy-t"], ["half-open", "dg-semi-t"]];
    var x = 40;
    i += tx(16, 118, "circuit breaker", "dg-muted", 10.5);
    for (var s = 0; s < st.length; s++) {
      i += '<rect x="' + x + '" y="128" width="118" height="34" rx="17" class="dg-box2" stroke-width="1.3"/>';
      i += tx(x + 59, 149, st[s][0], st[s][1], 12, "middle");
      if (s < st.length - 1) i += aRight(x + 118, x + 148, 145, "dg-line");
      x += 148;
    }
    return svg("0 0 484 178", i);
  }

  // test pyramid
  function dgPyramid() {
    var i = "";
    // three trapezoids
    i += '<path d="M150 20 L230 20 L246 66 L134 66 Z" stroke-width="1.4" style="stroke:var(--accent);fill:var(--accent-soft)"/>';
    i += tx(190, 48, "acceptance", "dg-accent", 11.5, "middle");
    i += '<path d="M120 74 L260 74 L286 128 L94 128 Z" class="dg-box2" stroke-width="1.4"/>';
    i += tx(190, 105, "integration · Testcontainers", "dg-strong", 11, "middle");
    i += '<path d="M78 136 L302 136 L330 196 L50 196 Z" class="dg-box2" stroke-width="1.4"/>';
    i += tx(190, 170, "unit · business rules", "dg-strong", 11.5, "middle");
    i += tx(350, 96, "coverage", "dg-muted", 11);
    i += tx(350, 116, "≥ 80%", "dg-full-t", 15);
    i += tx(350, 140, "critical path", "dg-muted", 10);
    return svg("0 0 440 206", i);
  }

  // observability pillars + correlation id
  function dgPillars() {
    var i = "";
    var cols = ["logs", "metrics", "traces"];
    var x = 40, W = 96, gap = 132;
    // correlation thread
    i += '<line x1="24" y1="34" x2="416" y2="34" class="dg-line-accent" stroke-width="1.6" stroke-dasharray="6 4"/>';
    i += tx(24, 26, "correlation id — HTTP → event → consume", "dg-accent", 10.5);
    for (var c = 0; c < cols.length; c++) {
      i += '<rect x="' + x + '" y="46" width="' + W + '" height="118" rx="10" class="dg-box2" stroke-width="1.4"/>';
      i += '<rect x="' + x + '" y="46" width="' + W + '" height="6" rx="3" class="dg-fill-accent"/>';
      i += tx(x + W / 2, 112, cols[c], "dg-strong", 13, "middle");
      x += gap;
    }
    i += '<line x1="20" y1="172" x2="420" y2="172" class="dg-line" stroke-width="1.6"/>';
    i += tx(220, 190, "/health · /metrics · structured JSON", "dg-muted", 10.5, "middle");
    return svg("0 0 440 200", i);
  }

  // scoring flow: metric -> category -> final
  function dgScoring() {
    var i = "";
    var chips = [["Pass", "1.0", "dg-full"], ["Partial", "0.5", "dg-semi"], ["Fail", "0.0", "dg-proxy"]];
    i += tx(18, 22, t("scoring.step1.title"), "dg-muted", 11);
    var y = 34;
    for (var c = 0; c < chips.length; c++) {
      i += '<rect x="18" y="' + (y + c * 30) + '" width="150" height="24" rx="12" class="dg-box2" stroke-width="1.2"/>';
      i += '<circle cx="34" cy="' + (y + c * 30 + 12) + '" r="5" class="' + chips[c][2] + '"/>';
      i += tx(48, y + c * 30 + 16, chips[c][0], "dg-strong", 11.5);
      i += tx(160, y + c * 30 + 16, chips[c][1], chips[c][2] + "-t", 12, "end");
    }
    i += tx(18, y + 108, "Indeterminate → excluded", "dg-muted", 10);
    // arrows
    i += aRight(176, 236, 78, "dg-line-accent");
    // category
    i += box(236, 44, 150, 70, "dg-box", 12);
    i += tx(311, 70, t("scoring.step2.title"), "dg-muted", 10, "middle");
    i += tx(311, 92, "× 5 → /5", "dg-strong", 14, "middle");
    i += aRight(392, 452, 78, "dg-line-accent");
    // final
    i += box(452, 44, 154, 70, "dg-box", 12);
    i += '<rect x="452" y="44" width="154" height="70" rx="12" fill="none" stroke="var(--accent)" stroke-width="1.6"/>';
    i += tx(529, 70, t("scoring.step3.title"), "dg-muted", 10, "middle");
    i += tx(529, 94, "Σ w·s = 0–5", "dg-accent", 15, "middle");
    return svg("0 0 620 160", i);
  }

  // methodology: runs -> median on a 0-5 axis
  function dgMethod() {
    var i = "";
    var runs = [2.9, 4.1, 3.4, 4.6, 3.8]; // illustrative spread
    var x0 = 40, x1 = 440, y = 96;
    i += '<line x1="' + x0 + '" y1="' + y + '" x2="' + x1 + '" y2="' + y + '" class="dg-line" stroke-width="1.6"/>';
    for (var s = 0; s <= 5; s++) {
      var xt = x0 + (x1 - x0) * (s / 5);
      i += '<line x1="' + xt + '" y1="' + (y - 5) + '" x2="' + xt + '" y2="' + (y + 5) + '" class="dg-line" stroke-width="1.4"/>';
      i += tx(xt, y + 20, String(s), "dg-muted", 10, "middle");
    }
    var vals = runs.slice().sort(function (a, b) { return a - b; });
    var med = vals[Math.floor(vals.length / 2)];
    for (var r = 0; r < runs.length; r++) {
      var xr = x0 + (x1 - x0) * (runs[r] / 5);
      i += '<circle cx="' + xr + '" cy="' + (y - 22) + '" r="5.5" class="dg-box" stroke-width="1.4" fill="var(--surface)"/>';
      i += '<circle cx="' + xr + '" cy="' + (y - 22) + '" r="2.4" class="dg-muted-fill"/>';
    }
    var xm = x0 + (x1 - x0) * (med / 5);
    i += '<line x1="' + xm + '" y1="' + (y - 44) + '" x2="' + xm + '" y2="' + (y + 8) + '" class="dg-line-accent" stroke-width="2"/>';
    i += '<circle cx="' + xm + '" cy="' + (y - 44) + '" r="4" class="dg-fill-accent"/>';
    i += tx(xm, y - 52, "median", "dg-accent", 11, "middle");
    i += tx(x0, y - 22, "runs", "dg-muted", 10.5, "end");
    i += tx(x1, 128, "⚠ < 5 runs = provisional", "dg-semi-t", 10.5, "end");
    return svg("0 0 480 140", i);
  }

  // radial 0-5 gauge
  function gauge(value) {
    var r = 48, cx = 60, cy = 60, circ = 2 * Math.PI * r, arc = circ * 0.75;
    var frac = Math.max(0, Math.min(1, value / 5));
    var i = '<g transform="rotate(135 60 60)">';
    i += '<circle cx="' + cx + '" cy="' + cy + '" r="' + r + '" fill="none" stroke="var(--chip)" stroke-width="9" stroke-linecap="round" stroke-dasharray="' + arc + ' ' + circ + '"/>';
    i += '<circle cx="' + cx + '" cy="' + cy + '" r="' + r + '" fill="none" stroke="var(--accent)" stroke-width="9" stroke-linecap="round" stroke-dasharray="' + (arc * frac) + ' ' + circ + '"/>';
    i += "</g>";
    i += '<text x="60" y="58" text-anchor="middle" font-size="30" font-weight="700" font-family="var(--font-display)" fill="var(--ink)">' + fx(value, 1) + "</text>";
    i += '<text x="60" y="76" text-anchor="middle" font-size="12" font-family="var(--font-mono)" fill="var(--muted)">/ 5</text>';
    return '<svg class="leadcard__gauge" viewBox="0 0 120 120" role="img" aria-label="score ' + fx(value, 1) + ' of 5">' + i + "</svg>";
  }

  var DG_BY_KEY = {
    requestFlow: dgRequestFlow, archLayers: dgArch, restStatus: dgRest, domain1n: dgDomain,
    outbox: dgOutbox, panMask: dgPan, resilience: dgResilience, pyramid: dgPyramid, pillars: dgPillars
  };

  /* =====================================================================
     DATA HELPERS
     ===================================================================== */
  function runsByModel(model) {
    return DATA.runs.filter(function (r) { return r.model === model && r.deep; });
  }
  function representativeRun(model) {
    var rs = runsByModel(model);
    if (!rs.length) rs = DATA.runs.filter(function (r) { return r.model === model; });
    rs.sort(function (a, b) { return b.weightedScore - a.weightedScore; });
    return rs[0] || null;
  }
  function featuredRun() {
    if (!DATA.leaderboard || !DATA.leaderboard.length) return null;
    return representativeRun(DATA.leaderboard[0].model);
  }
  function catScore(run, number) {
    if (!run) return null;
    var c = run.categories.find(function (x) { return x.number === number; });
    return c ? c.score : null;
  }

  /* =====================================================================
     RENDERERS
     ===================================================================== */
  function renderHowPipeline() { var m = document.getElementById("howPipeline"); if (m) m.innerHTML = dgPipelineWide(); }
  function renderTaskDomain() { var m = document.getElementById("taskDomain"); if (m) m.innerHTML = dgDomain(); }
  function renderScoringFlow() { var m = document.getElementById("scoringFlow"); if (m) m.innerHTML = dgScoring(); }
  function renderMethodDiagram() { var m = document.getElementById("methodDiagram"); if (m) m.innerHTML = dgMethod(); }

  function legendItems() {
    return ["FullAuto", "SemiOracle", "ProxyReview"].map(function (a) {
      return '<span class="lg"><span class="dot" style="background:var(' + AUTO_VAR[a] + ')"></span>' + esc(t("auto." + a)) + "</span>";
    }).join("");
  }
  function renderAutoLegend() { var m = document.getElementById("autoLegendMini"); if (m) m.innerHTML = legendItems(); }

  function renderScale() {
    var mount = document.getElementById("scaleList");
    if (!mount) return;
    var colors = ["--auto-proxy", "--auto-proxy", "--auto-semi", "--auto-semi", "--auto-full", "--auto-full"];
    var html = "";
    for (var n = 0; n <= 5; n++) {
      html += '<li><span class="scale__n" style="color:var(' + colors[n] + ');background:color-mix(in srgb, var(' + colors[n] + ') 12%, transparent)">' + n + "</span><span>" + esc(t("scoring.scale." + n)) + "</span></li>";
    }
    mount.innerHTML = html;
  }

  function renderWeights() {
    var mount = document.getElementById("weightsChart");
    if (!mount) return;
    var list = C.criteria.slice().sort(function (a, b) { return b.weightPct - a.weightPct; });
    var max = list[0].weightPct;
    var html = "";
    list.forEach(function (c) {
      var w = (c.weightPct / max) * 100;
      html += '<div class="wbar"><span class="wbar__label">' + esc(c.title[lang]) + '</span>' +
        '<span class="wbar__track"><span class="wbar__fill" style="width:' + w + '%;background:var(' + AUTO_VAR[c.automation] + ')"></span></span>' +
        '<span class="wbar__pct">' + c.weightPct + "%</span></div>";
    });
    mount.innerHTML = html;
  }

  function checkIcon() {
    return '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.4"><path d="M20 6L9 17l-5-5"/></svg>';
  }

  // Per-metric technical breakdown of what the evaluator actually checks (from content.criteriaChecks).
  function checksTable(number) {
    var rows = (C.criteriaChecks && C.criteriaChecks[number]) || [];
    if (!rows.length) return "";
    var items = rows.map(function (ck) {
      var tag = ck.t
        ? '<span class="check__tag check__tag--' + ck.t + '">' + esc(t("criteria.tag." + ck.t)) + "</span>"
        : "";
      return '<li class="check">' +
        '<div class="check__head">' +
          '<code class="check__name">' + esc(ck.n) + "</code>" + tag +
          '<span class="check__w">×' + esc(ck.w) + "</span>" +
        "</div>" +
        '<p class="check__how">' + esc(ck.how[lang] || ck.how.en) + "</p>" +
      "</li>";
    }).join("");
    return '<div class="crit__checks">' +
      "<h4>" + esc(t("criteria.checks")) + "</h4>" +
      '<p class="crit__checksnote">' + esc(t("criteria.checksNote")) + "</p>" +
      '<ul class="checks">' + items + "</ul></div>";
  }

  function renderCriteria() {
    var mount = document.getElementById("criteriaList");
    if (!mount) return;
    var list = C.criteria.slice();
    if (critSort === "weight") list.sort(function (a, b) { return b.weightPct - a.weightPct; });
    else list.sort(function (a, b) { return a.number - b.number; });
    var fr = featuredRun();
    var maxW = 12; // heaviest category
    var html = "";
    list.forEach(function (c) {
      var av = AUTO_VAR[c.automation];
      var barW = (c.weightPct / maxW) * 100;
      var look = c.look[lang].map(function (li) {
        return "<li>" + checkIcon() + "<span>" + esc(li) + "</span></li>";
      }).join("");
      var diagram = (c.diagram && DG_BY_KEY[c.diagram]) ? '<div class="crit__diagram">' + DG_BY_KEY[c.diagram]() + "</div>" : "";
      var sc = fr ? catScore(fr, c.number) : null;
      var live = fr && sc != null
        ? '<span class="crit__livescore">' + esc(t("criteria.liveScore")) + " · " + esc(fr.target) + " <b>" + fx(sc, 1) + "</b>" + esc(t("misc.of5")) + "</span>"
        : "";
      html +=
        '<li class="crit" style="--auto:var(' + av + ')" data-key="' + c.key + '">' +
          '<button class="crit__row" type="button" aria-expanded="false">' +
            '<span class="crit__num">' + (c.number < 10 ? "0" : "") + c.number + "</span>" +
            '<span class="crit__main">' +
              '<span class="crit__titlerow">' +
                '<span class="crit__title">' + esc(c.title[lang]) + "</span>" +
                '<span class="crit__badge" style="color:var(' + av + ');background:color-mix(in srgb,var(' + av + ') 12%,transparent)"><span class="dot" style="background:var(' + av + ')"></span>' + esc(t("auto." + c.automation)) + "</span>" +
                '<span class="crit__wt-inline">' + c.weightPct + "%</span>" +
              "</span>" +
              '<span class="crit__tag">' + esc(c.tagline[lang]) + "</span>" +
            "</span>" +
            '<span class="crit__meter">' +
              '<span class="crit__bar"><span class="crit__fill" style="width:' + barW + '%"></span></span>' +
              '<span class="crit__wt">' + c.weightPct + "%</span>" +
              '<svg class="crit__chev" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 9l6 6 6-6"/></svg>' +
            "</span>" +
          "</button>" +
          '<div class="crit__panel"><div class="crit__panelinner"><div class="crit__content">' +
            '<div class="crit__body">' +
              "<p>" + esc(c.body[lang]) + "</p>" +
              '<div class="crit__look"><h4>' + esc(t("criteria.look")) + "</h4><ul>" + look + "</ul></div>" +
              '<div class="crit__how"><h4>' + esc(t("criteria.how")) + "</h4><p>" + esc(c.how[lang]) + "</p></div>" +
            "</div>" +
            '<div class="crit__aside">' +
              '<p class="crit__iso">' + esc(t("criteria.iso")) + " · <b>" + esc(c.iso) + "</b></p>" +
              diagram + live +
            "</div>" +
            checksTable(c.number) +
          "</div></div></div>" +
        "</li>";
    });
    mount.innerHTML = html;

    // wire expand/collapse
    Array.prototype.forEach.call(mount.querySelectorAll(".crit__row"), function (btn) {
      btn.addEventListener("click", function () {
        var li = btn.closest(".crit");
        var open = li.classList.toggle("is-open");
        btn.setAttribute("aria-expanded", open ? "true" : "false");
      });
    });
  }

  function renderLeaderboardMeta() {
    var mount = document.getElementById("leaderboardMeta");
    if (!mount) return;
    mount.innerHTML =
      '<span class="lb__gen"><span class="pulse"></span>' + esc(t("lb.generated")) + " " + esc(DATA.generatedAtUtc || "—") + "</span>" +
      "<span><b>" + (DATA.modelCount || 0) + "</b> " + esc(t("nav.leaderboard").toLowerCase()) + "</span>" +
      "<span><b>" + (DATA.deepRunCount != null ? DATA.deepRunCount : DATA.runCount || 0) + "</b> deep runs</span>";
  }

  function renderLeaderboard() {
    var mount = document.getElementById("leaderboardMount");
    if (!mount) return;
    var rows = DATA.leaderboard || [];
    if (!rows.length) {
      mount.innerHTML = '<div style="padding:40px;text-align:center;color:var(--muted);font-family:var(--font-mono);font-size:14px">' + esc(t("lb.empty")) + "</div>";
      return;
    }
    var head =
      '<div class="lbhead">' +
        '<span class="num">' + esc(t("lb.col.rank")) + "</span>" +
        "<span>" + esc(t("lb.col.model")) + "</span>" +
        '<span class="num">' + esc(t("lb.col.runs")) + "</span>" +
        "<span>" + esc(t("lb.col.median")) + "</span>" +
        "<span>" + esc(t("lb.col.spread")) + "</span>" +
        '<span class="num flagcol">' + esc(t("lb.col.build")) + "</span>" +
        '<span class="num flagcol">' + esc(t("lb.col.boot")) + "</span>" +
        "<span></span>" +
      "</div>";

    var body = "";
    rows.forEach(function (r, idx) {
      var spread = r.runs > 1
        ? fx(r.mean, 2) + " ±" + fx(r.stdDev, 2) + " (" + fx(r.min, 1) + "–" + fx(r.max, 1) + ")"
        : t("lb.singleRun");
      body +=
        '<button class="lbrow" type="button" data-idx="' + idx + '" aria-expanded="false">' +
          '<span class="lb-rank ' + (r.rank === 1 ? "top" : "") + '">' + r.rank + "</span>" +
          '<span class="lb-model"><code>' + esc(r.model) + "</code>" + (r.provisional ? '<span class="prov" title="' + esc(t("lb.provisional")) + '">⚠</span>' : "") + "</span>" +
          '<span class="lb-runs">' + r.runs + "</span>" +
          '<span class="lb-median"><b>' + fx(r.median, 2) + "</b><span>/5</span></span>" +
          '<span class="lb-spread">' + esc(spread) + "</span>" +
          '<span class="lb-flag ' + (r.allBuild ? "ok" : "no") + '">' + (r.allBuild ? "✓" : "✕") + "</span>" +
          '<span class="lb-flag ' + (r.allBoot ? "ok" : "no") + '">' + (r.allBoot ? "✓" : "✕") + "</span>" +
          '<svg class="lb-exp" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 9l6 6 6-6"/></svg>' +
        "</button>" +
        '<div class="lbdetail" data-detail="' + idx + '"><div class="lbdetail__inner"><div class="lbdetail__pad">' +
          leaderboardDetail(r) +
        "</div></div></div>";
    });

    mount.innerHTML = head + body;

    Array.prototype.forEach.call(mount.querySelectorAll(".lbrow"), function (btn) {
      btn.addEventListener("click", function () {
        var idx = btn.getAttribute("data-idx");
        var detail = mount.querySelector('.lbdetail[data-detail="' + idx + '"]');
        var open = detail.classList.toggle("is-open");
        btn.classList.toggle("is-open", open);
        btn.setAttribute("aria-expanded", open ? "true" : "false");
      });
    });
  }

  // Bar colour by SCORE (not by automation level): 0/5 red -> 5/5 green, on a red->amber->green
  // hue ramp. Full bar (5.0) reads clearly green; a low category reads red. Null (not measured) stays muted.
  function scoreColor(sc) {
    if (sc == null) return "var(--muted)";
    var q = Math.max(0, Math.min(1, sc / 5));
    var hue = Math.round(q * 138);   // 0 = red, 138 = green
    return "hsl(" + hue + " 64% 46%)";
  }

  function leaderboardDetail(r) {
    var run = representativeRun(r.model);
    if (!run) return '<p style="color:var(--muted);font-family:var(--font-mono);font-size:13px">' + esc(t("lb.empty")) + "</p>";

    var cats = C.criteria.slice().sort(function (a, b) { return a.number - b.number; });
    var profile = cats.map(function (c) {
      var sc = catScore(run, c.number);
      var pct = sc != null ? (sc / 5) * 100 : 0;
      var av = AUTO_VAR[c.automation];
      return '<div class="catrow"><span class="catrow__name"><span style="width:8px;height:8px;border-radius:50%;flex:none;background:var(' + av + ')"></span>' + esc(c.title[lang]) + "</span>" +
        '<span class="catrow__track"><span class="catrow__fill" style="width:' + pct + '%;background:' + scoreColor(sc) + '"></span></span>' +
        '<span class="catrow__val">' + (sc != null ? fx(sc, 1) : "—") + "</span></div>";
    }).join("");

    var pen = run.scoreCapReason
      ? '<div class="runmeta__patch"><b>' + esc(t("lb.detail.patch")) + "</b>" + esc(run.scoreCapReason) + "</div>"
      : "";

    var meta =
      '<div class="runmeta">' +
        '<div class="runmeta__row"><span>' + esc(t("lb.detail.run")) + "</span><span>" + esc(run.target) + "</span></div>" +
        '<div class="runmeta__row"><span>' + esc(t("lb.col.median")) + "</span><span>" + fx(run.weightedScore, 2) + " / 5</span></div>" +
        '<div class="runmeta__row"><span>' + esc(t("lb.detail.builds")) + "</span><span>" + (run.builds ? t("lb.yes") : t("lb.no")) + "</span></div>" +
        '<div class="runmeta__row"><span>' + esc(t("lb.detail.boots")) + "</span><span>" + (run.boots ? t("lb.yes") : t("lb.no")) + "</span></div>" +
        '<div class="runmeta__row"><span>' + esc(t("lb.detail.coverage")) + "</span><span>" + Math.round((run.coverage || 0) * 100) + "%</span></div>" +
        '<div class="runmeta__row"><span>' + esc(t("lb.detail.run")) + "</span><span>" + esc((run.evaluatedAtUtc || "").split(" ")[0]) + "</span></div>" +
        pen +
      "</div>";

    return '<div class="lbdetail__cols"><div><h4 class="minihead">' + esc(t("lb.detail.title")) + '</h4><div class="catprofile">' + profile + "</div></div>" + meta + "</div>";
  }

  /* =====================================================================
     ANIMATION / I18N / EVENTS
     ===================================================================== */
  function applyStaticI18n() {
    Array.prototype.forEach.call(document.querySelectorAll("[data-i18n]"), function (n) {
      n.textContent = t(n.getAttribute("data-i18n"));
    });
    Array.prototype.forEach.call(document.querySelectorAll("[data-i18n-title]"), function (n) {
      n.setAttribute("title", t(n.getAttribute("data-i18n-title")));
    });
    document.getElementById("langLabel").textContent = t("lang.switch");
    document.documentElement.setAttribute("lang", lang);
  }

  function renderDynamic() {
    renderLeaderboardMeta(); renderLeaderboard();
    renderAutoLegend(); renderCriteria();
    renderHowPipeline();
    renderTaskDomain(); renderScale(); renderWeights();
    renderScoringFlow(); renderMethodDiagram();
  }

  function renderAll() { applyStaticI18n(); renderDynamic(); }

  function setupReveal() {
    var targets = document.querySelectorAll(".hero__lead, .pagehead, .section__head, .modes, .callout, .how__pair, .how__pipeline, .task__grid, .scoring__flow, .scoring__grid, .method__diagram, .method__cards, .lb__meta, .lb, .criteria__toolbar, .rubric, .section__more");
    var reveal = function (el) { el.classList.add("in"); };
    if (!("IntersectionObserver" in window)) {
      Array.prototype.forEach.call(targets, reveal);
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) { if (e.isIntersecting) { reveal(e.target); io.unobserve(e.target); } });
    }, { threshold: 0.06, rootMargin: "0px 0px -4% 0px" });
    Array.prototype.forEach.call(targets, function (el) { el.classList.add("reveal"); io.observe(el); });
    // safety net: never let content stay hidden if the observer never fires
    // (headless/screenshot, prerender, or IO edge cases)
    setTimeout(function () { Array.prototype.forEach.call(targets, reveal); }, 1400);
  }

  function wireEvents() {
    var langBtn = document.getElementById("langToggle");
    if (langBtn) langBtn.addEventListener("click", function () {
      lang = (lang === "pt") ? "en" : "pt";
      try { localStorage.setItem("bench-lang", lang); } catch (e) {}
      renderAll();
    });
    var themeBtn = document.getElementById("themeToggle");
    if (themeBtn) themeBtn.addEventListener("click", function () {
      var cur = root.getAttribute("data-theme") === "dark" ? "dark" : "light";
      var next = cur === "dark" ? "light" : "dark";
      root.setAttribute("data-theme", next);
      try { localStorage.setItem("bench-theme", next); } catch (e) {}
    });
    var sw = document.getElementById("sortWeight"), so = document.getElementById("sortOrder");
    if (sw && so) {
      sw.addEventListener("click", function () { critSort = "weight"; sw.classList.add("is-active"); so.classList.remove("is-active"); renderCriteria(); });
      so.addEventListener("click", function () { critSort = "order"; so.classList.add("is-active"); sw.classList.remove("is-active"); renderCriteria(); });
    }
  }

  renderAll();
  wireEvents();
  setupReveal();
})();
