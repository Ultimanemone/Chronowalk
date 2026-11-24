using ChronowalkGame.Game.Stage.Contracts;
using UnityEngine;

namespace ChronowalkGame.Game.Stage.Objects.Runtime
{
    public class LaserRuntime : MonoBehaviour, IRuntimeInitializable<Vector2Int>
    {
        private Vector2Int beamDirection = Vector2Int.up;
        private LineRenderer beamRenderer;

        public Vector2Int BeamDirection => beamDirection;

        private void Awake()
        {
            CacheBeamRenderer();
        }

        public void Initialize(Vector2Int direction)
        {
            beamDirection = direction;
            CacheBeamRenderer();
            if (beamRenderer == null)
            {
                Debug.LogError("Laser prefab requires a LineRenderer for its beam.", this);
                return;
            }

            beamRenderer.useWorldSpace = true;
            beamRenderer.positionCount = 0;
        }

        private void LateUpdate()
        {
            if (beamRenderer == null)
                return;

            GridManager grid = GridManager.Instance;
            if (grid == null || beamDirection == Vector2Int.zero)
            {
                beamRenderer.positionCount = 0;
                return;
            }

            Vector3 sourcePosition = transform.position;
            int beamLength = grid.GetLaserBeamLength(sourcePosition, beamDirection);
            Vector3 direction = new Vector3(beamDirection.x, beamDirection.y, 0f);
            Vector3 beamEnd = sourcePosition + direction * (beamLength + 0.5f);
            beamRenderer.positionCount = 2;
            beamRenderer.SetPosition(0, sourcePosition);
            beamRenderer.SetPosition(1, beamEnd);
        }

        private void CacheBeamRenderer()
        {
            if (beamRenderer == null)
                beamRenderer = GetComponentInChildren<LineRenderer>(true);
        }
    }
}
