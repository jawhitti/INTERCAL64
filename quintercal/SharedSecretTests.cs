// INTERCAL-Q: Shared Secret Generation Tests
//
// Alice and Bob generate a shared secret using entangled quantum boxes.
// The secret does not exist until they open their boxes.
// Neither party controls the outcome.
// The universe decides.
//
// Prerequisites:
//   - QRegistry.cs from the INTERCAL-Q project
//   - QValue.DEDKITTY sentinel (0x4445444B49545459)
//   - QValue.IsDead instance property
//   - xUnit test framework
//
// Note on DEDKITTY:
//   The dead cat sentinel replaces 0 as the collapse loser value.
//   Alive result: the qvalue's original value (here always 1)
//   Dead result:  QValue.DEDKITTY
//   After Bob flips: alive->1, dead->0. Alice and Bob agree.

using InterCalQ;
using Xunit;

namespace InterCalQ.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// SharedSecret: the protocol implementation
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Shared secret generation protocol.
/// Alice creates N entangled pairs and keeps one box from each.
/// Bob receives the other box from each pair.
/// Neither opens their boxes until the agreed time.
/// After opening, Bob flips his bits to match Alice.
/// </summary>
public class SharedSecret
{
    public readonly QValue[] _aliceBoxes;
    public readonly QValue[] _bobBoxes;
    private readonly int _length;
    private readonly QRegistry _registry;
    private bool _aliceOpened;
    private bool _bobOpened;

    public SharedSecret(int length, QRegistry registry)
    {
        _length = length;
        _registry = registry;
        _aliceBoxes = new QValue[length];
        _bobBoxes = new QValue[length];

        // Generate N entangled pairs
        // The secret does not exist yet
        for (int i = 0; i < length; i++)
        {
            var alice = new QValue(1, registry);
            var bob = new QValue(1, registry);
            alice.Swirl(bob);
            _aliceBoxes[i] = alice;
            _bobBoxes[i] = bob;
        }
    }

    /// <summary>
    /// Alice opens all her boxes at the agreed time.
    /// Returns her bit array: 1 for alive cat, 0 for dead cat.
    /// Collapses all pairs simultaneously via entanglement.
    /// </summary>
    public int[] AliceOpen()
    {
        if (_aliceOpened)
            throw new InterCalQException("Alice has already opened her boxes.");
        _aliceOpened = true;

        var bits = new int[_length];
        for (int i = 0; i < _length; i++)
        {
            _aliceBoxes[i].Observe();
            bits[i] = _aliceBoxes[i].IsDead ? 0 : 1;
        }
        return bits;
    }

    /// <summary>
    /// Eve opens Alice's boxes before Bob to try to steal a secret
    /// </summary>
    /// <summary>
    /// Eve intercepts Bob's boxes in transit and opens them.
    /// She learns the bits but destroys the entanglement.
    /// She creates fresh replacement boxes for Bob — but they
    /// are not entangled with Alice. The damage is done.
    /// </summary>
    public int[] EveIntercept()
    {
        var bits = new int[_length];
        for (int i = 0; i < _length; i++)
        {
            // Eve opens Bob's box — collapses both alice and bob
            _bobBoxes[i].Observe();
            bits[i] = _bobBoxes[i].IsDead ? 0 : 1;

            // Eve creates a fresh replacement box for Bob.
            // It is NOT entangled with Alice. The correlation is destroyed.
            int replacementValue = _bobBoxes[i].IsDead ? 1 : (int)_bobBoxes[i].Result;
            _bobBoxes[i] = new QValue(replacementValue, _registry);
        }
        return bits;
    }


