using UnityEngine;

namespace ChronowalkGame.Game.Stage.Contracts
{
    public interface IActorBlocker
    {
        bool BlocksMovement(Transform actor);
    }
}
