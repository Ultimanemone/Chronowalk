using ChronowalkGame.Game.PlayerEntities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChronowalkGame.Game.Stage
{
    public class StageInputManager : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveKeys;
        [SerializeField] private InputActionReference waitKey;
        [SerializeField] private InputActionReference interactKey;
        [SerializeField] private InputActionReference endLoopKey;
        [SerializeField] private InputActionReference eraseLoopKey;
        [SerializeField] private InputActionReference restartLoopKey;
        [SerializeField] private InputActionReference restartLevelKey;

        private void OnEnable()
        {
            SetActionsEnabled(true);
        }

        private void OnDisable()
        {
            SetActionsEnabled(false);
        }

        private void Update()
        {
            StageManager stage = StageManager.Instance;
            if (stage == null)
                return;

            Keyboard keyboard = Keyboard.current;
            if (WasPressed(restartLevelKey) ||
                (keyboard != null && keyboard.rKey.wasPressedThisFrame && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)))
            {
                stage.RestartLevel();
                return;
            }

            if (WasPressed(restartLoopKey) || (keyboard != null && keyboard.rKey.wasPressedThisFrame))
            {
                stage.RestartCurrentLoop();
                return;
            }

            if (WasPressed(eraseLoopKey) || (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame))
            {
                stage.RemoveMostRecentLoop();
                return;
            }

            if (WasPressed(endLoopKey) || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                stage.EndCurrentLoop();
                return;
            }

            if (moveKeys != null && moveKeys.action.WasPressedThisFrame())
            {
                Vector2Int direction = GetMoveDirection(moveKeys.action.ReadValue<Vector2>());
                if (direction != Vector2Int.zero)
                    stage.TrySubmitAction(ActionType.Move, direction);
                return;
            }

            if (WasPressed(waitKey))
            {
                stage.TrySubmitAction(ActionType.Wait, Vector2Int.zero);
                return;
            }

            if (WasPressed(interactKey))
            {
                Vector2Int direction = Player.Instance != null
                    ? DirectionUtility.ToVector(Player.Instance.Facing)
                    : Vector2Int.up;
                stage.TrySubmitAction(ActionType.Interact, direction);
            }
        }

        private void SetActionsEnabled(bool enabled)
        {
            SetActionEnabled(moveKeys, enabled);
            SetActionEnabled(waitKey, enabled);
            SetActionEnabled(interactKey, enabled);
            SetActionEnabled(endLoopKey, enabled);
            SetActionEnabled(eraseLoopKey, enabled);
            SetActionEnabled(restartLoopKey, enabled);
            SetActionEnabled(restartLevelKey, enabled);
        }

        private static void SetActionEnabled(InputActionReference actionReference, bool enabled)
        {
            if (actionReference == null)
                return;

            if (enabled)
                actionReference.action.Enable();
            else
                actionReference.action.Disable();
        }

        private static bool WasPressed(InputActionReference actionReference)
        {
            return actionReference != null && actionReference.action.WasPressedThisFrame();
        }

        private static Vector2Int GetMoveDirection(Vector2 input)
        {
            if (input == Vector2.zero)
                return Vector2Int.zero;

            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
                return new Vector2Int((int)Mathf.Sign(input.x), 0);

            return new Vector2Int(0, (int)Mathf.Sign(input.y));
        }
    }
}
