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
        public HalfEdge(int index, Vertex sourceVertex, Face face, HalfEdge prevEdge = null, HalfEdge nextEdge = null, HalfEdge twinEdge = null)
        {
            this.index = index;
            this.sourceVertex = sourceVertex;
            this.face = face;
            this.prevEdge = prevEdge;
            this.nextEdge = nextEdge;
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
            //tant que qu'on atteint pas une bordure.
            adjacentEdges.Add(halfEdge);
            //CW
            do
            {
                halfEdge = halfEdge.prevEdge.twinEdge != null ? halfEdge.prevEdge.twinEdge : halfEdge.prevEdge ;
                if (halfEdge == outgoingEdge) return adjacentEdges;
                adjacentEdges.Add(halfEdge);

            } while (halfEdge.twinEdge != null);

            //CCW
            if(outgoingEdge.twinEdge != null)
            {
                halfEdge = outgoingEdge.twinEdge.nextEdge;
                adjacentEdges.Add(halfEdge);

                while (halfEdge.twinEdge != null){
                    halfEdge = halfEdge.twinEdge.nextEdge;
                    adjacentEdges.Add(halfEdge);
                }
            }

           

            return adjacentEdges;
        }
        public List<Face> GetAdjacentFaces()
        {
            List<HalfEdge> adjacentEdges = GetAdjacentEdges();
            List<Face> adjacentFaces = new List<Face>();

            for (int i = 0; i < adjacentEdges.Count; i++)
                if (adjacentEdges[i].sourceVertex == this) adjacentFaces.Add(adjacentEdges[i].face);
            
            
            return adjacentFaces;
        }
        public List<HalfEdge> GetBorderEdges()
        {
            List<HalfEdge> borderEdges = new List<HalfEdge>();
            List<HalfEdge> adjacentEdges = GetAdjacentEdges();

            for (int i = 0; i < adjacentEdges.Count; i++)
                if (adjacentEdges[i].twinEdge == null) borderEdges.Add(adjacentEdges[i]);

            return borderEdges;
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
        public HalfEdgeMesh(Mesh mesh) // Constructeur prenant un mesh Vertex-Face en param�tre //magic happens
        {
            int nEdges = 4;

            vertices = new List<Vertex>();
            edges = new List<HalfEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);


            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            Dictionary<ulong, HalfEdge> dicoEdges = new Dictionary<ulong, HalfEdge>();
            HalfEdge halfEdge;

            for (int i = 0; i < m_quads.Length / 4; i++)
            {
                Face face = new Face(faces.Count);
                faces.Add(face);
                //quad's vertices index
                int[] quad_index = new int[nEdges];
                for (int j = 0; j < 4; j++)
                    quad_index[j] = m_quads[nEdges * i + j];

                //HalfEdge prevEdge = null;
                for (int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nEdges];

                    ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                    HalfEdge newEdge;
                    //Create newEdge if not in dico
                    if (dicoEdges.TryGetValue(key, out halfEdge))
                    {
                        newEdge = new HalfEdge(edges.Count, vertices[start], face, null, null, halfEdge);
                        edges.Add(newEdge);
                        halfEdge.twinEdge = newEdge;
                    }
                    //Create twinEdge
                    else
                    {
                        newEdge = new HalfEdge(edges.Count, vertices[start], face);
                        edges.Add(newEdge);

                        dicoEdges.Add(key, newEdge);
                    }


                    if (face.edge == null) face.edge = newEdge;
                    if (vertices[start].outgoingEdge == null) vertices[start].outgoingEdge = newEdge;

                    if(j != 0)
                    {
                        newEdge.prevEdge = edges[edges.Count - 2];
                        edges[edges.Count - 2].nextEdge = newEdge;
                    }

                    if(j == 3)
                    {
                        newEdge.nextEdge = edges[edges.Count - 4];
                        edges[edges.Count - 4].prevEdge = newEdge;
                    }
                }
            }

        }
        public Mesh ConvertToFaceVertexMesh() // Conversion vers un FaceVertexMesh
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
                {
                    m_quads[index++] = faceVertex[j].index;
                }
            }

            faceVertexMesh.vertices = m_vertices;
            faceVertexMesh.SetIndices(m_quads, MeshTopology.Quads, 0);

            return faceVertexMesh;
        }
        public void SubdivideCatmullClark()
        {
            List<Vector3> facePoints;
            List<Vector3> edgePoints;
            List<Vector3> vertexPoints;

            CatmullClarkCreateNewPoints(out facePoints, out edgePoints, out vertexPoints);

            for (int i = 0; i < edgePoints.Count; i++)
                SplitEdge(edges[i], edgePoints[i]);

            for (int i = 0; i < facePoints.Count; i++)
                SplitFace(faces[i], facePoints[i]);

            for (int i = 0; i < vertexPoints.Count; i++)
                vertices[i].position = vertexPoints[i];
        }
        public void CatmullClarkCreateNewPoints(out List<Vector3> facePoints, out List<Vector3> edgePoints, out List<Vector3> vertexPoints)
        {
            facePoints = new List<Vector3>();
            edgePoints = new List<Vector3>();
            vertexPoints = new List<Vector3>();
            List<Vector3> midPoints = new List<Vector3>();

            //facePoints
            for (int i = 0; i < faces.Count; i++)
            {
                List<Vertex> faceVertex = faces[i].GetFaceVertex();
                Vector3 C = new Vector3();

                for (int j = 0; j < faceVertex.Count; j++)
                    C += faceVertex[j].position;

                facePoints.Add(C / 4f);
            }

            //Mid Points and Edge Points
            for (int i = 0; i < edges.Count; i++)
            {
                midPoints.Add((edges[i].sourceVertex.position + edges[i].nextEdge.sourceVertex.position) / 2f);
                edgePoints.Add(edges[i].twinEdge != null ? (edges[i].sourceVertex.position + edges[i].twinEdge.sourceVertex.position + facePoints[edges[i].face.index] + facePoints[edges[i].twinEdge.face.index]) / 4f : midPoints[i]);
            }

            //Vertex Points
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 Q = new Vector3();
                Vector3 R = new Vector3();

                List<HalfEdge> adjacentEdges = vertices[i].GetAdjacentEdges();
                List<Face> adjacentFaces = vertices[i].GetAdjacentFaces();

                //pour toutes les vertices possédant autant d’edges incidentes que de faces adjacentes.
                if (adjacentEdges.Count == adjacentFaces.Count)
                {
                    float n = adjacentFaces.Count;
                    for (int j = 0; j < adjacentEdges.Count; j++)
                    {
                        Q += facePoints[adjacentFaces[j].index];
                        R += midPoints[adjacentEdges[j].index];
                    }
                    Q = Q / n;
                    R = R / n;

                    vertexPoints.Add((Q / n) + (2f * R / n) + ((n - 3f) * vertices[i].position / n));
                }
                //pour les vertices en bordure
                else
                {
                    List<HalfEdge> borderEdges = vertices[i].GetBorderEdges();
                    Vector3 tot_m = new Vector3();

                    for (int j = 0; j < borderEdges.Count; j++)
                        tot_m += midPoints[borderEdges[j].index];

                    vertexPoints.Add((tot_m + vertices[i].position) / 3f);
                }
            }
        }
        public void SplitEdge(HalfEdge edge, Vector3 splittingPoint)
        {
            Vertex newVertex;

            //if newVertex already created
            if (edge.twinEdge != null && edge.twinEdge.nextEdge.sourceVertex.position == splittingPoint)
            {
                newVertex = edge.twinEdge.nextEdge.sourceVertex;
            }
            else
            {
                newVertex = new Vertex(vertices.Count, splittingPoint);
                vertices.Add(newVertex);
            }

            HalfEdge newEdge = new HalfEdge(edges.Count, newVertex, edge.face, edge, edge.nextEdge, edge.twinEdge);
            edges.Add(newEdge);

            edge.nextEdge = newEdge;
            newEdge.nextEdge.prevEdge = newEdge;
            newVertex.outgoingEdge = newEdge;

            //if newVertex already created
            if (edge.twinEdge != null && edge.twinEdge.nextEdge.sourceVertex.position == splittingPoint)
            {
                newEdge.twinEdge.twinEdge = newEdge;
                edge.twinEdge = newEdge.twinEdge.nextEdge;
                newEdge.twinEdge.nextEdge.twinEdge = edge;
            }
        }
        public void SplitFace(Face face, Vector3 splittingPoint)
        {
            bool isRecycled = false;

            Face currentFace = face;

            Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
            vertices.Add(newVertex);

            List<HalfEdge> faceEdges = face.GetFaceEdges();

            for (int i = 0; i < faceEdges.Count; i += 2)
            {
                //Add newFace if old isRecycled
                if (isRecycled)
                {
                    currentFace = new Face(faces.Count);
                    faces.Add(currentFace);
                }

                HalfEdge rightEdge = faceEdges[(i - 1 + faceEdges.Count) % faceEdges.Count];
                HalfEdge bottomEdge = faceEdges[i];

                HalfEdge leftEdge = new HalfEdge(edges.Count, bottomEdge.nextEdge.sourceVertex, currentFace, bottomEdge);
                edges.Add(leftEdge);
                HalfEdge topEdge = new HalfEdge(edges.Count, newVertex, currentFace, leftEdge, rightEdge);

                edges.Add(topEdge);

                leftEdge.nextEdge = topEdge;
                if (newVertex.outgoingEdge == null) newVertex.outgoingEdge = topEdge;
                if (currentFace.edge == null) currentFace.edge = topEdge;
                //twinEdge 

                if (isRecycled)
                {
                    topEdge.twinEdge = rightEdge.prevEdge.nextEdge;
                    rightEdge.prevEdge.nextEdge.twinEdge = topEdge;
                    if (i == 6)
                    {
                        leftEdge.twinEdge = bottomEdge.nextEdge.prevEdge;
                        bottomEdge.nextEdge.prevEdge.twinEdge = leftEdge;
                    }
                }

                rightEdge.prevEdge = topEdge;
                bottomEdge.nextEdge = leftEdge;

                isRecycled = true;
            }
        }
        public string ConvertToCSVFormat(string separator = "\t") // Conversion vers format CSV
        {
            if (this == null) return "";

            // Attributs

            string str = "";

            List<string> strings = new List<string>();

            // R�cup�ration des vertices dans le fichier CSV

            for (int i = 0; i < vertices.Count; i++)
            {

                List<HalfEdge> adjacentEdges = vertices[i].GetAdjacentEdges();
                List<Face> adjacentFaces = vertices[i].GetAdjacentFaces();
                List<HalfEdge> borderEdges = vertices[i].GetBorderEdges();


                List<int> edgesIndex = new List<int>();
                List<int> facesIndex = new List<int>();
                List<int> borderEdgesIndex = new List<int>();

                for (int j = 0; j < adjacentEdges.Count; j++)
                    edgesIndex.Add(adjacentEdges[j].index);

                for (int j = 0; j < adjacentFaces.Count; j++)
                    facesIndex.Add(adjacentFaces[j].index);

                for (int j = 0; j < borderEdges.Count; j++)
                    borderEdgesIndex.Add(borderEdges[j].index);

                Vector3 pos = vertices[i].position;
                strings.Add(vertices[i].index + separator
                    + pos.x.ToString("N03") + " " + pos.y.ToString("N03") + " " + pos.z.ToString("N03") + separator
                    + vertices[i].outgoingEdge.index + separator
                    + string.Join(" ", edgesIndex) + separator
                    + string.Join(" ", facesIndex) + separator
                    + string.Join(" ", borderEdgesIndex) + separator
                    + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator + separator + separator + separator);

            // R�cup�ration des edges dans le fichier CSV

            for (int i = 0; i < edges.Count; i++)
            {
                strings[i] += edges[i].index + separator
                    + edges[i].sourceVertex.index + separator
                    + edges[i].face.index + separator
                    + edges[i].prevEdge.index + separator
                    + edges[i].nextEdge.index + separator
                    + $"{(edges[i].twinEdge != null ? $"{edges[i].twinEdge.index}" : "NULL")}" + separator + separator;
            }

            // R�cup�ration des faces dans le fichier CSV

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
                    + string.Join(" ", vertexIndex) + separator
                    + separator;
            }

            // Mise en page du fichier CSV

            str = "Vertex" + separator + separator + separator + separator + separator + separator + separator + "HalfEges" + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "outgoingEdge" + separator + "adjacentEdge" + separator + "Face Adj" + separator + "Border Edges" + separator + separator +
                "Index" + separator + "sourceVertex" + separator + "Face" + separator + "prevEdge" + separator + "nextEdge" + separator + "twinEdge" + separator + separator +
                "Index" + separator + "Edge" + separator + "CW Face Edges" + separator + "CW Face Vertices\n"
                + string.Join("\n", strings);
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
                if (drawVertices) Handles.Label(worldPos, "V" + vertices[i].index, style);
            }


            // Affichage des faces

            for (int i = 0; i < faces.Count; i++)
            {
                style.normal.textColor = Color.magenta;
                List<Vertex> faceVertex = faces[i].GetFaceVertex();
                Vector3 C = new Vector3();
                for (int j = 0; j < faceVertex.Count; j++)
                {
                    if (drawFaces) Gizmos.DrawLine(transform.TransformPoint(faceVertex[j].position), transform.TransformPoint(faceVertex[(j + 1) % faceVertex.Count].position));
                    C += faceVertex[j].position;
                }
                if (drawFaces) Handles.Label(transform.TransformPoint(C / 4f), "F" + faces[i].index, style);

                
                List<HalfEdge> faceEdges = faces[i].GetFaceEdges();

                // Affichage des edges
                for (int j = 0; j < faceEdges.Count; j++)
                {
                    style.normal.textColor = Color.blue;
                    Vector3 start = faceEdges[j].sourceVertex.position;
                    Vector3 end = faceEdges[j].nextEdge.sourceVertex.position;
                    Vector3 pos = Vector3.Lerp(Vector3.Lerp(start, end, 0.5f), C / 4f, 0.1f);

                    if (drawEdges) Handles.Label(transform.TransformPoint(pos), "e" + faceEdges[j].index, style);
                }

            }

        }
    }
}
