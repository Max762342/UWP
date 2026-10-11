// XB-Launcher Web - App-Logik
(function () {
  "use strict";

  const state = {
    profiles: [],
    selectedProfileId: null,
    selectedTargetId: null
  };

  function loadProfiles() {
    try {
      const raw = localStorage.getItem("xbl_profiles");
      if (raw) return JSON.parse(raw);
    } catch (_) {}
    return [{ id: "default", name: "Standard", targetId: "fabric-1.21.11", maxMemoryMb: 3072 }];
  }

  function saveProfiles() {
    localStorage.setItem("xbl_profiles", JSON.stringify(state.profiles));
  }

  const $ = (sel) => document.querySelector(sel);

  function setStatus(text, percent) {
    $("#status-text").textContent = text;
    if (typeof percent === "number") {
      $("#progress-bar").style.width = `${Math.max(0, Math.min(100, percent))}%`;
    }
  }

  function escapeHtml(str) {
    return String(str).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
  }

  function showPage(page) {
    document.querySelectorAll(".page").forEach((p) => (p.hidden = true));
    const el = $(`#page-${page}`);
    if (el) el.hidden = false;
    document.querySelectorAll(".nav-btn").forEach((b) => b.classList.toggle("active", b.dataset.page === page));
    setStatus(`Seite: ${page}`, 0);
  }

  document.querySelectorAll(".nav-btn").forEach((btn) => btn.addEventListener("click", () => showPage(btn.dataset.page)));

  function renderProfiles() {
    const sel = $("#profile-select");
    sel.innerHTML = "";
    for (const p of state.profiles) {
      const opt = document.createElement("option");
      opt.value = p.id;
      opt.textContent = p.name;
      sel.appendChild(opt);
    }
    sel.value = state.selectedProfileId;
  }

  function renderTargets() {
    const sel = $("#target-select");
    sel.innerHTML = "";
    for (const t of TARGETS) {
      const opt = document.createElement("option");
      opt.value = t.id;
      opt.textContent = targetDisplayName(t);
      sel.appendChild(opt);
    }
    sel.value = state.selectedTargetId;
  }

  function renderVersionGrid() {
    const grid = $("#version-grid");
    grid.innerHTML = "";
    for (const t of TARGETS) {
      const tile = document.createElement("div");
      tile.className = "tile" + (t.id === state.selectedTargetId ? " selected" : "");
      tile.innerHTML = `<div class="version">${t.mcVersion}</div><div class="loader">${t.loader}</div><div class="loader-version">${t.loaderVersion || ""}</div>`;
      tile.addEventListener("click", () => {
        state.selectedTargetId = t.id;
        renderVersionGrid();
        renderTargets();
        const prof = state.profiles.find((p) => p.id === state.selectedProfileId);
        if (prof) {
          prof.targetId = t.id;
          saveProfiles();
        }
        setStatus(`Ausgewaehlt: ${targetDisplayName(t)}`, 0);
      });
      grid.appendChild(tile);
    }
  }

  $("#profile-select").addEventListener("change", (e) => {
    state.selectedProfileId = e.target.value;
    const prof = state.profiles.find((p) => p.id === state.selectedProfileId);
    if (prof) {
      state.selectedTargetId = prof.targetId;
      renderVersionGrid();
      renderTargets();
    }
  });

  $("#target-select").addEventListener("change", (e) => {
    state.selectedTargetId = e.target.value;
    renderVersionGrid();
    const prof = state.profiles.find((p) => p.id === state.selectedProfileId);
    if (prof) {
      prof.targetId = state.selectedTargetId;
      saveProfiles();
    }
  });

  $("#add-profile-btn").addEventListener("click", () => {
    const id = "p" + Date.now();
    const p = { id, name: `Profil ${state.profiles.length + 1}`, targetId: "fabric-1.21.11", maxMemoryMb: 3072 };
    state.profiles.push(p);
    state.selectedProfileId = id;
    state.selectedTargetId = p.targetId;
    saveProfiles();
    renderProfiles();
    renderTargets();
    renderVersionGrid();
    setStatus(`Profil "${p.name}" erstellt.`, 0);
  });

  $("#play-btn").addEventListener("click", () => {
    const prof = state.profiles.find((p) => p.id === state.selectedProfileId);
    const target = TARGETS.find((t) => t.id === state.selectedTargetId);
    if (!prof || !target) {
      setStatus("Bitte Profil und Version auswaehlen.", 0);
      return;
    }
    setStatus(`Starte Minecraft ${targetDisplayName(target)} (Profil: ${prof.name})...`, 30);
    setTimeout(() => setStatus("Hinweis: Spielstart benoetigt die native App - Konfiguration wurde gespeichert.", 100), 800);
  });

  const HUB_TYPES = {
    mods: { el: "#results-mods", input: "#search-mods", type: "mod", defaultQuery: "sodium" },
    shaders: { el: "#results-shaders", input: "#search-shaders", type: "shader", defaultQuery: "complementary" },
    packs: { el: "#results-packs", input: "#search-packs", type: "resourcepack", defaultQuery: "faithful" }
  };

  async function searchHub(hub) {
    const cfg = HUB_TYPES[hub];
    const input = $(cfg.input);
    const query = input.value.trim() || cfg.defaultQuery;
    const container = $(cfg.el);
    setStatus(`Suche "${query}" auf Modrinth...`, 20);
    container.innerHTML = "";
    try {
      const facets = encodeURIComponent(`[["project_type:${cfg.type}"],["versions:1.21.11"]]`);
      const url = `https://api.modrinth.com/v2/search?query=${encodeURIComponent(query)}&facets=${facets}&limit=20&index=relevance`;
      const resp = await fetch(url);
      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
      const data = await resp.json();
      for (const hit of data.hits || []) {
        const item = document.createElement("div");
        item.className = "result-item";
        item.innerHTML = `<div class="info"><div class="title">${escapeHtml(hit.title)}</div><div class="desc">${escapeHtml(hit.description || "")}</div><div class="meta">von ${escapeHtml(hit.author || "-")}  ·  ${hit.downloads || 0} Downloads</div></div><button class="btn btn-primary">+</button>`;
        item.querySelector("button").addEventListener("click", () => setStatus(`"${hit.title}" zur Installationsliste hinzugefuegt.`, 100));
        container.appendChild(item);
      }
      setStatus(`${(data.hits || []).length} Ergebnisse gefunden.`, 100);
    } catch (err) {
      setStatus(`Suche fehlgeschlagen: ${err.message}`, 0);
    }
  }

  document.querySelectorAll("[data-search]").forEach((btn) => btn.addEventListener("click", () => searchHub(btn.dataset.search)));

  ["mods", "shaders", "packs"].forEach((hub) => {
    const input = $(HUB_TYPES[hub].input);
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter") searchHub(hub);
    });
  });

  function loadSettings() {
    const prof = state.profiles.find((p) => p.id === state.selectedProfileId);
    if (prof) {
      $("#profile-name").value = prof.name;
      $("#ram-slider").value = prof.maxMemoryMb || 3072;
      $("#ram-value").textContent = prof.maxMemoryMb || 3072;
    }
  }

  $("#ram-slider").addEventListener("input", (e) => {
    $("#ram-value").textContent = e.target.value;
  });

  $("#save-profile-btn").addEventListener("click", () => {
    const prof = state.profiles.find((p) => p.id === state.selectedProfileId);
    if (!prof) return;
    const name = $("#profile-name").value.trim();
    if (name) prof.name = name;
    prof.maxMemoryMb = parseInt($("#ram-slider").value, 10);
    saveProfiles();
    renderProfiles();
    setStatus("Profil gespeichert.", 100);
  });

  function init() {
    state.profiles = loadProfiles();
    state.selectedProfileId = state.profiles[0].id;
    state.selectedTargetId = state.profiles[0].targetId;
    renderProfiles();
    renderTargets();
    renderVersionGrid();
    loadSettings();
    showPage("play");
    setStatus("Bereit.", 0);
  }

  init();
})();
