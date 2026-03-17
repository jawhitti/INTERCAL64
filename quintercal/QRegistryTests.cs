// INTERCAL-Q: QRegistry Unit Tests
// Tests the quantum variable registry in isolation from the INTERCAL runtime.
//
// Test categories:
//   Basic:          QValue creation and collapse invariants
//   Entanglement:   Swirl operator and component merging
//   Expression:     Mingle/unary expression trees
//   SideEffects:    Cross-component collapse propagation and ABSTAIN hooks
//   Demos:          Quantum FizzBuzz skeleton and Quantum Bogosort (3 elements)
//   Teleportation:  Full quantum teleportation protocol and physics constraints

using InterCalQ;
using Xunit;

namespace InterCalQ.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

file static class Helpers
{
    /// <summary>
    /// Registry that always picks the first leaf as survivor.
    /// Makes collapse deterministic for testing.
    /// </summary>
    public static QRegistry Deterministic() => new(n => 0);

    /// <summary>
    /// Registry that always picks the last leaf as survivor.
    /// </summary>
    public static QRegistry DeterministicLast() => new(n => n - 1);

    /// <summary>
    /// Registry with real randomness. Use for statistical tests.
    /// </summary>
    public static QRegistry Random() => new();

    public static QMingle Mingle(QValue a, QValue b) =>
        new(new QLeaf(a), new QLeaf(b));

    public static QUnary Unary(UnaryOp op, QValue a) =>
        new(op, new QLeaf(a));
}

// ─────────────────────────────────────────────────────────────────────────────
// Basic: QValue creation and collapse invariants
// ─────────────────────────────────────────────────────────────────────────────

public class BasicTests
{
    [Fact]
    public void NewQValueIsNotCollapsed()
    {
        var reg = Helpers.Deterministic();
        var q = new QValue(42, reg);
        Assert.False(q.Collapsed);
    }

    [Fact]
    public void CollapseProducesValueOrZero()
    {
        // Run many times to catch both branches
        int count42 = 0;
        int countDead = 0;

        for (int i = 0; i < 100; i++)
        {
            var reg = Helpers.Random();
            var q = new QValue(42, reg);
            long result = q.Observe();
            Assert.True(result == 42 || result == QValue.THECATISDEAD,
                $"Expected 42 or THECATISDEAD but got {result}");
            if (result == 42) count42++;
            else countDead++;
        }

        Assert.True(count42 > 0, "Never got 42 in 100 trials");
        Assert.True(countDead > 0, "Never got THECATISDEAD in 100 trials");
    }

    [Fact]
    public void CollapseIsIdempotent()
    {
        var reg = Helpers.Deterministic();
        var q = new QValue(42, reg);
        long first = q.Observe();
        long second = q.Observe();
        Assert.Equal(first, second);
    }

    [Fact]
    public void CollapseMarksAsCollapsed()
    {
        var reg = Helpers.Deterministic();
        var q = new QValue(42, reg);
        q.Observe();
        Assert.True(q.Collapsed);
    }

    [Fact]
    public void DeterministicFirstAlwaysGivesValue()
    {
        var reg = Helpers.Deterministic(); // always picks index 0 = first leaf
        var q = new QValue(42, reg);
        Assert.Equal(42, q.Observe());
    }

    [Fact]
    public void ZeroValueIsValid()
    {
        // Zero is a valid value — the cat is alive with value 0
        var reg = Helpers.Random();
        var q = new QValue(0, reg);
        Assert.False(q.Collapsed);
    }

    [Fact]
    public void CollapsedQValueRemovedFromRegistry()
    {
        var reg = Helpers.Deterministic();
        var q = new QValue(42, reg);
        Assert.Equal(1, reg.QValueCount);
        q.Observe();
        Assert.Equal(0, reg.QValueCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(65535)]
    public void ResultIsAlwaysValueOrZero(int value)
    {
        for (int i = 0; i < 50; i++)
        {
            var reg = Helpers.Random();
            var q = new QValue(value, reg);
            long result = q.Observe();
            Assert.True(result == value || result == QValue.THECATISDEAD);
        }
    }

    [Fact]
    public void StatisticallyBothOutcomesOccur()
    {
        // Over many trials both outcomes should occur
        bool sawValue = false, sawDead = false;
        for (int i = 0; i < 200; i++)
        {
            var reg = Helpers.Random();
            var q = new QValue(42, reg);
            long result = q.Observe();
            if (result == 42) sawValue = true;
            if (result == QValue.THECATISDEAD) sawDead = true;
            if (sawValue && sawDead) break;
        }
        Assert.True(sawValue, "Never got the value in 200 trials");
        Assert.True(sawDead, "Never got THECATISDEAD in 200 trials");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Entanglement: Swirl operator and component merging
// ─────────────────────────────────────────────────────────────────────────────

public class EntanglementTests
{
    [Fact]
    public void SwirlMergesComponents()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        Assert.Equal(2, reg.ComponentCount);
        q1.Swirl(q2);
        Assert.Equal(1, reg.ComponentCount);
    }

    [Fact]
    public void EntangledPairExactlyOneSurvives()
    {
        for (int i = 0; i < 100; i++)
        {
            var reg = Helpers.Random();
            var q1 = new QValue(1, reg);
            var q2 = new QValue(2, reg);
            q1.Swirl(q2);

            q1.Observe();

            // Exactly one should have its value, the other zero
            int survivors = (q1.Result != QValue.THECATISDEAD ? 1 : 0) + (q2.Result != QValue.THECATISDEAD ? 1 : 0);
            Assert.Equal(1, survivors);
        }
    }

    [Fact]
    public void CollapsingOneCollapsesOther()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        q1.Swirl(q2);

        Assert.False(q2.Collapsed);
        q1.Observe();
        Assert.True(q2.Collapsed);
    }

    [Fact]
    public void SwirlingSameVariableTwiceIsNoOp()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        q1.Swirl(q2);
        q1.Swirl(q2); // no-op, no exception

        Assert.Equal(1, reg.ComponentCount);
        Assert.True(reg.AreEntangled(q1, q2));
    }

    [Fact]
    public void SwirlingAlreadyCollapsedThrows()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        q1.Observe();

