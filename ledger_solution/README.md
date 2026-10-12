# In-Memory Account Ledger Core

A dependency-free .NET 8 console implementation of the assessment's six-day, event-sourced ledger.

## Run
```bash
dotnet run --project src/Ledger
```
The app replays the supplied events in event order, prints accepted/rejected events, end-of-day ledger/available balances, fees, and Day 6 interest.

## Design
- Immutable input events are replayed in `OccurredDay` order (stable by event ID).
- Ledger entries are value-dated. A backdated entry changes the affected historical closing balance.
- Authorizations are holds, not ledger entries; they reduce available balance only.
- A settlement must reference an existing approved authorization. Unknown IDs are rejected.
- Reversals negate the referenced posted entry, including its original value date.
- Overdraft fees are derived from the final replayed ledger: AED 25 per account/day with a negative closing balance, avoiding a stale fee when a backdated entry is reversed.
- Interest is calculated at 0.04% per day on positive end-of-day ledger balances and capitalized as one credit at the end of Day 6. Currency precision is 2 decimals for AED and 3 for BHD; each daily accrual is rounded to currency precision and the sum is capitalized. This is an explicit interpretation; see `AMBIGUITIES.md`.

## Deliverables
- `NUMBERS.md`: expected calculations and event dispositions.
- `AMBIGUITIES.md`: unresolved specifications and chosen interpretations.
- `REJECTED.md`: incorrect acceptance claims and reasons.
- `WORKLOG.md`: example timestamped worklog (replace timestamps with your actual work times before submitting).
- `tests/AnnotatedFailingTest.md`: deliberately failing assertion described and annotated.

This is a working reference implementation, not a hosted GitHub repository. Create a Git repository and commit incrementally if the assessment requires verifiable commit history; do not fabricate historical commits.
