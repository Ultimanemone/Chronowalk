using System;
using System.Collections.Generic;
using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage.Tile;
using ChronowalkGame.Utils;
using UnityEngine;

namespace ChronowalkGame.Game.Stage
{
    public class StageManager : MonoSingleton<StageManager>
    {
        public enum StageState
        {
            NotInitialized,
            AwaitingAction,
            ResolvingTurn,
            TransitioningLoop,
            Completed,
            Error
        }

        [SerializeField] private LevelData levelData;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Player player;
        [SerializeField] private GameObject ghostPrefab;

        private readonly List<LoopActionLog> completedLoops = new List<LoopActionLog>();
        private readonly List<PlayerAction> currentLoopActions = new List<PlayerAction>();
        private readonly List<RoomObjectSnapshot> roomSnapshot = new List<RoomObjectSnapshot>();
        private readonly List<IRoomTurnSystem> turnSystems = new List<IRoomTurnSystem>();
        private readonly List<GhostPlayback> ghosts = new List<GhostPlayback>();

        private Transform roomRoot;
        private Transform ghostRoot;
        private int turnsUsed;
        private bool loopEndRequested;
        private string pendingLoopEndReason;

        public StageState State { get; private set; } = StageState.NotInitialized;
        public int TurnsUsed => turnsUsed;
        public int TurnLimit => levelData == null ? 0 : levelData.TurnLimit;
        public int TurnsRemaining => Mathf.Max(0, TurnLimit - TurnsUsed);
        public int CompletedLoopCount => completedLoops.Count;
        public int CurrentLoopNumber => completedLoops.Count + 1;
        public IReadOnlyList<LoopActionLog> CompletedLoops => completedLoops;
        public string LastLoopEndReason { get; private set; }

        public event Action<StageManager> StateChanged;
        public event Action<int, int> MoveCountChanged;
        public event Action<int> LoopChanged;

        private void Start()
        {
            if (levelData != null)
                Initialize(levelData);
            else
                Debug.LogWarning("StageManager needs a LevelData asset assigned or Initialize called before gameplay.", this);
        }

        protected override void OnDestroy()
        {
            if (roomRoot != null)
                Destroy(roomRoot.gameObject);
            if (ghostRoot != null)
                Destroy(ghostRoot.gameObject);
            base.OnDestroy();
        }

        public bool Initialize(LevelData level)
        {
            if (!ValidateLevel(level, out string validationError))
            {
                SetState(StageState.Error);
                Debug.LogError($"Cannot initialize stage: {validationError}", this);
                return false;
            }

            levelData = level;
            gridManager = gridManager != null ? gridManager : GridManager.Instance;
            player = player != null ? player : Player.Instance;
            if (gridManager == null || player == null)
            {
                SetState(StageState.Error);
                Debug.LogError("Stage initialization requires a GridManager and Player in the loaded scene.", this);
                return false;
            }

            DestroyRoomObjects();
            completedLoops.Clear();
            currentLoopActions.Clear();
            turnsUsed = 0;
            loopEndRequested = false;
            pendingLoopEndReason = null;
            LastLoopEndReason = null;

            roomRoot = new GameObject($"{level.name} Runtime Room").transform;
            roomRoot.SetParent(transform, false);
            gridManager.Initialize(level);

            for (int i = 0; i < level.Placements.Count; i++)
            {
                ObjectData placement = level.Placements[i];
                GameObject instance = CreateRoomObject(placement.Definition);
                instance.name = $"{placement.Definition.name} ({placement.Position.x}, {placement.Position.y})";
                instance.transform.SetParent(roomRoot, true);
                instance.transform.position = gridManager.CellToWorld(placement.Position);
                instance.transform.rotation = DirectionUtility.ToRotation(placement.Direction);
                gridManager.SetTile(placement.Position, placement.Definition, instance);

                foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour is IRoomTurnSystem turnSystem)
                        turnSystems.Add(turnSystem);
                }
            }

