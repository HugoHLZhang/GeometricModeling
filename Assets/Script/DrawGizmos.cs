using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawGizmos : MonoBehaviour
{
    MeshFilter m_Mf;
    void Start()
    {
        m_Mf = GetComponent<MeshFilter>();
    }
    private void OnDrawGizmos()
    {

        if (!m_Mf) return;
        Mesh mesh = m_Mf.mesh;  

        Vector3[] vertices = mesh.vertices;
        int[] quads = mesh.GetIndices(0);

        switch (mesh.name)
        {
            case "Terrain_Gfx Instance":
            case "Cage_Gfx Instance":
                Gizmos.color = Color.white;
                break;
            default:
                Gizmos.color = Color.black;
                break;
        }

        for (int i = 0; i < quads.Length / 4; i++)
        {
            int index1 = quads[4 * i];
            int index2 = quads[4 * i + 1];
            int index3 = quads[4 * i + 2];
            int index4 = quads[4 * i + 3];

            Vector3 pt1 = transform.TransformPoint(vertices[index1]);
            Vector3 pt2 = transform.TransformPoint(vertices[index2]);
            Vector3 pt3 = transform.TransformPoint(vertices[index3]);
            Vector3 pt4 = transform.TransformPoint(vertices[index4]);

            Gizmos.DrawLine(pt1, pt2);
            Gizmos.DrawLine(pt2, pt3);
            Gizmos.DrawLine(pt3, pt4);
            Gizmos.DrawLine(pt4, pt1);

        }
    }
}
