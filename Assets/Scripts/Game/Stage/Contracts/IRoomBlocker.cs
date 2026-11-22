using UnityEngine;

namespace ChronowalkGame.Game.Stage.Contracts
{
    public interface IRoomBlocker
    {
        bool BlocksMovement(Transform actor);
    }
}