            player.SetStart(level.PlayerStart, level.PlayerDirection);
            CaptureRoomSnapshot();
            SpawnGhosts();
            turnsUsed = 0;
            SetState(StageState.AwaitingAction);
            NotifyCountersChanged();
            return true;
        }

        public bool TrySubmitAction(ActionType type, Vector2Int direction)
        {
            return TrySubmitAction(new PlayerAction(type, direction));
        }

        public bool TrySubmitAction(PlayerAction action)
        {
            if (action.Type == ActionType.EndLoop)
                return EndCurrentLoop();

            if (State != StageState.AwaitingAction || !action.IsReplayable || player == null || !player.IsAlive)
                return false;

            if ((action.Type == ActionType.Move || action.Type == ActionType.Interact) &&
                !DirectionUtility.TryGetDirection(action.Direction, out _))
                return false;

            SetState(StageState.ResolvingTurn);
            loopEndRequested = false;
            pendingLoopEndReason = null;

            int turnIndex = turnsUsed;
            ReplayGhosts(turnIndex);
            currentLoopActions.Add(action);
            player.Act(action);

            int turnNumber = turnsUsed + 1;
            for (int i = 0; i < turnSystems.Count; i++)
                turnSystems[i].ResolveTurn(turnNumber);

            turnsUsed++;
            MoveCountChanged?.Invoke(turnsUsed, TurnLimit);

            if (State == StageState.Completed)
                return true;

            if (!loopEndRequested && turnsUsed >= TurnLimit)
            {
                loopEndRequested = true;
                pendingLoopEndReason = "Turn limit reached";
            }

            if (loopEndRequested)
            {
                FinishCurrentLoop(pendingLoopEndReason);
                return true;
            }

            SetState(StageState.AwaitingAction);
            return true;
        }

        public bool EndCurrentLoop()
        {
            if (State == StageState.ResolvingTurn)
            {
                loopEndRequested = true;
                pendingLoopEndReason = "Manually ended";
                return true;
            }

            if (State != StageState.AwaitingAction)
                return false;

            FinishCurrentLoop("Manually ended");
            return true;
        }

        public void NotifyPlayerDied()
        {
            if (State == StageState.ResolvingTurn)
            {
                loopEndRequested = true;
                pendingLoopEndReason = "Player died";
            }
            else if (State == StageState.AwaitingAction)
            {
                FinishCurrentLoop("Player died");
            }
        }

        public bool RestartCurrentLoop()
        {
            if (State != StageState.AwaitingAction && State != StageState.Completed)
                return false;

            ResetToLoopStart();
            LastLoopEndReason = "Current loop restarted";
            SetState(StageState.AwaitingAction);
            NotifyCountersChanged();
            return true;
        }

        public bool RemoveMostRecentLoop()
        {
            if (State != StageState.AwaitingAction || completedLoops.Count == 0)
                return false;

            completedLoops.RemoveAt(completedLoops.Count - 1);
            ResetToLoopStart();
            LastLoopEndReason = "Most recent loop erased";
            NotifyCountersChanged();
            return true;
        }

        public bool RestartLevel()
        {
            if (State != StageState.AwaitingAction && State != StageState.Completed)
                return false;

            completedLoops.Clear();
            currentLoopActions.Clear();
            ResetToLoopStart();
            LastLoopEndReason = "Level restarted";
            SetState(StageState.AwaitingAction);
            NotifyCountersChanged();
            return true;
        }

        public void CompleteLevel()
        {
            if (State == StageState.AwaitingAction || State == StageState.ResolvingTurn)
                SetState(StageState.Completed);
        }

        private bool ValidateLevel(LevelData level, out string error)
        {
            if (level == null)
            {
                error = "No LevelData asset was provided.";
                return false;
            }

            if (level.Width < 1 || level.Height < 1 || level.TurnLimit < 1)
            {
                error = "Level dimensions and turn limit must be greater than zero.";
                return false;
            }

            if (!IsCellInside(level, level.PlayerStart))
            {
                error = $"Player start {level.PlayerStart} is outside the level bounds.";
                return false;
            }

            if (level.Placements == null)
            {
                error = "Level placements list is missing.";
                return false;
            }

            Dictionary<Vector2Int, TileObject> occupiedCells = new Dictionary<Vector2Int, TileObject>();
            foreach (ObjectData placement in level.Placements)
            {
                if (placement == null || placement.Definition == null)
                {
                    error = "Every level placement must have a tile definition.";
                    return false;
                }

                if (!IsCellInside(level, placement.Position))
                {
                    error = $"Placement at {placement.Position} is outside the level bounds.";
                    return false;
                }

                if (occupiedCells.ContainsKey(placement.Position))
                {
                    error = $"More than one tile is placed at {placement.Position}.";
                    return false;
                }
                occupiedCells.Add(placement.Position, placement.Definition);
            }

            if (occupiedCells.TryGetValue(level.PlayerStart, out TileObject spawnTile) &&
                spawnTile.TileType != TileType.Corridor)
            {
                error = "Player start can overlap only a corridor tile.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsCellInside(LevelData level, Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < level.Width && cell.y >= 0 && cell.y < level.Height;
        }

        private GameObject CreateRoomObject(TileObject definition)
        {
            if (definition.Prefab != null)
                return Instantiate(definition.Prefab);

            GameObject instance = new GameObject(definition.name);
            if (definition.Sprite != null)
                instance.AddComponent<SpriteRenderer>().sprite = definition.Sprite;
            else
                Debug.LogWarning($"Tile definition '{definition.name}' has no prefab or sprite.", definition);
            return instance;
        }

        private void FinishCurrentLoop(string reason)
        {
            if (currentLoopActions.Count > 0)
                completedLoops.Add(new LoopActionLog(currentLoopActions));

            SetState(StageState.TransitioningLoop);
            LastLoopEndReason = reason;
            ResetToLoopStart();
            NotifyCountersChanged();
            SetState(StageState.AwaitingAction);
        }

        private void ResetToLoopStart()
        {
            ClearGhosts();
            for (int i = 0; i < roomSnapshot.Count; i++)
                roomSnapshot[i].Restore();

            currentLoopActions.Clear();
            turnsUsed = 0;
            loopEndRequested = false;
            pendingLoopEndReason = null;
            SpawnGhosts();
        }

        private void CaptureRoomSnapshot()
        {
            roomSnapshot.Clear();
            CaptureHierarchy(player.transform);
            if (roomRoot != null)
            {
                for (int i = 0; i < roomRoot.childCount; i++)
                    CaptureHierarchy(roomRoot.GetChild(i));
            }
        }

        private void CaptureHierarchy(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
                CaptureHierarchy(root.GetChild(i));

            roomSnapshot.Add(new RoomObjectSnapshot(root));
        }

        private void ReplayGhosts(int turnIndex)
        {
            for (int i = 0; i < ghosts.Count; i++)
            {
                IReadOnlyList<PlayerAction> actions = ghosts[i].Log.Actions;
                if (turnIndex < actions.Count)
                    ghosts[i].Actor.Replay(actions[turnIndex], gridManager);
            }
        }

        private void SpawnGhosts()
        {
            ClearGhosts();
            if (completedLoops.Count == 0)
                return;

            ghostRoot = new GameObject("Loop Ghosts").transform;
            ghostRoot.SetParent(transform, false);

            foreach (LoopActionLog log in completedLoops)
            {
                GameObject ghostObject;
                if (ghostPrefab != null)
                {
                    ghostObject = Instantiate(ghostPrefab);
                }
                else
                {
                    ghostObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ghostObject.transform.localScale = Vector3.one * 0.45f;
                    Collider collider = ghostObject.GetComponent<Collider>();
                    if (collider != null)
                        Destroy(collider);
                }

                ghostObject.name = $"Loop Ghost {ghosts.Count + 1}";
                ghostObject.transform.SetParent(ghostRoot, true);
                ghostObject.transform.position = gridManager.CellToWorld(levelData.PlayerStart);
                ghostObject.transform.rotation = DirectionUtility.ToRotation(levelData.PlayerDirection);

                Ghost ghost = ghostObject.GetComponent<Ghost>();
                if (ghost == null)
                    ghost = ghostObject.AddComponent<Ghost>();

                ghosts.Add(new GhostPlayback(log, ghost));
            }
        }

        private void ClearGhosts()
        {
            ghosts.Clear();
            if (ghostRoot != null)
                Destroy(ghostRoot.gameObject);
            ghostRoot = null;
        }

        private void DestroyRoomObjects()
        {
            ClearGhosts();
            roomSnapshot.Clear();
            turnSystems.Clear();
            if (roomRoot != null)
                Destroy(roomRoot.gameObject);
            roomRoot = null;
        }

        private void NotifyCountersChanged()
        {
            MoveCountChanged?.Invoke(turnsUsed, TurnLimit);
            LoopChanged?.Invoke(CurrentLoopNumber);
        }

        private void SetState(StageState nextState)
        {
            if (State == nextState)
                return;

            State = nextState;
            StateChanged?.Invoke(this);
        }

        private sealed class GhostPlayback
        {
            public LoopActionLog Log { get; }
            public Ghost Actor { get; }

            public GhostPlayback(LoopActionLog log, Ghost actor)
            {
                Log = log;
                Actor = actor;
            }
        }

        private sealed class RoomObjectSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly bool active;
            private readonly List<ComponentState> componentStates = new List<ComponentState>();

            public RoomObjectSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
                active = target.gameObject.activeSelf;

                foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
                {
                    if (behaviour is IRoomStateful roomStateful)
                        componentStates.Add(new ComponentState(roomStateful, roomStateful.CaptureRoomState()));
                }
            }

            public void Restore()
            {
                if (target == null)
                {
                    Debug.LogWarning("A room object was destroyed and cannot be restored from its loop snapshot.");
                    return;
                }

                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
                target.gameObject.SetActive(active);

                foreach (ComponentState componentState in componentStates)
                {
                    if (componentState.Component is UnityEngine.Object unityObject && unityObject == null)
                    {
                        Debug.LogWarning("A resettable room component was destroyed and cannot be restored.");
                        continue;
                    }

                    componentState.Component.RestoreRoomState(componentState.State);
                }
            }
        }

        private sealed class ComponentState
        {
            public IRoomStateful Component { get; }
            public object State { get; }

            public ComponentState(IRoomStateful component, object state)
            {
                Component = component;
                State = state;
            }
        }
    }
}
