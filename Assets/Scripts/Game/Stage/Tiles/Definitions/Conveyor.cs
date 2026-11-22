using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Conveyor", menuName = "Chronowalk/Tiles/Conveyor")]
    public class Conveyor : TileObjectDefinition
    {
        public override TileType TileType => TileType.Conveyor;
    }
}