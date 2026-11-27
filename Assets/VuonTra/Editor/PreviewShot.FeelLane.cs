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
                    GameState s = JuiceGarden(runner, day);
                    s.Coins = new BigNum(5780);
                    runner.Init(s);
                    s.TotalTaps = s.DailyTapsBase + 50;
                    screen.RebuildUi();
                    if (screen.Shell.State == ShellState.Title) screen.Shell.StartGame();
                    else if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
                    screen.SelectStation(-1);
                    screen.Refresh();
                    Step(screen, 2.5f);
                    string name = "168-" + locale + "-";
                    Capture(screen, name + "vuon");
                    Press(screen, "Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Claim");
                    Step(screen, 0.5f);
                    Capture(screen, name + "dang-bay");
                    Step(screen, 0.5f);
                    Capture(screen, name + "qua-va-so-noi");
                    Step(screen, 2.5f);
                }
            }
            finally
            {
                runner.DayClock = clock;
                Motion.Reduced = reduced;
                L10n.SetLocale("vi");
            }
        }
    }
}
