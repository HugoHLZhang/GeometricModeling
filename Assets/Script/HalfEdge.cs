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
        public HalfEdge prevEdge;
        public HalfEdge nextEdge;
        public HalfEdge twinEdge;
        public HalfEdge(int index, Vertex sourceVertex, Face face, HalfEdge twinEdge = null)
        {
            this.index = index;
            this.sourceVertex = sourceVertex;
            this.face = face;
            this.twinEdge = twinEdge;
        }
    }
    public class Vertex
    {
        public int index;
        public Vector3 position;
        public HalfEdge outgoingEdge;
        public Vertex(int index, Vector3 position)
        {
            this.index = index;
            this.position = position;
        }
    }
    public class Face
    {
        public int index;
        public HalfEdge edge;
        public Face(int index)
        {
            this.index = index;
        }

        public List<HalfEdge> GetFaceEdges()
        {
            List<HalfEdge> faceEdges = new List<HalfEdge>();
            HalfEdge halfEdge = edge;

            //Edge CW
            while (!faceEdges.Contains(halfEdge))
            {
                faceEdges.Add(halfEdge);
                halfEdge = halfEdge.nextEdge;
            }
            return faceEdges;
        }
        public List<Vertex> GetFaceVertex()
        {
            List<HalfEdge> faceEdges = GetFaceEdges();
            List<Vertex> faceVertices = new List<Vertex>();
            //Vertice CW
            foreach (var edge in faceEdges)
                faceVertices.Add(edge.sourceVertex);

            return faceVertices;
        }
    }
    public class HalfEdgeMesh
    {
        public List<Vertex> vertices;
        public List<HalfEdge> edges;
        public List<Face> faces;
        public HalfEdgeMesh(Mesh mesh) // Constructeur prenant un mesh Vertex-Face en paramètre //magic happens
        {
            int nEdges = 4;

            vertices = new List<Vertex>();
            edges = new List<HalfEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);


            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            int index = 0;

            Dictionary<ulong, HalfEdge> dicoEdges = new Dictionary<ulong, HalfEdge>();
           

            // Ajout des faces dans la liste Face faces

            for (int i = 0; i < m_quads.Length / 4; i++)
            {
                Face face = new Face(faces.Count);
                faces.Add(face);
                //quad's vertices index
                int[] quad_index = new int[nEdges];
                for (int j = 0; j < 4; j++)
                    quad_index[j] = m_quads[nEdges * i + j];

                // Ajout des edges dans la liste Winged Edge edges

                for (int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nEdges];

                    ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                    HalfEdge halfEdge = null;
                    //Create newEdge if not in dico
                    if (dicoEdges.TryGetValue(key, out halfEdge))

                    {
                        HalfEdge twinEdge = new HalfEdge(edges.Count, vertices[start], face, halfEdge);
                        edges.Add(twinEdge);
                        halfEdge.twinEdge = twinEdge;
                        Debug.Log(halfEdge.twinEdge);

                        if (vertices[start].outgoingEdge == null) vertices[start].outgoingEdge = twinEdge;

                        if (halfEdge.face == null) halfEdge.face = face;
                        if (face.edge == null) face.edge = twinEdge;
                    }
                    else //Update the edge found in dico
                    {
                        
                        HalfEdge newEdge = new HalfEdge(edges.Count, vertices[start], face);
                        edges.Add(newEdge);

                        if (face.edge == null) face.edge = newEdge;
                        if (vertices[start].outgoingEdge == null) vertices[start].outgoingEdge = newEdge;

                        dicoEdges.Add(key, newEdge);
                        
                    }
                }
            }

            index = 0;
            for (int i = 0; i < m_quads.Length / 4; i++)
            {
                //quad's vertices index
                int[] quad_index = new int[nEdges];
                for (int j = 0; j < 4; j++)
                    quad_index[j] = m_quads[nEdges * i + j];

                for (int j = 0; j < quad_index.Length; j++)
                {
                    edges[index].nextEdge = edges.Find(edge => edge.sourceVertex.index == quad_index[(j + 1) % 4] && edge.face == edges[index].face);
                    edges[index].prevEdge = edges.Find(edge => edge.sourceVertex.index == quad_index[(j - 1 + 4) % 4] && edge.face == edges[index].face);

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
                List<HalfEdge> faceEdges = faces[i].GetFaceEdges();
                List<Vertex> faceVertex = faces[i].GetFaceVertex();

                List<int> edgesIndex = new List<int>();
                List<int> vertexIndex = new List<int>();
                //Edge CW
                foreach (var edge in faceEdges)
                    edgesIndex.Add(edge.index);
                //Vertice CW
                foreach (var vertice in faceVertex)
                    vertexIndex.Add(vertice.index);


                strings[i] += faces[i].index + separator
                    + faces[i].edge.index + separator
                    + string.Join(" ", edgesIndex) + separator
                    + string.Join(" ", vertexIndex) + separator + separator;
            }

            // Mise en page du fichier CSV

            str = "Vertex" + separator + separator + separator + separator + "HalfEges" + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "outgoingEdge" + separator + separator +
                "Index" + separator + "sourceVertex" + separator + "Face" + separator + "prevEdge" + separator + "nextEdge" + separator + "twinEdge" + separator + separator +
                "Index" + separator + "Edge" + separator + "CW Edges" + separator + "CW Vertices\n"
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
