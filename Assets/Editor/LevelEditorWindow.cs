using ChronowalkGame.Game.PlayerEntities;
using ChronowalkGame.Game.Stage;
using ChronowalkGame.Game.Stage.Data;
using ChronowalkGame.Game.Stage.Objects.Definitions;
using ChronowalkGame.Game.Stage.Tiles.Definitions;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using UnityEditor;
using UnityEngine;

namespace ChronowalkGame.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private enum ToolMode
        {
            Place,
            Select
        }

        private enum PlacementLayer
        {
            Tile,
            Object
        }

        private const string PreviewRootName = "[Chronowalk Level Editor Preview]";

        private LevelData level;
        private StageElement selectedDefinition;
        private ToolMode toolMode;
        private PlacementLayer placementLayer;
        private Direction placementDirection;
        private int selectedPlacementIndex = -1;
        private bool editMode = true;
        private Vector2 paletteScroll;
        private Vector2 windowScroll;
        private string paletteSearch = string.Empty;
        private GameObject previewRoot;
        private List<StageElement> definitions = new List<StageElement>();
        private GUIStyle tileLabelStyle;
        private readonly HashSet<Vector2Int> brushedCells = new HashSet<Vector2Int>();
        private Vector2Int lastBrushCell;
        private bool hasLastBrushCell;
        private int brushUndoGroup;
        private int brushButton = -1;
        private bool eraseBrushStroke;

        [MenuItem("Chronowalk/Level Editor")]
        private static void Open()
        {
            GetWindow<LevelEditorWindow>("Level Editor");
        }

        public static void OpenForLevel(LevelData level)
        {
            if (level == null)
                return;

            LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
            window.level = level;
            window.selectedPlacementIndex = -1;
            window.RefreshPreview();
            window.Show();
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
            windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
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
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            Undo.RecordObject(level, "Edit Level Grid");
            EditorGUI.BeginChangeCheck();
            level.Width = Mathf.Max(1, EditorGUILayout.IntField("Minimum Width", level.Width));
            level.Height = Mathf.Max(1, EditorGUILayout.IntField("Minimum Height", level.Height));
            level.TurnLimit = Mathf.Max(1, EditorGUILayout.IntField("Turn Limit", level.TurnLimit));
            level.Origin = EditorGUILayout.Vector3Field("Origin", level.Origin);
            level.PlayerStart = EditorGUILayout.Vector2IntField("Player Start", level.PlayerStart);
            level.PlayerDirection = (Direction)EditorGUILayout.EnumPopup("Player Facing", level.PlayerDirection);
            if (EditorGUI.EndChangeCheck())
            {
                MarkLevelDirty();
                RefreshPreview();
                SceneView.RepaintAll();
            }
            BoundsInt gridBounds = level.GetGridBounds();
            EditorGUILayout.LabelField(
                "Current Bounds",
                $"X {gridBounds.xMin}..{gridBounds.xMax - 1}, Y {gridBounds.yMin}..{gridBounds.yMax - 1} " +
                $"({gridBounds.size.x} x {gridBounds.size.y})");

            EditorGUILayout.Space();
            editMode = EditorGUILayout.ToggleLeft("Edit in Scene View", editMode);
            EditorGUI.BeginChangeCheck();
            placementLayer = (PlacementLayer)GUILayout.Toolbar(
                (int)placementLayer,
                new[] { "Tile Placement", "Object Placement" });
            if (EditorGUI.EndChangeCheck())
            {
                EndBrushStroke();
                selectedDefinition = null;
                selectedPlacementIndex = -1;
            }

            toolMode = (ToolMode)GUILayout.Toolbar((int)toolMode, new[] { "Place", "Select" });
            using (new EditorGUI.DisabledScope(toolMode != ToolMode.Place))
                placementDirection = (Direction)EditorGUILayout.EnumPopup("New Placement Direction", placementDirection);

            EditorGUILayout.HelpBox(
                "Choose Tile Placement or Object Placement before using the palette. " +
                "Left click and drag places the selected type in Place mode; right click and drag erases only that layer. " +
                "Select mode selects placements in the chosen layer. " +
                "A cell can contain one tile and one object. Object placements can be individually marked pushable. " +
                "The grid expands to include placements (including negative coordinates). " +
                "The preview is temporary; save the Level Data asset to keep changes.",
                MessageType.Info);
            ObjectData spawnPlacement = level.Placements.Find(placement =>
                placement != null &&
                placement.Position == level.PlayerStart &&
                placement.Definition is StageTileDefinition);
            if (spawnPlacement != null &&
                spawnPlacement.Definition is StageTileDefinition spawnTile &&
                spawnTile.TileType != TileType.Corridor)
            {
                EditorGUILayout.HelpBox(
                    "Player start overlaps a non-corridor tile. The stage loader will reject this level.",
                    MessageType.Error);
            }

            DrawPalette();
            DrawSelectedPlacement();
            EditorGUILayout.EndScrollView();
        }

        private void DrawPalette()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                placementLayer == PlacementLayer.Tile ? "Tile Palette" : "Object Palette",
                EditorStyles.boldLabel);

            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No tile or object definitions found. Create them from Assets > Create > Chronowalk.",
                    MessageType.Warning);
                return;
            }

            paletteSearch = EditorGUILayout.TextField("Search", paletteSearch);
            float listHeight = Mathf.Clamp(position.height - 380f, 100f, 280f);
            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(listHeight));
            bool foundMatch = false;
            foreach (StageElement definition in definitions)
            {
                bool matchesLayer = placementLayer == PlacementLayer.Tile
                    ? definition is StageTileDefinition
                    : definition is StageObjectDefinition;
                if (definition == null ||
                    !matchesLayer ||
                    (!string.IsNullOrEmpty(paletteSearch) &&
                     definition.name.IndexOf(paletteSearch, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                foundMatch = true;
                Texture icon = definition.Sprite != null ? definition.Sprite.texture : null;
                GUIContent content = new GUIContent(definition.name, icon);
                bool selected = selectedDefinition == definition;
                if (GUILayout.Toggle(selected, content, "Button", GUILayout.Height(28)))
                    selectedDefinition = definition;
            }
            if (!foundMatch)
                EditorGUILayout.HelpBox(
                    $"No {placementLayer.ToString().ToLowerInvariant()} definitions match the search.",
                    MessageType.Info);
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
            EditorGUILayout.ObjectField("Definition", placement.Definition, typeof(StageElement), false);
            placement.Position = EditorGUILayout.Vector2IntField("Position", placement.Position);
            placement.Direction = (Direction)EditorGUILayout.EnumPopup("Direction", placement.Direction);
            if (placement.Definition is StageObjectDefinition)
                placement.CanBePushed = EditorGUILayout.Toggle("Can Be Pushed", placement.CanBePushed);
            if (EditorGUI.EndChangeCheck())
            {
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

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == brushButton)
            {
                EndBrushStroke();
                return;
            }

            if (!editMode || EditorApplication.isPlaying)
                return;

            if (currentEvent.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.R && toolMode == ToolMode.Place)
            {
                placementDirection = RotateClockwise(placementDirection);
                Repaint();
                currentEvent.Use();
            }

            bool isBrushEvent = currentEvent.type == EventType.MouseDown ||
                                currentEvent.type == EventType.MouseDrag;
            if (!isBrushEvent)
                return;

            if (currentEvent.type == EventType.MouseDown)
            {
                if (!TryGetCell(currentEvent.mousePosition, out Vector2Int cell))
                    return;

                if (currentEvent.button == 1)
                    BeginBrushStroke(cell, true, currentEvent.button);
                else if (currentEvent.button == 0 && toolMode == ToolMode.Place)
                    BeginBrushStroke(cell, false, currentEvent.button);
                else if (currentEvent.button == 0 && toolMode == ToolMode.Select)
                    SelectAt(cell);
                else
                    return;
            }
            else if (currentEvent.type == EventType.MouseDrag &&
                     currentEvent.button == brushButton &&
                     TryGetCell(currentEvent.mousePosition, out Vector2Int cell))
            {
                ContinueBrushStroke(cell);
            }
            else
                return;

            currentEvent.Use();
            sceneView.Repaint();
            Repaint();
        }

        private void BeginBrushStroke(Vector2Int cell, bool erase, int button)
        {
            EndBrushStroke();
            Undo.IncrementCurrentGroup();
            brushUndoGroup = Undo.GetCurrentGroup();
            brushButton = button;
            eraseBrushStroke = erase;
            Undo.SetCurrentGroupName(erase ? "Erase Level Tiles" : "Paint Level Tiles");
            ApplyBrushToCell(cell);
        }

        private void ContinueBrushStroke(Vector2Int cell)
        {
            if (!hasLastBrushCell)
            {
                BeginBrushStroke(cell, eraseBrushStroke, brushButton);
                return;
            }

            int x = lastBrushCell.x;
            int y = lastBrushCell.y;
            int dx = Mathf.Abs(cell.x - x);
            int dy = Mathf.Abs(cell.y - y);
            int stepX = x < cell.x ? 1 : -1;
            int stepY = y < cell.y ? 1 : -1;
            int error = dx - dy;

            while (true)
            {
                ApplyBrushToCell(new Vector2Int(x, y));
                if (x == cell.x && y == cell.y)
                    break;

                int doubledError = error * 2;
                if (doubledError > -dy)
                {
                    error -= dy;
                    x += stepX;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    y += stepY;
                }
            }
        }

        private void ApplyBrushToCell(Vector2Int cell)
        {
            lastBrushCell = cell;
            hasLastBrushCell = true;
            if (!brushedCells.Add(cell))
                return;

            if (eraseBrushStroke)
                RemoveAt(cell);
            else
                PlaceAt(cell);
        }

        private void EndBrushStroke()
        {
            if (hasLastBrushCell && brushUndoGroup >= 0)
                Undo.CollapseUndoOperations(brushUndoGroup);

            hasLastBrushCell = false;
            brushUndoGroup = -1;
            brushButton = -1;
            eraseBrushStroke = false;
            brushedCells.Clear();
        }

        private void DrawGrid()
        {
            Vector3 origin = level.Origin;
            BoundsInt bounds = level.GetGridBounds();
            int minX = bounds.xMin - 3;
            int minY = bounds.yMin - 3;
            int maxX = bounds.xMax + 3;
            int maxY = bounds.yMax + 3;
            Color gridColor = new Color(1f, 0.6f, 0f, 0.75f);
            Handles.color = gridColor;

            for (int x = minX; x <= maxX; x++)
            {
                Vector3 start = origin + new Vector3(x, minY, 0f);
                Vector3 end = origin + new Vector3(x, maxY, 0f);
                Handles.DrawLine(start, end);
            }

            for (int y = minY; y <= maxY; y++)
            {
                Vector3 start = origin + new Vector3(minX, y, 0f);
                Vector3 end = origin + new Vector3(maxX, y, 0f);
                Handles.DrawLine(start, end);
            }

            Handles.color = Color.white;
            Handles.Label(origin + new Vector3(minX, maxY + 0.2f, 0f), level.name);

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
                bool isObject = placement.Definition is StageObjectDefinition;
                Color previousColor = Handles.color;
                Handles.color = i == selectedPlacementIndex ? Color.yellow : Color.white;
                Handles.ArrowHandleCap(
                    0,
                    center + Vector3.back * (isObject ? 0.12f : 0.05f),
                    Quaternion.LookRotation(Vector3.forward, direction),
                    0.35f,
                    EventType.Repaint);
                Handles.Label(
                    center + Vector3.up * (isObject ? 0.56f : 0.32f),
                    GetLabel(placement.Definition),
                    GetLabelStyle(placement.Definition));
                Handles.color = previousColor;
            }
        }

        private static string GetLabel(StageElement definition)
        {
            if (definition is StageTileDefinition tile)
                return $"{tile.TileType}";

            if (definition is StageObjectDefinition roomObject)
                return $"{roomObject.ObjectType}";

            return "Missing";
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
            return true;
        }

        private void PlaceAt(Vector2Int cell)
        {
            if (selectedDefinition == null)
            {
                ShowNotification(new GUIContent($"Select a {placementLayer.ToString().ToLowerInvariant()} definition first."));
                return;
            }

            bool selectedElementMatchesLayer = placementLayer == PlacementLayer.Tile
                ? selectedDefinition is StageTileDefinition
                : selectedDefinition is StageObjectDefinition;
            if (!selectedElementMatchesLayer)
                return;

            if (placementLayer == PlacementLayer.Tile &&
                cell == level.PlayerStart &&
                selectedDefinition is StageTileDefinition selectedTile &&
                selectedTile.TileType != TileType.Corridor)
            {
                ShowNotification(new GUIContent("Only a corridor tile can share the player start cell."));
                return;
            }

            int existingIndex = FindPlacementAt(cell, placementLayer);
            Undo.RecordObject(level, placementLayer == PlacementLayer.Tile ? "Place Level Tile" : "Place Level Object");

            ObjectData placement = new ObjectData
            {
                Definition = selectedDefinition,
                Position = cell,
                Direction = placementDirection,
                CanBePushed = true
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
            selectedPlacementIndex = FindPlacementAt(cell, placementLayer);
            if (selectedPlacementIndex >= 0)
            {
                ObjectData placement = level.Placements[selectedPlacementIndex];
                selectedDefinition = placement.Definition;
                placementDirection = placement.Direction;
            }
        }

        private void RemoveAt(Vector2Int cell)
        {
            int index = FindPlacementAt(cell, placementLayer);
            if (index >= 0)
                RemovePlacement(index);
        }

        private void RemovePlacement(int index)
        {
            if (level == null || index < 0 || index >= level.Placements.Count)
                return;

            Undo.RecordObject(
                level,
                placementLayer == PlacementLayer.Tile ? "Erase Level Tile" : "Erase Level Object");
            level.Placements.RemoveAt(index);
            selectedPlacementIndex = -1;
            MarkLevelDirty();
            RefreshPreview();
        }

        private int FindPlacementAt(Vector2Int cell, PlacementLayer layer)
        {
            for (int i = level.Placements.Count - 1; i >= 0; i--)
            {
                ObjectData placement = level.Placements[i];
                bool matchesLayer = placement != null && (layer == PlacementLayer.Tile
                    ? placement.Definition is StageTileDefinition
                    : placement.Definition is StageObjectDefinition);
                if (matchesLayer && placement.Position == cell)
                    return i;
            }

            return -1;
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
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StageElement definition = AssetDatabase.LoadAssetAtPath<StageElement>(path);
                if (definition is StageTileDefinition || definition is StageObjectDefinition)
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
                Vector3 position = CellCenter(placement.Position);
                if (placement.Definition is StageObjectDefinition)
                    position.z -= 0.1f;
                instance.transform.position = position;
                instance.transform.rotation = DirectionRotation(placement.Direction);
            }
        }

        private GameObject CreatePreviewObject(StageElement definition)
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

        private GUIStyle GetLabelStyle(StageElement definition)
        {
            Color c;
            if (definition is StageTileDefinition)
                c = Color.white;
            else if (definition is StageObjectDefinition)
                c = Color.yellow;
            else
                c = Color.red;

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = c }
            };

            return style;
        }

        private void MarkLevelDirty()
        {
            EditorUtility.SetDirty(level);
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
