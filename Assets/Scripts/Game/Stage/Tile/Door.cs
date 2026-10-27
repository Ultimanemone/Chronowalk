using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Door", menuName = "Chronowalk/Tiles/Door")]
    public class Door : TileObject
    {
        public override TileType TileType => TileType.Door;
    }
}