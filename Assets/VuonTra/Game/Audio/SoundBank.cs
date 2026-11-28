using System;
using UnityEngine;

namespace VuonTra.Game
{
    public enum SoundMoment { Summon, GardenLevel, Plot, Gift, Pet, Tray }

    [CreateAssetMenu(menuName = "VuonTra/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        public const string ResourcePath = "SoundBank";
        public const string ClipFolder = "Assets/VuonTra/Art/Final";
        public const float PitchSpread = 0.05f;
        public const float Volume = 0.45f;
        public const float SemitonesPerTier = 1f;
        public const float MaxSeconds = 1.5f;

        public static readonly SoundMoment[] Moments = (SoundMoment[])Enum.GetValues(typeof(SoundMoment));

        public AudioClip Summon, GardenLevel, Plot, Gift, Pet, Tray;

        public AudioClip Clip(SoundMoment moment)
        {
            switch (moment)
            {
                case SoundMoment.Summon: return Summon;
                case SoundMoment.GardenLevel: return GardenLevel;
                case SoundMoment.Plot: return Plot;
                case SoundMoment.Gift: return Gift;
                case SoundMoment.Pet: return Pet;
                case SoundMoment.Tray: return Tray;
                default: return null;
            }
        }

        public void Set(SoundMoment moment, AudioClip clip)
        {
            switch (moment)
            {
                case SoundMoment.Summon: Summon = clip; break;
                case SoundMoment.GardenLevel: GardenLevel = clip; break;
                case SoundMoment.Plot: Plot = clip; break;
                case SoundMoment.Gift: Gift = clip; break;
                case SoundMoment.Pet: Pet = clip; break;
                case SoundMoment.Tray: Tray = clip; break;
            }
        }

        public static string FileOf(SoundMoment moment)
        {
            switch (moment)
            {
                case SoundMoment.Summon: return "sfx_goi-tho";
                case SoundMoment.GardenLevel: return "sfx_len-cap-vuon";
                case SoundMoment.Plot: return "sfx_mo-dat";
                case SoundMoment.Gift: return "sfx_qua";
                case SoundMoment.Pet: return "sfx_vuot-tho";
                case SoundMoment.Tray: return "sfx_thu-khay";
                default: return null;
            }
        }

        public static float TierPitch(int tier) => Mathf.Pow(2f, Mathf.Max(0, tier) * SemitonesPerTier / 12f);
    }
}
