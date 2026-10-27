using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Corridor", menuName = "Chronowalk/Tiles/Corridor")]
    public class Corridor : TileObject
    {
        public override TileType TileType => TileType.Corridor;
    }
}