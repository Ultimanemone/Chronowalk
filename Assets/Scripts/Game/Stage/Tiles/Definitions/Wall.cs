using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Wall", menuName = "Chronowalk/Tiles/Wall")]
    public class Wall : StageTileDefinition
    {
        public override TileType TileType => TileType.Wall;
        public override bool AllowsActors => false;
        public override bool AllowsObjects => false;
        public override bool BlocksLaser => true;
    }
}