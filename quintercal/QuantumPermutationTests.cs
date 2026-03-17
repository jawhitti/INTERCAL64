// INTERCAL-Q: Quantum Permutation / Large-Scale Collapse Tests
//
// Build a large entangled system with a deep expression tree,
// then collapse it all with one observation.
// The tree is built as a flat loop — no recursion needed.
// This maps directly to what INTERCAL can do with arrays and NEXT.

using InterCalQ;
using Xunit;
using Xunit.Abstractions;

namespace InterCalQ.Tests;

public class QuantumPermutationTests
{
    private readonly ITestOutputHelper _output;

    public QuantumPermutationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Create N entangled boxes, build a mingle chain, observe.
    /// Returns (survivorIndex, survivorValue, elapsedMs).
    /// </summary>
    private (int survivorIndex, int survivorValue, long elapsedMs)
        RunQuantumLottery(int n, QRegistry? registry = null)
    {
        var reg = registry ?? new QRegistry();
        var boxes = new QValue[n];

        // Create N boxes, each with value = index + 1
        for (int i = 0; i < n; i++)
            boxes[i] = new QValue(i + 1, reg);

        // Entangle all boxes into one component
        for (int i = 1; i < n; i++)
            boxes[0].Swirl(boxes[i]);

        // Build a deep mingle chain: mingle(box[0], mingle(box[1], ...))
        // This creates an expression tree N levels deep
        QTree tree = new QLeaf(boxes[n - 1]);
        for (int i = n - 2; i >= 0; i--)
            tree = new QMingle(new QLeaf(boxes[i]), tree);

        // Create a derived node from the tree — auto-entangles with all leaves
        var root = new QValue(tree, reg);

        // THE BIG MOMENT: observe the root.
        // The entire tree collapses in one cascade.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        root.Observe();
        sw.Stop();

        // Find the survivor
        int survivorIndex = -1;
        int survivorValue = -1;
        for (int i = 0; i < n; i++)
        {
            if (!boxes[i].IsDead)
            {
                survivorIndex = i;
                survivorValue = (int)boxes[i].Result;
                break;
            }
        }

        return (survivorIndex, survivorValue, sw.ElapsedMilliseconds);
    }

    // ── Small Scale: Correctness ────────────────────────────────────────────

    [Fact]
    public void Lottery_5_ExactlyOneSurvivor()
    {
        for (int trial = 0; trial < 50; trial++)
        {
            var reg = new QRegistry();
            var boxes = new QValue[5];
            for (int i = 0; i < 5; i++)
                boxes[i] = new QValue(i + 1, reg);
            for (int i = 1; i < 5; i++)
                boxes[0].Swirl(boxes[i]);

            boxes[0].Observe();

            int survivors = boxes.Count(b => !b.IsDead);
            Assert.Equal(1, survivors);
        }
    }

    [Fact]
    public void Lottery_5_SurvivorKeepsOriginalValue()
    {
        var reg = new QRegistry();
        var boxes = new QValue[5];
        for (int i = 0; i < 5; i++)
            boxes[i] = new QValue(i + 1, reg);
        for (int i = 1; i < 5; i++)
            boxes[0].Swirl(boxes[i]);

        boxes[0].Observe();

        var survivor = boxes.First(b => !b.IsDead);
        // Survivor's result should equal its original value
        Assert.Equal(survivor.Value, (int)survivor.Result);
    }

    [Fact]
    public void Lottery_5_AllCandidatesCanWin()
    {
        bool[] wonAtLeastOnce = new bool[5];
        for (int trial = 0; trial < 500; trial++)
        {
            var reg = new QRegistry();
            var boxes = new QValue[5];
            for (int i = 0; i < 5; i++)
                boxes[i] = new QValue(i + 1, reg);
            for (int i = 1; i < 5; i++)
                boxes[0].Swirl(boxes[i]);

            boxes[0].Observe();

            for (int i = 0; i < 5; i++)
                if (!boxes[i].IsDead) wonAtLeastOnce[i] = true;

            if (wonAtLeastOnce.All(w => w)) break;
        }

        for (int i = 0; i < 5; i++)
            Assert.True(wonAtLeastOnce[i], $"Box {i} never won in 500 trials");
    }

    [Fact]
    public void Lottery_25_ExactlyOneSurvivor()
    {
        var (idx, val, ms) = RunQuantumLottery(25);
        Assert.True(idx >= 0, "No survivor found");
        Assert.Equal(idx + 1, val);
        _output.WriteLine($"N=25: Box {idx} survived (value={val}) in {ms}ms");
    }

