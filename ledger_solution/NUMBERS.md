# Calculation notes

## Principal entries for ACC-001 before fee/interest effects
- E1 + E2 on Day 1: AED 1,200.00 - AED 950.00 = AED 250.00.
- E7 is a Day 5 debit with Day 2 value date: Day 2 pre-fee balance becomes AED -370.00.
- E4 credits AED 400.00 on Day 3.
- E5 is a valid AED 185.00 settlement of Auth-A if the authorization was approved.
- E6 references Auth-Z, which has no authorization event; reject without posting.
- E9 reverses E7 at its original value date, so E7's net principal effect becomes zero in the final event state.
- E8 creates a hold only if sufficient available funds remain when it is processed; the hold does not change ledger balance.
- E10 total is BHD 10.000; three precision-safe installments are 3.333, 3.333, and 3.334.

## Expected decisions
- E1, E2, E4: posted.
- E3 Auth-A: approved against Day 2 available balance of AED 250.00; AED 50.00 initially available after hold.
- E5: accepted as partial settlement of Auth-A (subject to interpretation noted in AMBIGUITIES.md).
- E6: rejected, no matching authorization.
- E7: posted with Day 2 value date.
- E8: authorization decision depends on the implementation's final balance/fee timing; the implementation tests it against the ledger state at event time and remaining holds.
- E9: reverses E7 principal at Day 2.
- E10: split precision-safe, total-preserving installments.

Because the brief does not completely specify fee timing, interest base, and installment dates, exact final totals should be treated as interpretation-dependent and must be confirmed with the assessor.
