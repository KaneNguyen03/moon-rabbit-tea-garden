using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    /// <summary>Everything the game ships with: the stations in unlock order and the shared art.</summary>
    [CreateAssetMenu(menuName = "VuonTra/Game Catalog", fileName = "GameCatalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public StationData[] Stations = new StationData[0];
        public UpgradeData[] Upgrades = new UpgradeData[0];
        public QuestData[] Quests = new QuestData[0];

        [Header("Shared art (swap the files to reskin)")]
        public Sprite Rabbit;
        public Sprite Scarf;
        public Sprite Panel;
        public Sprite Round;
        public Sprite Glow;
        public Sprite Currency;
        public Sprite Gear;
        public Sprite Background;

        public List<StationDef> BuildDefs()
        {
            var defs = new List<StationDef>(Stations.Length);
            foreach (var s in Stations) defs.Add(s.ToDef());
            return defs;
        }

        public List<UpgradeDef> BuildUpgrades()
        {
            var defs = new List<UpgradeDef>(Upgrades.Length);
            foreach (var u in Upgrades) defs.Add(u.ToDef());
            return defs;
        }

        public List<QuestDef> BuildQuests()
        {
            var defs = new List<QuestDef>(Quests.Length);
            foreach (var q in Quests) defs.Add(q.ToDef());
            return defs;
        }
    }
}
