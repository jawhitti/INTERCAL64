// INTERCAL-Q Runtime: Quantum variable registry
// Adapted from quintercal standalone library for integration with the INTERCAL runtime.

using System;
using System.Collections.Generic;
using System.Linq;

namespace INTERCAL.Runtime
{
    // ─────────────────────────────────────────────────────────────────────────
    // QValue: a value in superposition of {Value, VOID}
    // The cat is alive (Value) or dead (VOID). Opening the box collapses it.
    // ─────────────────────────────────────────────────────────────────────────

    public class QValue
    {
        /// <summary>VOID — the sentinel value for a dead cat. UINT64_MAX.</summary>
        public const ulong VOID = ulong.MaxValue;

        public int Value { get; }
        public bool Collapsed { get; internal set; }
        public ulong Result { get; internal set; }
        internal QRegistry Registry { get; }

        public QValue(int value, QRegistry registry)
        {
            Value = value;
            Registry = registry;
            registry.Register(this);
        }

        public void Swirl(QValue other)
        {
            if (Collapsed || other.Collapsed)
                throw new IntercalException("ICL077I: YOU CANNOT ENTANGLE A DEAD CAT");
            Registry.Entangle(this, other);
        }

        public ulong Observe()
        {
            if (Collapsed)
                return Result;
            Registry.Collapse(this);
            return Result;
        }

        public bool IsDead => Collapsed && Result == VOID;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // QRegistry: union-find structure tracking entanglement components
    // ─────────────────────────────────────────────────────────────────────────

    public class QRegistry
    {
        private readonly Dictionary<QValue, QValue> _parent = new();
        private readonly Dictionary<QValue, HashSet<QValue>> _components = new();
        private static readonly Random _rng = new();

        internal void Register(QValue q)
        {
            _parent[q] = q;
            _components[q] = new HashSet<QValue> { q };
        }

        private QValue Find(QValue q)
        {
            while (_parent[q] != q)
                q = _parent[q] = _parent[_parent[q]];
            return q;
        }

        public void Entangle(QValue a, QValue b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA == rootB) return; // idempotent

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

        public bool AreEntangled(QValue a, QValue b)
        {
            if (!_parent.ContainsKey(a) || !_parent.ContainsKey(b)) return false;
            return Find(a) == Find(b);
        }

        public void Collapse(QValue trigger)
        {
            if (!_parent.ContainsKey(trigger))
                throw new IntercalException("ICL091I: YOUR OBSERVATION HAS DISTURBED THE SYSTEM");

            var root = Find(trigger);
            var component = _components[root].ToList();
            var leaves = component.Where(q => true).ToList(); // all are leaves for now

            // Lone cat: 50/50 coin flip. Entangled: exactly one survivor.
            QValue survivor;
            if (leaves.Count == 1)
                survivor = _rng.Next(2) == 0 ? leaves[0] : null;
            else
                survivor = leaves[_rng.Next(leaves.Count)];

            foreach (var leaf in leaves)
            {
                leaf.Collapsed = true;
                leaf.Result = (leaf == survivor) ? (ulong)leaf.Value : QValue.VOID;
            }

            // Remove component from registry
            foreach (var node in component)
                _parent.Remove(node);
            _components.Remove(root);
        }
    }
}
