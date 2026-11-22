using ChronowalkGame.Game.Stage.Contracts;
using ChronowalkGame.Game.Stage.Objects.Runtime;
using ChronowalkGame.Game.Stage.Tiles.Definitions;
using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Runtime
{
    public class TrapRuntime : MonoBehaviour, IRoomStateful
    {
        private bool isFillableHole;
        private bool isFilled;

        public bool IsFillableHole => isFillableHole;
        public bool IsFilled => isFilled;
        public bool IsHazard => !isFilled;

        public void Configure(Trap.TrapBehavior behavior)
        {
            isFillableHole = behavior == Trap.TrapBehavior.FillableHole;
            isFilled = false;
        }

        public void FillWithBox(BoxRuntime box)
        {
            if (!isFillableHole || isFilled || box == null)
                return;

            isFilled = true;
            box.RemoveFromRoom();
        }

        public object CaptureRoomState()
        {
            return isFilled;
        }

        public void RestoreRoomState(object state)
        {
            if (state is bool filled)
                isFilled = filled;
        }
    }
}
