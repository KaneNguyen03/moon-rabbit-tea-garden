using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
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
        ForestBorder,
        UnlockedEmpty,
        Occupied,
        Reserved
    }

    public enum GardenSide { Middle = 0, Right = 1, Left = -1, Up = 2, Down = -2 }
    public struct TileData
    {
        public int Q, R;
        public TileState State;
        public string OccupantId;
        public Hex Hex => new Hex(Q, R);
    }

    public sealed class GardenGrid
    {
        public const int HalfRows = 3;
        public const int StartHalfWidth = 7;
        public const int StartTiles = 53;
        public const int MaxSegments = 9;
        public const double PitchX = 150d, PitchY = 200d;
        public const string MortarId = "mortar", ShopId = "shop", CounterId = "counter";

        static readonly Hex[] Directions = { new Hex(1, 0), new Hex(1, -1), new Hex(0, -1), new Hex(-1, 0), new Hex(-1, 1), new Hex(0, 1) };

        struct Band
        {
            public GardenSide Side;
            public int Depth;
            public Band(GardenSide side, int depth) { Side = side; Depth = depth; }
        }

        static readonly Band[] Bands =
        {
            new Band(GardenSide.Right, 6), new Band(GardenSide.Left, 6),
            new Band(GardenSide.Right, 12), new Band(GardenSide.Left, 12),
            new Band(GardenSide.Up, 8), new Band(GardenSide.Down, 8),
            new Band(GardenSide.Right, 6), new Band(GardenSide.Left, 6), new Band(GardenSide.Up, 4),
        };

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

        static readonly Hex[] StationTiles =
        {
            new Hex(0, 0),
            new Hex(2, 0),
            new Hex(1, 2),
            new Hex(-2, 2),
            new Hex(6, 0),
            new Hex(4, 2),
            new Hex(5, -2),
            new Hex(-5, 2),
            new Hex(8, -2),
            new Hex(10, -2),
            new Hex(-8, 2),
            new Hex(-8, 0),
            new Hex(11, -4),
            new Hex(13, -5),
            new Hex(11, -6),
            new Hex(-7, 5),
        };
        static readonly Hex[] RestTiles = { new Hex(-3, 1), new Hex(-4, 1), new Hex(4, 1), new Hex(-5, 0), new Hex(7, 2), new Hex(-7, -2) };
        static readonly Hex[] DecorTiles = { new Hex(-5, 3), new Hex(-3, 3), new Hex(-1, 3), new Hex(3, 1), new Hex(1, 3), new Hex(5, -3), new Hex(3, -2), new Hex(2, -3) };
        static readonly Hex MortarTile = new Hex(1, -1), ShopTile = new Hex(1, -3), CounterTile = new Hex(3, -3);

        public int Segments;
        readonly Dictionary<string, Hex> _tileOf = new Dictionary<string, Hex>();
        readonly Dictionary<Hex, string> _occupantAt = new Dictionary<Hex, string>();

        public GardenGrid() : this(0) { }
        public GardenGrid(int segments) { Segments = Math.Max(0, Math.Min(MaxSegments, segments)); }

        public static int Column(Hex h) => 2 * h.Q + h.R;

        public static GardenSide SideOf(int segment) => segment < 1 || segment > MaxSegments ? GardenSide.Middle : Bands[segment - 1].Side;
        public static int DepthOf(int segment) => segment < 1 || segment > MaxSegments ? 0 : Bands[segment - 1].Depth;

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

        static bool Within(Hex h, int n)
        {
            int c = Column(h);
            return h.R >= -ReachUp[n] && h.R <= ReachDown[n] && c >= -ReachLeft[n] && c <= ReachRight[n];
        }

        public static int SegmentOf(Hex h)
        {
            for (int n = 0; n <= MaxSegments; n++) if (Within(h, n)) return n;
            return -1;
        }

        public static int TileCount(int segments) => segments < 0 ? 0 : Strip(segments).Count;
        public static int SegmentTileCount(int segment) => segment < 1 || segment > MaxSegments ? 0 : TileCount(segment) - TileCount(segment - 1);
        public int LawnTiles => TileCount(Segments);

        public static int Distance(Hex a, Hex b) => Distance(new Hex(a.Q - b.Q, a.R - b.R));
        public static int Distance(Hex h) => Math.Max(Math.Abs(h.Q), Math.Max(Math.Abs(h.R), Math.Abs(h.Q + h.R)));
        public static int Distance(int q, int r) => Distance(new Hex(q, r));

        public static Hex[] Neighbours(Hex h)
        {
            var around = new Hex[6];
            for (int i = 0; i < 6; i++) around[i] = new Hex(h.Q + Directions[i].Q, h.R + Directions[i].R);
            return around;
        }

        public List<Hex> LawnNeighbours(Hex h)
        {
            var on = new List<Hex>(6);
            foreach (Hex n in Neighbours(h)) if (InLawn(n)) on.Add(n);
            return on;
        }

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

        public static double LocalX(int q, int r) => PitchX * (q + r / 2d);
        public static double LocalY(int q, int r) => -PitchY * r;

        public TileData Tile(int q, int r)
        {
            var h = new Hex(q, r);
            var t = new TileData { Q = q, R = r };
            if (_occupantAt.TryGetValue(h, out string id)) { t.State = TileState.Occupied; t.OccupantId = id; }
            else t.State = !InLawn(h) ? TileState.ForestBorder : IsReserved(h) ? TileState.Reserved : TileState.UnlockedEmpty;
            return t;
        }

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

        public bool Place(string id, int q, int r)
        {
            if (!InLawn(q, r) || IsStored(id)) return false;
            if (IsReserved(q, r) && !IsProp(id)) return false;
            return Seat(id, new Hex(q, r));
        }

        public bool Place(string id, int q, int r, bool beyondLawn) => beyondLawn ? Seat(id, new Hex(q, r)) : Place(id, q, r);

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

        public bool NearestEmpty(out Hex found) => NearestEmpty(new Hex(0, 0), out found);

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

        public static string StationId(StationDef def) => "station:" + def.Id;
        public static string RestId(int n) => "rest:" + n;
        public static string DecorId(int n) => "decor:" + n;

        public static Hex DefaultStationTile(int index)
        {
            if (index >= 0 && index < StationTiles.Length) return StationTiles[index];
            int segment = Expansion.WavesTaken(Math.Max(index, StationTiles.Length) + 1);
            bool first = index % 2 == 0;
            int c, r;
            if (segment > MaxSegments)
            {
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
            if (((c - r) & 1) != 0) c += c > 0 ? 1 : -1;
            return new Hex((c - r) / 2, r);
        }

        public static Hex DefaultRestTile(int n) => n >= 0 && n < RestTiles.Length ? RestTiles[n] : new Hex(-5 - n, 3);
        public static Hex DefaultDecorTile(int n) => n >= 0 && n < DecorTiles.Length ? DecorTiles[n] : DecorTiles[0];

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

        public static int SegmentsForPlots(int plots) => Math.Min(MaxSegments, Expansion.WavesTaken(plots));

        public static bool Known(string id) =>
            id == MortarId || id == ShopId || id == CounterId || id.StartsWith("station:") || id.StartsWith("rest:") || id.StartsWith("decor:");

        public static void Settle(IReadOnlyList<StationDef> defs, GameState s)
        {
            GardenGrid g = s.Grid ?? (s.Grid = new GardenGrid());
            g.Segments = Math.Max(g.Segments, SegmentsForPlots(s.Plots));
            var wanted = new List<string>();
            g.KeepStored(defs, s);
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
                bool found = id.StartsWith("station:") ? g.NearestEmptyApart(home, Expansion.StationSpacing, id, out Hex spare) : g.NearestEmpty(home, out spare);
                if (found) g.Seat(id, spare);
            }
        }

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

        public List<Hex> EmptyTiles()
        {
            var empty = new List<Hex>();
            foreach (Hex h in Strip(Segments)) if (!_occupantAt.ContainsKey(h) && !IsReserved(h)) empty.Add(h);
            return empty;
        }

        public bool NearestEmptyApart(int spacing, out Hex found)
        {
            foreach (Hex h in Strip(Segments))
            {
                if (_occupantAt.ContainsKey(h) || IsReserved(h)) continue;
                if (ApartFromStations(h, spacing, null, null)) { found = h; return true; }
            }
            return NearestEmpty(out found);
        }

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

        bool ApartFromStations(Hex h, int spacing, string self, string partner)
        {
            foreach (KeyValuePair<string, Hex> o in _tileOf)
                if (o.Key != self && o.Key != partner && o.Key.StartsWith("station:") && Distance(o.Value, h) < spacing) return false;
            return true;
        }

        public bool KeepsStationsApart(string id, Hex to, string partner = null) =>
            id == null || !id.StartsWith("station:") || ApartFromStations(to, Expansion.StationSpacing, id, partner);

        readonly HashSet<string> _stored = new HashSet<string>();

        public IEnumerable<string> Stored => _stored;
        public int StoredCount => _stored.Count;
        public bool IsStored(string id) => id != null && _stored.Contains(id);

        public static bool Movable(string id) => id != null && (id.StartsWith("station:") || id.StartsWith("rest:") || id.StartsWith("decor:"));

        public bool Move(Hex from, Hex to)
        {
            if (from == to || !InLawn(to) || IsReserved(to) || _occupantAt.ContainsKey(to)) return false;
            if (!_occupantAt.TryGetValue(from, out string id) || !Movable(id)) return false;
            if (!KeepsStationsApart(id, to)) return false;
            return Seat(id, to);
        }

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

        public bool Store(string id)
        {
            if (id == null || !id.StartsWith("station:") || !_tileOf.ContainsKey(id)) return false;
            Remove(id);
            _stored.Add(id);
            return true;
        }

        public bool TakeOut(string id)
        {
            if (!IsStored(id) || !NearestEmptyApart(new Hex(0, 0), Expansion.StationSpacing, id, out Hex tile)) return false;
            _stored.Remove(id);
            return Seat(id, tile);
        }

        public void Restore(string id)
        {
            if (id == null || !id.StartsWith("station:")) return;
            Remove(id);
            _stored.Add(id);
        }

        void KeepStored(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (_stored.Count == 0) return;
            var keep = new HashSet<string>();
            for (int i = 0; i < defs.Count && i < s.Plots && i < s.Levels.Length; i++) if (s.Levels[i] > 0) keep.Add(StationId(defs[i]));
            _stored.RemoveWhere(id => !keep.Contains(id));
            foreach (string id in _stored) Remove(id);
        }

        static readonly Hex[] ReservedTable =
        {
            new Hex(0, -2), new Hex(1, -2), new Hex(2, -2),
            new Hex(2, -1), new Hex(3, -1), new Hex(4, -1),
            new Hex(-3, -1), new Hex(-2, -1), new Hex(-3, 0), new Hex(-2, 0), new Hex(-4, 0),
            new Hex(-2, -3), new Hex(-1, -3), new Hex(0, -3), new Hex(-3, -2), new Hex(-2, -2), new Hex(-1, -2), new Hex(-1, -1), new Hex(0, -1),
        };
        static readonly HashSet<Hex> ReservedSet = new HashSet<Hex>(ReservedTable);

        public static IReadOnlyList<Hex> ReservedTiles => ReservedTable;
        public static bool IsReserved(Hex h) => ReservedSet.Contains(h);
        public static bool IsReserved(int q, int r) => IsReserved(new Hex(q, r));
        static bool IsProp(string id) => id == MortarId || id == ShopId || id == CounterId;

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
