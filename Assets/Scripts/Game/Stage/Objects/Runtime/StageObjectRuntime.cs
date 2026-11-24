using ChronowalkGame.Game.Data;
using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage;
using ChronowalkGame.Game.Stage.Contracts;
using ChronowalkGame.Game.Stage.Objects.Definitions;
using ChronowalkGame.Game.Stage.Tiles.Runtime;
using UnityEngine;

namespace ChronowalkGame.Game.Stage.Objects.Runtime
{
    public class StageObjectRuntime :
        MonoBehaviour,
        IActorBlocker,
        IPushable,
        ILaserBLocker,
        IDirectional,
        IRuntimeInitializable<StageObjectRuntime.InitializationData>
    {
        public readonly struct InitializationData
        {
            public StageObjectDefinition Definition { get; }
            public Transform Root { get; }
            public bool CanBePushed { get; }
            public bool HasDirection { get; }
            public Direction Direction { get; }

            public InitializationData(
                StageObjectDefinition definition,
                Transform root,
                bool canBePushed,
                bool hasDirection,
                Direction direction)
            {
                Definition = definition;
                Root = root;
                CanBePushed = canBePushed;
                HasDirection = hasDirection;
                Direction = direction;
            }
        }

        private StageObjectDefinition definition;
        private Transform objectRoot;
        private BoxRuntime box;
        private bool canBePushed;

        public bool HasDirection { get; private set; }
        public Direction Direction { get; private set; }
        public bool BlocksLaser => definition != null && definition.BlocksLaser;
        public GameObject ObjectGameObject => objectRoot != null ? objectRoot.gameObject : gameObject;

        public void Initialize(InitializationData initializationData)
        {
            definition = initializationData.Definition;
            objectRoot = initializationData.Root != null ? initializationData.Root : transform;
            canBePushed = initializationData.CanBePushed;
            HasDirection = initializationData.HasDirection;
            Direction = initializationData.Direction;
            box = GetComponentInChildren<BoxRuntime>(true);
        }

        public bool BlocksMovement(Transform actor)
        {
            return true;
        }

        public bool CanPush(Vector2Int direction, Transform actor, GridManager stageGrid)
        {
            if (definition == null || !canBePushed ||
                stageGrid == null || !DirectionUtils.TryGetDirection(direction, out _))
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