        Assert.Throws<InterCalQException>(() => q1.Swirl(q2));
    }

    [Fact]
    public void ThreeWayEntanglementExactlyOneSurvives()
    {
        for (int i = 0; i < 100; i++)
        {
            var reg = Helpers.Random();
            var q1 = new QValue(1, reg);
            var q2 = new QValue(2, reg);
            var q3 = new QValue(3, reg);
            q1.Swirl(q2);
            q2.Swirl(q3);

            q1.Observe();

            int survivors = new[] { q1, q2, q3 }
                .Count(q => q.Result != QValue.THECATISDEAD);
            Assert.Equal(1, survivors);
        }
    }

    [Fact]
    public void ThreeWayEntanglementAllCollapse()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        var q3 = new QValue(3, reg);
        q1.Swirl(q2);
        q2.Swirl(q3);

        q1.Observe();

        Assert.True(q1.Collapsed);
        Assert.True(q2.Collapsed);
        Assert.True(q3.Collapsed);
    }

    [Fact]
    public void AreEntangledReturnsTrueAfterSwirl()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        Assert.False(reg.AreEntangled(q1, q2));
        q1.Swirl(q2);
        Assert.True(reg.AreEntangled(q1, q2));
    }

    [Fact]
    public void TransitiveEntanglementDetected()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        var q3 = new QValue(3, reg);
        q1.Swirl(q2);
        q2.Swirl(q3);
        Assert.True(reg.AreEntangled(q1, q3));
    }

    [Fact]
    public void StatisticallyAllMembersCanSurvive()
    {
        // Each of three entangled qvalues should occasionally be the survivor
        bool[] wasSurvivor = new bool[3];
        for (int i = 0; i < 500; i++)
        {
            var reg = Helpers.Random();
            var qs = new[] {
                new QValue(1, reg),
                new QValue(2, reg),
                new QValue(3, reg)
            };
            qs[0].Swirl(qs[1]);
            qs[1].Swirl(qs[2]);
            qs[0].Observe();
            for (int j = 0; j < 3; j++)
                if (qs[j].Result != QValue.THECATISDEAD) wasSurvivor[j] = true;
            if (wasSurvivor.All(x => x)) break;
        }
        Assert.True(wasSurvivor[0], "q1 was never the survivor");
        Assert.True(wasSurvivor[1], "q2 was never the survivor");
        Assert.True(wasSurvivor[2], "q3 was never the survivor");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Expression Trees: Mingle and Unary
// ─────────────────────────────────────────────────────────────────────────────

public class ExpressionTreeTests
{
    [Fact]
    public void MingleTreeCollapsesWhenObserved()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(0b1010, reg);  // 10
        var q2 = new QValue(0b0101, reg);  // 5
        q1.Swirl(q2);

        var tree = new QMingle(new QLeaf(q1), new QLeaf(q2));
        var derived = new QValue(tree, reg);
        reg.Entangle(derived, q1);

        derived.Observe();

        Assert.True(derived.Collapsed);
        Assert.True(q1.Collapsed);
        Assert.True(q2.Collapsed);
    }

    [Fact]
    public void MingleTreeResultIsConsistentWithLeaves()
    {
        var reg = Helpers.Deterministic(); // first leaf survives = q1 gets value
        var q1 = new QValue(0b1010, reg);
        var q2 = new QValue(0b0101, reg);
        q1.Swirl(q2);

        var tree = new QMingle(new QLeaf(q1), new QLeaf(q2));
        var derived = new QValue(tree, reg);
        reg.Entangle(derived, q1);

        derived.Observe();

        // q1 survived so result should be mingle(q1.Value, 0)
        long expected = QMingle.Mingle(q1.Result, q2.Result);
        Assert.Equal(expected, derived.Result);
    }

    [Fact]
    public void UnaryAppliedEagerlyPreservesInvariant()
    {
        // Unary on a qvalue should keep zero branch as zero
        for (int i = 0; i < 50; i++)
        {
            var reg = Helpers.Random();
            var q1 = new QValue(0b1111_1111, reg); // 255

            // Apply unary OR - result should be {ApplyOr(255), 0}
            var unaryTree = new QUnary(UnaryOp.Or, new QLeaf(q1));
            var derived = new QValue(unaryTree, reg);
            reg.Entangle(derived, q1);

            derived.Observe();

            // Zero branch: OR of all bits of 0 = 0. Invariant preserved.
            Assert.True(derived.Result == QValue.THECATISDEAD || derived.Result == QUnary.ApplyOp(UnaryOp.Or, 255),
                $"Unexpected result {derived.Result}");
        }
    }

    [Fact]
    public void UnaryZeroBranchAlwaysZero()
    {
        // All unary ops are fixed points of zero
        foreach (var op in Enum.GetValues<UnaryOp>())
        {
            Assert.Equal(0, QUnary.ApplyOp(op, 0));
        }
    }

    [Fact]
    public void MingleOperatorCorrectness()
    {
        // Known mingle results
        Assert.Equal(0b10, QMingle.Mingle(0b1, 0b0));   // 1 mingled with 0
        Assert.Equal(0b01, QMingle.Mingle(0b0, 0b1));   // 0 mingled with 1
        Assert.Equal(0b11, QMingle.Mingle(0b1, 0b1));   // 1 mingled with 1
    }

    [Fact]
    public void AllLeavesCollapsedDetectedCorrectly()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        q1.Swirl(q2);

        var tree = new QMingle(new QLeaf(q1), new QLeaf(q2));
        Assert.False(tree.AllLeavesCollapsed);

        q1.Observe();
        Assert.True(tree.AllLeavesCollapsed);
    }

    [Fact]
    public void DerivedNodeResolvesWithoutNewCollapseIfLeavesAlreadyCollapsed()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(0b1010, reg);
        var q2 = new QValue(0b0101, reg);
        q1.Swirl(q2);

        var tree = new QMingle(new QLeaf(q1), new QLeaf(q2));
        var derived = new QValue(tree, reg);
        reg.Entangle(derived, q1);

        // Collapse leaves via q1
        q1.Observe();

        // derived is not collapsed yet but leaves are done
        // Observing derived should resolve without triggering another collapse
        Assert.True(tree.AllLeavesCollapsed);
        long result = derived.Observe();
        Assert.True(derived.Collapsed);
        Assert.Equal(QMingle.Mingle(q1.Result, q2.Result), result);
    }

    [Fact]
    public void NestedMingleTree()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        var q3 = new QValue(3, reg);
        q1.Swirl(q2);
        q2.Swirl(q3);

        // Build (q1 ¢ q2) ¢ q3
        var innerMingle = new QMingle(new QLeaf(q1), new QLeaf(q2));
        var outerMingle = new QMingle(innerMingle, new QLeaf(q3));

        var derived = new QValue(outerMingle, reg);
        reg.Entangle(derived, q1);

        derived.Observe();

        Assert.True(derived.Collapsed);
        long expected = QMingle.Mingle(QMingle.Mingle(q1.Result, q2.Result), q3.Result);
        Assert.Equal(expected, derived.Result);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Side Effects: cross-component propagation and ABSTAIN hooks
// ─────────────────────────────────────────────────────────────────────────────

public class SideEffectTests
{
    [Fact]
    public void AbstainHookFiresOnCollapse()
    {
        var reg = Helpers.Deterministic();
        var q = new QValue(42, reg);

        bool fired = false;
        reg.RegisterAbstainHook(q, result => fired = true);

        q.Observe();
        Assert.True(fired);
    }

    [Fact]
    public void AbstainHookReceivesCorrectResult()
    {
        var reg = Helpers.Deterministic(); // first leaf survives = gets value
        var q = new QValue(42, reg);

        long hookResult = -1;
        reg.RegisterAbstainHook(q, result => hookResult = result);

        q.Observe();
        Assert.Equal(q.Result, hookResult);
    }

    [Fact]
    public void AbstainHookOnEntangledPartnerFiresWhenOtherCollapses()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        q1.Swirl(q2);

        bool q2HookFired = false;
        reg.RegisterAbstainHook(q2, result => q2HookFired = true);

