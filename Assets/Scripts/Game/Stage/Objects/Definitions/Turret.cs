using UnityEngine;

namespace ChronowalkGame.Game.Stage.Objects.Definitions
{
    [CreateAssetMenu(fileName = "Turret", menuName = "Chronowalk/Objects/Turret")]
    public class Turret : RoomObjectDefinition
    {
        public override RoomObjectType ObjectType => RoomObjectType.Turret;
    }
}