// INTERCAL-Q: QRegistry Core Library
// Quantum variable registry with entanglement, collapse, and expression trees.
// This library is intentionally divorced from the INTERCAL runtime.
// It implements the quantum model described in INTERCAL-Q-design.md.

namespace InterCalQ;

// ─────────────────────────────────────────────────────────────────────────────
// QTree: expression tree nodes
// Only mingle and unary operators can appear in a quantum expression tree.
// Select always forces collapse and therefore never appears in the tree.
// ─────────────────────────────────────────────────────────────────────────────

public abstract class QTree
{
    /// <summary>
    /// Evaluate the tree after all leaves have been collapsed.
    /// Pure function — no side effects.
    /// </summary>
    public abstract long Evaluate();

    /// <summary>
    /// True if all leaf nodes in this tree have been collapsed.
    /// </summary>
    public abstract bool AllLeavesCollapsed { get; }

    /// <summary>
    /// Collect all QValues referenced by leaf nodes in this tree.
    /// </summary>
    public abstract IEnumerable<QValue> GetLeaves();

    /// <summary>
    /// Iterative post-order evaluation. Avoids stack overflow on deep trees.
    /// </summary>
    public static long EvaluateIterative(QTree root)
    {
        // Post-order traversal using two stacks
        var work = new Stack<QTree>();
        var values = new Stack<long>();
        // Use a marker to distinguish "push children" from "combine results"
        var ops = new Stack<object>(); // QMingle or QUnary marker

        work.Push(root);
        while (work.Count > 0)
        {
            var node = work.Pop();
            if (node is QLeaf leaf)
            {
                values.Push(leaf.Value.Result);
            }
            else if (node is QMingle m)
            {
                ops.Push(m);
                work.Push(node); // revisit marker
                work.Push(m.Right);
                work.Push(m.Left);
            }
            else if (node is QUnary u)
            {
                ops.Push(u);
                work.Push(node); // revisit marker
                work.Push(u.Operand);
            }
        }

        // That approach is tricky with revisit detection. Simpler:
        // Flatten to leaves, then re-fold with the tree structure.
        // Since we only have Mingle and Unary, and our trees are
        // always right-leaning chains for the permutation case,
        // just use an explicit stack-based evaluator.

        values.Clear();
        var evalWork = new Stack<(QTree node, bool childrenPushed)>();
        evalWork.Push((root, false));

        while (evalWork.Count > 0)
        {
            var (node, pushed) = evalWork.Pop();

            if (node is QLeaf lf)
            {
                values.Push(lf.Value.Result);
            }
            else if (node is QMingle mg)
            {
                if (pushed)
                {
                    var right = values.Pop();
                    var left = values.Pop();
                    values.Push(QMingle.Mingle(left, right));
                }
                else
                {
                    evalWork.Push((node, true));
                    evalWork.Push((mg.Right, false));
                    evalWork.Push((mg.Left, false));
                }
            }
            else if (node is QUnary un)
            {
                if (pushed)
                {
                    var val = values.Pop();
                    values.Push(QUnary.ApplyOp(un.Op, val));
                }
                else
                {
                    evalWork.Push((node, true));
                    evalWork.Push((un.Operand, false));
                }
            }
        }

        return values.Pop();
    }
}

public class QLeaf : QTree
{
    public QValue Value { get; }

    public QLeaf(QValue value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override long Evaluate() => Value.Result;
    public override bool AllLeavesCollapsed => Value.Collapsed;
    public override IEnumerable<QValue> GetLeaves() { yield return Value; }
}

public class QMingle : QTree
{
    public QTree Left { get; }
    public QTree Right { get; }

    public QMingle(QTree left, QTree right)
    {
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public override long Evaluate() => QTree.EvaluateIterative(this);
    public override bool AllLeavesCollapsed => GetLeaves().All(v => v.Collapsed);
    public override IEnumerable<QValue> GetLeaves()
    {
        // Iterative traversal to avoid stack overflow on deep trees
        var stack = new Stack<QTree>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node is QLeaf leaf)
                yield return leaf.Value;
            else if (node is QMingle m)
            {
                stack.Push(m.Right);
                stack.Push(m.Left);
            }
            else if (node is QUnary u)
                stack.Push(u.Operand);
        }
    }

