using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Prism", menuName = "Chronowalk/Tiles/Prism")]
    public class Prism : TileObject
    {
        public override TileType TileType => TileType.Prism;
    }
}