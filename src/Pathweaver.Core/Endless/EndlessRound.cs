using System.Collections.Generic;
using Pathweaver.Core.Levels;
using Pathweaver.Core.Rules;

namespace Pathweaver.Core.Endless
{
    /// <summary>
    /// One generated round: the level, and the routes it was built around.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The solution is part of the result rather than something recovered later. Endless Wayfare
    /// generates its boards by planning the routes first and deriving the board from them, so the
    /// plan is a witness that the round can be finished — no search is needed to know it, which is
    /// what makes generation cheap enough to run on a phone between rounds.
    /// </para>
    /// <para>
    /// It is also what a hint would be built from, if one is ever added.
    /// </para>
    /// </remarks>
    public sealed class EndlessRound
    {
        private readonly TilePlacement[] _solution;

        internal EndlessRound(LevelDefinition level, TilePlacement[] solution)
        {
            Level = level;
            _solution = solution;
        }

        public LevelDefinition Level { get; }

        /// <summary>
        /// The conduits the generator planned, each already oriented for its cell.
        /// </summary>
        /// <remarks>
        /// An unordered set rather than a sequence of moves. The order a player can build in
        /// depends on the order the bag deals, so a fixed move list would be a witness only for
        /// one draw sequence.
        /// </remarks>
        public IReadOnlyList<TilePlacement> Solution => _solution;

        public override string ToString() => $"{Level.Id}: {_solution.Length} conduits planned";
    }
}
