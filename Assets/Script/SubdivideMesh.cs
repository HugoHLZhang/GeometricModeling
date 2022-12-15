using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using WingedEdge;
using HalfEdge;

public class SubdivideMesh : MonoBehaviour
{
    public enum MeshType { VertexFaceMesh, WingedEdgeMesh, HalfEdgeMesh };
    MeshFilter m_Mf;
    [Header("Mesh")]
    [SerializeField] public MeshType mesh_type;
    [Header("Subdivide every x seconds")]
    [Range(1, 4)]
    [SerializeField] public int seconds = 2;
    WingedEdgeMesh m_WingedEdgeMesh;
    HalfEdgeMesh m_HalfEdgeMesh;
    Mesh m_Mesh;
    [Header("Gizmos & CSV")]
    [SerializeField] bool m_DisplayMeshWires = true;
    [SerializeField] bool m_DisplayMeshInfos = false;
    [SerializeField] bool m_DisplayMeshEdges = false;
    [SerializeField] bool m_DisplayMeshVertices = false;
    [SerializeField] bool m_DisplayMeshFaces = false;
    // Start is called before the first frame update
    void Start()
    {
        m_Mf = GetComponent<MeshFilter>();
        m_Mesh = m_Mf.mesh;

        switch (mesh_type)
        {
            case MeshType.WingedEdgeMesh:
                m_WingedEdgeMesh = new WingedEdgeMesh(m_Mesh);
                m_Mf.mesh = m_WingedEdgeMesh.ConvertToFaceVertexMesh();
                StartCoroutine(WingedEdgeSubdivide(seconds));
                break;
            case MeshType.HalfEdgeMesh:
                m_HalfEdgeMesh = new HalfEdgeMesh(m_Mesh);
                m_Mf.mesh = m_HalfEdgeMesh.ConvertToFaceVertexMesh();
                StartCoroutine(HalfEdgeSubdivide(seconds));
                break;
        }
        
    }
    IEnumerator WingedEdgeSubdivide(int seconds)
    {
        while (true)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(seconds);
                m_WingedEdgeMesh = new WingedEdgeMesh(m_Mf.mesh);
                m_WingedEdgeMesh.SubdivideCatmullClark();
                m_Mf.mesh = m_WingedEdgeMesh.ConvertToFaceVertexMesh();
            }
            yield return new WaitForSeconds(seconds);
            m_WingedEdgeMesh = new WingedEdgeMesh(m_Mesh);
            m_Mf.mesh = m_WingedEdgeMesh.ConvertToFaceVertexMesh();
        }
    }
    IEnumerator HalfEdgeSubdivide(int seconds)
    {
        while (true)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(seconds);
                m_HalfEdgeMesh = new HalfEdgeMesh(m_Mf.mesh);
                m_HalfEdgeMesh.SubdivideCatmullClark();
                m_Mf.mesh = m_HalfEdgeMesh.ConvertToFaceVertexMesh();
            }
            yield return new WaitForSeconds(seconds);
            m_HalfEdgeMesh = new HalfEdgeMesh(m_Mesh);
            m_Mf.mesh = m_HalfEdgeMesh.ConvertToFaceVertexMesh();
        }
    }
    public string ConvertToCSV(string separator = "\t")
    {
        if (!(m_Mf && m_Mf.mesh)) return "";
        string str = "";
        switch (mesh_type)
        {
            case MeshType.WingedEdgeMesh:
                str = m_WingedEdgeMesh.ConvertToCSVFormat();
                break;
            case MeshType.HalfEdgeMesh:
                str = m_HalfEdgeMesh.ConvertToCSVFormat();
                break;
            case MeshType.VertexFaceMesh:
                Vector3[] vertices = m_Mf.mesh.vertices;
                int[] quads = m_Mf.mesh.GetIndices(0);

                List<string> strings = new List<string>();

                for (int i = 0; i < vertices.Length; i++)
                    strings.Add(i.ToString() + separator + vertices[i].x.ToString("N03") + " " + vertices[i].y.ToString("N03") + " " + vertices[i].z.ToString("N03") + separator + separator);

                for (int i = vertices.Length; i < quads.Length / 4; i++)
                    strings.Add(separator + separator + separator);

                for (int i = 0; i < quads.Length / 4; i++)
                    strings[i] += i.ToString() + separator + quads[4 * i + 0].ToString() + "," + quads[4 * i + 1].ToString() + "," + quads[4 * i + 2].ToString() + "," + quads[4 * i + 3].ToString();

                str = "Vertices" + separator + separator + separator + "Faces\n" + "Index" + separator + "Position" + separator + separator + "Index" + separator + "Indices des vertices\n" + string.Join("\n", strings);

                break;
        }
        Debug.Log(str);
        return str;
    }
    private void OnDrawGizmos()
    {

        if (!(m_Mf && m_Mf.mesh)) return;
        Mesh mesh = m_Mf.mesh;

        //WingedEdgeDrawGizmos
        if (mesh_type == MeshType.WingedEdgeMesh)
        {
            WingedEdgeMesh wingedEdgeMesh = m_WingedEdgeMesh;
            wingedEdgeMesh.DrawGizmos(m_DisplayMeshVertices, m_DisplayMeshEdges, m_DisplayMeshFaces, transform);
        }
        //HalfEdgeDrawGizmos
        if (mesh_type == MeshType.HalfEdgeMesh)
        {
            HalfEdgeMesh halfEdgeMesh = m_HalfEdgeMesh;
            halfEdgeMesh.DrawGizmos(m_DisplayMeshVertices, m_DisplayMeshEdges, m_DisplayMeshFaces, transform);
        }

        Vector3[] vertices = mesh.vertices;
        int[] quads = mesh.GetIndices(0);

        Gizmos.color = Color.black;
        GUIStyle style = new GUIStyle();
        style.fontSize = 12;

        if (m_DisplayMeshInfos)
        {
            style.normal.textColor = Color.red;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(vertices[i]);
                Handles.Label(worldPos, i.ToString(), style);
            }
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

            if (m_DisplayMeshWires)
            {
                Gizmos.DrawLine(pt1, pt2);
                Gizmos.DrawLine(pt2, pt3);
                Gizmos.DrawLine(pt3, pt4);
                Gizmos.DrawLine(pt4, pt1);

            }
            if (m_DisplayMeshInfos)
            {
                style.normal.textColor = Color.green;
                string str = string.Format("{0}:{1},{2},{3},{4}", i, index1, index2, index3, index4);
                Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, str, style);
            }

        }


    }
}
