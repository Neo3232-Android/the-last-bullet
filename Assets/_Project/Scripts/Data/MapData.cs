using System.Collections.Generic;
using UnityEngine;

namespace TheLastBullet.Data
{
    /// <summary>
    /// One named area inside a big map. Maps are large (40–90 world units)
    /// and split into 3–5 zones; zones unlock as the player climbs the
    /// map's 10 levels. Each zone has points of interest and its own
    /// environment set from the model library (see Docs/MODEL_PIPELINE.md).
    /// </summary>
    public sealed class MapZoneDef
    {
        public string Id { get; }
        public string Name { get; }
        public Vector2 Center { get; }
        public float Radius { get; }
        public string PoiType { get; }
        public int UnlockLevel { get; }
        public string ModelSet { get; }

        public MapZoneDef(string id, string name, float x, float y, float radius,
            string poiType, int unlockLevel, string modelSet)
        {
            Id = id; Name = name; Center = new Vector2(x, y); Radius = radius;
            PoiType = poiType; UnlockLevel = unlockLevel; ModelSet = modelSet;
        }
    }

    /// <summary>
    /// The 20 arenas. Arena size is in world units; the portrait play area
    /// is taller than it is wide. zombieMix weights are relative — the level
    /// builder normalizes them. Sizes are ~2.5x the old 2D arenas: these
    /// maps are BIG, with multiple areas to fight across.
    /// </summary>
    public sealed class MapDef
    {
        public MapId Id { get; }
        public string Name { get; }
        public float ArenaWidth { get; }
        public float ArenaHeight { get; }
        public string Hazard { get; }
        public Dictionary<ZombieKind, int> ZombieMix { get; }
        public string Palette { get; }
        public List<MapZoneDef> Zones { get; }
        public bool IsDark { get; }
        public bool IsOutdoor { get; }

        public MapDef(MapId id, string name, float arenaWidth, float arenaHeight,
            string hazard, Dictionary<ZombieKind, int> zombieMix, string palette,
            List<MapZoneDef> zones, bool isDark, bool isOutdoor)
        {
            Id = id; Name = name; ArenaWidth = arenaWidth; ArenaHeight = arenaHeight;
            Hazard = hazard; ZombieMix = zombieMix; Palette = palette; Zones = zones;
            IsDark = isDark; IsOutdoor = isOutdoor;
        }
    }

