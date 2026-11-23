using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    [CreateAssetMenu(menuName = "VuonTra/Quest", fileName = "Quest")]
    public sealed class QuestData : ScriptableObject
    {
        public string Id;
        public string Title;
        public QuestMetric Metric;
        [Tooltip("Only for the StationLevel metric.")]
        public StationData Station;
        [Tooltip("0 on a daily CarrotsEarned quest means: use the day's own target.")]
        public double Target = 1;
        public bool Daily;
        [Tooltip("Reward: this many seconds of current income…")]
        public double RewardSeconds = 60;
        [Tooltip("…but never fewer carrots than this.")]
        public double RewardCarrots = 10;
        public int RewardClovers;
        public int RewardGolden;
        [Tooltip("Moon jars (#64), each in place of a clover: the day's free jar when it can still come, the clover when it cannot.")]
        public int RewardJars;
        [Tooltip("A quest of a season shows only while that season runs; None is every day.")]
        public Season Season;

        public QuestDef ToDef() => new QuestDef
        {
            Id = Id, Title = Title, Metric = Metric, StationId = Station != null ? Station.Id : null,
            Target = Target, Daily = Daily, RewardSeconds = RewardSeconds,
            RewardCarrots = RewardCarrots, RewardClovers = RewardClovers, RewardGolden = RewardGolden, Season = Season, RewardJars = RewardJars
        };
    }
}
