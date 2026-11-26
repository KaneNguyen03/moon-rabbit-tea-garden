using System;
using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed class GardenStandRow : MonoBehaviour
    {
        sealed class Station
        {
            public GardenStand Stand;
            public StationStand Look;
            public Sprite Prop, Pad;
            public Hex Tile;
            public bool Placed, Shown;
            public StandLook State = (StandLook)(-1);
            public long Worker = -1;
            public Transform Spot;
            public RabbitActor Rig;
            public readonly List<RabbitActor> Rigs = new List<RabbitActor>();
            public readonly List<RabbitActor> RigPrefabs = new List<RabbitActor>();
            public SpriteRenderer Still, StillShadow;
            public readonly List<GardenStand> Dressing = new List<GardenStand>();
        }

        readonly List<Station> _stations = new List<Station>();
        readonly List<GardenStand> _lamps = new List<GardenStand>();
        readonly List<SpriteRenderer> _stones = new List<SpriteRenderer>();
        readonly List<GroundPoint> _stoneSpots = new List<GroundPoint>();
        GardenStandSettings _settings;
        GameCatalog _catalog;
        CameraSpec _spec;
        Transform _paths;
        float _yaw;
        long[] _workers = new long[0];
        int _shown = -1;
        bool _pathsStale = true;

        public event Action<int> StationTapped;

        public int Count => _stations.Count;
        public GardenStandSettings Settings => _settings;
        public GardenStand Stand(int index) => _stations[index].Stand;
        public RabbitActor Rig(int index) => _stations[index].Rig;
        public IReadOnlyList<RabbitActor> RigsAt(int index) => _stations[index].Rigs;
        public SpriteRenderer StillRabbit(int index) => _stations[index].Still;
        public SpriteRenderer RabbitShadow(int index) => _stations[index].StillShadow;
        public IReadOnlyList<GardenStand> Dressing(int index) => _stations[index].Dressing;
        public IReadOnlyList<GardenStand> Lamps => _lamps;
        public IReadOnlyList<SpriteRenderer> Stones => _stones;

        public void Build(GameCatalog catalog, GardenStandSettings settings, GardenPanZoom touch, CameraSpec spec, float yawDegrees)
        {
            _catalog = catalog;
            _settings = settings;
            _spec = spec;
            _yaw = yawDegrees;
            _paths = new GameObject("Paths").transform;
            _paths.SetParent(transform, false);
            for (int i = 0; i < catalog.Stations.Length; i++)
            {
                StationData data = catalog.Stations[i];
                StationStand look = settings.Station(data.Id);
                GardenStand stand = GardenStand.Create("Station " + data.Id, transform, i, touch, yawDegrees, settings);
                stand.gameObject.SetActive(false);
                var station = new Station { Stand = stand, Look = look, Prop = look.Art != null ? look.Art : data.Prop, Pad = settings.PadFor(i) };
                foreach (DressingPiece piece in settings.Dressing)
                {
                    GardenStand dress = GardenStand.Create("Dressing " + data.Id, transform, -1, null, yawDegrees, settings);
                    dress.Show(piece.Art, GardenStands.Metres(piece.Role, piece.Height, settings.RabbitMetres), Color.white, null);
                    dress.gameObject.SetActive(false);
                    station.Dressing.Add(dress);
                }
                _stations.Add(station);
            }
            _workers = new long[_stations.Count];
        }

        public void Tap(int index) => StationTapped?.Invoke(index);

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            int count = Math.Min(defs.Count, _stations.Count);
            _shown = GardenStands.Shown(defs, s);
            GardenStands.Workers(s, _workers);
            for (int i = 0; i < count; i++)
            {
                Station station = _stations[i];
                bool shown = GardenStands.Standing(defs, s, _shown, i);
                if (shown != station.Shown)
                {
                    station.Shown = shown;
                    station.Stand.gameObject.SetActive(shown);
                    station.State = (StandLook)(-1);
                    _pathsStale = true;
                }
                if (!shown) continue;
                Hex tile = GardenStands.StationTile(defs, s, i);
                if (!station.Placed || tile != station.Tile)
                {
                    station.Placed = true;
                    station.Tile = tile;
                    station.Stand.PlaceAt(GardenFraming.Ground(tile, _spec));
                    station.State = (StandLook)(-1);
                    _pathsStale = true;
                }
                StandLook look = GardenStands.Look(s, i);
                long worker = look == StandLook.Open ? _workers[i] : 0;
                if (look == station.State && worker == station.Worker) continue;
                if (look != station.State) _pathsStale = true;
                station.State = look;
                station.Worker = worker;
                ShowStation(station, look);
                Staff(station, s, worker);
                station.Stand.Fit();
            }
            if (_pathsStale) LayGround(count);
        }

        void ShowStation(Station station, StandLook look)
        {
            bool land = look == StandLook.Land;
            Sprite art = land ? _catalog.PlotSign : station.Prop;
            double metres = land ? GardenStands.Metres(StandRole.Station, GardenStands.Range(StandRole.Station).Low, _settings.RabbitMetres) : _settings.Metres(station.Look);
            station.Stand.Show(art, metres, look == StandLook.Open ? Color.white : _settings.UnbuiltTint, land ? null : station.Pad);
        }

        void Dress(int count)
        {
            DressingPiece[] pieces = _settings.Dressing;
            for (int i = 0; i < _stations.Count; i++)
            {
                Station station = _stations[i];
                float side = i % 2 == 0 ? 1f : -1f;
                for (int k = 0; k < station.Dressing.Count; k++)
                {
                    GardenStand dress = station.Dressing[k];
                    GroundPoint foot = Ground(station);
                    var at = new GroundPoint(foot.X + pieces[k].Offset.x * side, foot.Z + pieces[k].Offset.y);
                    bool open = i < count && Open(station) && pieces[k].Art != null && ClearOfOthers(at, i, count);
                    dress.gameObject.SetActive(open);
                    if (open) dress.PlaceAt(at);
                }
            }
        }

        bool ClearOfOthers(GroundPoint at, int own, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (i == own || !_stations[i].Shown) continue;
                GroundPoint foot = Ground(_stations[i]);
                double dx = at.X - foot.X, dz = at.Z - foot.Z;
                if (dx * dx + dz * dz < _settings.KeepClear * _settings.KeepClear) return false;
            }
            return true;
        }

        void Staff(Station station, GameState s, long worker)
        {
            Rabbit rabbit = worker != 0 ? Rabbits.Find(s, worker) : null;
            RabbitActor prefab = rabbit != null && station.Look.Work != RabbitWork.Idle ? _settings.RigFor(Rabbits.KindOf(rabbit)) : null;
            bool rig = prefab != null;
            Sprite still = rabbit != null && !rig ? _catalog.RabbitArt(rabbit) : null;
            if (worker != 0 && station.Spot == null) Spot(station);
            if (station.Spot == null) return;
            station.Spot.gameObject.SetActive(worker != 0);
            station.Rig = rig ? RigOf(station, prefab) : null;
            foreach (RabbitActor each in station.Rigs) each.gameObject.SetActive(each == station.Rig);
            if (rig) station.Rig.SetWork(station.Look.Work);
            station.Still.enabled = still != null;
            float wide = 0f;
            if (still != null) wide = GardenStand.Stand(station.Still, still, _settings.RabbitMetres);
            else if (rig) wide = station.Rig.Extent().size.x;
            station.StillShadow.enabled = worker != 0;
            if (worker != 0) GardenStand.LayShadow(station.StillShadow, _settings, wide, station.Look.RabbitSpot);
        }

        RabbitActor RigOf(Station station, RabbitActor prefab)
        {
            int known = station.RigPrefabs.IndexOf(prefab);
            if (known >= 0) return station.Rigs[known];
            RabbitActor raised = Raise(station, prefab);
            raised.name = "Rig " + prefab.name;
            station.RigPrefabs.Add(prefab);
            station.Rigs.Add(raised);
            return raised;
        }

        RabbitActor Raise(Station station, RabbitActor prefab)
        {
            RabbitActor rabbit = Instantiate(prefab, station.Spot, false);
            rabbit.SetView(RabbitView.Front);
            rabbit.Shadow.GetComponent<SpriteRenderer>().enabled = false;
            rabbit.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder = GardenStand.WorkerOrder;
            Bounds natural = rabbit.Extent();
            float tall = natural.size.y > 0f ? natural.max.y - rabbit.transform.position.y : 1f;
            rabbit.transform.localScale = Vector3.one * ((float)_settings.RabbitMetres / tall);
            return rabbit;
        }

        void Spot(Station station)
        {
            var holder = new GameObject("Rabbit");
            holder.transform.SetParent(station.Stand.transform, false);
            holder.transform.localPosition = new Vector3(station.Look.RabbitSpot.x, 0f, station.Look.RabbitSpot.y);
            station.Spot = holder.transform;
            station.Still = GardenStand.Renderer("Art", holder.transform, GardenStand.WorkerOrder);
            station.Still.enabled = false;
            station.StillShadow = GardenStand.Renderer("Rabbit Shadow", station.Stand.Flat, GardenStand.ShadowOrder);
        }

        void LayGround(int count)
        {
            _pathsStale = false;
            Dress(count);
            _stoneSpots.Clear();
            int lamps = 0;
            foreach (StationPair pair in GardenStands.Paths)
            {
                if (pair.From >= count || pair.To >= count) continue;
                Station from = _stations[pair.From], to = _stations[pair.To];
                if (!Open(from) || !Open(to)) continue;
                GroundPoint a = Ground(from), b = Ground(to);
                GardenStands.Stones(a, b, _settings.StoneStep, _settings.StoneClear, _stoneSpots);
                if (_settings.LampPost == null) continue;
                GroundPoint post = GardenStands.Aside(a, b, _settings.LampPostAside);
                if (!ClearOfOthers(post, -1, count)) continue;
                GardenStand lamp = lamps < _lamps.Count ? _lamps[lamps] : Lamp();
                lamps++;
                lamp.gameObject.SetActive(true);
                lamp.PlaceAt(post);
            }
            for (int n = lamps; n < _lamps.Count; n++) _lamps[n].gameObject.SetActive(false);
            int kept = 0;
            for (int n = 0; n < _stoneSpots.Count; n++)
                if (ClearOfOthers(_stoneSpots[n], -1, count)) _stoneSpots[kept++] = _stoneSpots[n];
            _stoneSpots.RemoveRange(kept, _stoneSpots.Count - kept);
            for (int n = 0; n < _stoneSpots.Count; n++)
            {
                SpriteRenderer stone = n < _stones.Count ? _stones[n] : NewStone();
                stone.gameObject.SetActive(true);
                GardenStand.LayFlat(stone, _settings.Stone, _settings.StoneTint, _settings.StoneWidth, _settings.StoneWidth * 0.6f, new Vector2((float)_stoneSpots[n].X, (float)_stoneSpots[n].Z), GardenStand.PadLift);
            }
            for (int n = _stoneSpots.Count; n < _stones.Count; n++) _stones[n].gameObject.SetActive(false);
        }

        static bool Open(Station station) => station.Shown && station.State == StandLook.Open;

        static GroundPoint Ground(Station station)
        {
            Vector3 foot = station.Stand.transform.localPosition;
            return new GroundPoint(foot.x, foot.z);
        }

        GardenStand Lamp()
        {
            GardenStand lamp = GardenStand.Create("Lamp", transform, -1, null, _yaw, _settings);
            lamp.Show(_settings.LampPost, GardenStands.Metres(StandRole.LampPost, _settings.LampPostHeight, _settings.RabbitMetres), Color.white, null);
            _lamps.Add(lamp);
            return lamp;
        }

        SpriteRenderer NewStone()
        {
            SpriteRenderer stone = GardenStand.Renderer("Stone", _paths, GardenStand.PadOrder);
            _stones.Add(stone);
            return stone;
        }
    }
}
