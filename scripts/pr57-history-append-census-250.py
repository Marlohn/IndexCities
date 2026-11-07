#!/usr/bin/env python3
"""PR57 #250: read-only append-prefix census on real Actions checkpoint ZIP.

Usage: python3 scripts/pr57-history-append-census-250.py artifact.zip
Requires only Python stdlib. It does not run or modify the simulation.
A prefix match is an observation in these checkpoints, NOT proof of universal
append-only writers, nor permission to truncate, aggregate or omit history.
"""
import argparse
import hashlib
import json
import zipfile


def compact_bytes(value):
    # Comparable compact UTF-8 estimate; NOT the exact .NET JSON byte encoding.
    return len(json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("utf-8"))


def personal(state):
    return {c["Id"]: c.get("History", []) for c in state["Citizens"]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifact", help="ZIP artifact integrated-scale-pr57-real-coexist-243")
    args = parser.parse_args()
    with zipfile.ZipFile(args.artifact) as archive:
        paths = sorted(n for n in archive.namelist()
                       if n.startswith("overnight-") and n.endswith("/checkpoint-1.json"))
        if len(paths) < 2:
            raise SystemExit("Need at least two overnight checkpoint-1.json snapshots")
        snapshots = [(path, archive.read(path)) for path in paths]

    prior = None
    total_personal = total_ledger = total_city = 0
    print("PR57_250_SOURCE zip_sha256=" + hashlib.sha256(open(args.artifact, "rb").read()).hexdigest())
    for path, raw in snapshots:
        state = json.loads(raw)
        histories = personal(state)
        personal_count = sum(map(len, histories.values()))
        personal_bytes = sum(compact_bytes(e) for events in histories.values() for e in events)
        ledger_bytes = sum(compact_bytes(e) for e in state["Ledger"])
        city_bytes = sum(compact_bytes(e) for e in state["CityEvents"])
        print(f"PR57_250_SNAPSHOT name={path.split('/')[0]} now={state['Now']} "
              f"alive={sum(bool(c.get('Alive')) for c in state['Citizens'])} "
              f"citizens={len(state['Citizens'])} saveBytes={len(raw)} "
              f"personal={personal_count} ledger={len(state['Ledger'])} "
              f"cityEvents={len(state['CityEvents'])} "
              f"personalCompactBytes={personal_bytes} ledgerCompactBytes={ledger_bytes} "
              f"cityCompactBytes={city_bytes}")
        if prior is not None:
            old_path, old_state, old_histories = prior
            assert old_histories.keys() <= histories.keys(), (old_path, path, "missing-citizen")
            added = {}
            for citizen_id, old_events in old_histories.items():
                current = histories[citizen_id]
                assert current[:len(old_events)] == old_events, (path, citizen_id, "history-not-prefix")
                added[citizen_id] = current[len(old_events):]
                assert old_events + added[citizen_id] == current
            for citizen_id in histories.keys() - old_histories.keys():
                added[citizen_id] = histories[citizen_id]
            old_ledger = old_state["Ledger"]
            old_city = old_state["CityEvents"]
            assert state["Ledger"][:len(old_ledger)] == old_ledger, (path, "ledger-not-prefix")
            assert state["CityEvents"][:len(old_city)] == old_city, (path, "city-events-not-prefix")
            ledger_added = state["Ledger"][len(old_ledger):]
            city_added = state["CityEvents"][len(old_city):]
            assert old_ledger + ledger_added == state["Ledger"]
            assert old_city + city_added == state["CityEvents"]
            n_personal = sum(len(events) for events in added.values())
            delta_bytes = (sum(compact_bytes(e) for events in added.values() for e in events)
                           + sum(compact_bytes(e) for e in ledger_added)
                           + sum(compact_bytes(e) for e in city_added))
            total_personal += n_personal
            total_ledger += len(ledger_added)
            total_city += len(city_added)
            print(f"PR57_250_PREFIX from={old_path.split('/')[0]} to={path.split('/')[0]} "
                  f"personalAdded={n_personal} ledgerAdded={len(ledger_added)} "
                  f"cityAdded={len(city_added)} compactDeltaEventBytes={delta_bytes} "
                  "exactEventValueReconstruction=True allPriorEntriesPreserved=True")
        prior = (path, state, histories)
    print(f"PR57_250_TOTAL transitions={len(snapshots)-1} "
          f"personalAdded={total_personal} ledgerAdded={total_ledger} cityAdded={total_city} "
          "prefixViolations=0 eventReconstructionMismatches=0 "
          "scope=REAL_121_LIVING_16_SIM_HOURS_NOT_10K_80Y "
          "notIntegratedBenchmark=True notCausalClosure=True")


if __name__ == "__main__":
    main()
