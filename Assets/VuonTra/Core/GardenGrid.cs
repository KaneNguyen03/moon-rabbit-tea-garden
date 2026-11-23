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
        /// <summary>Beyond the lawn: forest or mist, until the segment it lies in is opened.</summary>
        ForestBorder,
        /// <summary>On the lawn with nothing standing on it.</summary>
        UnlockedEmpty,
        Occupied,
        /// <summary>On the lawn, kept clear for a prop that stands off the grid or for its plate (#54 follow-up): nothing is placed here.</summary>
        Reserved
    }

    /// <summary>Which way a segment of the strip opens from the middle (the horizontal garden).</summary>
    public enum GardenSide { Middle = 0, Right = 1, Left = -1 }

    public struct TileData
    {
        public int Q, R;
        public TileState State;
        /// <summary>What stands here (a station, a place to rest, a decoration, a prop), or null.</summary>
        public string OccupantId;
        public Hex Hex => new Hex(Q, R);
    }

    /// <summary>
    /// The garden stands on a hexagonal grid (#52) laid out as a horizontal strip (the horizontal garden, 23 Nov): seven rows of
    /// pointy-top tiles, the teapot at the centre tile (0,0). The lawn starts as the middle of the strip and widens one
    /// segment at a time, the mist clearing three tiles a row to the right, then to the left, and so on. Each thing in the
    /// garden has one tile of its own; the hexes are coordinates only, the ground is drawn as one continuous lawn.
    /// Stations are "station:&lt;id&gt;", places to rest "rest:&lt;n&gt;", decorations "decor:&lt;n&gt;" for decoration spot n, and
    /// the props that come with the garden are <see cref="MortarId"/>, <see cref="ShopId"/> and <see cref="CounterId"/>.
    /// Where the player's grid is silent (a save from before, a thing not yet placed) the default table says where each
    /// thing stands. Props and seats may stand on land not open yet; they came with the garden's frame, not with its land.
    /// </summary>
    public sealed class GardenGrid
    {
        /// <summary>The strip is this many rows above and below the teapot's row: seven rows in all.</summary>
        public const int HalfRows = 3;
        /// <summary>
        /// The middle of the strip, open from the start, in doubled columns (a tile's column is 2q + r, so the tiles of a row are
        /// two apart and the rows interleave): |2q + r| ≤ 7, four tiles each side of the teapot on the odd rows, three on the even.
        /// </summary>
        public const int StartHalfWidth = 7;
        /// <summary>Each segment widens one side of the lawn by this many doubled columns: three tiles a row, 21 tiles a segment.</summary>
        public const int SegmentWidth = 6;
        public const int TilesPerSegment = (2 * HalfRows + 1) * SegmentWidth / 2;
        /// <summary>The most segments the strip has room for: as many as the rings of the vertical garden past its three (R 4 … R 12).</summary>
        public const int MaxSegments = 9;
        /// <summary>World units between neighbouring tiles in a row, and between rows: a pointy-top lattice stretched tall.</summary>
        public const double PitchX = 150d, PitchY = 200d;
        public const string MortarId = "mortar", ShopId = "shop", CounterId = "counter";

        static readonly Hex[] Directions = { new Hex(1, 0), new Hex(1, -1), new Hex(0, -1), new Hex(-1, 0), new Hex(-1, 1), new Hex(0, 1) };

        /// <summary>
        /// Where station i stands by default, in the catalog's order. The teapot is the heart; the oven side (the bakeries, the
        /// beehive, the grape vine, the lotus, the rice paddy) grows to the right, the pond and stream side (the star jar, the
        /// flower spring, the observatory, the night tea boat) to the left. Each stands on the land of its own segment, but the
        /// moon milk tea and the sticky rice paddy, which stand on land the segment before opened on the other side, next to
        /// the leaf basket and the candy kiln that feed them. Every ingredient station stands within 520 units of what it feeds.
        /// </summary>
        static readonly Hex[] StationTiles =
        {
            new Hex(0, 0),   //  0 teapot: the heart
            new Hex(2, 0),   //  1 leaf basket: beside the teapot on the right
            new Hex(1, 2),   //  2 jam pot: below the leaf basket
            new Hex(-2, 2),  //  3 flower mortar: below the teapot, between the jam pot and the star jar it feeds
            new Hex(6, 0),   //  4 chestnut oven: segment 1, right
            new Hex(4, 2),   //  5 beehive: segment 1, right, between the oven and the jam pot it feeds
            new Hex(5, -2),  //  6 moon milk tea: segment 1's land, above the leaf basket that feeds it (segment 2)
            new Hex(-5, 2),  //  7 star jar: segment 2, left
            new Hex(8, -2),  //  8 grape vine: segment 3, right, beside the milk tea
            new Hex(9, 0),   //  9 mooncake bakery: segment 3, right
            new Hex(-8, 2),  // 10 flower spring: segment 4, left, beside the star jar
            new Hex(-8, 0),  // 11 star observatory: segment 4, left, above the spring
            new Hex(11, -2), // 12 lotus garden: segment 5, right, above the bakery it feeds
            new Hex(10, 2),  // 13 malt candy kiln: segment 5, right
            new Hex(12, 0),  // 14 sticky rice paddy: segment 5's land, beside the candy kiln it feeds (segment 6)
            new Hex(-11, 0), // 15 night tea boat: segment 6, the far left
        };
        /// <summary>
        /// The places to rest: the two stumps by the tea table left of the teapot, then one on the land each of the first four
        /// segments opens: the picnic rug (right), the stools (left), the hammock (right), the swing (left).
        /// </summary>
        static readonly Hex[] RestTiles = { new Hex(-3, 1), new Hex(-4, 1), new Hex(4, 1), new Hex(-5, 0), new Hex(7, 2), new Hex(-7, -2) };
        /// <summary>The eight decoration spots, all on the starting lawn and 300 apart: four along the foot (the first two by the resting corner), one by the leaf basket, two up the right, one by the tea shop.</summary>
        static readonly Hex[] DecorTiles = { new Hex(-5, 3), new Hex(-3, 3), new Hex(-1, 3), new Hex(3, 1), new Hex(1, 3), new Hex(5, -3), new Hex(3, -2), new Hex(2, -3) };
        /// <summary>The mortar just above the teapot; the tea shop at the top of the middle, home, with the guests' counter beside it.</summary>
        static readonly Hex MortarTile = new Hex(1, -1), ShopTile = new Hex(1, -3), CounterTile = new Hex(3, -3);

        /// <summary>How many segments of the strip are open beside the middle.</summary>
        public int Segments;
        readonly Dictionary<string, Hex> _tileOf = new Dictionary<string, Hex>();
        readonly Dictionary<Hex, string> _occupantAt = new Dictionary<Hex, string>();

        public GardenGrid() : this(0) { }
        public GardenGrid(int segments) { Segments = Math.Max(0, Math.Min(MaxSegments, segments)); }

        // ----- Geometry -----

        /// <summary>A tile's doubled column: 2q + r. The tiles of one row are two apart; x is 75 units a column.</summary>
        public static int Column(Hex h) => 2 * h.Q + h.R;

        /// <summary>Which side segment <paramref name="segment"/> (1 for the first) opens: the odd ones to the right, the even ones to the left.</summary>
        public static GardenSide SideOf(int segment) => segment <= 0 ? GardenSide.Middle : segment % 2 == 1 ? GardenSide.Right : GardenSide.Left;

        /// <summary>How many of the first <paramref name="segments"/> segments opened to the right, and to the left.</summary>
        public static int RightOf(int segments) => Math.Max(0, segments + 1) / 2;
        public static int LeftOf(int segments) => Math.Max(0, segments) / 2;

        /// <summary>
        /// The segment whose opening brings a tile onto the lawn: 0 for the middle, open from the start, 1, 2, … for the segments,
        /// and -1 for a tile off the strip (above or below its rows, or past its last segment).
        /// </summary>
        public static int SegmentOf(Hex h)
        {
            if (Math.Abs(h.R) > HalfRows) return -1;
            int c = Column(h);
            if (Math.Abs(c) <= StartHalfWidth) return 0;
            int along = (Math.Abs(c) - StartHalfWidth + SegmentWidth - 1) / SegmentWidth; // 1 for the first segment on that side
            int segment = c > 0 ? 2 * along - 1 : 2 * along;
            return segment <= MaxSegments ? segment : -1;
        }

        /// <summary>How many tiles a lawn with this many segments open holds: 53 in the middle, 21 more a segment.</summary>
        public static int TileCount(int segments) => segments < 0 ? 0 : StartTiles + TilesPerSegment * Math.Min(segments, MaxSegments);
        /// <summary>The tiles of the middle: four rows of eight and three of seven.</summary>
        public const int StartTiles = 53;
        /// <summary>How many tiles the lawn holds now.</summary>
        public int LawnTiles => TileCount(Segments);

        public static int Distance(Hex a, Hex b) => Distance(new Hex(a.Q - b.Q, a.R - b.R));
        /// <summary>Hex steps from the teapot's tile.</summary>
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

        /// <summary>The tiles exactly this many hex steps from the teapot's tile, going round from the lower left.</summary>
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

        static readonly List<Hex>[] StripCache = new List<Hex>[MaxSegments + 1];

        /// <summary>Every tile of a lawn with this many segments open, nearest the teapot first (ring by ring).</summary>
        public static IReadOnlyList<Hex> Strip(int segments)
        {
            int n = Math.Max(0, Math.Min(MaxSegments, segments));
            List<Hex> cached = StripCache[n];
            if (cached != null) return cached;
            var all = new List<Hex>(TileCount(n));
            for (int radius = 0; all.Count < TileCount(n); radius++)
                foreach (Hex h in Ring(radius))
                {
                    int segment = SegmentOf(h);
                    if (segment >= 0 && segment <= n) all.Add(h);
                }
            return StripCache[n] = all;
        }

        /// <summary>The tiles segment <paramref name="segment"/> opens, nearest the teapot first.</summary>
        public static List<Hex> SegmentTiles(int segment)
        {
            var tiles = new List<Hex>(TilesPerSegment);
            if (segment < 1 || segment > MaxSegments) return tiles;
            foreach (Hex h in Strip(segment)) if (SegmentOf(h) == segment) tiles.Add(h);
            return tiles;
        }

        public bool InLawn(Hex h)
        {
            int segment = SegmentOf(h);
            return segment >= 0 && segment <= Segments;
        }
        public bool InLawn(int q, int r) => InLawn(new Hex(q, r));

        /// <summary>A tile's position in the garden, relative to the teapot: columns PitchX apart, every row shifted half a tile, r growing downward.</summary>
        public static double LocalX(int q, int r) => PitchX * (q + r / 2d);
        public static double LocalY(int q, int r) => -PitchY * r;

        // ----- What stands where -----

        public TileData Tile(int q, int r)
        {
            var h = new Hex(q, r);
            var t = new TileData { Q = q, R = r };
            if (_occupantAt.TryGetValue(h, out string id)) { t.State = TileState.Occupied; t.OccupantId = id; }
            else t.State = !InLawn(h) ? TileState.ForestBorder : IsReserved(h) ? TileState.Reserved : TileState.UnlockedEmpty;
            return t;
        }

        /// <summary>Every tile of the lawn, nearest the teapot first.</summary>
        public List<TileData> Tiles()
        {
            var tiles = new List<TileData>(LawnTiles);
            foreach (Hex h in Strip(Segments)) tiles.Add(Tile(h.Q, h.R));
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
            if (!InLawn(q, r) || IsStored(id)) return false; // a stored station comes out with TakeOut (#54)
            if (IsReserved(q, r) && !IsProp(id)) return false; // kept clear for a prop or its plate
            return Seat(id, new Hex(q, r));
        }

        /// <summary>Like <see cref="Place(string, int, int)"/>; with <paramref name="beyondLawn"/> the tile may lie off the open lawn, as a saved seat or prop may.</summary>
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

        /// <summary>The empty lawn tile nearest the teapot, in <see cref="Strip"/> order; never a reserved one. False when the lawn is full.</summary>
        public bool NearestEmpty(out Hex found) => NearestEmpty(new Hex(0, 0), out found);

        /// <summary>The empty lawn tile nearest a given tile; ties go to the one nearer the teapot, then <see cref="Strip"/> order.</summary>
        public bool NearestEmpty(Hex near, out Hex found)
        {
            found = default;
            int best = int.MaxValue;
            foreach (Hex h in Strip(Segments))
            {
                if (_occupantAt.ContainsKey(h) || IsReserved(h)) continue;
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
            // More stations than the table knows: the two of each further segment stand in the middle of its land, one in the
            // upper rows and one in the lower, on the side it opens.
            int segment = Expansion.WavesTaken(Math.Max(index, StationTiles.Length) + 1);
            int along = (segment + 1) / 2, column = 6 * along + 4, r = index % 2 == 0 ? -2 : 2;
            if (SideOf(segment) == GardenSide.Left) column = -column;
            return new Hex((column - r) / 2, r);
        }

        public static Hex DefaultRestTile(int n) => n >= 0 && n < RestTiles.Length ? RestTiles[n] : new Hex(-5 - n, 3);
        public static Hex DefaultDecorTile(int n) => n >= 0 && n < DecorTiles.Length ? DecorTiles[n] : DecorTiles[0];

        /// <summary>Where an occupant stands by default, by its id; the teapot's tile for an id the table does not know.</summary>
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

        /// <summary>How many segments are open for a garden with this many plots: none to start, one for every land wave taken.</summary>
        public static int SegmentsForPlots(int plots) => Math.Min(MaxSegments, Expansion.WavesTaken(plots));

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
            g.Segments = Math.Max(g.Segments, SegmentsForPlots(s.Plots));
            var wanted = new List<string>();
            g.KeepStored(defs, s); // #54: only open stations with a plot stay in storage
            for (int i = 0; i < defs.Count && i < s.Plots; i++) if (!g.IsStored(StationId(defs[i]))) wanted.Add(StationId(defs[i]));
            for (int n = 0; n < Math.Min(s.RestSpots, RestSpots.Max); n++) wanted.Add(RestId(n));
            wanted.Add(MortarId); wanted.Add(ShopId); wanted.Add(CounterId);
            for (int n = 0; n < s.DecorSpots.Length; n++) if (s.DecorSpots[n] != null) wanted.Add(DecorId(n));
            var keep = new HashSet<string>(wanted);
            var gone = new List<string>();
            foreach (string id in g._tileOf.Keys) if (!keep.Contains(id)) gone.Add(id);
            foreach (string id in gone) g.Remove(id);
            g.LeaveReserved();
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

        // ----- Widening the lawn (#53, the horizontal garden) -----

        /// <summary>The tiles of the lawn with nothing standing on them, nearest the teapot first.</summary>
        public List<Hex> EmptyTiles()
        {
            var empty = new List<Hex>();
            foreach (Hex h in Strip(Segments)) if (!_occupantAt.ContainsKey(h) && !IsReserved(h)) empty.Add(h);
            return empty;
        }

        /// <summary>
        /// The empty lawn tile nearest the teapot that keeps at least <paramref name="spacing"/> tiles from every station, for a
        /// station the player did not place themselves; the plain nearest empty tile when no tile keeps that far. False when the lawn is full.
        /// </summary>
        public bool NearestEmptyApart(int spacing, out Hex found)
        {
            foreach (Hex h in Strip(Segments))
            {
                if (_occupantAt.ContainsKey(h) || IsReserved(h)) continue;
                bool apart = true;
                foreach (KeyValuePair<string, Hex> o in _tileOf)
                    if (o.Key.StartsWith("station:") && Distance(o.Value, h) < spacing) { apart = false; break; }
                if (apart) { found = h; return true; }
            }
            return NearestEmpty(out found);
        }

        // ----- Arranging the garden (#54) -----

        readonly HashSet<string> _stored = new HashSet<string>();

        /// <summary>The stations put away in storage: off the lawn, keeping their levels, earning nothing.</summary>
        public IEnumerable<string> Stored => _stored;
        public int StoredCount => _stored.Count;
        public bool IsStored(string id) => id != null && _stored.Contains(id);

        /// <summary>What the player may pick up and stand elsewhere: stations, places to rest and decorations; the props stay.</summary>
        public static bool Movable(string id) => id != null && (id.StartsWith("station:") || id.StartsWith("rest:") || id.StartsWith("decor:"));

        /// <summary>Moves what stands on one tile to an empty lawn tile. False when nothing movable stands there, or the other tile is taken, reserved or off the lawn.</summary>
        public bool Move(Hex from, Hex to)
        {
            if (from == to || !InLawn(to) || IsReserved(to) || _occupantAt.ContainsKey(to)) return false;
            if (!_occupantAt.TryGetValue(from, out string id) || !Movable(id)) return false;
            return Seat(id, to);
        }

        /// <summary>Exchanges what stands on two tiles. False unless both hold something movable.</summary>
        public bool Swap(Hex a, Hex b)
        {
            if (a == b || IsReserved(a) || IsReserved(b)) return false;
            if (!_occupantAt.TryGetValue(a, out string first) || !_occupantAt.TryGetValue(b, out string second)) return false;
            if (!Movable(first) || !Movable(second)) return false;
            _occupantAt[a] = second; _occupantAt[b] = first;
            _tileOf[first] = b; _tileOf[second] = a;
            return true;
        }

        /// <summary>Takes a station standing on the grid off it and into storage. False for anything that is not a station on the grid.</summary>
        public bool Store(string id)
        {
            if (id == null || !id.StartsWith("station:") || !_tileOf.ContainsKey(id)) return false;
            Remove(id);
            _stored.Add(id);
            return true;
        }

        /// <summary>Brings a stored station back onto the empty lawn tile nearest the teapot. False when it is not stored or the lawn is full.</summary>
        public bool TakeOut(string id)
        {
            if (!IsStored(id) || !NearestEmpty(out Hex tile)) return false;
            _stored.Remove(id);
            return Seat(id, tile);
        }

        /// <summary>Puts a station in storage as a save remembers it, with no tile; Settle drops it again if it may not be stored.</summary>
        public void Restore(string id)
        {
            if (id == null || !id.StartsWith("station:")) return;
            Remove(id);
            _stored.Add(id);
        }

        /// <summary>Lets go of stored stations that are no longer open stations with a plot (a new moon, a save that does not fit).</summary>
        void KeepStored(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (_stored.Count == 0) return;
            var keep = new HashSet<string>();
            for (int i = 0; i < defs.Count && i < s.Plots && i < s.Levels.Length; i++) if (s.Levels[i] > 0) keep.Add(StationId(defs[i]));
            _stored.RemoveWhere(id => !keep.Contains(id));
            foreach (string id in _stored) Remove(id);
        }

        // ----- Tiles kept clear for the props (#54 follow-up) -----

        /// <summary>
        /// The lawn tiles kept clear for the props that stand off the grid or reach past their own tile, so nothing the player
        /// arranges covers them: the leaf-catching basket past the leaf station's home tile and the plate hung on its left, the
        /// mortar's plate above the mortar, and, at the top left of the middle, the moon pond with its "Câu cá N/5" plate on the
        /// right bank, where the opening view reads it (#58) and the firefly jar below it with its "Đom đóm · còn N lượt" plate. They are the tiles whose cell the
        /// prop or its plate reaches more than 16 units into, as the garden draws them (GardenScreen's JarSpot, BasketSpot,
        /// MortarSpot, PondSpot). The mortar, the shop and the counter stand on tiles of their own and hold them; the shop and the
        /// counter have no plate. The pond's far bank reaches into the first segment on the left, (-3,-2), and so does the tile
        /// below the jar's plate, (-4,0); that tile and (-2,0) are kept because what stands there would cover the plate.
        /// </summary>
        static readonly Hex[] ReservedTable =
        {
            new Hex(0, -2), new Hex(1, -2), new Hex(2, -2),                                    // the mortar's plate
            new Hex(2, -1), new Hex(3, -1), new Hex(4, -1),                                    // the leaf-catching basket and its plate
            new Hex(-3, -1), new Hex(-2, -1), new Hex(-3, 0), new Hex(-2, 0), new Hex(-4, 0),  // the firefly jar and its plate, which a decoration below it would cover
            new Hex(-2, -3), new Hex(-1, -3), new Hex(0, -3), new Hex(-3, -2), new Hex(-2, -2), new Hex(-1, -2), new Hex(-1, -1), new Hex(0, -1), // the moon pond and its "Câu cá" plate on the right bank (#58), and the tile right of the plate, where a wide piece would cover it
        };
        static readonly HashSet<Hex> ReservedSet = new HashSet<Hex>(ReservedTable);

        /// <summary>Every tile kept clear for a prop or its plate, on the open lawn or not yet.</summary>
        public static IReadOnlyList<Hex> ReservedTiles => ReservedTable;
        /// <summary>True for a tile kept clear for a prop or its plate: nothing is placed, moved, swapped or settled there.</summary>
        public static bool IsReserved(Hex h) => ReservedSet.Contains(h);
        public static bool IsReserved(int q, int r) => IsReserved(new Hex(q, r));
        /// <summary>The props that come with the garden: they stand where the garden puts them.</summary>
        static bool IsProp(string id) => id == MortarId || id == ShopId || id == CounterId;

        /// <summary>What stands on a reserved tile (a save from before the tiles were kept) moves to the empty tile nearest it.</summary>
        void LeaveReserved()
        {
            var moving = new List<KeyValuePair<string, Hex>>();
            foreach (KeyValuePair<string, Hex> o in _tileOf) if (IsReserved(o.Value) && !IsProp(o.Key)) moving.Add(o);
            foreach (KeyValuePair<string, Hex> o in moving)
            {
                Remove(o.Key);
                if (NearestEmpty(o.Value, out Hex spare)) Seat(o.Key, spare);
            }
        }
    }
}
