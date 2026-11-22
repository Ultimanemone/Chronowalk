using UnityEngine;
using ChronowalkGame.Game.Stage.Data;

namespace ChronowalkGame.Game.Stage.Objects.Definitions
{
    public abstract class RoomObjectDefinition : StageElement
    {
        [SerializeField] private bool blocksLaser;

        public abstract RoomObjectType ObjectType { get; }
        public virtual bool BlocksLaser => blocksLaser;
    }
}