    /// <summary>
    /// Bob opens all his boxes at the agreed time.
    /// Returns his bit array after flipping: alive->0, dead->1.
    /// Bob flips because he always gets the opposite of Alice.
    /// After flipping his bits match Alice's.
    /// </summary>
    public int[] BobOpen()
    {
        if (_bobOpened)
            throw new InterCalQException("Bob has already opened his boxes.");
        _bobOpened = true;

        var bits = new int[_length];
        for (int i = 0; i < _length; i++)
        {
            _bobBoxes[i].Observe();
            // Bob flips: dead -> 1, alive -> 0 (opposite of Alice)
            bits[i] = _bobBoxes[i].IsDead ? 1 : 0;
        }
        return bits;
    }

    /// <summary>
    /// True if all boxes are still sealed.
    /// The secret genuinely does not exist yet.
    /// </summary>
    public bool SecretDoesNotExistYet =>
        _aliceBoxes.All(q => !q.Collapsed) &&
        _bobBoxes.All(q => !q.Collapsed);
}

// ─────────────────────────────────────────────────────────────────────────────
// Tests
// ─────────────────────────────────────────────────────────────────────────────

public class SharedSecretTests
{
    // ── Property 1: Secret does not exist in transit ──────────────────────────

    [Fact]
    public void SecretDoesNotExistInTransit_BeforeAnyBoxIsOpened()
    {
        // The most important property.
        // When Bob's boxes are in transit, the bits have no value.
        // There is nothing for an eavesdropper to steal.
        var reg = new QRegistry();
        var secret = new SharedSecret(16, reg);

        Assert.True(secret.SecretDoesNotExistYet,
            "Secret should not exist before any box is opened");
    }

    [Fact]
    public void SecretDoesNotExistInTransit_AllBoxesUncollapsed()
    {
        var reg = new QRegistry();
        var alice = new QValue(1, reg);
        var bob = new QValue(1, reg);
        alice.Swirl(bob);

        // Both boxes created and entangled
        // Neither has a value yet
        Assert.False(alice.Collapsed, "Alice's box has no value yet");
        Assert.False(bob.Collapsed,   "Bob's box has no value yet");

        // The secret comes into existence only when a box is opened
        // An eavesdropper intercepting bob's box gets a superposition
        // not a value
    }

    [Fact]
    public void SecretExistsAfterOpening()
    {
        var reg = new QRegistry();
        var secret = new SharedSecret(8, reg);

        Assert.True(secret.SecretDoesNotExistYet);

        secret.AliceOpen();

        Assert.False(secret.SecretDoesNotExistYet,
            "Secret should exist after Alice opens her boxes");
    }

    // ── Property 2: Complementary bits ───────────────────────────────────────

    [Fact]
    public void ComplementaryBits_AlwaysExactlyOneAliveOneDeadPerPair()
    {
        // The W state for N=2 guarantees exactly one cat is alive.
        // Alice and Bob always get opposite results before the flip.
        for (int i = 0; i < 1000; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            long aliceResult = alice.Observe();
            long bobResult = bob.Observe();

            bool exactlyOneAlive = alice.IsDead != bob.IsDead;

            Assert.True(exactlyOneAlive,
                $"Trial {i}: expected exactly one alive cat but got " +
                $"alice={aliceResult} bob={bobResult}");
        }
    }

    [Fact]
    public void ComplementaryBits_NeverBothAlive()
    {
        for (int i = 0; i < 1000; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            alice.Observe();
            bob.Observe();

            Assert.False(!alice.IsDead && !bob.IsDead,
                "Both cats alive - W state violated");
        }
    }

