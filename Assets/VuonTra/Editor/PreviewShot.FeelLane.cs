using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.EditorTools
{
    public static partial class PreviewShot
    {
        static void RenderFeelLane(GameRunner runner, GardenScreen screen)
        {
            System.Func<long> clock = runner.DayClock;
            long day = GiftCodes.Day(2026, 11, 28);
            runner.DayClock = () => day;
            screen.Raining = () => false;
            bool reduced = Motion.Reduced;
            Motion.Reduced = false;
            try
            {
                foreach (string locale in BothLocales)
                {
                    L10n.SetLocale(locale);
                    FeelLaneGarden(runner, screen, day);
                    string name = "168-" + locale + "-";
                    Capture(screen, name + "vuon");
                    Press(screen, "Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Claim");
                    Step(screen, 0.5f);
                    Capture(screen, name + "dang-bay");
                    Step(screen, 0.5f);
                    Capture(screen, name + "qua-va-so-noi");
                    Step(screen, 2.5f);
                    SheetClaims(runner, screen, day, name, "");
                }
                foreach (KeyValuePair<string, Vector2Int> tablet in Tablets)
                    AtFrameSize(tablet.Value, () =>
                    {
                        foreach (string locale in BothLocales)
                        {
                            L10n.SetLocale(locale);
                            SheetClaims(runner, screen, day, "168-" + locale + "-", "-" + tablet.Key);
                        }
                    });
            }
            finally
            {
                runner.DayClock = clock;
                Motion.Reduced = reduced;
                L10n.SetLocale("vi");
            }
        }

        static readonly KeyValuePair<string, Vector2Int>[] Tablets =
        {
            new KeyValuePair<string, Vector2Int>("4x3", new Vector2Int(1536, 2048)),
            new KeyValuePair<string, Vector2Int>("16x10", new Vector2Int(1200, 1920)),
        };

        static void SheetClaims(GameRunner runner, GardenScreen screen, long day, string name, string size)
        {
            FeelLaneGarden(runner, screen, day);
            screen.OpenSheet(2);
            Step(screen, 0.6f);
            Press(screen, "Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Claim");
            Step(screen, 1f);
            Capture(screen, name + "bang-nua-qua" + size);
            Step(screen, 2.5f);
            screen.CloseSheet();
            Step(screen, 0.5f);

            FeelLaneGarden(runner, screen, day);
            screen.OpenSheet(2);
            Step(screen, 0.5f);
            screen.DragSheet(screen.SheetFull);
            screen.ReleaseSheet();
            Step(screen, 0.6f);
            Press(screen, "Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Claim");
            Step(screen, 1f);
            Capture(screen, name + "bang-day-qua" + size);
            Step(screen, 2.5f);
            screen.CloseSheet();
            Step(screen, 0.5f);
        }

        static void FeelLaneGarden(GameRunner runner, GardenScreen screen, long day)
        {
            GameState s = JuiceGarden(runner, day);
            s.Coins = new BigNum(5780);
            runner.Init(s);
            s.TotalTaps = s.DailyTapsBase + 50;
            screen.RebuildUi();
            Motion.Reduced = false;
            if (screen.Shell.State == ShellState.Title) screen.Shell.StartGame();
            else if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 2.5f);
        }
    }
}
