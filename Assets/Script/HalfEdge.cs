using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace HalfEdge
{
    public class HalfEdge
    {
        public int index;
        public Vertex sourceVertex;
        public Face face;
        public HalfEdge prevEdge { set; get; }
        public HalfEdge nextEdge { set; get; }
        public HalfEdge twinEdge { set; get; }
        public HalfEdge(int index, Vertex sourceVertex, Face face)
        {
            this.index = index;
            this.sourceVertex = sourceVertex;
            this.face = face;
        }
    }
    public class Vertex
    {
        public int index;
        public Vector3 position;
        public HalfEdge outgoingEdge { set; get; }
        public Vertex(int index, Vector3 position)
        {
            this.index = index;
            this.position = position;
        }
    }
    public class Face
    {
        public int index;
        public HalfEdge edge { set; get; }
        public Face(int index)
        {
            this.index = index;
        }
    }
    public class HalfEdgeMesh
    {
        public List<Vertex> vertices;
        public List<HalfEdge> edges;
        public List<Face> faces;
        public HalfEdgeMesh(Mesh mesh)
        { // constructeur prenant un mesh Vertex-Face en paramètre //magic happens
            vertices = new List<Vertex>();
            edges = new List<HalfEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);


            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            int index = 0;
            //Add faces and edges to List Face faces and WingedEdge edges
            for (int i = 0; i < m_quads.Length / 4; i++)
            {
                faces.Add(new Face(i));
                int[] arr_index = new int[]
                {
                    m_quads[4 * i],
                    m_quads[4 * i + 1],
                    m_quads[4 * i + 2],
                    m_quads[4 * i + 3]
                };
                for (int j = 0; j < arr_index.Length; j++)
                {
                    edges.Add(new HalfEdge(index, vertices[arr_index[j]], faces[i]));
                    if (faces[i].edge == null) faces[i].edge = edges[index];
                    if(vertices[arr_index[j]].outgoingEdge == null) vertices[arr_index[j]].outgoingEdge = edges[index];
                    index++;
                }
            }
            index = 0;
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
                    edges[index].twinEdge = edges.Find(edge => edge.sourceVertex.index == arr_index[(j + 1) % 4] && edge.face != edges[index].face );
                    edges[index].nextEdge = edges.Find(edge => edge.sourceVertex.index == arr_index[(j + 1) % 4] && edge.face == edges[index].face);
                    edges[index].prevEdge = edges.Find(edge => edge.sourceVertex.index == arr_index[(j - 1 + 4) % 4] && edge.face == edges[index].face);

                    index++;
                }
            }

            //string p = "";
            //foreach(var x in vertices)
            //{
            //    p += $"V{x.index} : e{x.outgoingEdge.index} \n";
            //}
            //Debug.Log(p);

            //p = "";
            //foreach(var x in edges)
            //{
            //    p += $"e{x.index} : V{x.sourceVertex.index} | F{x.face.index} | Prev e{x.prevEdge.index} | Next e{x.nextEdge.index} | Twin e{x.twinEdge.index}\n";
            //}
            //Debug.Log(p);
            //p = "";
            //foreach(var x in faces)
            //{
            //    p += $"F{x.index} : e{x.edge.index}\n";
            //}
            //Debug.Log(p);
        }
        public Mesh ConvertToFaceVertexMesh()
        {
            Mesh faceVertexMesh = new Mesh();
            // magic happens 
            
            List<Vertex> vertices = this.vertices;
            List<HalfEdge> edges = this.edges;
            List<Face> faces = this.faces;

            Vector3[] m_vertices = new Vector3[vertices.Count];
            int[] m_quads = new int[faces.Count * 4];

            //Vertices

            for (int i = 0; i < vertices.Count; i++)
            {
                m_vertices[i] = vertices[i].position;
            }

            int index = 0;
            //Quads
            for (int i = 0; i < faces.Count; i++)
            {
                m_quads[index] = edges[index++].sourceVertex.index;
                m_quads[index] = edges[index++].sourceVertex.index;
                m_quads[index] = edges[index++].sourceVertex.index;
                m_quads[index] = edges[index++].sourceVertex.index;

            }

            faceVertexMesh.vertices = m_vertices;
            faceVertexMesh.SetIndices(m_quads, MeshTopology.Quads, 0);

            return faceVertexMesh;
        }
        public string ConvertToCSVFormat(string separator = "\t")
        {
            if (this == null) return "";
            Debug.Log("#################      WindgedEdgeMesh ConvertTOCSVFormat     #################");
            string str = "";
            List<Vertex> vertices = this.vertices;
            List<HalfEdge> edges = this.edges;
            List<Face> faces = this.faces;


            List<string> strings = new List<string>();

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 pos = vertices[i].position;
                strings.Add("V" + vertices[i].index + separator
                    + pos.x.ToString("N03") + " "
                    + pos.y.ToString("N03") + " "
                    + pos.z.ToString("N03") + separator
                    + "e" + vertices[i].outgoingEdge.index
                    + separator + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator);

            for (int i = 0; i < edges.Count; i++)
            {
                strings[i] += "e" + edges[i].index + separator
                    + "V" + edges[i].sourceVertex.index + separator
                    + "F" + edges[i].face.index + separator
                    + "e" + edges[i].prevEdge.index + separator
                    + "e" + edges[i].nextEdge.index + separator
                    + "e" + edges[i].twinEdge.index + separator + separator;
            }

            for (int i = 0; i < faces.Count; i++)
            {
                strings[i] += "F" + faces[i].index + separator
                   + "e" + faces[i].edge.index + separator
                    + separator;
            }

            str = "Vertex" + separator + separator + separator + separator + "HalfEges" + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "outgoingEdge" + separator + separator +
                "Index" + separator + "sourceVertex" + separator + "Face" + separator + "prevEdge" + separator + "nextEdge" + separator + "twinEdge" + separator + separator +
                "Index" + separator + "Edge\n"
                + string.Join("\n", strings);
            Debug.Log(str);
            return str;
        }
        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform)
        {
            //magic happens 
            List<Vertex> vertices = this.vertices;
            List<HalfEdge> edges = this.edges;
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
                    Vector3 worldPos = transform.TransformPoint(vertices[i].position);
                    Handles.Label(worldPos, "V" + vertices[i].index, style);
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

                    Vector3 pt1 = transform.TransformPoint(vertices[index1].position);
                    Vector3 pt2 = transform.TransformPoint(vertices[index2].position);
                    Vector3 pt3 = transform.TransformPoint(vertices[index3].position);
                    Vector3 pt4 = transform.TransformPoint(vertices[index4].position);

                    Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, "F" + faces[i].index, style);

                }
            }




            //edges
            if (drawEdges)
            {
                style.normal.textColor = Color.blue;
                foreach (var edge in edges)
                {
                    Vector3 pos = transform.TransformPoint(edge.sourceVertex.position);

                    Handles.Label(pos, "e" + edge.index, style);

                }
            }
        }
    }
}
