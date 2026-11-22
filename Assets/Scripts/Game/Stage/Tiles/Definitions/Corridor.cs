using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Corridor", menuName = "Chronowalk/Tiles/Corridor")]
    public class Corridor : StageTileDefinition
    {
        public override TileType TileType => TileType.Corridor;
    }
}