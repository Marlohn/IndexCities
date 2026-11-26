namespace IndexCities.Simulation;

// Experimental execution seam, never persisted or enabled by default.
// The original domain methods still own every effect. Public Advance is an
// invalidation boundary because State exposes mutable aliases to callers.
// No work is skipped in physical, needs, production or service phases yet.
public sealed partial class CitySimulation
{
    public bool ExperimentalCausalCalendarEnabled { get; set; }
    public long ExperimentalLoanPhaseCalls { get; private set; }
    public long ExperimentalLoanDeadlineRebuilds { get; private set; }
    DateTime? _experimentalNextLoanDue;

    void ExperimentalRebuildLoanDeadline()
    {
        ExperimentalLoanDeadlineRebuilds++;
        _experimentalNextLoanDue = null;
        foreach (var loan in State.Loans)
            if (loan.RemainingMonths > 0 &&
                (_experimentalNextLoanDue is null || loan.NextDue < _experimentalNextLoanDue.Value))
                _experimentalNextLoanDue = loan.NextDue;
    }

    void ExperimentalProcessFinance()
    {
        // Preserve the original phase order, including catch-up on coincident
        // dates. Do not run any callback before the canonical Step boundary.
        while (State.Now >= State.NextDaily)
        {
            DailyFinance();
            State.NextDaily = State.NextDaily.AddDays(1);
            ExperimentalRebuildLoanDeadline();
        }
        while (State.Now >= State.NextTaxDate)
        {
            PropertyTaxes();
            State.NextTaxDate = State.NextTaxDate.AddMonths(1);
            ExperimentalRebuildLoanDeadline();
        }
        if (_experimentalNextLoanDue is not DateTime due || State.Now < due)
            return;
        ExperimentalLoanPhaseCalls++;
        ProcessLoans();
        ExperimentalRebuildLoanDeadline();
    }
}
