using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.EditorTools
{
    public static partial class PreviewShot
    {
        public const string StandsFolder = "docs/anh/po-146";
        const int MilkTeaStation = 6;
        const string RiggedBreed = "tho-bong";
        static readonly int[] StaffedStations = { 0, 1, 2, 3, 4, MilkTeaStation };

        [MenuItem("VuonTra/Render 2.5D Stations Before And After (#146)")]
        public static void RenderStands25D()
        {
            Directory.CreateDirectory(StandsFolder);
            bool reduced = Motion.Reduced;
            try
            {
                Motion.Reduced = false;
                ShootGarden(false, "vi", false, false, "146-truoc", StandsFolder, -1, true);
                ShootGarden(true, "vi", false, false, "146-sau", StandsFolder, -1, true);
                ShootGarden(false, "vi", false, false, "146-truoc-gan", StandsFolder, MilkTeaStation, true);
                ShootGarden(true, "vi", false, false, "146-sau-gan", StandsFolder, MilkTeaStation, true);
            }
            finally
            {
                Features.Garden25D = Features.Garden25DByDefault;
                Motion.Reduced = reduced;
                UiKit.SetLargeText(false);
                L10n.SetLocale("vi");
            }
        }

        public const string LoopFrames = "Logs/po146-loop";
        const int LoopFps = 12;

        [MenuItem("VuonTra/Render 2.5D Stations And Work Loops (#146)")]
        public static void RenderAll146()
        {
            RenderStands25D();
            RenderLoops25D();
        }

        [MenuItem("VuonTra/Render 2.5D Work Loops Of The Three Sample Stations (#146)")]
        public static void RenderLoops25D()
        {
            bool reduced = Motion.Reduced;
            try
            {
                Motion.Reduced = false;
                foreach (int station in new[] { 0, 1, MilkTeaStation })
                    ShootGarden(true, "vi", false, false, "loop", StandsFolder, station, true, (runner, screen, eye, rt) => FilmLoop(runner, screen, eye, rt, station));
            }
            finally
            {
                Features.Garden25D = Features.Garden25DByDefault;
                Motion.Reduced = reduced;
                UiKit.SetLargeText(false);
                L10n.SetLocale("vi");
            }
        }

        static void FilmLoop(GameRunner runner, GardenScreen screen, Camera eye, RenderTexture rt, int station)
        {
            RabbitActor rig = runner.Garden25D.Stands.Rig(station);
            string dir = Path.Combine(LoopFrames, runner.Defs[station].Id);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            if (rig == null)
            {
                File.WriteAllText(Path.Combine(dir, "no-rig.txt"), "no rigged rabbit at this station");
                return;
            }
            AnimationClip clip = Array.Find(rig.Animator.runtimeAnimatorController.animationClips, c => c.name.EndsWith(rig.Work.ToString()));
            if (clip == null)
            {
                File.WriteAllText(Path.Combine(dir, "no-clip.txt"), "no clip for " + rig.Work);
                return;
            }
            rig.Animator.enabled = false;
            foreach (SpriteSkin skin in rig.GetComponentsInChildren<SpriteSkin>(true))
            {
                skin.alwaysUpdate = true;
                skin.forceCpuDeformation = true;
            }
            int frames = Mathf.CeilToInt(clip.length * LoopFps);
            var readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            for (int f = 0; f < frames; f++)
            {
                clip.SampleAnimation(rig.gameObject, f / (float)LoopFps);
                foreach (SpriteResolver resolver in rig.GetComponentsInChildren<SpriteResolver>(true)) resolver.ResolveSpriteToSpriteRenderer();
                Deform();
                Canvas.ForceUpdateCanvases();
                eye.Render();
                eye.Render();
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                readback.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"f{f:000}.png"), readback.EncodeToPNG());
            }
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(readback);
            File.WriteAllText(Path.Combine(dir, "clip.txt"), clip.name + " " + clip.length.ToString("0.000") + " s, " + frames + " frames at " + LoopFps + " fps");
        }

        static void Deform()
        {
            Type manager = typeof(SpriteSkin).Assembly.GetType("UnityEngine.U2D.Animation.DeformationManager");
            object instance = manager.GetProperty("instance").GetValue(null);
            manager.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(instance, null);
        }

        static void Staff(GameState state, System.Collections.Generic.IReadOnlyList<StationDef> defs)
        {
            state.Plots = MilkTeaStation + 1;
            state.Levels[MilkTeaStation] = 3;
            GardenGrid.Settle(defs, state);
            for (int n = 0; n < StaffedStations.Length; n++)
            {
                int station = StaffedStations[n];
                bool sample = station == 0 || station == 1 || station == MilkTeaStation;
                state.Rabbits.Add(new Rabbit { Id = 500 + n, Tier = sample ? 0 : n % 3, Kind = sample ? RiggedBreed : null, Name = "Thỏ " + n, Station = station });
            }
        }

        static void LookClose(GardenScreen screen, GameRunner runner, int station, bool island)
        {
            if (!island)
            {
                screen.ZoomToStation(station, false);
                Step(screen, 1f);
                return;
            }
            Garden25DWorld world = runner.Garden25D;
            GroundPoint foot = GardenFraming.Ground(GardenStands.StationTile(runner.Defs, runner.State, station), world.Rig.Spec);
            world.Rig.Frame(foot, world.Rig.Spec.NearDistance);
        }
    }
}
