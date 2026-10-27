using System;
using UnityEngine;

namespace ChronowalkGame.Game.PlayerEntities
{
    [Serializable]
    public struct PlayerAction
    {
        public ActionType Type;
        public Vector2Int Direction;

        public PlayerAction(ActionType type, Vector2Int direction)
        {
            Type = type;
            Direction = direction;
        }

        public bool IsReplayable => Type == ActionType.Move ||
                                    Type == ActionType.Wait ||
                                    Type == ActionType.Interact;
    }
}
