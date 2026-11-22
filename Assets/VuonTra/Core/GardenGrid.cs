using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>One tile of the lawn in axial hex coordinates: q runs right along a row, r runs down a row at a time.</summary>
    public struct Hex : IEquatable<Hex>
    {
        public int Q, R;
        public Hex(int q, int r) { Q = q; R = r; }
        public bool Equals(Hex other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is Hex h && Equals(h);
        public override int GetHashCode() => Q * 73856093 ^ R * 19349663;
        public override string ToString() => "(" + Q + "," + R + ")";
        public static bool operator ==(Hex a, Hex b) => a.Equals(b);
        public static bool operator !=(Hex a, Hex b) => !a.Equals(b);
    }

    public enum TileState
    {
        /// <summary>Beyond the lawn: forest, until a land wave opens the ring.</summary>
        ForestBorder,
        /// <summary>On the lawn with nothing standing on it.</summary>
        UnlockedEmpty,
        Occupied
    }

    public struct TileData
    {
        public int Q, R;
        public TileState State;
        /// <summary>What stands here (a station, a place to rest, a decoration, a prop), or null.</summary>
        public string OccupantId;
        public Hex Hex => new Hex(Q, R);
    }

    /// <summary>
    /// The garden stands on a hexagonal grid (#52): the lawn is every tile within <see cref="Radius"/> rings of the centre,
    /// where the teapot stands, and each thing in the garden has one tile of its own. The hexes are coordinates only:
    /// the ground is drawn as one continuous lawn. The lawn starts three rings wide and each land wave adds a ring.
    /// Stations are "station:<id>", places to rest "rest:<n>", decorations "decor:<n>" for decoration spot n, and
    /// the props that come with the garden are <see cref="MortarId"/>, <see cref="ShopId"/> and <see cref="CounterId"/>.
    /// Where the player's grid is silent (a save from before, a thing not yet placed) the default table says where
    /// each thing stands: today's layout, snapped to the nearest tile. Props and seats may stand beyond the lawn's
    /// current rings; they came with the garden's frame, not with its land.
    /// </summary>
    public sealed class GardenGrid
    {
        public const int StartingRadius = 3;
        public const int MaxRadius = 12;
        /// <summary>World units between neighbouring tiles in a row, and between rows: a pointy-top lattice stretched tall, for a portrait garden.</summary>
        public const double PitchX = 150d, PitchY = 200d;
        public const string MortarId = "mortar", ShopId = "shop", CounterId = "counter";

        static readonly Hex[] Directions = { new Hex(1, 0), new Hex(1, -1), new Hex(0, -1), new Hex(-1, 0), new Hex(-1, 1), new Hex(0, 1) };

        /// <summary>Where station i stands by default, in the catalog's order: the teapot at the heart, the rest around it as today.</summary>
        static readonly Hex[] StationTiles =
        {
            new Hex(0, 0),   //  0 teapot: the heart
            new Hex(2, 0),   //  1 leaf basket: beside the teapot on the right
            new Hex(-3, 2),  //  2 jam pot: down on the left
            new Hex(-1, 3),  //  3 flower mortar: below the teapot
            new Hex(-1, -2), //  4 chestnut oven: up on the left
            new Hex(-2, 0),  //  5 beehive: beside the teapot on the left
            new Hex(3, -2),  //  6 moon milk tea: up on the right
            new Hex(1, 2),   //  7 star jar: down on the right
            new Hex(2, -3),  //  8 grape vine: above the teapot, below the shop
            new Hex(0, -4),  //  9 mooncake bakery: top left
            new Hex(0, 4),   // 10 flower spring: bottom right
            new Hex(-3, 5),  // 11 star observatory: bottom left, within reach of the flower spring it feeds
        };
        /// <summary>The places to rest: the two stumps by the tea table at the bottom, the rug by the pond, the stools, the hammock, the swing.</summary>
        static readonly Hex[] RestTiles = { new Hex(-1, 5), new Hex(-3, 6), new Hex(3, -4), new Hex(0, 5), new Hex(-5, 6), new Hex(-1, 6) };
        /// <summary>The eight decoration spots: two in the resting corner, then up the sides and the middle, one by the tea shop.</summary>
        static readonly Hex[] DecorTiles = { new Hex(-3, 7), new Hex(-5, 7), new Hex(-3, 3), new Hex(1, 3), new Hex(-1, 2), new Hex(-3, 1), new Hex(-2, -1), new Hex(0, -3) };
        static readonly Hex MortarTile = new Hex(1, -1), ShopTile = new Hex(2, -4), CounterTile = new Hex(3, -3);

        public int Radius;
        readonly Dictionary<string, Hex> _tileOf = new Dictionary<string, Hex>();
        readonly Dictionary<Hex, string> _occupantAt = new Dictionary<Hex, string>();

        public GardenGrid() : this(StartingRadius) { }
        public GardenGrid(int radius) { Radius = Math.Max(0, Math.Min(MaxRadius, radius)); }

        // ----- Geometry -----

        /// <summary>How many tiles a lawn of this many rings holds: 1, 7, 19, 37, 61...</summary>
        public static int TileCount(int radius) => radius < 0 ? 0 : 3 * radius * radius + 3 * radius + 1;
        /// <summary>How many tiles the lawn holds now.</summary>
        public int LawnTiles => TileCount(Radius);

        public static int Distance(Hex a, Hex b) => Distance(new Hex(a.Q - b.Q, a.R - b.R));
        /// <summary>Rings from the centre.</summary>
        public static int Distance(Hex h) => Math.Max(Math.Abs(h.Q), Math.Max(Math.Abs(h.R), Math.Abs(h.Q + h.R)));
        public static int Distance(int q, int r) => Distance(new Hex(q, r));

        public static Hex[] Neighbours(Hex h)
        {
            var around = new Hex[6];
            for (int i = 0; i < 6; i++) around[i] = new Hex(h.Q + Directions[i].Q, h.R + Directions[i].R);
            return around;
        }

        /// <summary>The neighbours that are on the lawn.</summary>
        public List<Hex> LawnNeighbours(Hex h)
        {
            var on = new List<Hex>(6);
            foreach (Hex n in Neighbours(h)) if (InLawn(n)) on.Add(n);
            return on;
        }

        /// <summary>The tiles exactly this many rings out, going round from the right.</summary>
        public static List<Hex> Ring(int radius)
        {
            var ring = new List<Hex>();
            if (radius <= 0) { ring.Add(new Hex(0, 0)); return ring; }
            var h = new Hex(Directions[4].Q * radius, Directions[4].R * radius);
            for (int side = 0; side < 6; side++)
                for (int step = 0; step < radius; step++)
                {
                    ring.Add(h);
                    h = new Hex(h.Q + Directions[side].Q, h.R + Directions[side].R);
                }
            return ring;
        }

        /// <summary>Every tile within this many rings, nearest the centre first.</summary>
        public static List<Hex> Spiral(int radius)
        {
            var all = new List<Hex>(TileCount(radius));
            for (int ring = 0; ring <= radius; ring++) all.AddRange(Ring(ring));
            return all;
        }

        public bool InLawn(Hex h) => Distance(h) <= Radius;
        public bool InLawn(int q, int r) => InLawn(new Hex(q, r));

        /// <summary>A tile's position in the garden, relative to the centre: columns PitchX apart, every row shifted half a tile, r growing downward.</summary>
        public static double LocalX(int q, int r) => PitchX * (q + r / 2d);
        public static double LocalY(int q, int r) => -PitchY * r;

        // ----- What stands where -----

        public TileData Tile(int q, int r)
        {
            var h = new Hex(q, r);
            var t = new TileData { Q = q, R = r };
            if (_occupantAt.TryGetValue(h, out string id)) { t.State = TileState.Occupied; t.OccupantId = id; }
            else t.State = InLawn(h) ? TileState.UnlockedEmpty : TileState.ForestBorder;
            return t;
        }

        /// <summary>Every tile of the lawn, nearest the centre first.</summary>
        public List<TileData> Tiles()
        {
            var tiles = new List<TileData>(LawnTiles);
            foreach (Hex h in Spiral(Radius)) tiles.Add(Tile(h.Q, h.R));
            return tiles;
        }

        public int OccupantCount => _tileOf.Count;
        public IEnumerable<KeyValuePair<string, Hex>> Occupants => _tileOf;
        public string OccupantAt(int q, int r) => _occupantAt.TryGetValue(new Hex(q, r), out string id) ? id : null;
        public Hex? TileOf(string id) => id != null && _tileOf.TryGetValue(id, out Hex h) ? h : (Hex?)null;
        public bool TryTileOf(string id, out Hex h) { h = default; return id != null && _tileOf.TryGetValue(id, out h); }

        /// <summary>Stands something on a lawn tile; something already standing elsewhere moves. False when the tile is off the lawn or taken by another.</summary>
        public bool Place(string id, int q, int r)
        {
            if (!InLawn(q, r)) return false;
            return Seat(id, new Hex(q, r));
        }

        /// <summary>Like <see cref="Place(string, int, int)"/>; with <paramref name="beyondLawn"/> the tile may lie past the lawn's rings, as a saved seat or prop may.</summary>
        public bool Place(string id, int q, int r, bool beyondLawn) => beyondLawn ? Seat(id, new Hex(q, r)) : Place(id, q, r);

        /// <summary>Stands something on a tile, on the lawn or beyond it; something already standing elsewhere moves. False when the tile is taken by another.</summary>
        bool Seat(string id, Hex h)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (_occupantAt.TryGetValue(h, out string there)) return there == id;
            Remove(id);
            _tileOf[id] = h;
            _occupantAt[h] = id;
            return true;
        }

        public bool Remove(string id)
        {
            if (id == null || !_tileOf.TryGetValue(id, out Hex h)) return false;
            _tileOf.Remove(id);
            _occupantAt.Remove(h);
            return true;
        }

        public void Clear() { _tileOf.Clear(); _occupantAt.Clear(); }

        /// <summary>The empty lawn tile nearest the centre, in spiral order. False when the lawn is full.</summary>
        public bool NearestEmpty(out Hex found) => NearestEmpty(new Hex(0, 0), out found);

        /// <summary>The empty lawn tile nearest a given tile; ties go to the one nearer the centre, then spiral order.</summary>
        public bool NearestEmpty(Hex near, out Hex found)
        {
            found = default;
            int best = int.MaxValue;
            foreach (Hex h in Spiral(Radius))
            {
                if (_occupantAt.ContainsKey(h)) continue;
                int d = Distance(h, near);
                if (d < best) { best = d; found = h; }
            }
            return best != int.MaxValue;
        }

        // ----- The default table -----

        public static string StationId(StationDef def) => "station:" + def.Id;
        public static string RestId(int n) => "rest:" + n;
        public static string DecorId(int n) => "decor:" + n;

        public static Hex DefaultStationTile(int index)
        {
            if (index >= 0 && index < StationTiles.Length) return StationTiles[index];
            // More stations than the table knows: carry on downward in pairs, left and right of the middle.
            int k = Math.Max(0, index - StationTiles.Length) / 2, r = 8 + 2 * k;
            return new Hex((index % 2 == 0 ? -1 : 1) - 4 - k, r);
        }

        public static Hex DefaultRestTile(int n) => n >= 0 && n < RestTiles.Length ? RestTiles[n] : new Hex(-4 - n, 8);
        public static Hex DefaultDecorTile(int n) => n >= 0 && n < DecorTiles.Length ? DecorTiles[n] : DecorTiles[0];

        /// <summary>Where an occupant stands by default, by its id; the centre for an id the table does not know.</summary>
        public static Hex DefaultTile(string id, IReadOnlyList<StationDef> defs)
        {
            if (id == MortarId) return MortarTile;
            if (id == ShopId) return ShopTile;
            if (id == CounterId) return CounterTile;
            if (id.StartsWith("station:") && defs != null)
            {
                string stationId = id.Substring(8);
                for (int i = 0; i < defs.Count; i++) if (defs[i].Id == stationId) return DefaultStationTile(i);
            }
            if (id.StartsWith("rest:") && int.TryParse(id.Substring(5), out int rest)) return DefaultRestTile(rest);
            if (id.StartsWith("decor:") && int.TryParse(id.Substring(6), out int decor)) return DefaultDecorTile(decor);
            return new Hex(0, 0);
        }

        /// <summary>How wide the lawn is for a garden with this many plots: three rings to start, one more for every land wave taken.</summary>
        public static int RadiusForPlots(int plots) => Math.Min(MaxRadius, StartingRadius + Expansion.WavesTaken(plots));

        /// <summary>Whether an id is one the game stands on the grid.</summary>
        public static bool Known(string id) =>
            id == MortarId || id == ShopId || id == CounterId || id.StartsWith("station:") || id.StartsWith("rest:") || id.StartsWith("decor:");

        /// <summary>
        /// Brings the grid in step with the garden: every plot's station, every place to rest, every decoration standing and
        /// the props have a tile, and nothing else does. A thing without a tile takes its default one, or the nearest empty
        /// tile when that is taken. The lawn is never narrower than the plots call for.
        /// </summary>
        public static void Settle(IReadOnlyList<StationDef> defs, GameState s)
        {
            GardenGrid g = s.Grid ?? (s.Grid = new GardenGrid());
            g.Radius = Math.Max(g.Radius, RadiusForPlots(s.Plots));
            var wanted = new List<string>();
            for (int i = 0; i < defs.Count && i < s.Plots; i++) wanted.Add(StationId(defs[i]));
            for (int n = 0; n < Math.Min(s.RestSpots, RestSpots.Max); n++) wanted.Add(RestId(n));
            wanted.Add(MortarId); wanted.Add(ShopId); wanted.Add(CounterId);
            for (int n = 0; n < s.DecorSpots.Length; n++) if (s.DecorSpots[n] != null) wanted.Add(DecorId(n));
            var keep = new HashSet<string>(wanted);
            var gone = new List<string>();
            foreach (string id in g._tileOf.Keys) if (!keep.Contains(id)) gone.Add(id);
            foreach (string id in gone) g.Remove(id);
            foreach (string id in wanted)
            {
                if (g._tileOf.ContainsKey(id)) continue;
                Hex home = DefaultTile(id, defs);
                if (g.Seat(id, home)) continue;
                if (g.NearestEmpty(home, out Hex spare)) g.Seat(id, spare);
            }
        }

        /// <summary>Only the decorations: what stands on a spot has the spot's tile, what was put away has none.</summary>
        public static void SettleDecor(GameState s)
        {
            GardenGrid g = s.Grid ?? (s.Grid = new GardenGrid());
            for (int n = 0; n < s.DecorSpots.Length; n++)
            {
                string id = DecorId(n);
                if (s.DecorSpots[n] == null) g.Remove(id);
                else if (!g._tileOf.ContainsKey(id) && !g.Seat(id, DefaultDecorTile(n)) && g.NearestEmpty(DefaultDecorTile(n), out Hex spare)) g.Seat(id, spare);
            }
        }
    }
}
