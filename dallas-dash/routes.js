/**
 * Dallas Dash — the maps. Pure data, shared by the renderer, the client and
 * the rules. A route changes only what the run looks like (streets, skyline,
 * landmarks, signage); the course itself comes from sim.js and is the same on
 * every route, so the replay check never depends on which map was picked.
 *
 * Each route has exactly SPOTS landmarks; the sim's landmark slot `n` (0-9)
 * picks one. A sighting is stored account-side as routeIndex * SPOTS + n.
 */

export const SPOTS = 10;

export const ROUTES = [
  {
    id: "downtown",
    name: "Downtown Dallas",
    region: "Texas",
    blurb: "Glass towers, Main Street and the Arts District.",
    skyline: "dallas",
    flag: "texas",
    tree: "oak",
    sky: { top: [38, 92, 188], mid: [104, 162, 232], low: [255, 190, 132], haze: [240, 204, 172] },
    districts: [
      { name: "Main Street", mix: { glass: 5, loft: 3, brick: 2 } },
      { name: "Arts District", mix: { glass: 4, cantina: 2, loft: 2 } },
      { name: "West End", mix: { loft: 6, brick: 3, cantina: 1 } },
      { name: "Victory Park", mix: { glass: 7, cantina: 1, loft: 1 } },
      { name: "Farmers Market", mix: { cantina: 4, brick: 3, warehouse: 2 } },
    ],
    murals: ["BIG D", "DALLAS", "MAIN ST", "LONE STAR", "HOWDY Y'ALL"],
    shops: ["TACOS", "BBQ", "BOOTS", "COFFEE", "KOLACHES", "TEX-MEX"],
    ghosts: ["ELM ST", "MAIN ST", "COMMERCE ST", "DALLAS COTTON CO.", "WEST END"],
    signs: [["DOWNTOWN", "35E"], ["FAIR PARK", "30"]],
    landmarks: [
      { name: "Reunion Tower", model: "reunion" },
      { name: "Bank of America Plaza", model: "bofa" },
      { name: "Fountain Place", model: "fountain" },
      { name: "Omni Dallas", model: "omni" },
      { name: "Old Red Museum", model: "oldred" },
      { name: "Comerica Bank Tower", model: "comerica" },
      { name: "AT&T Discovery District", model: "discovery" },
      { name: "Dallas City Hall", model: "cityhall" },
      { name: "Winspear Opera House", model: "winspear" },
      { name: "Klyde Warren Park", model: "park" },
    ],
  },
  {
    id: "east",
    name: "East Dallas",
    region: "Texas",
    blurb: "Deep Ellum murals, Fair Park and Lower Greenville.",
    skyline: "dallas",
    flag: "texas",
    tree: "oak",
    sky: { top: [70, 52, 150], mid: [190, 110, 170], low: [255, 168, 110], haze: [246, 190, 160] },
    districts: [
      { name: "Deep Ellum", mix: { brick: 6, loft: 2, cantina: 2 } },
      { name: "Fair Park", mix: { deco: 5, cantina: 2, brick: 1 } },
      { name: "Lower Greenville", mix: { cantina: 6, brick: 3 } },
      { name: "Lakewood", mix: { cantina: 3, deco: 3, brick: 2 } },
      { name: "White Rock", mix: { cantina: 4, loft: 1, brick: 1 } },
    ],
    murals: ["DEEP ELLUM", "LIVE MUSIC", "GREENVILLE", "TEXAS PROUD", "BIG D"],
    shops: ["HONKY TONK", "RECORDS", "TATTOO", "BBQ", "MARGARITAS", "TACOS", "VINTAGE"],
    ghosts: ["ELM ST", "GASTON AVE", "GREENVILLE AVE", "DEEP ELLUM", "FAIR PARK"],
    signs: [["FAIR PARK", "30"], ["DEEP ELLUM", "345"]],
    landmarks: [
      { name: "Texas Star Ferris Wheel", model: "ferris" },
      { name: "Hall of State", model: "hallofstate" },
      { name: "Cotton Bowl", model: "stadium" },
      { name: "Fair Park Esplanade", model: "esplanade" },
      { name: "Traveling Man", model: "robotstatue" },
      { name: "Lakewood Theater", model: "lakewood" },
      { name: "Granada Theater", model: "granada" },
      { name: "White Rock Lake", model: "lake" },
      { name: "Dallas Arboretum", model: "arboretum" },
      { name: "Swiss Avenue Mansions", model: "mansion" },
    ],
  },
  {
    id: "west",
    name: "West Dallas",
    region: "Texas",
    blurb: "Trinity Groves, Bishop Arts and the Design District.",
    skyline: "dallas",
    flag: "texas",
    tree: "oak",
    sky: { top: [30, 110, 196], mid: [120, 186, 238], low: [255, 214, 150], haze: [236, 214, 182] },
    districts: [
      { name: "Trinity Groves", mix: { warehouse: 4, cantina: 3, brick: 2 } },
      { name: "Design District", mix: { warehouse: 6, loft: 2, glass: 1 } },
      { name: "Bishop Arts", mix: { cantina: 6, brick: 3, loft: 1 } },
      { name: "Oak Cliff", mix: { brick: 4, cantina: 3, deco: 2 } },
      { name: "Sylvan Thirty", mix: { glass: 3, warehouse: 3, cantina: 2 } },
    ],
    murals: ["OAK CLIFF", "WEST DALLAS", "BISHOP ARTS", "LONE STAR", "TRINITY"],
    shops: ["TACOS", "GALLERY", "BREWERY", "PALETAS", "BBQ", "BOOTS", "BIKES"],
    ghosts: ["SINGLETON BLVD", "DAVIS ST", "INDUSTRIAL BLVD", "SYLVAN AVE", "OAK CLIFF"],
    signs: [["OAK CLIFF", "35E"], ["FORT WORTH", "30"]],
    landmarks: [
      { name: "Margaret Hunt Hill Bridge", model: "archbridge" },
      { name: "Ronald Kirk Bridge", model: "footbridge" },
      { name: "Trinity Groves", model: "groves" },
      { name: "Bishop Arts Gateway", model: "gateway" },
      { name: "Texas Theatre", model: "texastheatre" },
      { name: "Kessler Theater", model: "kessler" },
      { name: "Belmont Hotel", model: "belmont" },
      { name: "Design District Galleries", model: "gallery" },
      { name: "Dallas Zoo Giraffe", model: "giraffe" },
      { name: "Trinity Overlook", model: "overlook" },
    ],
  },
  {
    id: "toronto",
    name: "Toronto",
    region: "Ontario",
    blurb: "Streetcar lines, the CN Tower and Kensington Market.",
    skyline: "toronto",
    flag: "canada",
    tree: "maple",
    sky: { top: [44, 104, 176], mid: [128, 176, 220], low: [236, 216, 196], haze: [214, 214, 210] },
    districts: [
      { name: "Financial District", mix: { glass: 7, loft: 2, brick: 1 } },
      { name: "Queen West", mix: { brick: 4, cantina: 3, rowhouse: 2 } },
      { name: "Kensington Market", mix: { rowhouse: 6, cantina: 3 } },
      { name: "Distillery District", mix: { brick: 6, warehouse: 3 } },
      { name: "Harbourfront", mix: { glass: 5, warehouse: 2, cantina: 1 } },
    ],
    murals: ["TORONTO", "THE 6IX", "QUEEN WEST", "KENSINGTON", "T.O."],
    shops: ["PATTIES", "DIM SUM", "BAKERY", "RECORDS", "VINTAGE", "PHO", "BAGELS"],
    ghosts: ["QUEEN ST W", "KING ST", "SPADINA AVE", "YONGE ST", "FRONT ST"],
    signs: [["GARDINER EXPY", "QEW"], ["DON VALLEY", "401"]],
    landmarks: [
      { name: "CN Tower", model: "cntower" },
      { name: "Rogers Centre", model: "dome" },
      { name: "Toronto City Hall", model: "tocityhall" },
      { name: "Gooderham Building", model: "flatiron" },
      { name: "Union Station", model: "union" },
      { name: "Royal Ontario Museum", model: "crystal" },
      { name: "Casa Loma", model: "castle" },
      { name: "Distillery District", model: "distillery" },
      { name: "Kensington Market", model: "kensington" },
      { name: "St. Lawrence Market", model: "market" },
    ],
  },
];

export function routeById(id) {
  return ROUTES.find((r) => r.id === id) || ROUTES[0];
}

export function routeIndex(id) {
  return Math.max(0, ROUTES.findIndex((r) => r.id === id));
}

/** The name of a stored sighting (routeIndex * SPOTS + n). */
export function sightingName(key) {
  const r = ROUTES[Math.floor(key / SPOTS)];
  return r ? r.landmarks[key % SPOTS].name : "";
}

export const DISTRICT_LEN = 520;

/** The neighbourhood at a distance along a route: each lasts DISTRICT_LEN. */
export function districtAt(distance, routeId) {
  const ds = routeById(routeId).districts;
  return ds[Math.floor(Math.max(0, distance) / DISTRICT_LEN) % ds.length];
}
