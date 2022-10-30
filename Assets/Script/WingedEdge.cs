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
           
    }
    public class Face
    {
        public int index;
        public WingedEdge edge;

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
        public List<Vector3> facePoints;
        public List<Vector3> edgePoints;
        public List<Vector3> vertexPoints;
        Dictionary<ulong, WingedEdge> dicoEdges;
        public WingedEdgeMesh(Mesh mesh)
        {
            int nEdges= 4; //quads

            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            dicoEdges = new Dictionary<ulong, WingedEdge>();
            WingedEdge wingedEdge;

            //Complete List Vertex
            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            //Complete List Face and WingedEdge
            for (int i = 0; i < m_quads.Length/ nEdges; i++) {

                //Add face
                Face face = new Face(faces.Count);
                faces.Add(face);
                
                //quads vertices index
                int[] quad_index = new int[nEdges];
                for(int k = 0; k < 4; k++)
                    quad_index[k] = m_quads[nEdges * i + k];

                for(int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nEdges];

                    ulong key = (ulong)Mathf.Min(start,end) + ((ulong)Mathf.Max(start, end) << 32);

                    if (!dicoEdges.TryGetValue(key, out wingedEdge)) //Create a new wingedEdge
                    {
                        wingedEdge = new WingedEdge(edges.Count, vertices[start], vertices[end], face, null);
                        edges.Add(wingedEdge);
                        
                        if(face.edge==null) face.edge = wingedEdge;

                        if (vertices[start].edge == null) vertices[start].edge = wingedEdge;
                        if (vertices[end].edge == null) vertices[end].edge = wingedEdge;

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
            foreach(var face in faces)
            {
                List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == face || edge.leftFace == face);
                List<WingedEdge> cw_edges = new List<WingedEdge>();
                List<Vertex> cw_vertices = new List<Vertex>();
                WingedEdge wingedEdge = face.edge;
                //Edge CW
                foreach (var edge in face_edges)
                {
                    cw_edges.Add(wingedEdge);
                    wingedEdge = (face == wingedEdge.rightFace) ? wingedEdge.endCCWEdge : wingedEdge.startCCWEdge;
                }
                //Vertice CW
                foreach (var edge in cw_edges)
                    cw_vertices.Add((edge.rightFace == face) ? edge.startVertex : edge.endVertex);

                
                foreach(var vertice in cw_vertices)
                    m_quads[index++] = vertice.index;

            }

            faceVertexMesh.vertices = m_vertices;
            faceVertexMesh.SetIndices(m_quads, MeshTopology.Quads, 0);

            return faceVertexMesh;
        }

        public void SubdivideCatmullClark()
        {
            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            CatmullClarkCreateNewPoints(out facePoints, out edgePoints, out vertexPoints);

            for (int i = 0; i < vertexPoints.Count; i++)
                vertices[i].position = vertexPoints[i];

            for (int i = 0; i < edgePoints.Count; i++)
                SplitEdge(edges[i], edgePoints[i]);
            foreach (var face in faces)
            {
                //get all edges of the current face
                List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == face || edge.leftFace == face);

                foreach (var edge in face_edges)
                {
                    if (edge.rightFace == face)
                    {
                        edge.startCWEdge = edges.Find(e => (e.endVertex == edge.startVertex && e.rightFace == face) || (e.startVertex == edge.startVertex && e.leftFace == face));
                        edge.endCCWEdge = edges.Find(e => (e.startVertex == edge.endVertex && e.rightFace == face) || (e.endVertex == edge.endVertex && e.leftFace == face));
                    }
                    if (edge.leftFace == face)
                    {
                        edge.startCCWEdge = edges.Find(e => (e.startVertex == edge.startVertex && e.rightFace == face) || (e.endVertex == edge.startVertex && e.leftFace == face));
                        edge.endCWEdge = edges.Find(e => (e.endVertex == edge.endVertex && e.rightFace == face) || (e.startVertex == edge.endVertex && e.leftFace == face));
                    }
                    if (edge.leftFace == null)
                    {
                        edge.startCCWEdge = edges.Find(e => e.endVertex == edge.startVertex && e.leftFace == null);
                        edge.endCWEdge = edges.Find(e => e.startVertex == edge.endVertex && e.leftFace == null);
                    }

                }
            }
            
            
            
            for (int i = 0; i < facePoints.Count; i++)
            {
                SplitFace(faces[i], facePoints[i]);
            }
            foreach (var face in faces)
            {
                //get all edges of the current face
                List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == face || edge.leftFace == face);

                foreach (var edge in face_edges)
                {
                    if (edge.rightFace == face)
                    {
                        edge.startCWEdge = edges.Find(e => (e.endVertex == edge.startVertex && e.rightFace == face) || (e.startVertex == edge.startVertex && e.leftFace == face));
                        edge.endCCWEdge = edges.Find(e => (e.startVertex == edge.endVertex && e.rightFace == face) || (e.endVertex == edge.endVertex && e.leftFace == face));
                    }
                    if (edge.leftFace == face)
                    {
                        edge.startCCWEdge = edges.Find(e => (e.startVertex == edge.startVertex && e.rightFace == face) || (e.endVertex == edge.startVertex && e.leftFace == face));
                        edge.endCWEdge = edges.Find(e => (e.endVertex == edge.endVertex && e.rightFace == face) || (e.startVertex == edge.endVertex && e.leftFace == face));
                    }
                    if (edge.leftFace == null)
                    {
                        edge.startCCWEdge = edges.Find(e => e.endVertex == edge.startVertex && e.leftFace == null);
                        edge.endCWEdge = edges.Find(e => e.startVertex == edge.endVertex && e.leftFace == null);
                    }

                }
            }
            
            facePoints = null;
            vertexPoints = null;
            edgePoints = null;
        }


        public void CatmullClarkCreateNewPoints(out List<Vector3> facePoints, out List<Vector3> edgePoints, out List<Vector3> vertexPoints)
        {

            facePoints = new List<Vector3>();
            edgePoints = new List<Vector3>();
            vertexPoints = new List<Vector3>();
            List<Vector3> mid_point = new List<Vector3>();
            //facePoints
            foreach (var face in faces)
            {
                List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == face || edge.leftFace == face);
                List<WingedEdge> cw_edges = new List<WingedEdge>();
                List<Vertex> cw_vertices = new List<Vertex>();
                WingedEdge wingedEdge = face.edge;
                //Edge CW
                foreach (var edge in face_edges)
                {
                    cw_edges.Add(wingedEdge);
                    wingedEdge = (face == wingedEdge.rightFace) ? wingedEdge.endCCWEdge : wingedEdge.startCCWEdge;
                }
                //Vertice CW
                foreach (var edge in cw_edges)
                    cw_vertices.Add((edge.rightFace == face) ? edge.startVertex : edge.endVertex);


                Vector3 C = new Vector3();
                foreach (var vertice in cw_vertices)
                    C += vertice.position;

                facePoints.Add(C / 4f);

            }

            //mid point
            foreach (var edge in edges)
                mid_point.Add((edge.startVertex.position + edge.endVertex.position) / 2f);

            //EdegePoints
            foreach (var edge in edges)
                edgePoints.Add(edge.leftFace != null ? (edge.startVertex.position + edge.endVertex.position + facePoints[edge.rightFace.index] + facePoints[edge.leftFace.index]) / 4f : mid_point[edge.index]);

            //vertices Points
            foreach (var vertice in vertices)
            {
                Vector3 Q = new Vector3();
                Vector3 R = new Vector3();
                List<WingedEdge> edge_adj = edges.FindAll(edge => edge.startVertex == vertice || edge.endVertex == vertice);
                
                
                if (edge_adj.FindAll(edge => edge.rightFace != null && edge.leftFace != null).Count == edge_adj.Count)
                {
                    Debug.Log("not bordure");
                    foreach (var face in edge_adj)
                        Q += (vertice == face.startVertex) ? facePoints[face.rightFace.index] : facePoints[face.leftFace.index];
                    foreach (var edge in edge_adj)
                        R += mid_point[edge.index];
                    Q = Q / (float)edge_adj.Count;
                    R = R / (float)edge_adj.Count;
                    vertexPoints.Add(( Q / (float)edge_adj.Count) + (2f * R ) / (float)edge_adj.Count  + ((float)(edge_adj.Count - 3) * vertice.position )/ (float)edge_adj.Count );
                }
                else
                {

                    Debug.Log("bordure");
                    List<WingedEdge> face_adj = edge_adj.FindAll(edge => edge.leftFace == null);
                    Vector3 tot_m = new Vector3();
                    foreach (var edge in face_adj)
                        tot_m += mid_point[edge.index];
                    vertexPoints.Add((tot_m + vertice.position) / 3f);
                }
            }


            string p = "";
            foreach (var face in facePoints)
            {
                p += $"{face.x} {face.y} {face.z} \t  \n";
            }
            Debug.Log(p);
            p = "";
            foreach (var edge in edgePoints)
            {
                p += $"{edge.x} {edge.y} {edge.z}\n";
            }
            Debug.Log(p);

            p = "";
            foreach (var face in vertexPoints)
            {
                p += $"{face.x} {face.y} {face.z}\n";
            }
            Debug.Log(p);

        }
        public void SplitEdge(WingedEdge edge, Vector3 splittingPoint)
        {
            Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
            vertices.Add(newVertex);


            WingedEdge newEdge = new WingedEdge(edges.Count, newVertex, edge.endVertex, edge.rightFace, edge.leftFace);
            edges.Add(newEdge);

            edge.endVertex = newVertex;
            newVertex.edge = newEdge;
        }
        public void SplitFace(Face face, Vector3 splittingPoint)
        {
            //Pour chaque face, il faut créer 4 faces, 4 nouvelles edges et une vertices central
            Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
            vertices.Add(newVertex);
            bool isRecycled = false;

            dicoEdges = new Dictionary<ulong, WingedEdge>();
            WingedEdge wingedEdge;

            List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == face || edge.leftFace == face);
            List<WingedEdge> cw_edges = new List<WingedEdge>();
            WingedEdge faceEdge = face.edge;
            //Edge CW
            for(int i = 0; i < 4; i++)
            {
                cw_edges.Add(faceEdge);
                faceEdge = face == faceEdge.rightFace ? face == faceEdge.endCCWEdge.rightFace ? faceEdge.endCCWEdge.endCCWEdge : faceEdge.endCCWEdge.startCCWEdge : face == faceEdge.startCCWEdge.rightFace ? faceEdge.startCCWEdge.endCCWEdge : faceEdge.startCCWEdge.startCCWEdge;
            }

            string p = "";
            for (int i = 0; i < cw_edges.Count; i++)
            {
                p += $"e{cw_edges[i].index} ";
            }
            p += "\n";
            string str = "";


            foreach (var e in edges)
            {
                int start = e.startVertex.index;
                int end = e.endVertex.index;

                ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                dicoEdges.Add(key, e);
            }

            foreach (var edge in cw_edges)
            {
                int[] quad_index = new int[4];
                quad_index[0] = newVertex.index;
                Face newFace = null;
                if (isRecycled)
                {
                    newFace = new Face(faces.Count);
                    faces.Add(newFace);
                }
                if (edge.rightFace == face)
                {
                    p += " RIGHT FACE : ";
                    if (edge.endCCWEdge == edge.endCWEdge)
                    {
                        quad_index[1] = edge.startCWEdge.endVertex == edge.startVertex ? edge.startCWEdge.startVertex.index : edge.startCWEdge.endVertex.index;
                        quad_index[2] = edge.startVertex.index;
                        quad_index[3] = edge.endVertex.index;

                        p += $"trouver edge du start : V{quad_index[1]} \n";
                    }
                    else
                    {
                        quad_index[1] = edge.startVertex.index;
                        quad_index[2] = edge.endVertex.index;
                        quad_index[3] = edge.endCCWEdge.startVertex == edge.endVertex ? edge.endCCWEdge.endVertex.index : edge.endCCWEdge.startVertex.index;

                        p += $"trouver edge du end  : V{quad_index[3]}\n";
                    }
                }
                else
                {
                    p += " LEFT FACE : ";
                    if (edge.endCWEdge == edge.endCCWEdge)
                    {
                        quad_index[1] = edge.endVertex.index;
                        quad_index[2] = edge.startVertex.index;
                        quad_index[3] = edge.startCCWEdge.endVertex == edge.startVertex ? edge.startCCWEdge.startVertex.index : edge.startCCWEdge.endVertex.index;

                        p += $"trouver edge du start : V{quad_index[3]}\n";

                    }
                    else
                    {
                        quad_index[1] = edge.endCWEdge.startVertex == edge.endVertex ? edge.endCWEdge.endVertex.index : edge.endCWEdge.startVertex.index;
                        quad_index[2] = edge.endVertex.index;
                        quad_index[3] = edge.startVertex.index;
                        p += $"trouver edge du start : V{quad_index[1]} \n";
                    }
                }
                for (int i = 0; i < quad_index.Length; i++)
                {
                    p += $"V{quad_index[i]}-";
                }
                p += "\n";


                for (int i = 0; i < quad_index.Length; i++)
                {
                    wingedEdge = null;
                    //recyle 
                    str += $"e{i} : ";
                    if (!isRecycled)
                    {
                        int start = quad_index[i];
                        int end = quad_index[(i + 1) % 4];
                        ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                        

                        if (!dicoEdges.TryGetValue(key, out wingedEdge))
                        {
                            str += $" - createNewEdge V{start} V{end} - ";
                            wingedEdge = new WingedEdge(edges.Count, vertices[start], vertices[end], face, null);
                            edges.Add(wingedEdge);

                            if (face.edge == null) face.edge = wingedEdge;

                            if (vertices[start].edge == null) vertices[start].edge = wingedEdge;
                            if (vertices[end].edge == null) vertices[end].edge = wingedEdge;

                            dicoEdges.Add(key, wingedEdge);
                        }
                        else
                        {
                            if (wingedEdge.startVertex == vertices[end] && wingedEdge.endVertex == vertices[start]) wingedEdge.leftFace = face;
                            if (face.edge == null) face.edge = wingedEdge;
                            str += $" - Edge already exist V{start} V{end} - ";
                        }


                    }
                    else
                    //new
                    {
                        p += $"V{quad_index[i]}V{quad_index[(i + 1) % 4]} - ";

                        int start = quad_index[i];
                        int end = quad_index[(i + 1) % 4];
                        ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                        if (!dicoEdges.TryGetValue(key, out wingedEdge))
                        {
                            str += $" - createNewEdge V{start} V{end} - ";
                            wingedEdge = new WingedEdge(edges.Count, vertices[start], vertices[end], newFace, null);
                            edges.Add(wingedEdge);

                            if (newFace.edge == null) newFace.edge = wingedEdge;

                            if (vertices[start].edge == null) vertices[start].edge = wingedEdge;
                            if (vertices[end].edge == null) vertices[end].edge = wingedEdge;

                            dicoEdges.Add(key, wingedEdge);
                        }
                        else
                        {
                            str += $" - Edge already exist V{start} V{end} - ";
                            if (wingedEdge.startVertex == vertices[start] && wingedEdge.endVertex == vertices[end]) wingedEdge.rightFace = newFace;
                            if (wingedEdge.startVertex == vertices[end] && wingedEdge.endVertex == vertices[start]) wingedEdge.leftFace = newFace;
                            if (newFace.edge == null) newFace.edge = wingedEdge;
                        }
                    }
                    
                }
                str += "\n";
                isRecycled = true;

            }
            //Debug.Log(p);
            //Debug.Log(str);
        }

        public string ConvertToCSVFormat(string separator = "\t")
        {
            if (this == null) return "";

            Debug.Log("#################      WindgedEdgeMesh ConvertTOCSVFormat     #################");

            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            List<Vector3> facePoints = this.facePoints;
            List<Vector3> edgePoints = this.edgePoints;
            List<Vector3> vertexPoints = this.vertexPoints;

            List<string> strings = new List<string>();
            string p = "";
            foreach (var edge in edges)
            {
                p += $"{edge.index} \t {edge.startVertex.index} \t {edge.endVertex.index} \t {(edge.leftFace != null ? edge.leftFace.index.ToString() : "NULL")} \t {(edge.rightFace != null ? edge.rightFace.index.ToString() : "NULL")} \t {(edge.startCCWEdge!=null?edge.startCCWEdge.index.ToString():"NULL")} \t {(edge.startCWEdge !=null ? edge.startCWEdge.index.ToString() : "NULL")}  \t {(edge.endCWEdge != null ? edge.endCWEdge.index.ToString() : "NULL")} \t {(edge.endCCWEdge !=null  ? edge.endCCWEdge.index.ToString() : "NULL")} \n";
            }
            Debug.Log(p);
            p = "";
            foreach (var vertice in vertices)
            {
                p += $"{vertice.index} \t {vertice.position.x} {vertice.position.y} {vertice.position.z} \t {(vertice.edge != null ? vertice.edge.index.ToString() : "NULL")}\n";
            }
            Debug.Log(p);

            p = "";
            foreach (var face in faces)
            {
                p += $"{face.index} \t {(face.edge != null ? face.edge.index.ToString() : "NULL")}\n";
            }
            Debug.Log(p);
            //Vertices
            foreach (var vertice in vertices)
            {
                List<WingedEdge> edges_vertice = edges.FindAll(edge => edge.startVertex == vertice || edge.endVertex == vertice);
                List<int> face_adj = new List<int>();
                List<int> edges_adj = new List<int>();
                foreach (var edge in edges_vertice)
                        if (edge.startVertex == vertice)
                            face_adj.Add(edge.rightFace.index);
                        else if (edge.leftFace != null && edge.endVertex == vertice)
                            face_adj.Add(edge.leftFace.index);
                foreach (var edge in edges_vertice)
                    edges_adj.Add(edge.index);

                strings.Add(vertice.index + separator
                    + vertice.position.x.ToString("N03") + " " 
                    + vertice.position.y.ToString("N03") + " " 
                    + vertice.position.z.ToString("N03") + separator 
                    + vertice.edge.index + separator
                    + string.Join(" ", edges_adj) + separator
                    + string.Join(" ", face_adj) + separator
                    + $"{(vertexPoints != null ? vertexPoints[vertice.index].x.ToString("N03") : "NULL")}" + " "
                    + $"{(vertexPoints != null ? vertexPoints[vertice.index].y.ToString("N03") : "NULL")}" + " "
                    + $"{(vertexPoints != null ? vertexPoints[vertice.index].z.ToString("N03") : "NULL")}"
                    + separator + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator + separator + separator + separator);

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
                    + $"{(edges[i].endCCWEdge != null ? edges[i].endCCWEdge.index.ToString() : "NULL")}" + separator
                    + $"{(edgePoints != null ? edgePoints[i].x.ToString("N03") : "NULL")}" + " "
                    + $"{(edgePoints != null ? edgePoints[i].y.ToString("N03") : "NULL")}" + " "
                    + $"{(edgePoints != null ? edgePoints[i].z.ToString("N03") : "NULL")}"
                    + separator + separator;
            }
            for (int i = 0; i < faces.Count; i++)
            {
                List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == faces[i] || edge.leftFace == faces[i]);
                List<int> cw_edges = new List<int>();
                List<int> cw_vertices = new List<int>();
                WingedEdge wingedEdge = faces[i].edge;
                //Edge CW
                foreach (var edge in face_edges)
                {
                    cw_edges.Add(wingedEdge.index);
                    wingedEdge = (faces[i] == wingedEdge.rightFace) ? wingedEdge.endCCWEdge : wingedEdge.startCCWEdge;
                }
                //Vertice CW
                foreach (var index in cw_edges)
                    cw_vertices.Add((edges[index].rightFace == faces[i]) ? edges[index].startVertex.index : edges[index].endVertex.index);



                strings[i] += faces[i].index + separator
                    + faces[i].edge.index + separator
                    + string.Join(" ", cw_edges) + separator
                    + string.Join(" ", cw_vertices) + separator
                    + $"{(facePoints != null ? facePoints[i].x.ToString("N03") : "NULL")}" + " "
                    + $"{(facePoints != null ? facePoints[i].y.ToString("N03") : "NULL")}" + " "
                    + $"{(facePoints != null ? facePoints[i].z.ToString("N03") : "NULL")}"
                    + separator + separator;
            }

            string str = "Vertex" + separator + separator + separator + separator + separator + separator + separator + "WingedEdges" + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "Edge" + separator + "Edges Adj" + separator + "Face Adj" + separator + "VertexPoints" + separator + separator +
                "Index" + separator + "Start Vertex" + separator + "End Vertex" + separator + "Left Face" + separator + "Right Face" + separator + "Start CCW Edge" + separator + "Start CW Edge" + separator + "End CW Edge" + separator + "End CCW Edge" + separator + "EdgePoints" + separator + separator +
                "Index" + separator + "Edge" + separator + "CW Edges" + separator + "CW Vertices" + separator + "FacePoints\n"
                + string.Join("\n", strings);
            Debug.Log(str);
            return str;
        }
        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform)
        {
            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            Gizmos.color = Color.black;
            GUIStyle style = new GUIStyle();
            style.fontSize = 12;

            //vertices
            if (drawVertices)
            {
                style.normal.textColor = Color.red;
                for (int i = 0; i < vertices.Count; i++)
                    Handles.Label(transform.TransformPoint(vertices[i].position), "V" + vertices[i].index, style);
            }

            ////faces
            if (drawFaces)
            {
                style.normal.textColor = Color.magenta;
                foreach (var face in faces)
                {
                    List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == face || edge.leftFace == face);
                    List<WingedEdge> cw_edges = new List<WingedEdge>();
                    List<Vertex> cw_vertices = new List<Vertex>();

                    WingedEdge wingedEdge = face.edge;
                    //Edge CW
                    foreach (var edge in face_edges)
                    {
                        cw_edges.Add(wingedEdge);
                        wingedEdge = (face == wingedEdge.rightFace) ? wingedEdge.endCCWEdge : wingedEdge.startCCWEdge;
                    }
                    //Vertice CW
                    foreach (var edge in cw_edges)
                        cw_vertices.Add((edge.rightFace == face) ? edge.startVertex : edge.endVertex);


                    Vector3 C = new Vector3();
                    for (int i = 0; i < cw_vertices.Count; i++)
                    {
                        Gizmos.DrawLine(cw_vertices[i].position, cw_vertices[(i + 1) % cw_vertices.Count].position);
                        C += cw_vertices[i].position;
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
