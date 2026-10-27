using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Conveyor", menuName = "Chronowalk/Tiles/Conveyor")]
    public class Conveyor : TileObject
    {
        public override TileType TileType => TileType.Conveyor;
    }
}