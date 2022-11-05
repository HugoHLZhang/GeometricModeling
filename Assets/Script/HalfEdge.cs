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
        public HalfEdgeMesh(Mesh mesh) // Constructeur prenant un mesh Vertex-Face en paramètre //magic happens
        {
            vertices = new List<Vertex>();
            edges = new List<HalfEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);


            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            int index = 0;

            // Ajout des faces dans la liste Face faces

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

                // Ajout des edges dans la liste Winged Edge edges

                for (int j = 0; j < arr_index.Length; j++)
                {
                    edges.Add(new HalfEdge(index, vertices[arr_index[j]], faces[i]));
                    if (faces[i].edge == null) faces[i].edge = edges[index];
                    if (vertices[arr_index[j]].outgoingEdge == null) vertices[arr_index[j]].outgoingEdge = edges[index];
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
                    edges[index].twinEdge = edges.Find(edge => edge.sourceVertex.index == arr_index[(j + 1) % 4] && edge.face != edges[index].face);
                    edges[index].nextEdge = edges.Find(edge => edge.sourceVertex.index == arr_index[(j + 1) % 4] && edge.face == edges[index].face);
                    edges[index].prevEdge = edges.Find(edge => edge.sourceVertex.index == arr_index[(j - 1 + 4) % 4] && edge.face == edges[index].face);

                    index++;
                }
            }

            //string p = "";
            //foreach (var x in vertices)
            //{
            //    p += $"V{x.index} : e{x.outgoingEdge.index} \n";
            //}
            //Debug.Log(p);

            //p = "";
            //foreach (var x in faces)
            //{
            //    p += $"F{x.index} : e{x.edge.index}\n";
            //}
            //Debug.Log(p);
            //p = "";
            //foreach (var x in edges)
            //{
            //    p += $"e{x.index} : V{x.sourceVertex.index} | F{x.face.index} " + $"| Prev e{(x.prevEdge!=null?$"{x.prevEdge.index}":"NULL")} | Next e{(x.nextEdge!=null?$"{x.nextEdge.index}":"NULL")} | Twin e{(x.twinEdge!=null? $"{x.twinEdge.index}": "NULL")} \n";
            //}
            //Debug.Log(p);
        }

        public Mesh ConvertToFaceVertexMesh() // Conversion vers un mesh FaceVertex
        {
            // Attributs 

            Mesh faceVertexMesh = new Mesh();
            
            List<Vertex> vertices = this.vertices;
            List<HalfEdge> edges = this.edges;
            List<Face> faces = this.faces;

            Vector3[] m_vertices = new Vector3[vertices.Count];
            int[] m_quads = new int[faces.Count * 4];

            //Conversion des vertices

            for (int i = 0; i < vertices.Count; i++)
            {
                m_vertices[i] = vertices[i].position;
            }

            int index = 0;

            // Conversion des quads

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

        public string ConvertToCSVFormat(string separator = "\t") // Conversion vers format CSV
        {
            if (this == null) return "";
            Debug.Log("#################      HalfEdgeMesh ConvertTOCSVFormat     #################");

            // Attributs

            string str = "";

            List<string> strings = new List<string>();

            // Récupération des vertices dans le fichier CSV

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 pos = vertices[i].position;
                strings.Add(vertices[i].index + separator
                    + pos.x.ToString("N03") + " "
                    + pos.y.ToString("N03") + " "
                    + pos.z.ToString("N03") + separator
                    + vertices[i].outgoingEdge.index
                    + separator + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator);

            // Récupération des edges dans le fichier CSV

            for (int i = 0; i < edges.Count; i++)
            {
                strings[i] += edges[i].index + separator
                    + edges[i].sourceVertex.index + separator
                    + edges[i].face.index + separator
                    + edges[i].prevEdge.index + separator
                    + edges[i].nextEdge.index + separator
                    + $"{( edges[i].twinEdge != null ? $"{edges[i].twinEdge.index}" : "NULL" )}" + separator + separator;
            }

            // Récupération des faces dans le fichier CSV

            for (int i = 0; i < faces.Count; i++)
            {
                strings[i] += faces[i].index + separator
                   + faces[i].edge.index + separator
                   + separator;
            }

            // Mise en page du fichier CSV

            str = "Vertex" + separator + separator + separator + separator + "HalfEges" + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "outgoingEdge" + separator + separator +
                "Index" + separator + "sourceVertex" + separator + "Face" + separator + "prevEdge" + separator + "nextEdge" + separator + "twinEdge" + separator + separator +
                "Index" + separator + "Edge\n"
                + string.Join("\n", strings);
            Debug.Log(str);
            return str;
        }

        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform) // Dessins des Gizmos
        {

            // Attributs

            List<Vertex> vertices = this.vertices;
            List<HalfEdge> edges = this.edges;
            List<Face> faces = this.faces;

            List<Vector3> facePoints = new List<Vector3>();

            Mesh mesh = this.ConvertToFaceVertexMesh();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            Gizmos.color = Color.black;
            GUIStyle style = new GUIStyle();
            style.fontSize = 12;

            // Affichage des vertices

            if (drawVertices)
            {
                style.normal.textColor = Color.red;
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 worldPos = transform.TransformPoint(vertices[i].position);
                    Handles.Label(worldPos, "V" + vertices[i].index, style);
                }
            }

            // Affichage des faces

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
                    facePoints.Add((pt1 + pt2 + pt3 + pt4) / 4.0f);
                }
            }

            // Affichage des edges

            if (drawEdges)
            {
                style.normal.textColor = Color.blue;
                foreach (var edge in edges)
                {
                    Vector3 center = facePoints[edge.face.index];
                    Vector3 start = edge.sourceVertex.position;
                    Vector3 end = edge.nextEdge.sourceVertex.position;
                    Vector3 pos = Vector3.Lerp(Vector3.Lerp(start, end, 0.5f), center, 0.2f);
                    Handles.Label(transform.TransformPoint(pos), "e" + edge.index, style);
                }
            }
        }
    }
}