    /// <summary>
    /// Interleave bits of two 16-bit values into one 32-bit value.
    /// This is INTERCAL's mingle operator.
    /// </summary>
    public static long Mingle(long a, long b)
    {
        // Dead cats contribute nothing — substitute 0
        if (a == QValue.DEDKITTY) a = 0;
        if (b == QValue.DEDKITTY) b = 0;

        long result = 0;
        for (int i = 0; i < 16; i++)
        {
            long bitA = (a >> i) & 1;
            long bitB = (b >> i) & 1;
            result |= (bitA << (2 * i + 1));
            result |= (bitB << (2 * i));
        }
        return result;
    }
}

public enum UnaryOp { And, Or, Xor }

public class QUnary : QTree
{
    public UnaryOp Op { get; }
    public QTree Operand { get; }

    public QUnary(UnaryOp op, QTree operand)
    {
        Op = op;
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
    }

    public override long Evaluate() => QTree.EvaluateIterative(this);
    public override bool AllLeavesCollapsed => Operand.AllLeavesCollapsed;
    public override IEnumerable<QValue> GetLeaves() => Operand.GetLeaves();

    /// <summary>
    /// Apply unary bitwise operator across all bits of value.
    /// Zero is a fixed point of all three operators, preserving the {value, 0} invariant.
    /// </summary>
    public static long ApplyOp(UnaryOp op, long value) => value == QValue.DEDKITTY ? QValue.DEDKITTY : op switch
    {
        UnaryOp.And => AndBits(value),
        UnaryOp.Or  => OrBits(value),
        UnaryOp.Xor => XorBits(value),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static long AndBits(long value)
    {
        long bit = 1;
        for (int i = 0; i < 16; i++) bit &= (value >> i) & 1;
        return bit == 1 ? 0xFFFF : 0;
    }

    private static long OrBits(long value)
    {
        long bit = 0;
        for (int i = 0; i < 16; i++) bit |= (value >> i) & 1;
        return bit == 1 ? 0xFFFF : 0;
    }

    private static long XorBits(long value)
    {
        long bit = 0;
        for (int i = 0; i < 16; i++) bit ^= (value >> i) & 1;
        return bit == 1 ? 0xFFFF : 0;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// QValue: a value in superposition of {Value, DEAD}
// The cat is alive (Value) or dead (DEDKITTY). Opening the box collapses it.
// ─────────────────────────────────────────────────────────────────────────────

public class QValue
{
    /// <summary>"THECATIS" in ASCII — the sentinel value for a dead cat.</summary>
    public const long DEDKITTY = 0x4445444B49545459;

    /// <summary>The classical value this wraps. The "color of the cat".</summary>
    public int Value { get; }

    /// <summary>Whether this qvalue has been observed.</summary>
    public bool Collapsed { get; internal set; }

    /// <summary>The classical result after collapse. Either Value or DEDKITTY.</summary>
    public long Result { get; internal set; }

    /// <summary>
    /// Expression tree for derived qvalues (result of quantum mingle).
    /// Null for leaf qvalues created directly from classical values.
    /// </summary>
    public QTree? Tree { get; }

    /// <summary>The registry this qvalue is registered with.</summary>
    internal QRegistry Registry { get; }

    /// <summary>
    /// Create a leaf qvalue from a classical value.
    /// Equivalent to DO []n <- #value
    /// </summary>
    public QValue(int value, QRegistry registry)
    {
        Value = value;
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        registry.Register(this);
    }

    /// <summary>
    /// Create a derived qvalue from an expression tree.
    /// All leaf QValues in the tree are automatically entangled with this node.
    /// Equivalent to DO []n <- []a ¢ []b
    /// </summary>
    public QValue(QTree tree, QRegistry registry)
    {
        Tree = tree ?? throw new ArgumentNullException(nameof(tree));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Value = 0; // will be determined at collapse time
        registry.Register(this);

        // Auto-entangle all leaves in the tree with this derived node
        foreach (var leaf in tree.GetLeaves())
        {
            if (!leaf.Collapsed && !registry.AreEntangled(this, leaf))
                registry.Entangle(this, leaf);
        }
    }

    /// <summary>
    /// Entangle this qvalue with another. The swirl operator @.
    /// Merges the two registry components.
    /// </summary>
    public void Swirl(QValue other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        if (Collapsed || other.Collapsed)
            throw new InterCalQException("ICL077I: YOU CANNOT ENTANGLE A DEAD CAT");
        Registry.Entangle(this, other);
    }

    /// <summary>
    /// Observe this qvalue, collapsing it and all entangled qvalues.
    /// Returns the classical result (Value or 0).
    /// Equivalent to DO .n <- []m
    /// </summary>
    public long Observe()
    {
        if (Collapsed)
            return Result;

        if (Tree != null && Tree.AllLeavesCollapsed)
        {
            Collapsed = true;
            Result = Tree.Evaluate();
            return Result;
        }

        Registry.Collapse(this);
        return Result;
    }

    /// <summary>True if this cat is dead after observation.</summary>
    /// <summary>True if this cat is dead after observation.</summary>
    public bool IsDead => Collapsed && Result == DEDKITTY;

    /// <summary>
    /// Apply select with a classical mask, forcing decoherence.
    /// Equivalent to DO .n <- []m ~ #mask
    /// </summary>
    public long Select(int mask)
    {
        long val = Observe();
        if (val == DEDKITTY) return DEDKITTY;
        return InterCalSelect((int)val, mask);
    }

    /// <summary>
    /// INTERCAL select operator: extract bits from left at positions set in right.
    /// </summary>
    public static int InterCalSelect(int value, int mask)
    {
        int result = 0;
        int bit = 0;
        for (int i = 0; i < 32; i++)
        {
            if (((mask >> i) & 1) == 1)
            {
                result |= ((value >> i) & 1) << bit;
                bit++;
            }
        }
        return result;
    }

    public override string ToString() =>
        Collapsed
            ? (Result == DEDKITTY ? "QValue(DEAD)" : $"QValue(collapsed={Result})")
            : $"QValue(superposed={Value}|DEAD)";
}

// ─────────────────────────────────────────────────────────────────────────────
// QRegistry: union-find structure tracking entanglement components
// ─────────────────────────────────────────────────────────────────────────────

public class QRegistry
{
    private readonly Dictionary<QValue, QValue> _parent = new();
    private readonly Dictionary<QValue, HashSet<QValue>> _components = new();
    private readonly Dictionary<QValue, List<Action<long>>> _abstainHooks = new();

    // Injected random source for testability
    private readonly Func<int, int> _random;

    public QRegistry(Func<int, int>? random = null)
    {
        // random(n) returns a value in [0, n)
        _random = random ?? (n => Random.Shared.Next(n));
    }

    /// <summary>
    /// Register a new qvalue as its own component.
    /// Called automatically by QValue constructor.
    /// </summary>
    internal void Register(QValue q)
    {
        _parent[q] = q;
        _components[q] = new HashSet<QValue> { q };
    }

    /// <summary>
    /// Find the root of the component containing q. With path compression.
    /// </summary>
    public QValue Find(QValue q)
    {
        while (_parent[q] != q)
            q = _parent[q] = _parent[_parent[q]];
        return q;
    }

    /// <summary>
    /// Merge the components containing a and b. The swirl operator.
    /// </summary>
    public void Entangle(QValue a, QValue b)
    {
        var rootA = Find(a);
        var rootB = Find(b);

        if (rootA == rootB)
            return; // Already entangled — idempotent

        // Union by size: merge smaller into larger
        var compA = _components[rootA];
        var compB = _components[rootB];

        if (compA.Count < compB.Count)
        {
            (rootA, rootB) = (rootB, rootA);
            (compA, compB) = (compB, compA);
        }

        _parent[rootB] = rootA;
        compA.UnionWith(compB);
        _components.Remove(rootB);
    }

    /// <summary>
    /// Check if two qvalues are in the same entangled component.
    /// </summary>
    public bool AreEntangled(QValue a, QValue b)
    {
        // Collapsed values are no longer in the registry
        if (!_parent.ContainsKey(a) || !_parent.ContainsKey(b)) return false;
        return Find(a) == Find(b);
    }

    /// <summary>
    /// Register a quantum ABSTAIN hook on a qvalue.
    /// The hook fires when the qvalue collapses, receiving the classical result.
    /// hook(result): if result == 0, abstain; if result != 0, reinstate.
    /// </summary>
    public void RegisterAbstainHook(QValue q, Action<long> hook)
    {
        if (!_abstainHooks.ContainsKey(q))
            _abstainHooks[q] = new List<Action<long>>();
        _abstainHooks[q].Add(hook);
    }

    /// <summary>
    /// Collapse the entire entangled component containing trigger.
    /// Exactly one leaf node survives with its value; all others get zero.
    /// Derived nodes evaluate from their now-collapsed inputs.
    /// All abstain hooks fire.
    /// </summary>
    public void Collapse(QValue trigger)
    {
        if (!_parent.ContainsKey(trigger))
            throw new InterCalQException("ICL091I: CANNOT COLLAPSE UNREGISTERED QVALUE");

        var root = Find(trigger);
        var component = _components[root].ToList();

        // Separate leaves (created from classical values) from derived nodes (expression trees)
        var leaves = component.Where(q => q.Tree == null).ToList();
        var derived = component.Where(q => q.Tree != null).ToList();

        if (leaves.Count == 0)
            throw new InterCalQException("ICL091I: COMPONENT HAS NO LEAVES. THIS SHOULD NOT HAPPEN.");

        // Collapse rule depends on whether the cat is alone or entangled:
        //   Lone cat: Schrödinger's classic 50/50 coin flip (alive or dead)
        //   Entangled cats: exactly one survivor, uniform random among the group
        QValue? survivor;
        if (leaves.Count == 1)
            survivor = _random(2) == 0 ? leaves[0] : null;
        else
            survivor = leaves[_random(leaves.Count)];

        foreach (var leaf in leaves)
        {
            leaf.Collapsed = true;
            leaf.Result = (leaf == survivor) ? leaf.Value : QValue.DEDKITTY;
        }

        // Resolve derived nodes - tree is now fully evaluable
        // Order doesn't matter because Evaluate() is recursive and pure
        foreach (var node in derived)
        {
            node.Collapsed = true;
            node.Result = node.Tree!.Evaluate();
        }

        // Fire abstain hooks - global side effects
        // All nodes in component fire, in registration order
        foreach (var node in component)
        {
            if (_abstainHooks.TryGetValue(node, out var hooks))
            {
                foreach (var hook in hooks)
                    hook(node.Result);
            }
        }

        // Remove component from registry
        foreach (var node in component)
            _parent.Remove(node);
        _components.Remove(root);
    }

    /// <summary>
    /// Number of currently registered entangled components.
    /// Useful for testing and debugging.
    /// </summary>
    public int ComponentCount => _components.Count;

    /// <summary>
    /// Number of currently registered qvalues.
    /// </summary>
    public int QValueCount => _parent.Count;

    /// <summary>
    /// Get all qvalues in the same component as q.
    /// Useful for testing.
    /// </summary>
    public IReadOnlySet<QValue> GetComponent(QValue q)
    {
        var root = Find(q);
        return _components[root];
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Exception
// ─────────────────────────────────────────────────────────────────────────────

public class InterCalQException : Exception
{
    public InterCalQException(string message) : base(message) { }
}
