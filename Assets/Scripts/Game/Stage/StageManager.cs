using System;
using System.Collections;
using System.Collections.Generic;
using ChronowalkGame.Game.Data;
using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage.Contracts;
using ChronowalkGame.Game.Stage.Data;
using ChronowalkGame.Game.Stage.Objects.Definitions;
using ChronowalkGame.Game.Stage.Objects.Runtime;
using ChronowalkGame.Game.Stage.Tiles.Definitions;
using ChronowalkGame.Game.Stage.Tiles.Runtime;
using ChronowalkGame.Utils;
using ChronowalkGame.Utils.Logger;
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
            Resetting,
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
        private readonly List<IStageTurnSystem> turnSystems = new List<IStageTurnSystem>();
        private readonly List<Ghost> ghosts = new List<Ghost>();

        private Transform roomRoot;
        private Transform ghostRoot;
        private int turnsUsed;
        private bool loopEndRequested;
        private string pendingLoopEndReason;
        private Coroutine turnResolution;

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
                if (placement.Definition is StageObjectDefinition)
                    instance.transform.position += Vector3.back * 0.1f;
                if (!(placement.Definition is StageObjectDefinition) || placement.HasDirection)
                    instance.transform.rotation = DirectionUtils.ToRotation(placement.Direction);

                if (placement.Definition is StageTileDefinition tileDefinition)
                {
                    gridManager.SetTile(placement.Position, tileDefinition, instance);
                    ConfigureTileRuntime(instance, tileDefinition);
                }
                else if (placement.Definition is StageObjectDefinition objectDefinition)
                {
                    gridManager.AddRoomObject(instance);
                    InitializeRoomObjectRuntime(
                        instance,
                        objectDefinition,
                        placement.Direction,
                        placement.HasDirection,
                        placement.CanBePushed);
                }

                foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour is IStageTurnSystem turnSystem)
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
                !DirectionUtils.TryGetDirection(action.Direction, out _))
                return false;

            if (action.Type == ActionType.Move &&
                !RoomActionResolver.CanMove(action.Direction, player.transform, gridManager))
                return false;

            if (!AreGhostsReady(turnsUsed))
            {
                SetState(StageState.Error);
                return false;
            }

            SetState(StageState.ResolvingTurn);
            loopEndRequested = false;
            pendingLoopEndReason = null;
            currentLoopActions.Add(action);
            turnResolution = StartCoroutine(ResolveTurn(action, turnsUsed));
            return true;
        }

        private IEnumerator ResolveTurn(PlayerAction action, int turnIndex)
        {
            yield return player.PlayAction(action, gridManager);
            if (State != StageState.ResolvingTurn)
                yield break;

            for (int i = 0; i < ghosts.Count; i++)
            {
                yield return ghosts[i].ReplayTurn(turnIndex);
                if (State != StageState.ResolvingTurn)
                    yield break;
            }

            int turnNumber = turnsUsed + 1;
            for (int i = 0; i < turnSystems.Count; i++)
                turnSystems[i].ResolveTurn(turnNumber);

            ResolveActorHazards();
            turnsUsed++;
            MoveCountChanged?.Invoke(turnsUsed, TurnLimit);

            if (State == StageState.Completed)
            {
                player.ShowIdleAnimation();
                turnResolution = null;
                yield break;
            }

            Vector2Int playerCell = gridManager.WorldToCell(player.transform.position);
            if (player.IsAlive && gridManager.IsType(TileType.Exit, playerCell))
            {
                player.ShowIdleAnimation();
                CompleteLevel();
                turnResolution = null;
                yield break;
            }

            if (!loopEndRequested && turnsUsed >= TurnLimit)
            {
                loopEndRequested = true;
                pendingLoopEndReason = "Turn limit reached";
            }

            if (loopEndRequested)
            {
                yield return FinishCurrentLoop(pendingLoopEndReason);
                turnResolution = null;
                yield break;
            }

            player.ShowIdleAnimation();
            SetState(StageState.AwaitingAction);
            turnResolution = null;
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

            StartCoroutine(FinishCurrentLoop("Manually ended"));
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
                StartCoroutine(FinishCurrentLoop("Player died"));
            }
        }

        public bool RestartCurrentLoop()
        {
            if (State != StageState.AwaitingAction &&
                State != StageState.ResolvingTurn &&
                State != StageState.Completed)
                return false;

            LastLoopEndReason = "Current loop restarted";
            BeginLoopReset();
            return true;
        }

        public bool RemoveMostRecentLoop()
        {
            if (State != StageState.AwaitingAction || completedLoops.Count == 0)
                return false;

            completedLoops.RemoveAt(completedLoops.Count - 1);
            LastLoopEndReason = "Most recent loop erased";
            BeginLoopReset();
            return true;
        }

        public bool RestartLevel()
        {
            if (State != StageState.AwaitingAction &&
                State != StageState.ResolvingTurn &&
                State != StageState.Completed)
                return false;

            CancelTurnResolution();
            completedLoops.Clear();
            currentLoopActions.Clear();
            LastLoopEndReason = "Level restarted";
            BeginLoopReset();
            AdvLogger.Log("Level restarted");
            return true;
        }

        public void CompleteLevel()
        {
            if (State == StageState.AwaitingAction || State == StageState.ResolvingTurn)
                SetState(StageState.Completed);
            AdvLogger.Log("Stage Complete");
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

            if (level.Placements == null)
            {
                error = "Level placements list is missing.";
                return false;
            }

            HashSet<Vector2Int> occupiedTileCells = new HashSet<Vector2Int>();
            HashSet<Vector2Int> occupiedObjectCells = new HashSet<Vector2Int>();
            foreach (ObjectData placement in level.Placements)
            {
                if (placement == null || placement.Definition == null)
                {
                    error = "Every level placement must have a room element definition.";
                    return false;
                }

                HashSet<Vector2Int> occupiedCells;
                if (placement.Definition is StageTileDefinition)
                    occupiedCells = occupiedTileCells;
                else if (placement.Definition is StageObjectDefinition)
                    occupiedCells = occupiedObjectCells;
                else
                {
                    error = $"Unsupported room element definition '{placement.Definition.name}'.";
                    return false;
                }

                if (!occupiedCells.Add(placement.Position))
                {
                    string elementType = placement.Definition is StageTileDefinition ? "tile" : "object";
                    error = $"More than one {elementType} is placed at {placement.Position}.";
                    return false;
                }
            }

            if (level.Placements.Exists(placement =>
                    placement.Definition is StageTileDefinition tile &&
                    placement.Position == level.PlayerStart &&
                    tile.TileType != TileType.Corridor))
            {
                error = "Player start can overlap only a corridor tile.";
                return false;
            }

            error = null;
            return true;
        }

        private GameObject CreateRoomObject(StageElement definition)
        {
            if (definition.Prefab != null)
                return Instantiate(definition.Prefab);

            GameObject instance = new GameObject(definition.name);
            return instance;
        }

        private static void ConfigureTileRuntime(GameObject instance, StageTileDefinition definition)
        {
            switch (definition.TileType)
            {
                case TileType.Trap:
                    TrapRuntime trapRuntime = instance.GetComponentInChildren<TrapRuntime>(true);
                    if (trapRuntime == null)
                        trapRuntime = instance.AddComponent<TrapRuntime>();
                    trapRuntime.Configure(((Trap)definition).Behavior);
                    break;
                case TileType.Door:
                    if (instance.GetComponentInChildren<DoorRuntime>(true) == null)
                        instance.AddComponent<DoorRuntime>();
                    break;
            }
        }

        private void InitializeRoomObjectRuntime(
            GameObject instance,
            StageObjectDefinition definition,
            Direction direction,
            bool hasDirection,
            bool canBePushed)
        {
            if (definition.ObjectType == RoomObjectType.Box &&
                instance.GetComponentInChildren<BoxRuntime>(true) == null)
                instance.AddComponent<BoxRuntime>();

            if (definition.ObjectType == RoomObjectType.Laser &&
                instance.GetComponentInChildren<LaserRuntime>(true) == null)
                instance.AddComponent<LaserRuntime>();

            StageObjectRuntime objectRuntime = instance.GetComponentInChildren<StageObjectRuntime>(true);
            if (objectRuntime == null)
                objectRuntime = instance.AddComponent<StageObjectRuntime>();
            objectRuntime.Initialize(new StageObjectRuntime.InitializationData(
                definition,
                instance.transform,
                canBePushed,
                hasDirection,
                direction));

            switch (definition.ObjectType)
            {
                case RoomObjectType.Box:
                    instance.GetComponentInChildren<BoxRuntime>(true).Initialize(instance.transform);
                    break;
                case RoomObjectType.Laser:
                    Vector2Int beamDirection = hasDirection
                        ? DirectionUtils.ToVector(direction)
                        : Vector2Int.zero;
                    instance.GetComponentInChildren<LaserRuntime>(true).Initialize(beamDirection);
                    break;
            }
        }

        private IEnumerator FinishCurrentLoop(string reason)
        {
            completedLoops.Add(new LoopActionLog(currentLoopActions));
            LastLoopEndReason = reason;
            yield return ResetToLoopStart();
        }

        private void BeginLoopReset()
        {
            CancelTurnResolution();
            StartCoroutine(ResetToLoopStart());
        }

        private IEnumerator ResetToLoopStart()
        {
            SetState(StageState.Resetting);
            Vector3 positionBeforeReset = player.transform.position;
            player.InterruptAnimation();
            ClearGhosts();
            for (int i = 0; i < roomSnapshot.Count; i++)
                roomSnapshot[i].Restore();

            Vector3 resetDestination = player.transform.position;
            player.transform.position = positionBeforeReset;
            Vector3 displacement = resetDestination - positionBeforeReset;
            Vector2Int directionVector = Mathf.Abs(displacement.x) >= Mathf.Abs(displacement.y)
                ? new Vector2Int((int)Mathf.Sign(displacement.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(displacement.y));
            Direction resetDirection = player.Facing;
            if (directionVector != Vector2Int.zero)
                DirectionUtils.TryGetDirection(directionVector, out resetDirection);

            currentLoopActions.Clear();
            turnsUsed = 0;
            loopEndRequested = false;
            pendingLoopEndReason = null;

            if (displacement.sqrMagnitude > Mathf.Epsilon)
                yield return player.AnimateResetTo(resetDestination, resetDirection);
            else
                player.ShowIdleAnimation();

            SpawnGhosts();
            SetState(StageState.AwaitingAction);
            NotifyCountersChanged();
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

        private bool AreGhostsReady(int turnIndex)
        {
            for (int i = 0; i < ghosts.Count; i++)
            {
                if (!ghosts[i].IsReadyForTurn(turnIndex))
                {
                    Debug.LogError(
                        $"Ghost for loop {ghosts[i].LoopNumber} was out of sync at turn {turnIndex}.",
                        ghosts[i]);
                    return false;
                }
            }

            return true;
        }

        private void ResolveActorHazards()
        {
            if (player.IsAlive)
            {
                Vector2Int playerCell = gridManager.WorldToCell(player.transform.position);
                if (gridManager.IsHazard(playerCell) || gridManager.IsLaserHazard(playerCell))
                    player.Die();
            }

            for (int i = 0; i < ghosts.Count; i++)
            {
                Ghost ghost = ghosts[i];
                if (!ghost.IsAlive)
                    continue;

                Vector2Int ghostCell = gridManager.WorldToCell(ghost.transform.position);
                if (gridManager.IsHazard(ghostCell) || gridManager.IsLaserHazard(ghostCell))
                    ghost.Die();
            }
        }

        private void CancelTurnResolution()
        {
            if (turnResolution == null)
                return;

            Coroutine activeResolution = turnResolution;
            turnResolution = null;
            StopCoroutine(activeResolution);
        }

        private void SpawnGhosts()
        {
            ClearGhosts();
            if (completedLoops.Count == 0)
                return;

            ghostRoot = new GameObject("Loop Ghosts").transform;
            ghostRoot.SetParent(transform, false);

            for (int i = 0; i < completedLoops.Count; i++)
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

                ghostObject.name = $"Loop Ghost {i + 1}";
                ghostObject.transform.SetParent(ghostRoot, true);

                Ghost ghost = ghostObject.GetComponent<Ghost>();
                if (ghost == null)
                    ghost = ghostObject.AddComponent<Ghost>();

                ghost.Initialize(
                    i + 1,
                    completedLoops[i],
                    levelData.PlayerStart,
                    levelData.PlayerDirection,
                    gridManager);
                ghosts.Add(ghost);
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
                    if (behaviour is IStateful roomStateful)
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
            public IStateful Component { get; }
            public object State { get; }

            public ComponentState(IStateful component, object state)
            {
                Component = component;
                State = state;
            }
        }
    }
}
