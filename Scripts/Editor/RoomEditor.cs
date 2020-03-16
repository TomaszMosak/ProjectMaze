using UnityEngine;
using UnityEditor;

namespace ProjectMaze
{
    [CustomEditor(typeof(RoomPrefab))]
    public class PlazaPrefabEditor : Editor
    {
        public override void OnInspectorGUI() {
            serializedObject.Update();

            RoomPrefab roomPrefab = (RoomPrefab)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("length"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fixedPosition"));
            if (roomPrefab.fixedPosition) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("xPosition"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("zPosition"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}