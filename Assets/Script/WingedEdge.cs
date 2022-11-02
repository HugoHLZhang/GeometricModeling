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
        public Face leftFace;
        public Face rightFace;
        public WingedEdge startCWEdge;
        public WingedEdge startCCWEdge;
        public WingedEdge endCWEdge;
        public WingedEdge endCCWEdge;
        public WingedEdge(int index, Vertex startVertex, Vertex endVertex, Face rightFace, Face leftFace, WingedEdge startCWEdge=null, WingedEdge startCCWEdge = null, WingedEdge endCWEdge = null, WingedEdge endCCWEdge = null)
        {
            this.index = index;
            this.startVertex = startVertex;
            this.endVertex = endVertex;
            this.rightFace = rightFace;
            this.leftFace = leftFace;
            this.startCWEdge = startCWEdge;
            this.startCCWEdge = startCCWEdge;
            this.endCWEdge = endCWEdge;
            this.endCCWEdge = endCCWEdge;
        }


    }
    public class Vertex
    {
        public int index;
        public Vector3 position;
        public WingedEdge edge;
        public Vertex(int index, Vector3 position)
        {
            this.index = index;
            this.position = position;
        }
        public List<WingedEdge> GetAdjacentEdges()
        {
            List<WingedEdge> adjacentEdges = new List<WingedEdge>();

            WingedEdge wingedEdge = edge;

            while (!adjacentEdges.Contains(wingedEdge))
            {
                adjacentEdges.Add(wingedEdge);
                wingedEdge = (this == wingedEdge.startVertex) ? wingedEdge.startCWEdge : wingedEdge.endCWEdge;
            }

            return adjacentEdges;
        }
        public List<Face> GetAdjacentFaces()
        {
            List<WingedEdge> adjacentEdges = GetAdjacentEdges();
            List<Face> adjacentFaces = new List<Face>();

            foreach (var edge in adjacentEdges)
                if(edge.leftFace != null) adjacentFaces.Add(this == edge.startVertex ? edge.rightFace : edge.leftFace);

            return adjacentFaces;
        }
        public List<WingedEdge> GetBorderEdges()
        {
            List<WingedEdge> borderEdges = new List<WingedEdge>();
            List<WingedEdge> adjacentEdges = GetAdjacentEdges();

            foreach (var edge in adjacentEdges)
                if (edge.leftFace == null) borderEdges.Add(edge);

            return borderEdges;
        }

    }
    public class Face
    {
        public int index;
        public WingedEdge edge;
        public Face(int index)
        {
            this.index = index;
        }
        public List<WingedEdge> GetFaceEdges()
        {
            List<WingedEdge> faceEdges = new List<WingedEdge>();
            WingedEdge wingedEdge = edge;

            //Edge CW
            while (!faceEdges.Contains(wingedEdge))
            {
                faceEdges.Add(wingedEdge);
                wingedEdge = (this == wingedEdge.rightFace) ? wingedEdge.endCCWEdge : wingedEdge.startCCWEdge;
            }
            return faceEdges;
        }
        public List<Vertex> GetFaceVertex()
        {
            List<WingedEdge> faceEdges = GetFaceEdges();
            List<Vertex> faceVertices = new List<Vertex>();
            //Vertice CW
            foreach (var edge in faceEdges)
                faceVertices.Add((edge.rightFace == this) ? edge.startVertex : edge.endVertex);

            return faceVertices;
        }
        
    }
    public class WingedEdgeMesh
    {
        public List<Vertex> vertices;
        public List<WingedEdge> edges;
        public List<Face> faces;

        public WingedEdgeMesh(Mesh mesh)
        {

            Debug.Log("#################             Mesh ConvertToWingedEdgeMesh           #################");
            int nEdges= 4; //quads

            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            Dictionary<ulong, WingedEdge> dicoEdges = new Dictionary<ulong, WingedEdge>();
            WingedEdge wingedEdge;

            //Complete List Vertex
            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            //Complete List Face and WingedEdge
            for (int i = 0; i < m_quads.Length/ nEdges; i++) {

                Face face = new Face(faces.Count);
                faces.Add(face);
                
                //quads vertices index
                int[] quad_index = new int[nEdges];
                for(int j = 0; j < 4; j++)
                    quad_index[j] = m_quads[nEdges * i + j];

                for(int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nEdges];

                    ulong key = (ulong)Mathf.Min(start,end) + ((ulong)Mathf.Max(start, end) << 32);

                    //Search Edge already added
                    if (!dicoEdges.TryGetValue(key, out wingedEdge)) 
                    {
                        wingedEdge = new WingedEdge(edges.Count, vertices[start], vertices[end], face, null);
                        edges.Add(wingedEdge);
                        
                        if(face.edge==null) face.edge = wingedEdge;

                        vertices[start].edge = wingedEdge;
                        vertices[end].edge = wingedEdge;

                        dicoEdges.Add(key, wingedEdge);
                    }
                    else //wingedEdge already created
                    {
                        wingedEdge.leftFace = face;
                        if (face.edge == null) face.edge = wingedEdge;
                    }
                }
            }

            //Complete WingedEdge (CW and CCW Edges)
            foreach(var face in faces)
            {
                //get all edges of the current face
                List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == face || edge.leftFace == face);

                foreach(var edge in face_edges)
                {
                    if (edge.rightFace == face)
                    {
                        edge.startCWEdge    = edges.Find(e => (e.endVertex == edge.startVertex && e.rightFace == face) || (e.startVertex == edge.startVertex && e.leftFace == face));
                        edge.endCCWEdge     = edges.Find(e => (e.startVertex == edge.endVertex && e.rightFace == face) || (e.endVertex == edge.endVertex && e.leftFace == face));
                    }
                    if (edge.leftFace == face)
                    {
                        edge.startCCWEdge   = edges.Find(e => (e.startVertex == edge.startVertex && e.rightFace == face) || (e.endVertex == edge.startVertex && e.leftFace == face));
                        edge.endCWEdge      = edges.Find(e => (e.endVertex == edge.endVertex && e.rightFace == face) || (e.startVertex == edge.endVertex && e.leftFace == face));
                    }
                    if (edge.leftFace == null)
                    {
                        edge.startCCWEdge   = edges.Find(e => e.endVertex == edge.startVertex && e.leftFace == null);
                        edge.endCWEdge      = edges.Find(e => e.startVertex == edge.endVertex && e.leftFace == null);
                    }

                }
            }
        }
        public Mesh ConvertToFaceVertexMesh()
        {
            Debug.Log("#################                WindgedEdgeMesh ConvertToFaceVertexMesh                     #################");

            Mesh faceVertexMesh = new Mesh();
            // magic happens 

            Vector3[] m_vertices = new Vector3[vertices.Count];
            int[] m_quads = new int[faces.Count*4];

            //Vertices
            for (int i = 0; i < vertices.Count; i++)
                m_vertices[i] = vertices[i].position;


            int index = 0;
            //Quads
            foreach(var face in faces)
            {
                List<Vertex> faceVertex = face.GetFaceVertex();
                foreach(var vertice in faceVertex)
                    m_quads[index++] = vertice.index;
            }

            faceVertexMesh.vertices = m_vertices;
            faceVertexMesh.SetIndices(m_quads, MeshTopology.Quads, 0);

            return faceVertexMesh;
        }
        public void SubdivideCatmullClark()
        {
            Debug.Log("#################                    WindgedEdgeMesh SubdivideCatmullClark                   #################");
            List<Vector3> facePoints = null;
            List<Vector3> edgePoints = null;
            List<Vector3> vertexPoints = null;

            CatmullClarkCreateNewPoints(out facePoints, out edgePoints, out vertexPoints);

            for (int i = 0; i < edgePoints.Count; i++)
                SplitEdge(edges[i], edgePoints[i]);
           
            List<WingedEdge> eef = vertices[0].GetAdjacentEdges();
            Debug.Log(eef.Count);
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

            List<Vector3> midPoints= new List<Vector3>();
            //facePoints
            foreach (var face in faces)
            {
                List<Vertex> faceVertex = face.GetFaceVertex();
                Vector3 C = new Vector3();
                
                foreach (var vertice in faceVertex)
                    C += vertice.position;

                facePoints.Add(C / 4f);
            }

            //Mid Points
            foreach (var edge in edges)
                midPoints.Add((edge.startVertex.position + edge.endVertex.position) / 2f);

            //Edge Points
            foreach (var edge in edges)
                edgePoints.Add(edge.leftFace != null ? (edge.startVertex.position + edge.endVertex.position + facePoints[edge.rightFace.index] + facePoints[edge.leftFace.index]) / 4f : midPoints[edge.index]);

            //Vertex Points
            foreach (var vertice in vertices)
            {
                
                Vector3 Q = new Vector3();
                Vector3 R = new Vector3();
                List<WingedEdge> adjacentEdges = vertice.GetAdjacentEdges();
                List<Face> adjacentFaces = vertice.GetAdjacentFaces();

                

                //toutes les vertices poss�dant autant d�edges incidentes que de faces adjacentes.
                if (adjacentEdges.Count == adjacentFaces.Count)
                {
                    float n = adjacentFaces.Count;
                    foreach (var edge in adjacentEdges)
                        Q += (vertice == edge.startVertex) ? facePoints[edge.rightFace.index] : facePoints[edge.leftFace.index];
                    foreach (var edge in adjacentEdges)
                        R += midPoints[edge.index];
                    Q = Q / n;
                    R = R / n;
                    vertexPoints.Add(( Q / n) + (2f * R  / n)  + ((n - 3f) * vertice.position / n) );
                }
                //pour les vertices en bordure
                else
                {
                    List<WingedEdge> borderEdges = vertice.GetBorderEdges();
                    Vector3 tot_m = new Vector3();
                    foreach (var edge in borderEdges)
                        tot_m += midPoints[edge.index];
                    vertexPoints.Add((tot_m + vertice.position) / 3f);
                }
            }
        }
        public void SplitEdge(WingedEdge edge, Vector3 splittingPoint)
        {
            Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
            vertices.Add(newVertex);


            WingedEdge newEdge = new WingedEdge(edges.Count, newVertex, edge.endVertex, edge.rightFace, edge.leftFace, edge, edge, edge.endCWEdge, edge.endCCWEdge);
            edges.Add(newEdge);


            //update cw and ccw edge
            if (edge.endCWEdge.startCCWEdge == edge) edge.endCWEdge.startCCWEdge = newEdge;
            if (edge.endCCWEdge.startCWEdge == edge) edge.endCCWEdge.startCWEdge = newEdge;
            if (edge.endCWEdge.endCCWEdge == edge) edge.endCWEdge.endCCWEdge = newEdge;
            if (edge.endCCWEdge.endCWEdge == edge) edge.endCCWEdge.endCWEdge = newEdge;

            edge.endVertex = newVertex;
            edge.endVertex.edge = newEdge;
            
            edge.endCCWEdge = newEdge;
            edge.endCWEdge = newEdge;

            newVertex.edge = newEdge;
            newEdge.endVertex.edge = newEdge;



        }
        public void SplitFace(Face face, Vector3 splittingPoint)
        {
            bool isRecycled = false;

            Face currentFace = face;

            Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
            vertices.Add(newVertex);

            List<WingedEdge> faceEdges= face.GetFaceEdges();
            List<Vertex> faceVertex = face.GetFaceVertex();
            
            //Reorder Lists. So faceVertex[i] can be used to create the newEdge 
            if (face.edge.rightFace == face)
            {
                faceVertex.Insert(0, faceVertex[faceVertex.Count-1]);
                faceVertex.RemoveAt(faceVertex.Count-1);

                faceEdges.Insert(0, faceEdges[faceEdges.Count - 1]);
                faceEdges.RemoveAt(faceEdges.Count - 1);
            }
            
            
            for (int i = 0; i < faceEdges.Count; i+=2)
            {
                //Add newFace
                if (isRecycled)
                {
                    currentFace = new Face(faces.Count);
                    faces.Add(currentFace);
                }


                WingedEdge endCWEdge = faceEdges[(i - 1 + faceEdges.Count) % faceEdges.Count];
                WingedEdge endCCWEdge = faceEdges[i];
                WingedEdge endCCWEdgeNextEdge = faceEdges[(i + 1) % faceEdges.Count];

                WingedEdge newEdge = new WingedEdge(edges.Count, newVertex, faceVertex[i], currentFace, null, null, null, endCWEdge, endCCWEdge);
                edges.Add(newEdge);


                //Update for every split Face

                //Update Vertex's and Face's edge
                newVertex.edge = newEdge;
                if (currentFace.edge == null) currentFace.edge = newEdge;


                //Update des edges adjacentes � la endVertex de la nouvelle Edge
                if(endCWEdge.endVertex == newEdge.endVertex) 
                    endCWEdge.endCCWEdge = newEdge;
                if(endCWEdge.startVertex == newEdge.endVertex) 
                    endCWEdge.startCCWEdge = newEdge;


                if(endCCWEdge.startVertex == newEdge.endVertex)
                    endCCWEdge.startCWEdge = newEdge;
                if(endCCWEdge.endVertex == newEdge.endVertex)
                    endCCWEdge.endCWEdge = newEdge;


                //Update for the newFace
                if (isRecycled)
                {
                    //Update rightFace's & leftFace's edge
                    if (endCCWEdge.startVertex == newEdge.endVertex)
                    {
                        endCCWEdge.rightFace = currentFace;
                        if (endCCWEdgeNextEdge.startVertex == endCCWEdge.endVertex)
                            endCCWEdgeNextEdge.rightFace = currentFace;
                        if(endCCWEdgeNextEdge.endVertex == endCCWEdge.endVertex)
                            endCCWEdgeNextEdge.leftFace = currentFace;
                    }
                    if(endCCWEdge.endVertex == newEdge.endVertex)
                    {
                        endCCWEdge.leftFace = currentFace;
                        if (endCCWEdgeNextEdge.startVertex == endCCWEdge.startVertex)
                            endCCWEdgeNextEdge.rightFace = currentFace;
                        if (endCCWEdgeNextEdge.endVertex == endCCWEdge.startVertex)
                            endCCWEdgeNextEdge.leftFace = currentFace;
                    }

                    //Update newEdge startCCW/startCWEdge/leftFace
                    newEdge.startCCWEdge = edges[edges.Count - 2];
                    edges[edges.Count - 2].startCWEdge = newEdge;
                    newEdge.leftFace = edges[edges.Count - 2].rightFace;

                    //Update the last split of the Face - complete missing information of the first newEdge created
                    if((endCCWEdgeNextEdge.endVertex == edges[edges.Count - 4].endVertex || endCCWEdgeNextEdge.startVertex == edges[edges.Count - 4].endVertex ) && newVertex == edges[edges.Count - 4].startVertex)
                    {
                        newEdge.startCWEdge = edges[edges.Count - 4];
                        edges[edges.Count - 4].startCCWEdge = newEdge;
                        edges[edges.Count - 4].leftFace = currentFace;
                    }


                }

                isRecycled = true;
            }
        }
        public string ConvertToCSVFormat(string separator = "\t")
        {
            if (this == null) return "";

            Debug.Log("#################                WindgedEdgeMesh ConvertTOCSVFormat                  #################");


            List<string> strings = new List<string>();
            
            //Vertices
            foreach (var vertice in vertices)
            {
                List<WingedEdge> adjacentEdges = vertice.GetAdjacentEdges();
                List<Face> adjacentFaces = vertice.GetAdjacentFaces();

                List<int> edgesIndex = new List<int>();
                List<int> facesIndex = new List<int>();

                foreach (var edge in adjacentEdges)
                    edgesIndex.Add(edge.index);

                foreach (var face in adjacentFaces)
                    if(face != null) facesIndex.Add(face.index);


                strings.Add(vertice.index + separator
                    + vertice.position.x.ToString("N03") + " " 
                    + vertice.position.y.ToString("N03") + " " 
                    + vertice.position.z.ToString("N03") + separator 
                    + vertice.edge.index + separator
                    + string.Join(" ", edgesIndex) + separator
                    + string.Join(" ", facesIndex) 
                    + separator + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator + separator + separator);

            //Edges
            for (int i = 0; i < edges.Count; i++)
            {
                strings[i] += edges[i].index + separator
                    + edges[i].startVertex.index + separator
                    + edges[i].endVertex.index + separator
                    + $"{(edges[i].leftFace != null ? edges[i].leftFace.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].rightFace != null ? edges[i].rightFace.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].startCCWEdge != null ? edges[i].startCCWEdge.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].startCWEdge != null ? edges[i].startCWEdge.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].endCWEdge != null ? edges[i].endCWEdge.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].endCCWEdge != null ? edges[i].endCCWEdge.index.ToString() : "NULL")}"
                    + separator + separator;
            }
            for (int i = 0; i < faces.Count; i++)
            {
                List<WingedEdge> faceEdges = faces[i].GetFaceEdges();
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

            string str = "Vertex" + separator + separator + separator + separator + separator + separator + "WingedEdges" + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "Edge" + separator + "Edges Adj" + separator + "Face Adj" + separator + separator +
                "Index" + separator + "Start Vertex" + separator + "End Vertex" + separator + "Left Face" + separator + "Right Face" + separator + "Start CCW Edge" + separator + "Start CW Edge" + separator + "End CW Edge" + separator + "End CCW Edge" + separator + separator +
                "Index" + separator + "Edge" + separator + "CW Edges" + separator + "CW Vertices\n"
                + string.Join("\n", strings);
            Debug.Log(str);
            return str;
        }
        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform)
        {

            Gizmos.color = Color.black;
            GUIStyle style = new GUIStyle();
            style.fontSize = 12;

            //vertices
            if (drawVertices)
            {
                style.normal.textColor = Color.red;
                foreach (var vertice in vertices)
                    Handles.Label(transform.TransformPoint(vertice.position), "V" + vertice.index, style);
            }

            ////faces
            if (drawFaces)
            {
                style.normal.textColor = Color.magenta;
                foreach (var face in faces)
                {
                    List<Vertex> faceVertex = face.GetFaceVertex();

                    Vector3 C = new Vector3();
                    for (int i = 0; i < faceVertex.Count; i++)
                    {
                        Gizmos.DrawLine(faceVertex[i].position, faceVertex[(i + 1) % faceVertex.Count].position);
                        C += faceVertex[i].position;
                    }

                    Handles.Label(transform.TransformPoint(C / 4f), "F" + face.index, style);

                }
            }

            //edges
            if (drawEdges)
            {
                style.normal.textColor = Color.blue;
                for (int i = 0; i < edges.Count; i++)
                {
                    Vector3 start = transform.TransformPoint(edges[i].startVertex.position);
                    Vector3 end = transform.TransformPoint(edges[i].endVertex.position);
                    Vector3 pos = Vector3.Lerp(start, end, 0.5f);

                    Gizmos.DrawLine(start, end);
                    Handles.Label(pos, "e" + edges[i].index, style);
                }
            }


        }
    }
}
