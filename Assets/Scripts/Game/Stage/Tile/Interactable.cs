using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tile
{
    [CreateAssetMenu(fileName = "Interactable", menuName = "Chronowalk/Tiles/Interactable")]
    public class Interactable : TileObject
    {
        public override TileType TileType => TileType.Interactable;
    }
}