using System;

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
        private EndlessRun(ulong seed, int round, int bestRound)
        {
            Seed = seed;
            Round = round;
            BestRound = bestRound;
        }

        /// <summary>The seed the current run was started with.</summary>
        public ulong Seed { get; }

        /// <summary>The round being played, counted from one.</summary>
        public int Round { get; }

        /// <summary>The furthest round ever reached, across every run.</summary>
        public int BestRound { get; }

        public static EndlessRun Start(ulong seed) => new EndlessRun(seed, round: 1, bestRound: 1);

        /// <summary>
        /// Rebuilds a run from stored numbers, correcting anything impossible.
        /// </summary>
        /// <remarks>
        /// Clamped rather than rejected. A round below one or a best behind the current round can
        /// only come from a damaged file or an older build, and neither is worth losing a run over.
        /// </remarks>
        public static EndlessRun Of(ulong seed, int round, int bestRound)
        {
            var safeRound = Math.Max(1, round);
            return new EndlessRun(seed, safeRound, Math.Max(safeRound, bestRound));
        }

        /// <summary>The round the player is on.</summary>
        public EndlessRound CurrentRound() => EndlessGenerator.Generate(Round, Seed);

        /// <summary>Moves on after finishing the current round.</summary>
        public EndlessRun Cleared() => new EndlessRun(Seed, Round + 1, Math.Max(BestRound, Round + 1));

        /// <summary>
        /// Starts again from the first round, on a new seed.
        /// </summary>
        /// <remarks>
        /// A new seed rather than the old one, so starting again is a new run rather than a second
        /// attempt at boards the player has already seen. The best round survives, because it is
        /// the only lasting thing the mode has.
        /// </remarks>
        public EndlessRun Abandoned(ulong newSeed) => new EndlessRun(newSeed, round: 1, bestRound: BestRound);

        public override string ToString() => $"endless round {Round} (best {BestRound})";
    }
}
