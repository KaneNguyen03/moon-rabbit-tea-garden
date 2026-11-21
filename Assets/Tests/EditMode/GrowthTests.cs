using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>Plots, energy, rabbit levels and items.</summary>
    public class GrowthTests
    {
        List<StationDef> defs;
        GameState s;

        static readonly ItemDef Straw = new ItemDef { Id = "straw", Name = "Mũ rơm", Slot = ItemSlot.Hat, Effect = ItemEffect.StationIncome, BaseAmount = 0.10 };
        static readonly ItemDef Chef = new ItemDef { Id = "chef", Name = "Mũ bếp", Slot = ItemSlot.Hat, Effect = ItemEffect.GardenSpeed, BaseAmount = 0.05 };
        static readonly ItemDef Lamp = new ItemDef { Id = "lamp", Name = "Lồng đèn", Slot = ItemSlot.Charm, Effect = ItemEffect.AwayIncome, BaseAmount = 0.10 };

        [SetUp]
        public void SetUp()
        {
            defs = new List<StationDef>();
            for (int i = 0; i < 6; i++)
                defs.Add(new StationDef { Id = "s" + i, Name = "S" + i, Kind = StationKind.Brew, BaseCost = 10 * System.Math.Pow(10, i), CostGrowth = 1.1, BaseIncome = 1, CycleSeconds = 1 });
            s = GameState.NewGame(defs);
        }

        Rabbit Add(int tier, int station = -1)
        {
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Name = "R", Station = station };
            s.Rabbits.Add(r);
            return r;
        }

        // ----- plots -----

        [Test]
        public void New_Garden_Has_Four_Plots_Or_Fewer_Stations()
        {
            Assert.AreEqual(4, s.Plots);
            Assert.AreEqual(3, GameState.NewGame(Fixture.Defs()).Plots);
        }

        [Test]
        public void A_Station_Needs_A_Plot_And_Plots_Come_By_Widening_The_Garden()
        {
            s.Levels = new[] { 1, 1, 1, 1, 0, 0 };
            s.Coins = new BigNum(1e12);
            Assert.IsFalse(Economy.CanUnlock(s, 4));
            Assert.IsFalse(Simulation.TryBuy(defs, s, 4));
            Assert.IsFalse(Plots.TryBuy(defs, s), "carrots alone do not widen the garden");

            s.GardenXp = Garden.XpForLevel(3);
            Add(0); Add(0); Add(0);
            Assert.AreEqual((1e5 + 1e6) * 0.25, Plots.CostOfNext(defs, s).ToDouble(), 1e-3, "both plots of the wave");
            Assert.IsTrue(Plots.TryBuy(defs, s));
            Assert.AreEqual(6, s.Plots, "a wave opens two plots");
            Assert.IsTrue(Simulation.TryBuy(defs, s, 4));
            Assert.IsFalse(Plots.CanBuy(defs, s), "six stations, six plots: nothing left");
            Assert.IsFalse(Plots.TryBuy(defs, s));
        }

        // ----- energy -----

        [Test]
        public void Energy_Starts_Full_And_Refills_To_The_Cap()
        {
            Assert.AreEqual(Energy.Max, s.Energy);
            s.Energy = 0;
            Energy.Advance(s, 60);
            Assert.AreEqual(10, s.Energy, 1e-9);
            Energy.Advance(s, 100000);
            Assert.AreEqual(Energy.Max, s.Energy);
        }

        [Test]
        public void Boost_Costs_Energy_And_Doubles_Work_While_It_Lasts()
        {
            Assert.IsTrue(Energy.TryBoost(s));
            Assert.AreEqual(50, s.Energy, 1e-9);
            Assert.IsFalse(Energy.TryBoost(s), "already running");
            Assert.AreEqual(20, Energy.Advance(s, 10), 1e-9);
            // 20 s of burst left, then 10 s of normal time
            Assert.AreEqual(20 * 2 + 10, Energy.Advance(s, 30), 1e-9);
            Assert.AreEqual(0, s.BoostSecondsLeft);
            Assert.AreEqual(5, Energy.Advance(s, 5), 1e-9);
        }

        [Test]
        public void Cannot_Boost_On_A_Low_Bar()
        {
            s.Energy = 49;
            Assert.IsFalse(Energy.TryBoost(s));
            Assert.AreEqual(49, s.Energy);
        }

        [Test]
        public void Tick_During_A_Boost_Earns_Double()
        {
            Energy.TryBoost(s);
            Simulation.Tick(defs, s, 10);
            Assert.AreEqual(20, s.Coins.ToDouble(), 1e-9);
        }

        [Test]
        public void Time_Away_Refills_Energy_But_Pays_No_Burst()
        {
            Energy.TryBoost(s);
            s.Energy = 0;
            s.LastSeenUnixSeconds = 0;
            BigNum paid = Simulation.ClaimAway(defs, s, 120, out _);
            Assert.AreEqual(120, paid.ToDouble(), 1e-6);
            Assert.AreEqual(20, s.Energy, 1e-9);
            Assert.AreEqual(0, s.BoostSecondsLeft);
        }

        // ----- rabbit levels -----

        [Test]
        public void Levels_Raise_The_Multiplier_Up_To_The_Tier_Cap()
        {
            Rabbit r = Add(0);
            Assert.AreEqual(1.1, Rabbits.Multiplier(s, r), 1e-12);
            s.Coins = new BigNum(1e9);
            Assert.AreEqual(100, Rabbits.LevelCost(r).ToDouble(), 1e-6);
            Assert.IsTrue(Rabbits.TryLevelUp(s, r.Id));
            Assert.AreEqual(1.1 * 1.05, Rabbits.Multiplier(s, r), 1e-12);
            Assert.AreEqual(130, Rabbits.LevelCost(r).ToDouble(), 1e-6);
            for (int i = 0; i < 20; i++) Rabbits.TryLevelUp(s, r.Id);
            Assert.AreEqual(10, r.Level);
            Assert.IsTrue(Rabbits.IsMaxLevel(r));
            Assert.IsFalse(Rabbits.TryLevelUp(s, r.Id));
        }

        [Test]
        public void Higher_Tiers_Cost_More_To_Train_And_Go_Further()
        {
            Assert.AreEqual(100 * 32, Rabbits.LevelCost(Add(5)).ToDouble(), 1e-6);
            Assert.AreEqual(60, Rabbits.LevelCaps[5]);
        }

        [Test]
        public void Cannot_Level_Up_When_Broke()
        {
            Rabbit r = Add(1);
            Assert.IsFalse(Rabbits.TryLevelUp(s, r.Id));
            Assert.IsFalse(Rabbits.TryLevelUp(s, 999));
            Assert.AreEqual(1, r.Level);
        }

        [Test]
        public void Merge_Keeps_The_Highest_Level()
        {
            Add(0).Level = 3; Add(0).Level = 7; Add(0);
            Assert.AreEqual(7, Rabbits.Merge(s, 0).Level);
        }

        // ----- items -----

        [Test]
        public void Granted_Items_Scale_With_Tier()
        {
            Assert.AreEqual(0.10, Items.Grant(s, Straw, 0).Amount, 1e-12);
            Assert.AreEqual(0.70, Items.Grant(s, Straw, 5).Amount, 1e-12);
            Assert.AreEqual(5, Items.Grant(s, Straw, 99).Tier);
            Assert.AreEqual(3, s.Items.Count);
        }

        [Test]
        public void A_Hat_Raises_Its_Wearers_Station()
        {
            s.Levels[0] = 10; // 10 × 2 (milestone) = 20 per cycle
            Rabbit r = Add(0, 0);
            Item hat = Items.Grant(s, Straw, 0);
            Assert.IsTrue(Items.Equip(s, hat.Id, r.Id));
            Assert.AreEqual(20 * 1.1 * 1.1, Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-9);
        }

        [Test]
        public void One_Item_Per_Slot_And_Items_Move_Between_Rabbits()
        {
            Rabbit a = Add(0), b = Add(0);
            Item hat1 = Items.Grant(s, Straw, 0), hat2 = Items.Grant(s, Chef, 0), lamp = Items.Grant(s, Lamp, 0);
            Items.Equip(s, hat1.Id, a.Id);
            Items.Equip(s, lamp.Id, a.Id);
            Items.Equip(s, hat2.Id, a.Id);
            Assert.AreEqual(0, hat1.WornBy, "replaced");
            Assert.AreSame(hat2, Items.Worn(s, a.Id, ItemSlot.Hat));
            Assert.AreSame(lamp, Items.Worn(s, a.Id, ItemSlot.Charm));

            Items.Equip(s, hat2.Id, b.Id);
            Assert.IsNull(Items.Worn(s, a.Id, ItemSlot.Hat));
            Assert.IsFalse(Items.Equip(s, hat1.Id, 999), "no such rabbit");
            Assert.IsTrue(Items.Unequip(s, lamp.Id));
            Assert.IsNull(Items.Worn(s, a.Id, ItemSlot.Charm));
        }

        [Test]
        public void Garden_Wide_Items_Only_Count_While_The_Wearer_Works()
        {
            Rabbit r = Add(0);
            Items.Equip(s, Items.Grant(s, Chef, 2).Id, r.Id);   // +10% speed
            Items.Equip(s, Items.Grant(s, Lamp, 0).Id, r.Id);   // +10% away
            Modifiers resting = Modifiers.From(null, s);
            Assert.AreEqual(1, resting.SpeedMultiplier, 1e-12);
            Assert.AreEqual(1, resting.AwayIncomeMultiplier, 1e-12);

            Rabbits.Assign(s, r.Id, 0);
            Modifiers working = Modifiers.From(null, s);
            Assert.AreEqual(1.1, working.SpeedMultiplier, 1e-12);
            Assert.AreEqual(1.1, working.AwayIncomeMultiplier, 1e-12);
        }

        [Test]
        public void Away_Bonus_Applies_To_Away_Pay_Only()
        {
            Rabbit r = Add(0, 0);
            Items.Equip(s, Items.Grant(s, Lamp, 0).Id, r.Id);
            Modifiers m = Modifiers.From(null, s);
            s.LastSeenUnixSeconds = 0;
            BigNum paid = Simulation.ClaimAway(defs, s, 100, out _, m);
            Assert.AreEqual(100 * 1.1 * 1.1, paid.ToDouble(), 1e-6); // rabbit ×1.1, lamp ×1.1
            var fresh = GameState.NewGame(defs);
            Simulation.Tick(defs, fresh, 10, null, m);
            Assert.AreEqual(10, fresh.Coins.ToDouble(), 1e-9);
        }

        [Test]
        public void Merge_Hands_Items_To_The_New_Rabbit()
        {
            Rabbit a = Add(0), b = Add(0);
            Add(0);
            Item hatA = Items.Grant(s, Straw, 0), hatB = Items.Grant(s, Chef, 0), lamp = Items.Grant(s, Lamp, 0);
            Items.Equip(s, hatA.Id, a.Id);
            Items.Equip(s, hatB.Id, b.Id);
            Items.Equip(s, lamp.Id, b.Id);
            Rabbit merged = Rabbits.Merge(s, 0);
            Assert.AreEqual(merged.Id, hatA.WornBy);
            Assert.AreEqual(0, hatB.WornBy, "the slot was taken");
            Assert.AreEqual(merged.Id, lamp.WornBy);
        }

        // ----- save -----

        [Test]
        public void Save_RoundTrip_Version_Four()
        {
            s.Plots = 5;
            s.Energy = 37.5;
            s.BoostSecondsLeft = 12;
            Rabbit r = Add(3, 0);
            r.Level = 17;
            Item hat = Items.Grant(s, Chef, 2);
            Items.Grant(s, Lamp, 1);
            Items.Equip(s, hat.Id, r.Id);

            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(5, back.Plots);
            Assert.AreEqual(37.5, back.Energy, 1e-9);
            Assert.AreEqual(12, back.BoostSecondsLeft, 1e-9);
            Assert.AreEqual(17, back.Rabbits[0].Level);
            Assert.AreEqual(2, back.Items.Count);
            Assert.AreEqual(r.Id, back.Items[0].WornBy);
            Assert.AreEqual(ItemEffect.GardenSpeed, back.Items[0].Effect);
            Assert.AreEqual(ItemSlot.Charm, back.Items[1].Slot);
            Assert.AreEqual(0.10, back.Items[0].Amount, 1e-12);
            Assert.AreEqual(s.NextItemId, back.NextItemId);
        }

        [Test]
        public void Older_Save_Keeps_Every_Open_Station_And_Gets_Full_Energy()
        {
            const string v3 = "{\"version\":3,\"stations\":[{\"id\":\"s0\",\"level\":9,\"progress\":0},{\"id\":\"s1\",\"level\":4,\"progress\":0},{\"id\":\"s2\",\"level\":2,\"progress\":0},{\"id\":\"s3\",\"level\":2,\"progress\":0},{\"id\":\"s4\",\"level\":1,\"progress\":0},{\"id\":\"s5\",\"level\":1,\"progress\":0}],\"rabbits\":[{\"id\":1,\"tier\":2,\"name\":\"A\",\"station\":\"s0\"}]}";
            GameState back = SaveCodec.Decode(defs, v3).State;
            Assert.AreEqual(6, back.Plots, "no station the player already had gets locked");
            Assert.AreEqual(Energy.Max, back.Energy);
            Assert.AreEqual(1, back.Rabbits[0].Level);
            Assert.IsEmpty(back.Items);
        }

        [Test]
        public void Load_Repairs_Bad_Growth_Data()
        {
            const string json = "{\"version\":4,\"stations\":[{\"id\":\"s0\",\"level\":3,\"progress\":0}],\"plots\":-5,\"energy\":9999,\"boost\":-3," +
                "\"rabbits\":[{\"id\":1,\"tier\":0,\"level\":500,\"name\":\"A\",\"station\":\"\"}]," +
                "\"items\":[{\"id\":1,\"def\":\"x\",\"name\":\"H1\",\"slot\":0,\"effect\":0,\"tier\":0,\"amount\":0.1,\"wornBy\":1}," +
                "{\"id\":2,\"def\":\"x\",\"name\":\"H2\",\"slot\":0,\"effect\":7,\"tier\":40,\"amount\":-2,\"wornBy\":1}," +
                "{\"id\":3,\"def\":\"x\",\"name\":\"C\",\"slot\":1,\"effect\":2,\"tier\":0,\"amount\":0.1,\"wornBy\":77},7]}";
            GameState back = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(4, back.Plots);
            Assert.AreEqual(Energy.Max, back.Energy);
            Assert.AreEqual(0, back.BoostSecondsLeft);
            Assert.AreEqual(10, back.Rabbits[0].Level, "capped at the tier's limit");
            Assert.AreEqual(3, back.Items.Count);
            Assert.AreEqual(1, back.Items[0].WornBy);
            Assert.AreEqual(0, back.Items[1].WornBy, "second hat on the same rabbit comes off");
            Assert.AreEqual(5, back.Items[1].Tier);
            Assert.AreEqual(0, back.Items[1].Amount);
            Assert.AreEqual(0, back.Items[2].WornBy, "wearer does not exist");
        }
    }
}
