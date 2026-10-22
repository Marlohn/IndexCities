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
            foreach (var fact in HistorySink.Read(temp))
            {
                if (fact.At < previousAt) throw new InvalidOperationException("History out of order");
                previousAt = fact.At;
                if (fact.Type == 3) meals++;
                if (fact.Type == 2) trips++;
                readFacts++;
            }
            if (readFacts != facts || trips <= 0 || meals <= 0 || bytes >= facts * 21)
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

            // A family in this four-person fixture starts at ages 58, 56,
            // 54 and 52. Its oldest adult retires after 12 years; after
            // 18 years nobody works. Shopping must continue to happen via
            // physical trips and paid purchases in BOTH situations.
            var retirementFacts = temp + ".retired.facts";
            var retirementDays = (CausalCity.Epoch.AddYears(19) - CausalCity.Epoch).Days;
            using (var retirement = new CausalCity(new LabConfig(4, retirementDays, 42, retirementFacts, false)))
            {
                retirement.Run();
                retirement.AssertConservation();
            }
            var firstRetirement = (long)(CausalCity.Epoch.AddYears(12) - CausalCity.Epoch).TotalSeconds;
            var secondRetirement = (long)(CausalCity.Epoch.AddYears(14) - CausalCity.Epoch).TotalSeconds;
            var allRetired = (long)(CausalCity.Epoch.AddYears(18) - CausalCity.Epoch).TotalSeconds;
            long successorPurchases = 0, retiredPurchases = 0;
            foreach (var fact in HistorySink.Read(retirementFacts))
            {
                if (fact.Type != 10 || fact.Amount <= 0) continue;
                if (fact.Owner == 1 && fact.At >= firstRetirement && fact.At < secondRetirement)
                    successorPurchases++;
                if (fact.At >= allRetired) retiredPurchases++;
            }
            File.Delete(retirementFacts);
            if (successorPurchases == 0 || retiredPurchases == 0)
                throw new InvalidOperationException(
                    $"Household food shopping stopped after retirement: successor={successorPurchases}, retirees={retiredPurchases}");
            watch.Stop();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                success = true, scope = "14 days, 256 people, real trips and transactions in simplified fixture",
                worlds = 3, deterministic = true, hash = hashA,
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
            foreach (var path in new[] { temp, temp + ".retired.facts", temp + ".gz", temp + ".save.json" })
                if (File.Exists(path)) File.Delete(path);
        }
    }
}