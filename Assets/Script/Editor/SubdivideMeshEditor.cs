using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SubdivideMesh))]
public class SubdivideMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SubdivideMesh mesh = (SubdivideMesh)target;


        GUILayout.BeginHorizontal();
        GUILayout.Label("Convert To CSV");
        if (GUILayout.Button("CopyToClipboard", GUILayout.MaxWidth(320)))
        {
            mesh.ConvertToCSV();
        }

        GUILayout.EndHorizontal();
    }
}