    public static class MapData
    {
        public static readonly List<MapDef> All = new List<MapDef>
        {
            new MapDef(MapId.CityStreet, "City Street", 55f, 75f,
                "Explosive barrels scattered around — shoot them to blow up nearby zombies, but don't stand close.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 10 }, { ZombieKind.Runner, 5 }, { ZombieKind.Exploder, 3 }, { ZombieKind.Spitter, 2 } },
                "Asphalt grey, neon signs, gold muzzle flashes, dark sky.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("plaza", "Central Plaza", 0f, 10f, 16f, "Plaza", 1, "city"),
                    new MapZoneDef("alley", "Back Alley", -18f, -12f, 12f, "Alley", 3, "city"),
                    new MapZoneDef("gas", "Gas Station", 18f, -15f, 12f, "GasStation", 5, "city"),
                    new MapZoneDef("subway-in", "Subway Entrance", 0f, -28f, 10f, "Stairwell", 7, "subway"),
                }, false, true),
            new MapDef(MapId.Graveyard, "Graveyard", 55f, 75f,
                "Fog rolls across the arena, cutting visibility. Zombies fade in and out of the mist.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 10 }, { ZombieKind.Crawler, 5 }, { ZombieKind.Screamer, 3 }, { ZombieKind.Brute, 2 } },
                "Cold blues, grey tombstones, pale moonlight, green fog.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("crypt", "Old Crypt", 0f, 12f, 14f, "Crypt", 1, "graveyard"),
                    new MapZoneDef("chapel", "Chapel Ruins", -18f, -10f, 12f, "Chapel", 3, "graveyard"),
                    new MapZoneDef("mausoleum", "Mausoleum Row", 18f, -12f, 12f, "Mausoleum", 5, "graveyard"),
                    new MapZoneDef("pit", "The Open Pit", 0f, -28f, 10f, "Pit", 7, "graveyard"),
                }, true, true),
            new MapDef(MapId.Mall, "Mall", 50f, 70f,
                "Tight aisles between shop fronts. Funnels the horde — and you — into narrow lanes.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 8 }, { ZombieKind.Runner, 6 }, { ZombieKind.ZombieDog, 4 }, { ZombieKind.Leaper, 2 } },
                "Warm shop lights, tiled floor, glass and chrome, red sale signs.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("foodcourt", "Food Court", 0f, 12f, 14f, "FoodCourt", 1, "mall"),
                    new MapZoneDef("arcade", "Arcade", -16f, -8f, 11f, "Arcade", 3, "mall"),
                    new MapZoneDef("shops", "Shop Row", 16f, -10f, 11f, "ShopRow", 5, "mall"),
                    new MapZoneDef("parking", "Parking Garage", 0f, -26f, 10f, "Parking", 7, "city"),
                }, false, false),
            new MapDef(MapId.Farm, "Farm", 65f, 80f,
                "Wide open fields with wooden fences that block movement and line of sight.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 10 }, { ZombieKind.Brute, 3 }, { ZombieKind.Runner, 4 }, { ZombieKind.Crawler, 3 } },
                "Golden wheat, brown dirt, red barn wood, dusk orange sky.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("barn", "Red Barn", 0f, 14f, 15f, "Barn", 1, "farm"),
                    new MapZoneDef("field", "Wheat Field", -20f, -10f, 14f, "Field", 3, "farm"),
                    new MapZoneDef("silo", "Silo Yard", 20f, -12f, 12f, "Silo", 5, "farm"),
                    new MapZoneDef("pond", "Duck Pond", 0f, -30f, 10f, "Pond", 7, "farm"),
                }, false, true),
            new MapDef(MapId.Hospital, "Hospital", 50f, 70f,
                "Long corridors with gurneys and doors. Sight lines are long but escape routes are few.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 8 }, { ZombieKind.Crawler, 5 }, { ZombieKind.Spitter, 4 }, { ZombieKind.Armored, 2 } },
                "Sterile white and teal, flickering fluorescents, dark blood stains.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("ward", "Main Ward", 0f, 12f, 14f, "Ward", 1, "hospital"),
                    new MapZoneDef("pharmacy", "Pharmacy", -16f, -8f, 11f, "Pharmacy", 3, "hospital"),
                    new MapZoneDef("morgue", "Morgue", 16f, -10f, 11f, "Morgue", 5, "hospital"),
                    new MapZoneDef("roof", "Helipad", 0f, -26f, 10f, "Helipad", 7, "hospital"),
                }, false, false),
            new MapDef(MapId.Subway, "Subway", 45f, 65f,
                "Tight and dark — the flashlight matters here. Trains block the edges of the platform.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Runner, 6 }, { ZombieKind.Leaper, 4 }, { ZombieKind.ZombieDog, 4 }, { ZombieKind.Walker, 6 } },
                "Dark concrete, yellow platform lines, harsh white station lights.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("platform", "Main Platform", 0f, 10f, 13f, "Platform", 1, "subway"),
                    new MapZoneDef("tunnel", "North Tunnel", 0f, 24f, 9f, "Tunnel", 3, "subway"),
                    new MapZoneDef("ticket", "Ticket Hall", -14f, -12f, 10f, "TicketHall", 5, "subway"),
                    new MapZoneDef("depot", "Train Depot", 14f, -14f, 10f, "Depot", 7, "subway"),
                }, true, false),
            new MapDef(MapId.Rooftop, "Rooftop", 40f, 55f,
                "Small arena with nowhere to run. Vents and AC units break up the space.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Runner, 6 }, { ZombieKind.ZombieDog, 5 }, { ZombieKind.Leaper, 4 }, { ZombieKind.Exploder, 3 } },
                "Night city skyline, purple and orange glow, black tar roof.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("helipad", "Helipad", 0f, 8f, 11f, "Helipad", 1, "rooftop"),
                    new MapZoneDef("vents", "Vent Farm", -12f, -10f, 9f, "Vents", 3, "rooftop"),
                    new MapZoneDef("edge", "The Ledge", 12f, -12f, 9f, "Ledge", 5, "rooftop"),
                }, false, true),
            new MapDef(MapId.SnowTown, "Snow Town", 60f, 75f,
                "Slippery ice patches — the player slides a little, and visibility drops in snow flurries.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 10 }, { ZombieKind.Armored, 4 }, { ZombieKind.Brute, 3 }, { ZombieKind.Spitter, 2 } },
                "White snow, pale blue shadows, warm window lights, grey stone.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("square", "Town Square", 0f, 10f, 15f, "Plaza", 1, "snow"),
                    new MapZoneDef("lake", "Frozen Lake", -20f, -12f, 13f, "FrozenLake", 3, "snow"),
                    new MapZoneDef("cabins", "Cabin Row", 20f, -12f, 12f, "Cabins", 5, "snow"),
                    new MapZoneDef("church", "Old Church", 0f, -28f, 10f, "Church", 7, "snow"),
                }, false, true),
            new MapDef(MapId.Prison, "Prison", 50f, 70f,
                "Cell-block corridors with guard towers as cover. Long sight lines down the blocks, tight turns between cells.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 8 }, { ZombieKind.Brute, 5 }, { ZombieKind.Armored, 3 }, { ZombieKind.Runner, 3 } },
                "Grey concrete, steel bars, orange jumpsuits, harsh white lights.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("cells", "Cell Block A", 0f, 12f, 14f, "Cells", 1, "prison"),
                    new MapZoneDef("yard", "The Yard", -16f, -10f, 12f, "Yard", 3, "prison"),
                    new MapZoneDef("tower", "Guard Tower", 16f, -10f, 11f, "Tower", 5, "prison"),
                    new MapZoneDef("solitary", "Solitary", 0f, -26f, 10f, "Solitary", 7, "prison"),
                }, false, false),
            new MapDef(MapId.Airport, "Airport", 75f, 90f,
                "Huge open terminal. Luggage carts scattered as obstacles — good cover, but they break up your escape routes.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Runner, 8 }, { ZombieKind.Walker, 8 }, { ZombieKind.ZombieDog, 4 }, { ZombieKind.Leaper, 2 } },
                "Bright white terminal, blue seats, glass walls, glowing departure boards.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("terminal", "Main Terminal", 0f, 14f, 18f, "Terminal", 1, "airport"),
                    new MapZoneDef("runway", "Runway", -26f, -18f, 16f, "Runway", 3, "airport"),
                    new MapZoneDef("luggage", "Luggage Hall", 26f, -16f, 14f, "Luggage", 5, "airport"),
                    new MapZoneDef("hangar", "Hangar", 0f, -34f, 12f, "Hangar", 7, "airport"),
                }, false, true),
            new MapDef(MapId.Casino, "Casino", 55f, 70f,
                "Neon everywhere. Rows of slot machines make solid cover — and exploders love hiding between them.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Exploder, 6 }, { ZombieKind.Walker, 8 }, { ZombieKind.Runner, 4 }, { ZombieKind.Spitter, 2 } },
                "Neon pink and gold, purple carpet, flashing slot machines, dark corners.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("slots", "Slot Floor", 0f, 10f, 15f, "Slots", 1, "casino"),
                    new MapZoneDef("bar", "The Bar", -18f, -10f, 11f, "Bar", 3, "casino"),
                    new MapZoneDef("vault", "The Vault", 18f, -12f, 11f, "Vault", 5, "casino"),
                    new MapZoneDef("stage", "Show Stage", 0f, -26f, 10f, "Stage", 7, "casino"),
                }, false, false),
            new MapDef(MapId.School, "School", 55f, 75f,
                "Classroom blocks and long hallways funnel the horde into tight streams — great for piercing weapons, bad for getting surrounded.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 10 }, { ZombieKind.Runner, 4 }, { ZombieKind.Crawler, 4 }, { ZombieKind.Screamer, 2 } },
                "Cream walls, green chalkboards, wooden desks, buzzing fluorescents.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("hall", "Main Hall", 0f, 12f, 14f, "Hall", 1, "school"),
                    new MapZoneDef("gym", "Gymnasium", -18f, -10f, 12f, "Gym", 3, "school"),
                    new MapZoneDef("library", "Library", 18f, -12f, 11f, "Library", 5, "school"),
                    new MapZoneDef("playground", "Playground", 0f, -28f, 10f, "Playground", 7, "school"),
                }, false, true),
            new MapDef(MapId.Junkyard, "Junkyard", 60f, 75f,
                "Car wrecks everywhere as cover, and explosive barrels all over the place. One bad shot can chain-react the whole yard.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 8 }, { ZombieKind.Exploder, 5 }, { ZombieKind.Brute, 3 }, { ZombieKind.Armored, 2 } },
                "Rust brown, grey steel, oil black, orange welding sparks.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("crusher", "Car Crusher", 0f, 12f, 15f, "Crusher", 1, "junkyard"),
                    new MapZoneDef("piles", "Scrap Piles", -20f, -10f, 13f, "Scrap", 3, "junkyard"),
                    new MapZoneDef("crane", "Crane Yard", 20f, -12f, 12f, "Crane", 5, "junkyard"),
                    new MapZoneDef("office", "Scrap Office", 0f, -28f, 10f, "Office", 7, "junkyard"),
                }, false, true),
            new MapDef(MapId.Beach, "Beach", 65f, 80f,
                "Open sand with wet patches near the water that slow movement. Nowhere to hide — but nowhere for them to hide either.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Crawler, 8 }, { ZombieKind.Walker, 8 }, { ZombieKind.Runner, 3 }, { ZombieKind.ZombieDog, 2 } },
                "Pale sand, turquoise sea, white sun glare, palm green.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("shore", "Shoreline", 0f, 16f, 16f, "Shore", 1, "beach"),
                    new MapZoneDef("pier", "The Pier", -20f, -8f, 12f, "Pier", 3, "beach"),
                    new MapZoneDef("huts", "Beach Huts", 20f, -12f, 12f, "Huts", 5, "beach"),
                    new MapZoneDef("dunes", "Dune Field", 0f, -30f, 10f, "Dunes", 7, "beach"),
                }, false, true),
            new MapDef(MapId.Forest, "Forest", 60f, 75f,
                "Dense trees block sight lines and movement. Thick fog drifts between the trunks — leapers come out of nowhere.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Leaper, 6 }, { ZombieKind.Walker, 8 }, { ZombieKind.Runner, 4 }, { ZombieKind.Screamer, 2 } },
                "Deep green, brown trunks, grey fog, dappled moonlight.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("clearing", "The Clearing", 0f, 10f, 15f, "Clearing", 1, "forest"),
                    new MapZoneDef("grove", "Dark Grove", -20f, -10f, 13f, "Grove", 3, "forest"),
                    new MapZoneDef("pond", "Forest Pond", 20f, -12f, 12f, "Pond", 5, "forest"),
                    new MapZoneDef("cabin", "Hunter's Cabin", 0f, -28f, 10f, "Cabin", 7, "forest"),
                }, true, true),
            new MapDef(MapId.Laboratory, "Laboratory", 50f, 70f,
                "Sterile halls with goo puddles that slow the player on contact. Spitters thrive here.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Spitter, 7 }, { ZombieKind.Walker, 7 }, { ZombieKind.Crawler, 4 }, { ZombieKind.Armored, 2 } },
                "White tile, glowing green goo, blue lab lights, brushed steel.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("lab", "Main Lab", 0f, 12f, 14f, "Lab", 1, "lab"),
                    new MapZoneDef("storage", "Sample Storage", -16f, -8f, 11f, "Storage", 3, "lab"),
                    new MapZoneDef("quarantine", "Quarantine", 16f, -10f, 11f, "Quarantine", 5, "lab"),
                    new MapZoneDef("reactor", "Reactor Room", 0f, -26f, 10f, "Reactor", 7, "lab"),
                }, false, false),
            new MapDef(MapId.Desert, "Desert", 65f, 80f,
                "Dunes and cacti break up the ground. Heat haze shimmers at range, making distant zombies hard to track.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Armored, 6 }, { ZombieKind.Walker, 8 }, { ZombieKind.Brute, 3 }, { ZombieKind.Runner, 3 } },
                "Sand gold, orange sun, cactus green, pale heat-haze sky.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("dunes", "Great Dunes", 0f, 14f, 16f, "Dunes", 1, "desert"),
                    new MapZoneDef("oasis", "The Oasis", -20f, -10f, 13f, "Oasis", 3, "desert"),
                    new MapZoneDef("ruins", "Buried Ruins", 20f, -12f, 12f, "Ruins", 5, "desert"),
                    new MapZoneDef("outpost", "Desert Outpost", 0f, -30f, 10f, "Outpost", 7, "desert"),
                }, false, true),
            new MapDef(MapId.Castle, "Castle", 55f, 75f,
                "Stone courtyards and narrow gates. The gates funnel the horde — hold one and you hold the castle.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Brute, 5 }, { ZombieKind.Armored, 4 }, { ZombieKind.Walker, 8 }, { ZombieKind.Spitter, 2 } },
                "Grey stone, torch orange, crimson banners, dark night sky.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("courtyard", "Courtyard", 0f, 12f, 15f, "Courtyard", 1, "castle"),
                    new MapZoneDef("gate", "The Gatehouse", 0f, 28f, 10f, "Gate", 3, "castle"),
                    new MapZoneDef("throne", "Throne Room", -18f, -12f, 12f, "Throne", 5, "castle"),
                    new MapZoneDef("dungeon", "The Dungeon", 18f, -14f, 11f, "Dungeon", 7, "dungeon"),
                }, true, false),
            new MapDef(MapId.Stadium, "Stadium", 75f, 90f,
                "A giant open bowl with the stands as walls. Massive hordes spawn here — the biggest waves in the game.",
                new Dictionary<ZombieKind, int> { { ZombieKind.Walker, 12 }, { ZombieKind.Runner, 6 }, { ZombieKind.ZombieDog, 4 }, { ZombieKind.Brute, 2 } },
                "Green pitch, white lines, bright floodlights, grey crowd stands.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("pitch", "The Pitch", 0f, 12f, 18f, "Pitch", 1, "stadium"),
                    new MapZoneDef("stands-n", "North Stands", 0f, 30f, 12f, "Stands", 3, "stadium"),
                    new MapZoneDef("tunnel", "Player Tunnel", -24f, -16f, 12f, "Tunnel", 5, "stadium"),
                    new MapZoneDef("vip", "VIP Boxes", 24f, -18f, 12f, "VIP", 7, "stadium"),
                }, false, true),
            new MapDef(MapId.Sewer, "Sewer", 45f, 65f,
                "Dark tunnels where the flashlight is essential. Murky water slows movement and hides what's in it.",
                new Dictionary<ZombieKind, int> { { ZombieKind.ZombieDog, 7 }, { ZombieKind.Crawler, 5 }, { ZombieKind.Walker, 6 }, { ZombieKind.Leaper, 3 } },
                "Dark green-brown, murky water, dim yellow lamps, rust red pipes.",
                new List<MapZoneDef>
                {
                    new MapZoneDef("main", "Main Channel", 0f, 10f, 13f, "Channel", 1, "sewer"),
                    new MapZoneDef("pump", "Pump Room", -14f, -10f, 10f, "Pump", 3, "sewer"),
                    new MapZoneDef("sump", "The Sump", 14f, -12f, 10f, "Sump", 5, "sewer"),
                    new MapZoneDef("outflow", "Outflow Gate", 0f, -24f, 9f, "Gate", 7, "sewer"),
                }, true, false),
        };

        public static MapDef Get(MapId id)
        {
            foreach (var m in All)
                if (m.Id == id) return m;
            return All[0];
        }

        public static int IndexOf(MapId id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id) return i;
            return 0;
        }
    }
}
