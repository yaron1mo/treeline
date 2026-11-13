// Headless FPV waypoint checks: pure Treeline.Sim, no Unity runtime.
using System;
using System.IO;
using Treeline.Sim;

static class FpvWaypointSmoke
{
    static void Check(bool yes, string why) { if (!yes) throw new Exception(why); }
    static Battle Make(string dir)
    {
        var units = UnitTable.Parse(File.ReadAllText(Path.Combine(dir, "units.csv")));
        var armour = ArmourTable.Parse(File.ReadAllText(Path.Combine(dir, "armour.csv")));
        var obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(dir, "obstacles.csv")));
        var rules = BattleRules.Parse(File.ReadAllText(Path.Combine(dir, "battle_rules.csv")));
        var field = new Field { Name = "headless-fpv", Width = 40f, Height = 200f };
        return new Battle(new NavGrid(field, obstacles), units, 5000f,
            new VisionMap(field, obstacles), armour, 1000f, 1, rules);
    }
    static void Main(string[] args)
    {
        var dir = Path.Combine(args.Length > 0 ? args[0] : ".", "unity/Assets/Data");
        var units = UnitTable.Parse(File.ReadAllText(Path.Combine(dir,"units.csv")));
        Check(units.Get("old_tank").Hp == 100f, "old tank HP");
        Check(units.Get("modern_tank").Hp == 100f, "modern tank HP");
        Check(units.Get("ifv").Hp == 100f, "IFV HP");
        Check(units.Get("fpv_operator").HitDamage == 35f, "FPV hit_damage");
        Check(units.Get("fpv_operator").SightRangeM == 20f, "FPV operator sight");
        Check(units.Get("fpv_operator").DroneSightRangeM == 20f, "FPV drone sight");

        var b = Make(dir);
        Defender op; string why;
        Check(b.TryPlace("fpv_operator", 20f, 150f, out op, out why), "place operator");
        Check(!b.TrySetDroneWaypoint(op.Id, -1f, 40f, out why), "reject outside field");
        Check(b.TrySetDroneWaypoint(op.Id, 20f, 80f, out why), "set waypoint: " + why);
        Check(b.QueueWave("regular", 1, 0f, 1f), "queue wave");
        b.StartRound();
        int totalLaunched = 0, lastDrone = -1;
        bool flewToWaypoint = false, foundOwnTarget = false;
        for(int i=0;i<1400 && b.Outcome==BattleOutcome.Ongoing;i++)
        {
            b.Step(0.05f);
            foreach(var dr in b.Drones)
            {
                if(dr.Id != lastDrone) { lastDrone=dr.Id; totalLaunched++; }
                if(dr.UsesWaypoint && Math.Abs(dr.Y-80f)<0.01f) flewToWaypoint=true;
                if(dr.UsesWaypoint && dr.Target!=null) foundOwnTarget=true;
            }
        }
        Check(totalLaunched==1,"expected exactly one FPV launch, got "+totalLaunched);
        Check(flewToWaypoint,"drone did not reach waypoint");
        Check(foundOwnTarget,"drone did not find a target with its own sensor");
        Check(b.Stats.ContainsKey("regular") && b.Stats["regular"].Killed==1,"FPV strike failed");
        Check(b.Outcome==BattleOutcome.Won,"battle did not finish");
        Console.WriteLine("FPV waypoint smoke tests PASS; 100HP tanks, 35 damage, independent 20m drone sensor.");
    }
}
