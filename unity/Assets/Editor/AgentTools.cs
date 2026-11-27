// Entry points for agents that run the Unity Editor without a window (-batchmode) on the self-hosted runner
// (2026-11-27). Call them with tools/unity/unity.ps1. Each method ends the Editor with exit code 0 (success) or 1.
//   AgentTools.Ping          opens the project (imports and compiles), prints the version and the scenes.
//   AgentTools.CaptureEdit   renders a scene in edit mode to a PNG.
//   AgentTools.CapturePlay   enters play mode, waits, then renders the main camera to a PNG.
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
    static bool battleStarted;
    static bool seriesStart, seriesMid, seriesDeath;
    static Treeline.Sim.Run captureRun;

    public static void CapturePlay()
    {
        try
        {
            EditorSceneManager.OpenScene(Arg("-agentScene", "Assets/Scenes/SampleScene.unity"));
            waitSeconds = ArgInt("-agentWait", 5);
            playStart = -1;
            battleStarted = false;
            seriesStart = seriesMid = seriesDeath = false;
            captureRun = null;
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
        if (!battleStarted && Arg("-agentBattle", "") == "1" && EditorApplication.timeSinceStartup - playStart >= 1)
        {
            battleStarted = true;
            try { StartStyleBattle(); }
            catch (Exception e) { Debug.LogError("[AgentTools] battle setup: " + e); EditorApplication.Exit(1); return; }
        }
        if (Arg("-agentSeries", "") == "1" && battleStarted)
        {
            double elapsed = EditorApplication.timeSinceStartup - playStart;
            string prefix = Arg("-agentOut", "capture.png");
            string stem = Path.Combine(Path.GetDirectoryName(prefix) ?? "", Path.GetFileNameWithoutExtension(prefix));
            if (!seriesStart && elapsed >= 4) { Save(FindCamera(), stem + "-start.png"); seriesStart = true; }
            if (!seriesMid && elapsed >= 12) { Save(FindCamera(), stem + "-mid.png"); seriesMid = true; }
            if (!seriesDeath && captureRun != null && captureRun.Battle.Rewards > 0)
            { Save(FindCamera(), stem + "-death.png"); seriesDeath = true; }
        }
        if (EditorApplication.timeSinceStartup - playStart < waitSeconds) return;
        EditorApplication.update -= PlayTick;
        int code = 0;
        try { Save(FindCamera(), Arg("-agentOut", "capture.png")); }
        catch (Exception e) { Debug.LogError("[AgentTools] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void StartStyleBattle()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<Treeline.Game.PlaytestView>();
        if (view == null) throw new InvalidOperationException("PlaytestView is missing");
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var type = view.GetType();
        var files = (System.Collections.Generic.List<string>)type.GetField("fieldFiles", flags).GetValue(view);
        int field = files.IndexOf("forest-belts");
        if (field < 0) throw new InvalidOperationException("Forest belts is missing");
        type.GetField("current", flags).SetValue(view, field);
        type.GetMethod("NewRun", flags).Invoke(view, null);
        var run = (Treeline.Sim.Run)type.GetField("run", flags).GetValue(view);
        captureRun = run;
        run.Battle.AddBudget(1000f); // Capture setup only.
        foreach (var item in new[] { ("rifle_squad", 70f, 260f), ("machine_gun", 100f, 270f), ("sniper", 130f, 280f) })
        {
            bool placed = run.Place(item.Item1, item.Item2, item.Item3, out _, out string why);
            Debug.Log("[AgentTools] place " + item.Item1 + ": " + placed + " " + why);
        }
        Debug.Log("[AgentTools] start wave: " + run.StartNextWave());
        type.GetField("simSpeed", flags).SetValue(view, 3);
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
        if (Arg("-agentBattle", "") == "1")
        {
            cam.rect = new Rect(0, 0, 1, 1);
            cam.transform.position = new Vector3(0, 0, cam.transform.position.z);
            cam.orthographicSize = 210f;
        }
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
