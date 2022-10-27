using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace WingedEdge
{
    public class WingedEdge
    {
        public int index;
        public Vertex startVertex { set; get; }
        public Vertex endVertex { set; get; }
        public Face leftFace { set; get; }
        public Face rightFace { set; get; }
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
        public Vector3 position { set; get; }
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
        {
            int nbIndex;

            switch (mesh.GetTopology(0))
            {
                case MeshTopology.Quads:
                    nbIndex = 4;
                    break;
                case MeshTopology.Triangles:
                    nbIndex = 3;
                    break;
                default:
                    return;
            }

            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            


            Dictionary<ulong, WingedEdge> dico = new Dictionary<ulong, WingedEdge>();
            WingedEdge edge;

            //Complete List Vertex
            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            //Complete List Face and WingedEdge
            for (int i = 0; i < m_quads.Length/ nbIndex; i++) {
                Face face = new Face(faces.Count);
                faces.Add(face);
                int[] quad_index = new int[nbIndex];

                for(int k = 0; k < 4; k++)
                    quad_index[k] = m_quads[nbIndex * i + k];

                for(int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nbIndex];

                    ulong key = (ulong)Mathf.Min(start,end) + ((ulong)Mathf.Max(start, end) << 32);
                    if (!dico.TryGetValue(key, out edge))
                    {
                        edge = new WingedEdge(edges.Count, vertices[start], vertices[end], face, null);
                        edges.Add(edge);
                        
                        if(face.edge==null) face.edge = edge;

                        vertices[start].edge = edge;
                        vertices[end].edge = edge;
                        
                        dico.Add(key, edge);
                    }
                    else
                    {
                        edge.leftFace = face;
                        if (face.edge == null) face.edge = edge;
                    }
                }


            }

            //Complete WingedEdge (CW and CCW Edges)
            for (int i = 0; i < m_quads.Length / nbIndex; i++)
            {
                int[] quad_index = new int[nbIndex];

                for (int k = 0; k < 4; k++)
                    quad_index[k] = m_quads[nbIndex * i + k];

                for (int j = 0; j < quad_index.Length; j++)
                {
                    int prev = quad_index[(j - 1 + nbIndex) % nbIndex];
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nbIndex];
                    int next = quad_index[(j + 2) % nbIndex];



                    ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                    if (dico.TryGetValue(key, out edge))
                    {
                        
                        //cw
                        if (edge.startVertex == vertices[start] && edge.endVertex == vertices[end])//CW
                        {
                            edge.startCWEdge = edges.Find(e => (e.startVertex == vertices[prev] && e.endVertex == vertices[start]) || (e.startVertex == vertices[start] && e.endVertex == vertices[prev]));
                            edge.endCCWEdge = edges.Find(e => (e.startVertex == vertices[next] && e.endVertex == vertices[end]) || (e.startVertex == vertices[end] && e.endVertex == vertices[next]));
                        }
                        //ccw
                        if (edge.endVertex == vertices[start] && edge.startVertex == vertices[end])//CCW
                        {
                            edge.startCCWEdge = edges.Find(e => (e.startVertex == vertices[end] && e.endVertex == vertices[next]) || (e.startVertex == vertices[next] && e.endVertex == vertices[end]));
                            edge.endCWEdge = edges.Find(e => (e.startVertex == vertices[start] && e.endVertex == vertices[prev]) || (e.startVertex == vertices[prev] && e.endVertex == vertices[start]));
                        }

                        //complete null value
                        if (edge.leftFace == null && edge.endCWEdge == null) edge.endCWEdge = edges.Find(e => e.startVertex == edge.endVertex && e.rightFace.index == (edge.rightFace.index - 1 + faces.Count)%faces.Count);
                        if (edge.leftFace == null && edge.startCCWEdge== null) edge.startCCWEdge = edges.Find(e => e.endVertex == edge.startVertex && e.rightFace.index == (edge.rightFace.index + 1) % faces.Count);
                        
                        if (edge.leftFace == null && edge.endCWEdge == null) edge.endCWEdge = edge.endCCWEdge;
                        if (edge.leftFace == null && edge.startCCWEdge== null) edge.startCCWEdge = edge.startCWEdge;


                    }
                }
            }
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
                m_vertices[i] = vertices[i].position;

            int index = 0;
            //Quads
            for (int i = 0; i < faces.Count; i++)
            {
                //WingedEdge e = faces[i].edge;
                //List<WingedEdge> edges_face = edges.FindAll(edge => edge.leftFace == faces[i] || edge.rightFace == faces[i]);

                //for (int j = 0; j < edges_face.Count; j++)
                //{
                //    if (edges_face[j].rightFace.index == i)
                //        m_quads[index++] = edges_face[j].startVertex.index;
                //    else 
                //        m_quads[index++] = edges_face[j].endVertex.index;
                //}


                WingedEdge e = faces[i].edge;

                if (e.rightFace.index == i)
                {
                    m_quads[index++] = e.startVertex.index;
                    m_quads[index++] = e.endVertex.index;
                    if (e.endCCWEdge.rightFace.index == i)
                        m_quads[index++] = e.endCCWEdge.endVertex.index;
                    if (e.endCCWEdge.leftFace != null && e.endCCWEdge.leftFace.index == i)
                        m_quads[index++] = e.endCCWEdge.startVertex.index;
                    if (e.startCWEdge.rightFace.index == i)
                        m_quads[index++] = e.startCWEdge.startVertex.index;
                    if (e.startCWEdge.leftFace != null && e.startCWEdge.leftFace.index == i)
                        m_quads[index++] = e.startCWEdge.endVertex.index;
                }
                if (e.leftFace != null && e.leftFace.index == i)
                {
                    m_quads[index++] = e.endVertex.index;
                    m_quads[index++] = e.startVertex.index;
                    if (e.startCCWEdge.rightFace.index == i)
                        m_quads[index++] = e.startCCWEdge.endVertex.index;
                    if (e.startCCWEdge.leftFace != null && e.startCCWEdge.leftFace.index == i)
                        m_quads[index++] = e.startCCWEdge.startVertex.index;
                    if (e.endCWEdge.rightFace.index == i)
                        m_quads[index++] = e.endCWEdge.startVertex.index;
                    if (e.endCWEdge.leftFace != null && e.endCWEdge.leftFace.index == i)
                        m_quads[index++] = e.endCWEdge.endVertex.index;
                }

            }

            faceVertexMesh.vertices = m_vertices;
            faceVertexMesh.SetIndices(m_quads, MeshTopology.Quads, 0);

            return faceVertexMesh;
        }

        public void SubdivideCatmullClark()
        {

        }
        public void CatmullClarkCreateNewPoints(out List<Vector3> facePoints, out List<Vector3> edgePoints, out List<Vector3> vertexPoints)
        {
            facePoints = new List<Vector3>();
            edgePoints = new List<Vector3>();
            vertexPoints = new List<Vector3>();

            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            Mesh mesh = this.ConvertToFaceVertexMesh();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            List<Vector3> mid_point = new List<Vector3>();

            //facePoints
            for (int i = 0; i < m_quads.Length/4; i++)
            {
                int index1 = m_quads[4 * i];
                int index2 = m_quads[4 * i + 1];
                int index3 = m_quads[4 * i + 2];
                int index4 = m_quads[4 * i + 3];

                Vector3 pt1 = vertices[index1].position;
                Vector3 pt2 = vertices[index2].position;
                Vector3 pt3 = vertices[index3].position;
                Vector3 pt4 = vertices[index4].position;

                facePoints.Add((pt1 + pt2 + pt3 + pt4) / 4f);

            }
            //EdegePoints
            foreach(var edge in edges)
                edgePoints.Add( edge.leftFace != null ? (edge.startVertex.position + edge.endVertex.position + facePoints[edge.rightFace.index] + facePoints[edge.leftFace.index])/4f : (edge.startVertex.position + edge.endVertex.position)/2f);

            //mid point
            foreach(var edge in edges)
                mid_point.Add((edge.startVertex.position + edge.endVertex.position) / 2f);

            //vertices Points
            foreach (var vertice in vertices)
            {
                Vector3 Q = new Vector3();
                Vector3 R = new Vector3();
                List<WingedEdge> edge_adj = edges.FindAll(edge => edge.startVertex == vertice || edge.endVertex == vertice);

                if (edge_adj.FindAll(edge => edge.rightFace != null && edge.leftFace != null).Count >= 3)
                {
                    List<Face> face_adj = new List<Face>();
                    foreach (var a in edge_adj)
                        if (a.startVertex == vertice)
                            face_adj.Add(a.rightFace);
                        else if (a.leftFace != null && a.endVertex == vertice)
                            face_adj.Add(a.leftFace);
                    foreach (var face in face_adj)
                        Q += facePoints[face.index];
                    foreach (var edge in edge_adj)
                        R += mid_point[edge.index];
                    Q = Q / (float)face_adj.Count;
                    R = R / (float)edge_adj.Count;

                    vertexPoints.Add((1f / (float)face_adj.Count) * Q + (2f / (float)face_adj.Count) * R + (float)(face_adj.Count - 3) / (float)face_adj.Count * vertice.position);
                }
                else
                {
                    Vector3 tot_m = new Vector3();
                    foreach (var edge in edge_adj)
                        if (edge.leftFace == null)
                            tot_m += mid_point[edge.index];
                    vertexPoints.Add((tot_m + vertice.position) / 3f);
                }
            }
        }
        public void SplitEdge(WingedEdge edge, Vector3 splittingPoint)
        {
            Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
            vertices.Add(newVertex);
            WingedEdge newEdge = new WingedEdge(edges.Count, newVertex, edge.endVertex, edge.rightFace, edge.leftFace);
            edges.Add(newEdge);
            edge.endVertex = newVertex;

        }
        public void SplitFace(Face face, Vector3 splittingPoint)
        {
            List<WingedEdge> all_edge = edges.FindAll(edge => edge.rightFace == face || edge.leftFace == face);
            Debug.Log(all_edge.Count);
            //for(int i = 0; i < all_edge.Count; i++)
            //{

            //}

        }

        public string ConvertToCSVFormat(string separator = "\t")
        {
            if (this == null) return "";
            Debug.Log("#################      WindgedEdgeMesh ConvertTOCSVFormat     #################");
            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            List<string> strings = new List<string>();

            //Vertices
            foreach (var vertice in vertices)
            {
                List<WingedEdge> edges_vertice = edges.FindAll(edge => edge.startVertex == vertice || edge.endVertex == vertice);
                int[] edges_adj = new int[edges_vertice.Count];
                for (int i = 0; i < edges_vertice.Count; i++)
                    edges_adj[i] = edges_vertice[i].index;
                Vector3 pos = vertice.position;
                strings.Add(vertice.index + separator
                    + pos.x.ToString("N03") + " "
                    + pos.y.ToString("N03") + " "
                    + pos.z.ToString("N03") + separator 
                    + string.Join(" ", edges_adj)
                    + separator + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator);

            //Edges
            for (int i = 0; i < edges.Count; i++)
            {
                strings[i] += edges[i].index + separator
                    + edges[i].startVertex.index + separator
                    + edges[i].endVertex.index + separator
                    + $"{(edges[i].leftFace != null ? $"{edges[i].leftFace.index}" : "NULL")}" + separator
                    + $"{(edges[i].rightFace != null ? $"{edges[i].rightFace.index}" : "NULL")}" + separator
                    + edges[i].startCCWEdge.index + separator
                    + edges[i].startCWEdge.index + separator
                    + edges[i].endCWEdge.index + separator
                    + edges[i].endCCWEdge.index + separator
                    + separator;
            }

            //Faces
            for(int i = 0; i < faces.Count; i++)
            {
                List<WingedEdge> edges_face = edges.FindAll(edge => edge.leftFace == faces[i] || edge.rightFace == faces[i]);
                int[] face_edges = new int[edges_face.Count];
                for (int j = 0; j < edges_face.Count; j++)
                    face_edges[j] = edges_face[j].index;
                strings[i] += faces[i].index + separator + string.Join(" ", face_edges) +  separator + separator;
            }

            string str = "Vertex" + separator + separator + separator + separator + "WingedEdges" + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "Edge" + separator + separator +
                "Index" + separator + "Start Vertex" + separator + "End Vertex" + separator + "Left Face" + separator + "Right Face" + separator + "Start CCW Edge" + separator + "Start CW Edge" + separator + "End CW Edge" + separator + "End CCW Edge" + separator + separator +
                "Index" + separator + "Edge\n"
                + string.Join("\n", strings);
            Debug.Log(str);
            return str;
        }
        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform)
        {
            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            Mesh mesh = this.ConvertToFaceVertexMesh();
            int[] m_quads = mesh.GetIndices(0);

            List<Vector3> facePoints;
            List<Vector3> edgePoints;
            List<Vector3> vertexPoints;

            this.CatmullClarkCreateNewPoints(out facePoints, out edgePoints, out vertexPoints);



            Gizmos.color = Color.black;
            GUIStyle style = new GUIStyle();
            GUIStyle style2 = new GUIStyle();
            style.fontSize = 12;
            style2.fontSize = 12;

            //vertices
            if (drawVertices)
            {
                style.normal.textColor = Color.red;
                style2.normal.textColor = Color.black;
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 worldPos = transform.TransformPoint(vertices[i].position);
                    //Handles.Label(worldPos, "V"+vertices[i].index, style);
                    
                    Handles.Label(transform.TransformPoint(vertexPoints[i]), "V'" + i, style2);
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

                    //Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, "F" + faces[i].index, style);
                    style.normal.textColor = Color.green;
                    Handles.Label(facePoints[i], "C" + i.ToString(), style);
                }
            }




            //edges
            if (drawEdges)
            {
                style.normal.textColor = Color.blue;
                style2.normal.textColor = Color.cyan;
                for (int i=0; i < edges.Count; i++)
                {
                    Vector3 start = transform.TransformPoint(edges[i].startVertex.position);
                    Vector3 end = transform.TransformPoint(edges[i].endVertex.position);
                    Vector3 pos = Vector3.Lerp(start, end, 0.5f);

                    //Handles.Label(pos, "e" + edges[i].index, style);
                    
                    Handles.Label(transform.TransformPoint(edgePoints[i]), "E'" + i, style2);
                }
            }
           
        }
    }
}
