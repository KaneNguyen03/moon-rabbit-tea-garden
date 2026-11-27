using System;
using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed class GardenFixtures : MonoBehaviour
    {
        public const int FirstIndex = -100;

        static readonly GardenFixture[] All = { GardenFixture.Mortar, GardenFixture.Shop, GardenFixture.Aquarium };

        readonly GardenStand[] _fixtures = new GardenStand[All.Length];
        readonly bool[] _shown = new bool[All.Length];
        readonly Hex[] _tiles = new Hex[All.Length];
        readonly List<GardenStand> _trees = new List<GardenStand>();
        readonly List<GroundPoint> _treeSpots = new List<GroundPoint>();
        GardenStandSettings _settings;
        CameraSpec _spec;
        float _yaw;

        public event Action<GardenFixture> Tapped;

        public GardenStand Fixture(GardenFixture fixture) => _fixtures[(int)fixture];
        public IReadOnlyList<GardenStand> Trees => _trees;

        public static bool IsFixture(int index) => index <= FirstIndex && index > FirstIndex - All.Length;

        public void Build(GameCatalog catalog, GardenStandSettings settings, GardenPanZoom touch, CameraSpec spec, float yawDegrees)
        {
            _settings = settings;
            _spec = spec;
            _yaw = yawDegrees;
            for (int f = 0; f < All.Length; f++)
            {
                GardenFixture fixture = All[f];
                GardenStand stand = GardenStand.Create(fixture.ToString(), transform, FirstIndex - f, touch, yawDegrees, settings);
                FixtureStand look = settings.FixtureFor(fixture);
                Sprite art = look.Art != null ? look.Art : Fallback(catalog, fixture);
                stand.Show(art, GardenStands.Metres(look.Role, look.Height, settings.RabbitMetres), look.Tint, null);
                stand.gameObject.SetActive(false);
                _fixtures[f] = stand;
                _tiles[f] = new Hex(int.MinValue, int.MinValue);
            }
        }

        static Sprite Fallback(GameCatalog catalog, GardenFixture fixture) => fixture switch
        {
            GardenFixture.Mortar => catalog.Mortar,
            GardenFixture.Shop => catalog.ShopProp,
            _ => catalog.FishBasket,
        };

        public void Tap(int index)
        {
            if (IsFixture(index)) Tapped?.Invoke(All[FirstIndex - index]);
        }

        public void Refresh(GameState s)
        {
            for (int f = 0; f < All.Length; f++)
            {
                bool shown = GardenStands.FixtureShown(s, All[f]);
                if (shown != _shown[f])
                {
                    _shown[f] = shown;
                    _fixtures[f].gameObject.SetActive(shown);
                }
                if (!shown) continue;
                Hex tile = GardenStands.FixtureTile(s, All[f]);
                if (tile == _tiles[f]) continue;
                _tiles[f] = tile;
                _fixtures[f].PlaceAt(GardenFraming.Ground(tile, _spec));
            }
        }

        public void Ring(GroundRect shore, float ground)
        {
            _treeSpots.Clear();
            if (_settings.Tree == null) return;
            GardenStands.TreeRow(shore, _settings.TreeSpacing, _settings.TreeBehind, _treeSpots);
            for (int n = 0; n < _treeSpots.Count; n++)
            {
                GardenStand tree = n < _trees.Count ? _trees[n] : Tree();
                tree.gameObject.SetActive(true);
                tree.PlaceAt(_treeSpots[n], ground);
            }
            for (int n = _treeSpots.Count; n < _trees.Count; n++) _trees[n].gameObject.SetActive(false);
        }

        GardenStand Tree()
        {
            GardenStand tree = GardenStand.Create("Tree", transform, -1, null, _yaw, _settings);
            tree.Show(_settings.Tree, GardenStands.Metres(StandRole.Tree, _settings.TreeHeight, _settings.RabbitMetres), Color.white, null);
            _trees.Add(tree);
            return tree;
        }
    }
}
