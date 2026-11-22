using System;
using ChronowalkGame.Game.Stage.Contracts;
using ChronowalkGame.Game.PlayerEntities;
using UnityEngine;

namespace ChronowalkGame.Game.Stage
{
    public static class RoomActionResolver
    {
        public static bool CanMove(Vector2Int direction, Transform actor, GridManager grid)
        {
            if (actor == null || grid == null || !DirectionUtility.TryGetDirection(direction, out _))
                return false;

            Vector2Int targetCell = grid.WorldToCell(actor.position) + direction;
            if (grid.TryGetPushable(targetCell, out IRoomPushable pushable))
            {
                return pushable.CanPush(direction, actor, grid) &&
                       grid.IsWalkableIgnoringRoomObject(targetCell, actor);
            }

            return grid.IsWalkable(targetCell, actor);
        }

        public static void Resolve(
            PlayerAction action,
            Transform actor,
            GridManager grid,
            Action<Direction> setFacing,
            Action onHazardEntered)
        {
            if (actor == null || grid == null || setFacing == null)
                return;

            if (action.Type == ActionType.Move)
            {
                if (!DirectionUtility.TryGetDirection(action.Direction, out Direction direction))
                    return;
                if (!CanMove(action.Direction, actor, grid))
                    return;

                setFacing(direction);
                Vector2Int targetCell = grid.WorldToCell(actor.position) + action.Direction;
                if (grid.TryGetPushable(targetCell, out IRoomPushable pushable))
                {
                    if (!pushable.TryPush(action.Direction, actor, grid))
                        return;
                }

                actor.position = grid.CellToWorld(targetCell);

                if (grid.IsHazard(targetCell))
                    onHazardEntered?.Invoke();
            }
            else if (action.Type == ActionType.Interact)
            {
                if (!DirectionUtility.TryGetDirection(action.Direction, out Direction direction))
                    return;

                setFacing(direction);
                Vector2Int targetCell = grid.WorldToCell(actor.position) + action.Direction;
                GameObject interactedObject = null;
                if (grid.TryGetRuntimeObject(targetCell, out GameObject target))
                {
                    interactedObject = target;
                    InteractWith(target, actor);
                }

                if (grid.TryGetTileRuntimeObject(targetCell, out GameObject tileTarget) &&
                    tileTarget != interactedObject)
                    InteractWith(tileTarget, actor);
            }

            if (actor != null && grid.IsLaserHazard(grid.WorldToCell(actor.position)))
                onHazardEntered?.Invoke();
        }

        private static void InteractWith(GameObject target, Transform actor)
        {
            foreach (MonoBehaviour behaviour in target.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IRoomInteractable interactable)
                    interactable.Interact(actor);
            }
        }
    }
}
