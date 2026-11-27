// Entry points for agents that run the Unity Editor without a window (-batchmode) on the self-hosted runner
// (2026-11-27). Call them with tools/unity/unity.ps1. Each method ends the Editor with exit code 0 (success) or 1.
//   AgentTools.Ping          opens the project (imports and compiles), prints the version and the scenes.
//   AgentTools.CaptureEdit   renders a scene in edit mode to a PNG.
//   AgentTools.CapturePlay   enters play mode, waits, then renders the main camera to a PNG.
//   AgentTools.Showroom      lays out PNG sprites (pixel-perfect) on a background and renders a phone-size PNG.
//     -agentImages <folder of PNGs> -agentBackground <png, optional> -agentScale <screen px per sprite px, default 3>
//     -agentOut <png> -agentWidth 1080 -agentHeight 2340. Sprites are placed left to right, top to bottom, in name order.
// Arguments: -agentScene Assets/Scenes/SampleScene.unity  -agentOut <png path>  -agentWait <seconds>
//            -agentWidth 1280  -agentHeight 720
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AgentTools
{
    static string Arg(string name, string fallback)
    {
        string[] a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++)
            if (a[i] == name) return a[i + 1];
        return fallback;
    }

    static int ArgInt(string name, int fallback) => int.TryParse(Arg(name, ""), out int v) ? v : fallback;

    public static void Ping()
    {
        Debug.Log("[AgentTools] Unity " + Application.unityVersion + ", project " + Application.dataPath);
        foreach (var s in EditorBuildSettings.scenes) Debug.Log("[AgentTools] scene in build: " + s.path);
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            Debug.Log("[AgentTools] scene: " + AssetDatabase.GUIDToAssetPath(guid));
        EditorApplication.Exit(0);
    }

    public static void CaptureEdit()
    {
        try
        {
            EditorSceneManager.OpenScene(Arg("-agentScene", "Assets/Scenes/SampleScene.unity"));
            Save(FindCamera(), Arg("-agentOut", "capture.png"));
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[AgentTools] " + e);
            EditorApplication.Exit(1);
        }
    }

    static double playStart;
    static float waitSeconds;
    static bool densityReady;
    static SpriteRenderer[] densityRenderers;
    static Sprite[][] densityFrames;

    // Visual-only comparison placed over the live battle. It does not alter Sim.
    static void SetupDensityComparison()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<Treeline.Game.PlaytestView>();
        if (view == null) throw new InvalidOperationException("PlaytestView is missing.");
        var type = view.GetType();
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var files = (System.Collections.Generic.List<string>)type.GetField("fieldFiles", flags).GetValue(view);
        int forest = files.IndexOf("forest-belts");
        if (forest < 0) throw new InvalidOperationException("Forest belts is missing.");
        type.GetField("current", flags).SetValue(view, forest);
        type.GetMethod("NewRun", flags).Invoke(view, null);
        type.GetMethod("StartNextWave", flags).Invoke(view, null);
        string[] names = { "current", "one-half", "double", "painted" };
        densityRenderers = new SpriteRenderer[4];
        densityFrames = new Sprite[4][];
        Camera cam = FindCamera();
        for (int i = 0; i < names.Length; i++)
        {
            string file = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,
                "docs/design/visual/density/" + names[i] + ".png");
            Texture2D tex = Load(file);
            if (i == 3) tex.filterMode = FilterMode.Bilinear;
            int cell = tex.height / 2;
            densityFrames[i] = new Sprite[4];
            for (int f = 0; f < 4; f++)
                densityFrames[i][f] = Sprite.Create(tex, new Rect(f * cell, cell, cell, cell),
                    new Vector2(0.5f, 0.5f), cell / 12f);
            var go = new GameObject("Density comparison " + names[i]);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 200;
            Vector3 pos = new Vector3(cam.transform.position.x + (i - 1.5f) * cam.orthographicSize * 0.23f, cam.transform.position.y, 0f);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            densityRenderers[i] = renderer;
        }
        densityReady = true;
    }


    public static void CapturePlay()
    {
        try
        {
            EditorSceneManager.OpenScene(Arg("-agentScene", "Assets/Scenes/SampleScene.unity"));
            waitSeconds = ArgInt("-agentWait", 5);
            playStart = -1;
            densityReady = false;
            EditorApplication.update += PlayTick;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception e)
        {
            Debug.LogError("[AgentTools] " + e);
            EditorApplication.Exit(1);
        }
    }

    static void PlayTick()
    {
        if (!EditorApplication.isPlaying) return;
        if (playStart < 0) playStart = EditorApplication.timeSinceStartup;
        if (Arg("-agentDensity", "") == "1" && EditorApplication.timeSinceStartup - playStart > 1)
        {
            if (!densityReady) SetupDensityComparison();
            int frame = (int)(EditorApplication.timeSinceStartup * 5) % 4;
            for (int i = 0; i < densityRenderers.Length; i++) densityRenderers[i].sprite = densityFrames[i][frame];
        }
        if (EditorApplication.timeSinceStartup - playStart < waitSeconds) return;
        EditorApplication.update -= PlayTick;
        int code = 0;
        try { var cam = FindCamera(); if (densityReady) cam.rect = new Rect(0, 0, 1, 1); Save(cam, Arg("-agentOut", "capture.png")); }
        catch (Exception e) { Debug.LogError("[AgentTools] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    public static void Showroom()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            int w = ArgInt("-agentWidth", 1080), h = ArgInt("-agentHeight", 2340), scale = ArgInt("-agentScale", 3);
            var camGo = new GameObject("Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = h / 2f;              // 1 world unit = 1 screen pixel
            cam.transform.position = new Vector3(w / 2f, -h / 2f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.40f, 0.28f);
            cam.tag = "MainCamera";

            string bg = Arg("-agentBackground", "");
            if (bg.Length > 0 && File.Exists(bg))
            {
                Texture2D t = Load(bg);
                var go = new GameObject("Background");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0f, 1f), 1f);
                go.transform.localScale = new Vector3((float)w / t.width, (float)h / t.height, 1f);
                sr.sortingOrder = -100;
            }

            string dir = Arg("-agentImages", "");
            string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.png") : new string[0];
            Array.Sort(files, StringComparer.Ordinal);
            float x = 40, y = -60, rowH = 0;
            foreach (string f in files)
            {
                Texture2D t = Load(f);
                float sw = t.width * scale, sh = t.height * scale;
                if (x + sw > w - 40) { x = 40; y -= rowH + 40; rowH = 0; }
                var go = new GameObject(Path.GetFileNameWithoutExtension(f));
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0f, 1f), 1f);
                go.transform.position = new Vector3(x, y, 0f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                x += sw + 40; rowH = Mathf.Max(rowH, sh);
                Debug.Log("[AgentTools] showroom: " + f);
            }
            Save(cam, Arg("-agentOut", "showroom.png"));
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[AgentTools] " + e);
            EditorApplication.Exit(1);
        }
    }

    static Texture2D Load(string path)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(File.ReadAllBytes(path));
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    static Camera FindCamera()
    {
        if (Camera.main != null) return Camera.main;
        Camera[] all = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (all.Length == 0) throw new InvalidOperationException("No camera in the scene.");
        return all[0];
    }

    static void Save(Camera cam, string path)
    {
        int w = ArgInt("-agentWidth", 1280), h = ArgInt("-agentHeight", 720);
        var rt = new RenderTexture(w, h, 24);
        RenderTexture old = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        cam.targetTexture = old;
        RenderTexture.active = null;
        string full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllBytes(full, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        rt.Release();
        Debug.Log("[AgentTools] saved " + full);
    }
}
