using ChronowalkGame.Game.Stage.Tile;
using ChronowalkGame.Utils;
using UnityEngine;

namespace ChronowalkGame.Game.Stage
{
    public class GridManager : MonoSingleton<GridManager>
    {
        private TileObject[,] tiles;
        private GameObject[,] runtimeObjects;
        private Vector3 origin;

        public int Width => tiles == null ? 0 : tiles.GetLength(0);
        public int Height => tiles == null ? 0 : tiles.GetLength(1);

        public void Initialize(LevelData level)
        {
            if (level == null)
                throw new System.ArgumentNullException(nameof(level));

            tiles = new TileObject[level.Width, level.Height];
            runtimeObjects = new GameObject[level.Width, level.Height];
            origin = level.Origin;
        }

        public bool SetTile(Vector2Int cell, TileObject tile, GameObject runtimeObject = null)
        {
            if (!IsWithinBounds(cell))
                return false;

            tiles[cell.x, cell.y] = tile;
            runtimeObjects[cell.x, cell.y] = runtimeObject;
            return true;
        }

        public bool TryGetTile(Vector2Int cell, out TileObject tile)
        {
            tile = null;
            if (!IsWithinBounds(cell))
                return false;

            tile = tiles[cell.x, cell.y];
            return tile != null;
        }

        public bool TryGetRuntimeObject(Vector2Int cell, out GameObject runtimeObject)
        {
            runtimeObject = null;
            if (!IsWithinBounds(cell))
                return false;

            runtimeObject = runtimeObjects[cell.x, cell.y];
            return runtimeObject != null;
        }

        public bool IsType(TileType type, Vector2Int cell)
        {
            return TryGetTile(cell, out TileObject tile) && tile.TileType == type;
        }

        public bool IsWalkable(Vector2Int cell)
        {
            if (!IsWithinBounds(cell))
                return false;

            if (!TryGetTile(cell, out TileObject tile))
                return true;

            return tile.TileType == TileType.Corridor || tile.TileType == TileType.Trap;
        }

        public bool IsHazard(Vector2Int cell)
        {
            return IsType(TileType.Trap, cell);
        }

        public bool IsWithinBounds(Vector2Int cell)
        {
            return tiles != null &&
                   cell.x >= 0 && cell.x < tiles.GetLength(0) &&
                   cell.y >= 0 && cell.y < tiles.GetLength(1);
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
