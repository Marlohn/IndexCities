namespace IndexCities.Simulation;

// PR57 #589 — isolated, opt-in exact minimum certificate.
// No cross-call state or cached world view; public edits are read live.
public sealed partial class CitySimulation
{
    long _pr57589Slices;
    long _pr57589Certificates;
    long _pr57589Cutoffs;
    long _pr57589Skipped;
}