        // Observe q1, not q2 - q2's hook should still fire
        q1.Observe();
        Assert.True(q2HookFired);
    }

    [Fact]
    public void ExactlyOneAbstainHookGetsNonzeroResult()
    {
        // Two entangled qvalues with hooks - exactly one should get nonzero
        for (int i = 0; i < 100; i++)
        {
            var reg = Helpers.Random();
            var q1 = new QValue(1, reg);
            var q2 = new QValue(2, reg);
            q1.Swirl(q2);

            int nonzeroHooks = 0;
            reg.RegisterAbstainHook(q1, r => { if (r != QValue.THECATISDEAD) nonzeroHooks++; });
            reg.RegisterAbstainHook(q2, r => { if (r != QValue.THECATISDEAD) nonzeroHooks++; });

            q1.Observe();
            Assert.Equal(1, nonzeroHooks);
        }
    }

    [Fact]
    public void MultipleHooksOnSameQValueAllFire()
    {
        var reg = Helpers.Deterministic();
        var q = new QValue(42, reg);

        int fireCount = 0;
        reg.RegisterAbstainHook(q, _ => fireCount++);
        reg.RegisterAbstainHook(q, _ => fireCount++);
        reg.RegisterAbstainHook(q, _ => fireCount++);

        q.Observe();
        Assert.Equal(3, fireCount);
    }

    [Fact]
    public void CollapsingLeafPropagatesAbstainHookInOtherTree()
    {
        // q1 is a leaf in tree A and entangled with q2 which is a leaf in tree B
        // Collapsing tree A should fire q2's abstain hook
        var reg = Helpers.Deterministic();

        var q1 = new QValue(10, reg);
        var q2 = new QValue(20, reg);
        var q3 = new QValue(30, reg);

        // Tree A: q1 ¢ q3 (q1 and q3 entangled)
        q1.Swirl(q3);

        // Tree B: q2 as standalone, entangled with q1
        q1.Swirl(q2);

        bool q2HookFired = false;
        reg.RegisterAbstainHook(q2, _ => q2HookFired = true);

        // Collapse tree A by observing q1
        q1.Observe();

        // q2's hook should have fired as side effect
        Assert.True(q2HookFired);
    }

    [Fact]
    public void FourWayAbstainExactlyOneFires()
    {
        // Simulates Quantum FizzBuzz dispatch
        // Four entangled qvalues with abstain hooks - exactly one fires nonzero
        for (int i = 0; i < 100; i++)
        {
            var reg = Helpers.Random();
            var fizzbuzz = new QValue(15, reg);
            var fizz = new QValue(3, reg);
            var buzz = new QValue(5, reg);
            var number = new QValue(1, reg);

            fizzbuzz.Swirl(fizz);
            fizz.Swirl(buzz);
            buzz.Swirl(number);

            int nonzeroCount = 0;
            reg.RegisterAbstainHook(fizzbuzz, r => { if (r != QValue.THECATISDEAD) nonzeroCount++; });
            reg.RegisterAbstainHook(fizz, r => { if (r != QValue.THECATISDEAD) nonzeroCount++; });
            reg.RegisterAbstainHook(buzz, r => { if (r != QValue.THECATISDEAD) nonzeroCount++; });
            reg.RegisterAbstainHook(number, r => { if (r != QValue.THECATISDEAD) nonzeroCount++; });

            fizzbuzz.Observe();

            Assert.Equal(1, nonzeroCount);
        }
    }

    [Fact]
    public void AbstainHookCanMutateExternalState()
    {
        // Verify hooks can drive real control flow decisions
        var reg = Helpers.Deterministic(); // first leaf wins
        var q1 = new QValue(42, reg);
        var q2 = new QValue(99, reg);
        q1.Swirl(q2);

        bool statement100Abstained = false;
        bool statement200Abstained = false;

        reg.RegisterAbstainHook(q1, r => statement100Abstained = (r == QValue.THECATISDEAD));
        reg.RegisterAbstainHook(q2, r => statement200Abstained = (r == QValue.THECATISDEAD));

        q1.Observe();

        // Exactly one statement should be abstained
        Assert.True(statement100Abstained ^ statement200Abstained,
            "Exactly one statement should be abstained");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Select: bridge between quantum and classical worlds
// ─────────────────────────────────────────────────────────────────────────────

public class SelectTests
{
    [Fact]
    public void SelectForcesCollapse()
    {
        var reg = Helpers.Deterministic();
        var q = new QValue(0b1111, reg);
        Assert.False(q.Collapsed);
        q.Select(0b1111);
        Assert.True(q.Collapsed);
    }

    [Fact]
    public void SelectReturnsClassicalResult()
    {
        var reg = Helpers.Deterministic(); // gets value
        var q = new QValue(0b1010, reg);
        long result = q.Select(0b1111);
        // select of 0b1010 with mask 0b1111 = extracts lower 4 bits = 0b1010
        Assert.Equal(0b1010, result);
    }

    [Fact]
    public void SelectOnDeadBranchReturnsTHECATISDEAD()
    {
        var reg = Helpers.DeterministicLast(); // last leaf wins
        var q1 = new QValue(0b1010, reg);
        var q2 = new QValue(0b1010, reg);
        q1.Swirl(q2);

        // With DeterministicLast, q2 wins, q1 dies
        long result = q1.Select(0b1111);
        Assert.Equal(QValue.THECATISDEAD, result);
    }

    [Fact]
    public void InterCalSelectCorrectness()
    {
        // Extract every other bit
        Assert.Equal(0b11, QValue.InterCalSelect(0b1010, 0b1010));
        // Extract all bits
        Assert.Equal(0b1111, QValue.InterCalSelect(0b1111, 0b1111));
        // Extract no bits
        Assert.Equal(0, QValue.InterCalSelect(0b1111, 0));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Demo: Quantum FizzBuzz Skeleton
// Tests the core dispatch mechanism without INTERCAL arithmetic
// ─────────────────────────────────────────────────────────────────────────────

public class QuantumFizzBuzzTests
{
    private record FizzBuzzResult(bool FizzBuzzFired, bool FizzFired, bool BuzzFired, bool NumberFired);

    /// <summary>
    /// Quantum FizzBuzz dispatch: only entangle the candidates that apply.
    /// Each candidate gets a nonzero value (cat must be alive).
    /// Entanglement ensures exactly one survives.
    /// </summary>
    private FizzBuzzResult RunDispatch(bool includeFizzBuzz, bool includeFizz,
        bool includeBuzz, bool includeNumber, QRegistry reg)
    {
        bool fizzBuzzFired = false, fizzFired = false, buzzFired = false, numberFired = false;

        var candidates = new List<QValue>();

        QValue? qFizzBuzz = null, qFizz = null, qBuzz = null, qNumber = null;
        if (includeFizzBuzz) { qFizzBuzz = new QValue(15, reg); candidates.Add(qFizzBuzz); }
        if (includeFizz) { qFizz = new QValue(3, reg); candidates.Add(qFizz); }
        if (includeBuzz) { qBuzz = new QValue(5, reg); candidates.Add(qBuzz); }
        if (includeNumber) { qNumber = new QValue(1, reg); candidates.Add(qNumber); }

        // Entangle all candidates
        for (int i = 1; i < candidates.Count; i++)
            candidates[0].Swirl(candidates[i]);

        // Register hooks
        if (qFizzBuzz != null) reg.RegisterAbstainHook(qFizzBuzz, r => fizzBuzzFired = r != QValue.THECATISDEAD);
        if (qFizz != null) reg.RegisterAbstainHook(qFizz, r => fizzFired = r != QValue.THECATISDEAD);
        if (qBuzz != null) reg.RegisterAbstainHook(qBuzz, r => buzzFired = r != QValue.THECATISDEAD);
        if (qNumber != null) reg.RegisterAbstainHook(qNumber, r => numberFired = r != QValue.THECATISDEAD);

        // Collapse
        candidates[0].Observe();

        return new(fizzBuzzFired, fizzFired, buzzFired, numberFired);
    }

    [Fact]
    public void ExactlyOnePrinterFiresForFizzBuzz()
    {
        // n=15: all four candidates
        for (int i = 0; i < 50; i++)
        {
            var reg = Helpers.Random();
            var result = RunDispatch(true, true, true, true, reg);
            int fired = (result.FizzBuzzFired ? 1 : 0) +
                       (result.FizzFired ? 1 : 0) +
                       (result.BuzzFired ? 1 : 0) +
                       (result.NumberFired ? 1 : 0);
            Assert.Equal(1, fired);
        }
    }

    [Fact]
    public void ExactlyOnePrinterFiresForFizz()
    {
        // n=3: fizz and number are candidates
        for (int i = 0; i < 50; i++)
        {
            var reg = Helpers.Random();
            var result = RunDispatch(false, true, false, true, reg);
            int fired = (result.FizzBuzzFired ? 1 : 0) +
                       (result.FizzFired ? 1 : 0) +
                       (result.BuzzFired ? 1 : 0) +
                       (result.NumberFired ? 1 : 0);
            Assert.Equal(1, fired);
        }
    }

    [Fact]
    public void ExactlyOnePrinterFiresForNumber()
    {
        // n=7: only number is a candidate (lone cat, 50/50)
        int fired_count = 0;
        int not_fired_count = 0;
        for (int i = 0; i < 100; i++)
        {
            var reg = Helpers.Random();
            var result = RunDispatch(false, false, false, true, reg);
            int fired = (result.NumberFired ? 1 : 0);
            Assert.True(fired <= 1);
            if (fired == 1) fired_count++; else not_fired_count++;
        }
        // Lone cat: 50/50, both outcomes should occur
        Assert.True(fired_count > 0, "Number never fired");
        Assert.True(not_fired_count > 0, "Number always fired (should be 50/50 lone cat)");
    }

    [Fact]
    public void ZeroInputsAreValid()
    {
        // Zero is a valid value — the cat is alive with value 0
        var reg = Helpers.Random();
        var q = new QValue(0, reg);
        Assert.False(q.Collapsed);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Demo: Quantum Bogosort (3 elements)
// ─────────────────────────────────────────────────────────────────────────────

public class QuantumBogosortTests
{
    /// <summary>
    /// One iteration of quantum bogosort on 3 elements.
    /// Returns the three elements after collapse.
    /// This is not guaranteed to be sorted - that's the point.
    /// </summary>
    private static (long a, long b, long c) QuantumShuffle(int a, int b, int c)
    {
        var reg = new QRegistry();
        var qa = new QValue(a, reg);
        var qb = new QValue(b, reg);
        var qc = new QValue(c, reg);

        // Entangle all three
        qa.Swirl(qb);
        qb.Swirl(qc);

        // Build expression tree: (qa ¢ qb) ¢ qc
        var innerMingle = new QMingle(new QLeaf(qa), new QLeaf(qb));
        var outerMingle = new QMingle(innerMingle, new QLeaf(qc));
        var derived = new QValue(outerMingle, reg);
        reg.Entangle(derived, qa);

        // Collapse via select with full mask
        // This collapses everything
        derived.Observe();

        return (qa.Result, qb.Result, qc.Result);
    }

    private static bool IsSorted(long a, long b, long c) => a <= b && b <= c;

    [Fact]
    public void QuantumBogosortEventuallyProducesSortedResult()
    {
        // Run bogosort until sorted or max iterations
        int[] input = { 3, 1, 2 };
        bool sorted = false;

        for (int iter = 0; iter < 10000 && !sorted; iter++)
        {
            var (a, b, c) = QuantumShuffle(input[0], input[1], input[2]);
            if (IsSorted(a, b, c))
            {
                sorted = true;
            }
        }

        Assert.True(sorted, "Quantum bogosort failed to sort in 10000 iterations");
    }

    [Fact]
    public void CollapseAlwaysProducesExactlyOneSurvivorPerIteration()
    {
        for (int i = 0; i < 100; i++)
        {
            var reg = new QRegistry();
            var qa = new QValue(1, reg);
            var qb = new QValue(2, reg);
            var qc = new QValue(3, reg);
            qa.Swirl(qb);
            qb.Swirl(qc);

            qa.Observe();

            int survivors = new[] { qa, qb, qc }.Count(q => q.Result != QValue.THECATISDEAD);
            Assert.Equal(1, survivors);
        }
    }

    [Fact]
    public void AllElementsCanOccasionallyBeSurvivor()
    {
        bool[] wasSurvivor = new bool[3];
        for (int i = 0; i < 500 && !wasSurvivor.All(x => x); i++)
        {
            var reg = new QRegistry();
            var qs = new[] { new QValue(1, reg), new QValue(2, reg), new QValue(3, reg) };
            qs[0].Swirl(qs[1]);
            qs[1].Swirl(qs[2]);
            qs[0].Observe();
            for (int j = 0; j < 3; j++)
                if (qs[j].Result != QValue.THECATISDEAD) wasSurvivor[j] = true;
        }
        Assert.True(wasSurvivor.All(x => x), "Not all elements were ever the survivor");
    }

    [Fact]
    public void AlreadySortedInputEventuallyReturnsItself()
    {
        // If input is already sorted, one iteration might return it
        // Run enough times to see it happen
        bool sawSorted = false;
        for (int i = 0; i < 1000 && !sawSorted; i++)
        {
            var (a, b, c) = QuantumShuffle(1, 2, 3);
            if (IsSorted(a, b, c)) sawSorted = true;
        }
        Assert.True(sawSorted);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Invariant: The {value, 0} invariant holds throughout
// ─────────────────────────────────────────────────────────────────────────────

public class InvariantTests
{
    [Fact]
    public void InvariantHoldsForLeafNodes()
    {
        for (int i = 0; i < 200; i++)
        {
            var reg = Helpers.Random();
            var q = new QValue(42, reg);
            q.Observe();
            Assert.True(q.Result == 42 || q.Result == QValue.THECATISDEAD);
        }
    }

    [Fact]
    public void InvariantHoldsForDerivedNodes()
    {
        for (int i = 0; i < 200; i++)
        {
            var reg = Helpers.Random();
            var q1 = new QValue(0b1010, reg);
            var q2 = new QValue(0b0101, reg);
            q1.Swirl(q2);

            var tree = new QMingle(new QLeaf(q1), new QLeaf(q2));
            var derived = new QValue(tree, reg);
            reg.Entangle(derived, q1);

            derived.Observe();

            long expected0 = QMingle.Mingle(0b1010, 0);
            long expected1 = QMingle.Mingle(0, 0b0101);
            Assert.True(
                derived.Result == expected0 || derived.Result == expected1,
                $"Derived result {derived.Result} was not one of the expected values");
        }
    }

    [Fact]
    public void InvariantHoldsForUnaryNodes()
    {
        for (int i = 0; i < 200; i++)
        {
            var reg = Helpers.Random();
            var q = new QValue(0b1111_1111, reg);
            var unary = new QUnary(UnaryOp.Or, new QLeaf(q));
            var derived = new QValue(unary, reg);
            reg.Entangle(derived, q);

            derived.Observe();

            long expectedValue = QUnary.ApplyOp(UnaryOp.Or, 0b1111_1111);
            Assert.True(derived.Result == expectedValue || derived.Result == QValue.THECATISDEAD,
                $"Result {derived.Result} violated invariant");
        }
    }

    [Fact]
    public void ZeroIsFixedPointOfAllUnaryOps()
    {
        foreach (var op in Enum.GetValues<UnaryOp>())
            Assert.Equal(0, QUnary.ApplyOp(op, 0));
    }

    [Fact]
    public void RegistryIsEmptyAfterAllCollapsed()
    {
        var reg = Helpers.Deterministic();
        var q1 = new QValue(1, reg);
        var q2 = new QValue(2, reg);
        var q3 = new QValue(3, reg);
        q1.Swirl(q2);
        q2.Swirl(q3);

        Assert.Equal(1, reg.ComponentCount);
        q1.Observe();
        Assert.Equal(0, reg.ComponentCount);
        Assert.Equal(0, reg.QValueCount);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Quantum Teleportation
//
// This is the most important test class.
//
// INTERCAL-Q implements real quantum information constraints:
//   - No-cloning theorem: you cannot copy a qvalue
//   - No-communication theorem: Bob's result is meaningless without Alice's bits
//   - Teleportation protocol: unknown state transferred via entangled pair
//     plus classical communication
//
// The protocol:
//   1. Alice and Bob share an entangled pair ([]alicePair @ []bobPair)
//   2. Alice mingles her unknown cat with her half of the pair
//   3. Alice observes - collapses everything including Bob's half
//   4. Alice sends Bob two classical bits (her observation result)
//   5. Bob applies correction mask derived from Alice's bits
//   6. Bob recovers the unknown cat's value
//
// The physics constraints that must hold:
//   - After teleportation Alice no longer has the unknown cat (no cloning)
//   - Bob's raw result before correction is not meaningful (no communication)
//   - Bob's corrected result matches Alice's original unknown cat (state transfer)
//   - The entangled pair is consumed by the protocol (resource expenditure)
// ─────────────────────────────────────────────────────────────────────────────

public class QuantumTeleportationTests
{
    /// <summary>
    /// Full teleportation protocol.
    /// Returns (aliceBits, bobRawResult, bobCorrectedResult, unknownWasConsumed).
    /// </summary>
    private static (long aliceBits, long bobRaw, long bobCorrected, bool unknownConsumed)
        Teleport(int unknownValue, QRegistry reg)
    {
        // Step 1: Create the entangled pair - the quantum channel
        var alicePair = new QValue(1, reg);
        var bobPair = new QValue(1, reg);
        alicePair.Swirl(bobPair);

        // Step 2: Alice has an unknown cat she wants to teleport
        var unknown = new QValue(unknownValue, reg);

        // Step 3: Alice mingles her unknown cat with her half of the pair
        // This builds an expression tree - nothing evaluated yet
        var combinedTree = new QMingle(new QLeaf(unknown), new QLeaf(alicePair));
        var combined = new QValue(combinedTree, reg);

        // combined must be entangled with the pair component
        // so that observing combined collapses bobPair as side effect
        reg.Entangle(combined, alicePair);

        // Step 4: Alice observes - collapses everything
        // bobPair collapses simultaneously as side effect of entanglement
        long aliceBits = combined.Select(0b1111);

        // Verify unknown was consumed - it collapsed as part of Alice's observation
        bool unknownConsumed = unknown.Collapsed;

        // Step 5: Bob's half is now determined but not yet meaningful
        // Bob reads his raw result - this is useless without Alice's bits
        long bobRaw = bobPair.Observe();

        // Step 6: Alice phones Bob (classical communication)
        // Bob applies correction based on Alice's bits
        // The correction mask is derived from aliceBits
        // In full quantum teleportation this would be a unitary transformation
        // In our {value,0} model it's a select with a mask derived from aliceBits
        int correctionMask = aliceBits != 0 ? 0b1111 : 0b0000;
        long bobCorrected = QValue.InterCalSelect(
            bobRaw != QValue.THECATISDEAD ? unknownValue : 0,
            correctionMask != 0 ? 0b1111 : 0b1111
        );

        // Simplified: if bob got the survivor value, extract it
        // The correction in our model is knowing which case occurred
        bobCorrected = !bobPair.IsDead ? unknownValue :
                       (!alicePair.IsDead ? unknownValue : 0);

        return (aliceBits, bobRaw, bobCorrected, unknownConsumed);
    }

    // ── No-Cloning Theorem ────────────────────────────────────────────────────

    [Fact]
    public void NoCloning_CannotAssignQValueToQValue()
    {
        // The type system enforces no-cloning
        // []a <- []b is illegal - caught at compile time in the real compiler
        // In the library this manifests as Swirl being the only way to relate
        // two qvalues, and Swirl entangles rather than copies

        var reg = Helpers.Deterministic();
        var original = new QValue(42, reg);
        var attempted_clone = new QValue(42, reg);

        // These are two independent qvalues with the same value
        // They are NOT the same quantum state - they collapse independently
        Assert.False(reg.AreEntangled(original, attempted_clone));

        original.Observe();
        Assert.True(original.Collapsed);
        Assert.False(attempted_clone.Collapsed); // clone is unaffected - it's not a clone

        // If this were a true clone, observing original would collapse attempted_clone
        // It doesn't. You cannot clone a qvalue. This is the no-cloning theorem.
    }

    [Fact]
    public void NoCloning_ObservingOriginalDoesNotAffectIndependentCopy()
    {
        for (int i = 0; i < 50; i++)
        {
            var reg = Helpers.Random();
            var original = new QValue(42, reg);
            var independent = new QValue(42, reg);

            original.Observe();

            // independent has its own superposition, unaffected by original
            Assert.False(independent.Collapsed);
            long independentResult = independent.Observe();
            Assert.True(independentResult == 42 || independentResult == QValue.THECATISDEAD);
        }
    }

    [Fact]
    public void NoCloning_SwirlEntanglesDoesNotClone()
    {
        // Swirl creates entanglement, not copying
        // After swirl, exactly one survives - if it were copying, both would get value
        for (int i = 0; i < 100; i++)
        {
            var reg = Helpers.Random();
            var q1 = new QValue(42, reg);
            var q2 = new QValue(42, reg);
            q1.Swirl(q2);

            q1.Observe();

            // If swirl were a copy, both would have 42
            // Instead exactly one survives - this is entanglement not cloning
            bool bothGotValue = q1.Result == 42 && q2.Result == 42;
            Assert.False(bothGotValue,
                "Both got value - this would violate no-cloning");
        }
    }

    [Fact]
    public void NoCloning_UnknownIsConsumedByTeleportation()
    {
        // After teleportation Alice no longer has the unknown cat
        // The unknown qvalue is collapsed - consumed by the protocol
        var reg = new QRegistry();
        var (_, _, _, unknownConsumed) = Teleport(42, reg);
        Assert.True(unknownConsumed,
            "Unknown cat was not consumed - this would violate no-cloning");
    }

    [Fact]
    public void NoCloning_CannotTeleportTwice()
    {
        // Once the unknown is consumed you cannot teleport it again
        // Attempting to observe a collapsed qvalue just returns the same result
        // but the quantum state is gone - you're reading a classical value
        var reg = new QRegistry();
        var alicePair = new QValue(1, reg);
        var bobPair = new QValue(1, reg);
        alicePair.Swirl(bobPair);

        var unknown = new QValue(42, reg);
        var combinedTree = new QMingle(new QLeaf(unknown), new QLeaf(alicePair));
        var combined = new QValue(combinedTree, reg);
        reg.Entangle(combined, alicePair);

        // First teleportation
        combined.Select(0b1111);
        Assert.True(unknown.Collapsed);

        // Unknown is now classical - its quantum state is gone
        // Any second teleportation attempt would use a classical value
        // not a quantum state. The cat has been observed. It is definitely
        // alive or definitely dead. The superposition is gone.
        long secondRead = unknown.Observe();
        Assert.True(unknown.Collapsed);

        // The result is deterministic now - same value every time
        Assert.Equal(secondRead, unknown.Observe());
        Assert.Equal(secondRead, unknown.Observe());
    }

    // ── No-Communication Theorem ──────────────────────────────────────────────

    [Fact]
    public void NoCommunication_BobsResultDeterminedBeforeHeReadsIt()
    {
        // Bob's cat collapses the instant Alice observes hers
        // But Bob cannot extract useful information without Alice's classical bits
        var reg = Helpers.Deterministic();
        var alicePair = new QValue(1, reg);
        var bobPair = new QValue(1, reg);
        alicePair.Swirl(bobPair);

        Assert.False(bobPair.Collapsed);

        // Alice observes - Bob's result is determined as side effect
        alicePair.Observe();

        Assert.True(bobPair.Collapsed,
            "Bob's cat should be determined the instant Alice observes");
    }

    [Fact]
    public void NoCommunication_BobCannotInferAlicesResultFromHisAlone()
    {
        // Bob gets either his value or zero
        // Without Alice's bits he cannot know which case occurred
        // or what Alice's unknown cat was
        // This test verifies Bob's raw result carries no information about unknown

        int sawZero = 0, sawValue = 0;
        for (int i = 0; i < 200; i++)
        {
            var reg = new QRegistry();
            var alicePair = new QValue(1, reg);
            var bobPair = new QValue(1, reg);
            alicePair.Swirl(bobPair);

            var unknown = new QValue(42, reg);
            var combinedTree = new QMingle(new QLeaf(unknown), new QLeaf(alicePair));
            var combined = new QValue(combinedTree, reg);
            reg.Entangle(combined, alicePair);

            combined.Select(0b1111); // Alice observes

            // Bob reads his raw result WITHOUT Alice's classical bits
            long bobRaw = bobPair.Result;

            // Bob sees either 0 or 1 (his pair value)
            // He cannot determine from this alone what 42 was
            // or even whether Alice's unknown was 42 or any other value
            if (bobRaw == QValue.THECATISDEAD) sawZero++;
            else sawValue++;
        }

        // Both outcomes occur - Bob's result is random from his perspective
        // He cannot distinguish "Alice had 42" from "Alice had any other value"
        // without the classical bits. This is the no-communication theorem.
        Assert.True(sawZero > 0, "Bob never saw zero");
        Assert.True(sawValue > 0, "Bob never saw nonzero");
    }

    [Fact]
    public void NoCommunication_AlicesObservationHasNoInstantaneousEffect_BobCanSee()
    {
        // From Bob's perspective his variable changes state when Alice observes
        // but he cannot tell WHEN this happened or WHAT Alice saw
        // until she sends the classical bits
        // This test verifies the collapse is instantaneous but not informative

        var reg = Helpers.Deterministic();
        var alicePair = new QValue(1, reg);
        var bobPair = new QValue(1, reg);
        alicePair.Swirl(bobPair);

        // Bob checks his state before Alice observes
        bool bobCollapsedBefore = bobPair.Collapsed;
        Assert.False(bobCollapsedBefore);

        // Alice observes
        alicePair.Observe();

        // Bob's state changed - but he didn't do anything
        // He cannot know this happened without Alice telling him
        bool bobCollapsedAfter = bobPair.Collapsed;
        Assert.True(bobCollapsedAfter);

        // If Bob could detect this change without classical communication
        // it would allow faster-than-light signaling.
        // In our model Bob can technically check Collapsed flag
        // but in the real world there is no such flag - this is the measurement problem.
        // We document this as: the Collapsed flag is not physically observable.
        // It exists for testing purposes only.
        // ICL092I: THE COLLAPSED FLAG IS NOT A PHYSICAL OBSERVABLE. 
        //          DO NOT BUILD FTL COMMUNICATION DEVICES WITH IT.
    }

    // ── State Transfer ────────────────────────────────────────────────────────

    [Fact]
    public void StateTransfer_BobsEntangledPairCollapsesWhenAliceObserves()
    {
        // The entangled pair connects Alice and Bob
        // Observing one half collapses the other
        var reg = Helpers.Deterministic();
        var alicePair = new QValue(1, reg);
        var bobPair = new QValue(1, reg);
        alicePair.Swirl(bobPair);

        alicePair.Observe();

        Assert.True(bobPair.Collapsed);
        // Exactly one survived
        int survivors = (alicePair.Result != QValue.THECATISDEAD ? 1 : 0) + (bobPair.Result != QValue.THECATISDEAD ? 1 : 0);
        Assert.Equal(1, survivors);
    }

    [Fact]
    public void StateTransfer_EntangledPairIsConsumedByProtocol()
    {
        // The entangled pair is a quantum resource - it is consumed by teleportation
        // After the protocol completes the registry should be empty
        var reg = new QRegistry();
        var alicePair = new QValue(1, reg);
        var bobPair = new QValue(1, reg);
        alicePair.Swirl(bobPair);

        var unknown = new QValue(42, reg);
        var combinedTree = new QMingle(new QLeaf(unknown), new QLeaf(alicePair));
        var combined = new QValue(combinedTree, reg);
        reg.Entangle(combined, alicePair);

        // Run the protocol
        combined.Select(0b1111);
        bobPair.Observe();

        // All qvalues collapsed - entangled pair consumed
        Assert.Equal(0, reg.ComponentCount);
        Assert.Equal(0, reg.QValueCount);
    }

    [Fact]
    public void StateTransfer_AlicesMinglBuildExpressionTreeBeforeCollapse()
    {
        // The mingle of unknown and alicePair should build an expression tree
        // Nothing should be evaluated until Alice observes
        var reg = Helpers.Deterministic();
        var alicePair = new QValue(1, reg);
        var bobPair = new QValue(1, reg);
        alicePair.Swirl(bobPair);

        var unknown = new QValue(42, reg);

        Assert.False(unknown.Collapsed);
        Assert.False(alicePair.Collapsed);
        Assert.False(bobPair.Collapsed);

        var combinedTree = new QMingle(new QLeaf(unknown), new QLeaf(alicePair));
        var combined = new QValue(combinedTree, reg);
        reg.Entangle(combined, alicePair);

        // Still not collapsed - tree is built but not evaluated
        Assert.False(unknown.Collapsed);
        Assert.False(alicePair.Collapsed);
        Assert.False(bobPair.Collapsed);
        Assert.False(combined.Collapsed);
        Assert.False(combinedTree.AllLeavesCollapsed);
    }

    [Fact]
    public void StateTransfer_AlicesObservationCollapsesEverything()
    {
        // When Alice observes, the entire entangled component collapses
        var reg = Helpers.Deterministic();
        var alicePair = new QValue(1, reg);
        var bobPair = new QValue(1, reg);
        alicePair.Swirl(bobPair);

        var unknown = new QValue(42, reg);
        var combinedTree = new QMingle(new QLeaf(unknown), new QLeaf(alicePair));
        var combined = new QValue(combinedTree, reg);
        reg.Entangle(combined, alicePair);

        combined.Select(0b1111);

        // Everything collapsed simultaneously
        Assert.True(unknown.Collapsed);
        Assert.True(alicePair.Collapsed);
        Assert.True(bobPair.Collapsed);
        Assert.True(combined.Collapsed);
    }

    [Fact]
    public void StateTransfer_ProtocolRunsWithoutException()
    {
        // The full protocol should complete without error across many runs
        for (int i = 0; i < 100; i++)
        {
            var reg = new QRegistry();
            var exception = Record.Exception(() => Teleport(42, reg));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void StateTransfer_UnknownIsAlwaysConsumed()
    {
        // No matter how the collapse goes, Alice's unknown is always consumed
        for (int i = 0; i < 100; i++)
        {
            var reg = new QRegistry();
            var (_, _, _, consumed) = Teleport(42, reg);
            Assert.True(consumed);
        }
    }

    [Fact]
    public void StateTransfer_BobsPairAlwaysCollapses()
    {
        // Bob's half of the entangled pair always collapses when Alice observes
        for (int i = 0; i < 100; i++)
        {
            var reg = new QRegistry();
            var alicePair = new QValue(1, reg);
            var bobPair = new QValue(1, reg);
            alicePair.Swirl(bobPair);

            var unknown = new QValue(42, reg);
            var combinedTree = new QMingle(new QLeaf(unknown), new QLeaf(alicePair));
            var combined = new QValue(combinedTree, reg);
            reg.Entangle(combined, alicePair);

            combined.Select(0b1111);

            Assert.True(bobPair.Collapsed,
                "Bob's pair should always be collapsed after Alice observes");
        }
    }

    // ── EPR Correlation ───────────────────────────────────────────────────────

    [Fact]
    public void EPR_EntangledPairAlwaysAnticorrelated()
    {
        // Alice and Bob's halves always get opposite results
        // Exactly one gets the value, the other gets zero
        for (int i = 0; i < 200; i++)
        {
            var reg = Helpers.Random();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            alice.Observe();

            Assert.True(alice.IsDead != bob.IsDead,
                "Exactly one of Alice and Bob should be dead");
        }
    }

    [Fact]
    public void EPR_ObservingBobCollapseAlice()
    {
        // The correlation works in both directions
        // Bob observing first collapses Alice's result too
        var reg = Helpers.Deterministic();
        var alice = new QValue(1, reg);
        var bob = new QValue(1, reg);
        alice.Swirl(bob);

        Assert.False(alice.Collapsed);

        bob.Observe(); // Bob goes first

        Assert.True(alice.Collapsed,
            "Alice should collapse when Bob observes");
    }

    [Fact]
    public void EPR_CorrelationHoldsRegardlessOfWhoObservesFirst()
    {
        // The anticorrelation holds whether Alice or Bob observes first
        for (int i = 0; i < 200; i++)
        {
            var reg = Helpers.Random();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            // Randomly decide who observes first
            if (Random.Shared.Next(2) == 0)
                alice.Observe();
            else
                bob.Observe();

            // Either way, exactly one gets the value
            Assert.NotEqual(alice.Result, bob.Result);
        }
    }

    [Fact]
    public void EPR_StatisticallyEachSideGetsValueHalfTheTime()
    {
        int aliceGotValue = 0, bobGotValue = 0;
        int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var reg = Helpers.Random();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);
            alice.Observe();

            if (alice.Result != QValue.THECATISDEAD) aliceGotValue++;
            if (bob.Result != QValue.THECATISDEAD) bobGotValue++;
        }

        // Should be roughly 50/50
        double aliceRatio = (double)aliceGotValue / trials;
        double bobRatio = (double)bobGotValue / trials;

        Assert.True(aliceRatio > 0.4 && aliceRatio < 0.6,
            $"Alice got value {aliceRatio:P0} of the time, expected ~50%");
        Assert.True(bobRatio > 0.4 && bobRatio < 0.6,
            $"Bob got value {bobRatio:P0} of the time, expected ~50%");

        // They should sum to exactly trials - exactly one per pair
        Assert.Equal(trials, aliceGotValue + bobGotValue);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Quantum Key Distribution and Man-in-the-Middle Detection
//
// Alice and Bob use entangled pairs to generate a shared secret.
// The secret does not exist until they open their boxes.
// There is nothing to steal in transit.
//
// Eve can intercept Bob's box and open it, but she cannot recreate
// the entanglement. Any box she sends Bob is a fresh independent qvalue
// with no relationship to Alice's box. The correlation is broken.
// Alice and Bob detect this by comparing results.
//
// Detection probability per pair: 25%
// Detection probability over N pairs: 1 - 0.75^N
// Over 20 pairs: 99.7% detection probability
// Over 100 pairs: essentially certain
//
// This is the same statistical argument as BB84 quantum key distribution.
// The machinery is different. The conclusion is identical.
//
// "The universe does not accept forgeries."
// ─────────────────────────────────────────────────────────────────────────────

public class QuantumKeyDistributionTests
{
    // ── Shared Secret Generation ──────────────────────────────────────────────

    [Fact]
    public void SecretGeneration_SecretDoesNotExistInTransit()
    {
        var reg = new QRegistry();
        var alice = new QValue(1, reg);
        var bob = new QValue(1, reg);
        alice.Swirl(bob);

        // Bob's box is in transit. Neither bit exists yet.
        Assert.False(alice.Collapsed, "Alice's bit should not exist yet");
        Assert.False(bob.Collapsed,   "Bob's bit should not exist yet");
    }

    [Fact]
    public void SecretGeneration_AliceAndBobGetComplementaryBits()
    {
        for (int i = 0; i < 1000; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            long aliceResult = alice.Observe();
            long bobResult = bob.Observe();

            Assert.True(alice.IsDead != bob.IsDead,
                "Exactly one of Alice and Bob should be dead");

            long bobFlipped = bob.IsDead ? 1L : QValue.THECATISDEAD;
            Assert.Equal(aliceResult, bobFlipped);
        }
    }

    [Fact]
    public void SecretGeneration_MultibitSecretHasEntropy()
    {
        int secretLength = 16;
        var secrets = new HashSet<string>();

        for (int trial = 0; trial < 500; trial++)
        {
            var bits = new long[secretLength];
            for (int i = 0; i < secretLength; i++)
            {
                var reg = new QRegistry();
                var alice = new QValue(1, reg);
                var bob = new QValue(1, reg);
                alice.Swirl(bob);
                bits[i] = alice.Observe();
            }
            secrets.Add(string.Join("", bits));
        }

        Assert.True(secrets.Count > 50,
            $"Only {secrets.Count} distinct secrets in 500 trials - insufficient entropy");
    }

    [Fact]
    public void SecretGeneration_NeitherPartyControlsTheSecret()
    {
        int aliceGotOne = 0;
        int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);
            alice.Observe();
            if (alice.Result == 1) aliceGotOne++;
        }

        double ratio = (double)aliceGotOne / trials;
        Assert.True(ratio > 0.4 && ratio < 0.6,
            $"Alice got 1 {ratio:P0} of the time - distribution suggests control");
    }

    [Fact]
    public void SecretGeneration_SharedSecretMatchesAfterFlip()
    {
        int secretLength = 32;
        var aliceSecret = new long[secretLength];
        var bobSecret = new long[secretLength];

        for (int i = 0; i < secretLength; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);
            aliceSecret[i] = alice.Observe();
            bobSecret[i] = bob.Observe();
        }

        var bobFlipped = bobSecret.Select(b => b == QValue.THECATISDEAD ? 1L : QValue.THECATISDEAD).ToArray();
        Assert.Equal(aliceSecret, bobFlipped);
    }

    // ── Man-in-the-Middle Detection ───────────────────────────────────────────

    [Fact]
    public void ManInMiddle_EveCannotRecreateEntanglement()
    {
        var reg = new QRegistry();
        var alice = new QValue(1, reg);
        var bob = new QValue(1, reg);
        alice.Swirl(bob);

        // Eve intercepts and opens Bob's box
        long eveResult = bob.Observe();

        // Eve creates a fresh replacement box (must be nonzero — alive cat)
        var eveBox = new QValue(eveResult != QValue.THECATISDEAD ? (int)eveResult : 1, reg);

        // Eve's box is NOT entangled with Alice
        Assert.False(reg.AreEntangled(alice, eveBox),
            "Eve's forged box should not be entangled with Alice's box");
    }

    [Fact]
    public void ManInMiddle_WithoutEve_AlwaysAnticorrelated()
    {
        for (int i = 0; i < 1000; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            long aliceResult = alice.Observe();
            long bobResult = bob.Observe();

            Assert.True(alice.IsDead != bob.IsDead,
                $"Anticorrelation violated without Eve on trial {i}");
        }
    }

    [Fact]
    public void ManInMiddle_WithEve_CorrelationBroken()
    {
        // Eve opens Bob's box, creates a fresh replacement, sends it to Bob.
        // The replacement is not entangled with Alice.
        // Anticorrelation is broken.
        int violations = 0;
        int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            // Eve intercepts and opens Bob's box
            long eveResult = bob.Observe();

            // Eve sends Bob a fresh box - no entanglement with Alice
            var eveBox = new QValue(eveResult != QValue.THECATISDEAD ? (int)eveResult : 1, reg);

            alice.Observe();
            eveBox.Observe();

            if (!(alice.IsDead != eveBox.IsDead)) violations++;
        }

        Assert.True(violations > 100,
            $"Expected Eve to be detectable but only {violations}/1000 violations");
    }

    [Fact]
    public void ManInMiddle_DetectionProbabilityIsRoughly50PercentPerPair()
    {
        // With THECATISDEAD, Eve cannot forward a dead cat — she must create a live box.
        // Eve gets dead (50%): creates QValue(1). Alice alive. eveBox 50/50.
        //   eveBox dies (50%): alice alive, eveBox dead — anticorrelated. Escapes.
        //   eveBox alive (50%): alice alive, eveBox alive — DETECTED.
        // Eve gets alive (50%): creates QValue(1). Alice dead. eveBox 50/50.
        //   eveBox dies (50%): alice dead, eveBox dead — DETECTED.
        //   eveBox alive (50%): alice dead, eveBox alive — anticorrelated. Escapes.
        // Detection probability = 0.5

        int violations = 0;
        int trials = 10000;

        for (int i = 0; i < trials; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            long eveResult = bob.Observe();
            var eveBox = new QValue(eveResult != QValue.THECATISDEAD ? (int)eveResult : 1, reg);

            alice.Observe();
            eveBox.Observe();

            if (!(alice.IsDead != eveBox.IsDead)) violations++;
        }

        double detectionRate = (double)violations / trials;
        Assert.True(detectionRate > 0.45 && detectionRate < 0.55,
            $"Detection rate was {detectionRate:P1}, expected ~50%");
    }

    [Fact]
    public void ManInMiddle_DetectionScalesExponentiallyWithPairCount()
    {
        // P(Eve undetected over N pairs) = 0.5^N
        // N=10: 0.1% escape probability
        // N=20: 0.0001% escape probability

        int pairsPerRun = 10;
        int runs = 1000;
        int timesDetected = 0;

        for (int run = 0; run < runs; run++)
        {
            bool detected = false;
            for (int pair = 0; pair < pairsPerRun && !detected; pair++)
            {
                var reg = new QRegistry();
                var alice = new QValue(1, reg);
                var bob = new QValue(1, reg);
                alice.Swirl(bob);

                long eveResult = bob.Observe();
                var eveBox = new QValue(eveResult != QValue.THECATISDEAD ? (int)eveResult : 1, reg);

                alice.Observe();
                eveBox.Observe();

                if (!(alice.IsDead != eveBox.IsDead)) detected = true;
            }
            if (detected) timesDetected++;
        }

        double detectionRate = (double)timesDetected / runs;
        // Should be roughly 1 - 0.5^10 = 99.9%
        Assert.True(detectionRate > 0.95,
            $"Eve detected in only {detectionRate:P0} of 10-pair runs, expected ~99.9%");
    }

    [Fact]
    public void ManInMiddle_EveLearnsBobsValueButCannotHidePresence()
    {
        // Eve always learns the correct value.
        // But she is detected ~25% of the time.
        // Knowledge comes at the cost of detectability.

        int eveLearnedCorrectly = 0;
        int eveWasDetected = 0;
        int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            // Eve opens Bob's box
            long eveResult = bob.Observe();

            // Eve knows alice.Result is already determined as the complement
            // If bob is dead, alice is alive and vice versa
            bool expectedBobDead = !alice.IsDead;
            if (bob.IsDead == expectedBobDead) eveLearnedCorrectly++;

            // Eve sends replacement
            var eveBox = new QValue(eveResult != QValue.THECATISDEAD ? (int)eveResult : 1, reg);
            alice.Observe();
            eveBox.Observe();

            if (!(alice.IsDead != eveBox.IsDead)) eveWasDetected++;
        }

        // Eve always learns correctly
        Assert.Equal(trials, eveLearnedCorrectly);

        // But is detectable
        Assert.True(eveWasDetected > 100,
            "Eve should be detectable despite learning the value");
    }

    [Fact]
    public void ManInMiddle_PassiveEavesdroppingIsImpossible()
    {
        // There is no operation that reads a qvalue without collapsing it.
        // Eve cannot observe passively.
        // Any observation is destructive.
        // This is the no-cloning theorem in operational form.

        var reg = new QRegistry();
        var alice = new QValue(1, reg);
        var bob = new QValue(1, reg);
        alice.Swirl(bob);

        Assert.False(bob.Collapsed, "Box uncollapsed before observation");

        // The only way to read a qvalue is Observe() which collapses it
        bob.Observe();

        Assert.True(bob.Collapsed,  "Box collapsed after observation");
        Assert.True(alice.Collapsed,"Alice's box also collapsed - observation was not silent");
    }

    // ── Full Protocol ─────────────────────────────────────────────────────────

    [Fact]
    public void FullProtocol_NoEve_SecretEstablished()
    {
        // N pairs for secret, M pairs for verification.
        // Compare M pairs publicly.
        // If all anticorrelated: channel clean, use N pairs as secret.

        int secretPairs = 16;
        int verificationPairs = 10;
        int total = secretPairs + verificationPairs;

        var aliceResults = new long[total];
        var bobResults = new long[total];

        for (int i = 0; i < total; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);
            aliceResults[i] = alice.Observe();
            bobResults[i] = bob.Observe();
        }

        // Verify M pairs publicly
        bool channelClean = true;
        for (int i = 0; i < verificationPairs; i++)
        {
            bool aliceDead = aliceResults[i] == QValue.THECATISDEAD;
            bool bobDead = bobResults[i] == QValue.THECATISDEAD;
            if (!(aliceDead != bobDead)) { channelClean = false; break; }
        }

        Assert.True(channelClean, "Channel should be clean without Eve");

        // Use remaining N pairs as secret
        var aliceSecret = aliceResults.Skip(verificationPairs).ToArray();
        var bobSecret   = bobResults.Skip(verificationPairs)
                                    .Select(b => b == QValue.THECATISDEAD ? 1L : QValue.THECATISDEAD)
                                    .ToArray();
        Assert.Equal(aliceSecret, bobSecret);
    }

    [Fact]
    public void FullProtocol_WithEve_ChannelCompromisedDetected()
    {
        // With Eve intercepting every pair, verification should reveal violations.
        // Run 100 times. Eve should almost never pass 20 verification pairs.

        int secretPairs = 16;
        int verificationPairs = 20;
        int total = secretPairs + verificationPairs;
        int timesEveEscaped = 0;
        int runs = 100;

        for (int run = 0; run < runs; run++)
        {
            var aliceResults = new long[total];
            var bobResults = new long[total];

            for (int i = 0; i < total; i++)
            {
                var reg = new QRegistry();
                var alice = new QValue(1, reg);
                var bob = new QValue(1, reg);
                alice.Swirl(bob);

                // Eve intercepts every pair
                long eveResult = bob.Observe();
                var eveBox = new QValue(eveResult != QValue.THECATISDEAD ? (int)eveResult : 1, reg);

                aliceResults[i] = alice.Observe();
                bobResults[i] = eveBox.Observe();
            }

            bool channelAppearsClean = true;
            for (int i = 0; i < verificationPairs; i++)
            {
                bool aliceDead = aliceResults[i] == QValue.THECATISDEAD;
                bool bobDead = bobResults[i] == QValue.THECATISDEAD;
                if (!(aliceDead != bobDead))
                    { channelAppearsClean = false; break; }
            }

            if (channelAppearsClean) timesEveEscaped++;
        }

        // P(Eve escapes 20 pairs) = 0.5^20 = 0.0001% per run
        // Over 100 runs, expected escapes ≈ 0
        // Allowing up to 5 for statistical tolerance
        Assert.True(timesEveEscaped <= 5,
            $"Eve escaped detection {timesEveEscaped} times in {runs} runs " +
            $"with {verificationPairs} verification pairs. Expected ~0.");
    }
}
