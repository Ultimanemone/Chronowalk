using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Box", menuName = "Chronowalk/Tiles/Box")]
    public class Box : TileObject
    {
        public override TileType TileType => TileType.Box;
    }
}