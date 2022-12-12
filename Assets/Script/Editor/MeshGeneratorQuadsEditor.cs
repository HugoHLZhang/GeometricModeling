using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshGeneratorQuads))]
public class MeshGeneratorQuadsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        MeshGeneratorQuads generator = (MeshGeneratorQuads)target;


        GUILayout.BeginHorizontal();
        GUILayout.Label("Convert To CSV");
        if (GUILayout.Button("CopyToClipboard", GUILayout.MaxWidth(320)))
        {
            generator.ConvertToCSV();
        }

        GUILayout.EndHorizontal();
    }
}
