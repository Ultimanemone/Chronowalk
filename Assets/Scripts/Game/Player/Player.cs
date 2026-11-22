using ChronowalkGame.Game.Stage;
using ChronowalkGame.Game.Stage.Contracts;
using ChronowalkGame.Utils;
using ChronowalkGame.Utils.Logger;
using UnityEngine;

namespace ChronowalkGame.Game.PlayerEntities
{
    public class Player : MonoSingleton<Player>, IRoomStateful
    {
        [SerializeField] private Direction facing = Direction.Up;

        public Direction Facing => facing;
        public bool IsAlive { get; private set; } = true;

        public void Act(PlayerAction action)
        {
            if (!IsAlive)
                return;

            switch (action.Type)
            {
                case ActionType.Move:
                    Move(action.Direction);
                    break;
                case ActionType.Interact:
                    Interact(action.Direction);
                    break;
                case ActionType.Wait:
                    ResolveAction(action);
                    break;
            }

            AdvLogger.Log(action.ToString());
        }

        public void Act(ActionType type, Vector2Int direction)
        {
            Act(new PlayerAction(type, direction));
        }

        public bool CanMoveTo(Vector2Int cell)
        {
            return GridManager.Instance != null && GridManager.Instance.IsWalkable(cell, transform);
        }

        public void SetStart(Vector2Int cell, Direction direction)
        {
            if (GridManager.Instance == null)
                return;

            transform.position = GridManager.Instance.CellToWorld(cell);
            SetFacing(direction);
            IsAlive = true;
        }

        public void Die()
        {
            if (!IsAlive)
                return;

            IsAlive = false;
            if (StageManager.Instance != null)
                StageManager.Instance.NotifyPlayerDied();
        }

        public object CaptureRoomState()
        {
            return new PlayerState(facing, IsAlive);
        }

        public void RestoreRoomState(object state)
        {
            if (!(state is PlayerState playerState))
                return;

            facing = playerState.Facing;
            IsAlive = playerState.IsAlive;
        }

        private void Move(Vector2Int direction)
        {
            ResolveAction(new PlayerAction(ActionType.Move, direction));
        }

        private void Interact(Vector2Int direction)
        {
            ResolveAction(new PlayerAction(ActionType.Interact, direction));
        }

        private void ResolveAction(PlayerAction action)
        {
            if (GridManager.Instance == null)
                return;

            RoomActionResolver.Resolve(
                action,
                transform,
                GridManager.Instance,
                SetFacing,
                Die);
        }

        private void SetFacing(Direction direction)
        {
            facing = direction;
            transform.rotation = DirectionUtility.ToRotation(direction);
        }

        private sealed class PlayerState
        {
            public Direction Facing { get; }
            public bool IsAlive { get; }

            public PlayerState(Direction facing, bool isAlive)
            {
                Facing = facing;
                IsAlive = isAlive;
            }
        }
    }
}
