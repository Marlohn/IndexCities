#!/usr/bin/env python3
"""#410 observational scalar-only ordinary-leader shadow after #408; disposable checkout only.

Deliberately omits OccupiesLane/BodyAcrossLane/BlocksEntry and cannot replace
the canonical scan. It tests the scalar-only hypothesis's false decisions.
"""
from pathlib import Path
p = Path("src/IndexCities.Simulation/Mobility.cs")
s = p.read_text()
def once(old, new):
    global s
    count = s.count(old)
    assert count == 1, (old[:110], count)
    s = s.replace(old, new, 1)

once('    static readonly bool Pr57408Enabled=Environment.GetEnvironmentVariable("PR57_408_WINNER_CAUSE")=="1";', '''    static readonly bool Pr57410Enabled=Environment.GetEnvironmentVariable("PR57_410_SCALAR_SHADOW")=="1";
    long _pr57410Scans,_pr57410Candidates,_pr57410Eligible,_pr57410OrdinaryGeometry;
    long _pr57410BindingOrdinary,_pr57410BindingOrdinaryExact,_pr57410BindingOrdinarySameId;
    long _pr57410StepMismatch,_pr57410FalseBinding,_pr57410MissedBinding,_pr57410NoWinner;
    long _pr57410Witnesses;
    static readonly bool Pr57408Enabled=Environment.GetEnvironmentVariable("PR57_408_WINNER_CAUSE")=="1";''')

once('                void Pr57ScanLane(IEnumerable<Vehicle> candidates){', '''                var pr57410Sample=Pr57410Enabled&&pr57408Sample;
                var pr57410Distance=double.MaxValue;
                var pr57410Id=-1;
                void Pr57410Consider(Vehicle other){
                    _pr57410Candidates++;
                    // Scalar-only hypothesis, not a replacement for canonical predicates.
                    if(other.Id==v.Id||other.Status is not (VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded))return;
                    _pr57410Eligible++;
                    var relative=other.Position-v.Position;
                    var sn=pr57FastScan?pr57Sin:Math.Sin(v.Heading);
                    var cs=pr57FastScan?pr57Cos:Math.Cos(v.Heading);
                    var forward=relative.X*sn+relative.Z*cs;
                    if(!(forward>0))return;
                    var side=Math.Abs(relative.X*cs-relative.Z*sn);
                    if(!(side<2.6))return;
                    if(!(Math.Cos(other.Heading-v.Heading)>.5))return;
                    var back=-relative.X*Math.Sin(other.Heading)-relative.Z*Math.Cos(other.Heading);
                    var backSide=Math.Abs(-relative.X*Math.Cos(other.Heading)+relative.Z*Math.Sin(other.Heading));
                    if(back>0&&backSide<2.6&&other.Id>v.Id&&other.Status!=VehicleStatus.Stranded)return;
                    _pr57410OrdinaryGeometry++;
                    if(forward<pr57410Distance){pr57410Distance=forward;pr57410Id=other.Id;}
                }
                void Pr57ScanLane(IEnumerable<Vehicle> candidates){''')

once('                    Pr57ActorShadowRead(v,other,ReferenceEquals(other,liveOther));', '''                    Pr57ActorShadowRead(v,other,ReferenceEquals(other,liveOther));
                    if(pr57410Sample)Pr57410Consider(other);''')

once('                Pr57CandidateEndQuery();\n                if(blocker is not null)Pr57CausalWinner(v,blocker);', '''                Pr57CandidateEndQuery();
                if(pr57410Sample){
                    _pr57410Scans++;
                    var nominal=Math.Min(d,speed*remaining);
                    var canonicalStep=Math.Min(nominal,Math.Max(0,leaderDistance-8));
                    var scalarStep=Math.Min(nominal,Math.Max(0,pr57410Distance-8));
                    var canonicalBinding=blocker is not null&&Math.Max(0,leaderDistance-8)<nominal;
                    var scalarBinding=pr57410Id>=0&&Math.Max(0,pr57410Distance-8)<nominal;
                    if(pr57410Id<0)_pr57410NoWinner++;
                    if(scalarBinding&&!canonicalBinding)_pr57410FalseBinding++;
                    if(canonicalBinding&&!scalarBinding)_pr57410MissedBinding++;
                    if(BitConverter.DoubleToInt64Bits(canonicalStep)!=BitConverter.DoubleToInt64Bits(scalarStep)){
                        _pr57410StepMismatch++;
                        if(_pr57410Witnesses++<16)
                            Console.WriteLine($"PR57_410_WITNESS scan={_pr57410Scans} reader={v.Id} cause={pr57408Cause} canonicalId={blocker?.Id??-1} scalarId={pr57410Id} canonicalDistance={leaderDistance:R} scalarDistance={pr57410Distance:R} nominal={nominal:R} canonicalStep={canonicalStep:R} scalarStep={scalarStep:R} entering={enteringLane} wait={pr57408Wait} scope=PRE_SAFETY_ONLY");
                    }
                    if(canonicalBinding&&pr57408Cause==6){
                        _pr57410BindingOrdinary++;
                        if(pr57410Id==blocker?.Id){
                            _pr57410BindingOrdinarySameId++;
                            if(BitConverter.DoubleToInt64Bits(pr57410Distance)==BitConverter.DoubleToInt64Bits(leaderDistance))_pr57410BindingOrdinaryExact++;
                        }
                    }
                }
                if(blocker is not null)Pr57CausalWinner(v,blocker);''')

once('        if(pr57408Sample&&_pr57408Physical==600&&!_pr57408Printed){', '''        if(pr57408Sample&&_pr57408Physical==600&&Pr57410Enabled)
            Console.WriteLine($"PR57_410_SUMMARY scans={_pr57410Scans} candidates={_pr57410Candidates} eligible={_pr57410Eligible} geometry={_pr57410OrdinaryGeometry} bindingOrdinary={_pr57410BindingOrdinary} bindingOrdinarySameId={_pr57410BindingOrdinarySameId} bindingOrdinaryExact={_pr57410BindingOrdinaryExact} stepMismatch={_pr57410StepMismatch} falseBinding={_pr57410FalseBinding} missedBinding={_pr57410MissedBinding} noWinner={_pr57410NoWinner} witnesses={Math.Min(16,_pr57410Witnesses)} observational=True noSkippedSlices=True causalClosure=False scalarOnly=True");
        if(pr57408Sample&&_pr57408Physical==600&&!_pr57408Printed){''')
p.write_text(s)
print("PR57_410_INJECTED observational=True scalarOnly=True noSkippedSlices=True")
