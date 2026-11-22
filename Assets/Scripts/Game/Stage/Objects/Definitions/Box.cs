using UnityEngine;

namespace ChronowalkGame.Game.Stage.Objects.Definitions
{
    [CreateAssetMenu(fileName = "Box", menuName = "Chronowalk/Objects/Box")]
    public class Box : RoomObjectDefinition
    {
        public override RoomObjectType ObjectType => RoomObjectType.Box;
        public override bool BlocksLaser => true;
    }
}