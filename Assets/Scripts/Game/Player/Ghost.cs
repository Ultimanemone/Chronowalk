using ChronowalkGame.Game.Stage;
using UnityEngine;

namespace ChronowalkGame.Game.PlayerEntities
{
    public class Ghost : MonoBehaviour
    {
        private LoopActionLog actionLog;
        private GridManager grid;
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
            gameObject.SetActive(true);
            transform.position = grid.CellToWorld(spawnCell);
            SetFacing(initialDirection);
        }

        public bool ReplayTurn(int turnIndex)
        {
            if (!IsReadyForTurn(turnIndex))
                return false;

            nextTurnIndex++;
            if (!IsAlive || actionLog == null || turnIndex >= actionLog.Actions.Count)
                return true;

            PlayerAction action = actionLog.Actions[turnIndex];
            RoomActionResolver.Resolve(
                action,
                transform,
                grid,
                SetFacing,
                Die);
            return true;
        }

        private void SetFacing(Direction direction)
        {
            transform.rotation = DirectionUtility.ToRotation(direction);
        }

        private void Die()
        {
            IsAlive = false;
            gameObject.SetActive(false);
        }
    }
}
