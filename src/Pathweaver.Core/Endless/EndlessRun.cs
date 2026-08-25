using System;
using Pathweaver.Core.Rules;

namespace Pathweaver.Core.Endless
{
    /// <summary>
    /// How far a player has got in Endless Wayfare.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole mode's state is three numbers: the seed the run was started with, the round it is
    /// on, and the furthest round ever reached. Everything else — the board, its endpoints, its
    /// tile supply, its target — is derived from the first two by
    /// <see cref="EndlessGenerator"/>, so nothing about a generated board is ever stored.
    /// </para>
    /// <para>
    /// That is also what makes the save file trivial and future-proof: a change to generation
    /// changes the boards a saved run produces, which is acceptable in a mode whose whole promise
    /// is that the next board is new.
    /// </para>
    /// </remarks>
    public sealed class EndlessRun
    {
        private EndlessRun(ulong seed, int round, int bestRound, int carriedPivotTokens, int carriedSkips)
        {
            Seed = seed;
            Round = round;
            BestRound = bestRound;
            CarriedPivotTokens = carriedPivotTokens;
            CarriedSkips = carriedSkips;
        }

        /// <summary>The seed the current run was started with.</summary>
        public ulong Seed { get; }

        /// <summary>The round being played, counted from one.</summary>
        public int Round { get; }

        /// <summary>The furthest round ever reached, across every run.</summary>
        public int BestRound { get; }

        /// <summary>
        /// Pivot Tokens the player still held when the last round was finished.
        /// </summary>
        /// <remarks>
        /// Carried because a Pivot Token is earned, by building a route of four conduits or more.
        /// Emptying the pool at a round boundary takes back a reward the player was already given,
        /// and on the device it read as a defect: the pips appeared and then vanished.
        /// </remarks>
        public int CarriedPivotTokens { get; }

        /// <summary>Skips the player still held when the last round was finished.</summary>
        public int CarriedSkips { get; }

        public static EndlessRun Start(ulong seed)
            => new EndlessRun(seed, round: 1, bestRound: 1, carriedPivotTokens: 0, carriedSkips: 0);

        /// <summary>
        /// Rebuilds a run from stored numbers, correcting anything impossible.
        /// </summary>
        /// <remarks>
        /// Clamped rather than rejected. A round below one, a best behind the current round, or a
        /// negative token count can only come from a damaged file or an older build, and none is
        /// worth losing a run over.
        /// </remarks>
        public static EndlessRun Of(ulong seed, int round, int bestRound, int carriedPivotTokens = 0, int carriedSkips = 0)
        {
            var safeRound = Math.Max(1, round);

            return new EndlessRun(
                seed,
                safeRound,
                Math.Max(safeRound, bestRound),
                Math.Max(0, carriedPivotTokens),
                Math.Max(0, carriedSkips));
        }

        /// <summary>The round the player is on, with whatever they are still carrying.</summary>
        public EndlessRound CurrentRound()
            => CurrentRound(TokenRules.BaseCapacity, TokenRules.BaseCapacity);

        /// <summary>
        /// The round the player is on, under the ceilings their relics have earned them.
        /// </summary>
        /// <remarks>
        /// The ceilings are passed in rather than read from the atlas here, because a run knows
        /// nothing about the World Atlas and should not have to.
        /// </remarks>
        public EndlessRound CurrentRound(int tokenCapacity, int skipCapacity)
            => EndlessGenerator.Generate(
                Round, Seed, CarriedPivotTokens, CarriedSkips, tokenCapacity, skipCapacity);

        /// <summary>
        /// Moves on after finishing the current round, keeping what is left in hand.
        /// </summary>
        /// <param name="pivotTokensLeft">Pivot Tokens unspent on the finished board.</param>
        /// <param name="skipsLeft">Skips unspent on the finished board.</param>
        public EndlessRun Cleared(int pivotTokensLeft = 0, int skipsLeft = 0)
            => new EndlessRun(
                Seed,
                Round + 1,
                Math.Max(BestRound, Round + 1),
                Math.Max(0, pivotTokensLeft),
                Math.Max(0, skipsLeft));

        /// <summary>
        /// Updates what the player is carrying without moving the run on.
        /// </summary>
        /// <remarks>
        /// Clearing a round records the tokens held at that moment, but a finished board stays
        /// playable — PRD section 3.2A rewards extending routes — so tokens can still be earned and
        /// spent afterwards. This is how the run catches up with that before the next round is dealt.
        /// </remarks>
        public EndlessRun Carrying(int pivotTokens, int skips)
            => new EndlessRun(Seed, Round, BestRound, Math.Max(0, pivotTokens), Math.Max(0, skips));

        /// <summary>
        /// Starts again from the first round, on a new seed.
        /// </summary>
        /// <remarks>
        /// A new seed rather than the old one, so starting again is a new run rather than a second
        /// attempt at boards the player has already seen. The best round survives, because it is
        /// the only lasting thing the mode has; a hoard of tokens does not, or the first round of a
        /// second attempt would be easier than the first round of the first.
        /// </remarks>
        public EndlessRun Abandoned(ulong newSeed)
            => new EndlessRun(newSeed, round: 1, BestRound, carriedPivotTokens: 0, carriedSkips: 0);

        public override string ToString() => $"endless round {Round} (best {BestRound})";
    }
}
