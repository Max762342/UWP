// XB-Launcher Web – Target-Katalog (spiegelt TargetCatalog.cs)
const FABRIC_LOADER = "0.19.2";

const NEOFORGE_BY_MC = {
  "1.21": "21.0.167",
  "1.21.1": "21.1.172",
  "1.21.3": "21.3.39",
  "1.21.4": "21.4.147",
  "1.21.5": "21.5.86",
  "1.21.6": "21.6.27",
  "1.21.9": "21.9.38",
  "1.21.11": "21.11.30",
  "26.1": "26.1.10",
  "26.2": "26.2.5",
  "26.3": "26.3.2",
  "26.4": "26.4.1"
};

const FORGE_BY_MC = {
  "1.21": "52.0.16",
  "1.21.1": "52.0.28",
  "1.21.3": "53.1.0",
  "1.21.4": "54.1.4",
  "1.21.5": "55.0.12"
};

const MC_VERSIONS = [
  "1.21", "1.21.1", "1.21.3", "1.21.4",
  "1.21.5", "1.21.6", "1.21.9", "1.21.11",
  "26.1", "26.2", "26.3", "26.4"
];

// Baut alle Launch-Targets analog zu TargetCatalog.Build()
function buildTargets() {
  const list = [];
  for (const mc of MC_VERSIONS) {
    list.push({ id: `vanilla-${mc}`, mcVersion: mc, loader: "Vanilla", loaderVersion: null });
    list.push({ id: `fabric-${mc}`, mcVersion: mc, loader: "Fabric", loaderVersion: FABRIC_LOADER });
    if (NEOFORGE_BY_MC[mc]) {
      list.push({ id: `neoforge-${mc}`, mcVersion: mc, loader: "NeoForge", loaderVersion: NEOFORGE_BY_MC[mc] });
    }
    if (FORGE_BY_MC[mc]) {
      list.push({ id: `forge-${mc}`, mcVersion: mc, loader: "Forge", loaderVersion: FORGE_BY_MC[mc] });
    }
  }
  return list;
}

const TARGETS = buildTargets();

function targetDisplayName(t) {
  return t.loader === "Vanilla" ? t.mcVersion : `${t.mcVersion} + ${t.loader} ${t.loaderVersion}`;
}