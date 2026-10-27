using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Trap", menuName = "Chronowalk/Tiles/Trap")]
    public class Trap : TileObject
    {
        public override TileType TileType => TileType.Trap;
    }
}