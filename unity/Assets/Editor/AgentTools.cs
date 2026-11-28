// Entry points for agents that run the Unity Editor without a window (-batchmode) on the self-hosted runner
// (2026-11-27). Call them with tools/unity/unity.ps1. Each method ends the Editor with exit code 0 (success) or 1.
//   AgentTools.Ping          opens the project (imports and compiles), prints the version and the scenes.
//   AgentTools.CaptureEdit   renders a scene in edit mode to a PNG.
//   AgentTools.CapturePlay   enters play mode, waits, then renders the main camera to a PNG.
//     Battle options for CapturePlay: -agentDefenders "machine_gun:60:350,rifle_squad:140:350" places defenders,
//     -agentWave starts that many waves (default 0), -agentTimeScale speeds up the battle (default 1).
//     -agentBudget adds funds; -agentAttackers id:count:delay:interval adds attackers after the wave starts.
//     -agentFocusWorld x:y and -agentOrtho set the camera for the capture.
//     -agentWaitForMixed 1 waits up to 30 s for corpses and scattered parts.
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

    public static void CapturePlay()
    {
        try
        {
            EditorSceneManager.OpenScene(Arg("-agentScene", "Assets/Scenes/SampleScene.unity"));
            waitSeconds = ArgInt("-agentWait", 5);
            playStart = -1;
            battleSet = false;
            EditorApplication.update += PlayTick;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception e)
        {
            Debug.LogError("[AgentTools] " + e);
            EditorApplication.Exit(1);
        }
    }

    static bool battleSet;

    // Starts a battle in the PlaytestView by reflection, so that a capture shows units (2026-11-27).
    static void SetUpBattle()
    {
        battleSet = true;
        Type viewType = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            viewType = asm.GetType("Treeline.Game.PlaytestView");
            if (viewType != null) break;
        }
        if (viewType == null) { Debug.Log("[AgentTools] no PlaytestView"); return; }
        var view = UnityEngine.Object.FindFirstObjectByType(viewType);
        if (view == null) { Debug.Log("[AgentTools] PlaytestView not in the scene"); return; }
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        object run = viewType.GetField("run", flags)?.GetValue(view);
        object battle = run?.GetType().GetProperty("Battle")?.GetValue(run);
        float.TryParse(Arg("-agentBudget", "0"), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float extraBudget);
        if (battle != null && extraBudget > 0f)
            battle.GetType().GetMethod("AddBudget")?.Invoke(battle, new object[] { extraBudget });
        string defs = Arg("-agentDefenders", "");
        if (run != null && defs.Length > 0)
        {
            var place = run.GetType().GetMethod("Place");
            foreach (string d in defs.Split(','))
            {
                string[] p = d.Split(':');
                if (p.Length < 3 || place == null) continue;
                object[] a = { p[0], float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                               float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture), null, null };
                object ok = place.Invoke(run, a);
                Debug.Log("[AgentTools] place " + d + ": " + ok + " " + a[4]);
            }
        }
        int waves = ArgInt("-agentWave", 0);
        var start = viewType.GetMethod("StartNextWave", flags);
        for (int i = 0; i < waves && start != null; i++) start.Invoke(view, null);
        string attackers = Arg("-agentAttackers", "");
        if (battle != null && attackers.Length > 0)
        {
            var queue = battle.GetType().GetMethod("QueueWave");
            foreach (string entry in attackers.Split(','))
            {
                string[] p = entry.Split(':');
                if (p.Length != 4 || queue == null) continue;
                object ok = queue.Invoke(battle, new object[] { p[0], int.Parse(p[1]),
                    float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture) });
                Debug.Log("[AgentTools] queue " + entry + ": " + ok);
            }
        }
        float ts = 1f;
        float.TryParse(Arg("-agentTimeScale", "1"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ts);
        Time.timeScale = Mathf.Max(0.1f, ts);
        Debug.Log("[AgentTools] battle set: waves " + waves + ", time scale " + Time.timeScale);
    }

    static void PlayTick()
    {
        if (!EditorApplication.isPlaying) return;
        if (playStart < 0) playStart = EditorApplication.timeSinceStartup;
        if (!battleSet && EditorApplication.timeSinceStartup - playStart > 1.0) SetUpBattle();
        double age = EditorApplication.timeSinceStartup - playStart;
        if (age < waitSeconds) return;
        if (Arg("-agentWaitForMixed", "0") == "1" && !HasMixedMarks() && age < waitSeconds + 30.0) return;
        EditorApplication.update -= PlayTick;
        int code = 0;
        try
        {
            var cam = FindCamera();
            string[] focus = Arg("-agentFocusWorld", "").Split(':');
            if (focus.Length == 2)
            {
                float x = float.Parse(focus[0], System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(focus[1], System.Globalization.CultureInfo.InvariantCulture);
                cam.transform.position = new Vector3(x, y, cam.transform.position.z);
            }
            if (float.TryParse(Arg("-agentOrtho", ""), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float ortho) && ortho > 0f)
                cam.orthographicSize = ortho;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            LogCaptureState();
            Save(cam, Arg("-agentOut", "capture.png"));
        }
        catch (Exception e) { Debug.LogError("[AgentTools] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static bool HasMixedMarks()
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("Treeline.Render.BattlefieldUnitLayer");
            if (type == null) continue;
            var layer = UnityEngine.Object.FindFirstObjectByType(type);
            if (layer == null) continue;
            var corpses = type.GetField("corpses", flags)?.GetValue(layer) as System.Collections.ICollection;
            var parts = type.GetField("flyingParts", flags)?.GetValue(layer) as System.Collections.ICollection;
            return corpses != null && corpses.Count > 0 && parts != null && parts.Count > 0;
        }
        return false;
    }

    static void LogCaptureState()
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var viewType = assembly.GetType("Treeline.Game.PlaytestView");
            if (viewType != null)
            {
                var view = UnityEngine.Object.FindFirstObjectByType(viewType);
                object run = view == null ? null : viewType.GetField("run", flags)?.GetValue(view);
                object battle = run?.GetType().GetProperty("Battle")?.GetValue(run);
                if (battle != null)
                {
                    var attackers = battle.GetType().GetProperty("Attackers")?.GetValue(battle) as System.Collections.ICollection;
                    var defenders = battle.GetType().GetProperty("Defenders")?.GetValue(battle) as System.Collections.ICollection;
                    Debug.Log("[AgentTools] capture units: attackers " + attackers?.Count + ", defenders " + defenders?.Count);
                }
            }
            var layerType = assembly.GetType("Treeline.Render.BattlefieldUnitLayer");
            if (layerType == null) continue;
            var layer = UnityEngine.Object.FindFirstObjectByType(layerType);
            if (layer == null) continue;
            var corpses = layerType.GetField("corpses", flags)?.GetValue(layer) as System.Collections.ICollection;
            var parts = layerType.GetField("flyingParts", flags)?.GetValue(layer) as System.Collections.ICollection;
            var blood = layerType.GetField("bloodMarks", flags)?.GetValue(layer) as System.Collections.ICollection;
            Debug.Log("[AgentTools] capture marks: corpses " + corpses?.Count + ", parts " + parts?.Count + ", blood " + blood?.Count);
        }
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
