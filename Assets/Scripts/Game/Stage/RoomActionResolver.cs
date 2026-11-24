using System;
using ChronowalkGame.Game.Data;
using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage.Contracts;
using UnityEngine;

namespace ChronowalkGame.Game.Stage
{
    public static class RoomActionResolver
    {
        public static bool CanMove(Vector2Int direction, Transform actor, GridManager grid)
        {
            if (actor == null || grid == null || !DirectionUtils.TryGetDirection(direction, out _))
                return false;

            Vector2Int targetCell = grid.WorldToCell(actor.position) + direction;
            if (grid.TryGetPushable(targetCell, out IPushable pushable))
            {
                return pushable.CanPush(direction, actor, grid) &&
                       grid.IsWalkableIgnoringRoomObject(targetCell, actor);
            }

            return grid.IsWalkable(targetCell, actor);
        }

        public static bool ApplyAction(
            PlayerAction action,
            Transform actor,
            GridManager grid,
            Action<Direction> setFacing,
            out Vector3 moveDestination)
        {
            moveDestination = actor != null ? actor.position : Vector3.zero;
            if (actor == null || grid == null || setFacing == null)
                return false;

            if (action.Type == ActionType.Move)
            {
                if (!DirectionUtils.TryGetDirection(action.Direction, out Direction direction) ||
                    !CanMove(action.Direction, actor, grid))
                    return false;

                setFacing(direction);
                Vector2Int targetCell = grid.WorldToCell(actor.position) + action.Direction;
                if (grid.TryGetPushable(targetCell, out IPushable pushable) &&
                    !pushable.TryPush(action.Direction, actor, grid))
                    return false;

                moveDestination = grid.CellToWorld(targetCell);
                moveDestination.z = actor.position.z;
                return true;
            }

            if (action.Type == ActionType.Interact)
            {
                if (!DirectionUtils.TryGetDirection(action.Direction, out Direction direction))
                    return false;

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

            return action.Type == ActionType.Interact || action.Type == ActionType.Wait;
        }

        private static void InteractWith(GameObject target, Transform actor)
        {
            foreach (MonoBehaviour behaviour in target.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IInteractable interactable)
                    interactable.Interact(actor);
            }
        }
    }
}
