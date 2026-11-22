using UnityEngine;

namespace ChronowalkGame.Game.Stage.Objects.Definitions
{
    [CreateAssetMenu(fileName = "Prism", menuName = "Chronowalk/Objects/Prism")]
    public class Prism : StageObjectDefinition
    {
        public override RoomObjectType ObjectType => RoomObjectType.Prism;
    }
}