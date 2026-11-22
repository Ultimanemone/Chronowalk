using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Exit", menuName = "Chronowalk/Tiles/Exit")]
    public class Exit : TileObjectDefinition
    {
        public override TileType TileType => TileType.Exit;
    }
}
