using ChronowalkGame.Data;
using UnityEngine;

namespace ChronowalkGame.Core.Stage
{
    public class Player : MonoBehaviour
    {
        public void Move(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    transform.position += Vector3.up;
                    break;
                case Direction.Down:
                    transform.position += Vector3.down;
                    break;
                case Direction.Left:
                    transform.position += Vector3.left;
                    break;
                case Direction.Right:
                    transform.position += Vector3.right;
                    break;
            }
        }
    }
}