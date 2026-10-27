using ChronowalkGame.Game.Stage;
using ChronowalkGame.Utils;
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
                    break;
            }
        }

        public void Act(ActionType type, Vector2Int direction)
        {
            Act(new PlayerAction(type, direction));
        }

        public bool CanMoveTo(Vector2Int cell)
        {
            return GridManager.Instance != null && GridManager.Instance.IsWalkable(cell);
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
            if (!DirectionUtility.TryGetDirection(direction, out Direction newFacing))
                return;

            SetFacing(newFacing);
            Vector2Int targetCell = GridManager.Instance.WorldToCell(transform.position) + direction;
            if (!CanMoveTo(targetCell))
                return;

            transform.position = GridManager.Instance.CellToWorld(targetCell);
            if (GridManager.Instance.IsHazard(targetCell))
                Die();
        }

        private void Interact(Vector2Int direction)
        {
            if (!DirectionUtility.TryGetDirection(direction, out Direction newFacing))
                return;

            SetFacing(newFacing);
            Vector2Int targetCell = GridManager.Instance.WorldToCell(transform.position) + direction;
            if (!GridManager.Instance.TryGetRuntimeObject(targetCell, out GameObject target))
                return;

            foreach (MonoBehaviour behaviour in target.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IRoomInteractable interactable)
                    interactable.Interact(transform);
            }
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
