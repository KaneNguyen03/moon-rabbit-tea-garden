using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D.Animation;
using VuonTra.Game;
using static VuonTra.EditorTools.RabbitRigSpec;

namespace VuonTra.EditorTools
{
    public static class RabbitCapture
    {
        const int Fps = 24;
        const float TiltDegrees = 40f;
        static readonly RabbitView[] Rows = { RabbitView.Front, RabbitView.ThreeQuarterLeft, RabbitView.ThreeQuarterRight };
        static readonly Color Ground = new Color(0.239f, 0.514f, 0.412f, 1f);

        static string OutDir(string name) => Path.GetFullPath($"Logs/rabbit25d/{name}");

        static void Deform()
        {
            Type manager = typeof(SpriteSkin).Assembly.GetType("UnityEngine.U2D.Animation.DeformationManager");
            object instance = manager.GetProperty("instance").GetValue(null);
            manager.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(instance, null);
        }

        sealed class Actor
        {
            public GameObject Rabbit;
            public AnimationClip Clip;
            public WorkLoop Loop;
        }

        static Actor Spawn(RabbitKind kind, RabbitView view, WorkLoop loop, Vector3 position, Quaternion facing, bool flatShadow)
        {
            var rabbit = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(kind.Prefab));
            rabbit.transform.SetPositionAndRotation(position, facing);
            rabbit.GetComponent<Animator>().enabled = false;
            rabbit.GetComponent<RabbitActor>().SetView(view);
            if (flatShadow) RabbitRigBuilder.Find(rabbit.transform, ShadowTarget).rotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (SpriteSkin skin in rabbit.GetComponentsInChildren<SpriteSkin>(true))
            {
                skin.alwaysUpdate = true;
                skin.forceCpuDeformation = true;
            }
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(kind.Clip(loop));
            return new Actor { Rabbit = rabbit, Clip = clip, Loop = loop };
        }

        static void Pose(Actor actor, float time)
        {
            actor.Clip.SampleAnimation(actor.Rabbit, time % actor.Clip.length);
            foreach (SpriteResolver resolver in actor.Rabbit.GetComponentsInChildren<SpriteResolver>(true))
                resolver.ResolveSpriteToSpriteRenderer();
        }

