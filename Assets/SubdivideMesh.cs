using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WingedEdge;
public class SubdivideMesh : MonoBehaviour
{
    MeshFilter m_Mf;
    WingedEdgeMesh m_WingedEdgeMesh;
    // Start is called before the first frame update
    void Start()
    {
        m_Mf = GetComponent<MeshFilter>();
        Debug.Log(m_Mf.mesh.vertices[0]);
        //m_WingedEdgeMesh = new WingedEdgeMesh(m_Mf.mesh);
        //GUIUtility.systemCopyBuffer = m_WingedEdgeMesh.ConvertToCSVFormat();
        //m_WingedEdgeMesh.SubdivideCatmullClark();
        //m_Mf.mesh = m_WingedEdgeMesh.ConvertToFaceVertexMesh();
    }

}
