using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    [CreateAssetMenu(menuName = "VuonTra/Game Catalog", fileName = "GameCatalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public StationData[] Stations = new StationData[0];
        public UpgradeData[] Upgrades = new UpgradeData[0];
        public QuestData[] Quests = new QuestData[0];
        public ItemData[] Items = new ItemData[0];
        [Tooltip("Names handed out to newly called rabbits.")]
        public string[] RabbitNames = new string[0];
        [Tooltip("Optional: one drawing per tier (silver … red). Empty slots use the shared rabbit.")]
        public Sprite[] TierRabbits = new Sprite[0];

        [Header("Shared art (swap the files to reskin)")]
        public Sprite Rabbit;
        public Sprite Scarf;
        public Sprite Panel;
        public Sprite Round;
        public Sprite Glow;
        public Sprite Currency;
        [Tooltip("The four-leaf clover: the rare currency quests pay in.")]
        public Sprite Clover;
        [Tooltip("The pointing hand of the first minute's guidance.")]
        public Sprite Hand;
        [Tooltip("The mortar for the mooncake game, beside the teapot.")]
        public Sprite Mortar;
        [Tooltip("A stone lantern and a flower planter: the dressing set around the stations.")]
        public Sprite StoneLantern, Planter;
        [Tooltip("The mooncake game's own art: the empty mortar, the pestle, the dough raw and being worked, the cake done and dented.")]
        public Sprite MortarEmpty, Pestle, Dough, DoughPounded, Cake, CakeDented;
        [Tooltip("The wooden dish under each product in the tray.")]
        public Sprite TrayWood;
        [Tooltip("The golden carrot, the gift box and the tea shop by the pond.")]
        public Sprite GoldenCarrot, GiftBox, ShopProp;
        [Tooltip("The moon jar (#64), drawn by hand; the jar is drawn in code while this is empty.")]
        public Sprite MoonJar;
        [Tooltip("The six fish of the moon pond (#58), commonest first; each is a tinted circle while its picture is missing.")]
        public Sprite[] Fish = new Sprite[0];
        [Tooltip("The basket the caught fish go into (#58).")]
        public Sprite FishBasket;
        [Tooltip("The pond card's float afloat and pulled under, and its water (#58); drawn in code while missing.")]
        public Sprite FloatUp, FloatDown, PondWater;
        public Sprite[] Ripples = new Sprite[0];
        public Sprite FishSprite(int i) => Fish != null && i >= 0 && i < Fish.Length ? Fish[i] : null;
        public Sprite Gear;
        public Sprite Shadow;
        public Sprite IconUp;
        public Sprite IconCharm;
        public Sprite IconQuest;
        public Sprite Background;

        [Header("Painted 9-slice frames (#143); an empty slot is drawn with Panel in the palette's flat colours")]
        public Sprite CardFrame;
        public Sprite ButtonFrame;
        public Sprite SlotFrame;
        public Sprite BarFrame;
        public Sprite HudFrame;
        public Sprite TabFrame;

        public Sprite FrameArt(FrameKind kind)
        {
            switch (kind)
            {
                case FrameKind.Card: return CardFrame;
                case FrameKind.Button: return ButtonFrame;
                case FrameKind.Slot: return SlotFrame;
                case FrameKind.Bar: return BarFrame;
                case FrameKind.Hud: return HudFrame;
                case FrameKind.Tab: return TabFrame;
                default: return null;
            }
        }

        [Header("Street backdrop layers (hills, trees and ground tile sideways)")]
        public Texture2D BgSky;
        public Texture2D BgHills;
        public Texture2D BgTrees;
        public Texture2D BgGround;
        public Texture2D BgVignette;
        [Tooltip("Grass seen from above; tiles in both directions under the draggable world.")]
        public Texture2D BgGrass;
        public Texture2D BgMoonbeam;

        [Tooltip("Garden dressing, in this order: tree, bush, lantern, fence, mushrooms, stones, pond, tea table, stump.")]
        public Sprite[] Decor = new Sprite[0];

        public const int DecorLantern = 2;

        public Sprite HangingLantern => Decor != null && Decor.Length > DecorLantern ? Decor[DecorLantern] : null;

        public List<StationDef> BuildDefs()
        {
            var defs = new List<StationDef>(Stations.Length);
            foreach (var s in Stations) defs.Add(s.ToDef());
            return defs;
        }

        [Tooltip("The garden level badge; the number is drawn over its lower half.")]
        public Sprite LevelBadge;
        [Tooltip("A low bank of mist, laid in overlapping patches over land the garden has not grown into.")]
        public Sprite Mist;

        [Tooltip("The moon gate a called rabbit steps out of.")]
        public Sprite MoonGate;
        [Tooltip("What hangs on the moon gate's arch in each season (#8): star lanterns, apricot branches, a pine wreath.")]
        public Sprite GateMidAutumn, GateTet, GateChristmas;

        public Sprite GateDecor(Season season)
        {
            switch (season)
            {
                case Season.MidAutumn: return GateMidAutumn;
                case Season.Tet: return GateTet;
                case Season.Christmas: return GateChristmas;
                default: return null;
            }
        }

        [Tooltip("Bare ground with a signpost: a plot that has nothing built on it yet.")]
        public Sprite PlotSign;
        [Tooltip("An empty woven mat: where a rabbit could stand at a station.")]
        public Sprite Pad;
        [Tooltip("Painted ground a working station stands on (earth, a deck, flagstones, flowers), taken in turn by station index.")]
        public Sprite[] StationPads = new Sprite[0];

        public Sprite StationPad(int index) => StationPads == null || StationPads.Length == 0 ? null : StationPads[index % StationPads.Length];

        [Tooltip("Furniture for the places to rest, indexed by seat; null where the seat is a stump that is already in the garden.")]
        public Sprite[] RestProps = new Sprite[0];

        [Tooltip("A white picture for each trade, indexed by RabbitTrade; tinted where it is shown.")]
        public Sprite[] TradeIcons = new Sprite[0];

        public Sprite TradeIcon(RabbitTrade trade)
        {
            int i = (int)trade;
            return TradeIcons != null && i >= 0 && i < TradeIcons.Length ? TradeIcons[i] : null;
        }

        public const float KindHeadTop = 0.755f, KindHeadX = 150.5f / 320f;
        public const float HatLift = 0.117f, HatSize = 0.44f, CharmHeight = 0.37f, CharmSize = 0.19f;
        public const float KindAspect = 320f / 343f;

        [Tooltip("One drawing per rabbit kind, in the order of RabbitKinds.All.")]
        public Sprite[] KindArt = new Sprite[0];

        public Sprite KindSprite(string kindId)
        {
            int index = RabbitKinds.IndexOf(kindId);
            if (KindArt != null && index >= 0 && index < KindArt.Length && KindArt[index] != null) return KindArt[index];
            RabbitKind kind = RabbitKinds.Find(kindId);
            return RabbitFor(kind != null ? kind.Tier : 0);
        }

        public Sprite RabbitArt(Rabbit rabbit) => KindSprite(Rabbits.KindOf(rabbit));

        public Sprite RabbitFor(int tier)
        {
            if (TierRabbits != null && tier >= 0 && tier < TierRabbits.Length && TierRabbits[tier] != null) return TierRabbits[tier];
            return Rabbit;
        }

        public Sprite DecorArt(string kindId)
        {
            switch (kindId)
            {
                case "den-da": return StoneLantern;
                case "bon-hoa": return Planter;
                case "ghe-nam": return RestProps != null && RestProps.Length > 3 ? RestProps[3] : null;
                case "tham-picnic": return RestProps != null && RestProps.Length > 2 ? RestProps[2] : null;
            }
            return null;
        }

        public Sprite ItemIcon(string defId)
        {
            foreach (ItemData item in Items)
                if (item != null && item.Id == defId) return item.Icon;
            return null;
        }

        public float ItemHatLift(string defId)
        {
            foreach (ItemData item in Items)
                if (item != null && item.Id == defId) return item.HatLift;
            return 0f;
        }

        public List<ItemDef> BuildItems()
        {
            var defs = new List<ItemDef>(Items.Length);
            foreach (var i in Items) defs.Add(i.ToDef());
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
