using System.Diagnostics;
using System.Text.Json;

namespace IndexCities.CausalEngine;

internal static class EngineSelfTest
{
    public static int Run()
    {
        var temp = Path.Combine(Path.GetTempPath(), "indexcities-causal-" + Guid.NewGuid().ToString("N") + ".facts");
        try
        {
            var watch = Stopwatch.StartNew();
            string hashA, hashB;
            long journeys, sales, births, facts;
            using (var a = new CausalCity(new LabConfig(256, 14, 42, temp, true)))
            {
                a.Run();
                a.AssertConservation();
                hashA = a.Fingerprint();
                journeys = a.Journeys; sales = a.Sales;
                births = a.Births; facts = a.RecordedFacts;
                if (a.MinimumActive < 256 || a.PeakActive < 256 ||
                    journeys <= 0 || sales <= 0 || births <= 0 || facts <= 0)
                    throw new InvalidOperationException("Self-test fixture inactive or inconsistent");
            }
            var bytes = new FileInfo(temp).Length;
            long readFacts = 0, previousAt = -1, meals = 0, trips = 0;
            var householdPurchasesPerDay = new HashSet<(long Day, int Household)>();
            foreach (var fact in HistorySink.Read(temp))
            {
                if (fact.At < previousAt) throw new InvalidOperationException("History out of order");
                previousAt = fact.At;
                if (fact.Type == 3) meals++;
                if (fact.Type == 2) trips++;
                if (fact.Type == 10 && fact.Owner is >= 0 and < 256 && fact.Amount > 0 &&
                    !householdPurchasesPerDay.Add((fact.At / CausalCity.DaySeconds, fact.Owner % 64)))
                    throw new InvalidOperationException("Two adults purchased for the same household on one day");
                readFacts++;
            }
            if (readFacts != facts || trips <= 0 || meals <= 0 ||
                householdPurchasesPerDay.Count <= 0 || bytes >= facts * 21)
                throw new InvalidOperationException($"Corrupt compact factual history: read={readFacts}, expected={facts}, bytes={bytes}");
            using (var b = new CausalCity(new LabConfig(256, 14, 42, null, true)))
            {
                b.Run();
                b.AssertConservation();
                hashB = b.Fingerprint();
                if (hashA != hashB)
                    throw new InvalidOperationException($"Nondeterministic states: {hashA} != {hashB}");
            }
            // Resume after day seven rather than silently restarting a city.
            // The history file is a lossless concatenation of GZip members.
            var compressed = temp + ".gz";
            var snapshot = temp + ".save.json";
            string resumedHash; long resumedFacts;
            using (var firstHalf = new CausalCity(new LabConfig(256, 14, 42, compressed, true)))
            {
                firstHalf.RunUntilDay(7);
                firstHalf.SaveSnapshot(snapshot);
            }
            using (var secondHalf = CausalCity.FromSnapshot(snapshot))
            {
                secondHalf.Run();
                secondHalf.AssertConservation();
                resumedHash = secondHalf.Fingerprint();
                resumedFacts = secondHalf.RecordedFacts;
                if (resumedHash != hashA || resumedFacts != facts)
                    throw new InvalidOperationException(
                        $"Save/resume changed causal result: {resumedHash}/{resumedFacts} vs {hashA}/{facts}");
            }
            long reopened = 0;
            foreach (var entry in HistorySink.Read(compressed)) reopened++;
            if (reopened != facts)
                throw new InvalidOperationException($"GZip history append lost records: {reopened}/{facts}");
            File.Delete(compressed);
            File.Delete(snapshot);

            // These two fixtures retain actual farm workers and delivered
            // groceries. In 256 sims, household 0 has adults aged 64, 34,
            // 53, 23: the first retiree must be replaced by a working shopper.
            // In 196 sims, household 0 has four adults all aged 64: none is
            // working after six years, so retirees must shop themselves.
            var successorFacts = temp + ".successor.facts.gz";
            var retirementFacts = temp + ".retired.facts.gz";
            var retirementDays = (CausalCity.Epoch.AddYears(7) - CausalCity.Epoch).Days;
            using (var successor = new CausalCity(new LabConfig(256, retirementDays, 48, successorFacts, false)))
            {
                successor.Run();
                successor.AssertConservation();
            }
            using (var retirement = new CausalCity(new LabConfig(196, retirementDays, 48, retirementFacts, false)))
            {
                retirement.Run();
                retirement.AssertConservation();
            }
            var firstRetirement = (long)(CausalCity.Epoch.AddYears(6) - CausalCity.Epoch).TotalSeconds;
            long successorPurchases = 0, retiredPurchases = 0;
            foreach (var fact in HistorySink.Read(successorFacts))
                if (fact.Type == 10 && fact.Amount > 0 && fact.Owner == 64 &&
                    fact.At >= firstRetirement) successorPurchases++;
            foreach (var fact in HistorySink.Read(retirementFacts))
                if (fact.Type == 10 && fact.Amount > 0 && fact.Owner == 0 &&
                    fact.At >= firstRetirement) retiredPurchases++;
            File.Delete(successorFacts);
            File.Delete(retirementFacts);
            if (successorPurchases == 0 || retiredPurchases == 0)
                throw new InvalidOperationException(
                    $"Household food shopping stopped after retirement: successor={successorPurchases}, retirees={retiredPurchases}");
            // This counterfactual must preserve both conservation and causal
            // checkpoint equivalence. In particular, payroll depends on firm
            // receipts and shift counts which must survive save/resume.
            var feedbackConfig = new LabConfig(256, 30, 42, null, true, WageFeedback: true);
            string feedbackHash;
            using (var complete = new CausalCity(feedbackConfig))
            {
                complete.Run();
                complete.AssertConservation();
                feedbackHash = complete.Fingerprint();
            }
            var feedbackSave = temp + ".feedback.save.json";
            string feedbackResumedHash;
            using (var partial = new CausalCity(feedbackConfig))
            {
                partial.RunUntilDay(15);
                partial.SaveSnapshot(feedbackSave);
            }
            using (var resumed = CausalCity.FromSnapshot(feedbackSave))
            {
                resumed.Run();
                resumed.AssertConservation();
                feedbackResumedHash = resumed.Fingerprint();
            }
            File.Delete(feedbackSave);
            if (feedbackHash != feedbackResumedHash)
                throw new InvalidOperationException("Revenue-backed payroll changed after save/resume");
            // Counterfactual demographic clock: make births depend on actual
            // households (not an absolute citywide cap), and retain the exact
            // appointment state across a checkpoint without extra citizens.
            var renewalConfig = new LabConfig(256, 365, 42, null, true,
                WageFeedback: true, CohortRenewal: true);
            string renewalHash;
            long renewalBirths;
            using (var continuous = new CausalCity(renewalConfig))
            {
                continuous.Run();
                continuous.AssertConservation();
                renewalHash = continuous.Fingerprint();
                renewalBirths = continuous.Births;
            }
            var renewalSave = temp + ".renewal.save.json";
            using (var half = new CausalCity(renewalConfig))
            {
                half.RunUntilDay(183);
                half.SaveSnapshot(renewalSave);
            }
            using (var continued = CausalCity.FromSnapshot(renewalSave))
            {
                continued.Run();
                continued.AssertConservation();
                if (renewalHash != continued.Fingerprint() || renewalBirths != continued.Births)
                    throw new InvalidOperationException("Family renewal diverged after save/resume");
            }
            File.Delete(renewalSave);
            if (renewalBirths <= 0)
                throw new InvalidOperationException("Household birth appointments produced no births");
            // Revenue-funded, rotating real shifts must conserve cash and
            // produce the same result after an exact day-boundary checkpoint.
            // Workers with no allocated shift receive NO wage.
            var demandConfig = new LabConfig(256, 60, 42, null, true,
                CohortRenewal: true, DemandStaffing: true);
            var demandSave = temp + ".demand.save.json";
            string demandFingerprint;
            long demandWages;
            using (var completeDemand = new CausalCity(demandConfig))
            {
                completeDemand.Run();
                completeDemand.AssertConservation();
                demandFingerprint = completeDemand.Fingerprint();
                using var report = JsonDocument.Parse(JsonSerializer.Serialize(completeDemand.Report(0)));
                demandWages = report.RootElement.GetProperty("paidWagesCents").GetInt64();
                var shifts = report.RootElement.GetProperty("workingDays").GetInt64();
                if (demandWages <= 0 || shifts <= 0 || shifts >= 256 * 60)
                    throw new InvalidOperationException("Demand based work is not real, selective and paid");
            }
            using (var firstDemand = new CausalCity(demandConfig))
            {
                firstDemand.RunUntilDay(30);
                firstDemand.SaveSnapshot(demandSave);
            }
            using (var secondDemand = CausalCity.FromSnapshot(demandSave))
            {
                secondDemand.Run();
                secondDemand.AssertConservation();
                if (demandFingerprint != secondDemand.Fingerprint())
                    throw new InvalidOperationException("Demand-funded shifts diverged after checkpoint");
            }
            File.Delete(demandSave);

            // Small closed-household stress case: when the final family
            // member dies, a positive estate must NOT finance city pensions.
            long unclaimedEstate;
            using (var orphan = new CausalCity(new LabConfig(4, 365, 48, null, false)))
            {
                orphan.Run();
                orphan.AssertConservation();
                unclaimedEstate = orphan.ReserveGlobalCents;
            }
            if (unclaimedEstate <= 0)
                throw new InvalidOperationException("Heirless money did not reach the Global Reserve");
            watch.Stop();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                success = true, scope = "14 days, 256 people, real trips and transactions in simplified fixture",
                worlds = 7, deterministic = true, hash = hashA,
                payrollFeedbackSaveResumeExact = feedbackHash == feedbackResumedHash,
                heirlessEstatesReturnToReserve = unclaimedEstate > 0,
                familyRenewalSaveResumeExact = true, familyRenewalBirths = renewalBirths,
                demandFundedShiftsConserved = demandWages > 0,
                demandStaffingSaveResumeExact = true,
                saveResumeExact = resumedHash == hashA,
                compressedHistoryReplayExact = reopened == facts,
                journeys, sales, births, successorPurchases, retiredPurchases, historyFacts = facts,
                historyBytes = bytes, historyFullyPersisted = readFacts == facts,
                originalByteEquivalent = facts * 21, compressionWithoutGzip = (double)bytes / (facts * 21),
                elapsedSeconds = watch.Elapsed.TotalSeconds,
                warning = "Does NOT prove IndexCities SPEC fidelity or target 10k/80y"
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("SELF-TEST FAILED: " + ex);
            return 1;
        }
        finally
        {
            foreach (var path in new[] { temp, temp + ".retired.facts.gz", temp + ".successor.facts.gz", temp + ".gz", temp + ".save.json", temp + ".feedback.save.json", temp + ".renewal.save.json", temp + ".demand.save.json" })
                if (File.Exists(path)) File.Delete(path);
        }
    }
}