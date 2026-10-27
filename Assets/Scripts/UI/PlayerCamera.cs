using ChronowalkGame.Game.PlayerEntities;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace ChronowalkGame.UI
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset;
        [SerializeField] private Vector3 eulerAngles;
        [SerializeField] private float distance;

        private void Start()
        {
            transform.position = CalculatePosition();
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, CalculatePosition(), Time.deltaTime * 5f);
        }

        private Vector3 CalculatePosition()
        {
            Vector3 position = Player.Instance.transform.localPosition;
            Vector3 sphericalOffset = Quaternion.Euler(eulerAngles) * Vector3.forward * distance;
            position += offset + sphericalOffset;
            return position;
        }
    }
}