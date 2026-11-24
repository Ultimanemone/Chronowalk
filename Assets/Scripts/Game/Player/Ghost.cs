using System.Collections;
using ChronowalkGame.Game.Data;
using ChronowalkGame.Game.Stage;
using UnityEngine;

namespace ChronowalkGame.Game.PlayerEntities
{
    public class Ghost : MonoBehaviour
    {
        private LoopActionLog actionLog;
        private GridManager grid;
        private ActorAnimation actorAnimation;
        private Direction facing;
        private int nextTurnIndex;

        public int LoopNumber { get; private set; }
        public int TurnsReplayed => nextTurnIndex;
        public bool IsAlive { get; private set; }
        public bool HasFinishedActions => actionLog != null && nextTurnIndex >= actionLog.Actions.Count;

        public bool IsReadyForTurn(int turnIndex)
        {
            return turnIndex == nextTurnIndex;
        }

        public void Initialize(
            int loopNumber,
            LoopActionLog recordedActions,
            Vector2Int spawnCell,
            Direction initialDirection,
            GridManager stageGrid)
        {
            if (recordedActions == null)
                throw new System.ArgumentNullException(nameof(recordedActions));
            if (stageGrid == null)
                throw new System.ArgumentNullException(nameof(stageGrid));
            if (loopNumber < 1)
                throw new System.ArgumentOutOfRangeException(nameof(loopNumber));

            LoopNumber = loopNumber;
            actionLog = recordedActions;
            grid = stageGrid;
            nextTurnIndex = 0;
            IsAlive = true;
            actorAnimation = GetComponentInChildren<ActorAnimation>(true);
            if (actorAnimation == null)
                actorAnimation = gameObject.AddComponent<ActorAnimation>();
            actorAnimation.Configure(transform);
            gameObject.SetActive(true);
            transform.position = grid.CellToWorld(spawnCell);
            SetFacing(initialDirection);
            actorAnimation.SetIdle(initialDirection);
        }

        public IEnumerator ReplayTurn(int turnIndex)
        {
            if (!IsReadyForTurn(turnIndex))
                yield break;

            nextTurnIndex++;
            if (!IsAlive || actionLog == null || turnIndex >= actionLog.Actions.Count)
                yield break;

            PlayerAction action = actionLog.Actions[turnIndex];
            if (!RoomActionResolver.ApplyAction(action, transform, grid, SetFacing, out Vector3 destination))
                yield break;

            if (action.Type == ActionType.Move)
                yield return actorAnimation.MoveTo(destination, facing);

            actorAnimation.SetIdle(facing);
        }

        private void SetFacing(Direction direction)
        {
            facing = direction;
        }

        public void Die()
        {
            IsAlive = false;
            gameObject.SetActive(false);
        }

    }
}
