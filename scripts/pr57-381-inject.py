#!/usr/bin/env python3
"""PR57 #381: distinguish operational dt=0 calls from physical reader-gate slices.

Apply after #367..#379 in a disposable checkout. Observational only: original
predicate, actor order, sub-slices, Advance(2), economy and saves are unchanged.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "src/IndexCities.Simulation"
mobility = root / "Mobility.cs"
bitset = root / "Mobility.BitsetReader377.Pr57.cs"

def patch(path, old, new):
    source = path.read_text(encoding="utf-8")
    assert source.count(old) == 1, (path, old, source.count(old))
    path.write_text(source.replace(old, new), encoding="utf-8")

# #374 counts *every* MoveVehiclesUnsplit, including the operational
# MoveVehiclesUnsplit(0, dt) before physical slices in a batched Step.
# Close the prior slice BEFORE #377 rotates its current reader masks.
patch(mobility, "        Pr57374BeginSlice();",
      "        Pr57374BeginSlice();\n        Pr57381BeginSlice(dt,operationsDt);")
patch(bitset, "        Pr57379Finish();",
      "        Pr57379Finish();\n        Pr57381Finish();")

out = root / "Mobility.ReaderRegime381.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Numerics;

namespace IndexCities.Simulation;

// Classify the first 600 calls to MoveVehiclesUnsplit by their actual dt,
// operationsDt and batch state. Presence is read from #377's already-collected
// PARTIAL OccupiesLane masks; no new candidate traversal or predicate calls.
// This is NOT a complete causal dependency signature.
public sealed partial class CitySimulation
{
    static readonly bool Pr57381Enabled =
        Environment.GetEnvironmentVariable("PR57_381_READER_REGIME")=="1";
    bool _pr57381HaveCurrent;
    bool _pr57381DtZero, _pr57381Operations, _pr57381BatchNull;
    int _pr57381BatchCount, _pr57381Travelling, _pr57381Eligible;
    long _pr57381Slices, _pr57381DtZeroSlices, _pr57381MotionSlices;
    long _pr57381OpsSlices, _pr57381BatchNullSlices, _pr57381BatchEmptySlices;
    long _pr57381Zero, _pr57381ZeroDtZero, _pr57381ZeroMotion;
    long _pr57381ZeroOps, _pr57381ZeroBatchNull, _pr57381ZeroBatchEmpty;
    long _pr57381ZeroNoTravelling, _pr57381ZeroNoEligible, _pr57381ZeroWithEligible;
    long _pr57381ReaderOccurrences, _pr57381OpsReaders, _pr57381MotionReaders;
    long _pr57381ZeroBatchNonempty, _pr57381ZeroMotionBatchNonempty;
    int _pr57381MaxReaders;

    void Pr57381Close()
    {
        if(!_pr57381HaveCurrent)return;
        ulong lo=0,hi=0;
        foreach(var mask in _pr57377Current.Values)
        {
            lo|=mask.Lo;
            hi|=mask.Hi;
        }
        int readers=BitOperations.PopCount(lo)+BitOperations.PopCount(hi);
        _pr57381Slices++;
        _pr57381ReaderOccurrences+=readers;
        _pr57381MaxReaders=Math.Max(_pr57381MaxReaders,readers);
        if(_pr57381DtZero)_pr57381DtZeroSlices++;
        else _pr57381MotionSlices++;
        if(_pr57381Operations){_pr57381OpsSlices++;_pr57381OpsReaders+=readers;}
        else _pr57381MotionReaders+=readers;
        if(_pr57381BatchNull)_pr57381BatchNullSlices++;
        else if(_pr57381BatchCount==0)_pr57381BatchEmptySlices++;
        if(readers==0)
        {
            _pr57381Zero++;
            if(_pr57381DtZero)_pr57381ZeroDtZero++;
            else _pr57381ZeroMotion++;
            if(_pr57381Operations)_pr57381ZeroOps++;
            if(_pr57381BatchNull)_pr57381ZeroBatchNull++;
            else if(_pr57381BatchCount==0)_pr57381ZeroBatchEmpty++;
            else
            {
                _pr57381ZeroBatchNonempty++;
                if(!_pr57381DtZero)_pr57381ZeroMotionBatchNonempty++;
            }
            if(_pr57381Travelling==0)_pr57381ZeroNoTravelling++;
            if(_pr57381Eligible==0)_pr57381ZeroNoEligible++;
            else _pr57381ZeroWithEligible++;
        }
        _pr57381HaveCurrent=false;
    }

    void Pr57381BeginSlice(double dt,double? operationsDt)
    {
        if(!Pr57381Enabled||!_pr57377Active)return;
        // #374 has already advanced its call counter; #377 has NOT rotated
        // its masks. Thus Current still represents the preceding call.
        Pr57381Close();
        if(_pr57374PhysicalSlices>Pr57377SliceLimit)return;
        _pr57381HaveCurrent=true;
        _pr57381DtZero=dt<=0;
        _pr57381Operations=operationsDt.HasValue;
        _pr57381BatchNull=_vehicleMovementBatch is null;
        _pr57381BatchCount=_vehicleMovementBatch?.Count??-1;
        _pr57381Travelling=0;
        _pr57381Eligible=0;
        foreach(var v in State.Vehicles)
        {
            if(v.Status!=VehicleStatus.Travelling)continue;
            _pr57381Travelling++;
            if(_vehicleMovementBatch is null||operationsDt.HasValue||
               _vehicleMovementBatch.Contains(v.Id))
                _pr57381Eligible++;
        }
    }

    void Pr57381Finish()
    {
        if(!Pr57381Enabled)return;
        Pr57381Close();
        if(_pr57381Slices!=600||_pr57381Zero!=83||
           _pr57381ReaderOccurrences!=13690)
            throw new InvalidOperationException("#381 reader regime / #379 mismatch");
        Console.WriteLine($"PR57_381_SUMMARY calls={_pr57381Slices} "+
            $"dtZero={_pr57381DtZeroSlices} dtPositive={_pr57381MotionSlices} "+
            $"operationsNonNull={_pr57381OpsSlices} operationsNull={_pr57381Slices-_pr57381OpsSlices} "+
            $"batchNull={_pr57381BatchNullSlices} batchEmpty={_pr57381BatchEmptySlices} "+
            $"zeroReaders={_pr57381Zero} zeroDtZero={_pr57381ZeroDtZero} "+
            $"zeroDtPositive={_pr57381ZeroMotion} zeroOperations={_pr57381ZeroOps} "+
            $"zeroBatchNull={_pr57381ZeroBatchNull} zeroBatchEmpty={_pr57381ZeroBatchEmpty} "+
            $"zeroBatchNonempty={_pr57381ZeroBatchNonempty} "+
            $"zeroMotionBatchNonempty={_pr57381ZeroMotionBatchNonempty} "+
            $"zeroNoTravelling={_pr57381ZeroNoTravelling} "+
            $"zeroNoEligible={_pr57381ZeroNoEligible} "+
            $"zeroWithEligible={_pr57381ZeroWithEligible} "+
            $"readerOccurrences={_pr57381ReaderOccurrences} "+
            $"opsReaders={_pr57381OpsReaders} motionReaders={_pr57381MotionReaders} "+
            $"maxReaders={_pr57381MaxReaders} "+
            "scope=FIRST_600_UNSPLIT_CALLS_REAL_PARTIAL_OCCUPIES "+
            "noCausalClosure=True noDecisionReuse=True noSkippedSlices=True observational=True");
    }
}
''', encoding="utf-8")
print("PR57_381_INJECTED observational dt/operationsDt/batch reader-regime classifier")
