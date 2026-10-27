using System.Collections.Generic;
using ChronowalkGame.Game.PlayerEntities;

namespace ChronowalkGame.Game.Stage
{
    public sealed class LoopActionLog
    {
        private readonly List<PlayerAction> actions;

        public IReadOnlyList<PlayerAction> Actions => actions;

        public LoopActionLog(IEnumerable<PlayerAction> recordedActions)
        {
            actions = new List<PlayerAction>(recordedActions);
        }
    }
}
