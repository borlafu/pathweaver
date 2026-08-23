using System;

namespace Pathweaver.Core.Rules
{
    /// <summary>
    /// How many Pivot Tokens a player holds.
    /// </summary>
    /// <remarks>
    /// PRD section 3.2B: tokens are spent to rotate or retrieve an already-placed
    /// tile, which removes deadlock frustration while still rewarding forward
    /// planning. The pool is a value, so an earlier count is simply an earlier
    /// value — undo and replay need no bookkeeping.
    /// </remarks>
    public readonly struct PivotTokenPool : IEquatable<PivotTokenPool>
    {
        private PivotTokenPool(int count)
        {
            Count = count;
        }

        public static PivotTokenPool Empty => new PivotTokenPool(0);

        public int Count { get; }

        /// <summary>Whether a token is available to spend.</summary>
        public bool CanSpend => Count > 0;

        public static bool operator ==(PivotTokenPool left, PivotTokenPool right) => left.Equals(right);

        public static bool operator !=(PivotTokenPool left, PivotTokenPool right) => !left.Equals(right);

        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a negative count.
        /// </exception>
        public static PivotTokenPool Of(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, "A token count cannot be negative.");
            }

            return new PivotTokenPool(count);
        }

        /// <summary>
        /// Returns a pool holding <paramref name="tokens"/> more. Earning zero is
        /// the common case, since most routes do not reach the threshold.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a negative amount; spending has its own method.
        /// </exception>
        public PivotTokenPool Earn(int tokens)
        {
            if (tokens < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tokens), tokens, "Use Spend to remove a token.");
            }

            return new PivotTokenPool(Count + tokens);
        }

        /// <summary>
        /// Returns a pool with one token fewer.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the pool is empty. Returning the pool unchanged would let
        /// the interface offer a rotation the player cannot pay for, changing the
        /// board without taking the cost.
        /// </exception>
        public PivotTokenPool Spend()
        {
            if (!CanSpend)
            {
                throw new InvalidOperationException("No Pivot Token is available to spend.");
            }

            return new PivotTokenPool(Count - 1);
        }

        public bool Equals(PivotTokenPool other) => Count == other.Count;

        public override bool Equals(object? obj) => obj is PivotTokenPool other && Equals(other);

        public override int GetHashCode() => Count.GetHashCode();

        public override string ToString() => $"{Count} Pivot Tokens";
    }
}
