// Leaflet-kartan. Laddas som modul från WorldMap.razor via JavaScript interop.
// Leaflet (window.L) laddas i App.razor.

let map = null;
let geoLayer = null;
let dotNetRef = null;
let statsByIso = new Map();
let highlighted = new Set();
let selectedIso = null;

// Ritar om kartan när datorn byter mellan ljust och mörkt läge
const themeQuery = window.matchMedia("(prefers-color-scheme: dark)");
const onThemeChange = () => restyle();

// Samma gränser som legenden i WorldMap.razor
const BUCKETS = [
    { min: 10000, color: "#7f1d1d" },
    { min: 1000, color: "#c2410c" },
    { min: 25, color: "#f59e0b" },
    { min: 0, color: "#fde68a" }
];

// Koder där Natural Earth och backend skiljer sig åt
// Kosovo, Somaliland och Nordcypern räknas till XKX, Somalia och Cypern i UCDP
const ISO_ALIASES = { KOS: "XKX", SOL: "SOM", CYN: "CYP" };

// Natural Earth skriver ISO-koden på olika sätt. Frankrike och Norge har "-99" i ISO_A3.
function isoOf(feature) {
    const p = feature.properties || {};
    const candidates = [p.ISO_A3, p.ISO_A3_EH, p.ADM0_A3, p.iso_a3, feature.id];
    const iso = candidates.find(c => typeof c === "string" && c.length === 3 && c !== "-99");
    if (!iso) return null;
    const upper = iso.toUpperCase();
    return ISO_ALIASES[upper] ?? upper;
}

function nameOf(feature) {
    const p = feature.properties || {};
    return p.NAME || p.ADMIN || p.name || isoOf(feature) || "";
}

// Läser en färg från CSS, så att kartan följer ljust och mörkt läge
function cssVar(name, fallback) {
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;
}

// Dödsfall finns bara från 1989. Före det färgas länderna efter intensitet.
const DEATHS_FROM_YEAR = 1989;

function colorFor(stat) {
    if (!stat) return null;
    if (stat.year < DEATHS_FROM_YEAR) {
        return stat.maxIntensity >= 2 ? "#c2410c" : "#f59e0b";
    }
    const deaths = stat.battleDeaths ?? 0;
    return BUCKETS.find(b => deaths >= b.min).color;
}

function styleFor(feature) {
    const iso = isoOf(feature);
    const fill = colorFor(statsByIso.get(iso));
    const marked = highlighted.has(iso) || iso === selectedIso;
    return {
        fillColor: fill ?? cssVar("--map-land", "#d6d2ca"),
        fillOpacity: 1,
        color: marked ? cssVar("--highlight", "#0f766e") : cssVar("--map-border", "#ffffff"),
        weight: marked ? 2.5 : 0.6
    };
}

function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, c =>
        ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

function tooltipFor(feature) {
    const stat = statsByIso.get(isoOf(feature));
    const name = escapeHtml(stat?.countryName || nameOf(feature));
    if (!stat) return `<strong>${name}</strong><br>Ingen registrerad konflikt`;

    const deaths = stat.year < DEATHS_FROM_YEAR
        ? "uppgift saknas (före 1989)"
        : (stat.battleDeaths ?? 0).toLocaleString("sv-SE");
    const level = stat.maxIntensity >= 2 ? "krig (1 000+ döda)" : "mindre konflikt";

    return `<strong>${name}</strong><br>` +
        `Aktiva konflikter: ${stat.activeConflicts}<br>` +
        `Högsta nivå: ${level}<br>` +
        `Dödsfall (best): ${deaths}`;
}

/** Skapar kartan och laddar ländernas gränser. */
export async function init(element, ref, geoJsonUrl) {
    if (!window.L) throw new Error("Leaflet är inte laddat. Kontrollera App.razor.");
    dispose();
    dotNetRef = ref;

    map = L.map(element, {
        center: [20, 10],
        zoom: 2,
        minZoom: 1,
        maxZoom: 6,
        worldCopyJump: true
    });
    map.attributionControl.setPrefix(false);
    map.attributionControl.addAttribution("Data: UCDP · Gränser: Natural Earth");

    const response = await fetch(geoJsonUrl);
    if (!response.ok) throw new Error(`Kunde inte ladda ${geoJsonUrl} (${response.status})`);
    const geo = await response.json();

    geoLayer = L.geoJSON(geo, {
        style: styleFor,
        onEachFeature: (feature, layer) => {
            layer.bindTooltip(() => tooltipFor(feature), { sticky: true });
            layer.on("click", () => {
                const iso = isoOf(feature);
                if (iso && dotNetRef) dotNetRef.invokeMethodAsync("OnCountryClicked", iso);
            });
            layer.on("mouseover", () => layer.setStyle({ weight: 2 }));
            layer.on("mouseout", () => layer.setStyle(styleFor(feature)));
        }
    }).addTo(map);

    themeQuery.addEventListener("change", onThemeChange);

    // Containern kan ha bytt storlek medan kartan laddade
    setTimeout(() => map && map.invalidateSize(), 0);
}

/** Färgar länderna. stats = lista med CountryStatDto (camelCase från C#). */
export function setStats(stats) {
    statsByIso = new Map((stats || []).map(s => [String(s.isoCode).toUpperCase(), s]));
    restyle();
}

/** Markerar länder (från AI:n eller ett klick) och zoomar till dem om zoom = true. */
export function setHighlight(isoCodes, selected, zoom) {
    highlighted = new Set((isoCodes || []).map(i => String(i).toUpperCase()));
    selectedIso = selected ? String(selected).toUpperCase() : null;
    restyle();

    if (zoom && geoLayer && highlighted.size > 0) {
        const bounds = L.latLngBounds([]);
        geoLayer.eachLayer(layer => {
            if (highlighted.has(isoOf(layer.feature))) bounds.extend(layer.getBounds());
        });
        if (bounds.isValid()) map.flyToBounds(bounds, { padding: [30, 30], maxZoom: 5, duration: 0.8 });
    }
}

function restyle() {
    if (geoLayer) geoLayer.setStyle(styleFor);
}

export function dispose() {
    themeQuery.removeEventListener("change", onThemeChange);
    if (map) {
        map.remove();
        map = null;
    }
    geoLayer = null;
    dotNetRef = null;
    statsByIso = new Map();
    highlighted = new Set();
    selectedIso = null;
}