        static Sprite Plain()
        {
            var texture = new Texture2D(4, 4);
            texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        static void Shoot(Camera camera, int width, int height, string dir, int frames, List<Actor> actors)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.aspect = width / (float)height;
            var readback = new Texture2D(width, height, TextureFormat.RGB24, false);
            for (int f = 0; f < frames; f++)
            {
                float t = f / (float)Fps;
                foreach (Actor actor in actors) Pose(actor, t);
                Deform();
                camera.Render();
                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readback.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"f{f:000}.png"), readback.EncodeToPNG());
            }
            RenderTexture.active = null;
        }

        static Camera NewCamera()
        {
            var camera = new GameObject("CaptureCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ground;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            return camera;
        }

        static int Frames() => Mathf.CeilToInt(Loops.Max(l => RabbitClipWriter.Length(l)) * Fps);

        static void Flat(RabbitKind kind)
        {
            var actors = new List<Actor>();
            for (int row = 0; row < Rows.Length; row++)
            for (int col = 0; col < Loops.Length; col++)
                actors.Add(Spawn(kind, Rows[row], Loops[col], new Vector3(col * 3.6f, -row * 5.6f, 0f), Quaternion.identity, false));
            Camera camera = NewCamera();
            camera.orthographic = true;
            camera.orthographicSize = 8.4f;
            camera.transform.position = new Vector3(5.4f, -3.35f, -10f);
            Shoot(camera, 1500, 1680, OutDir($"{kind.Id}/flat"), Frames(), actors);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            foreach (Actor actor in actors) UnityEngine.Object.DestroyImmediate(actor.Rabbit);
        }

        static void Tilted(RabbitKind[] kinds)
        {
            Quaternion look = Quaternion.Euler(TiltDegrees, 0f, 0f);
            var ground = new GameObject("Ground").AddComponent<SpriteRenderer>();
            ground.sprite = Plain();
            ground.color = new Color(0.36f, 0.58f, 0.34f, 1f);
            ground.sortingOrder = -100;
            ground.transform.SetPositionAndRotation(new Vector3(5.1f, 0f, 8f), Quaternion.Euler(90f, 0f, 0f));
            ground.transform.localScale = new Vector3(40f, 30f, 1f);
            var actors = new List<Actor>();
            for (int row = 0; row < Rows.Length; row++)
            for (int col = 0; col < kinds.Length; col++)
                actors.Add(Spawn(kinds[col], Rows[row], Loops[(row + col) % Loops.Length], new Vector3(col * 3.4f, 0f, row * 6f), look, true));
            Camera camera = NewCamera();
            camera.orthographic = false;
            camera.fieldOfView = 34f;
            camera.transform.rotation = look;
            camera.transform.position = new Vector3(5.1f, 1.2f, 6f) + look * new Vector3(0f, 0f, -27f);
            Shoot(camera, 1400, 1000, OutDir("tilted"), Frames(), actors);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(ground.gameObject);
            foreach (Actor actor in actors) UnityEngine.Object.DestroyImmediate(actor.Rabbit);
        }

        static string CheckTools(RabbitKind kind)
        {
            var report = new StringBuilder();
            int failures = 0;
            foreach (WorkLoop loop in Loops)
            foreach (RabbitView view in new[] { RabbitView.Front, RabbitView.ThreeQuarterLeft })
            {
                Actor actor = Spawn(kind, view, loop, Vector3.zero, Quaternion.identity, false);
                for (int i = 0; i < loop.Beats.Length; i++)
                {
                    float time = RabbitClipWriter.BeatStart(loop, i) + loop.Beats[i].Length * 0.97f;
                    Pose(actor, time);
                    foreach (SpriteResolver resolver in actor.Rabbit.GetComponentsInChildren<SpriteResolver>(false)
                                 .Where(r => r.GetCategory() == ToolCategory))
                    {
                        var renderer = resolver.GetComponent<SpriteRenderer>();
                        string expected = Expected(loop, i, view == RabbitView.Front ? FrontKey : SideKey, resolver.name);
                        string shown = renderer.enabled ? renderer.sprite.name : null;
                        bool ok = expected == shown;
                        if (!ok) failures++;
                        report.AppendLine($"{kind.Id}_{loop.Work} beat {i + 1} {resolver.name}: expected {expected ?? "-"} shown {shown ?? "-"} {(ok ? "ok" : "FAIL")}");
                    }
                }
                UnityEngine.Object.DestroyImmediate(actor.Rabbit);
            }
            report.AppendLine(failures == 0 ? "tool check: all ok" : $"tool check: {failures} FAIL");
            return report.ToString();
        }

        static string Expected(WorkLoop loop, int beat, string viewKey, string handObject)
        {
            string hand = viewKey + ":" + handObject.Substring(handObject.Length - 1);
            string held = null;
            for (int i = 0; i <= beat; i++)
                foreach (ToolSwap swap in loop.Beats[i].Tools.Where(s => s.Hand == hand))
                    held = swap.Label;
            return held;
        }

        static string BeatTable()
        {
            var json = new StringBuilder("{\"fps\":" + Fps + ",\"loops\":[");
            json.Append(string.Join(",", Loops.Select(loop =>
                "{\"clip\":\"" + loop.Work + "\",\"length\":" + RabbitClipWriter.Length(loop).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"beats\":[" + string.Join(",", loop.Beats.Select((b, i) =>
                    "{\"name\":\"" + b.Name + "\",\"pose\":" +
                    (RabbitClipWriter.BeatStart(loop, i) + b.Length * 0.7f).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "}")) + "]}")));
            json.Append("]}");
            return json.ToString();
        }

        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RabbitKind[] kinds = RabbitRigBuilder.ReadyKinds();
            Directory.CreateDirectory(OutDir(""));
            File.WriteAllText(Path.Combine(OutDir(""), "beats.json"), BeatTable());
            var tools = new StringBuilder();
            foreach (RabbitKind kind in kinds)
            {
                tools.Append(CheckTools(kind));
                Flat(kind);
            }
            File.WriteAllText(Path.Combine(OutDir(""), "tool-check.txt"), tools.ToString());
            Tilted(kinds);
            Debug.Log("[rabbit25d] captured " + string.Join(", ", kinds.Select(k => k.Id)) + "\n" + tools);
        }
    }
}
