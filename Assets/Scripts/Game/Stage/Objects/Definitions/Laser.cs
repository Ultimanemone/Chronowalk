using UnityEngine;

namespace ChronowalkGame.Game.Stage.Objects.Definitions
{
    [CreateAssetMenu(fileName = "Laser", menuName = "Chronowalk/Objects/Laser")]
    public class Laser : StageObjectDefinition
    {
        public override RoomObjectType ObjectType => RoomObjectType.Laser;
    }
}