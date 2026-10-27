using ChronowalkGame.Game.Stage;
using UnityEngine;

namespace ChronowalkGame.Game.PlayerEntities
{
    public class Ghost : MonoBehaviour
    {
        public void Replay(PlayerAction action, GridManager grid)
        {
            if (grid == null || !gameObject.activeSelf)
                return;

            if (action.Type == ActionType.Move &&
                DirectionUtility.TryGetDirection(action.Direction, out Direction direction))
            {
                transform.rotation = DirectionUtility.ToRotation(direction);
                Vector2Int targetCell = grid.WorldToCell(transform.position) + action.Direction;
                if (!grid.IsWalkable(targetCell))
                    return;

                transform.position = grid.CellToWorld(targetCell);
                if (grid.IsHazard(targetCell))
                    gameObject.SetActive(false);
            }
            else if (action.Type == ActionType.Interact &&
                     DirectionUtility.TryGetDirection(action.Direction, out direction))
            {
                transform.rotation = DirectionUtility.ToRotation(direction);
                Vector2Int targetCell = grid.WorldToCell(transform.position) + action.Direction;
                if (grid.TryGetRuntimeObject(targetCell, out GameObject target))
                {
                    foreach (MonoBehaviour behaviour in target.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (behaviour is IRoomInteractable interactable)
                            interactable.Interact(transform);
                    }
                }
            }
        }
    }
}