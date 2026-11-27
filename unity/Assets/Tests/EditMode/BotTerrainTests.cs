using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // Issue #49 (2026-11-28): the scripted players in tools/balance score houses and hill tops as candidate
    // spots for defenders. The tools are not part of the Sim assembly, so this test compiles
    // tools/balance/Routes.cs at run time (the exact file WaveCost and PlaytestRun use) and drives it with the
    // Sim the way WaveCost.Put and PlaytestRun.Place do: terrain spots with a score above 1 first, taken
    // houses skipped, lane points otherwise.
    public class BotTerrainTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        static Assembly routesAsm;
        static Assembly RoutesAsm => routesAsm ?? (routesAsm = CompileRoutes());

        static Assembly CompileRoutes()
        {
            string path = Path.Combine(RepoRoot, "tools", "balance", "Routes.cs");
            Assert.IsTrue(File.Exists(path), "Routes.cs not found: " + path);
            var parms = new CompilerParameters { GenerateInMemory = true, GenerateExecutable = false };
            // The Sim assembly, plus the netstandard facade when the host uses it (Unity compiles against
            // netstandard 2.1; under plain Mono the compiler adds the standard references itself).
            parms.ReferencedAssemblies.Add(typeof(NavGrid).Assembly.Location);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                if (a.GetName().Name == "netstandard" && !string.IsNullOrEmpty(a.Location))
                {
                    parms.ReferencedAssemblies.Add(a.Location);
                    break;
                }
            CompilerResults res = CodeDomProvider.CreateProvider("CSharp").CompileAssemblyFromFile(parms, path);
            var errors = new System.Text.StringBuilder();
            foreach (CompilerError e in res.Errors)
                if (!e.IsWarning) errors.AppendLine(e.ToString());
            if (errors.Length > 0)
                throw new Exception("tools/balance/Routes.cs did not compile:\n" + errors);
            return res.CompiledAssembly;
        }

        static object CallRoutes(string method, int nArgs, params object[] args)
        {
            foreach (var m in RoutesAsm.GetType("Routes").GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (m.Name == method && m.GetParameters().Length == nArgs)
                    return m.Invoke(null, args);
            throw new MissingMethodException("Routes." + method + "/" + nArgs);
        }

        static Pt[] Rect(float x, float y, float w, float h) =>
            new[] { new Pt(x, y), new Pt(x + w, y), new Pt(x + w, y + h), new Pt(x, y + h) };

        // A small field whose routes run straight down from the top edge (no spawns), with the house or hill
        // beside the middle route inside the placing band (60%-90% of the height, as in WaveCost).
        static Battle MakeField(string featureType, out Field field, out VisionMap vision, out NavGrid grid)
        {
            field = new Field { Name = "bot-terrain", Width = 100f, Height = 300f };
            field.Features.Add(new Feature { Type = featureType, Poly = Rect(45f, 200f, 10f, 8f) });
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            grid = new NavGrid(field, obstacles);
            vision = new VisionMap(field, obstacles);
            var b = new Battle(grid, UnitTable.Parse(Data("units.csv")), 5000f, vision,
                ArmourTable.Parse(Data("armour.csv")), 1000f, 1, BattleRules.Parse(Data("battle_rules.csv")));
            b.PlacementMinY = 0f;
            return b;
        }

        class Spot
        {
            public float X, Y, Score;
            public int House;
        }

        // Routes.TerrainSpots for this defender type, read back as plain values.
        static List<Spot> Spots(string key, Field field, VisionMap vision, NavGrid grid, UnitTable units, string defId)
        {
            object lanes = CallRoutes("Of", 4, key, grid, field.Height * 0.6f, field.Height * 0.9f);
            object raw = CallRoutes("TerrainSpots", 7, key, field, vision, lanes, units.Get(defId),
                field.Height * 0.6f, field.Height * 0.9f);
            var list = new List<Spot>();
            foreach (object s in (IEnumerable)raw)
            {
                var t = s.GetType();
                list.Add(new Spot
                {
                    X = (float)t.GetField("X").GetValue(s),
                    Y = (float)t.GetField("Y").GetValue(s),
                    House = (int)t.GetField("House").GetValue(s),
                    Score = (float)t.GetField("Score").GetValue(s),
                });
            }
            return list;
        }

        [Test]
        public void HouseNextToTheRoute_TheScriptedPlayerPutsAnInfantryDefenderInIt()
        {
            var b = MakeField("house", out var field, out var vision, out var grid);
            var units = UnitTable.Parse(Data("units.csv"));
            Assert.AreEqual(1, vision.Houses.Count);
            var spots = Spots("house-test", field, vision, grid, units, "rifle_squad");
            Assert.IsTrue(spots.Count > 0, "no terrain spots");
            Assert.AreEqual(0, spots[0].House, "the best spot for a rifle squad is the house");
            Assert.Greater(spots[0].Score, 1f, "the house scores above a lane point");
            // The player places the defender on the best spot (the first steps of WaveCost.Put).
            Assert.IsTrue(b.TryPlace("rifle_squad", spots[0].X, spots[0].Y, out var d, out string why), why);
            Assert.AreEqual(0, d.House, "the defender stands in the house");
            Assert.AreEqual(d.Id, b.HouseOccupant(0));
        }

        [Test]
        public void HouseNextToTheRoute_LongRangeTypesAndReconStayOnTheLanePoints()
        {
            MakeField("house", out var field, out var vision, out var grid);
            var units = UnitTable.Parse(Data("units.csv"));
            foreach (var id in new[] { "sniper", "anti_tank", "mortar", "recon_drone" })
            {
                var spots = Spots("house-lr-" + id, field, vision, grid, units, id);
                foreach (var s in spots)
                    Assert.LessOrEqual(s.Score, 1f, id + " must not prefer the house");
            }
        }

        [Test]
        public void HillTopThatSeesTheRoute_LongRangeTypesAndReconGetIt_InfantryDoesNot()
        {
            var b = MakeField("hill", out var field, out var vision, out var grid);
            var units = UnitTable.Parse(Data("units.csv"));
            foreach (var id in new[] { "sniper", "anti_tank", "recon_drone" })
            {
                var spots = Spots("hill-lr-" + id, field, vision, grid, units, id);
                Assert.IsTrue(spots.Count > 0, id + ": no terrain spots");
                Assert.AreEqual(-1, spots[0].House);
                Assert.AreEqual("hill", vision.ObstacleAt(spots[0].X, spots[0].Y).Id, id + ": spot on the hill top");
                Assert.Greater(spots[0].Score, 1f, id + " should take the hill top");
            }
            foreach (var s in Spots("hill-inf", field, vision, grid, units, "rifle_squad"))
                Assert.LessOrEqual(s.Score, 1f, "infantry must not prefer the hill top");
            // The player can stand on the top and gets the on-top sight bonus there (obstacles.csv hill).
            var top = Spots("hill-top", field, vision, grid, units, "sniper")[0];
            Assert.IsTrue(b.TryPlace("sniper", top.X, top.Y, out var d, out string why), why);
            Assert.AreEqual(top.X, d.X, 0.01f);
            Assert.AreEqual(top.Y, d.Y, 0.01f);
            Assert.AreEqual(65f * 1.3f, vision.SightRadius(d.X, d.Y, 65f), 0.01f, "hill on_sight_mult");
        }

        [Test]
        public void TerrainSpots_AreDeterministic_SameFieldAndTypeGiveTheSameOrder()
        {
            MakeField("house", out var field, out var vision, out var grid);
            var units = UnitTable.Parse(Data("units.csv"));
            var a = Spots("det-a", field, vision, grid, units, "rifle_squad");
            var c = Spots("det-b", field, vision, grid, units, "rifle_squad");
            Assert.AreEqual(a.Count, c.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].X, c[i].X, 0.001f, "X of spot " + i);
                Assert.AreEqual(a[i].Y, c[i].Y, 0.001f, "Y of spot " + i);
                Assert.AreEqual(a[i].House, c[i].House, "house of spot " + i);
                Assert.AreEqual(a[i].Score, c[i].Score, 0.001f, "score of spot " + i);
            }
        }
    }
}
