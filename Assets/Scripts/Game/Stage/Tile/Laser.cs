using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Laser", menuName = "Chronowalk/Tiles/Laser")]
    public class Laser : TileObject
    {
        public override TileType TileType => TileType.Laser;
    }
}