using ChronowalkGame.Game.PlayerEntities;
using UnityEngine;

namespace ChronowalkGame.Game.Stage.Objects.Runtime
{
    public class LaserRuntime : MonoBehaviour
    {
        private Vector2Int beamDirection = Vector2Int.up;

        public Vector2Int BeamDirection => beamDirection;

        public void Configure(Direction direction)
        {
            beamDirection = DirectionUtility.ToVector(direction);
        }
    }
}
