using UnityEngine;

namespace ChronowalkGame.Game.PlayerEntities
{
    public static class DirectionUtility
    {
        public static bool TryGetDirection(Vector2Int vector, out Direction direction)
        {
            if (vector == Vector2Int.up)
                direction = Direction.Up;
            else if (vector == Vector2Int.down)
                direction = Direction.Down;
            else if (vector == Vector2Int.left)
                direction = Direction.Left;
            else if (vector == Vector2Int.right)
                direction = Direction.Right;
            else
            {
                direction = Direction.Up;
                return false;
            }

            return true;
        }

        public static Vector2Int ToVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Down: return Vector2Int.down;
                case Direction.Left: return Vector2Int.left;
                case Direction.Right: return Vector2Int.right;
                default: return Vector2Int.up;
            }
        }

        public static Quaternion ToRotation(Direction direction)
        {
            switch (direction)
            {
                case Direction.Right: return Quaternion.Euler(0f, 0f, -90f);
                case Direction.Down: return Quaternion.Euler(0f, 0f, 180f);
                case Direction.Left: return Quaternion.Euler(0f, 0f, 90f);
                default: return Quaternion.identity;
            }
        }
    }
}