    [Fact]
    public void ComplementaryBits_NeverBothDead()
    {
        for (int i = 0; i < 1000; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);

            alice.Observe();
            bob.Observe();

            Assert.False(alice.IsDead && bob.IsDead,
                "Both cats dead - W state violated");
        }
    }

    // ── Property 3: Shared secret matches after flip ──────────────────────────

    [Fact]
    public void SharedSecretMatches_SingleBit()
    {
        var reg = new QRegistry();
        var secret = new SharedSecret(1, reg);
        var aliceBits = secret.AliceOpen();
        var bobBits = secret.BobOpen();
        Assert.Equal(aliceBits, bobBits);
    }

    [Fact]
    public void SharedSecretMatches_EightBits()
    {
        var reg = new QRegistry();
        var secret = new SharedSecret(8, reg);
        var aliceBits = secret.AliceOpen();
        var bobBits = secret.BobOpen();
        Assert.Equal(aliceBits, bobBits);
    }

    [Fact]
    public void SharedSecretMatches_ThirtyTwoBits()
    {
        var reg = new QRegistry();
        var secret = new SharedSecret(32, reg);
        var aliceBits = secret.AliceOpen();
        var bobBits = secret.BobOpen();
        Assert.Equal(aliceBits, bobBits);
    }

    [Fact]
    public void SharedSecretMatches_OneHundredTwentyEightBits()
    {
        var reg = new QRegistry();
        var secret = new SharedSecret(128, reg);
        var aliceBits = secret.AliceOpen();
        var bobBits = secret.BobOpen();
        Assert.Equal(aliceBits, bobBits);
    }

    [Fact]
    public void SharedSecretMatches_AcrossManyTrials()
    {
        // Verify the match holds across many independent runs
        for (int trial = 0; trial < 100; trial++)
        {
            var reg = new QRegistry();
            var secret = new SharedSecret(16, reg);
            var aliceBits = secret.AliceOpen();
            var bobBits = secret.BobOpen();
            Assert.Equal(aliceBits, bobBits);
        }
    }

    [Fact]
    public void SharedSecretMatches_BobOpenBeforeAlice()
    {
        // Order of opening should not matter
        // The W state collapse is symmetric
        var reg = new QRegistry();
        var secret = new SharedSecret(16, reg);
        var bobBits = secret.BobOpen();    // Bob opens first
        var aliceBits = secret.AliceOpen();
        Assert.Equal(aliceBits, bobBits);
    }

    // ── Property 4: Neither party controls the secret ────────────────────────

    [Fact]
    public void NeitherPartyControls_AliceDistributionIsRoughlyFiftyFifty()
    {
        // If Alice controlled the outcome she would always get 1.
        // The distribution should be statistically 50/50.
        int aliceGotOne = 0;
        int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);
            alice.Observe();
            if (!alice.IsDead) aliceGotOne++;
        }

        double ratio = (double)aliceGotOne / trials;
        Assert.True(ratio > 0.4 && ratio < 0.6,
            $"Alice got 1 in {ratio:P0} of trials. " +
            $"Expected ~50%. Distribution suggests control.");
    }

    [Fact]
    public void NeitherPartyControls_BobDistributionIsRoughlyFiftyFifty()
    {
        int bobGotOne = 0;
        int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);
            alice.Observe(); // Alice opens first
            bob.Observe();
            if (!bob.IsDead) bobGotOne++;
        }

        double ratio = (double)bobGotOne / trials;
        Assert.True(ratio > 0.4 && ratio < 0.6,
            $"Bob got 1 in {ratio:P0} of trials. " +
            $"Expected ~50%. Distribution suggests control.");
    }

    [Fact]
    public void NeitherPartyControls_AliceAndBobSumToExactlyTrials()
    {
        // Alice got alive count + Bob got alive count = exactly trials
        // Because exactly one is alive per pair, always
        int aliceAlive = 0;
        int bobAlive = 0;
        int trials = 1000;

        for (int i = 0; i < trials; i++)
        {
            var reg = new QRegistry();
            var alice = new QValue(1, reg);
            var bob = new QValue(1, reg);
            alice.Swirl(bob);
            alice.Observe();
            bob.Observe();
            if (!alice.IsDead) aliceAlive++;
            if (!bob.IsDead)  bobAlive++;
        }

        Assert.True(trials == aliceAlive + bobAlive,
            "Alice alive + Bob alive must equal exactly trials. " +
            "Exactly one cat per pair, always.");
    }

    // ── Property 5: Entropy ───────────────────────────────────────────────────

    [Fact]
    public void Entropy_MultibitSecretHasManyDistinctValues()
    {
        // A 16-bit secret should not be the same value every time.
        // Over 500 trials we should see many distinct secrets.
        int secretLength = 16;
        var secrets = new HashSet<string>();

        for (int trial = 0; trial < 500; trial++)
        {
            var reg = new QRegistry();
            var secret = new SharedSecret(secretLength, reg);
            var bits = secret.AliceOpen();
            secrets.Add(string.Join("", bits));
            secret.BobOpen(); // consume Bob's side
        }

        Assert.True(secrets.Count > 50,
            $"Only {secrets.Count} distinct secrets in 500 trials. " +
            $"Expected > 50. Insufficient entropy.");
    }

    [Fact]
    public void Entropy_SecretContainsBothOnesAndZeros()
    {
        // A secret of length 32 should not be all ones or all zeros
        // with overwhelming probability
        bool sawOne = false;
        bool sawZero = false;

        for (int trial = 0; trial < 100 && !(sawOne && sawZero); trial++)
        {
            var reg = new QRegistry();
            var secret = new SharedSecret(32, reg);
            var bits = secret.AliceOpen();
            secret.BobOpen();

            if (bits.Any(b => b == 1)) sawOne = true;
            if (bits.Any(b => b == 0)) sawZero = true;
        }

        Assert.True(sawOne,  "Never saw a 1 bit in 100 trials of 32-bit secrets");
        Assert.True(sawZero, "Never saw a 0 bit in 100 trials of 32-bit secrets");
    }

    [Fact]
    public void Entropy_SecretIsNotPredictable()
    {
        // Two independent secrets of the same length should usually differ
        bool sawDifference = false;

        for (int trial = 0; trial < 100 && !sawDifference; trial++)
        {
            var reg1 = new QRegistry();
            var secret1 = new SharedSecret(8, reg1);
            var bits1 = secret1.AliceOpen();
            secret1.BobOpen();

            var reg2 = new QRegistry();
            var secret2 = new SharedSecret(8, reg2);
            var bits2 = secret2.AliceOpen();
            secret2.BobOpen();

            if (!bits1.SequenceEqual(bits2)) sawDifference = true;
        }

        Assert.True(sawDifference,
            "Two independent 8-bit secrets were identical in 100 trials. " +
            "This is astronomically unlikely without a bug.");
    }

    // ── Property 6: Protocol integrity ───────────────────────────────────────

    [Fact]
    public void ProtocolIntegrity_CannotOpenTwice_Alice()
    {
        var reg = new QRegistry();
        var secret = new SharedSecret(8, reg);
        secret.AliceOpen();
        Assert.Throws<InterCalQException>(() => secret.AliceOpen());
    }

    [Fact]
    public void ProtocolIntegrity_CannotOpenTwice_Bob()
    {
        var reg = new QRegistry();
        var secret = new SharedSecret(8, reg);
        secret.BobOpen();
        Assert.Throws<InterCalQException>(() => secret.BobOpen());
    }

    [Fact]
    public void ProtocolIntegrity_RegistryEmptyAfterBothOpen()
    {
        // All qvalues consumed after both parties open
        // Entangled pairs are a quantum resource - used once then gone
        var reg = new QRegistry();
        var secret = new SharedSecret(8, reg);

        Assert.Equal(16, reg.QValueCount); // 8 pairs = 16 qvalues

        secret.AliceOpen();
        secret.BobOpen();

        Assert.True(reg.QValueCount == 0,
            "All qvalues should be consumed after protocol completes. " +
            "Entangled pairs cannot be reused.");
    }

    [Fact]
    public void ProtocolIntegrity_OpeningCollapsesBothSidesSimultaneously()
    {
        // When Alice opens her box, Bob's box collapses as a side effect
        // The collapse is instantaneous and total
        var reg = new QRegistry();
        var alice = new QValue(1, reg);
        var bob = new QValue(1, reg);
        alice.Swirl(bob);

        Assert.False(bob.Collapsed, "Bob's box should be uncollapsed before Alice opens");

        alice.Observe(); // Alice opens

        Assert.True(bob.Collapsed,
            "Bob's box should be collapsed the instant Alice opens hers. " +
            "The collapse is instantaneous. " +
            "Bob does not know his result until he opens his box. " +
            "But his result is already determined.");
    }

    [Fact]
    public void ProtocolIntegrity_LongerSecretTakesMorePairs()
    {
        // Each bit requires one entangled pair
        // N bits = N pairs = 2N qvalues in registry
        foreach (int length in new[] { 1, 8, 16, 32 })
        {
            var reg = new QRegistry();
            var secret = new SharedSecret(length, reg);
            Assert.True(reg.QValueCount == length * 2,
                $"Secret of length {length} should use {length * 2} qvalues");
        }
    }

    // ── The narrative test ────────────────────────────────────────────────────

    [Fact]
    public void FullNarrative_AliceAndBobEstablishSharedSecret()
    {
        // This test tells the complete story.
        // Read it as documentation.
	
        int secretLength = 32;
        var reg = new QRegistry();

        // Alice creates 32 entangled pairs and sends Bob his boxes.
        // Neither party has opened anything yet.
        var secret = new SharedSecret(secretLength, reg);

        // The secret does not exist.
        // Bob's boxes are in transit.
        // An eavesdropper intercepting the boxes finds superpositions.
        // There is nothing to steal.
        Assert.True(secret.SecretDoesNotExistYet,
            "NARRATIVE: The secret does not exist yet. Nothing to steal.");

        // At the agreed time, Alice opens all her boxes.
        var aliceSecret = secret.AliceOpen();

        // At the agreed time, Bob opens all his boxes and flips his bits.
        var bobSecret = secret.BobOpen();

        // Alice and Bob now hold identical 32-bit secrets.
        // Neither chose the bits.
        // The universe decided.
        Assert.Equal(aliceSecret, bobSecret);

        // The secret has genuine entropy.
        Assert.True(aliceSecret.Any(b => b == 0) && aliceSecret.Any(b => b == 1),
            "NARRATIVE: The secret contains both 0s and 1s. It is not trivial.");

        // The entangled pairs are consumed. They cannot be reused.
        Assert.True(reg.QValueCount == 0,
            "NARRATIVE: The entangled pairs are consumed. " +
            "Quantum resources are spent. " +
            "To generate a new secret, new pairs must be created.");
    }
    [Fact]
    public void FullNarrative_EveTriesToInterceptSecret()
    {
        // Eve intercepts Bob's boxes in transit and opens them.
        // She learns the bits. She creates replacements and sends them to Bob.
        // But the replacements are not entangled with Alice.
        // Alice and Bob compare a subset of their bits and detect the tampering.

        int secretLength = 32;
        var reg = new QRegistry();

        // Alice creates 32 entangled pairs and sends Bob his boxes.
        var secret = new SharedSecret(secretLength, reg);

        // The secret does not exist yet.
        Assert.True(secret.SecretDoesNotExistYet);

        // Eve intercepts Bob's boxes and opens them.
        // She learns the bits but destroys the entanglement.
        var eveBits = secret.EveIntercept();

        // Alice opens her boxes — but the entanglement is already destroyed.
        var aliceSecret = secret.AliceOpen();

        // Bob opens his replacement boxes (Eve's forgeries).
        var bobSecret = secret.BobOpen();

        // Alice and Bob compare their secrets.
        // With real entanglement they would always match.
        // With Eve's forgeries, roughly half the bits disagree.
        int mismatches = 0;
        for (int i = 0; i < secretLength; i++)
        {
            if (aliceSecret[i] != bobSecret[i]) mismatches++;
        }

        // Over 32 bits, expect ~16 mismatches (50% per bit).
        // With real entanglement: 0 mismatches.
        // Eve's presence is obvious.
        Assert.True(mismatches > 5,
            $"Only {mismatches} mismatches in {secretLength} bits. " +
            "Eve should be detectable.");
    }
}
