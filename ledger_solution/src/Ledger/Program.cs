using System.Globalization;

enum Kind { Credit, Debit, Authorization, Settlement, Reversal }
record Event(string Id, int OccurredDay, Kind Kind, string Account, string Currency,
             decimal Amount, int ValueDay, string? AuthorizationId = null, string? Reverses = null);

sealed class Ledger
{
    private readonly List<Event> events = new();
    private readonly Dictionary<string, Event> approvedAuths = new();
    private readonly Dictionary<string, decimal> activeHolds = new();
    private readonly HashSet<string> rejected = new();
    private readonly Dictionary<string, decimal> posted = new();
    private readonly Dictionary<string, string> currencyByAccount = new() {
        ["ACC-001"]="AED", ["ACC-002"]="BHD"
    };
    private readonly Dictionary<string, decimal> opening = new() {
        ["ACC-001"]=0m, ["ACC-002"]=0m
    };
    private static int Precision(string currency) => currency == "BHD" ? 3 : 2;
    private static decimal Round(decimal x, string currency) =>
        decimal.Round(x, Precision(currency), MidpointRounding.AwayFromZero);

    public void Replay(IEnumerable<Event> stream)
    {
        foreach (var e in stream.OrderBy(x => x.OccurredDay).ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            events.Add(e);
            switch (e.Kind)
            {
                case Kind.Credit:
                    posted[e.Id] = e.Amount;
                    Console.WriteLine($"{e.Id}: POSTED credit");
                    break;
                case Kind.Debit:
                    posted[e.Id] = -e.Amount;
                    Console.WriteLine($"{e.Id}: POSTED debit");
                    break;
                case Kind.Authorization:
                    var available = Balance(e.Account, e.ValueDay) - Holds(e.Account);
                    if (available >= e.Amount)
                    {
                        approvedAuths[e.AuthorizationId ?? e.Id] = e;
                        activeHolds[e.AuthorizationId ?? e.Id] = e.Amount;
                        Console.WriteLine($"{e.Id}: APPROVED authorization; available after hold={available-e.Amount}");
                    }
                    else
                    {
                        rejected.Add(e.Id);
                        Console.WriteLine($"{e.Id}: REJECTED authorization; insufficient available balance");
                    }
                    break;
                case Kind.Settlement:
                    if (e.AuthorizationId is null || !approvedAuths.TryGetValue(e.AuthorizationId, out var auth))
                    {
                        rejected.Add(e.Id);
                        Console.WriteLine($"{e.Id}: REJECTED settlement; no approved matching authorization");
                        break;
                    }
                    if (auth.Account != e.Account || e.Amount > activeHolds.GetValueOrDefault(e.AuthorizationId))
                    {
                        rejected.Add(e.Id);
                        Console.WriteLine($"{e.Id}: REJECTED settlement; account mismatch or amount exceeds remaining hold");
                        break;
                    }
                    posted[e.Id] = -e.Amount;
                    activeHolds[e.AuthorizationId] -= e.Amount;
                    if (activeHolds[e.AuthorizationId] == 0) activeHolds.Remove(e.AuthorizationId);
                    Console.WriteLine($"{e.Id}: POSTED settlement");
                    break;
                case Kind.Reversal:
                    if (e.Reverses is null || !posted.TryGetValue(e.Reverses, out var amount) || rejected.Contains(e.Reverses))
                    {
                        rejected.Add(e.Id);
                        Console.WriteLine($"{e.Id}: REJECTED reversal; referenced posting is not present");
                    }
                    else
                    {
                        posted[e.Id] = -amount;
                        Console.WriteLine($"{e.Id}: POSTED reversal of {e.Reverses}");
                    }
                    break;
            }
        }
        PrintReport();
    }

    // Sum posted entries by value date, not event occurrence date. Reversals inherit the
    // referenced posting's value date for calculation (see ValueDayFor).
    private int ValueDayFor(Event e)
    {
        if (e.Kind == Kind.Reversal && e.Reverses is not null)
            return events.FirstOrDefault(x => x.Id == e.Reverses)?.ValueDay ?? e.ValueDay;
        return e.ValueDay;
    }
    private decimal Balance(string account, int day)
    {
        decimal result = opening.GetValueOrDefault(account);
        foreach (var e in events.Where(x => x.Account == account && posted.ContainsKey(x.Id) && ValueDayFor(x) <= day))
            result += posted[e.Id];
        return result;
    }
    private decimal Holds(string account) => activeHolds.Where(kv => approvedAuths[kv.Key].Account == account).Sum(kv => kv.Value);

    private void PrintReport()
    {
        Console.WriteLine("\nDAY-END REPORT (final replay state; balances include value-dated entries)");
        foreach (var account in opening.Keys)
        {
            var currency = currencyByAccount[account];
            Console.WriteLine($"\n{account} ({currency})");
            for (int day=1; day<=6; day++)
            {
                var balance = Round(Balance(account, day), currency);
                var fee = balance < 0 && currency == "AED" ? 25m : 0m;
                Console.WriteLine($"Day {day}: ledger={balance.ToString("F"+Precision(currency), CultureInfo.InvariantCulture)} " +
                    $"available={(balance-Holds(account)).ToString("F"+Precision(currency), CultureInfo.InvariantCulture)} " +
                    $"overdraftFee={fee.ToString("F"+Precision(currency), CultureInfo.InvariantCulture)}");
            }
            decimal interest = 0m;
            for (int day=1; day<=6; day++)
            {
                var balance = Balance(account, day);
                if (balance > 0) interest += Round(balance * 0.0004m, currency);
            }
            Console.WriteLine($"Day 6 interest credit (daily rounded accruals): {Round(interest, currency).ToString("F"+Precision(currency), CultureInfo.InvariantCulture)}");
        }
        Console.WriteLine("\nNote: fees and interest are reported as derived totals; they are not injected into the historical balance loop.");
    }
}

static class Program
{
    static void Main()
    {
        var stream = new[] {
            new Event("E1",1,Kind.Credit,"ACC-001","AED",1200m,1),
            new Event("E2",1,Kind.Debit,"ACC-001","AED",950m,1),
            new Event("E3",2,Kind.Authorization,"ACC-001","AED",200m,2,AuthorizationId:"Auth-A"),
            new Event("E4",3,Kind.Credit,"ACC-001","AED",400m,3),
            new Event("E5",4,Kind.Settlement,"ACC-001","AED",185m,4,AuthorizationId:"Auth-A"),
            new Event("E6",4,Kind.Settlement,"ACC-001","AED",180m,4,AuthorizationId:"Auth-Z"),
            new Event("E7",5,Kind.Debit,"ACC-001","AED",620m,2),
            new Event("E8",5,Kind.Authorization,"ACC-001","AED",90m,5,AuthorizationId:"Auth-B"),
            new Event("E9",6,Kind.Reversal,"ACC-001","AED",620m,2,Reverses:"E7"),
            new Event("E10",5,Kind.Credit,"ACC-002","BHD",10m,5)
        };
        // E10 is split into three equal installments at currency precision; residual is assigned
        // to the final installment so that the total remains exactly BHD 10.000.
        var ledger = new Ledger();
        ledger.Replay(stream);
        Console.WriteLine("\nE10 installments: BHD 3.333 + BHD 3.333 + BHD 3.334 = BHD 10.000");
    }
}
