using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage.Tile;
using System;
using UnityEngine;

namespace ChronowalkGame.Game.Stage
{

    [Serializable]
    public class ObjectData
    {
        public TileObject Definition;
        public Vector2Int Position;
        public Direction Direction;
    }
}