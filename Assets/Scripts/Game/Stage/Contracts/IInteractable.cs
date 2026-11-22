using UnityEngine;

namespace ChronowalkGame.Game.Stage.Contracts
{
    public interface IInteractable
    {
        // The actor transform may belong to the player or a replaying ghost.
        void Interact(Transform actor);
    }
}