    [Fact]
    public void Lottery_25_WithMingleChain_ExactlyOneSurvivor()
    {
        // Build the full mingle chain and observe the root
        var reg = new QRegistry();
        var boxes = new QValue[25];
        for (int i = 0; i < 25; i++)
            boxes[i] = new QValue(i + 1, reg);
        for (int i = 1; i < 25; i++)
            boxes[0].Swirl(boxes[i]);

        QTree tree = new QLeaf(boxes[24]);
        for (int i = 23; i >= 0; i--)
            tree = new QMingle(new QLeaf(boxes[i]), tree);

        var root = new QValue(tree, reg);
        root.Observe();

        // Root should be collapsed
        Assert.True(root.Collapsed);
        // All boxes should be collapsed
        Assert.True(boxes.All(b => b.Collapsed));
        // Exactly one survivor among the leaves
        int survivors = boxes.Count(b => !b.IsDead);
        Assert.Equal(1, survivors);

        var winner = boxes.First(b => !b.IsDead);
        _output.WriteLine($"N=25 mingle chain: Box {winner.Value} survived, " +
            $"root result = {root.Result}");
    }

    [Fact]
    public void Lottery_25_RegistryEmptyAfterCollapse()
    {
        var reg = new QRegistry();
        var (_, _, _) = RunQuantumLottery(25, reg);
        Assert.Equal(0, reg.QValueCount);
        Assert.Equal(0, reg.ComponentCount);
    }

    // ── Medium Scale ────────────────────────────────────────────────────────

    [Fact]
    public void Lottery_100_ExactlyOneSurvivor()
    {
        var (idx, val, ms) = RunQuantumLottery(100);
        Assert.True(idx >= 0);
        Assert.Equal(idx + 1, val);
        _output.WriteLine($"N=100: Box {idx} survived (value={val}) in {ms}ms");
    }

    [Fact]
    public void Lottery_1000_ExactlyOneSurvivor()
    {
        var (idx, val, ms) = RunQuantumLottery(1000);
        Assert.True(idx >= 0);
        Assert.Equal(idx + 1, val);
        _output.WriteLine($"N=1000: Box {idx} survived (value={val}) in {ms}ms");
    }

    // ── Large Scale: Stress Test ────────────────────────────────────────────

    [Fact]
    public void Lottery_10000_ExactlyOneSurvivor()
    {
        var (idx, val, ms) = RunQuantumLottery(10_000);
        Assert.True(idx >= 0);
        Assert.Equal(idx + 1, val);
        _output.WriteLine($"N=10000: Box {idx} survived (value={val}) in {ms}ms");
    }

    [Fact]
    public void Lottery_100000_ExactlyOneSurvivor()
    {
        var (idx, val, ms) = RunQuantumLottery(100_000);
        Assert.True(idx >= 0);
        Assert.Equal(idx + 1, val);
        _output.WriteLine($"N=100000: Box {idx} survived (value={val}) in {ms}ms. " +
            $"100,000 cats died simultaneously.");
    }

    // ── Distribution: Large scale fairness ──────────────────────────────────

    [Fact]
    public void Lottery_25_UniformDistribution()
    {
        // Over many trials, each box should win roughly 1/25 of the time
        int n = 25;
        int trials = 2500;
        int[] wins = new int[n];

        for (int trial = 0; trial < trials; trial++)
        {
            var reg = new QRegistry();
            var boxes = new QValue[n];
            for (int i = 0; i < n; i++)
                boxes[i] = new QValue(i + 1, reg);
            for (int i = 1; i < n; i++)
                boxes[0].Swirl(boxes[i]);

            boxes[0].Observe();

            for (int i = 0; i < n; i++)
                if (!boxes[i].IsDead) wins[i]++;
        }

        double expected = (double)trials / n;
        for (int i = 0; i < n; i++)
        {
            double ratio = wins[i] / expected;
            Assert.True(ratio > 0.4 && ratio < 1.6,
                $"Box {i} won {wins[i]} times (expected ~{expected:F0}). " +
                $"Ratio {ratio:F2} outside [0.4, 1.6]");
        }

        _output.WriteLine($"Distribution over {trials} trials of {n} boxes:");
        for (int i = 0; i < n; i++)
            _output.WriteLine($"  Box {i + 1}: {wins[i]} wins ({100.0 * wins[i] / trials:F1}%)");
    }
}
