using NUnit.Framework;
using Treeline.Sim;

namespace Treeline.Tests
{
    public class NavTests
    {
        static Field Blank(float w = 40f, float h = 40f) => new Field { Name = "test", Width = w, Height = h };

        [Test]
        public void OpenField_EveryTopCellReachesTheRearEdge()
        {
            var grid = new NavGrid(Blank());
            var flow = new FlowField(grid, UnitClass.Infantry);
            for (int c = 0; c < grid.Cols; c++)
            {
                var path = flow.Trace(c);
                Assert.IsNotEmpty(path);
                Assert.AreEqual(grid.Rows - 1, path[path.Count - 1] / grid.Cols);
            }
        }

        [Test]
        public void RiverAcrossTheField_BlocksVehiclesButNotInfantry()
        {
            var field = Blank();
            field.Features.Add(new Feature { Type = "river", Width = 6f, Line = new[] { new Pt(-5, 20), new Pt(45, 20) } });
            var grid = new NavGrid(field);
            Assert.IsFalse(new FlowField(grid, UnitClass.Vehicle).Reachable(grid.CellAt(20, 1)));
            Assert.IsTrue(new FlowField(grid, UnitClass.Infantry).Reachable(grid.CellAt(20, 1)));
        }

        [Test]
        public void FordInTheRiver_LetsVehiclesThrough()
        {
            var field = Blank();
            field.Features.Add(new Feature { Type = "river", Width = 6f, Line = new[] { new Pt(-5, 20), new Pt(45, 20) } });
            field.Features.Add(new Feature { Type = "ford", Poly = new[] { new Pt(14, 14), new Pt(26, 14), new Pt(26, 26), new Pt(14, 26) } });
            var grid = new NavGrid(field);
            Assert.IsTrue(new FlowField(grid, UnitClass.Vehicle).Reachable(grid.CellAt(20, 1)));
        }

        [Test]
        public void ParsesTheShippedFieldFormat()
        {
            const string json = "{\"name\":\"T\",\"size\":[200,400],\"features\":[{\"type\":\"belt\",\"line\":[[0,10],[50,12.5]],\"width\":14},{\"type\":\"pond\",\"poly\":[[1,1],[5,1],[5,5]]}]}";
            var f = FieldParser.Parse(json);
            Assert.AreEqual("T", f.Name);
            Assert.AreEqual(400f, f.Height);
            Assert.AreEqual(2, f.Features.Count);
            Assert.AreEqual(14f, f.Features[0].Width);
            Assert.AreEqual(3, f.Features[1].Poly.Length);
        }
    }
}
