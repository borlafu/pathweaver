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
    /// <para>
    /// A pool also carries its own ceiling, and earning past it pays nothing. The ceiling belongs
    /// here rather than being checked wherever tokens are granted, because it was not checked
    /// anywhere: tokens accumulated without limit while the interface showed a column of three,
    /// so a player could hold six of something the game claimed a maximum of three of. What the
    /// band of legal ceilings is remains a rules question — see
    /// <see cref="TokenRules.CapacityWith"/> — and this type only guarantees that a pool never
    /// holds more than the ceiling it was given.
    /// </para>
    /// </remarks>
    public readonly struct TokenPool : IEquatable<TokenPool>
    {
        private TokenPool(int count, int capacity)
        {
            Count = count;
            Capacity = capacity;
        }

        public static TokenPool Empty => new TokenPool(0, TokenRules.BaseCapacity);

        public int Count { get; }

        /// <summary>
        /// The most this pool can hold.
        /// </summary>
        /// <remarks>
        /// Travels with the pool rather than being looked up, because it is not one number for the
        /// whole game: relics raise it, so two boards can legitimately have different ceilings, and
        /// a resumed board has to remember the one it was dealt.
        /// </remarks>
        public int Capacity { get; }

        /// <summary>Whether a token is available to spend.</summary>
        public bool CanSpend => Count > 0;

        /// <summary>Whether earning would now pay nothing.</summary>
        public bool IsFull => Count >= Capacity;

        public static bool operator ==(TokenPool left, TokenPool right) => left.Equals(right);

        public static bool operator !=(TokenPool left, TokenPool right) => !left.Equals(right);

        /// <summary>
        /// A pool at the standard ceiling.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a negative count, or one above the standard ceiling.
        /// </exception>
        public static TokenPool Of(int count) => Of(count, TokenRules.BaseCapacity);

        /// <summary>
        /// A pool at a given ceiling.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for a negative count, a ceiling below one, or a count above the ceiling. A count
        /// over the ceiling is a caller's arithmetic mistake and is refused rather than clamped —
        /// silently discarding a token the player was promised is worse than failing where the
        /// promise was made. The one place a count legitimately arrives too large is an old save,
        /// which clamps deliberately and says so.
        /// </exception>
        public static TokenPool Of(int count, int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "A pool that can hold nothing is not a pool.");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, "A token count cannot be negative.");
            }

            if (count > capacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, $"A pool holding {count} exceeds its ceiling of {capacity}.");
            }

            return new TokenPool(count, capacity);
        }

        /// <summary>
        /// Returns a pool holding <paramref name="tokens"/> more, up to its ceiling. Earning zero is
        /// the common case, since most routes do not reach the threshold.
        /// </summary>
        /// <remarks>
        /// Earning into a full pool pays nothing rather than failing. A completed route is a legal
        /// move whose reward happens to be spent already, and refusing the move over it would mean
        /// a full pool made the board unplayable.
        /// </remarks>
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

            return new TokenPool(Math.Min(Count + tokens, Capacity), Capacity);
        }

        /// <summary>
        /// The same pool under a different ceiling, keeping as much of the count as still fits.
        /// </summary>
        /// <remarks>
        /// What a carried count needs when it meets a board whose ceiling is lower than the one it
        /// was earned under — a run played with relics unlocked, resumed after they were spent
        /// elsewhere, or a save written before ceilings existed at all.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for a ceiling below one.</exception>
        public TokenPool Capped(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "A pool that can hold nothing is not a pool.");
            }

            return new TokenPool(Math.Min(Count, capacity), capacity);
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

            return new TokenPool(Count - 1, Capacity);
        }

        public bool Equals(TokenPool other) => Count == other.Count && Capacity == other.Capacity;

        public override bool Equals(object? obj) => obj is TokenPool other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Count, Capacity);

        public override string ToString() => $"{Count} of {Capacity} tokens";
    }
}
