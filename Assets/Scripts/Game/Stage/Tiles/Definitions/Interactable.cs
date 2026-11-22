using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Interactable", menuName = "Chronowalk/Tiles/Interactable")]
    public class Interactable : TileObjectDefinition
    {
        public override TileType TileType => TileType.Interactable;
    }
}