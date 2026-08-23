using System;

namespace Pathweaver.Core.Rules
{
    /// <summary>
    /// A count of spendable tokens.
    /// </summary>
    /// <remarks>
    /// Used for both currencies the player carries: Pivot Tokens, which take a placed
    /// conduit back off the board per PRD section 3.2B, and skips, which discard the
    /// tile in hand for the next one. They behave identically — earn, spend, never go
    /// negative — so they share a type rather than duplicating one.
    /// <para>
    /// The pool is a value, so an earlier count is simply an earlier value and undo
    /// and replay need no bookkeeping.
    /// </para>
    /// </remarks>
    public readonly struct TokenPool : IEquatable<TokenPool>
    {
        private TokenPool(int count)
        {
            Count = count;
        }

        public static TokenPool Empty => new TokenPool(0);

        public int Count { get; }

        /// <summary>Whether a token is available to spend.</summary>
        public bool CanSpend => Count > 0;

        public static bool operator ==(TokenPool left, TokenPool right) => left.Equals(right);

        public static bool operator !=(TokenPool left, TokenPool right) => !left.Equals(right);

        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a negative count.
        /// </exception>
        public static TokenPool Of(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, "A token count cannot be negative.");
            }

            return new TokenPool(count);
        }

        /// <summary>
        /// Returns a pool holding <paramref name="tokens"/> more. Earning zero is
        /// the common case, since most routes do not reach the threshold.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a negative amount; spending has its own method.
        /// </exception>
        public TokenPool Earn(int tokens)
        {
            if (tokens < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tokens), tokens, "Use Spend to remove a token.");
            }

            return new TokenPool(Count + tokens);
        }

        /// <summary>
        /// Returns a pool with one token fewer.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the pool is empty. Returning the pool unchanged would let the
        /// interface offer an action the player cannot pay for, changing the board
        /// without taking the cost.
        /// </exception>
        public TokenPool Spend()
        {
            if (!CanSpend)
            {
                throw new InvalidOperationException("No token is available to spend.");
            }

            return new TokenPool(Count - 1);
        }

        public bool Equals(TokenPool other) => Count == other.Count;

        public override bool Equals(object? obj) => obj is TokenPool other && Equals(other);

        public override int GetHashCode() => Count.GetHashCode();

        public override string ToString() => $"{Count} tokens";
    }
}
