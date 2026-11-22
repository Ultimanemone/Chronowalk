using ChronowalkGame.Game.Stage;
using UnityEditor;
using UnityEngine;

namespace ChronowalkGame.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Open in Level Editor"))
                LevelEditorWindow.OpenForLevel((LevelData)target);
        }
    }
}
