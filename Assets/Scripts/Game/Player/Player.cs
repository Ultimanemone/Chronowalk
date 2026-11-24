using System.Collections;
using ChronowalkGame.Game.Data;
using ChronowalkGame.Game.Stage;
using ChronowalkGame.Game.Stage.Contracts;
using ChronowalkGame.Utils;
using ChronowalkGame.Utils.Logger;
using UnityEngine;

namespace ChronowalkGame.Game.PlayerEntities
{
    public class Player : MonoSingleton<Player>, IStateful
    {
        [SerializeField] private Direction facing = Direction.Up;
        private ActorAnimation actorAnimation;

        public Direction Facing => facing;
        public bool IsAlive { get; private set; } = true;

        protected override void Awake()
        {
            base.Awake();
            actorAnimation = GetComponentInChildren<ActorAnimation>(true);
            if (actorAnimation == null)
                Debug.LogError("Player requires an ActorAnimation component on its Animator object.", this);
            else
                actorAnimation.Configure(transform);
        }

        public IEnumerator PlayAction(PlayerAction action, GridManager grid)
        {
            if (!IsAlive)
                yield break;

            if (!RoomActionResolver.ApplyAction(action, transform, grid, SetFacing, out Vector3 destination))
                yield break;

            if (action.Type == ActionType.Move)
            {
                if (actorAnimation != null)
                    yield return actorAnimation.MoveTo(destination, facing);
                else
                    transform.position = destination;
            }

            AdvLogger.Log(action.ToString());
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
            ShowIdleAnimation();
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

        public void ShowIdleAnimation()
        {
            actorAnimation?.SetIdle(facing);
        }

        public void InterruptAnimation()
        {
            actorAnimation?.Interrupt(facing);
        }

        public IEnumerator AnimateResetTo(Vector3 destination, Direction direction)
        {
            if (actorAnimation != null)
                yield return actorAnimation.MoveTo(destination, direction);
            else
                transform.position = destination;

            ShowIdleAnimation();
        }

        private void SetFacing(Direction direction)
        {
            facing = direction;
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
