using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Interactable", menuName = "Chronowalk/Tiles/Interactable")]
    public class Interactable : StageTileDefinition
    {
        public override TileType TileType => TileType.Interactable;
    }
}