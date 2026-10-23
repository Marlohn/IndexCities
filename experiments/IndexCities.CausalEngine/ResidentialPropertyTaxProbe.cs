namespace IndexCities.CausalEngine;

// Counterfactual for approved category of municipal revenue (property tax),
// NOT the IndexCities V1 real-estate valuation/purchase/inheritance model.
// Each existing simulated house is initialized with ONE living adult deed owner;
// the declared fixed monthly amount is only a sensitivity parameter.
// No house, buyer, title sale, cash, tax receipt, or forgiveness is manufactured.
internal sealed partial class CausalCity
{
    private void CollectExistingResidentialPropertyTaxes()
    {
        foreach (var h in _households)
        {
            var ownerId = h.PropertyOwnerPersonId;
            if (ownerId < 0) continue; // abandoned/unowned property has no fictitious payer
            var owner = _people[ownerId];
            if (!owner.Alive || owner.HouseholdId != h.Id)
                throw new InvalidOperationException("A residential property tax has no real current owner");

            // Tax is assessed to ONE privately owned physical home. The
            // outstanding debt is a bookkeeping claim, never city revenue.
            var newAssessment = _config.MonthlyResidentialTaxCents;
            _residentialTaxAssessedCents = checked(_residentialTaxAssessedCents + newAssessment);
            h.ResidentialPropertyTaxDebtCents = checked(h.ResidentialPropertyTaxDebtCents + newAssessment);

            // Collect actual available owner money, possibly only a partial
            // payment against old + new debt; do not borrow, print or duplicate.
            var paid = Math.Min(owner.Cash, h.ResidentialPropertyTaxDebtCents);
            if (paid > 0)
            {
                owner.Cash -= paid;
                h.ResidentialPropertyTaxDebtCents -= paid;
                _treasury = checked(_treasury + paid);
                _residentialTaxPaidCents = checked(_residentialTaxPaidCents + paid);
                _history.Fact(_time, ownerId, 29, paid);
            }
        }
    }
}
