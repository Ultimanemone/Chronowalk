using System;
using System.Collections.Generic;
using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage;
using ChronowalkGame.Game.Stage.Tile;
using UnityEditor;
using UnityEngine;

namespace ChronowalkGame.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private enum ToolMode
        {
            Place,
            Select,
            Erase
        }

        private const string PreviewRootName = "[Chronowalk Level Editor Preview]";

        private LevelData level;
        private TileObject selectedDefinition;
        private ToolMode toolMode;
        private Direction placementDirection;
        private int selectedPlacementIndex = -1;
        private bool editMode;
        private Vector2 paletteScroll;
        private GameObject previewRoot;
        private List<TileObject> definitions = new List<TileObject>();

        [MenuItem("Chronowalk/Level Editor")]
        private static void Open()
        {
            GetWindow<LevelEditorWindow>("Level Editor");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RefreshDefinitions();
            RefreshPreview();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            DestroyPreview();
        }

        private void OnProjectChange()
        {
            RefreshDefinitions();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            level = (LevelData)EditorGUILayout.ObjectField("Level Data", level, typeof(LevelData), false);
            if (EditorGUI.EndChangeCheck())
            {
                selectedPlacementIndex = -1;
                RefreshPreview();
                SceneView.RepaintAll();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Level"))
                    CreateLevelAsset();

                using (new EditorGUI.DisabledScope(level == null))
                {
                    if (GUILayout.Button("Save"))
                    {
                        EditorUtility.SetDirty(level);
                        AssetDatabase.SaveAssets();
                    }

                    if (GUILayout.Button("Refresh Preview"))
                        RefreshPreview();
                }
            }

            if (level == null)
            {
                EditorGUILayout.HelpBox("Create a level asset or assign one to begin editing.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            Undo.RecordObject(level, "Edit Level Grid");
            EditorGUI.BeginChangeCheck();
            level.Width = Mathf.Max(1, EditorGUILayout.IntField("Width", level.Width));
            level.Height = Mathf.Max(1, EditorGUILayout.IntField("Height", level.Height));
            level.TurnLimit = Mathf.Max(1, EditorGUILayout.IntField("Turn Limit", level.TurnLimit));
            level.Origin = EditorGUILayout.Vector3Field("Origin", level.Origin);
            level.PlayerStart = EditorGUILayout.Vector2IntField("Player Start", level.PlayerStart);
            level.PlayerDirection = (Direction)EditorGUILayout.EnumPopup("Player Facing", level.PlayerDirection);
            if (EditorGUI.EndChangeCheck())
            {
                ClampPlacements();
                level.PlayerStart = new Vector2Int(
                    Mathf.Clamp(level.PlayerStart.x, 0, level.Width - 1),
                    Mathf.Clamp(level.PlayerStart.y, 0, level.Height - 1));
                MarkLevelDirty();
                RefreshPreview();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            editMode = EditorGUILayout.ToggleLeft("Edit in Scene View", editMode);
            toolMode = (ToolMode)GUILayout.Toolbar((int)toolMode, new[] { "Place", "Select", "Erase" });
            using (new EditorGUI.DisabledScope(toolMode != ToolMode.Place))
                placementDirection = (Direction)EditorGUILayout.EnumPopup("New Placement Direction", placementDirection);

            EditorGUILayout.HelpBox(
                "With Scene View editing enabled, click a grid cell to use the selected tool. " +
                "The preview is temporary; save the Level Data asset to keep changes.",
                MessageType.Info);
            ObjectData spawnPlacement = level.Placements.Find(
                placement => placement != null && placement.Position == level.PlayerStart);
            if (spawnPlacement != null &&
                spawnPlacement.Definition != null &&
                spawnPlacement.Definition.TileType != TileType.Corridor)
            {
                EditorGUILayout.HelpBox(
                    "Player start overlaps a non-corridor tile. The stage loader will reject this level.",
                    MessageType.Error);
            }

            DrawPalette();
            DrawSelectedPlacement();
        }

        private void DrawPalette()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);

            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox("No tile definition assets found. Create one from Assets > Create > Chronowalk > Tiles.", MessageType.Warning);
                return;
            }

            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(160));
            foreach (TileObject definition in definitions)
            {
                if (definition == null)
                    continue;

                Texture icon = definition.Sprite != null ? definition.Sprite.texture : null;
                GUIContent content = new GUIContent(definition.name, icon);
                bool selected = selectedDefinition == definition;
                if (GUILayout.Toggle(selected, content, "Button", GUILayout.Height(28)))
                    selectedDefinition = definition;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawSelectedPlacement()
        {
            if (level == null || selectedPlacementIndex < 0 || selectedPlacementIndex >= level.Placements.Count)
                return;

            ObjectData placement = level.Placements[selectedPlacementIndex];
            if (placement == null)
                return;

            Undo.RecordObject(level, "Edit Level Placement");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Placement", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ObjectField("Definition", placement.Definition, typeof(TileObject), false);
            placement.Position = EditorGUILayout.Vector2IntField("Position", placement.Position);
            placement.Direction = (Direction)EditorGUILayout.EnumPopup("Direction", placement.Direction);
            if (EditorGUI.EndChangeCheck())
            {
                ClampPlacement(placement);
                MarkLevelDirty();
                RefreshPreview();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Delete Selected Placement"))
                RemovePlacement(selectedPlacementIndex);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (level == null)
                return;

            DrawGrid();
            DrawPlayerStart();
            DrawPlacementDirections();

            if (!editMode || EditorApplication.isPlaying)
                return;

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.R && toolMode == ToolMode.Place)
            {
                placementDirection = RotateClockwise(placementDirection);
                Repaint();
                currentEvent.Use();
            }

            if (currentEvent.type != EventType.MouseDown || (currentEvent.button != 0 && currentEvent.button != 1))
                return;

            if (!TryGetCell(currentEvent.mousePosition, out Vector2Int cell))
                return;

            if (currentEvent.button == 1 || toolMode == ToolMode.Erase)
                RemoveAt(cell);
            else if (toolMode == ToolMode.Place)
                PlaceAt(cell);
            else
                SelectAt(cell);

            currentEvent.Use();
            sceneView.Repaint();
            Repaint();
        }

        private void DrawGrid()
        {
            Vector3 origin = level.Origin;
            Color gridColor = new Color(0.25f, 0.8f, 1f, 0.75f);
            Handles.color = gridColor;

            for (int x = 0; x <= level.Width; x++)
            {
                Vector3 start = origin + new Vector3(x, 0f, 0f);
                Vector3 end = start + new Vector3(0f, level.Height, 0f);
                Handles.DrawLine(start, end);
            }

            for (int y = 0; y <= level.Height; y++)
            {
                Vector3 start = origin + new Vector3(0f, y, 0f);
                Vector3 end = start + new Vector3(level.Width, 0f, 0f);
                Handles.DrawLine(start, end);
            }

            Handles.color = Color.white;
            Handles.Label(origin + new Vector3(0f, level.Height + 0.2f, 0f), level.name);

            Event currentEvent = Event.current;
            if (editMode && TryGetCell(currentEvent.mousePosition, out Vector2Int hoveredCell))
            {
                Vector3 bottomLeft = origin + new Vector3(hoveredCell.x, hoveredCell.y, -0.02f);
                Vector3[] corners =
                {
                    bottomLeft,
                    bottomLeft + Vector3.right,
                    bottomLeft + Vector3.right + Vector3.up,
                    bottomLeft + Vector3.up
                };
                Handles.DrawSolidRectangleWithOutline(
                    corners,
                    new Color(1f, 0.85f, 0.1f, 0.15f),
                    new Color(1f, 0.85f, 0.1f, 0.9f));
            }
        }

        private void DrawPlacementDirections()
        {
            if (level == null)
                return;

            for (int i = 0; i < level.Placements.Count; i++)
            {
                ObjectData placement = level.Placements[i];
                if (placement == null)
                    continue;

                Vector3 center = CellCenter(placement.Position);
                Vector3 direction = DirectionVector(placement.Direction);
                Color previousColor = Handles.color;
                Handles.color = i == selectedPlacementIndex ? Color.yellow : Color.white;
                Handles.ArrowHandleCap(
                    0,
                    center + Vector3.back * 0.05f,
                    Quaternion.LookRotation(Vector3.forward, direction),
                    0.35f,
                    EventType.Repaint);
                Handles.color = previousColor;
            }
        }

        private void DrawPlayerStart()
        {
            Vector3 center = CellCenter(level.PlayerStart);
            Handles.color = Color.green;
            Handles.DrawWireCube(center, new Vector3(0.8f, 0.8f, 0.05f));
            Handles.ArrowHandleCap(
                0,
                center + Vector3.back * 0.05f,
                Quaternion.LookRotation(Vector3.forward, DirectionVector(level.PlayerDirection)),
                0.45f,
                EventType.Repaint);
            Handles.Label(center + Vector3.up * 0.45f, "Player Start");
            Handles.color = Color.white;
        }

        private bool TryGetCell(Vector2 guiPosition, out Vector2Int cell)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
            Plane gridPlane = new Plane(Vector3.forward, level.Origin);
            if (!gridPlane.Raycast(ray, out float distance))
            {
                cell = default;
                return false;
            }

            Vector3 localPoint = ray.GetPoint(distance) - level.Origin;
            cell = new Vector2Int(Mathf.FloorToInt(localPoint.x), Mathf.FloorToInt(localPoint.y));
            return cell.x >= 0 && cell.x < level.Width && cell.y >= 0 && cell.y < level.Height;
        }

        private void PlaceAt(Vector2Int cell)
        {
            if (selectedDefinition == null)
            {
                ShowNotification(new GUIContent("Select a tile definition first."));
                return;
            }

            if (cell == level.PlayerStart &&
                selectedDefinition.TileType != TileType.Corridor)
            {
                ShowNotification(new GUIContent("Only a corridor tile can share the player start cell."));
                return;
            }

            int existingIndex = FindPlacementAt(cell);
            Undo.RecordObject(level, "Place Level Tile");

            ObjectData placement = new ObjectData
            {
                Definition = selectedDefinition,
                Position = cell,
                Direction = placementDirection
            };

            if (existingIndex >= 0)
            {
                level.Placements[existingIndex] = placement;
                selectedPlacementIndex = existingIndex;
            }
            else
            {
                level.Placements.Add(placement);
                selectedPlacementIndex = level.Placements.Count - 1;
            }

            MarkLevelDirty();
            RefreshPreview();
        }

        private void SelectAt(Vector2Int cell)
        {
            selectedPlacementIndex = FindPlacementAt(cell);
            if (selectedPlacementIndex >= 0)
            {
                ObjectData placement = level.Placements[selectedPlacementIndex];
                selectedDefinition = placement.Definition;
                placementDirection = placement.Direction;
            }
        }

        private void RemoveAt(Vector2Int cell)
        {
            int index = FindPlacementAt(cell);
            if (index >= 0)
                RemovePlacement(index);
        }

        private void RemovePlacement(int index)
        {
            if (level == null || index < 0 || index >= level.Placements.Count)
                return;

            Undo.RecordObject(level, "Erase Level Tile");
            level.Placements.RemoveAt(index);
            selectedPlacementIndex = -1;
            MarkLevelDirty();
            RefreshPreview();
        }

        private int FindPlacementAt(Vector2Int cell)
        {
            for (int i = level.Placements.Count - 1; i >= 0; i--)
            {
                ObjectData placement = level.Placements[i];
                if (placement != null && placement.Position == cell)
                    return i;
            }

            return -1;
        }

        private void ClampPlacement(ObjectData placement)
        {
            placement.Position = new Vector2Int(
                Mathf.Clamp(placement.Position.x, 0, level.Width - 1),
                Mathf.Clamp(placement.Position.y, 0, level.Height - 1));
        }

        private void CreateLevelAsset()
        {
            const string folder = "Assets/Levels";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Levels");

            LevelData newLevel = CreateInstance<LevelData>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NewLevel.asset");
            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();
            level = newLevel;
            selectedPlacementIndex = -1;
            RefreshPreview();
            EditorGUIUtility.PingObject(newLevel);
        }

        private void RefreshDefinitions()
        {
            definitions.Clear();
            string[] guids = AssetDatabase.FindAssets("t:TileObject");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TileObject definition = AssetDatabase.LoadAssetAtPath<TileObject>(path);
                if (definition != null)
                    definitions.Add(definition);
            }

            definitions.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
        }

        private void RefreshPreview()
        {
            DestroyPreview();
            if (level == null || EditorApplication.isPlaying)
                return;

            previewRoot = new GameObject(PreviewRootName)
            {
                hideFlags = HideFlags.DontSave
            };

            foreach (ObjectData placement in level.Placements)
            {
                if (placement == null || placement.Definition == null)
                    continue;

                GameObject instance = CreatePreviewObject(placement.Definition);
                if (instance == null)
                    continue;

                instance.name = $"{placement.Definition.name} ({placement.Position.x}, {placement.Position.y})";
                SetHideFlagsRecursively(instance.transform, HideFlags.DontSave);
                instance.transform.SetParent(previewRoot.transform, false);
                instance.transform.position = CellCenter(placement.Position);
                instance.transform.rotation = DirectionRotation(placement.Direction);
            }
        }

        private GameObject CreatePreviewObject(TileObject definition)
        {
            if (definition.Prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(definition.Prefab);
                return instance;
            }

            if (definition.Sprite != null)
            {
                GameObject spriteObject = new GameObject(definition.name);
                spriteObject.AddComponent<SpriteRenderer>().sprite = definition.Sprite;
                return spriteObject;
            }

            return new GameObject(definition.name);
        }

        private void DestroyPreview()
        {
            if (previewRoot != null)
                DestroyImmediate(previewRoot);
            previewRoot = null;
        }

        private static void SetHideFlagsRecursively(Transform target, HideFlags flags)
        {
            target.gameObject.hideFlags = flags;
            for (int i = 0; i < target.childCount; i++)
                SetHideFlagsRecursively(target.GetChild(i), flags);
        }

        private Vector3 CellCenter(Vector2Int position)
        {
            return level.Origin + new Vector3(position.x + 0.5f, position.y + 0.5f, 0f);
        }

        private static Vector3 DirectionVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Down: return Vector3.down;
                case Direction.Left: return Vector3.left;
                case Direction.Right: return Vector3.right;
                default: return Vector3.up;
            }
        }

        private static Quaternion DirectionRotation(Direction direction)
        {
            float angle = direction == Direction.Right ? -90f :
                          direction == Direction.Down ? 180f :
                          direction == Direction.Left ? 90f : 0f;
            return Quaternion.Euler(0f, 0f, angle);
        }

        private static Direction RotateClockwise(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Direction.Right;
                case Direction.Right: return Direction.Down;
                case Direction.Down: return Direction.Left;
                default: return Direction.Up;
            }
        }

        private void MarkLevelDirty()
        {
            EditorUtility.SetDirty(level);
        }

        private void ClampPlacements()
        {
            foreach (ObjectData placement in level.Placements)
            {
                if (placement != null)
                    ClampPlacement(placement);
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                DestroyPreview();
            else if (state == PlayModeStateChange.EnteredEditMode)
                RefreshPreview();
        }

        private void OnUndoRedo()
        {
            RefreshPreview();
            Repaint();
            SceneView.RepaintAll();
        }
    }
}
