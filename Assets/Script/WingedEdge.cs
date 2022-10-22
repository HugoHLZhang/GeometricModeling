using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace WingedEdge
{
    public class WingedEdge
    {
        public int index;
        public Vertex startVertex;
        public Vertex endVertex;
        public Face leftFace { set; get; }
        public Face rightFace;
        public WingedEdge startCWEdge { set; get; }
        public WingedEdge startCCWEdge { set; get; }
        public WingedEdge endCWEdge { set; get; }
        public WingedEdge endCCWEdge { set; get; }
        public WingedEdge(int index, Vertex startVertex, Vertex endVertex, Face rightFace, Face leftFace)
        {
            this.index = index;
            this.startVertex = startVertex;
            this.endVertex = endVertex;
            this.rightFace = rightFace;
            this.leftFace = leftFace;
        }
    }
    public class Vertex
    {
        public int index;
        public Vector3 position;
        public WingedEdge edge { set; get; }
        public Vertex(int index, Vector3 position)
        {
            this.index = index;
            this.position = position;
        }
           
    }
    public class Face
    {
        public int index;
        public WingedEdge edge { set; get; }

        public Face(int index)
        {
            this.index = index;
        }
    }
    public class WingedEdgeMesh
    {
        public List<Vertex> vertices;
        public List<WingedEdge> edges;
        public List<Face> faces;
        public WingedEdgeMesh(Mesh mesh)
        {// constructeur prenant un mesh Vertex-Face en paramètre 
         // magic happens 

            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            Dictionary<ulong, WingedEdge> dico = new Dictionary<ulong, WingedEdge>();
            WingedEdge e;

            Debug.Log(mesh.name);

            //Add mesh.vertices to List Vertex vertices
            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            int index = 0;
            //Add faces and edges to List Face faces and WingedEdge edges
            for (int i = 0; i < m_quads.Length/4; i++) {
                
                faces.Add(new Face(i));
                int[] arr_index = new int[]
                { 
                    m_quads[4 * i],
                    m_quads[4 * i + 1],
                    m_quads[4 * i + 2],
                    m_quads[4 * i + 3] 
                };  

                
                for(int j = 0; j < arr_index.Length; j++)
                {
                    ulong key = (ulong)Mathf.Min(arr_index[j], arr_index[(j + 1) % 4]) + ((ulong)Mathf.Max(arr_index[j], arr_index[(j + 1) % 4]) << 32);
                    if (!dico.TryGetValue(key, out e))
                    {
                        edges.Add(new WingedEdge(index, vertices[arr_index[j]], vertices[arr_index[(j + 1) % 4]], faces[i], null));
                        
                        faces[i].edge = edges[index];
                        
                        vertices[arr_index[j]].edge = edges[index];
                        vertices[arr_index[(j + 1) % 4]].edge = edges[index];
                        
                        dico.Add(key, edges[index]);
                        index++;
                    }
                    else
                    {
                        edges[e.index].leftFace = faces[i];
                        faces[i].edge = edges[e.index];
                    }
                }


            }

            //Complete StartCCW/CWEdges and EndCCW/CWEdges of List edges 
            //string p = "";
            for (int i = 0; i < m_quads.Length / 4; i++)
            {
                int[] arr_index = new int[] 
                { 
                    m_quads[4 * i], 
                    m_quads[4 * i + 1], 
                    m_quads[4 * i + 2], 
                    m_quads[4 * i + 3] 
                };
                for (int j = 0; j < arr_index.Length; j++)
                {
                    ulong key = (ulong)Mathf.Min(arr_index[j], arr_index[(j + 1) % 4]) + ((ulong)Mathf.Max(arr_index[j], arr_index[(j + 1) % 4]) << 32);
                    if (dico.TryGetValue(key, out e))
                    {
                        if (arr_index[j] == e.startVertex.index && arr_index[(j + 1) % 4] == e.endVertex.index)//CW
                        {
                            edges[e.index].startCWEdge = edges.Find(x => x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j] || x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]);
                            edges[e.index].endCCWEdge = edges.Find(x => (x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]) || (x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4]));
                            //p += "e" + e.index + " : SCW e" + edges.Find(x => x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j] || x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]).index + " (index " + arr_index[j] + arr_index[(j - 1 + 4) % 4] + ")"
                            //+ " - ECCW e" + edges.Find(x => (x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]) || (x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4])).index + " (index " + arr_index[(j + 2) % 4] + arr_index[(j + 1) % 4] + ")\n";
                        }

                        if (arr_index[j] == e.endVertex.index && arr_index[(j + 1) % 4] == e.startVertex.index)//CCW
                        {
                            edges[e.index].startCCWEdge = edges.Find(x => x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4] || x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]);
                            edges[e.index].endCWEdge = edges.Find(x => (x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]) || (x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j]));
                            //p += "e" + e.index + " : SCCW e" + edges.Find(x => x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4] || x.startVertex.index == arr_index[(j+2)%4] && x.endVertex.index == arr_index[(j+1)%4]).index + " (index " + arr_index[(j + 2) % 4] + arr_index[(j + 1) % 4] + ")"
                            //+ " - ECW e" + edges.Find(x => (x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]) || (x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[ j ])).index + " (index " + arr_index[j] + arr_index[(j - 1 + 4) % 4] + ")\n";
                        }
                        
                        if (e.startCWEdge == null)  edges[e.index].startCWEdge  = edges[e.index].startCCWEdge;
                        if (e.startCCWEdge == null) edges[e.index].startCCWEdge = edges[e.index].startCWEdge;
                        if (e.endCWEdge == null)    edges[e.index].endCWEdge    = edges[e.index].endCCWEdge;
                        if (e.endCCWEdge == null)   edges[e.index].endCCWEdge   = edges[e.index].endCWEdge;
                    }
                }
            }
            /*
            Debug.Log(p);
                
            p = "Dictionary<ulong, WingedEdge> dico : \n";
            foreach ( var dic in dico)
            {
                p += "key " + dic.Key.ToString() + " => value : e" + dic.Value.index.ToString() + "\n";
            }
            Debug.Log(p);
            p = "Faces - Vertex : \n";
            index = 0;
            for (int i = 0; i < m_quads.Length; i+=4)
            {
                p += "F"+index++ + " : " + "V"+m_quads[i] + " - " + "V" + m_quads[i + 1] + " - " + "V" + m_quads[i+2] + " - " + "V" + m_quads[i+3] + "\n";
            }
            Debug.Log(p);
            //vertices
            p = "Vertex - edges : \n";
            foreach (var x in vertices)
            {
                p += "V"+x.index.ToString() + ": " + x.position.ToString() + " | e" + x.edge.index +  " \n";
            }
            Debug.Log(p);
            //faces
            p = "Faces - edges : \n";
            foreach (var x in faces)
            {
                p += x.index.ToString() + ": F" + x.index.ToString() + " - e" + x.edge.index + " \n";
            }
            Debug.Log(p);

            //wingedEdge

            p = "WingedEdges : \n";
            foreach (var x in edges)
            {
                p += $"e{x.index} : V{x.startVertex.index} - V{x.endVertex.index}| {(x.leftFace == null ? "NoLeftFace" : $"F{x.leftFace.index}")} | {(x.rightFace == null ? "NoRightFace" : $"F{x.rightFace.index}")} | SCCW : e{x.startCCWEdge.index} - SCW : e{x.startCWEdge.index} - ECW : e{x.endCWEdge.index} - ECCW : e{x.endCCWEdge.index}\n";
            }
            Debug.Log(p);
            */




        }
        public Mesh ConvertToFaceVertexMesh()
        {
            Mesh faceVertexMesh = new Mesh();
            // magic happens 

            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            Vector3[] m_vertices = new Vector3[vertices.Count];
            int[] m_quads = new int[faces.Count*4];

            //Vertices
         
            for (int i = 0; i < vertices.Count; i++)
            {
                m_vertices[i] = vertices[i].position;
            }

            int index = 0;
            //Quads
            for (int i = 0; i < faces.Count; i++)
            {
                WingedEdge e = faces[i].edge;

                if(e.rightFace.index == i)
                {
                    m_quads[index++] = e.startVertex.index;
                    m_quads[index++] = e.endVertex.index;
                    if(e.endCCWEdge.rightFace.index == i) 
                        m_quads[index++] = e.endCCWEdge.endVertex.index;
                    if (e.endCCWEdge.leftFace != null && e.endCCWEdge.leftFace.index == i )
                        m_quads[index++] = e.endCCWEdge.startVertex.index;
                    if (e.startCWEdge.rightFace.index == i) 
                        m_quads[index++] = e.startCWEdge.startVertex.index;
                    if (e.startCWEdge.leftFace != null && e.startCWEdge.leftFace.index == i )
                        m_quads[index++] = e.startCWEdge.endVertex.index;
                }
                if ( e.leftFace != null && e.leftFace.index == i)
                {
                    m_quads[index++] = e.endVertex.index;
                    m_quads[index++] = e.startVertex.index;
                    if(e.startCCWEdge.rightFace.index == i)
                        m_quads[index++] = e.startCCWEdge.endVertex.index;
                    if(e.startCCWEdge.leftFace != null && e.startCCWEdge.leftFace.index == i )
                        m_quads[index++] = e.startCCWEdge.startVertex.index;
                    if (e.endCWEdge.rightFace.index == i)
                        m_quads[index++] = e.endCWEdge.startVertex.index;
                    if(e.endCWEdge.leftFace != null && e.endCWEdge.leftFace.index == i )
                        m_quads[index++] = e.endCWEdge.endVertex.index;
                }
               
            }

            faceVertexMesh.vertices = m_vertices;
            faceVertexMesh.SetIndices(m_quads, MeshTopology.Quads, 0);

            return faceVertexMesh;
        }
        public string ConvertToCSVFormat(string separator = "\t")
        {

            string str = "";
            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;


            List<string> strings = new List<string>();

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 pos = vertices[i].position;
                strings.Add("V"+vertices[i].index + separator
                    + pos.x.ToString("N03") + " "
                    + pos.y.ToString("N03") + " "
                    + pos.z.ToString("N03") + separator 
                    + "e"+vertices[i].edge.index
                    + separator + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator);

            for (int i = 0; i < edges.Count; i++)
            {
                strings[i] += "e" + edges[i].index + separator
                    + "V" + edges[i].startVertex.index + separator
                    + "V" + edges[i].endVertex.index + separator
                    + $"{(edges[i].leftFace != null ? $"F{edges[i].leftFace.index}" : "")}" + separator
                    + $"{(edges[i].rightFace != null ? $"F{edges[i].rightFace.index}" : "")}" + separator
                    + "e" + edges[i].startCCWEdge.index + separator
                    + "e" + edges[i].startCWEdge.index + separator
                    + "e" + edges[i].endCWEdge.index + separator
                    + "e" + edges[i].endCCWEdge.index + separator
                     + separator;
            }

            for(int i = 0; i < faces.Count; i++)
            {
                strings[i] += "F" + faces[i].index + separator
                   + "e" + faces[i].edge.index + separator
                    + separator;
            }

            str = "Vertex" + separator + separator + separator + separator + "WingedEdges" + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "Edge" + separator + separator +
                "Index" + separator + "Start Vertex" + separator + "End Vertex" + separator + "Left Face" + separator + "Right Face" + separator + "Start CCW Edge" + separator + "Start CW Edge" + separator + "End CW Edge" + separator + "End CCW Edge" + separator + separator +
                "Index" + separator + "Edge\n"
                + string.Join("\n", strings);
            return str;
        }
        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces)
        {
            //magic happens 

            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            Mesh mesh = this.ConvertToFaceVertexMesh();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            Gizmos.color = Color.black;
            GUIStyle style = new GUIStyle();
            style.fontSize = 12;

            //vertices
            
            if (drawVertices)
            {
                style.normal.textColor = Color.red;
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 worldPos = vertices[i].position;
                    Handles.Label(worldPos, "V"+vertices[i].index, style);
                }
            }

            //faces
            if (drawFaces)
            {
                style.normal.textColor = Color.magenta;
                for (int i = 0; i < faces.Count; i++)
                {
                    int index1 = m_quads[4 * i];
                    int index2 = m_quads[4 * i + 1];
                    int index3 = m_quads[4 * i + 2];
                    int index4 = m_quads[4 * i + 3];

                    Vector3 pt1 = vertices[index1].position;
                    Vector3 pt2 = vertices[index2].position;
                    Vector3 pt3 = vertices[index3].position;
                    Vector3 pt4 = vertices[index4].position;

                    Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, "F" + faces[i].index, style);

                }
            }




            //edges
            if (drawEdges)
            {
                style.normal.textColor = Color.blue;
                foreach (var edge in edges)
                {
                    Vector3 start = edge.startVertex.position;
                    Vector3 end = edge.endVertex.position;
                    Vector3 pos = Vector3.Lerp(start, end, 0.5f);

                    Handles.Label(pos, "e" + edge.index, style);

                }
            }
           
        }
    }
}
