using System;
using System.Collections.Generic;
using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage.Tile;
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
            PlayerStart.x = Mathf.Clamp(PlayerStart.x, 0, Width - 1);
            PlayerStart.y = Mathf.Clamp(PlayerStart.y, 0, Height - 1);
        }
    }
}