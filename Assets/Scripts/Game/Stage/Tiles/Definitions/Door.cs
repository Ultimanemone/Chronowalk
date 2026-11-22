using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Door", menuName = "Chronowalk/Tiles/Door")]
    public class Door : StageTileDefinition
    {
        public override TileType TileType => TileType.Door;
    }
}