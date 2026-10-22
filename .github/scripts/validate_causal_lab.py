#!/usr/bin/env python3
"""Trusted minimum CausalEngine smoke checker; NOT SPEC or 80y certification."""
import json
import sys
from pathlib import Path

if len(sys.argv) != 4:
    sys.exit("expected self-test, 1k and 10k JSON paths")
self_test, thousand, ten_thousand = (
    json.loads(Path(p).read_text()) for p in sys.argv[1:]
)
if (self_test.get("success") is not True
        or self_test.get("deterministic") is not True
        or self_test.get("historyFullyPersisted") is not True):
    sys.exit("self-test failed deterministic replay or persistent history")
for result, count in ((thousand, 1000), (ten_thousand, 10000)):
    if result.get("requestedSims") != count or result.get("simulatedDays") != 30:
        sys.exit(f"{count} people / 30 days not actually simulated")
    if result.get("minimumAliveAtDayBoundary", 0) < count:
        sys.exit(f"{count} simultaneous individuals were not maintained")
    if any(result.get(key, 0) <= 0 for key in
           ("journeys", "salesFoodUnits", "recordedFacts", "births", "cargoDeliveries")):
        sys.exit("mobility, economy, demography or history inactive")
    if result.get("moneyConserved") is not True or result.get("historyPersistent") is not True:
        sys.exit("conservation or persistent history failed")
    if result.get("status") == "ATINGIU":
        sys.exit("30d partial fixture cannot certify 80y of complete gameplay")
print("Integrated 1k/10k x30d smoke passed. NOT proof of SPEC coverage or 80y.")
