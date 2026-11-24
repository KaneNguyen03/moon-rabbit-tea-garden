using System;
using System.Text;

namespace VuonTra.Core
{
    public interface IReviewPrompt
    {
        void RequestReview();
        void OpenStorePage();
    }

    public sealed class FakeReviewPrompt : IReviewPrompt
    {
        public Action<string> Said = _ => { };
        public int Requests { get; private set; }
        public int StorePages { get; private set; }

        public void RequestReview()
        {
            Requests++;
            Said("[VuonTra] review prompt: the store's own review box would open here");
        }

        public void OpenStorePage()
        {
            StorePages++;
            Said("[VuonTra] review prompt: the store page would open here");
        }
    }

    public enum ReviewMoment { None, GoldenRabbit, ThreeStars, LandOpened, TenHearts }

    public sealed class ReviewRecord
    {
        public int Asks;
        public long LastAskDay = -1;
        public bool Liked;
    }

    public static class ReviewAsk
    {
        public const long MinDaysPlayed = 3;
        public const double MinPlaySeconds = 30d * 60d;
        public const int MaxAsks = 3;
        public const long DaysBetweenAsks = 60;
        public const int HappyTier = 4;
        public const int HappyStars = 3;
        public const double FreshSeconds = 120d;
        public const int FeedbackMaxLength = 200;

        public static bool IsHappy(Rabbit summoned) => summoned != null && summoned.Tier >= HappyTier;

        public static bool IsHappy(int stars) => stars >= HappyStars;

        public static bool Done(GameState s) => s.Review.Liked || s.Review.Asks >= MaxAsks;

        public static bool CanAsk(GameState s, long day)
        {
            if (Done(s) || !Tutorial.IsDone(s)) return false;
            if (Seasons.DaysPlayed(s) < MinDaysPlayed || s.PlaySeconds < MinPlaySeconds) return false;
            return s.Review.LastAskDay < 0 || day - s.Review.LastAskDay >= DaysBetweenAsks;
        }

        public static void Asked(GameState s, long day)
        {
            s.Review.Asks = Math.Min(MaxAsks, s.Review.Asks + 1);
            s.Review.LastAskDay = day;
        }

        public static void Like(GameState s) => s.Review.Liked = true;

        public static string NameOf(ReviewMoment moment)
        {
            switch (moment)
            {
                case ReviewMoment.GoldenRabbit: return "golden_rabbit";
                case ReviewMoment.ThreeStars: return "three_stars";
                case ReviewMoment.LandOpened: return "land_opened";
                case ReviewMoment.TenHearts: return "ten_hearts";
                default: return "none";
            }
        }

        public static string OneLine(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new StringBuilder(raw.Length);
            bool gap = false;
            foreach (char c in raw)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c)) { gap = sb.Length > 0; continue; }
                if (gap) sb.Append(' ');
                gap = false;
                sb.Append(c);
            }
            return sb.Length > FeedbackMaxLength ? sb.ToString(0, FeedbackMaxLength).TrimEnd() : sb.ToString();
        }

        public static string MailTo(string email, string subject, string body) =>
            "mailto:" + email + "?subject=" + Uri.EscapeDataString(subject ?? "") + "&body=" + Uri.EscapeDataString(body ?? "");
    }

    public sealed class ReviewWatch
    {
        public bool Crashed;
        public bool PurchaseFailed;
        public ReviewMoment Waiting { get; private set; }
        public ReviewMoment LastAsked { get; private set; }
        double _notedAt;

        public void NewSession()
        {
            Crashed = false;
            PurchaseFailed = false;
            Waiting = ReviewMoment.None;
        }

        public bool CanAsk(GameState s, long day, bool busy) => !busy && !Crashed && !PurchaseFailed && ReviewAsk.CanAsk(s, day);

        public bool Note(GameState s, long day, ReviewMoment moment)
        {
            if (moment == ReviewMoment.None || !CanAsk(s, day, false)) return false;
            Waiting = moment;
            _notedAt = s.PlaySeconds;
            return true;
        }

        public ReviewMoment Take(GameState s, long day, bool busy)
        {
            if (Waiting == ReviewMoment.None) return ReviewMoment.None;
            if (s.PlaySeconds - _notedAt > ReviewAsk.FreshSeconds || !CanAsk(s, day, false))
            {
                Waiting = ReviewMoment.None;
                return ReviewMoment.None;
            }
            if (busy) return ReviewMoment.None;
            ReviewMoment moment = Waiting;
            Waiting = ReviewMoment.None;
            LastAsked = moment;
            ReviewAsk.Asked(s, day);
            return moment;
        }
    }
}
