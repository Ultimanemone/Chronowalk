using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Turret", menuName = "Chronowalk/Tiles/Turret")]
    public class Turret : TileObject
    {
        public override TileType TileType => TileType.Turret;
    }
}