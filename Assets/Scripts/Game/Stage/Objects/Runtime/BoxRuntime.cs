using UnityEngine;
using ChronowalkGame.Game.Stage.Contracts;

namespace ChronowalkGame.Game.Stage.Objects.Runtime
{
    public class BoxRuntime : MonoBehaviour, IRoomStateful
    {
        private const int TurretHitsToBreak = 2;

        private Transform tileRoot;
        private int turretHitsRemaining = TurretHitsToBreak;

        public int TurretHitsRemaining => turretHitsRemaining;

        public void Configure(Transform root)
        {
            tileRoot = root != null ? root : transform;
            turretHitsRemaining = TurretHitsToBreak;
        }

        public void HitByTurret()
        {
            if (turretHitsRemaining <= 0)
                return;

            turretHitsRemaining--;
            if (turretHitsRemaining == 0)
                RemoveFromRoom();
        }

        public object CaptureRoomState()
        {
            return turretHitsRemaining;
        }

        public void RestoreRoomState(object state)
        {
            if (state is int hitsRemaining)
                turretHitsRemaining = hitsRemaining;
        }

        public void RemoveFromRoom()
        {
            Transform root = tileRoot != null ? tileRoot : transform;
            root.gameObject.SetActive(false);
        }
    }
}
