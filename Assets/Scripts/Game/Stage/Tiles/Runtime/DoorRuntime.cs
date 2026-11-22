using UnityEngine;
using ChronowalkGame.Game.Stage.Contracts;

namespace ChronowalkGame.Game.Stage.Tiles.Runtime
{
    public class DoorRuntime : MonoBehaviour,
        IActorBlocker,
        IInteractable,
        IObjectEntryRule,
        IStateful
    {
        public bool IsOpen { get; private set; }
        public bool AllowsObjectEntry => IsOpen;

        public bool BlocksMovement(Transform actor)
        {
            return !IsOpen;
        }

        public void Interact(Transform actor)
        {
            IsOpen = true;
        }

        public object CaptureRoomState()
        {
            return IsOpen;
        }

        public void RestoreRoomState(object state)
        {
            if (state is bool isOpen)
                IsOpen = isOpen;
        }
    }
}
