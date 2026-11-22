using UnityEngine;

namespace ChronowalkGame.Game.Stage.Tiles.Definitions
{
    [CreateAssetMenu(fileName = "Trap", menuName = "Chronowalk/Tiles/Trap")]
    public class Trap : StageTileDefinition
    {
        public enum TrapBehavior
        {
            Deadly,
            FillableHole
        }

        [SerializeField] private TrapBehavior behavior;

        public override TileType TileType => TileType.Trap;
        public TrapBehavior Behavior => behavior;
    }
}