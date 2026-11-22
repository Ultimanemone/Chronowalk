using System.Collections.Generic;
using ChronowalkGame.Game.Stage.Contracts;
using ChronowalkGame.Game.Stage.Data;
using ChronowalkGame.Game.Stage.Objects.Runtime;
using ChronowalkGame.Game.Stage.Tiles.Definitions;
using ChronowalkGame.Game.Stage.Tiles.Runtime;
using ChronowalkGame.Utils;
using UnityEngine;

namespace ChronowalkGame.Game.Stage
{
    public class GridManager : MonoSingleton<GridManager>
    {
        private Dictionary<Vector2Int, StageTileDefinition> tiles = new Dictionary<Vector2Int, StageTileDefinition>();
        private Dictionary<Vector2Int, GameObject> tileRuntimeObjects = new Dictionary<Vector2Int, GameObject>();
        private List<GameObject> roomObjects = new List<GameObject>();
        private Vector3 origin;
        private BoundsInt bounds;

        public int Width => bounds.size.x;
        public int Height => bounds.size.y;
        public BoundsInt Bounds => bounds;

        public void Initialize(LevelData level)
        {
            if (level == null)
                throw new System.ArgumentNullException(nameof(level));

            bounds = level.GetGridBounds();
            tiles.Clear();
            tileRuntimeObjects.Clear();
            roomObjects.Clear();
            origin = level.Origin;
        }

        public bool SetTile(Vector2Int cell, StageTileDefinition tile, GameObject runtimeObject = null)
        {
            if (!IsWithinBounds(cell))
                return false;

            if (tile == null)
                tiles.Remove(cell);
            else
                tiles[cell] = tile;

            if (runtimeObject == null)
                tileRuntimeObjects.Remove(cell);
            else
                tileRuntimeObjects[cell] = runtimeObject;
            return true;
        }

        public bool AddRoomObject(GameObject runtimeObject)
        {
            if (runtimeObject == null)
                return false;

            Vector2Int cell = WorldToCell(runtimeObject.transform.position);
            if (!IsWithinBounds(cell))
                return false;

            roomObjects.Add(runtimeObject);
            return true;
        }

        public bool TryGetTile(Vector2Int cell, out StageTileDefinition tile)
        {
            tile = null;
            if (!IsWithinBounds(cell))
                return false;

            return tiles.TryGetValue(cell, out tile);
        }

        public bool IsType(TileType type, Vector2Int cell)
        {
            return TryGetTile(cell, out StageTileDefinition tile) && tile.TileType == type;
        }

        public bool IsWalkable(Vector2Int cell, Transform actor = null)
        {
            return IsWalkableIgnoringRoomObject(cell, actor) && !TryGetRoomObject(cell, out _);
        }

        public bool IsWalkableIgnoringRoomObject(Vector2Int cell, Transform actor = null)
        {
            if (!IsWithinBounds(cell))
                return false;

            bool hasMovementRule = false;
            if (TryGetTileRuntimeObject(cell, out GameObject tileObject))
            {
                foreach (MonoBehaviour behaviour in tileObject.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!(behaviour is IActorBlocker blocker))
                        continue;

                    hasMovementRule = true;
                    if (blocker.BlocksMovement(actor))
                        return false;
                }
            }

            if (hasMovementRule)
                return true;

            if (!TryGetTile(cell, out StageTileDefinition tile))
                return true;
            if (tile.TileType == TileType.Trap &&
                TryGetTrap(cell, out TrapRuntime trap) &&
                trap.IsFilled)
                return true;

            return tile.AllowsActors;
        }

        public bool TryGetPushable(Vector2Int cell, out IPushable pushable)
        {
            pushable = null;
            return TryGetRoomObject(cell, out StageObjectRuntime roomObject) &&
                   (pushable = roomObject) != null;
        }

        public bool TryGetTrap(Vector2Int cell, out TrapRuntime trap)
        {
            trap = null;
            if (!TryGetTileRuntimeObject(cell, out GameObject runtimeObject))
                return false;

            trap = runtimeObject.GetComponentInChildren<TrapRuntime>(true);
            return trap != null;
        }

        public bool TryGetRuntimeObject(Vector2Int cell, out GameObject runtimeObject)
        {
            if (TryGetRoomObject(cell, out StageObjectRuntime roomObject))
            {
                runtimeObject = roomObject.ObjectGameObject;
                return true;
            }

            return TryGetTileRuntimeObject(cell, out runtimeObject);
        }

