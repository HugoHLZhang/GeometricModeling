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

        public List<HalfEdge> GetAdjacentEdges()
        {
            List<HalfEdge> adjacentEdges = new List<HalfEdge>();
            HalfEdge halfEdge = outgoingEdge;
            adjacentEdges.Add(halfEdge);
            //adjacentEdges.Add(halfEdge);
            //while (halfEdge.twinEdge != null || halfEdge != outgoingEdge){
            //    halfEdge = halfEdge.nextEdge.twinEdge != null ? halfEdge.nextEdge.twinEdge : halfEdge.nextEdge;
            //    adjacentEdges.Add(halfEdge);

            //}



            //tant que 

            return adjacentEdges;
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
            do
            {
                faceEdges.Add(halfEdge);
                halfEdge = halfEdge.nextEdge;
            } while (halfEdge != edge);
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
            HalfEdge halfEdge;

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
                HalfEdge prevEdge = null;
                for (int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nEdges];

                    ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                    HalfEdge newEdge = null;
                    //Create newEdge if not in dico
                    if (dicoEdges.TryGetValue(key, out halfEdge))
                    {
                        newEdge = new HalfEdge(edges.Count, vertices[start], face, halfEdge);
                        edges.Add(newEdge);
                        halfEdge.twinEdge = newEdge;


                    }
                    else //Update the edge found in dico
                    {

                        newEdge = new HalfEdge(edges.Count, vertices[start], face);
                        edges.Add(newEdge);

                        dicoEdges.Add(key, newEdge);

                    }
                    if (face.edge == null) face.edge = newEdge;
                    if (vertices[start].outgoingEdge == null) vertices[start].outgoingEdge = newEdge;
                    if (prevEdge != null)
                    {
                        newEdge.prevEdge = prevEdge;
                        prevEdge.nextEdge = newEdge;
                    }
                    if (j == 3)
                    {
                        newEdge.nextEdge = edges[edges.Count - 4];
                        edges[edges.Count - 4].prevEdge = newEdge;
                    }

                    prevEdge = newEdge;

                }
            }

        }

        public Mesh ConvertToFaceVertexMesh() // Conversion vers un mesh FaceVertex
        {
            // Attributs 

            Mesh faceVertexMesh = new Mesh();


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
                List<Vertex> faceVertex = faces[i].GetFaceVertex();
                for (int j = 0; j < faceVertex.Count; j++)
                    m_quads[index++] = faceVertex[j].index;
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

                List<HalfEdge> adjacentEdges = vertices[i].GetAdjacentEdges();
                List<int> edgesIndex = new List<int>();
                for (int j = 0; j < adjacentEdges.Count; j++)
                    edgesIndex.Add(adjacentEdges[j].index);
                Vector3 pos = vertices[i].position;
                strings.Add(vertices[i].index + separator
                    + pos.x.ToString("N03") + " " + pos.y.ToString("N03") + " " + pos.z.ToString("N03") + separator
                    + vertices[i].outgoingEdge.index + separator
                    + string.Join(" ", edgesIndex) + separator
                    + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator + separator);

            // Récupération des edges dans le fichier CSV

            for (int i = 0; i < edges.Count; i++)
            {
                strings[i] += edges[i].index + separator
                    + edges[i].sourceVertex.index + separator
                    + edges[i].face.index + separator
                    + edges[i].prevEdge.index + separator
                    + edges[i].nextEdge.index + separator
                    + $"{(edges[i].twinEdge != null ? $"{edges[i].twinEdge.index}" : "NULL")}" + separator + separator;
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

            str = "Vertex" + separator + separator + separator + separator + separator + "HalfEges" + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "outgoingEdge" + separator + "adjacentEdge" + separator + separator +
                "Index" + separator + "sourceVertex" + separator + "Face" + separator + "prevEdge" + separator + "nextEdge" + separator + "twinEdge" + separator + separator +
                "Index" + separator + "Edge" + separator + "CW Face Edges" + separator + "CW Face Vertices\n"
                + string.Join("\n", strings);
            Debug.Log(str);
            return str;
        }

        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform) // Dessins des Gizmos
        {

            // Attributs

            Mesh mesh = this.ConvertToFaceVertexMesh();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            Gizmos.color = Color.black;
            GUIStyle style = new GUIStyle();
            style.fontSize = 12;

            // Affichage des vertices


            style.normal.textColor = Color.red;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 worldPos = transform.TransformPoint(vertices[i].position);
                if (drawVertices)
                {
                    Handles.Label(worldPos, "V" + vertices[i].index, style);
                }
            }


            // Affichage des faces


            for (int i = 0; i < faces.Count; i++)
            {
                //int index1 = m_quads[4 * i];
                //int index2 = m_quads[4 * i + 1];
                //int index3 = m_quads[4 * i + 2];
                //int index4 = m_quads[4 * i + 3];

                //Vector3 pt1 = transform.TransformPoint(vertices[index1].position);
                //Vector3 pt2 = transform.TransformPoint(vertices[index2].position);
                //Vector3 pt3 = transform.TransformPoint(vertices[index3].position);
                //Vector3 pt4 = transform.TransformPoint(vertices[index4].position);
                style.normal.textColor = Color.magenta;
                List<Vertex> faceVertex = faces[i].GetFaceVertex();
                Vector3 C = new Vector3();
                for (int j = 0; j < faceVertex.Count; j++)
                {
                    Gizmos.DrawLine(transform.TransformPoint(faceVertex[j].position), transform.TransformPoint(faceVertex[(j + 1) % faceVertex.Count].position));
                    C += faceVertex[j].position;
                }
                Handles.Label(transform.TransformPoint(C / 4f), "F" + faces[i].index, style);
                List<HalfEdge> faceEdges = faces[i].GetFaceEdges();

                for (int j = 0; j < faceEdges.Count; j++)
                {
                    style.normal.textColor = Color.blue;
                    Vector3 start = faceEdges[j].sourceVertex.position;
                    Vector3 end = faceEdges[j].nextEdge.sourceVertex.position;
                    Vector3 pos = Vector3.Lerp(Vector3.Lerp(start, end, 0.5f), C / 4f, 0.1f);

                    Handles.Label(transform.TransformPoint(pos), "e" + faceEdges[j].index, style);
                }

            }


            // Affichage des edges


            //style.normal.textColor = Color.blue;
            //foreach (var edge in edges)
            //{
            //    Vector3 center = facePoints[edge.face.index];
            //    Vector3 start = edge.sourceVertex.position;
            //    Vector3 end = edge.nextEdge.sourceVertex.position;
            //    Vector3 pos = Vector3.Lerp(Vector3.Lerp(start, end, 0.5f), center, 0.2f);
            //    if (drawEdges)
            //    {
            //        Handles.Label(transform.TransformPoint(pos), "e" + edge.index, style);
            //    }
            //}
        }
    }
}
