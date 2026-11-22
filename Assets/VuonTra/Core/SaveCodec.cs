using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VuonTra.Core
{
    public enum LoadStatus
    {
        Ok,
        /// <summary>Nothing saved yet.</summary>
        Empty,
        /// <summary>The text is not a save this game wrote.</summary>
        Corrupt,
        /// <summary>Written by a newer build; must not be overwritten.</summary>
        NewerVersion
    }

    public struct LoadResult
    {
        public LoadStatus Status;
        /// <summary>The loaded state, or a new game for every status except Ok.</summary>
        public GameState State;
    }

    /// <summary>
    /// GameState to and from JSON text. Stations are stored by id, so adding or reordering
    /// stations in a later build keeps the player's progress.
    /// </summary>
    public static class SaveCodec
    {
        /// <summary>1 = stations only. 2 adds upgrades, quest counters, daily state and clovers. 3 adds rabbits. 4 adds plots, rabbit levels, items and energy. 5 adds rabbit kinds. 6 adds the album of kinds seen. 7 adds places to rest. 8 adds the garden's experience. 9 the quick-upgrade choices, 10 the product tray, 11 the player profile, 12 the last view. The hex grid under the garden (#52) is an additive block, no new version.</summary>
        public const int CurrentVersion = 14;

        public static string Encode(IReadOnlyList<StationDef> defs, GameState s, IReadOnlyList<UpgradeDef> upgrades = null)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"version\":").Append(CurrentVersion);
            sb.Append(",\"coinsMantissa\":").Append(s.Coins.Mantissa.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"coinsExponent\":").Append(s.Coins.Exponent.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stations\":[");
            for (int i = 0; i < defs.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":");
                WriteString(sb, defs[i].Id);
                sb.Append(",\"level\":").Append(s.Levels[i].ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"progress\":").Append(Sane(s.Progress[i]).ToString("R", CultureInfo.InvariantCulture));
                double work = s.TrayWork != null && i < s.TrayWork.Length ? Sane(s.TrayWork[i]) : 0d;
                sb.Append(",\"trayWork\":").Append(work.ToString("R", CultureInfo.InvariantCulture));
                sb.Append('}');
            }
            sb.Append("],\"buyMode\":").Append(s.BuyMode.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"spendShare\":").Append(s.SpendShare.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(",\"lastSeen\":").Append(s.LastSeenUnixSeconds.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"hintSeen\":").Append(s.HintSeen ? "true" : "false");

            sb.Append(",\"upgrades\":[");
            int upgradeCount = upgrades == null ? 0 : Math.Min(upgrades.Count, s.UpgradeLevels.Length);
            for (int i = 0; i < upgradeCount; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":");
                WriteString(sb, upgrades[i].Id);
                sb.Append(",\"level\":").Append(s.UpgradeLevels[i].ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            sb.Append("],\"clovers\":").Append(s.Clovers.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"taps\":").Append(s.TotalTaps.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"levelsBought\":").Append(s.LevelsBought.ToString(CultureInfo.InvariantCulture));
            WriteBig(sb, "earned", s.TotalEarned);
            sb.Append(",\"dailyDay\":").Append(s.DailyDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dailyTaps\":").Append(s.DailyTapsBase.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dailyLevels\":").Append(s.DailyLevelsBase.ToString(CultureInfo.InvariantCulture));
            WriteBig(sb, "dailyEarned", s.DailyEarnedBase);
            WriteBig(sb, "dailyTarget", s.DailyCarrotTarget);
            sb.Append(",\"claimed\":[");
            bool first = true;
            foreach (string id in s.Claimed)
            {
                if (!first) sb.Append(',');
                first = false;
                WriteString(sb, id);
            }
            sb.Append("],\"gardenXp\":").Append(s.GardenXp.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"restSpots\":").Append(s.RestSpots.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"freshKinds\":[");
            {
                int n = 0;
                foreach (string kind in s.FreshKinds)
                {
                    if (n++ > 0) sb.Append(',');
                    WriteString(sb, kind);
                }
            }
            sb.Append("],\"seenKinds\":[");
            first = true;
            foreach (string id in s.SeenKinds)
            {
                if (!first) sb.Append(',');
                first = false;
                WriteString(sb, id);
            }
            sb.Append("],\"nextRabbitId\":").Append(s.NextRabbitId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"rabbits\":[");
            for (int i = 0; i < s.Rabbits.Count; i++)
            {
                Rabbit r = s.Rabbits[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(r.Id.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"tier\":").Append(r.Tier.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"level\":").Append(r.Level.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"affection\":").Append(r.Affection.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"petDay\":").Append(r.PetDay.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"dye\":").Append(r.Dye.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"kind\":");
                WriteString(sb, Rabbits.KindOf(r));
                sb.Append(",\"name\":");
                WriteString(sb, r.Name);
                // Stations by id, like everywhere else in the save, so reordering them is safe.
                sb.Append(",\"station\":");
                WriteString(sb, r.Station >= 0 && r.Station < defs.Count ? defs[r.Station].Id : "");
                sb.Append('}');
            }
            sb.Append("],\"plots\":").Append(s.Plots.ToString(CultureInfo.InvariantCulture));
            // Version 10: the product tray.
            sb.Append(",\"traySlots\":").Append(s.TraySlots.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"goldenToday\":").Append(s.GoldenToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"photoDay\":").Append(s.PhotoDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"photosTaken\":").Append(s.PhotosTaken.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"tutorial\":").Append(s.TutorialStep.ToString(CultureInfo.InvariantCulture));
            // Version 13: golden carrots and the shop's daily gift.
            sb.Append(",\"golden\":").Append(s.GoldenCarrots.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"giftDay\":").Append(s.GiftDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"giftStreak\":").Append(s.GiftStreak.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"giftBig\":").Append(s.GiftBigDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"giftBigGolden\":").Append(s.GiftBigGolden ? "true" : "false");
            sb.Append(",\"visitorBought\":").Append(s.VisitorBoughtDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"visitorSeen\":").Append(s.VisitorSeenDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"tutorialClover\":").Append(s.TutorialCloverGiven ? "true" : "false");
            sb.Append(",\"photosGold\":").Append(s.PhotosWithGoldRabbit.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"photosRest\":").Append(s.PhotosWithFullRest.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"photosSeason\":").Append(s.PhotosWithSeasonRabbit.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"maxDay\":").Append(s.MaxDaySeen.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"seasonGreeted\":").Append(s.SeasonGreetedDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"awayDouble\":").Append(s.AwayDoubleDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"playSeconds\":").Append(Sane(s.PlaySeconds).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"season\":").Append(s.Season.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"packs\":").Append(s.PacksBought.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"paid\":[");
            for (int i = 0; i < s.PaidTransactions.Count; i++)
            {
                if (i > 0) sb.Append(',');
                WriteString(sb, s.PaidTransactions[i]);
            }
            sb.Append(']');
            // Version 14: the mooncake game.
            sb.Append(",\"pounding\":{\"day\":").Append(s.PoundingDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"plays\":").Append(s.PoundingPlays.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"star\":").Append(s.PoundingStarToday ? "true" : "false");
            sb.Append(",\"three\":").Append(s.PoundingThreeToday ? "true" : "false");
            sb.Append(",\"games\":").Append(s.PoundingGames.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"best\":").Append(s.PoundingBestStars.ToString(CultureInfo.InvariantCulture)).Append('}');
            // Still version 14: the leaf-catching game, its own block. A reader without it defaults the block, so no bump.
            sb.Append(",\"leaves\":{\"day\":").Append(s.Leaves.Day.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"plays\":").Append(s.Leaves.Plays.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"star\":").Append(s.Leaves.StarToday ? "true" : "false");
            sb.Append(",\"games\":").Append(s.Leaves.Games.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"best\":").Append(s.Leaves.BestStars.ToString(CultureInfo.InvariantCulture)).Append('}');
            // Still version 14: the firefly night, its own block with the day's capped gifts. Absent in older saves, so no bump.
            sb.Append(",\"fireflies\":{\"day\":").Append(s.Fireflies.Day.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"plays\":").Append(s.Fireflies.Plays.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"clovers\":").Append(s.Fireflies.CloversToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"golden\":").Append(s.Fireflies.GoldenToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"games\":").Append(s.Fireflies.Games.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"best\":").Append(s.Fireflies.BestStars.ToString(CultureInfo.InvariantCulture)).Append('}');
            sb.Append(",\"album\":[");
            for (int i = 0; i < s.Album.Count; i++)
            {
                if (i > 0) sb.Append(',');
                WriteString(sb, s.Album[i]);
            }
            sb.Append(']');
            sb.Append(",\"tray\":[");
            for (int i = 0; i < s.Tray.Count; i++)
            {
                Product product = s.Tray[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"station\":");
                WriteString(sb, product.Station >= 0 && product.Station < defs.Count ? defs[product.Station].Id : "");
                sb.Append(",\"count\":").Append(product.Count.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"golden\":").Append(product.Golden ? "true" : "false");
                sb.Append('}');
            }
            sb.Append(']');
            sb.Append(",\"energy\":").Append(Sane(s.Energy).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"boost\":").Append(Sane(s.BoostSecondsLeft).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"nextItemId\":").Append(s.NextItemId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"items\":[");
            for (int i = 0; i < s.Items.Count; i++)
            {
                Item item = s.Items[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(item.Id.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"def\":");
                WriteString(sb, item.DefId);
                sb.Append(",\"name\":");
                WriteString(sb, item.Name);
                sb.Append(",\"slot\":").Append(((int)item.Slot).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"effect\":").Append(((int)item.Effect).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"tier\":").Append(item.Tier.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"amount\":").Append(Sane(item.Amount).ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"wornBy\":").Append(item.WornBy.ToString(CultureInfo.InvariantCulture));
                sb.Append('}');
            }
            // Version 11: the player profile.
            sb.Append("],\"profile\":{\"name\":");
            WriteString(sb, s.Profile.GardenName);
            sb.Append(",\"avatar\":").Append(s.Profile.AvatarRabbitId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"linkRewarded\":").Append(s.Profile.LinkRewarded ? "true" : "false");
            sb.Append(",\"code\":");
            WriteString(sb, s.Profile.Code);
            sb.Append("}");
            // Version 12: where the player last looked.
            sb.Append(",\"view\":{\"x\":").Append(Sane(s.ViewX).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"y\":").Append(Sane(s.ViewY).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"zoom\":").Append(Sane(s.ViewZoom).ToString("R", CultureInfo.InvariantCulture));
            sb.Append('}');
            // Guests' orders (#20), an additive block: the schedule, the counts, and the guest at the counter if any.
            sb.Append(",\"orders\":{\"clock\":").Append(Sane(s.OrderClock).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"today\":").Append(s.OrdersToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"day\":").Append(s.OrderDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"delivered\":").Append(s.OrdersDelivered.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dailyBase\":").Append(s.DailyOrdersBase.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"nextId\":").Append(s.NextOrderId.ToString(CultureInfo.InvariantCulture));
            if (s.Order != null)
            {
                Order o = s.Order;
                sb.Append(",\"order\":{\"id\":").Append(o.Id.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"guest\":");
                WriteString(sb, o.Guest);
                sb.Append(",\"left\":").Append(Sane(o.Left).ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"seconds\":").Append(Sane(o.RewardSeconds).ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"clovers\":").Append(o.RewardClovers.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"lines\":[");
                for (int i = 0; i < o.Lines.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"station\":");
                    WriteString(sb, o.Lines[i].Station >= 0 && o.Lines[i].Station < defs.Count ? defs[o.Lines[i].Station].Id : "");
                    sb.Append(",\"count\":").Append(o.Lines[i].Count.ToString(CultureInfo.InvariantCulture)).Append('}');
                }
                sb.Append("]}");
            }
            sb.Append('}');
            // A rabbit asking for its picture (#56), an additive block: the clock, the bubble if any, today's count and the totals.
            sb.Append(",\"photoAsk\":{\"clock\":").Append(Sane(s.PhotoAskClock).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"left\":").Append(Sane(s.PhotoAskLeft).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"rabbit\":").Append(s.PhotoAskRabbit.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"today\":").Append(s.PhotoAsksToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"day\":").Append(s.PhotoAskDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"answered\":").Append(s.PhotoAsksAnswered.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dailyBase\":").Append(s.DailyPhotoAsksBase.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            // The album's milestones whose gifts were given (#61), an additive list.
            sb.Append(",\"albumMilestones\":[");
            first = true;
            foreach (int milestone in s.AlbumMilestonesClaimed)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append(milestone.ToString(CultureInfo.InvariantCulture));
            }
            sb.Append(']');
            // Decorating the garden (#11), an additive block: what is owned by kind, what stands on each spot, the lantern colour.
            sb.Append(",\"decor\":{\"owned\":{");
            for (int i = 0; i < Decor.Kinds.Length; i++)
            {
                if (i > 0) sb.Append(',');
                WriteString(sb, Decor.Kinds[i].Id);
                sb.Append(':').Append((i < s.DecorOwned.Length ? s.DecorOwned[i] : 0).ToString(CultureInfo.InvariantCulture));
            }
            sb.Append("},\"spots\":[");
            for (int i = 0; i < s.DecorSpots.Length; i++)
            {
                if (i > 0) sb.Append(',');
                if (s.DecorSpots[i] == null) sb.Append("null"); else WriteString(sb, s.DecorSpots[i]);
            }
            sb.Append("],\"lantern\":").Append(s.LanternColor.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            // Moon jars (#64), an additive block: how many are held, the shop's day and count, the day of the last free jar.
            sb.Append(",\"moonJars\":{\"held\":").Append(s.MoonJars.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"buyDay\":").Append(s.MoonJarBuyDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"bought\":").Append(s.MoonJarsBoughtToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"freeDay\":").Append(s.MoonJarFreeDay.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            // The hex grid (#52), an additive block: how wide the lawn is, and the tile of everything standing on it. Empty tiles follow.
            sb.Append(",\"grid\":{\"radius\":").Append(s.Grid.Radius.ToString(CultureInfo.InvariantCulture)).Append(",\"tiles\":[");
            {
                var ids = new List<string>();
                foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants) ids.Add(o.Key);
                ids.Sort(string.CompareOrdinal); // the same grid always writes the same text
                for (int i = 0; i < ids.Count; i++)
                {
                    Hex h = s.Grid.TileOf(ids[i]).Value;
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"q\":").Append(h.Q.ToString(CultureInfo.InvariantCulture)).Append(",\"r\":").Append(h.R.ToString(CultureInfo.InvariantCulture)).Append(",\"id\":");
                    WriteString(sb, ids[i]);
                    sb.Append('}');
                }
            }
            sb.Append(']');
            if (s.Grid.StoredCount > 0)
            {
                // The stations put away (#54), an additive list written only when there are some.
                var stored = new List<string>(s.Grid.Stored);
                stored.Sort(string.CompareOrdinal);
                sb.Append(",\"stored\":[");
                for (int i = 0; i < stored.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    WriteString(sb, stored[i]);
                }
                sb.Append(']');
            }
            sb.Append("}}");
            return sb.ToString();
        }

        public static LoadResult Decode(IReadOnlyList<StationDef> defs, string json, IReadOnlyList<UpgradeDef> upgrades = null)
        {
            var result = new LoadResult { State = GameState.NewGame(defs, upgrades == null ? 0 : upgrades.Count) };
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Status = LoadStatus.Empty;
                return result;
            }

            Dictionary<string, object> root;
            try
            {
                root = new Reader(json).ReadDocument() as Dictionary<string, object>;
            }
            catch (FormatException)
            {
                root = null;
            }

            if (root == null || !TryNumber(root, "version", out double version) || version < 1d)
            {
                result.Status = LoadStatus.Corrupt;
                return result;
            }
            if (version > CurrentVersion)
            {
                result.Status = LoadStatus.NewerVersion;
                return result;
            }

            var s = result.State;
            TryNumber(root, "coinsMantissa", out double mantissa);
            TryNumber(root, "coinsExponent", out double exponent);
            s.Coins = BigNum.FromParts(mantissa, ToLong(exponent));

            if (root.TryGetValue("stations", out object stationsObj) && stationsObj is List<object> stations)
            {
                foreach (object item in stations)
                {
                    if (!(item is Dictionary<string, object> entry)) continue;
                    if (!(entry.TryGetValue("id", out object idObj) && idObj is string id)) continue;
                    int index = IndexOf(defs, id);
                    if (index < 0) continue; // a station this build no longer has

                    TryNumber(entry, "level", out double level);
                    TryNumber(entry, "progress", out double progress);
                    s.Levels[index] = level > 0d ? (int)Math.Min(level, int.MaxValue) : 0;
                    s.Progress[index] = (progress >= 0d && progress < 1d) ? progress : 0d;
                    // Version 10: work towards the next special batch. Never more than one batch's worth.
                    if (TryNumber(entry, "trayWork", out double trayWork)) s.TrayWork[index] = Math.Max(0d, Math.Min(Tray.ProductEvery, trayWork));
                }
            }
            if (s.Levels.Length > 0 && s.Levels[0] < 1) s.Levels[0] = 1;

            s.BuyMode = Simulation.BuyOne;
            if (TryNumber(root, "buyMode", out double buyMode))
            {
                if (buyMode == Simulation.BuyMax) s.BuyMode = Simulation.BuyMax;
                else if (buyMode == Simulation.BuyTen) s.BuyMode = Simulation.BuyTen;
                else if (buyMode == Simulation.BuyToMilestone) s.BuyMode = Simulation.BuyToMilestone;
            }
            // Version 9: how much of the purse a quick upgrade may spend. Only the three choices the game offers.
            s.SpendShare = 1d;
            if (TryNumber(root, "spendShare", out double share) && (share == 0.5d || share == 0.25d)) s.SpendShare = share;
            TryNumber(root, "lastSeen", out double lastSeen);
            s.LastSeenUnixSeconds = ToLong(lastSeen);
            s.HintSeen = root.TryGetValue("hintSeen", out object hint) && hint is bool b && b;

            // Everything below arrived in version 2; a version 1 save simply has none of it.
            if (upgrades != null && root.TryGetValue("upgrades", out object upgradesObj) && upgradesObj is List<object> savedUpgrades)
            {
                foreach (object item in savedUpgrades)
                {
                    if (!(item is Dictionary<string, object> entry)) continue;
                    if (!(entry.TryGetValue("id", out object idObj) && idObj is string id)) continue;
                    for (int i = 0; i < upgrades.Count; i++)
                    {
                        if (upgrades[i].Id != id) continue;
                        TryNumber(entry, "level", out double level);
                        s.UpgradeLevels[i] = (int)Math.Max(0d, Math.Min(level, upgrades[i].MaxLevel));
                    }
                }
            }
            s.Clovers = (int)Math.Max(0d, Math.Min(ReadNumber(root, "clovers"), int.MaxValue));
            s.TotalTaps = Math.Max(0L, ToLong(ReadNumber(root, "taps")));
            s.LevelsBought = Math.Max(0L, ToLong(ReadNumber(root, "levelsBought")));
            s.TotalEarned = ReadBig(root, "earned");
            s.DailyDay = ToLong(ReadNumber(root, "dailyDay"));
            s.DailyTapsBase = Math.Max(0L, ToLong(ReadNumber(root, "dailyTaps")));
            s.DailyLevelsBase = Math.Max(0L, ToLong(ReadNumber(root, "dailyLevels")));
            s.DailyEarnedBase = ReadBig(root, "dailyEarned");
            s.DailyCarrotTarget = ReadBig(root, "dailyTarget");
            if (root.TryGetValue("claimed", out object claimedObj) && claimedObj is List<object> claimed)
            {
                foreach (object item in claimed)
                    if (item is string claimedId) s.Claimed.Add(claimedId);
            }

            // Version 3: the rabbit collection.
            if (root.TryGetValue("rabbits", out object rabbitsObj) && rabbitsObj is List<object> rabbits)
            {
                var taken = new HashSet<int>();
                var ids = new HashSet<long>();
                foreach (object item in rabbits)
                {
                    if (!(item is Dictionary<string, object> entry)) continue;
                    long rabbitId = ToLong(ReadNumber(entry, "id"));
                    if (rabbitId <= 0 || !ids.Add(rabbitId)) continue; // no id, or a duplicate
                    var rabbit = new Rabbit
                    {
                        Id = rabbitId,
                        Tier = (int)Math.Max(0d, Math.Min(ReadNumber(entry, "tier"), Rabbits.TierCount - 1)),
                        Level = 1,
                        Name = entry.TryGetValue("name", out object nameObj) && nameObj is string name ? name : "",
                    };
                    if (entry.TryGetValue("station", out object stationObj) && stationObj is string stationId && stationId.Length > 0)
                    {
                        int index = IndexOf(defs, stationId);
                        // One rabbit per station, and only at stations that are open.
                        if (index >= 0 && s.Levels[index] > 0 && taken.Add(index)) rabbit.Station = index;
                    }
                    rabbit.Level = (int)Math.Max(1d, Math.Min(ReadNumber(entry, "level"), Rabbits.LevelCaps[rabbit.Tier]));
                    // The bond (#19): saves from before it, or odd values, start with no hearts and no day.
                    rabbit.Affection = (int)Math.Max(0d, Math.Min(ReadNumber(entry, "affection"), Rabbits.AffectionMax));
                    rabbit.PetDay = TryNumber(entry, "petDay", out double petDay) && petDay >= 0d ? ToLong(petDay) : -1;
                    // Fur dye (#44): saves from before it, and colours the palette lacks, mean its own coat.
                    rabbit.Dye = TryNumber(entry, "dye", out double dye) && dye >= 0d && dye < Shop.Dyes.Length ? (int)dye : -1;
                    // Version 5: the kind. Older saves have none, and a kind must belong to the rabbit's tier.
                    rabbit.Kind = entry.TryGetValue("kind", out object kindObj) && kindObj is string kindId ? kindId : null;
                    rabbit.Kind = Rabbits.KindOf(rabbit);
                    s.Rabbits.Add(rabbit);
                    if (rabbitId > s.NextRabbitId) s.NextRabbitId = rabbitId;
                }
            }
            // Version 7: places to rest. Older saves start with the usual two.
            s.RestSpots = root.ContainsKey("restSpots")
                ? (int)Math.Max(RestSpots.Starting, Math.Min(ReadNumber(root, "restSpots"), RestSpots.Max))
                : RestSpots.Starting;

            // Version 6: the album. Whatever the save says, every rabbit still here has certainly been seen.
            if (root.TryGetValue("seenKinds", out object seenObj) && seenObj is List<object> seen)
                foreach (object item in seen)
                    if (item is string seenId && RabbitKinds.Find(seenId) != null) s.SeenKinds.Add(seenId); // straight into the album: nothing here is new
            foreach (Rabbit r in s.Rabbits) s.SeenKinds.Add(Rabbits.KindOf(r));
            // The "new" marks: only kinds that are in the album can carry one.
            if (root.TryGetValue("freshKinds", out object freshObj) && freshObj is List<object> fresh)
                foreach (object item in fresh)
                    if (item is string kind && s.SeenKinds.Contains(kind)) s.FreshKinds.Add(kind);

            long savedNext = ToLong(ReadNumber(root, "nextRabbitId"));
            if (savedNext > s.NextRabbitId) s.NextRabbitId = savedNext;

            // Version 4. A save from before plots existed keeps every station it had already opened.
            int highestOpen = 0;
            for (int i = 0; i < s.Levels.Length; i++)
                if (s.Levels[i] > 0) highestOpen = i + 1;
            int plots = TryNumber(root, "plots", out double savedPlots) ? (int)Math.Min(savedPlots, defs.Count) : 0;
            s.Plots = Math.Min(defs.Count, Math.Max(Math.Max(plots, highestOpen), Math.Min(defs.Count, Plots.Starting)));

            s.Energy = TryNumber(root, "energy", out double energy) ? Math.Max(0d, Math.Min(energy, Energy.Max)) : Energy.Max;
            s.BoostSecondsLeft = Math.Max(0d, Math.Min(ReadNumber(root, "boost"), Energy.BoostSeconds));

            if (root.TryGetValue("items", out object itemsObj) && itemsObj is List<object> items)
            {
                var itemIds = new HashSet<long>();
                var slots = new HashSet<string>();
                foreach (object raw in items)
                {
                    if (!(raw is Dictionary<string, object> entry)) continue;
                    long itemId = ToLong(ReadNumber(entry, "id"));
                    if (itemId <= 0 || !itemIds.Add(itemId)) continue;
                    double slot = ReadNumber(entry, "slot"), effect = ReadNumber(entry, "effect");
                    var item = new Item
                    {
                        Id = itemId,
                        DefId = entry.TryGetValue("def", out object defObj) && defObj is string defId ? defId : "",
                        Name = entry.TryGetValue("name", out object itemNameObj) && itemNameObj is string itemName ? itemName : "",
                        Slot = slot == 1d ? ItemSlot.Charm : ItemSlot.Hat,
                        Effect = effect == 1d ? ItemEffect.GardenSpeed : (effect == 2d ? ItemEffect.AwayIncome : (effect == 3d ? ItemEffect.None : ItemEffect.StationIncome)),
                        Tier = (int)Math.Max(0d, Math.Min(ReadNumber(entry, "tier"), Rabbits.TierCount - 1)),
                        Amount = Math.Max(0d, Math.Min(ReadNumber(entry, "amount"), 1000d)),
                    };
                    long wornBy = ToLong(ReadNumber(entry, "wornBy"));
                    // Worn only by a rabbit that exists, one item per slot.
                    if (wornBy > 0 && Rabbits.Find(s, wornBy) != null && slots.Add(wornBy + ":" + (int)item.Slot)) item.WornBy = wornBy;
                    s.Items.Add(item);
                    if (itemId > s.NextItemId) s.NextItemId = itemId;
                }
            }
            long savedNextItem = ToLong(ReadNumber(root, "nextItemId"));
            if (savedNextItem > s.NextItemId) s.NextItemId = savedNextItem;

            // A garden that already holds the land of a wave also has that wave's place to rest, and keeps any it had beyond that.
            s.RestSpots = Math.Max(s.RestSpots, Math.Min(RestSpots.Max, RestSpots.Starting + Expansion.WavesTaken(s.Plots)));

            // Version 8: the garden's experience. An older save is given what its progress would have earned,
            // and no garden is ever below the level its land already needs.
            double xp = TryNumber(root, "gardenXp", out double savedXp) && savedXp >= 0d ? savedXp : Garden.BackfillXp(defs, s);
            s.GardenXp = Math.Max(xp, Garden.XpForLevel(Expansion.LevelForPlots(defs, s.Plots)));

            // Version 10: the product tray. Only known stations, stacks within their limit, no more slots than the game has.
            s.TraySlots = TryNumber(root, "traySlots", out double traySlots)
                ? (int)Math.Max(Tray.StartingSlots, Math.Min(traySlots, Tray.MaxSlots))
                : Tray.StartingSlots;
            s.GoldenToday = TryNumber(root, "goldenToday", out double goldenToday) ? (int)Math.Max(0d, Math.Min(goldenToday, Tray.GoldenPerDay)) : 0;
            s.PhotoDay = TryNumber(root, "photoDay", out double photoDay) ? ToLong(photoDay) : -1;
            s.PhotosTaken = TryNumber(root, "photosTaken", out double photosTaken) ? Math.Max(0, ToLong(photosTaken)) : 0;
            // The first minute's guidance. A save from before it that has played at all is not shown it.
            if (TryNumber(root, "tutorial", out double tutorial)) s.TutorialStep = (int)Math.Max(0, Math.Min(tutorial, Tutorial.Steps));
            else s.TutorialStep = s.HintSeen || (s.Levels.Length > 0 && s.Levels[0] > 1) || s.TotalTaps > 0 ? Tutorial.Steps : 0;
            s.TutorialCloverGiven = root.TryGetValue("tutorialClover", out object tutClover) && tutClover is bool tc && tc;
            // Version 13: golden carrots. An older save has none, and its first daily gift is still to come.
            s.GoldenCarrots = TryNumber(root, "golden", out double goldenCarrots) ? (int)Math.Max(0d, Math.Min(goldenCarrots, int.MaxValue)) : 0;
            s.GiftDay = TryNumber(root, "giftDay", out double giftDay) ? ToLong(giftDay) : -1;
            s.GiftStreak = TryNumber(root, "giftStreak", out double giftStreak) ? (int)Math.Max(0d, Math.Min(giftStreak, 100000)) : 0;
            // The first week's big gift (#57), added within version 14: a save without it has the big gift still to come.
            s.GiftBigDay = TryNumber(root, "giftBig", out double giftBig) ? Math.Max(-1L, ToLong(giftBig)) : -1;
            s.GiftBigGolden = root.TryGetValue("giftBigGolden", out object bigGolden) && bigGolden is bool bg && bg;
            s.VisitorBoughtDay = TryNumber(root, "visitorBought", out double visitorBought) ? ToLong(visitorBought) : -1;
            s.VisitorSeenDay = TryNumber(root, "visitorSeen", out double visitorSeen) ? ToLong(visitorSeen) : -1;
            s.PhotosWithGoldRabbit = TryNumber(root, "photosGold", out double photosGold) ? Math.Max(0, ToLong(photosGold)) : 0;
            s.PhotosWithFullRest = TryNumber(root, "photosRest", out double photosRest) ? Math.Max(0, ToLong(photosRest)) : 0;
            s.PhotosWithSeasonRabbit = TryNumber(root, "photosSeason", out double photosSeason) ? Math.Max(0, ToLong(photosSeason)) : 0;
            s.MaxDaySeen = TryNumber(root, "maxDay", out double maxDay) ? ToLong(maxDay) : -1;
            s.SeasonGreetedDay = TryNumber(root, "seasonGreeted", out double greeted) ? ToLong(greeted) : -1;
            s.AwayDoubleDay = TryNumber(root, "awayDouble", out double awayDouble) ? ToLong(awayDouble) : -1;
            s.PlaySeconds = TryNumber(root, "playSeconds", out double played) ? Math.Max(0d, Sane(played)) : 0d;
            s.Season = TryNumber(root, "season", out double season) ? (int)Math.Max(0d, Math.Min(season, NewMoon.MaxSeasons)) : 0;
            s.PacksBought = TryNumber(root, "packs", out double packs) ? (int)Math.Max(0d, Math.Min(packs, int.MaxValue)) : 0;
            if (root.TryGetValue("paid", out object paidObj) && paidObj is List<object> paid)
            {
                foreach (object item in paid)
                    if (item is string paidId && !s.PaidTransactions.Contains(paidId)) s.PaidTransactions.Add(paidId);
                while (s.PaidTransactions.Count > Storefront.ReceiptMemory) s.PaidTransactions.RemoveAt(0);
            }
            // Version 14: the mooncake game. An older save has all its plays and no games behind it.
            if (root.TryGetValue("pounding", out object poundObj) && poundObj is Dictionary<string, object> pound)
            {
                s.PoundingDay = TryNumber(pound, "day", out double pDay) ? ToLong(pDay) : -1;
                s.PoundingPlays = (int)Math.Max(0d, Math.Min(ReadNumber(pound, "plays"), Pounding.PlaysPerDay));
                s.PoundingStarToday = pound.TryGetValue("star", out object pStar) && pStar is bool ps && ps;
                s.PoundingThreeToday = pound.TryGetValue("three", out object pThree) && pThree is bool pt && pt;
                s.PoundingGames = Math.Max(0, ToLong(ReadNumber(pound, "games")));
                s.PoundingBestStars = (int)Math.Max(0d, Math.Min(ReadNumber(pound, "best"), 3));
            }
            // The leaf-catching game. A save from before it has all its plays and no games behind it.
            if (root.TryGetValue("leaves", out object leavesObj) && leavesObj is Dictionary<string, object> leaves)
            {
                s.Leaves.Day = TryNumber(leaves, "day", out double lDay) ? ToLong(lDay) : -1;
                s.Leaves.Plays = (int)Math.Max(0d, Math.Min(ReadNumber(leaves, "plays"), Leaves.PlaysPerDay));
                s.Leaves.StarToday = leaves.TryGetValue("star", out object lStar) && lStar is bool ls && ls;
                s.Leaves.Games = Math.Max(0, ToLong(ReadNumber(leaves, "games")));
                s.Leaves.BestStars = (int)Math.Max(0d, Math.Min(ReadNumber(leaves, "best"), 3));
            }
            // The firefly night. A save from before it has all its plays, the day's gifts untouched and no games behind it.
            if (root.TryGetValue("fireflies", out object firefliesObj) && firefliesObj is Dictionary<string, object> fireflies)
            {
                s.Fireflies.Day = TryNumber(fireflies, "day", out double fDay) ? ToLong(fDay) : -1;
                s.Fireflies.Plays = (int)Math.Max(0d, Math.Min(ReadNumber(fireflies, "plays"), Fireflies.PlaysPerDay));
                s.Fireflies.CloversToday = (int)Math.Max(0d, Math.Min(ReadNumber(fireflies, "clovers"), Fireflies.CloverCapPerDay));
                s.Fireflies.GoldenToday = (int)Math.Max(0d, Math.Min(ReadNumber(fireflies, "golden"), Fireflies.GoldenCapPerDay));
                s.Fireflies.Games = Math.Max(0, ToLong(ReadNumber(fireflies, "games")));
                s.Fireflies.BestStars = (int)Math.Max(0d, Math.Min(ReadNumber(fireflies, "best"), 3));
            }
            if (root.TryGetValue("album", out object albumObj) && albumObj is List<object> album)
                foreach (object item in album)
                    if (item is string path && path.Length > 0 && s.Album.Count < Photos.AlbumSize && !s.Album.Contains(path)) s.Album.Add(path);
            if (root.TryGetValue("tray", out object trayObj) && trayObj is List<object> tray)
                foreach (object item in tray)
                {
                    if (!(item is Dictionary<string, object> entry) || s.Tray.Count >= s.TraySlots) continue;
                    int station = entry.TryGetValue("station", out object stationObj) && stationObj is string stationId ? IndexOf(defs, stationId) : -1;
                    int count = (int)Math.Min(ReadNumber(entry, "count"), Tray.StackLimit);
                    if (station < 0 || count < 1) continue;
                    bool golden = entry.TryGetValue("golden", out object goldenObj) && goldenObj is bool g && g;
                    s.Tray.Add(new Product { Station = station, Count = count, Golden = golden });
                }

            // Version 11: the profile. A save from before it, or with a code that is not one, gets a fresh code;
            // the avatar must be a rabbit that is still here.
            if (root.TryGetValue("profile", out object profileObj) && profileObj is Dictionary<string, object> profile)
            {
                s.Profile.GardenName = profile.TryGetValue("name", out object nameObj) && nameObj is string gardenName ? (Profile.CleanName(gardenName) ?? "") : "";
                long avatar = ToLong(ReadNumber(profile, "avatar"));
                s.Profile.AvatarRabbitId = avatar > 0 && Rabbits.Find(s, avatar) != null ? avatar : 0;
                if (profile.TryGetValue("code", out object codeObj) && codeObj is string code && Profile.IsCode(code)) s.Profile.Code = code;
                s.Profile.LinkRewarded = profile.TryGetValue("linkRewarded", out object rewardedObj) && rewardedObj is bool rewarded && rewarded;
            }
            if (!Profile.IsCode(s.Profile.Code)) s.Profile.Code = Profile.NewCode();

            // Version 12: the last view. A zoom outside what the game offers means "never recorded".
            if (root.TryGetValue("view", out object viewObj) && viewObj is Dictionary<string, object> view
                && TryNumber(view, "zoom", out double zoom) && zoom >= 0.3d && zoom <= 2.5d)
            {
                s.ViewZoom = zoom;
                s.ViewX = ReadNumber(view, "x");
                s.ViewY = ReadNumber(view, "y");
            }

            // Decorating the garden (#11). A save from before it owns nothing, stands nothing, and hangs paper lanterns.
            if (root.TryGetValue("decor", out object decorObj) && decorObj is Dictionary<string, object> decor)
            {
                if (decor.TryGetValue("owned", out object ownedObj) && ownedObj is Dictionary<string, object> owned)
                    for (int i = 0; i < Decor.Kinds.Length; i++)
                        s.DecorOwned[i] = (int)Math.Max(0d, Math.Min(ReadNumber(owned, Decor.Kinds[i].Id), Decor.Spots));
                if (decor.TryGetValue("spots", out object spotsObj) && spotsObj is List<object> spots)
                    for (int i = 0; i < Decor.Spots && i < spots.Count; i++)
                    {
                        // Only a kind the shop sells, and never more standing than are owned: the extras go back to storage.
                        string kind = spots[i] as string;
                        if (kind == null || Decor.IndexOf(kind) < 0 || Decor.Available(s, kind) <= 0) continue;
                        s.DecorSpots[i] = kind;
                    }
                double lantern = ReadNumber(decor, "lantern");
                s.LanternColor = lantern >= 0d && lantern < Decor.LanternColors.Length ? (int)lantern : 0;
            }
            // Guests' orders (#20). A save from before them: the first guest comes a minute after opening, nothing delivered.
            if (root.TryGetValue("orders", out object ordersObj) && ordersObj is Dictionary<string, object> orders)
            {
                s.OrderClock = TryNumber(orders, "clock", out double clock) ? Math.Max(0d, Math.Min(Orders.Interval, clock)) : Orders.StartingClock;
                s.OrdersToday = (int)Math.Max(0d, Math.Min(ReadNumber(orders, "today"), Orders.PerDay));
                s.OrderDay = TryNumber(orders, "day", out double orderDay) ? ToLong(orderDay) : -1;
                s.OrdersDelivered = Math.Max(0L, ToLong(ReadNumber(orders, "delivered")));
                s.DailyOrdersBase = Math.Max(0L, Math.Min(s.OrdersDelivered, ToLong(ReadNumber(orders, "dailyBase"))));
                s.NextOrderId = Math.Max(0L, ToLong(ReadNumber(orders, "nextId")));
                if (orders.TryGetValue("order", out object orderObj) && orderObj is Dictionary<string, object> entry)
                {
                    var order = new Order
                    {
                        Id = ToLong(ReadNumber(entry, "id")),
                        Guest = entry.TryGetValue("guest", out object guestObj) && guestObj is string guest && RabbitKinds.Find(guest) != null ? guest : RabbitKinds.DefaultFor(0),
                        Left = Math.Max(0d, Math.Min(Orders.WaitSeconds, ReadNumber(entry, "left"))),
                        RewardSeconds = Math.Max(0d, Math.Min(Orders.RewardSeconds, ReadNumber(entry, "seconds"))),
                        RewardClovers = (int)Math.Max(0d, Math.Min(ReadNumber(entry, "clovers"), 1)),
                    };
                    if (entry.TryGetValue("lines", out object linesObj) && linesObj is List<object> lines)
                        foreach (object raw in lines)
                        {
                            if (!(raw is Dictionary<string, object> line) || order.Lines.Count >= Orders.MaxLines) continue;
                            int station = line.TryGetValue("station", out object stationObj) && stationObj is string stationId ? IndexOf(defs, stationId) : -1;
                            int count = (int)Math.Min(ReadNumber(line, "count"), Tray.StackLimit);
                            if (station < 0 || count < 1) continue;
                            order.Lines.Add(new OrderLine { Station = station, Count = count });
                        }
                    // A guest with nothing left to ask for, or whose patience is spent, is not kept.
                    if (order.Lines.Count > 0 && order.Left > 0d) s.Order = order;
                    if (order.Id > s.NextOrderId) s.NextOrderId = order.Id;
                }
            }

            // A rabbit asking for its picture (#56). A save from before it: the clock starts from nothing, nobody asks.
            if (root.TryGetValue("photoAsk", out object askObj) && askObj is Dictionary<string, object> ask)
            {
                s.PhotoAskClock = TryNumber(ask, "clock", out double askClock) ? Math.Max(0d, Math.Min(PhotoAsks.Interval, askClock)) : 0d;
                s.PhotoAskLeft = Math.Max(0d, Math.Min(PhotoAsks.StaySeconds, ReadNumber(ask, "left")));
                s.PhotoAskRabbit = Math.Max(0L, ToLong(ReadNumber(ask, "rabbit")));
                // A bubble whose time is spent, or whose rabbit is not in this garden, is not kept.
                if (s.PhotoAskLeft <= 0d || Rabbits.Find(s, s.PhotoAskRabbit) == null) { s.PhotoAskRabbit = 0; s.PhotoAskLeft = 0d; }
                s.PhotoAsksToday = (int)Math.Max(0d, Math.Min(ReadNumber(ask, "today"), PhotoAsks.PerDay));
                s.PhotoAskDay = TryNumber(ask, "day", out double askDay) ? ToLong(askDay) : -1;
                s.PhotoAsksAnswered = Math.Max(0L, ToLong(ReadNumber(ask, "answered")));
                s.DailyPhotoAsksBase = Math.Max(0L, Math.Min(s.PhotoAsksAnswered, ToLong(ReadNumber(ask, "dailyBase"))));
            }

            // The album's milestones (#61). A save from before them has given none; only the milestones the game has count.
            if (root.TryGetValue("albumMilestones", out object milestonesObj) && milestonesObj is List<object> milestones)
                foreach (object item in milestones)
                    if (item is double m && Array.IndexOf(AlbumSets.Milestones, (int)m) >= 0 && m == Math.Floor(m)) s.AlbumMilestonesClaimed.Add((int)m);
            // Moon jars (#64). A save from before them holds none, and today's jars are all still to come.
            if (root.TryGetValue("moonJars", out object jarsObj) && jarsObj is Dictionary<string, object> jars)
            {
                s.MoonJars = (int)Math.Max(0d, Math.Min(ReadNumber(jars, "held"), MoonJars.Cap));
                s.MoonJarBuyDay = TryNumber(jars, "buyDay", out double jarBuyDay) ? ToLong(jarBuyDay) : -1;
                s.MoonJarsBoughtToday = (int)Math.Max(0d, Math.Min(ReadNumber(jars, "bought"), MoonJars.BuyPerDay));
                s.MoonJarFreeDay = TryNumber(jars, "freeDay", out double jarFreeDay) ? ToLong(jarFreeDay) : -1;
            }

            // The hex grid (#52). A save from before stands on the default table: every plot's station, place to rest and
            // decoration standing gets its tile, so nothing is lost. A tile claimed twice keeps its first claimant.
            s.Grid = new GardenGrid(GardenGrid.RadiusForPlots(s.Plots));
            if (root.TryGetValue("grid", out object gridObj) && gridObj is Dictionary<string, object> grid)
            {
                if (TryNumber(grid, "radius", out double radius)) s.Grid.Radius = (int)Math.Max(0d, Math.Min(radius, GardenGrid.MaxRadius));
                if (grid.TryGetValue("tiles", out object tilesObj) && tilesObj is List<object> tiles)
                    foreach (object raw in tiles)
                    {
                        if (!(raw is Dictionary<string, object> tile) || !TryNumber(tile, "q", out double q) || !TryNumber(tile, "r", out double r)) continue;
                        if (!(tile.TryGetValue("id", out object idObj) && idObj is string id) || !GardenGrid.Known(id) || s.Grid.TileOf(id) != null) continue;
                        if (Math.Abs(q) > GardenGrid.MaxRadius * 2 || Math.Abs(r) > GardenGrid.MaxRadius * 2 || s.Grid.OccupantAt((int)q, (int)r) != null) continue;
                        s.Grid.Place(id, (int)q, (int)r, true);
                    }
                // The stations put away (#54); Settle lets go of any that is not an open station with a plot.
                if (grid.TryGetValue("stored", out object storedObj) && storedObj is List<object> stored)
                    foreach (object raw in stored)
                        if (raw is string id && GardenGrid.Known(id)) s.Grid.Restore(id);
            }
            GardenGrid.Settle(defs, s);

            result.Status = LoadStatus.Ok;
            return result;
        }

        static int IndexOf(IReadOnlyList<StationDef> defs, string id)
        {
            for (int i = 0; i < defs.Count; i++)
                if (defs[i].Id == id) return i;
            return -1;
        }

        /// <summary>The top-level object of a JSON text, or null when it is not one. For the cloud envelope.</summary>
        public static Dictionary<string, object> ReadObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return new Reader(json).ReadDocument() as Dictionary<string, object>; }
            catch (FormatException) { return null; }
        }

        /// <summary>A string as JSON, quotes and all.</summary>
        public static string Quote(string value)
        {
            var sb = new StringBuilder((value ?? "").Length + 2);
            WriteString(sb, value);
            return sb.ToString();
        }

        public static bool TryNumber(Dictionary<string, object> obj, string key, out double value)
        {
            if (obj.TryGetValue(key, out object raw) && raw is double d && !double.IsNaN(d) && !double.IsInfinity(d))
            {
                value = d;
                return true;
            }
            value = 0d;
            return false;
        }

        static double ReadNumber(Dictionary<string, object> obj, string key)
        {
            TryNumber(obj, key, out double value);
            return value;
        }

        static BigNum ReadBig(Dictionary<string, object> obj, string key)
        {
            return BigNum.FromParts(ReadNumber(obj, key + "Mantissa"), ToLong(ReadNumber(obj, key + "Exponent")));
        }

        static void WriteBig(StringBuilder sb, string key, BigNum value)
        {
            sb.Append(",\"").Append(key).Append("Mantissa\":").Append(value.Mantissa.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"").Append(key).Append("Exponent\":").Append(value.Exponent.ToString(CultureInfo.InvariantCulture));
        }

        static long ToLong(double v)
        {
            if (double.IsNaN(v)) return 0L;
            if (v >= 9.2e18) return long.MaxValue;
            if (v <= -9.2e18) return long.MinValue;
            return (long)v;
        }

        static double Sane(double v) => (double.IsNaN(v) || double.IsInfinity(v)) ? 0d : v;

        static void WriteString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (char c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        /// <summary>
        /// A small strict JSON reader. Core cannot use Unity's JsonUtility, and the save has to
        /// tolerate unknown fields. Objects become Dictionary, arrays List, numbers double.
        /// Anything malformed throws FormatException.
        /// </summary>
        sealed class Reader
        {
            const int MaxDepth = 32;
            readonly string _text;
            int _pos;

            public Reader(string text) { _text = text; }

            public object ReadDocument()
            {
                object value = ReadValue(0);
                SkipSpace();
                if (_pos != _text.Length) throw new FormatException("Trailing text");
                return value;
            }

            object ReadValue(int depth)
            {
                if (depth > MaxDepth) throw new FormatException("Too deep");
                SkipSpace();
                if (_pos >= _text.Length) throw new FormatException("Unexpected end");
                char c = _text[_pos];
                if (c == '{') return ReadObject(depth);
                if (c == '[') return ReadArray(depth);
                if (c == '"') return ReadString();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                return ReadNumber();
            }

            Dictionary<string, object> ReadObject(int depth)
            {
                var obj = new Dictionary<string, object>();
                _pos++; // {
                SkipSpace();
                if (Peek() == '}') { _pos++; return obj; }
                while (true)
                {
                    SkipSpace();
                    if (Peek() != '"') throw new FormatException("Expected key");
                    string key = ReadString();
                    SkipSpace();
                    if (Peek() != ':') throw new FormatException("Expected ':'");
                    _pos++;
                    obj[key] = ReadValue(depth + 1);
                    SkipSpace();
                    char c = Peek();
                    _pos++;
                    if (c == '}') return obj;
                    if (c != ',') throw new FormatException("Expected ',' or '}'");
                }
            }

            List<object> ReadArray(int depth)
            {
                var list = new List<object>();
                _pos++; // [
                SkipSpace();
                if (Peek() == ']') { _pos++; return list; }
                while (true)
                {
                    list.Add(ReadValue(depth + 1));
                    SkipSpace();
                    char c = Peek();
                    _pos++;
                    if (c == ']') return list;
                    if (c != ',') throw new FormatException("Expected ',' or ']'");
                }
            }

            string ReadString()
            {
                var sb = new StringBuilder();
                _pos++; // opening quote
                while (true)
                {
                    if (_pos >= _text.Length) throw new FormatException("Unterminated string");
                    char c = _text[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }

                    if (_pos >= _text.Length) throw new FormatException("Unterminated escape");
                    char e = _text[_pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 > _text.Length) throw new FormatException("Bad \\u escape");
                            if (!int.TryParse(_text.Substring(_pos, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                                throw new FormatException("Bad \\u escape");
                            sb.Append((char)code);
                            _pos += 4;
                            break;
                        default: throw new FormatException("Bad escape");
                    }
                }
            }

            double ReadNumber()
            {
                int start = _pos;
                while (_pos < _text.Length)
                {
                    char c = _text[_pos];
                    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') _pos++;
                    else break;
                }
                if (_pos == start) throw new FormatException("Unexpected character");
                if (!double.TryParse(_text.Substring(start, _pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    throw new FormatException("Bad number");
                return value;
            }

            void Expect(string word)
            {
                if (_pos + word.Length > _text.Length || string.CompareOrdinal(_text, _pos, word, 0, word.Length) != 0)
                    throw new FormatException("Unexpected token");
                _pos += word.Length;
            }

            char Peek()
            {
                if (_pos >= _text.Length) throw new FormatException("Unexpected end");
                return _text[_pos];
            }

            void SkipSpace()
            {
                while (_pos < _text.Length)
                {
                    char c = _text[_pos];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _pos++;
                    else break;
                }
            }
        }
    }
}