        public bool TryGetRoomObject(Vector2Int cell, out StageObjectRuntime roomObject)
        {
            roomObject = null;
            if (!IsWithinBounds(cell))
                return false;

            for (int i = 0; i < roomObjects.Count; i++)
            {
                GameObject candidate = roomObjects[i];
                if (candidate == null || !candidate.activeInHierarchy ||
                    WorldToCell(candidate.transform.position) != cell)
                    continue;

                roomObject = candidate.GetComponentInChildren<StageObjectRuntime>(true);
                if (roomObject != null)
                    return true;
            }

            return false;
        }

        public bool CanObjectEnter(Vector2Int cell, StageObjectRuntime movingObject = null)
        {
            if (!IsWithinBounds(cell))
                return false;

            if (TryGetTile(cell, out StageTileDefinition tile) && !tile.AllowsObjects)
                return false;

            if (TryGetTileRuntimeObject(cell, out GameObject tileRuntime))
            {
                foreach (MonoBehaviour behaviour in tileRuntime.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour is IObjectEntryRule entryRule && !entryRule.AllowsObjectEntry)
                        return false;
                }
            }

            for (int i = 0; i < roomObjects.Count; i++)
            {
                GameObject candidate = roomObjects[i];
                if (candidate == null || !candidate.activeInHierarchy ||
                    WorldToCell(candidate.transform.position) != cell)
                    continue;

                StageObjectRuntime occupant = candidate.GetComponentInChildren<StageObjectRuntime>(true);
                if (occupant != null && occupant != movingObject)
                    return false;
            }

            return true;
        }

        public bool IsHazard(Vector2Int cell)
        {
            return TryGetTrap(cell, out TrapRuntime trap) && trap.IsHazard;
        }

        public bool IsLaserHazard(Vector2Int cell)
        {
            foreach (GameObject runtimeObject in roomObjects)
            {
                if (runtimeObject == null || !runtimeObject.activeInHierarchy)
                    continue;

                LaserRuntime laser = runtimeObject.GetComponentInChildren<LaserRuntime>(true);
                if (laser == null)
                    continue;

                Vector2Int direction = laser.BeamDirection;
                if (direction == Vector2Int.zero)
                    continue;

                Vector2Int beamCell = WorldToCell(runtimeObject.transform.position) + direction;
                while (IsWithinBounds(beamCell))
                {
                    if (BlocksLaser(beamCell))
                        break;

                    if (beamCell == cell)
                        return true;

                    beamCell += direction;
                }
            }

            return false;
        }

        private bool BlocksLaser(Vector2Int cell)
        {
            if (TryGetTile(cell, out StageTileDefinition tile) && tile.BlocksLaser)
                return true;

            if (TryGetTileRuntimeObject(cell, out GameObject tileObject))
            {
                foreach (MonoBehaviour behaviour in tileObject.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour is ILaserBLocker laserBlocker && laserBlocker.BlocksLaser)
                        return true;
                }
            }

            if (TryGetRoomObject(cell, out StageObjectRuntime roomObject) && roomObject.BlocksLaser)
                return true;

            return false;
        }

        public bool TryGetTileRuntimeObject(Vector2Int cell, out GameObject runtimeObject)
        {
            runtimeObject = null;
            if (!IsWithinBounds(cell) ||
                !tileRuntimeObjects.TryGetValue(cell, out GameObject candidate) ||
                candidate == null ||
                !candidate.activeInHierarchy ||
                WorldToCell(candidate.transform.position) != cell)
                return false;

            runtimeObject = candidate;
            return true;
        }

        public bool IsWithinBounds(Vector2Int cell)
        {
            return tiles != null &&
                   cell.x >= bounds.xMin && cell.x < bounds.xMax &&
                   cell.y >= bounds.yMin && cell.y < bounds.yMax;
        }

        public Vector2Int WorldToCell(Vector3 position)
        {
            Vector3 localPosition = position - origin;
            return new Vector2Int(
                Mathf.FloorToInt(localPosition.x),
                Mathf.FloorToInt(localPosition.y));
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return origin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        public static Vector2Int ToCell(Vector3 vector)
        {
            return new Vector2Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));
        }
    }
}
