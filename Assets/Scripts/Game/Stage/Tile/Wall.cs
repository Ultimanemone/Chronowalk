using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Wall", menuName = "Chronowalk/Tiles/Wall")]
    public class Wall : TileObject
    {
        public override TileType TileType => TileType.Wall;
    }
}