// Run with tools/unity/unity.ps1 -Method PartAudit.Run.
using System;
using System.Collections;
using System.Reflection;
using Treeline.Render;
using Treeline.Sim;
using UnityEditor;
using UnityEngine;

public static class PartAudit
{
    static BattlefieldUnitLayer layer;
    static double started;
    static int phase;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        phase = 0;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (phase == 0)
            {
                layer = new GameObject("Part audit").AddComponent<BattlefieldUnitLayer>();
                layer.Initialize(200f, 400f);
                typeof(BattlefieldUnitLayer).GetField("bloodEnabled", Private).SetValue(layer, true);
                AddBlast(1);
                started = EditorApplication.timeSinceStartup;
                phase = 1;
                return;
            }
            if (phase == 1 && EditorApplication.timeSinceStartup - started >= 2.0)
            {
                var type = typeof(BattlefieldUnitLayer);
                type.GetMethod("UpdateFlyingParts", Private).Invoke(layer, null);
                var list = (IList)type.GetField("flyingParts", Private).GetValue(layer);
                if (list.Count != 12) throw new Exception("Parts did not stay after landing: " + list.Count);
                foreach (var part in list)
                {
                    var field = part.GetType().GetField("Settled");
                    if (!(bool)field.GetValue(part)) throw new Exception("Part did not settle");
                }
                for (int i = 2; i <= 20; i++) AddBlast(i);
                if (list.Count != 128) throw new Exception("Wrong part cap: " + list.Count);
                type.GetMethod("ClearVisuals", Private).Invoke(layer, null);
                if (list.Count != 0) throw new Exception("Parts did not clear at battle end");
                Debug.Log("[AgentTools] Part audit passed: landing, persistence, cap 128, cleanup");
                EditorApplication.update -= Tick;
                EditorApplication.Exit(0);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[AgentTools] Part audit failed: " + e);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(1);
        }
    }

    static void AddBlast(int key)
    {
        var type = typeof(BattlefieldUnitLayer);
        object visual = type.GetMethod("Spawn", Private).Invoke(layer, new object[] { "recruit", key, Vector2.zero, true });
        var attacker = new Attacker { X = 0f, Y = 0f, FatalBlastRadius = 12f, FatalOriginX = 1f, FatalOriginY = 1f };
        type.GetMethod("BeginBreakApart", Private).Invoke(layer, new object[] { visual, key, attacker });
    }
}
