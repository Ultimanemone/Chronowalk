using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage;
using ChronowalkGame.Game.Stage.Contracts;
using ChronowalkGame.Game.Stage.Objects.Definitions;
using ChronowalkGame.Game.Stage.Tiles.Runtime;
using UnityEngine;

namespace ChronowalkGame.Game.Stage.Objects.Runtime
{
    public class StageObjectRuntime : MonoBehaviour, IActorBlocker, IPushable, ILaserBLocker

    {
        private StageObjectDefinition definition;
        private Transform objectRoot;
        private BoxRuntime box;
        private bool canBePushed;

        public bool BlocksLaser => definition != null && definition.BlocksLaser;
        public GameObject ObjectGameObject => objectRoot != null ? objectRoot.gameObject : gameObject;

        public void Configure(
            StageObjectDefinition objectDefinition,
            Transform root,
            bool pushable)
        {
            definition = objectDefinition;
            objectRoot = root != null ? root : transform;
            canBePushed = pushable;
            box = GetComponentInChildren<BoxRuntime>(true);
            if (box != null)
                box.Configure(objectRoot);
        }

        public bool BlocksMovement(Transform actor)
        {
            return true;
        }

        public bool CanPush(Vector2Int direction, Transform actor, GridManager stageGrid)
        {
            if (definition == null || !canBePushed ||
                stageGrid == null || !DirectionUtility.TryGetDirection(direction, out _))
                return false;

            Vector2Int targetCell = stageGrid.WorldToCell(objectRoot.position) + direction;
            return stageGrid.CanObjectEnter(targetCell, this);
        }

        public bool TryPush(Vector2Int direction, Transform actor, GridManager stageGrid)
        {
            if (!CanPush(direction, actor, stageGrid))
                return false;

            Vector2Int targetCell = stageGrid.WorldToCell(objectRoot.position) + direction;
            if (stageGrid.TryGetTrap(targetCell, out TrapRuntime trap) && !trap.IsFilled)
            {
                if (trap.IsFillableHole && box != null)
                    trap.FillWithBox(box);
                else
                    RemoveFromRoom();
                return true;
            }

            Vector3 targetPosition = stageGrid.CellToWorld(targetCell);
            targetPosition.z = objectRoot.position.z;
            objectRoot.position = targetPosition;
            return true;
        }

        private void RemoveFromRoom()
        {
            objectRoot.gameObject.SetActive(false);
        }
    }
}
