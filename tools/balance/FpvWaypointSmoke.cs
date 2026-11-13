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
        Check(units.Get("fpv_operator").Damage == 70f, "FPV damage");
        Check(Math.Abs(units.Get("fpv_operator").AttackSpeed - (1f/3f)) < 0.00001f, "3-second FPV cooldown");
        Check(units.Get("fpv_operator").BlastRadiusM == 15f, "FPV blast radius");
        Check(units.Get("fpv_operator").DroneSpeedMps == 35f, "FPV air speed");
        Check(units.Get("mortar").BlastRadiusM == 30f, "Mortar blast");
        Check(units.Get("anti_tank").BlastRadiusM == 5f, "ATGM blast");
        Check(Math.Abs(1f / units.Get("anti_tank").AttackSpeed - 7f) < 0.0001f, "ATGM 7-second interval");
        Check(Math.Abs(Battle.BlastFalloff(0f,15f)-1f)<0.00001f, "blast core");
        Check(Math.Abs(Battle.BlastFalloff(7.5f,15f)-0.5f)<0.00001f, "half-radius blast");
        Check(Math.Abs(Battle.BlastFalloff(15f,15f))<0.00001f, "blast boundary");
        Check(Math.Abs(Battle.BlastFalloff(20f,15f))<0.00001f, "outside blast");
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
        Check(totalLaunched==1,"expected exactly one FPV launched on this single-target wave, got "+totalLaunched);
        Check(flewToWaypoint,"drone did not reach waypoint");
        Check(foundOwnTarget,"drone did not find a target with its own sensor");
        Check(b.Stats.ContainsKey("regular") && b.Stats["regular"].Killed==1,"FPV strike failed");
        Check(b.Outcome==BattleOutcome.Won,"battle did not finish");
        // A separate controlled point-blank impact guarantees we inspect the instant of the explosion.
        var impact = Make(dir);
        Defender impactOp;
        Check(impact.TryPlace("fpv_operator",20f,150f,out impactOp,out why),"place impact test operator");
        Check(impact.TrySetDroneWaypoint(impactOp.Id,20f,80f,out why),"point for impact");
        Check(impact.QueueWave("regular",1,0f,1f),"queue impact target");
        impact.StartRound();
        impact.Step(0.05f);  // launch drone while target is initially far away
        Check(impactOp.DroneState==DroneWeaponState.InFlight,"in flight before impact");
        Check(impactOp.ReloadLeft==0f,"no cooldown while flying");
        Check(impact.Attackers.Count==1,"target spawned for impact");
        impact.Attackers[0].Def.SpeedMps=0f; // freeze target: walking otherwise rewrites manually set coordinates
        impact.Attackers[0].X=20f; impact.Attackers[0].Y=148f;
        for(int i=0;i<20 && impact.Outcome==BattleOutcome.Ongoing;i++) impact.Step(0.05f);
        Check(impact.Stats["regular"].Killed==1,"detonated FPV kills point-blank regular");
        Check(impactOp.DroneState==DroneWeaponState.Reloading,"reload starts on detonation");
        Check(impactOp.ReloadLeft>2.9f,"full 3 seconds start on detonation");

        // Independently verify the shot-down case: no countdown while drone is airborne.
        var interception = Make(dir);
        Defender other;
        Check(interception.TryPlace("fpv_operator",20f,150f,out other,out why),"place operator for interception");
        Check(interception.TrySetDroneWaypoint(other.Id,20f,80f,out why),"waypoint for interception");
        Check(interception.QueueWave("regular",1,0f,1f),"queue interception wave");
        interception.StartRound();
        interception.Step(0.05f);
        Check(other.DroneState==DroneWeaponState.InFlight,"drone in flight must show shooting state");
        Check(other.ReloadLeft==0f,"reload must NOT start at launch");
        Check(interception.Drones.Count==1,"exactly one active drone");
        int shotDroneId=interception.Drones[0].Id;
        Check(interception.TryDestroyDrone(shotDroneId),"drone shot down");
        Check(other.DroneState==DroneWeaponState.Reloading,"shot down starts reload");
        Check(Math.Abs(other.ReloadLeft-3f)<0.0001f,"3 seconds after shootdown");
        for(int i=0;i<55;i++) interception.Step(0.05f);
        Check(other.DroneState==DroneWeaponState.Reloading,"still reloading before 3 sec");
        for(int i=0;i<10;i++) interception.Step(0.05f);
        Check(other.DroneState==DroneWeaponState.InFlight,"new drone ready and launched after reload");
        Console.WriteLine("PASS: 35m/s, 70 damage, 3s post-loss reload, 5m/15m/30m blast radii and linear falloff.");
    }
}
