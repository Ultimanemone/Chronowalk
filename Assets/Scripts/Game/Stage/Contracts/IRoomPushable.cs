using UnityEngine;
using ChronowalkGame.Game.Stage;

namespace ChronowalkGame.Game.Stage.Contracts
{
    public interface IRoomPushable
    {
        bool CanPush(Vector2Int direction, Transform actor, GridManager grid);
        bool TryPush(Vector2Int direction, Transform actor, GridManager grid);
    }
}
