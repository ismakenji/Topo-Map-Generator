using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TopoLines))]
public class TopoLinesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Topo Map"))
        {
            var topoLines = (TopoLines)target;
            topoLines.GenerateTopoMap();
            EditorUtility.SetDirty(topoLines);
        }
    }
}
