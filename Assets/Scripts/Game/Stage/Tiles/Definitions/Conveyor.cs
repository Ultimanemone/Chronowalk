using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Conveyor", menuName = "Chronowalk/Tiles/Conveyor")]
    public class Conveyor : StageTileDefinition
    {
        public override TileType TileType => TileType.Conveyor;
    }
}