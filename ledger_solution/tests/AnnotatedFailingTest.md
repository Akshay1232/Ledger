# Deliberately failing test (assessment artifact)

```csharp
// Deliberately failing assertion — do not include this in the passing test suite.
Assert.Equal(3.334m, 10.000m / 3m);
```

**Why it fails:** BHD has three decimal places, but 10.000 divided by three is 3.333333… . Three installments of 3.334 sum to 10.002, so this assertion encodes an impossible requirement.

**What it reveals:** The installment rule needs an explicit residual-allocation policy. A total-preserving split is 3.333 + 3.333 + 3.334 = 10.000.
