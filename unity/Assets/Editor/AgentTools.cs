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

    public static void CapturePlay()
    {
        try
        {
            EditorSceneManager.OpenScene(Arg("-agentScene", "Assets/Scenes/SampleScene.unity"));
            waitSeconds = ArgInt("-agentWait", 5);
            playStart = -1;
            battleStarted = false;
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
        if (!battleStarted && Arg("-agentBattle", "0") == "1" && EditorApplication.timeSinceStartup - playStart >= 1)
        {
            try { StartCaptureBattle(); battleStarted = true; }
            catch (Exception e) { Debug.LogError("[AgentTools] " + e); EditorApplication.Exit(1); return; }
        }
        if (EditorApplication.timeSinceStartup - playStart < waitSeconds) return;
        EditorApplication.update -= PlayTick;
        int code = 0;
        try { Save(FindCamera(), Arg("-agentOut", "capture.png")); }
        catch (Exception e) { Debug.LogError("[AgentTools] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    // Use the playtest commands so the capture does not change the game or its rules.
    static void StartCaptureBattle()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<Treeline.Game.PlaytestView>();
        if (view == null) throw new InvalidOperationException("PlaytestView not found.");
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var run = (Treeline.Sim.Run)typeof(Treeline.Game.PlaytestView).GetField("run", flags).GetValue(view);
        if (run == null) throw new InvalidOperationException("Playtest run not ready.");
        var fields = (System.Collections.Generic.List<Treeline.Sim.Field>)typeof(Treeline.Game.PlaytestView).GetField("fields", flags).GetValue(view);
        int current = (int)typeof(Treeline.Game.PlaytestView).GetField("current", flags).GetValue(view);
        var field = fields[current];
        for (int i = 0; i < 3; i++)
        {
            bool ok = run.Place("rifle_squad", field.Width * (0.35f + i * 0.15f), field.Height * 0.8f, out _, out string why);
            Debug.Log("[AgentTools] defender " + i + ": " + (ok ? "placed" : why));
        }
        typeof(Treeline.Game.PlaytestView).GetMethod("StartNextWave", flags).Invoke(view, null);
        Debug.Log("[AgentTools] started wave 1 for capture");
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
