using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VuonTra.Core
{
    public enum LoadStatus
    {
        Ok,
        Empty,
        Corrupt,
        NewerVersion
    }

    public struct LoadResult
    {
        public LoadStatus Status;
        public GameState State;
    }

    public static class SaveCodec
    {
        const int LegacyStartingRadius = 3;
        public const long LegacyFirstDay = 0;

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
                sb.Append(",\"affection\":").Append(Math.Min(Rabbits.GiftHearts, Rabbits.HeartsOf(r)).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"bond\":").Append(r.Affection.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"petDay\":").Append(r.PetDay.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"dye\":").Append(r.Dye.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"kind\":");
                WriteString(sb, Rabbits.KindOf(r));
                sb.Append(",\"name\":");
                WriteString(sb, r.Name);
                sb.Append(",\"station\":");
                WriteString(sb, r.Station >= 0 && r.Station < defs.Count ? defs[r.Station].Id : "");
                sb.Append('}');
            }
            sb.Append("],\"plots\":").Append(s.Plots.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"traySlots\":").Append(s.TraySlots.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"goldenToday\":").Append(s.GoldenToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"photoDay\":").Append(s.PhotoDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"photosTaken\":").Append(s.PhotosTaken.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"tutorial\":").Append(s.TutorialStep.ToString(CultureInfo.InvariantCulture));
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
            sb.Append(",\"firstDay\":").Append(s.FirstDaySeen.ToString(CultureInfo.InvariantCulture));
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
            sb.Append(",\"pounding\":{\"day\":").Append(s.PoundingDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"plays\":").Append(s.PoundingPlays.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"star\":").Append(s.PoundingStarToday ? "true" : "false");
            sb.Append(",\"three\":").Append(s.PoundingThreeToday ? "true" : "false");
            sb.Append(",\"games\":").Append(s.PoundingGames.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"best\":").Append(s.PoundingBestStars.ToString(CultureInfo.InvariantCulture)).Append('}');
            sb.Append(",\"leaves\":{\"day\":").Append(s.Leaves.Day.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"plays\":").Append(s.Leaves.Plays.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"star\":").Append(s.Leaves.StarToday ? "true" : "false");
            sb.Append(",\"games\":").Append(s.Leaves.Games.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"best\":").Append(s.Leaves.BestStars.ToString(CultureInfo.InvariantCulture)).Append('}');
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
            sb.Append("],\"profile\":{\"name\":");
            WriteString(sb, s.Profile.GardenName);
            sb.Append(",\"avatar\":").Append(s.Profile.AvatarRabbitId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"linkRewarded\":").Append(s.Profile.LinkRewarded ? "true" : "false");
            sb.Append(",\"code\":");
            WriteString(sb, s.Profile.Code);
            sb.Append("}");
            sb.Append(",\"view\":{\"x\":").Append(Sane(s.ViewX).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"y\":").Append(Sane(s.ViewY).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"zoom\":").Append(Sane(s.ViewZoom).ToString("R", CultureInfo.InvariantCulture));
            sb.Append('}');
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
            sb.Append(",\"photoAsk\":{\"clock\":").Append(Sane(s.PhotoAskClock).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"left\":").Append(Sane(s.PhotoAskLeft).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"rabbit\":").Append(s.PhotoAskRabbit.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"today\":").Append(s.PhotoAsksToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"day\":").Append(s.PhotoAskDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"answered\":").Append(s.PhotoAsksAnswered.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dailyBase\":").Append(s.DailyPhotoAsksBase.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            sb.Append(",\"albumMilestones\":[");
            first = true;
            foreach (int milestone in s.AlbumMilestonesClaimed)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append(milestone.ToString(CultureInfo.InvariantCulture));
            }
            sb.Append(']');
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
            sb.Append(",\"moonJars\":{\"held\":").Append(s.MoonJars.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"buyDay\":").Append(s.MoonJarBuyDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"bought\":").Append(s.MoonJarsBoughtToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"freeDay\":").Append(s.MoonJarFreeDay.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            sb.Append(",\"ads\":{\"day\":").Append(s.AdsToday.Day.ToString(CultureInfo.InvariantCulture)).Append(",\"used\":{");
            {
                bool firstAd = true;
                foreach (string key in Ads.Keys())
                {
                    if (!s.AdsToday.Used.TryGetValue(key, out int used) || used <= 0) continue;
                    if (!firstAd) sb.Append(',');
                    firstAd = false;
                    WriteString(sb, key);
                    sb.Append(':').Append(used.ToString(CultureInfo.InvariantCulture));
                }
            }
            sb.Append("},\"passUntil\":").Append(s.NoAdsUntilDay.ToString(CultureInfo.InvariantCulture)).Append('}');
            sb.Append(",\"grid\":{\"map\":").Append(s.Grid.Segments.ToString(CultureInfo.InvariantCulture)).Append(",\"tiles\":[");
            {
                var ids = new List<string>();
                foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants) ids.Add(o.Key);
                ids.Sort(string.CompareOrdinal);
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
            sb.Append('}');
            sb.Append(",\"codesUsed\":[");
            first = true;
            foreach (string code in s.CodesUsed)
            {
                if (!first) sb.Append(',');
                first = false;
                WriteString(sb, code);
            }
            sb.Append("],\"mail\":{\"day\":").Append(s.MailDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"letters\":[");
            for (int i = 0; i < s.Letters.Count; i++)
            {
                Letter letter = s.Letters[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":");
                WriteString(sb, letter.Id);
                sb.Append(",\"kind\":").Append(((int)letter.Kind).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"season\":").Append(((int)letter.Season).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"day\":").Append(letter.Day.ToString(CultureInfo.InvariantCulture));
                if (letter.Kind == LetterKind.WeekChest) sb.Append(",\"chest\":").Append(letter.Chest.ToString(CultureInfo.InvariantCulture));
                if (letter.Kind == LetterKind.MonthPass)
                {
                    sb.Append(",\"month\":").Append(letter.Month.ToString(CultureInfo.InvariantCulture));
                    sb.Append(",\"premium\":").Append(letter.Premium ? "true" : "false");
                    sb.Append(",\"tiers\":").Append(letter.Tiers.ToString(CultureInfo.InvariantCulture));
                    WriteBig(sb, "coins", letter.Coins);
                }
                sb.Append(",\"golden\":").Append(letter.Golden.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"clovers\":").Append(letter.Clovers.ToString(CultureInfo.InvariantCulture));
                if (letter.Kind == LetterKind.Newcomer)
                {
                    sb.Append(",\"jars\":").Append(letter.Jars.ToString(CultureInfo.InvariantCulture));
                    sb.Append(",\"purple\":").Append(letter.Purple.ToString(CultureInfo.InvariantCulture));
                }
                sb.Append('}');
            }
            sb.Append("]}");
            sb.Append(",\"week\":{\"index\":").Append(s.WeekIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"points\":").Append(s.WeekPoints.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"chests\":").Append(s.WeekChestsClaimed.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"boxDay\":").Append(s.BoxDay.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            sb.Append(",\"monthPass\":{\"month\":").Append(s.PassMonth.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"points\":").Append(s.PassPoints.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"free\":").Append(s.PassFreeClaimed.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"premium\":").Append(s.PassPremiumClaimed.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"bought\":").Append(s.PassBoughtMonth.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"lanterns\":").Append(s.MonthLanterns.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"frames\":").Append(s.MonthFrames.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"frame\":").Append(s.PhotoFrame.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            sb.Append(",\"frog\":{\"day\":").Append(s.FrogDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"showers\":").Append(s.FrogShowers.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"today\":").Append(s.FrogsToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"met\":").Append(s.FrogsMet.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"left\":").Append(Sane(s.FrogLeft).ToString("R", CultureInfo.InvariantCulture));
            sb.Append('}');
            sb.Append(",\"fishing\":{\"baits\":").Append(s.Baits.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"baitAt\":").Append(s.BaitAt.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"casts\":").Append(s.Casts.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"basket\":[");
            for (int i = 0; i < Fishing.Kinds; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append((s.Fish != null && i < s.Fish.Length ? s.Fish[i] : 0).ToString(CultureInfo.InvariantCulture));
            }
            sb.Append("],\"caught\":").Append(s.FishCaught.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"fed\":").Append(s.FishFed.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            sb.Append(",\"recipes\":{\"leaves\":").Append(s.RecipeLeaves.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"rest\":").Append(Sane(s.RestLeafSeconds).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"dishes\":[");
            for (int i = 0; i < defs.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":");
                WriteString(sb, defs[i].Id);
                sb.Append(",\"levels\":[");
                for (int d = 0; d < Recipes.Dishes; d++)
                {
                    if (d > 0) sb.Append(',');
                    sb.Append(Recipes.DishLevel(s, i, d).ToString(CultureInfo.InvariantCulture));
                }
                sb.Append("]}");
            }
            sb.Append("]}");
            sb.Append(",\"tally\":{\"pets\":").Append(s.Pets.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"boosts\":").Append(s.Boosts.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"moves\":").Append(s.LayoutMoves.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"merges\":").Append(s.Merges.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"cloverCalls\":").Append(s.CloverCalls.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"goldenCalls\":").Append(s.GoldenCalls.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"jars\":").Append(s.JarsOpened.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"gifts\":").Append(s.DailyGifts.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dailyToday\":").Append(s.DailyQuestsToday.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"bestFish\":").Append(s.BestFish.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            sb.Append(",\"newcomer\":{\"claimed\":").Append(s.Newcomer.Claimed.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"days\":").Append(s.Newcomer.DaysPaid.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"closed\":").Append(s.Newcomer.Closed ? "true" : "false");
            sb.Append('}');
            sb.Append(",\"offers\":{\"clock\":").Append(s.OfferClock.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"mark\":").Append(Sane(s.OfferPlayMark).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"list\":[");
            for (int i = 0; i < s.OfferRecords.Count; i++)
            {
                OfferRecord offer = s.OfferRecords[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":");
                WriteString(sb, offer.Id);
                sb.Append(",\"start\":").Append(offer.Start.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"shown\":").Append(offer.Shown ? "true" : "false");
                sb.Append(",\"bought\":").Append(offer.Bought ? "true" : "false");
                sb.Append(",\"closed\":").Append(offer.Closed ? "true" : "false");
                sb.Append('}');
            }
            sb.Append("]}");
            sb.Append(",\"review\":{\"asks\":").Append(s.Review.Asks.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"last\":").Append(s.Review.LastAskDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"liked\":").Append(s.Review.Liked ? "true" : "false");
            sb.Append('}');
            sb.Append(",\"welcomeBack\":{\"seen\":").Append(s.WelcomeBack.SeenDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"start\":").Append(s.WelcomeBack.StartDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"away\":").Append(s.WelcomeBack.AwayDays.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"checkIns\":").Append(s.WelcomeBack.CheckIns.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"checkInDay\":").Append(s.WelcomeBack.CheckInDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"cardSeen\":").Append(s.WelcomeBack.CardSeen ? "true" : "false");
            sb.Append('}');
            sb.Append(",\"trunk\":{\"window\":").Append(s.Trunk.Window.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"items\":[");
            for (int i = 0; i < s.Trunk.Codes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                WriteString(sb, s.Trunk.Codes[i]);
            }
            sb.Append("],\"sale\":").Append(s.Trunk.Sale.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"bought\":").Append(s.Trunk.Bought.ToString(CultureInfo.InvariantCulture));
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
                    if (index < 0) continue;

                    TryNumber(entry, "level", out double level);
                    TryNumber(entry, "progress", out double progress);
                    s.Levels[index] = level > 0d ? (int)Math.Min(level, int.MaxValue) : 0;
                    s.Progress[index] = (progress >= 0d && progress < 1d) ? progress : 0d;
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
            s.SpendShare = 1d;
            if (TryNumber(root, "spendShare", out double share) && (share == 0.5d || share == 0.25d)) s.SpendShare = share;
            TryNumber(root, "lastSeen", out double lastSeen);
            s.LastSeenUnixSeconds = ToLong(lastSeen);
            s.HintSeen = root.TryGetValue("hintSeen", out object hint) && hint is bool b && b;

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

            if (root.TryGetValue("rabbits", out object rabbitsObj) && rabbitsObj is List<object> rabbits)
            {
                var taken = new HashSet<int>();
                var ids = new HashSet<long>();
                foreach (object item in rabbits)
                {
                    if (!(item is Dictionary<string, object> entry)) continue;
                    long rabbitId = ToLong(ReadNumber(entry, "id"));
                    if (rabbitId <= 0 || !ids.Add(rabbitId)) continue;
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
                        if (index >= 0 && s.Levels[index] > 0 && taken.Add(index)) rabbit.Station = index;
                    }
                    rabbit.Level = (int)Math.Max(1d, Math.Min(ReadNumber(entry, "level"), Rabbits.LevelCaps[rabbit.Tier]));
                    rabbit.Affection = TryNumber(entry, "bond", out double bond)
                        ? (int)Math.Max(0d, Math.Min(bond, Rabbits.AffectionMax))
                        : Rabbits.PointsFor((int)Math.Max(0d, Math.Min(ReadNumber(entry, "affection"), Rabbits.GiftHearts)));
                    rabbit.PetDay = TryNumber(entry, "petDay", out double petDay) && petDay >= 0d ? ToLong(petDay) : -1;
                    rabbit.Dye = TryNumber(entry, "dye", out double dye) && dye >= 0d && dye < Shop.Dyes.Length ? (int)dye : -1;
                    rabbit.Kind = entry.TryGetValue("kind", out object kindObj) && kindObj is string kindId ? kindId : null;
                    rabbit.Kind = Rabbits.KindOf(rabbit);
                    s.Rabbits.Add(rabbit);
                    if (rabbitId > s.NextRabbitId) s.NextRabbitId = rabbitId;
                }
            }
            s.RestSpots = root.ContainsKey("restSpots")
                ? (int)Math.Max(RestSpots.Starting, Math.Min(ReadNumber(root, "restSpots"), RestSpots.Max))
                : RestSpots.Starting;

            if (root.TryGetValue("seenKinds", out object seenObj) && seenObj is List<object> seen)
                foreach (object item in seen)
                    if (item is string seenId && RabbitKinds.Find(seenId) != null) s.SeenKinds.Add(seenId);
            foreach (Rabbit r in s.Rabbits) s.SeenKinds.Add(Rabbits.KindOf(r));
            if (root.TryGetValue("freshKinds", out object freshObj) && freshObj is List<object> fresh)
                foreach (object item in fresh)
                    if (item is string kind && s.SeenKinds.Contains(kind)) s.FreshKinds.Add(kind);

            long savedNext = ToLong(ReadNumber(root, "nextRabbitId"));
            if (savedNext > s.NextRabbitId) s.NextRabbitId = savedNext;

            int highestOpen = 0;
            for (int i = 0; i < s.Levels.Length; i++)
                if (s.Levels[i] > 0) highestOpen = i + 1;
            int plots = TryNumber(root, "plots", out double savedPlots) ? (int)Math.Min(savedPlots, defs.Count) : 0;
            s.Plots = Math.Min(defs.Count, Math.Max(Math.Max(plots, highestOpen), Math.Min(defs.Count, Plots.Starting)));
            s.Plots = Math.Max(s.Plots, Expansion.PlotsForSegments(defs, Expansion.WavesTaken(s.Plots)));

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
                    if (wornBy > 0 && Rabbits.Find(s, wornBy) != null && slots.Add(wornBy + ":" + (int)item.Slot)) item.WornBy = wornBy;
                    s.Items.Add(item);
                    if (itemId > s.NextItemId) s.NextItemId = itemId;
                }
            }
            long savedNextItem = ToLong(ReadNumber(root, "nextItemId"));
            if (savedNextItem > s.NextItemId) s.NextItemId = savedNextItem;

            s.RestSpots = Math.Max(s.RestSpots, Math.Min(RestSpots.Max, RestSpots.Starting + Expansion.WavesTaken(s.Plots)));

            double xp = TryNumber(root, "gardenXp", out double savedXp) && savedXp >= 0d ? savedXp : Garden.BackfillXp(defs, s);
            s.GardenXp = Math.Max(xp, Garden.XpForLevel(Expansion.LevelForPlots(defs, s.Plots)));

            s.TraySlots = TryNumber(root, "traySlots", out double traySlots)
                ? (int)Math.Max(Tray.StartingSlots, Math.Min(traySlots, Tray.MaxSlots))
                : Tray.StartingSlots;
            s.GoldenToday = TryNumber(root, "goldenToday", out double goldenToday) ? (int)Math.Max(0d, Math.Min(goldenToday, Tray.GoldenPerDay)) : 0;
            s.PhotoDay = TryNumber(root, "photoDay", out double photoDay) ? ToLong(photoDay) : -1;
            s.PhotosTaken = TryNumber(root, "photosTaken", out double photosTaken) ? Math.Max(0, ToLong(photosTaken)) : 0;
            if (TryNumber(root, "tutorial", out double tutorial)) s.TutorialStep = (int)Math.Max(0, Math.Min(tutorial, Tutorial.Steps));
            else s.TutorialStep = s.HintSeen || (s.Levels.Length > 0 && s.Levels[0] > 1) || s.TotalTaps > 0 ? Tutorial.Steps : 0;
            s.TutorialCloverGiven = root.TryGetValue("tutorialClover", out object tutClover) && tutClover is bool tc && tc;
            s.GoldenCarrots = TryNumber(root, "golden", out double goldenCarrots) ? (int)Math.Max(0d, Math.Min(goldenCarrots, int.MaxValue)) : 0;
            s.GiftDay = TryNumber(root, "giftDay", out double giftDay) ? ToLong(giftDay) : -1;
            s.GiftStreak = TryNumber(root, "giftStreak", out double giftStreak) ? (int)Math.Max(0d, Math.Min(giftStreak, 100000)) : 0;
            s.GiftBigDay = TryNumber(root, "giftBig", out double giftBig) ? Math.Max(-1L, ToLong(giftBig)) : -1;
            s.GiftBigGolden = root.TryGetValue("giftBigGolden", out object bigGolden) && bigGolden is bool bg && bg;
            s.VisitorBoughtDay = TryNumber(root, "visitorBought", out double visitorBought) ? ToLong(visitorBought) : -1;
            s.VisitorSeenDay = TryNumber(root, "visitorSeen", out double visitorSeen) ? ToLong(visitorSeen) : -1;
            s.PhotosWithGoldRabbit = TryNumber(root, "photosGold", out double photosGold) ? Math.Max(0, ToLong(photosGold)) : 0;
            s.PhotosWithFullRest = TryNumber(root, "photosRest", out double photosRest) ? Math.Max(0, ToLong(photosRest)) : 0;
            s.PhotosWithSeasonRabbit = TryNumber(root, "photosSeason", out double photosSeason) ? Math.Max(0, ToLong(photosSeason)) : 0;
            s.MaxDaySeen = TryNumber(root, "maxDay", out double maxDay) ? ToLong(maxDay) : -1;
            s.FirstDaySeen = TryNumber(root, "firstDay", out double firstDay) ? ToLong(firstDay) : LegacyFirstDay;
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
            if (root.TryGetValue("pounding", out object poundObj) && poundObj is Dictionary<string, object> pound)
            {
                s.PoundingDay = TryNumber(pound, "day", out double pDay) ? ToLong(pDay) : -1;
                s.PoundingPlays = (int)Math.Max(0d, Math.Min(ReadNumber(pound, "plays"), Pounding.PlaysPerDay));
                s.PoundingStarToday = pound.TryGetValue("star", out object pStar) && pStar is bool ps && ps;
                s.PoundingThreeToday = pound.TryGetValue("three", out object pThree) && pThree is bool pt && pt;
                s.PoundingGames = Math.Max(0, ToLong(ReadNumber(pound, "games")));
                s.PoundingBestStars = (int)Math.Max(0d, Math.Min(ReadNumber(pound, "best"), 3));
            }
            if (root.TryGetValue("leaves", out object leavesObj) && leavesObj is Dictionary<string, object> leaves)
            {
                s.Leaves.Day = TryNumber(leaves, "day", out double lDay) ? ToLong(lDay) : -1;
                s.Leaves.Plays = (int)Math.Max(0d, Math.Min(ReadNumber(leaves, "plays"), Leaves.PlaysPerDay));
                s.Leaves.StarToday = leaves.TryGetValue("star", out object lStar) && lStar is bool ls && ls;
                s.Leaves.Games = Math.Max(0, ToLong(ReadNumber(leaves, "games")));
                s.Leaves.BestStars = (int)Math.Max(0d, Math.Min(ReadNumber(leaves, "best"), 3));
            }
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

            if (root.TryGetValue("profile", out object profileObj) && profileObj is Dictionary<string, object> profile)
            {
                s.Profile.GardenName = profile.TryGetValue("name", out object nameObj) && nameObj is string gardenName ? (Profile.CleanName(gardenName) ?? "") : "";
                long avatar = ToLong(ReadNumber(profile, "avatar"));
                s.Profile.AvatarRabbitId = avatar > 0 && Rabbits.Find(s, avatar) != null ? avatar : 0;
                if (profile.TryGetValue("code", out object codeObj) && codeObj is string code && Profile.IsCode(code)) s.Profile.Code = code;
                s.Profile.LinkRewarded = profile.TryGetValue("linkRewarded", out object rewardedObj) && rewardedObj is bool rewarded && rewarded;
            }
            if (!Profile.IsCode(s.Profile.Code)) s.Profile.Code = Profile.NewCode();

            if (root.TryGetValue("view", out object viewObj) && viewObj is Dictionary<string, object> view
                && TryNumber(view, "zoom", out double zoom) && zoom >= 0.3d && zoom <= 2.5d)
            {
                s.ViewZoom = zoom;
                s.ViewX = ReadNumber(view, "x");
                s.ViewY = ReadNumber(view, "y");
            }

            if (root.TryGetValue("decor", out object decorObj) && decorObj is Dictionary<string, object> decor)
            {
                if (decor.TryGetValue("owned", out object ownedObj) && ownedObj is Dictionary<string, object> owned)
                    for (int i = 0; i < Decor.Kinds.Length; i++)
                        s.DecorOwned[i] = (int)Math.Max(0d, Math.Min(ReadNumber(owned, Decor.Kinds[i].Id), Decor.Spots));
                if (decor.TryGetValue("spots", out object spotsObj) && spotsObj is List<object> spots)
                    for (int i = 0; i < Decor.Spots && i < spots.Count; i++)
                    {
                        string kind = spots[i] as string;
                        if (kind == null || Decor.IndexOf(kind) < 0 || Decor.Available(s, kind) <= 0) continue;
                        s.DecorSpots[i] = kind;
                    }
                double lantern = ReadNumber(decor, "lantern");
                s.LanternColor = lantern >= 0d && lantern < Decor.LanternColors.Length + MonthPass.Months ? (int)lantern : 0;
            }
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
                    if (order.Lines.Count > 0 && order.Left > 0d) s.Order = order;
                    if (order.Id > s.NextOrderId) s.NextOrderId = order.Id;
                }
            }

            if (root.TryGetValue("photoAsk", out object askObj) && askObj is Dictionary<string, object> ask)
            {
                s.PhotoAskClock = TryNumber(ask, "clock", out double askClock) ? Math.Max(0d, Math.Min(PhotoAsks.Interval, askClock)) : 0d;
                s.PhotoAskLeft = Math.Max(0d, Math.Min(PhotoAsks.StaySeconds, ReadNumber(ask, "left")));
                s.PhotoAskRabbit = Math.Max(0L, ToLong(ReadNumber(ask, "rabbit")));
                if (s.PhotoAskLeft <= 0d || Rabbits.Find(s, s.PhotoAskRabbit) == null) { s.PhotoAskRabbit = 0; s.PhotoAskLeft = 0d; }
                s.PhotoAsksToday = (int)Math.Max(0d, Math.Min(ReadNumber(ask, "today"), PhotoAsks.PerDay));
                s.PhotoAskDay = TryNumber(ask, "day", out double askDay) ? ToLong(askDay) : -1;
                s.PhotoAsksAnswered = Math.Max(0L, ToLong(ReadNumber(ask, "answered")));
                s.DailyPhotoAsksBase = Math.Max(0L, Math.Min(s.PhotoAsksAnswered, ToLong(ReadNumber(ask, "dailyBase"))));
            }

            if (root.TryGetValue("albumMilestones", out object milestonesObj) && milestonesObj is List<object> milestones)
                foreach (object item in milestones)
                    if (item is double m && Array.IndexOf(AlbumSets.Milestones, (int)m) >= 0 && m == Math.Floor(m)) s.AlbumMilestonesClaimed.Add((int)m);
            if (root.TryGetValue("moonJars", out object jarsObj) && jarsObj is Dictionary<string, object> jars)
            {
                s.MoonJars = (int)Math.Max(0d, Math.Min(ReadNumber(jars, "held"), MoonJars.Cap));
                s.MoonJarBuyDay = TryNumber(jars, "buyDay", out double jarBuyDay) ? ToLong(jarBuyDay) : -1;
                s.MoonJarsBoughtToday = (int)Math.Max(0d, Math.Min(ReadNumber(jars, "bought"), MoonJars.BuyPerDay));
                s.MoonJarFreeDay = TryNumber(jars, "freeDay", out double jarFreeDay) ? ToLong(jarFreeDay) : -1;
            }
            if (root.TryGetValue("codesUsed", out object codesObj) && codesObj is List<object> codes)
                foreach (object item in codes)
                    if (item is string code && code.Length > 0) s.CodesUsed.Add(code);
            s.MailDay = s.MaxDaySeen;
            if (root.TryGetValue("mail", out object mailObj) && mailObj is Dictionary<string, object> mail)
            {
                s.MailDay = TryNumber(mail, "day", out double mailDay) ? Math.Max(-1L, ToLong(mailDay)) : s.MaxDaySeen;
                if (mail.TryGetValue("letters", out object lettersObj) && lettersObj is List<object> letters)
                    foreach (object raw in letters)
                    {
                        if (!(raw is Dictionary<string, object> entry) || s.Letters.Count >= Mailbox.MaxLetters) continue;
                        if (!(entry.TryGetValue("id", out object letterIdObj) && letterIdObj is string letterId && letterId.Length > 0)) continue;
                        if (s.Letters.Exists(l => l.Id == letterId)) continue;
                        double letterKind = ReadNumber(entry, "kind"), letterSeason = ReadNumber(entry, "season"), letterChest = ReadNumber(entry, "chest");
                        if (letterKind != (int)LetterKind.MakeUp && letterKind != (int)LetterKind.Season && letterKind != (int)LetterKind.WeekChest && letterKind != (int)LetterKind.Newcomer && letterKind != (int)LetterKind.MonthPass) continue;
                        bool chestLetter = letterKind == (int)LetterKind.WeekChest;
                        if (chestLetter && (letterChest < 0d || letterChest >= QuestChests.Chests.Length || letterChest != Math.Floor(letterChest))) continue;
                        bool gift = letterKind == (int)LetterKind.Newcomer;
                        bool monthLetter = letterKind == (int)LetterKind.MonthPass;
                        int letterTiers = monthLetter ? (int)Math.Max(0d, Math.Min(ReadNumber(entry, "tiers"), MonthPass.AllTiers)) & MonthPass.AllTiers : 0;
                        if (monthLetter && letterTiers == 0) continue;
                        s.Letters.Add(new Letter
                        {
                            Id = letterId,
                            Kind = (LetterKind)(int)letterKind,
                            Season = letterSeason >= 0d && letterSeason <= (double)Season.Christmas && letterSeason == Math.Floor(letterSeason) ? (Season)(int)letterSeason : Season.None,
                            Day = ToLong(ReadNumber(entry, "day")),
                            Chest = chestLetter ? (int)letterChest : 0,
                            Month = monthLetter ? ToLong(ReadNumber(entry, "month")) : 0,
                            Premium = monthLetter && entry.TryGetValue("premium", out object premiumObj) && premiumObj is bool premiumLetter && premiumLetter,
                            Tiers = letterTiers,
                            Coins = monthLetter ? ReadBig(entry, "coins") : BigNum.Zero,
                            GiftGolden = gift ? Small(ReadNumber(entry, "golden"), 1000) : 0,
                            GiftClovers = gift ? Small(ReadNumber(entry, "clovers"), 1000) : 0,
                            GiftJars = gift ? Small(ReadNumber(entry, "jars"), MoonJars.Cap) : 0,
                            GiftPurple = gift ? Small(ReadNumber(entry, "purple"), Newcomer.Days) : 0,
                        });
                    }
            }

            if (root.TryGetValue("week", out object weekObj) && weekObj is Dictionary<string, object> week)
            {
                s.WeekIndex = TryNumber(week, "index", out double weekIndex) ? Math.Max(-1L, ToLong(weekIndex)) : -1;
                s.WeekPoints = (int)Math.Max(0d, Math.Min(ReadNumber(week, "points"), QuestChests.MaxPoints));
                s.WeekChestsClaimed = (int)Math.Max(0d, Math.Min(ReadNumber(week, "chests"), int.MaxValue)) & ((1 << QuestChests.Chests.Length) - 1);
                s.BoxDay = TryNumber(week, "boxDay", out double boxDay) ? Math.Max(-1L, ToLong(boxDay)) : -1;
            }

            if (root.TryGetValue("monthPass", out object monthObj) && monthObj is Dictionary<string, object> month)
            {
                s.PassMonth = TryNumber(month, "month", out double passMonth) ? Math.Max(-1L, ToLong(passMonth)) : -1;
                s.PassPoints = (int)Math.Max(0d, Math.Min(ReadNumber(month, "points"), MonthPass.MaxPoints));
                s.PassFreeClaimed = MonthMask(month, "free", MonthPass.AllTiers);
                s.PassPremiumClaimed = MonthMask(month, "premium", MonthPass.AllTiers);
                s.PassBoughtMonth = TryNumber(month, "bought", out double passBought) ? Math.Max(-1L, ToLong(passBought)) : -1;
                s.MonthLanterns = MonthMask(month, "lanterns", MonthPass.AllMonths);
                s.MonthFrames = MonthMask(month, "frames", MonthPass.AllMonths);
                double frame = ReadNumber(month, "frame");
                s.PhotoFrame = frame == Math.Floor(frame) && MonthPass.OwnsFrame(s, (int)Math.Max(-1d, Math.Min(frame, MonthPass.Months))) ? (int)frame : MonthPass.PlainFrame;
            }
            if (MonthPass.LanternMonth(s.LanternColor) >= 0 && !MonthPass.OwnsLantern(s, MonthPass.LanternMonth(s.LanternColor))) s.LanternColor = 0;

            if (root.TryGetValue("ads", out object adsObj) && adsObj is Dictionary<string, object> ads)
            {
                s.AdsToday.Day = TryNumber(ads, "day", out double adsDay) ? ToLong(adsDay) : -1;
                if (ads.TryGetValue("used", out object usedObj) && usedObj is Dictionary<string, object> used)
                    foreach (string key in Ads.Keys())
                    {
                        int n = (int)Math.Max(0d, Math.Min(ReadNumber(used, key), Ads.CapOfKey(key)));
                        if (n > 0) s.AdsToday.Used[key] = n;
                    }
                s.NoAdsUntilDay = TryNumber(ads, "passUntil", out double passUntil) ? Math.Max(-1L, ToLong(passUntil)) : -1;
            }

            s.Grid = new GardenGrid(GardenGrid.SegmentsForPlots(s.Plots));
            if (root.TryGetValue("grid", out object gridObj) && gridObj is Dictionary<string, object> grid)
            {
                if (TryNumber(grid, "map", out double map))
                {
                    s.Grid.Segments = (int)Math.Max(0d, Math.Min(map, GardenGrid.MaxSegments));
                    if (grid.TryGetValue("tiles", out object tilesObj) && tilesObj is List<object> tiles)
                        foreach (object raw in tiles)
                        {
                            if (!(raw is Dictionary<string, object> tile) || !TryNumber(tile, "q", out double q) || !TryNumber(tile, "r", out double r)) continue;
                            if (!(tile.TryGetValue("id", out object idObj) && idObj is string id) || !GardenGrid.Known(id) || s.Grid.TileOf(id) != null) continue;
                            if (Math.Abs(q) > 1000d || Math.Abs(r) > 1000d) continue;
                            var at = new Hex((int)q, (int)r);
                            if (GardenGrid.SegmentOf(at) < 0 || s.Grid.OccupantAt(at.Q, at.R) != null) continue;
                            s.Grid.Place(id, at.Q, at.R, true);
                        }
                }
                else if (LegacySegments(grid, out double legacy))
                {
                    s.Grid.Segments = (int)Math.Max(0d, Math.Min(legacy, GardenGrid.MaxSegments));
                    s.ViewX = s.ViewY = s.ViewZoom = 0d;
                }
                if (grid.TryGetValue("stored", out object storedObj) && storedObj is List<object> stored)
                    foreach (object raw in stored)
                        if (raw is string id && GardenGrid.Known(id)) s.Grid.Restore(id);
            }
            GardenGrid.Settle(defs, s);

            if (root.TryGetValue("frog", out object frogObj) && frogObj is Dictionary<string, object> frog)
            {
                s.FrogDay = TryNumber(frog, "day", out double frogDay) ? ToLong(frogDay) : -1;
                s.FrogShowers = (int)Math.Max(0d, Math.Min(ReadNumber(frog, "showers"), (1 << Weather.ShowersPerDay) - 1));
                s.FrogsToday = (int)Math.Max(0d, Math.Min(ReadNumber(frog, "today"), FrogRabbit.PerDay));
                s.FrogsMet = Math.Max(0L, ToLong(ReadNumber(frog, "met")));
                s.FrogLeft = Math.Max(0d, Math.Min(FrogRabbit.StaySeconds, ReadNumber(frog, "left")));
            }

            if (root.TryGetValue("fishing", out object fishingObj) && fishingObj is Dictionary<string, object> fishing)
            {
                s.Baits = (int)Math.Max(0d, Math.Min(ReadNumber(fishing, "baits"), Fishing.BaitCap));
                s.BaitAt = TryNumber(fishing, "baitAt", out double baitAt) && baitAt >= 0d ? ToLong(baitAt) : -1;
                s.Casts = Math.Max(0L, ToLong(ReadNumber(fishing, "casts")));
                if (fishing.TryGetValue("basket", out object basketObj) && basketObj is List<object> basket)
                    for (int i = 0; i < Fishing.Kinds && i < basket.Count; i++)
                        if (basket[i] is double n && n > 0d) s.Fish[i] = (int)Math.Min(n, int.MaxValue);
                s.FishCaught = Math.Max(0L, ToLong(ReadNumber(fishing, "caught")));
                s.FishFed = Math.Max(0L, ToLong(ReadNumber(fishing, "fed")));
            }
            if (root.TryGetValue("offers", out object offersObj) && offersObj is Dictionary<string, object> offers)
            {
                s.OfferClock = TryNumber(offers, "clock", out double offerClock) && offerClock >= 0d ? ToLong(offerClock) : -1;
                s.OfferPlayMark = Math.Max(0d, Math.Min(Sane(ReadNumber(offers, "mark")), s.PlaySeconds));
                if (offers.TryGetValue("list", out object listObj) && listObj is List<object> list)
                    foreach (object raw in list)
                    {
                        if (!(raw is Dictionary<string, object> o) || !(o.TryGetValue("id", out object idObj) && idObj is string id)) continue;
                        if (Offers.Find(id) == null || Offers.RecordOf(s, id) != null) continue;
                        s.OfferRecords.Add(new OfferRecord
                        {
                            Id = id,
                            Start = Math.Max(0L, ToLong(ReadNumber(o, "start"))),
                            Shown = o.TryGetValue("shown", out object shown) && shown is bool sh && sh,
                            Bought = o.TryGetValue("bought", out object bought) && bought is bool bo && bo,
                            Closed = o.TryGetValue("closed", out object closed) && closed is bool cl && cl,
                        });
                    }
            }

            if (root.TryGetValue("recipes", out object recipesObj) && recipesObj is Dictionary<string, object> recipes)
            {
                s.RecipeLeaves = (int)Math.Max(0d, Math.Min(ReadNumber(recipes, "leaves"), int.MaxValue));
                s.RestLeafSeconds = Math.Max(0d, Math.Min(ReadNumber(recipes, "rest"), Recipes.RestSecondsPerLeaf));
                if (recipes.TryGetValue("dishes", out object dishesObj) && dishesObj is List<object> dishes)
                    foreach (object item in dishes)
                    {
                        if (!(item is Dictionary<string, object> entry) || !(entry.TryGetValue("id", out object idObj) && idObj is string id)) continue;
                        int index = IndexOf(defs, id);
                        if (index < 0 || !(entry.TryGetValue("levels", out object levelsObj) && levelsObj is List<object> levels)) continue;
                        for (int d = 0; d < Recipes.Dishes && d < levels.Count; d++)
                            if (levels[d] is double level) Recipes.SetLevel(s, index, d, (int)Math.Max(1d, Math.Min(level, Recipes.MaxLevel)));
                    }
            }

            if (root.TryGetValue("review", out object reviewObj) && reviewObj is Dictionary<string, object> review)
            {
                s.Review.Asks = (int)Math.Max(0d, Math.Min(ReadNumber(review, "asks"), ReviewAsk.MaxAsks));
                s.Review.LastAskDay = TryNumber(review, "last", out double lastAsk) ? Math.Max(-1L, ToLong(lastAsk)) : -1;
                s.Review.Liked = review.TryGetValue("liked", out object liked) && liked is bool likedFlag && likedFlag;
            }

            if (root.TryGetValue("welcomeBack", out object welcomeObj) && welcomeObj is Dictionary<string, object> welcome)
            {
                s.WelcomeBack.SeenDay = TryNumber(welcome, "seen", out double welcomeSeen) ? Math.Max(-1L, ToLong(welcomeSeen)) : s.MaxDaySeen;
                s.WelcomeBack.StartDay = TryNumber(welcome, "start", out double start) ? Math.Max(-1L, ToLong(start)) : -1;
                s.WelcomeBack.AwayDays = Small(ReadNumber(welcome, "away"), WelcomeBack.MaxAwayDays);
                s.WelcomeBack.CheckIns = Small(ReadNumber(welcome, "checkIns"), WelcomeBack.Days);
                s.WelcomeBack.CheckInDay = TryNumber(welcome, "checkInDay", out double checkInDay) ? Math.Max(-1L, ToLong(checkInDay)) : -1;
                s.WelcomeBack.CardSeen = !(welcome.TryGetValue("cardSeen", out object cardSeenObj) && cardSeenObj is bool cardSeen) || cardSeen;
            }
            else s.WelcomeBack.SeenDay = s.MaxDaySeen;

            if (root.TryGetValue("trunk", out object trunkObj) && trunkObj is Dictionary<string, object> trunk)
                ReadTrunk(s.Trunk, trunk);

            if (root.TryGetValue("tally", out object tallyObj) && tallyObj is Dictionary<string, object> tally)
            {
                s.Pets = Math.Max(0L, ToLong(ReadNumber(tally, "pets")));
                s.Boosts = Math.Max(0L, ToLong(ReadNumber(tally, "boosts")));
                s.LayoutMoves = Math.Max(0L, ToLong(ReadNumber(tally, "moves")));
                s.Merges = Math.Max(0L, ToLong(ReadNumber(tally, "merges")));
                s.CloverCalls = Math.Max(0L, ToLong(ReadNumber(tally, "cloverCalls")));
                s.GoldenCalls = Math.Max(0L, ToLong(ReadNumber(tally, "goldenCalls")));
                s.JarsOpened = Math.Max(0L, ToLong(ReadNumber(tally, "jars")));
                s.DailyGifts = Math.Max(0L, ToLong(ReadNumber(tally, "gifts")));
                s.DailyQuestsToday = Small(ReadNumber(tally, "dailyToday"), 1000);
                s.BestFish = Small(ReadNumber(tally, "bestFish"), Fishing.Kinds);
            }
            for (int i = s.Fish.Length - 1; i >= 0 && s.BestFish <= i; i--) if (s.Fish[i] > 0) s.BestFish = i + 1;

            if (root.TryGetValue("newcomer", out object newcomerObj) && newcomerObj is Dictionary<string, object> newcomer)
            {
                int all = (1 << Newcomer.Tasks) - 1;
                s.Newcomer.Claimed = Small(ReadNumber(newcomer, "claimed"), all) & all;
                s.Newcomer.DaysPaid = Small(ReadNumber(newcomer, "days"), (1 << Newcomer.Days) - 1);
                s.Newcomer.Closed = newcomer.TryGetValue("closed", out object closedObj) && closedObj is bool closed && closed;
            }
            else s.Newcomer.Closed = Seasons.DaysPlayed(s) >= Newcomer.Days;

            result.Status = LoadStatus.Ok;
            return result;
        }

        static int Small(double v, int max) => (int)Math.Max(0d, Math.Min(v, max));

        static void ReadTrunk(TrunkRecord r, Dictionary<string, object> trunk)
        {
            if (!TryNumber(trunk, "window", out double window) || !(trunk.TryGetValue("items", out object itemsObj) && itemsObj is List<object> items)) return;
            var codes = new List<string>();
            foreach (object item in items)
                if (item is string code && Trunk.IsCode(code) && codes.Count < Trunk.Shown) codes.Add(code);
            if (codes.Count != items.Count) return;
            r.Window = Math.Max(-1L, ToLong(window));
            r.Codes = codes;
            r.Sale = TryNumber(trunk, "sale", out double sale) && sale >= 0d && sale < codes.Count ? (int)sale : -1;
            r.Bought = Small(ReadNumber(trunk, "bought"), (1 << Trunk.Shown) - 1);
        }

        static int IndexOf(IReadOnlyList<StationDef> defs, string id)
        {
            for (int i = 0; i < defs.Count; i++)
                if (defs[i].Id == id) return i;
            return -1;
        }

        public static Dictionary<string, object> ReadObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return new Reader(json).ReadDocument() as Dictionary<string, object>; }
            catch (FormatException) { return null; }
        }

        public static string Quote(string value)
        {
            var sb = new StringBuilder((value ?? "").Length + 2);
            WriteString(sb, value);
            return sb.ToString();
        }

        static bool LegacySegments(Dictionary<string, object> grid, out double segments)
        {
            if (TryNumber(grid, "segments", out segments)) return true;
            if (!TryNumber(grid, "radius", out double radius)) return false;
            segments = radius - LegacyStartingRadius;
            return true;
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

        static int MonthMask(Dictionary<string, object> obj, string key, int all) => (int)Math.Max(0d, Math.Min(ReadNumber(obj, key), all)) & all;

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
                _pos++;
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
                _pos++;
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
                _pos++;
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
