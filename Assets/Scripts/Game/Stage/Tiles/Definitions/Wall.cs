using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Wall", menuName = "Chronowalk/Tiles/Wall")]
    public class Wall : TileObjectDefinition
    {
        public override TileType TileType => TileType.Wall;
        public override bool AllowsActors => false;
        public override bool AllowsObjects => false;
        public override bool BlocksLaser => true;
    }
}