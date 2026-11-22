using System.Collections.Generic;
using ChronowalkGame.Game.PlayerEntities;

namespace ChronowalkGame.Game.Stage
{
    public sealed class LoopActionLog
    {
        private readonly List<PlayerAction> actions;
        private readonly IReadOnlyList<PlayerAction> readOnlyActions;

        public IReadOnlyList<PlayerAction> Actions => readOnlyActions;

        public LoopActionLog(IEnumerable<PlayerAction> recordedActions)
        {
            actions = new List<PlayerAction>(recordedActions);
            readOnlyActions = actions.AsReadOnly();
        }
    }
}
