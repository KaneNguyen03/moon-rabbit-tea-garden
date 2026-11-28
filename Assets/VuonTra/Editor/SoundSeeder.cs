using System.IO;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;

namespace VuonTra.EditorTools
{
    public static class SoundSeeder
    {
        public const string BankPath = "Assets/Resources/" + SoundBank.ResourcePath + ".asset";

        public static string ClipPath(SoundMoment moment) => SoundBank.ClipFolder + "/" + SoundBank.FileOf(moment) + ".ogg";

        [MenuItem("VuonTra/Seed Sound Bank")]
        public static SoundBank Seed()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BankPath));
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            foreach (SoundMoment moment in SoundBank.Moments)
                bank.Set(moment, AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(moment)));
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Sfx.ReloadBank();
            return bank;
        }
    }
}
