# Ambiguities and chosen interpretations

1. **Closing-balance date boundary.** The brief's phrase “value_date < that day” conflicts with its Day 2 example, which includes E7's Day 2 value date. Implementation uses `value_date <= day`.
2. **Interest base.** Interpreted as 0.04% of each positive end-of-day ledger balance, daily, rounded to currency precision, then summed and credited once at Day 6. The brief does not specify whether interest itself compounds daily or whether accrual uses unrounded precision.
3. **BHD installments.** 10.000 / 3 cannot be represented as three identical amounts to three decimals. Use 3.333, 3.333, 3.334, assigning the residual to the final installment.
4. **Auth-A settlement.** E5 settles 185 against a 200 hold. Implementation debits 185 and releases 185 of the hold, leaving 15 held. Whether settlement should release the full hold is not stated.
5. **Backdated reversal and fees.** E9 reverses E7 with the same value date. Fees are derived from the final replay state, so a reversal removes the negative balance attributable solely to E7 and its associated derived fee. If fees must be immutable booked entries, an explicit fee-reversal event is needed.
6. **Overdraft fee currency.** AED 25 is specified, but the policy for non-AED accounts is not. Implementation applies it only to AED accounts.
7. **Interest eligibility.** The text says positive balance; implementation excludes non-positive balances and computes per-day rounded accruals. It does not accrue interest on holds.
8. **Event ordering.** Input is replayed by occurrence day and event ID for deterministic ordering. The supplied sequence already provides intended order; IDs are used only as a tie-breaker.
9. **Authorization availability after retroactive entries.** Authorizations are approved against the ledger state known at the moment they occur. Later backdated events do not retroactively revoke an approval.
10. **E10 schedule.** “Posted as three equal instalments” gives no specific posting days or value dates for each installment. The sample reports the exact split, but does not invent three separate event dates.
