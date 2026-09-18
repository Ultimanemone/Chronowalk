using UnityEngine;
using UnityEngine.InputSystem;

namespace ChronowalkGame.Core.Managers
{
    public class StageInputManager : MonoBehaviour
    {
        void Start()
        {

        }

        private void Update()
        {
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {

            }
            else if (Keyboard.current.sKey.wasPressedThisFrame)
            {

            }
            else if (Keyboard.current.aKey.wasPressedThisFrame)
            {

            }
            else if (Keyboard.current.dKey.wasPressedThisFrame)
            {

            }
        }
    }
}