#!/usr/bin/env python3
"""PR57 #480: exact observed-input equality of canonical CanTurnNow body prefixes.

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
once(mob, "&&CanTurnNow(o)).ToList();", "&&Pr57480PreferenceCanTurn(o)).ToList();")
once(mob, "        Pr57Stage249EndSlice();",
     "        Pr57Stage249EndSlice();\n        Pr57480EndSlice();")

shared = root / "src/IndexCities.Simulation/Mobility.SharedSpace.cs"
once(shared, "    bool CanTurnNow(Vehicle o)\n    {", """    // #480: strictly observational, no cache and no decision changes.
    static readonly bool Pr57480Enabled =
        Environment.GetEnvironmentVariable("PR57_480_PREFIX_INPUTS") == "1";
    sealed class Pr57480Trace
    {
        public string Actor = "", Poses = "";
        public readonly System.Text.StringBuilder Candidates = new();
        public int Visits, Blocker;
        public long TimeBits;
    }
    readonly Dictionary<int,Pr57480Trace> _pr57480Previous = new();
    readonly Dictionary<int,Pr57480Trace> _pr57480Current = new();
    Pr57480Trace? _pr57480Active;
    bool _pr57480InPreference, _pr57480CaptureBody;
    long _pr57480Slices, _pr57480Calls, _pr57480Pairs,
         _pr57480SameActor, _pr57480SamePoses, _pr57480SameCandidates,
         _pr57480SameObserved, _pr57480SameBlocker, _pr57480SameTime,
         _pr57480SameVisitCount, _pr57480AllVisits, _pr57480MatchedVisits;
    bool Pr57480PreferenceCanTurn(Vehicle o)
    {
        if (!Pr57480Enabled) return CanTurnNow(o);
        var old = _pr57480InPreference;
        _pr57480InPreference = true;
        try { return CanTurnNow(o); }
        finally { _pr57480InPreference = old; }
    }
    void Pr57480Begin(Vehicle o, List<(Vehicle Body,Point Position,double Heading)> way)
    {
        if (!Pr57480Enabled || !_pr57480InPreference) return;
        var poses = new System.Text.StringBuilder();
        foreach (var p in way)
            poses.Append(BitConverter.DoubleToInt64Bits(p.Position.X)).Append(',')
                .Append(BitConverter.DoubleToInt64Bits(p.Position.Z)).Append(',')
                .Append(BitConverter.DoubleToInt64Bits(p.Heading)).Append(';');
        _pr57480Active = new Pr57480Trace
        {
            Actor = Pr57MutationSnapshot(o),
            Poses = poses.ToString(),
            TimeBits = BitConverter.DoubleToInt64Bits(VehicleMovementTime)
        };
    }
    Vehicle? Pr57480BodyOnWay(Vehicle o, List<(Vehicle Body,Point Position,double Heading)> way)
    {
        if (_pr57480Active is null) return BodyOnWay(o,way);
        var old = _pr57480CaptureBody;
        _pr57480CaptureBody = true;
        try { return BodyOnWay(o,way); }
        finally { _pr57480CaptureBody = old; }
    }
    void Pr57480Finish(Vehicle o, Vehicle? blocker)
    {
        var trace = _pr57480Active;
        _pr57480Active = null;
        if (trace is null) return;
        trace.Blocker = blocker?.Id ?? -1;
        _pr57480Calls++;
        _pr57480AllVisits += trace.Visits;
        if (!_pr57480Current.ContainsKey(o.Id) &&
            _pr57480Previous.TryGetValue(o.Id, out var prior))
        {
            _pr57480Pairs++;
            var actorSame = string.Equals(prior.Actor, trace.Actor, StringComparison.Ordinal);
            var posesSame = string.Equals(prior.Poses, trace.Poses, StringComparison.Ordinal);
            var candidatesSame = string.Equals(prior.Candidates.ToString(),
                trace.Candidates.ToString(), StringComparison.Ordinal);
            if (actorSame) _pr57480SameActor++;
            if (posesSame) _pr57480SamePoses++;
            if (candidatesSame) _pr57480SameCandidates++;
            if (prior.Visits == trace.Visits) _pr57480SameVisitCount++;
            if (prior.Blocker == trace.Blocker) _pr57480SameBlocker++;
            if (prior.TimeBits == trace.TimeBits) _pr57480SameTime++;
            if (actorSame && posesSame && candidatesSame)
            {
                _pr57480SameObserved++;
                _pr57480MatchedVisits += trace.Visits;
            }
        }
        // Keep LAST invocation per actor for the next physical slice.
        _pr57480Current[o.Id] = trace;
    }
    void Pr57480EndSlice()
    {
        if (!Pr57480Enabled) return;
        _pr57480Slices++;
        _pr57480Previous.Clear();
        foreach (var pair in _pr57480Current)
            _pr57480Previous[pair.Key] = pair.Value;
        _pr57480Current.Clear();
        Console.WriteLine($"PR57_480_SUM slices={_pr57480Slices} calls={_pr57480Calls} pairs={_pr57480Pairs} sameActor={_pr57480SameActor} samePoses={_pr57480SamePoses} sameCandidates={_pr57480SameCandidates} sameObserved={_pr57480SameObserved} sameBlocker={_pr57480SameBlocker} sameTime={_pr57480SameTime} sameVisitCount={_pr57480SameVisitCount} allVisits={_pr57480AllVisits} matchedVisits={_pr57480MatchedVisits} observational=True exactDependencyClosure=False");
    }
    bool CanTurnNow(Vehicle o)
    {""")
once(shared, "        var turn=TurnWay(o,out _);\n",
     "        var turn=TurnWay(o,out _);\n        Pr57480Begin(o,turn);\n")
once(shared, "        var body=BodyOnWay(o,turn);\n",
     "        var body=Pr57480BodyOnWay(o,turn);\n        Pr57480Finish(o,body);\n")
once(shared, """        foreach(var o in State.Vehicles){
            if(census){visited++;Pr57306Read(v,o);}""", """        foreach(var o in State.Vehicles){
            if(_pr57480CaptureBody && _pr57480Active is { } trace)
            {
                trace.Visits++;
                trace.Candidates.Append('|').Append(o.Id).Append(':')
                    .Append(Pr57MutationSnapshot(o));
            }
            if(census){visited++;Pr57306Read(v,o);}""")
print("PR57_480_INJECTED observational=True noCache=True noSkip=True")
