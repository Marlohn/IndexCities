#!/usr/bin/env python3
"""PR57 #481: exact observed-input equality of canonical CanTurnNow body prefixes.

Only injected in a disposable CI checkout. No result reuse, no skipped work.
Full persisted actor/candidate JSON strings and exact pose bits are compared;
roads, transient writers, signals and other external inputs are NOT covered.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]

def once(path, old, new):
    text = path.read_text(encoding="utf-8")
    assert text.count(old) == 1, (path, old[:120], text.count(old))
    path.write_text(text.replace(old, new, 1), encoding="utf-8")

mob = root / "src/IndexCities.Simulation/Mobility.cs"
once(mob, "&&CanTurnNow(o)).ToList();", "&&Pr57481PreferenceCanTurn(o)).ToList();")
once(mob, "        Pr57Stage249EndSlice();",
     "        Pr57Stage249EndSlice();\n        Pr57481EndSlice();")

shared = root / "src/IndexCities.Simulation/Mobility.SharedSpace.cs"
once(shared, "    bool CanTurnNow(Vehicle o)\n    {", """    // #481: strictly observational, no cache and no decision changes.
    static readonly bool Pr57481Enabled =
        Environment.GetEnvironmentVariable("PR57_481_PREFIX_READS") == "1";
    sealed class Pr57481Trace
    {
        public string Actor = "", Poses = "", ReadActor = "", Entry = "";
        public readonly System.Text.StringBuilder Candidates = new(), ReadCandidates = new();
        public int Visits, Blocker;
        public long TimeBits;
    }
    readonly Dictionary<int,Pr57481Trace> _pr57481Previous = new();
    readonly Dictionary<int,Pr57481Trace> _pr57481Current = new();
    Pr57481Trace? _pr57481Active;
    bool _pr57481InPreference, _pr57481CaptureBody;
    long _pr57481Slices, _pr57481Calls, _pr57481Pairs,
         _pr57481SameActor, _pr57481SamePoses, _pr57481SameCandidates,
         _pr57481SameObserved, _pr57481SameBlocker, _pr57481SameTime,
         _pr57481SameVisitCount, _pr57481AllVisits, _pr57481MatchedVisits,
         _pr57481SameReadActor, _pr57481SameReadCandidates, _pr57481SameEntry,
         _pr57481SameReadObserved, _pr57481FullChangedReadSame, _pr57481ReadChanged,
         _pr57481ReadMatchedVisits;
    bool Pr57481PreferenceCanTurn(Vehicle o)
    {
        if (!Pr57481Enabled) return CanTurnNow(o);
        var old = _pr57481InPreference;
        _pr57481InPreference = true;
        try { return CanTurnNow(o); }
        finally { _pr57481InPreference = old; }
    }
    // #481: effective values read by BodyOnWay guards and Penetration.
    // NOT a complete dependency closure for CanTurnNow, TurnWay, or external writers.
    static long Pr57481Bits(double x) => BitConverter.DoubleToInt64Bits(x);
    string Pr57481Read(Vehicle v) =>
        $"{v.Id}:{v.TowTargetId}:{v.TowTruckId}:{(int)v.Status}:" +
        $"{Pr57481Bits(v.Position.X)}:{Pr57481Bits(v.Position.Z)}:" +
        $"{Pr57481Bits(v.Heading)}:{Pr57481Bits(BodyHalfLength(v))}:" +
        $"{Pr57481Bits(BodyHalfWidth(v))}";
    void Pr57481Begin(Vehicle o, List<(Vehicle Body,Point Position,double Heading)> way)
    {
        if (!Pr57481Enabled || !_pr57481InPreference) return;
        var poses = new System.Text.StringBuilder();
        foreach (var p in way)
            poses.Append(BitConverter.DoubleToInt64Bits(p.Position.X)).Append(',')
                .Append(BitConverter.DoubleToInt64Bits(p.Position.Z)).Append(',')
                .Append(BitConverter.DoubleToInt64Bits(p.Heading)).Append(';');
        _pr57481Active = new Pr57481Trace
        {
            Actor = Pr57MutationSnapshot(o),
            ReadActor = Pr57481Read(o),
            Entry = $"{Pr57481Bits(State.Entry.X)}:{Pr57481Bits(State.Entry.Z)}",
            Poses = poses.ToString(),
            TimeBits = BitConverter.DoubleToInt64Bits(VehicleMovementTime)
        };
    }
    Vehicle? Pr57481BodyOnWay(Vehicle o, List<(Vehicle Body,Point Position,double Heading)> way)
    {
        if (_pr57481Active is null) return BodyOnWay(o,way);
        var old = _pr57481CaptureBody;
        _pr57481CaptureBody = true;
        try { return BodyOnWay(o,way); }
        finally { _pr57481CaptureBody = old; }
    }
    void Pr57481Finish(Vehicle o, Vehicle? blocker)
    {
        var trace = _pr57481Active;
        _pr57481Active = null;
        if (trace is null) return;
        trace.Blocker = blocker?.Id ?? -1;
        _pr57481Calls++;
        _pr57481AllVisits += trace.Visits;
        if (!_pr57481Current.ContainsKey(o.Id) &&
            _pr57481Previous.TryGetValue(o.Id, out var prior))
        {
            _pr57481Pairs++;
            var actorSame = string.Equals(prior.Actor, trace.Actor, StringComparison.Ordinal);
            var posesSame = string.Equals(prior.Poses, trace.Poses, StringComparison.Ordinal);
            var candidatesSame = string.Equals(prior.Candidates.ToString(),
                trace.Candidates.ToString(), StringComparison.Ordinal);
            if (actorSame) _pr57481SameActor++;
            if (posesSame) _pr57481SamePoses++;
            if (candidatesSame) _pr57481SameCandidates++;
            if (prior.Visits == trace.Visits) _pr57481SameVisitCount++;
            var readActorSame = string.Equals(prior.ReadActor,trace.ReadActor,StringComparison.Ordinal);
            var readCandidatesSame = string.Equals(prior.ReadCandidates.ToString(),
                trace.ReadCandidates.ToString(),StringComparison.Ordinal);
            var entrySame = string.Equals(prior.Entry,trace.Entry,StringComparison.Ordinal);
            if(readActorSame) _pr57481SameReadActor++;
            if(readCandidatesSame) _pr57481SameReadCandidates++;
            if(entrySame) _pr57481SameEntry++;
            var readSame = readActorSame && posesSame && readCandidatesSame && entrySame;
            if(readSame){_pr57481SameReadObserved++;_pr57481ReadMatchedVisits+=trace.Visits;}
            else _pr57481ReadChanged++;
            if(readSame && !(actorSame && posesSame && candidatesSame))
                _pr57481FullChangedReadSame++;
            if (prior.Blocker == trace.Blocker) _pr57481SameBlocker++;
            if (prior.TimeBits == trace.TimeBits) _pr57481SameTime++;
            if (actorSame && posesSame && candidatesSame)
            {
                _pr57481SameObserved++;
                _pr57481MatchedVisits += trace.Visits;
            }
        }
        // Keep LAST invocation per actor for the next physical slice.
        _pr57481Current[o.Id] = trace;
    }
    void Pr57481EndSlice()
    {
        if (!Pr57481Enabled) return;
        _pr57481Slices++;
        _pr57481Previous.Clear();
        foreach (var pair in _pr57481Current)
            _pr57481Previous[pair.Key] = pair.Value;
        _pr57481Current.Clear();
        Console.WriteLine($"PR57_481_SUM slices={_pr57481Slices} calls={_pr57481Calls} pairs={_pr57481Pairs} sameActor={_pr57481SameActor} samePoses={_pr57481SamePoses} sameCandidates={_pr57481SameCandidates} sameObserved={_pr57481SameObserved} sameBlocker={_pr57481SameBlocker} sameTime={_pr57481SameTime} sameVisitCount={_pr57481SameVisitCount} allVisits={_pr57481AllVisits} matchedVisits={_pr57481MatchedVisits} sameReadActor={_pr57481SameReadActor} sameReadCandidates={_pr57481SameReadCandidates} sameEntry={_pr57481SameEntry} sameReadObserved={_pr57481SameReadObserved} fullChangedReadSame={_pr57481FullChangedReadSame} readChanged={_pr57481ReadChanged} readMatchedVisits={_pr57481ReadMatchedVisits} observational=True exactDependencyClosure=False");
    }
    bool CanTurnNow(Vehicle o)
    {""")
once(shared, "        var turn=TurnWay(o,out _);\n",
     "        var turn=TurnWay(o,out _);\n        Pr57481Begin(o,turn);\n")
once(shared, "        var body=BodyOnWay(o,turn);\n",
     "        var body=Pr57481BodyOnWay(o,turn);\n        Pr57481Finish(o,body);\n")
once(shared, """        foreach(var o in State.Vehicles){
            if(census){visited++;Pr57306Read(v,o);}""", """        foreach(var o in State.Vehicles){
            if(_pr57481CaptureBody && _pr57481Active is { } trace)
            {
                trace.Visits++;
                trace.Candidates.Append('|').Append(o.Id).Append(':')
                    .Append(Pr57MutationSnapshot(o));
                trace.ReadCandidates.Append('|').Append(o.Id).Append(':')
                    .Append(Pr57481Read(o));
            }
            if(census){visited++;Pr57306Read(v,o);}""")
print("PR57_481_INJECTED observational=True noCache=True noSkip=True")
