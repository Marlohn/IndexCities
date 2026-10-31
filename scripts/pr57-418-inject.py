#!/usr/bin/env python3
"""PR57 #418: append one test-only post-repair tow witness in disposable CI checkout.

No production files, rules, clock, actor order, or movement code are modified.
The fixture is synthetic (post-repair parked state), but the tow is requested
and dispatched by the canonical public Advance(2) path, not manually spawned.
"""
from pathlib import Path

path = Path("tests/IndexCities.Simulation.Tests/TowingTests.cs")
source = path.read_text()
assert source.endswith("}\n"), "TowingTests closing brace changed"
assert "Pr57_418_postrepair_parked_car_calls_tow_and_consumes_nonlocal_push" not in source

test = r'''
    // PR57 #418: a synthetic post-repair parked checkpoint, NOT a claim that
    // this fixture was naturally reached by a mechanic. From that checkpoint,
    // TryCallService -> CallTow -> TowReachedDock and the later Push are all
    // executed by public Advance(2). No WaitingSince override or manual tow.
    [Fact] public void Pr57_418_postrepair_parked_car_calls_tow_and_consumes_nonlocal_push()
    {
        var scene=City();var sim=scene.Sim;var car=scene.Car;
        car.Status=VehicleStatus.Parked;
        car.Position=new Point(0,-9);car.Heading=Math.PI/2;car.Fuel=40;
        car.Route.Clear();car.RouteIndex=0;car.DockPosition=null;
        car.DriverId=null;scene.Driver.VehicleId=null;
        car.Service=ServiceKind.Repair;car.ServicePhase=ServicePhase.AwaitingReturn;
        car.ServiceShopId=null;car.ServiceBay=1;car.ServiceSpot=null;
        car.PushPath.Clear();car.PushHeading=null;
        car.TowTruckId=null;car.TowRetryAt=0;car.TowRoadRevision=null;
        // WaitingSince remains the fixture's natural zero, unlike #416.
        Assert.Equal(0,car.WaitingSince);
        var parked=car.Position;
        var callStep=-1;var writeStep=-1;var moveStep=-1;
        for(var step=1;step<=1200;step++)
        {
            sim.Advance(2);
            var tow=scene.Tow;
            if(callStep<0&&car.TowTruckId!=null)callStep=step;
            if(writeStep<0&&tow is {TowPhase:TowPhase.Loading}&&tow.TowTargetId==car.Id)
            {
                writeStep=step;
                // The real nonlocal writer cleared the target's bay. Its
                // PushPath may already be consumed if target followed tow.
                Assert.Null(car.ServiceBay);
                Assert.True(car.PushPath.Count>0||car.Position!=parked,
                    "TowReachedDock did not expose a push or a changed pose");
            }
            if(moveStep<0&&car.Position!=parked)moveStep=step;
            if(writeStep>0&&moveStep>0)break;
        }
        Assert.True(callStep>0,"No canonical tow call: "+car.Cause);
        Assert.True(writeStep>0,"Tow never reached parked target: "+scene.Tow?.Cause);
        Assert.InRange(moveStep,writeStep,writeStep+1);
        Assert.Equal(VehicleStatus.Parked,car.Status);
        sim.CheckInvariants();
        var save=sim.SaveToJson();
        Assert.Equal(save,CitySimulation.FromJson(save).SaveToJson());
        Console.WriteLine($"PR57_418_WITNESS callStep={callStep} writeStep={writeStep} moveStep={moveStep} sameStep={moveStep==writeStep} target={car.Id} tow={scene.Tow?.Id} syntheticPostRepair=True manualTow=False forcedOrder=False");
    }
'''
path.write_text(source[:-2] + test + "\n}\n")
print("PR57_418_INJECTED testOnly=True productionUnchanged=True")
