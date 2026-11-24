using System;
using System.Collections.Generic;
using ChronowalkGame.Game.Data;
using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage.Data;
using UnityEngine;

namespace ChronowalkGame.Game.Stage
{
    [CreateAssetMenu(fileName = "NewLevel", menuName = "Chronowalk/Level")]
    public class LevelData : ScriptableObject
    {
        [Min(1)] public int Width = 8;
        [Min(1)] public int Height = 8;
        [Min(1)] public int TurnLimit = 20;
        public Vector3 Origin;
        public Vector2Int PlayerStart;
        public Direction PlayerDirection = Direction.Up;
        public List<ObjectData> Placements = new List<ObjectData>();

        private void OnValidate()
        {
            Width = Mathf.Max(1, Width);
            Height = Mathf.Max(1, Height);
            TurnLimit = Mathf.Max(1, TurnLimit);
            Placements ??= new List<ObjectData>();
        }

        public BoundsInt GetGridBounds()
        {
            int minX = 0;
            int minY = 0;
            int maxX = Width - 1;
            int maxY = Height - 1;

            IncludeCell(PlayerStart, ref minX, ref minY, ref maxX, ref maxY);
            if (Placements != null)
            {
                foreach (ObjectData placement in Placements)
                {
                    if (placement != null)
                        IncludeCell(placement.Position, ref minX, ref minY, ref maxX, ref maxY);
                }
            }

            return new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
        }

        private static void IncludeCell(
            Vector2Int cell,
            ref int minX,
            ref int minY,
            ref int maxX,
            ref int maxY)
        {
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }
    }
}