// Run with tools/unity/unity.ps1 -Method JuiceAudit.Run.
using System;
using System.Reflection;
using Treeline.Render;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class JuiceAudit
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 100f;
            var original = camera.transform.position;
            var effects = new GameObject("Effects").AddComponent<BattlefieldEffects>();
            effects.Initialize(200f, 400f);
            effects.Explosion(Vector2.zero, 10f);
            typeof(BattlefieldEffects).GetMethod("LateUpdate", Private).Invoke(effects, null);
            if (camera.transform.position == original) throw new Exception("Blast did not shake the camera");
            typeof(BattlefieldEffects).GetMethod("OnDisable", Private).Invoke(effects, null);
            if (Vector3.Distance(camera.transform.position, original) > 0.0001f)
                throw new Exception("Camera did not return after shake");

            var layer = new GameObject("Units").AddComponent<BattlefieldUnitLayer>();
            var type = typeof(BattlefieldUnitLayer);
            Time.timeScale = 0.75f;
            var register = type.GetMethod("RegisterKill", Private);
            register.Invoke(layer, new object[] { 1f });
            register.Invoke(layer, new object[] { 1.1f });
            if (!Mathf.Approximately(Time.timeScale, 0.225f)) throw new Exception("Multi-kill did not slow time");
            var late = type.GetMethod("LateUpdate", Private);
            late.Invoke(layer, null);
            late.Invoke(layer, null);
            if (!Mathf.Approximately(Time.timeScale, 0.225f)) throw new Exception("Slow motion ended too early");
            late.Invoke(layer, null);
            if (!Mathf.Approximately(Time.timeScale, 0.75f)) throw new Exception("Time scale did not return");
            type.GetMethod("BeginSlowMotion", Private).Invoke(layer, null);
            Time.timeScale = 0.9f;
            type.GetMethod("OnDisable", Private).Invoke(layer, null);
            if (!Mathf.Approximately(Time.timeScale, 0.9f)) throw new Exception("External time scale was overwritten");
            Time.timeScale = 1f;
            Debug.Log("[AgentTools] Juice audit passed: blast shake, camera reset, slow motion, time scale reset");
            EditorApplication.update -= Tick;
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Time.timeScale = 1f;
            Debug.LogError("[AgentTools] Juice audit failed: " + e);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(1);
        }
    }
}
