using ChronowalkGame.Game.PlayerEntities;
using System;
using UnityEngine;

namespace ChronowalkGame.Game.Stage.Data
{

    [Serializable]
    public class ObjectData
    {
        public StageElement Definition;
        public Vector2Int Position;
        public Direction Direction;
        public bool CanBePushed = true;
    }
}