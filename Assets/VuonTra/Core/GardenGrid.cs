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

    /// <summary>Which way a segment of the map opens from the middle (the 2D map, #91): right, left, up or down.</summary>
    public enum GardenSide { Middle = 0, Right = 1, Left = -1, Up = 2, Down = -2 }

    public struct TileData
    {
        public int Q, R;
        public TileState State;
        /// <summary>What stands here (a station, a place to rest, a decoration, a prop), or null.</summary>
        public string OccupantId;
        public Hex Hex => new Hex(Q, R);
    }

    /// <summary>
    /// The garden stands on a hexagonal grid (#52) laid out as a 2D map (#91): pointy-top tiles around the teapot at the centre
    /// tile (0,0), with the tea shop just above it. The lawn starts as the middle, seven rows of seven or eight tiles, and grows
    /// one segment at a time, the mist clearing a band on one side: right, left, right, left (the oven side and the pond side,
    /// as on the horizontal strip before), then up and down (the land of #67's stations). After every segment the lawn is a
    /// rectangle of rows and doubled columns, so a band up or down runs the whole width open by then. Each thing in the
    /// garden has one tile of its own; the hexes are coordinates only, the ground is drawn as one continuous lawn.
    /// Stations are "station:&lt;id&gt;", places to rest "rest:&lt;n&gt;", decorations "decor:&lt;n&gt;" for decoration spot n, and
    /// the props that come with the garden are <see cref="MortarId"/>, <see cref="ShopId"/> and <see cref="CounterId"/>.
    /// Where the player's grid is silent (a save from before, a thing not yet placed) the default table says where each
    /// thing stands. Props and seats may stand on land not open yet; they came with the garden's frame, not with its land.
    /// </summary>
    public sealed class GardenGrid
    {
        /// <summary>The middle is this many rows above and below the teapot's row: seven rows in all.</summary>
        public const int HalfRows = 3;
        /// <summary>
        /// The middle, open from the start, in doubled columns (a tile's column is 2q + r, so the tiles of a row are two apart
        /// and the rows interleave): |2q + r| ≤ 7, four tiles each side of the teapot on the odd rows, three on the even.
        /// </summary>
        public const int StartHalfWidth = 7;
        /// <summary>The tiles of the middle: four rows of eight and three of seven.</summary>
        public const int StartTiles = 53;
        /// <summary>The most segments the map has room for: the six of the catalog's sixteen stations, and three more.</summary>
        public const int MaxSegments = 9;
        /// <summary>World units between neighbouring tiles in a row, and between rows: a pointy-top lattice stretched tall.</summary>
        public const double PitchX = 150d, PitchY = 200d;
        public const string MortarId = "mortar", ShopId = "shop", CounterId = "counter";

        static readonly Hex[] Directions = { new Hex(1, 0), new Hex(1, -1), new Hex(0, -1), new Hex(-1, 0), new Hex(-1, 1), new Hex(0, 1) };

        /// <summary>One segment of the map: the side it opens and how deep its band is, in doubled columns (right, left) or rows (up, down).</summary>
        struct Band
        {
            public GardenSide Side;
            public int Depth;
            public Band(GardenSide side, int depth) { Side = side; Depth = depth; }
        }

        /// <summary>
        /// The segments in the order they open (#91). The first four keep the horizontal strip's sides: the oven side to the
        /// right (three tiles a row), the pond side to the left, then six tiles a row more on each (the strip's third and fifth,
        /// fourth and sixth segments in one). The fifth opens eight rows up and the sixth eight rows down, the whole width
        /// across: the land of #67's stations. Past the catalog's sixteen stations the map goes on right, left and up.
        /// Fully open for sixteen stations the lawn is 51 doubled columns by 23 rows: 3 900 by 4 600 units, about three screens
        /// wide and two tall at the usual zoom before the wood around it.
        /// </summary>
        static readonly Band[] Bands =
        {
            new Band(GardenSide.Right, 6), new Band(GardenSide.Left, 6),
            new Band(GardenSide.Right, 12), new Band(GardenSide.Left, 12),
            new Band(GardenSide.Up, 8), new Band(GardenSide.Down, 8),
            new Band(GardenSide.Right, 6), new Band(GardenSide.Left, 6), new Band(GardenSide.Up, 4),
        };

        /// <summary>How far the lawn reaches from the teapot's tile with n segments open, n = 0 … MaxSegments: doubled columns right and left, rows up and down.</summary>
        static readonly int[] ReachRight = new int[MaxSegments + 1], ReachLeft = new int[MaxSegments + 1], ReachUp = new int[MaxSegments + 1], ReachDown = new int[MaxSegments + 1];

        static GardenGrid()
        {
            ReachRight[0] = ReachLeft[0] = StartHalfWidth;
            ReachUp[0] = ReachDown[0] = HalfRows;
            for (int n = 1; n <= MaxSegments; n++)
            {
                Band b = Bands[n - 1];
                ReachRight[n] = ReachRight[n - 1] + (b.Side == GardenSide.Right ? b.Depth : 0);
                ReachLeft[n] = ReachLeft[n - 1] + (b.Side == GardenSide.Left ? b.Depth : 0);
                ReachUp[n] = ReachUp[n - 1] + (b.Side == GardenSide.Up ? b.Depth : 0);
                ReachDown[n] = ReachDown[n - 1] + (b.Side == GardenSide.Down ? b.Depth : 0);
            }
        }

        /// <summary>
        /// Where station i stands by default, in the catalog's order. The teapot is the heart; the oven side (the bakeries, the
        /// beehive, the grape vine) grows to the right, the pond and stream side (the star jar, the flower spring, the
        /// observatory) to the left, as on the horizontal strip, and #67's stations open up and down (#91): the lotus garden,
        /// the malt candy kiln and the sticky rice paddy on the band above the oven side, the night tea boat on the band below
        /// the pond side. Each stands on the land of its own segment, but the moon milk tea, on the first segment's land above
        /// the leaf basket that feeds it, and the sticky rice paddy, on the fifth's beside the candy kiln it feeds. Every
        /// ingredient station stands within 520 units of what it feeds: the mooncake bakery stands two rows up, beside the
        /// grape vine, so the lotus garden on the band above is near it.
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
            new Hex(10, -2), //  9 mooncake bakery: segment 3, right, beside the grape vine, under the lotus
            new Hex(-8, 2),  // 10 flower spring: segment 4, left, beside the star jar
            new Hex(-8, 0),  // 11 star observatory: segment 4, left, above the spring
            new Hex(11, -4), // 12 lotus garden: segment 5, up, above the bakery it feeds
            new Hex(13, -5), // 13 malt candy kiln: segment 5, up
            new Hex(11, -6), // 14 sticky rice paddy: segment 5's land, beside the candy kiln it feeds (segment 6)
            new Hex(-7, 5),  // 15 night tea boat: segment 6, down, on the pond side
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

        /// <summary>How many segments of the map are open around the middle.</summary>
        public int Segments;
        readonly Dictionary<string, Hex> _tileOf = new Dictionary<string, Hex>();
        readonly Dictionary<Hex, string> _occupantAt = new Dictionary<Hex, string>();

        public GardenGrid() : this(0) { }
        public GardenGrid(int segments) { Segments = Math.Max(0, Math.Min(MaxSegments, segments)); }

        // ----- Geometry -----

        /// <summary>A tile's doubled column: 2q + r. The tiles of one row are two apart; x is 75 units a column.</summary>
        public static int Column(Hex h) => 2 * h.Q + h.R;

        /// <summary>Which side segment <paramref name="segment"/> (1 for the first) opens: right, left, right, left, up, down, then right, left, up.</summary>
        public static GardenSide SideOf(int segment) => segment < 1 || segment > MaxSegments ? GardenSide.Middle : Bands[segment - 1].Side;

        /// <summary>How deep segment <paramref name="segment"/>'s band is: doubled columns for one to the right or left, rows for one up or down; 0 for none.</summary>
        public static int DepthOf(int segment) => segment < 1 || segment > MaxSegments ? 0 : Bands[segment - 1].Depth;

        /// <summary>How many of the first <paramref name="segments"/> segments opened to the right, the left, up and down.</summary>
        public static int RightOf(int segments) => Count(segments, GardenSide.Right);
        public static int LeftOf(int segments) => Count(segments, GardenSide.Left);
        public static int UpOf(int segments) => Count(segments, GardenSide.Up);
        public static int DownOf(int segments) => Count(segments, GardenSide.Down);

        static int Count(int segments, GardenSide side)
        {
            int n = 0;
            for (int i = 1; i <= Math.Min(segments, MaxSegments); i++) if (Bands[i - 1].Side == side) n++;
            return n;
        }

        /// <summary>
        /// How far the lawn reaches from the teapot's tile on one side with this many segments open: the outermost doubled
        /// column to the right or left (7 for the middle), the outermost row up or down (3 for the middle). The lawn's edge is
        /// half a tile past it.
        /// </summary>
        public static int Reach(int segments, GardenSide side)
        {
            int n = Math.Max(0, Math.Min(MaxSegments, segments));
            switch (side)
            {
                case GardenSide.Right: return ReachRight[n];
                case GardenSide.Left: return ReachLeft[n];
                case GardenSide.Up: return ReachUp[n];
                case GardenSide.Down: return ReachDown[n];
                default: return 0;
            }
        }

        /// <summary>Whether a tile is on the lawn of a map with this many segments open: inside its rectangle of rows and doubled columns.</summary>
        static bool Within(Hex h, int n)
        {
            int c = Column(h);
            return h.R >= -ReachUp[n] && h.R <= ReachDown[n] && c >= -ReachLeft[n] && c <= ReachRight[n];
        }

        /// <summary>
        /// The segment whose opening brings a tile onto the lawn: 0 for the middle, open from the start, 1, 2, … for the segments,
        /// and -1 for a tile off the map (past its last segment on any side).
        /// </summary>
        public static int SegmentOf(Hex h)
        {
            for (int n = 0; n <= MaxSegments; n++) if (Within(h, n)) return n;
            return -1;
        }

        /// <summary>How many tiles a lawn with this many segments open holds: 53 in the middle, then 21, 21, 42, 42, 204 and 204 for the catalog's six.</summary>
        public static int TileCount(int segments) => segments < 0 ? 0 : Strip(segments).Count;
        /// <summary>How many tiles segment <paramref name="segment"/> opens; 0 for none.</summary>
        public static int SegmentTileCount(int segment) => segment < 1 || segment > MaxSegments ? 0 : TileCount(segment) - TileCount(segment - 1);
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

        /// <summary>Every tile of a lawn with this many segments open, nearest the teapot first (ring by ring). The name is the strip's; it is the whole map's lawn.</summary>
        public static IReadOnlyList<Hex> Strip(int segments)
        {
            int n = Math.Max(0, Math.Min(MaxSegments, segments));
            List<Hex> cached = StripCache[n];
            if (cached != null) return cached;
            int count = 0;
            for (int r = -ReachUp[n]; r <= ReachDown[n]; r++)
                for (int c = -ReachLeft[n]; c <= ReachRight[n]; c++)
                    if (((c - r) & 1) == 0) count++;
            var all = new List<Hex>(count);
            for (int radius = 0; all.Count < count; radius++)
                foreach (Hex h in Ring(radius))
                    if (Within(h, n)) all.Add(h);
            return StripCache[n] = all;
        }

        /// <summary>The tiles segment <paramref name="segment"/> opens, nearest the teapot first.</summary>
        public static List<Hex> SegmentTiles(int segment)
        {
            var tiles = new List<Hex>(SegmentTileCount(segment));
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
            // More stations than the table knows: the two of each further segment stand in the middle of its band, one in the
            // upper rows and one in the lower of a band to the right or left, one left and one right of a band up or down.
            int segment = Expansion.WavesTaken(Math.Max(index, StationTiles.Length) + 1);
            bool first = index % 2 == 0;
            int c, r;
            if (segment > MaxSegments)
            {
                // Past the map: further out right and left in turn, off the lawn; Settle stands such a station on the lawn.
                int past = segment - MaxSegments;
                c = (ReachRight[MaxSegments] + 4 * ((past + 1) / 2) + 1) * (past % 2 == 1 ? 1 : -1);
                r = first ? -2 : 2;
            }
            else
            {
                GardenSide side = SideOf(segment);
                int middle = Reach(segment - 1, side) + DepthOf(segment) / 2;
                if (side == GardenSide.Right || side == GardenSide.Left)
                {
                    r = first ? -2 : 2;
                    c = side == GardenSide.Right ? middle : -middle;
                }
                else
                {
                    r = side == GardenSide.Up ? -middle : middle;
                    c = first ? -6 : 6;
                }
            }
            if (((c - r) & 1) != 0) c += c > 0 ? 1 : -1; // a tile's column and row are both odd or both even
            return new Hex((c - r) / 2, r);
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
        /// tile when that is taken. The lawn is never narrower than the plots call for. A thing standing past the open lawn (the
        /// new moon closed its land, or a save put it there) goes back the same way, the teapot to its own tile (#85); what
        /// was stored stays stored.
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
            g.LeaveClosedLand(defs);
            foreach (string id in wanted)
            {
                if (g._tileOf.ContainsKey(id)) continue;
                Hex home = DefaultTile(id, defs);
                if (g.Seat(id, home)) continue;
                // A station keeps its distance from the others (#88) when its own tile is taken, as long as the lawn has room.
                bool found = id.StartsWith("station:") ? g.NearestEmptyApart(home, Expansion.StationSpacing, id, out Hex spare) : g.NearestEmpty(home, out spare);
                if (found) g.Seat(id, spare);
            }
        }

        /// <summary>
        /// Takes what stands past the open lawn off its tile, for Settle to stand it again on its default tile or the empty tile
        /// nearest it (#85). The teapot goes home: whatever the player stood on its tile meanwhile makes way for it first.
        /// </summary>
        void LeaveClosedLand(IReadOnlyList<StationDef> defs)
        {
            var closed = new List<string>();
            foreach (KeyValuePair<string, Hex> o in _tileOf) if (!InLawn(o.Value)) closed.Add(o.Key);
            if (closed.Count == 0) return;
            foreach (string id in closed) Remove(id);
            if (defs != null && defs.Count > 0)
            {
                string teapot = StationId(defs[0]);
                Hex home = DefaultStationTile(0);
                if (closed.Contains(teapot) && _occupantAt.TryGetValue(home, out string there) && Movable(there)) Remove(there);
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

        // ----- Widening the lawn (#53, the 2D map of #91) -----

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
                if (ApartFromStations(h, spacing, null, null)) { found = h; return true; }
            }
            return NearestEmpty(out found);
        }

        /// <summary>
        /// The empty lawn tile nearest <paramref name="near"/> that keeps at least <paramref name="spacing"/> tiles from every
        /// station but <paramref name="self"/>; ties go as in <see cref="NearestEmpty(Hex, out Hex)"/>. The plain nearest empty
        /// tile when no tile keeps that far. False when the lawn is full.
        /// </summary>
        public bool NearestEmptyApart(Hex near, int spacing, string self, out Hex found)
        {
            found = default;
            int best = int.MaxValue;
            foreach (Hex h in Strip(Segments))
            {
                if (_occupantAt.ContainsKey(h) || IsReserved(h)) continue;
                int d = Distance(h, near);
                if (d < best && ApartFromStations(h, spacing, self, null)) { best = d; found = h; }
            }
            return best != int.MaxValue || NearestEmpty(near, out found);
        }

        /// <summary>True when no station but <paramref name="self"/> and <paramref name="partner"/> stands closer than <paramref name="spacing"/> tiles to <paramref name="h"/>.</summary>
        bool ApartFromStations(Hex h, int spacing, string self, string partner)
        {
            foreach (KeyValuePair<string, Hex> o in _tileOf)
                if (o.Key != self && o.Key != partner && o.Key.StartsWith("station:") && Distance(o.Value, h) < spacing) return false;
            return true;
        }

        /// <summary>
        /// Whether <paramref name="id"/> may stand on <paramref name="to"/> as far as the other stations go (#88): a station keeps
        /// <see cref="Expansion.StationSpacing"/> tiles from every other, as the stations the game stands itself do, since its art is
        /// about two tiles wide; a place to rest or a decoration may stand anywhere. <paramref name="partner"/>, the thing a swap
        /// trades places with, is left out: the two keep the distance they had.
        /// </summary>
        public bool KeepsStationsApart(string id, Hex to, string partner = null) =>
            id == null || !id.StartsWith("station:") || ApartFromStations(to, Expansion.StationSpacing, id, partner);

        // ----- Arranging the garden (#54) -----

        readonly HashSet<string> _stored = new HashSet<string>();

        /// <summary>The stations put away in storage: off the lawn, keeping their levels, earning nothing.</summary>
        public IEnumerable<string> Stored => _stored;
        public int StoredCount => _stored.Count;
        public bool IsStored(string id) => id != null && _stored.Contains(id);

        /// <summary>What the player may pick up and stand elsewhere: stations, places to rest and decorations; the props stay.</summary>
        public static bool Movable(string id) => id != null && (id.StartsWith("station:") || id.StartsWith("rest:") || id.StartsWith("decor:"));

        /// <summary>Moves what stands on one tile to an empty lawn tile. False when nothing movable stands there, the other tile is taken, reserved or off the lawn, or a station would stand next to another (#88).</summary>
        public bool Move(Hex from, Hex to)
        {
            if (from == to || !InLawn(to) || IsReserved(to) || _occupantAt.ContainsKey(to)) return false;
            if (!_occupantAt.TryGetValue(from, out string id) || !Movable(id)) return false;
            if (!KeepsStationsApart(id, to)) return false;
            return Seat(id, to);
        }

        /// <summary>Exchanges what stands on two tiles. False unless both hold something movable and neither station lands next to another (#88).</summary>
        public bool Swap(Hex a, Hex b)
        {
            if (a == b || IsReserved(a) || IsReserved(b)) return false;
            if (!_occupantAt.TryGetValue(a, out string first) || !_occupantAt.TryGetValue(b, out string second)) return false;
            if (!Movable(first) || !Movable(second)) return false;
            if (!KeepsStationsApart(first, b, second) || !KeepsStationsApart(second, a, first)) return false;
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
