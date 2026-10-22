namespace IndexCities.CausalEngine;

// Isolated material-and-payroll stress experiment, NOT a new SPEC rule.
// Collection workers already deliver real physical waste to a finite depot.
// Separate SIM employees physically attend a treatment plant on that SAME
// depot node. Treatment moves finite material to a monitored treated inventory;
// it cannot remove trash, mint money or process waste without paid labour.
// Facility size, capacity and material end-state require further product work.
internal sealed partial class CausalCity
{
    private const int WasteTreatmentJob = SanitationJob + 1;
    private const int WasteTreatmentGrossWage = 80;
    private const int WasteTreatmentUnitsPerShift = 50;
    private const int MaxWasteTreatmentWorkers = 256;
    // A physical, finite landfill compartment, not an infinite sink. Both
    // raw and processed waste remain present in auditable inventories.
    private const long TreatedLandfillCapacity = 500_000_000L;

    private long _wasteTreated, _wasteTreatmentPaidShifts;

    private void FinishMunicipalTreatmentShift(Person worker)
    {
        worker.AtWork = false;
        worker.HeadingToWork = false;
        if (worker.Node != SanitationDepotNode ||
            _garbageAtDepot <= 0 || _wasteTreated >= TreatedLandfillCapacity ||
            _treasury < WasteTreatmentGrossWage)
        {
            StartTrip(worker, worker.Home);
            return;
        }

        // Only ACTUALLY DEPOSITED refuse exists at the treatment plant;
        // refuse still carried by collection workers is not processed.
        var amount = Math.Min(WasteTreatmentUnitsPerShift,
            Math.Min(_garbageAtDepot, TreatedLandfillCapacity - _wasteTreated));
        _garbageAtDepot -= amount;
        _wasteTreated += amount;

        var pensionContribution = WasteTreatmentGrossWage / 10;
        _treasury -= WasteTreatmentGrossWage;
        _treasury += pensionContribution;
        worker.Cash += WasteTreatmentGrossWage - pensionContribution;
        worker.GrossEarnings += WasteTreatmentGrossWage;
        worker.ContributedDays++;
        _paidWages += WasteTreatmentGrossWage;
        _municipalWagesPaidCents += WasteTreatmentGrossWage;
        _wasteTreatmentPaidShifts++;
        _workdays++;

        _history.Fact(_time, worker.Id, 28, amount);
        _history.Fact(_time, worker.Id, 7, WasteTreatmentGrossWage - pensionContribution);
        _history.Fact(_time, worker.Id, 15, pensionContribution);
        StartTrip(worker, worker.Home);
    }
}
