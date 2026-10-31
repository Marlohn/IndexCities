namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 #560: isolate the mandatory recovery/consequence tail for a
    // zero-distance physical decision. A future certificate may avoid only
    // upstream physical evaluation, NEVER this tail or global slice cleanup.
    // True means the canonical caller must continue its inner movement loop;
    // false means it must break. Order and all side effects are unchanged.
    long _pr57BlockedTail560Calls;

    bool Pr57BlockedRecovery560(Vehicle v,Vehicle? blocker,bool enteringLane,bool profile)
    {
        _pr57BlockedTail560Calls++;
        if(Environment.GetEnvironmentVariable("PR57_BLOCKED_TAIL_DIAG")=="1"
            &&_pr57BlockedTail560Calls<=3)
            Console.WriteLine($"PR57_BLOCKED_TAIL_560 calls={_pr57BlockedTail560Calls} vehicle={v.Id} blocker={blocker?.Id}");
        long allocStart;
        if(v.TowPhase==TowPhase.ToVehicle){
            allocStart=profile?GC.GetAllocatedBytesForCurrentThread():0L;
            var recovered=TryTowContraflow(v)||TryTowFromBehind(v);
            if(profile){_pr57BlockedAllocBytes[1]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[1]++;}
            if(recovered)return true;
        }
        if(!enteringLane&&blocker!=null&&blocker.Id!=v.TowTargetId){
            allocStart=profile?GC.GetAllocatedBytesForCurrentThread():0L;
            var bypassed=TryBypassStopped(v,blocker);
            if(profile){_pr57BlockedAllocBytes[2]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[2]++;}
            if(bypassed)return true;
        }
        if(blocker!=null&&TowExitHolds(blocker)){
            allocStart=profile?GC.GetAllocatedBytesForCurrentThread():0L;
            var turned=TryUTurn(v,blocker);
            if(profile){_pr57BlockedAllocBytes[3]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[3]++;}
            if(turned)return true;
        }
        if(blocker!=null){
            allocStart=profile?GC.GetAllocatedBytesForCurrentThread():0L;
            var turned=TowHeldTooLong(v,blocker)&&TryUTurn(v,blocker,TowClosedRoads(v));
            if(profile){_pr57BlockedAllocBytes[4]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[4]++;}
            if(turned){CheckTowFuel(v);return true;}
        }
        allocStart=profile?GC.GetAllocatedBytesForCurrentThread():0L;
        TowWaitsWithoutAccess(v);
        if(profile){_pr57BlockedAllocBytes[5]+=GC.GetAllocatedBytesForCurrentThread()-allocStart;_pr57BlockedCalls[5]++;}
        return false;
    }
}
