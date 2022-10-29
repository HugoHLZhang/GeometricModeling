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
            int nbIndex = 4; //quads

            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);



            dicoEdges = new Dictionary<ulong, WingedEdge>();
            WingedEdge edge;

            //Complete List Vertex
            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            //Complete List Face and WingedEdge
            for (int i = 0; i < m_quads.Length/ nbIndex; i++) {

                //Add face
                Face face = new Face(faces.Count);
                faces.Add(face);
                
                //quads vertices index
                int[] quad_index = new int[nbIndex];
                for(int k = 0; k < 4; k++)
                    quad_index[k] = m_quads[nbIndex * i + k];

                
                for(int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nbIndex];

                    ulong key = (ulong)Mathf.Min(start,end) + ((ulong)Mathf.Max(start, end) << 32);
                    if (!dicoEdges.TryGetValue(key, out edge))
                    {
                        edge = new WingedEdge(edges.Count, vertices[start], vertices[end], face, null);
                        edges.Add(edge);
                        
                        if(face.edge==null) face.edge = edge;

                        if (vertices[start].edge == null) vertices[start].edge = edge;
                        if (vertices[end].edge == null) vertices[end].edge = edge;

                        dicoEdges.Add(key, edge);
                    }
                    else
                    {
                        edge.leftFace = face;
                        if (face.edge == null) face.edge = edge;
                    }
                }


            }



            //Complete WingedEdge (CW and CCW Edges)
            for (int i = 0; i < faces.Count; i++)
            {
                List<WingedEdge> face_edges = edges.FindAll(edge => edge.rightFace == faces[i] || edge.leftFace == faces[i]);

                for (int j = 0; j < face_edges.Count; j++)
                {
                    if (face_edges[j].rightFace == faces[i])
                    {
                        edges[face_edges[j].index].startCWEdge = edges.Find(e => (e.endVertex == face_edges[j].startVertex && e.rightFace == faces[i]) || (e.startVertex == face_edges[j].startVertex && e.leftFace == faces[i]));
                        edges[face_edges[j].index].endCCWEdge = edges.Find(e => (e.startVertex == face_edges[j].endVertex && e.rightFace == faces[i]) || (e.endVertex == face_edges[j].endVertex && e.leftFace == faces[i]));
                    }
                    if (face_edges[j].leftFace == null)
                    {
                        edges[face_edges[j].index].startCCWEdge = edges.Find(e => e.endVertex == face_edges[j].startVertex && e.leftFace == null);
                        edges[face_edges[j].index].endCWEdge = edges.Find(e => e.startVertex == face_edges[j].endVertex && e.leftFace == null);
                    }
                    if (face_edges[j].leftFace == faces[i])
                    {
                        edges[face_edges[j].index].startCCWEdge = edges.Find(e => (e.startVertex == face_edges[j].startVertex && e.rightFace == faces[i]) || (e.endVertex == face_edges[j].startVertex && e.leftFace == faces[i]));
                        edges[face_edges[j].index].endCWEdge = edges.Find(e => (e.endVertex == face_edges[j].endVertex && e.rightFace == faces[i]) || (e.startVertex == face_edges[j].endVertex && e.leftFace == faces[i]));
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
                WingedEdge edge = faces[i].edge;
                List<int> edge_index = new List<int>();
                List<int> vertice_index = new List<int>();
                //Edge CW
                for (int j = 0; j < 4; j++)
                {
                    edge_index.Add(edge.index);
                    edge = (faces[i] == edge.rightFace) ? edge.endCCWEdge : edge.startCCWEdge;
                }
                //Vertice CW
                foreach (var a in edge_index) 
                    vertice_index.Add((edges[a].rightFace == faces[i]) ? edges[a].startVertex.index : edges[a].endVertex.index);

                for (int k = 0; k < vertice_index.Count; k++)
                    m_quads[index++] = vertice_index[k];
            }

            faceVertexMesh.vertices = m_vertices;
            faceVertexMesh.SetIndices(m_quads, MeshTopology.Quads, 0);

            return faceVertexMesh;
        }
        /*public void SubdivideCatmullClark()
        {
            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            List<Vector3> facePoints;
            List<Vector3> edgePoints;
            List<Vector3> vertexPoints;

            CatmullClarkCreateNewPoints(out facePoints, out edgePoints, out vertexPoints);
            //int i = 0;
            //foreach (var vertex in vertexPoints)
            //{
            //    vertices[i++].position = vertex;
            //}

            //for (i = 0; i < edgePoints.Count; i++)
            //{
            //    SplitEdge(edges[i], edgePoints[i]);
            //}
            //for (i = 0; i < facePoints.Count; i++)
            //{
            //    Debug.Log(i);
            //    Debug.Log(facePoints[i]);
            //    SplitFace(faces[i], facePoints[i]);
            //}
            //Dictionary<ulong, WingedEdge> dico = new Dictionary<ulong, WingedEdge>();
            //WingedEdge edge;
            //string p = "";
            //foreach (var e in edges)
            //{
            //    e.startCWEdge = null;
            //    e.startCCWEdge = null;
            //    e.endCWEdge = null;
            //    e.endCCWEdge = null;
            //    int start = e.startVertex.index;
            //    int end = e.endVertex.index;

            //    ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
            //    dico.Add(key, e);
            //}

            //for (i = 0; i < faces.Count; i++)
            //{
            //    Face face = faces[i];
            //    int[] quad_index = new int[4];
            //    if (face == face.edge.rightFace)
            //    {
            //        quad_index[0] = face.edge.startVertex.index;//start
            //        quad_index[1] = face.edge.endVertex.index;//end
            //        //next
            //        if (edges.Exists(edge => edge.startVertex.index == quad_index[1] && edge.rightFace == face))
            //            quad_index[2] = edges.Find(edge => edge.startVertex.index == quad_index[1] && edge.rightFace == face).endVertex.index;
            //        else if (edges.Exists(edge => edge.endVertex.index == quad_index[1] && edge.leftFace == face))
            //            quad_index[2] = edges.Find(edge => edge.endVertex.index == quad_index[1] && edge.leftFace == face).startVertex.index;
            //        //last
            //        else if (edges.Exists(edge => edge.endVertex.index == quad_index[0] && edge.rightFace == face))
            //            quad_index[3] = edges.Find(edge => edge.endVertex.index == quad_index[0] && edge.rightFace == face).startVertex.index;

            //        else if (edges.Exists(edge => edge.startVertex.index == quad_index[0] && edge.leftFace == face))
            //            quad_index[3] = edges.Find(edge => edge.startVertex.index == quad_index[0] && edge.leftFace == face).endVertex.index;
            //    }
            //    if (face.edge.leftFace != null && face == face.edge.leftFace)
            //    {
            //        quad_index[0] = face.edge.endVertex.index;//start
            //        quad_index[1] = face.edge.startVertex.index;//end
            //        //next
            //        if (edges.Exists(edge => edge.startVertex.index == quad_index[1] && edge.rightFace == face))
            //            quad_index[2] = edges.Find(edge => edge.startVertex.index == quad_index[1] && edge.rightFace == face).endVertex.index;
            //        else if (edges.Exists(edge => edge.endVertex.index == quad_index[1] && edge.leftFace == face))
            //            quad_index[2] = edges.Find(edge => edge.endVertex.index == quad_index[1] && edge.leftFace == face).startVertex.index;
            //        //last
            //        else if (edges.Exists(edge => edge.endVertex.index == quad_index[0] && edge.rightFace == face))
            //            quad_index[3] = edges.Find(edge => edge.endVertex.index == quad_index[0] && edge.rightFace == face).startVertex.index;

            //        else if (edges.Exists(edge => edge.startVertex.index == quad_index[0] && edge.leftFace == face))
            //            quad_index[3] = edges.Find(edge => edge.startVertex.index == quad_index[0] && edge.leftFace == face).endVertex.index;
            //    }

            //    for (int j = 0; j < quad_index.Length; j++)
            //    {

            //        int prev = quad_index[(j - 1 + 4) % 4];
            //        int start = quad_index[j];
            //        int end = quad_index[(j + 1) % 4];
            //        int next = quad_index[(j + 2) % 4];

            //        p += "j = " + j + $" (quadLength:{quad_index.Length} : " + prev + " " + start + " " + end + " " + next + "\n";

            //        ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
            //        if (dico.TryGetValue(key, out edge))
            //        {

            //            //cw
            //            if (edge.startVertex == vertices[start] && edge.endVertex == vertices[end])//CW
            //            {
            //                edge.startCWEdge = edges.Find(e => (e.startVertex == vertices[prev] && e.endVertex == vertices[start]) || (e.startVertex == vertices[start] && e.endVertex == vertices[prev]));
            //                edge.endCCWEdge = edges.Find(e => (e.startVertex == vertices[next] && e.endVertex == vertices[end]) || (e.startVertex == vertices[end] && e.endVertex == vertices[next]));
            //            }
            //            //ccw
            //            if (edge.endVertex == vertices[start] && edge.startVertex == vertices[end])//CCW
            //            {
            //                edge.startCCWEdge = edges.Find(e => (e.startVertex == vertices[end] && e.endVertex == vertices[next]) || (e.startVertex == vertices[next] && e.endVertex == vertices[end]));
            //                edge.endCWEdge = edges.Find(e => (e.startVertex == vertices[start] && e.endVertex == vertices[prev]) || (e.startVertex == vertices[prev] && e.endVertex == vertices[start]));
            //            }

            //            //complete null value
            //            if (edge.leftFace == null && edge.endCWEdge == null) edge.endCWEdge = edges.Find(e => e.startVertex == edge.endVertex && e.rightFace.index == (edge.rightFace.index - 1 + faces.Count) % faces.Count);
            //            if (edge.leftFace == null && edge.startCCWEdge == null) edge.startCCWEdge = edges.Find(e => e.endVertex == edge.startVertex && e.rightFace.index == (edge.rightFace.index + 1) % faces.Count);

            //            if (edge.leftFace == null && edge.endCWEdge == null) edge.endCWEdge = edge.endCCWEdge;
            //            if (edge.leftFace == null && edge.startCCWEdge == null) edge.startCCWEdge = edge.startCWEdge;


            //        }
            //    }
            //}
            //Debug.Log(p);
            string p = "";
            int cnt = 0;
            foreach (var v in facePoints)
            {
                p += $"facePoints{cnt++} : {v}\n";
            }
            Debug.Log(p);
            p = "";
            cnt = 0;
            foreach (var v in edgePoints)
            {
                p += $"edgePoints{cnt++} : {v}\n";
            }
            Debug.Log(p);

            p = "";
            cnt = 0;
            foreach (var v in vertexPoints)
            {
                p += $"vertexPoints{cnt++} : {v}\n";
            }
            Debug.Log(p);

        }
        */
        /*public void CatmullClarkCreateNewPoints(out List<Vector3> facePoints, out List<Vector3> edgePoints, out List<Vector3> vertexPoints)
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

                if (edge_adj.FindAll(edge => edge.rightFace != null && edge.leftFace != null).Count >= 2)
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
        */
        //public void SplitEdge(WingedEdge edge, Vector3 splittingPoint)
        //{
        //    Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
        //    vertices.Add(newVertex);
        //    WingedEdge newEdge = new WingedEdge(edges.Count, newVertex, edge.endVertex, edge.rightFace, edge.leftFace, edge, edge, edge.endCWEdge, edge.endCCWEdge);
        //    edges.Add(newEdge);
        //    edge.endCWEdge = newEdge;
        //    edge.endCCWEdge = newEdge;
        //    edge.endVertex = newVertex;
        //    newVertex.edge = newEdge;
        //}


        public void SubdivideCatmullClark()
        {
            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;

            //Quads
            for (int i = 0; i < faces.Count; i++)
            {
                WingedEdge edge = faces[i].edge;
                List<int> edge_index = new List<int>();
                List<int> vertice_index = new List<int>();
                //Edge CW
                for (int j = 0; j < 4; j++)
                {
                    edge_index.Add(edge.index);
                    edge = (faces[i] == edge.rightFace) ? edge.endCCWEdge : edge.startCCWEdge;
                }
                //Vertice CW
                foreach (var a in edge_index) 
                    vertice_index.Add((edges[a].rightFace == faces[i]) ? edges[a].startVertex.index : edges[a].endVertex.index);

            }

            CatmullClarkCreateNewPoints(out facePoints, out edgePoints, out vertexPoints);
        }


        public void CatmullClarkCreateNewPoints(out List<Vector3> facePoints, out List<Vector3> edgePoints, out List<Vector3> vertexPoints)
        {

            facePoints = new List<Vector3>();
            edgePoints = new List<Vector3>();
            vertexPoints = new List<Vector3>();
            List<Vector3> mid_point = new List<Vector3>();

            //facePoints
            for (int i = 0; i < faces.Count; i++)
            {
                WingedEdge edge = faces[i].edge;
                List<int> edge_index = new List<int>();
                List<int> vertice_index = new List<int>();
                //Edge CW
                for (int j = 0; j < 4; j++)
                {
                    edge_index.Add(edge.index);
                    edge = (faces[i] == edge.rightFace) ? edge.endCCWEdge : edge.startCCWEdge;
                }
                //Vertice CW
                foreach (var a in edge_index)   vertice_index.Add((edges[a].rightFace == faces[i]) ? edges[a].startVertex.index : edges[a].endVertex.index);

                Vector3 C = new Vector3();
                foreach (var index in vertice_index)
                    C += vertices[index].position;

                facePoints.Add( C / 4f );
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
                
                Debug.Log(edge_adj.Count);
                if (edge_adj.FindAll(edge => edge.rightFace != null && edge.leftFace != null).Count == edge_adj.Count)
                {
                    foreach (var face in edge_adj)
                        Q += (vertice == face.startVertex) ? facePoints[face.rightFace.index] : facePoints[face.leftFace.index];
                    foreach (var edge in edge_adj)
                        R += mid_point[edge.index];
                    Q = Q / (float)edge_adj.Count;
                    R = R / (float)edge_adj.Count;
                    vertexPoints.Add((1f / (float)edge_adj.Count) * Q + (2f / (float)edge_adj.Count) * R + (float)(edge_adj.Count - 3) / (float)edge_adj.Count * vertice.position);
                }
                else
                {
                    List<WingedEdge> face_adj = edge_adj.FindAll(edge => edge.leftFace == null);
                    Vector3 tot_m = new Vector3();
                    foreach (var edge in face_adj)
                        tot_m += mid_point[edge.index];
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
            
            
            edge.endCWEdge = newEdge;
            edge.endCCWEdge = newEdge;
            edge.endVertex = newVertex;
            newVertex.edge = newEdge;
        }
        public void SplitFace(Face face, Vector3 splittingPoint)
        {
            //Pour chaque face, il faut créer 4 faces, 4 nouvelles edges et une vertices central
        }

        /*public void SplitFace(Face face, Vector3 splittingPoint)
        {
            //Dictionary<ulong, WingedEdge> dico = new Dictionary<ulong, WingedEdge>();
            //WingedEdge edge;


            Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
            vertices.Add(newVertex);
            List<WingedEdge> face_edges = edges.FindAll(edge => (edge.rightFace == face && edge.endCWEdge == edge.endCCWEdge) || (edge.leftFace == face && edge.startCCWEdge == edge.startCWEdge));

            //List<WingedEdge> face_edges = edges.FindAll(edge => (edge.rightFace == face || edge.leftFace == face) && edge.startCWEdge != null && edge.endCWEdge != null);
            bool recycle = false;

            string p = "";
            for (int i = 0; i < face_edges.Count; i++)
            {
                p += $"e{face_edges[i].index}";
            }
            Dictionary<ulong, WingedEdge> dico = new Dictionary<ulong, WingedEdge>();
            WingedEdge edge;

            foreach (var e in edges)
            {
                int start = e.startVertex.index;
                int end = e.endVertex.index;

                ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                dico.Add(key, e);
            }

            p += "\n";
            for (int k = 0; k < 4; k++)
            {
                int[] quad_index = new int[4];
                quad_index[0] = newVertex.index;
                Face newFace = null;
                if (face_edges[k].rightFace == face)
                {
                    if (edges.Exists(edge => edge.startCWEdge == face_edges[k].startCWEdge && edge.startCCWEdge == face_edges[k].startCWEdge))
                        quad_index[1] = edges.Find(edge => edge.startCWEdge == face_edges[k].startCWEdge && edge.startCCWEdge == face_edges[k].startCWEdge).startVertex.index;

                    quad_index[2] = face_edges[k].startVertex.index;
                    quad_index[3] = face_edges[k].endVertex.index;

                }
                else
                {
                    if (edges.Exists(edge => edge.startCCWEdge == face_edges[k].endCWEdge && edge.startCWEdge == face_edges[k].endCWEdge))
                        quad_index[1] = edges.Find(edge => edge.startCCWEdge == face_edges[k].endCWEdge && edge.startCWEdge == face_edges[k].endCWEdge).startVertex.index;

                    quad_index[2] = face_edges[k].endVertex.index;
                    quad_index[3] = face_edges[k].startVertex.index;
                }
                if (!recycle)
                {
                    p += "RF";
                    recycle = true;
                }
                else
                {
                    p += "NF";
                    newFace = new Face(faces.Count);

                    faces.Add(newFace);
                }
                for (int j = 0; j < quad_index.Length; j++)
                {
                    if (newFace == null)
                    {

                        p += $"V{quad_index[j]}V{quad_index[(j + 1) % 4]} - ";
                        int start = quad_index[j];
                        int end = quad_index[(j + 1) % 4];
                        ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                        if (!dico.TryGetValue(key, out edge))
                        {
                            p += "Vstart = " + vertices[start].index + "Vend = " + vertices[end].index;

                            edge = new WingedEdge(edges.Count, vertices[start], vertices[end], face, null);
                            edges.Add(edge);

                            if (face.edge == null) face.edge = edge;

                            if (vertices[start].edge == null) vertices[start].edge = edge;
                            if (vertices[end].edge == null) vertices[end].edge = edge;

                            dico.Add(key, edge);
                        }
                        else
                        {
                            if (edge.startVertex == vertices[end] && edge.endVertex == vertices[start]) edge.leftFace = face;
                            if (face.edge == null) face.edge = edge;
                        }


                    }
                    else if (newFace != null)
                    {

                        p += $"V{quad_index[j]}V{quad_index[(j + 1) % 4]} - ";

                        int start = quad_index[j];
                        int end = quad_index[(j + 1) % 4];
                        ulong key = (ulong)Mathf.Min(start, end) + ((ulong)Mathf.Max(start, end) << 32);
                        if (!dico.TryGetValue(key, out edge))
                        {
                            edge = new WingedEdge(edges.Count, vertices[start], vertices[end], newFace, null);
                            edges.Add(edge);

                            if (newFace.edge == null) newFace.edge = edge;

                            if (vertices[start].edge == null) vertices[start].edge = edge;
                            if (vertices[end].edge == null) vertices[end].edge = edge;

                            dico.Add(key, edge);
                        }
                        else
                        {
                            if (edge.startVertex == vertices[start] && edge.endVertex == vertices[end]) edge.rightFace = newFace;
                            if (edge.startVertex == vertices[end] && edge.endVertex == vertices[start]) edge.leftFace = newFace;
                            if (newFace.edge == null) newFace.edge = edge;
                        }
                    }


                }


                p += "\n";
            }
                Debug.Log(p);
            



        }
        */
        public string ConvertToCSVFormat(string separator = "\t")
        {
            if (this == null) return "";
            Debug.Log("#################      WindgedEdgeMesh ConvertTOCSVFormat     #################");
            List<Vertex> vertices = this.vertices;
            List<WingedEdge> edges = this.edges;
            List<Face> faces = this.faces;
            //List<Vector3> facePoints = this.facePoints;
            //List<Vector3> edgePoints = this.edgePoints;
            //List<Vector3> vertexPoints = this.vertexPoints;

            List<string> strings = new List<string>();

            //Vertices
            foreach (var vertice in vertices)
            {
                List<WingedEdge> edges_vertice = edges.FindAll(edge => edge.startVertex == vertice || edge.endVertex == vertice);
                 List<int> face_adj = new List<int>();
                    foreach (var a in edges_vertice)
                        if (a.startVertex == vertice)
                            face_adj.Add(a.rightFace.index);
                        else if (a.leftFace != null && a.endVertex == vertice)
                            face_adj.Add(a.leftFace.index);
                int[] edges_adj = new int[edges_vertice.Count];
                for (int i = 0; i < edges_vertice.Count; i++)
                    edges_adj[i] = edges_vertice[i].index;
                Vector3 pos = vertice.position;
                strings.Add(vertice.index + separator
                    + pos.x.ToString("N03") + " " + pos.y.ToString("N03") + " " + pos.z.ToString("N03") + separator 
                    + vertice.edge.index + separator
                    + string.Join(" ", edges_adj) + separator
                    + string.Join(" ", face_adj) + separator
                    //+ vertexPoints != null ? vertexPoints[vertice.index].x.ToString("N03"):"NULL" + " "
                    //+ vertexPoints != null ? vertexPoints[vertice.index].y.ToString("N03") : "NULL" + " "
                    //+ vertexPoints != null ? vertexPoints[vertice.index].z.ToString("N03") : "NULL" 
                    + separator + separator);
            }

            for (int i = vertices.Count; i < edges.Count; i++)
                strings.Add(separator + separator + separator + separator + separator + separator + separator);

            //Edges
            for (int i = 0; i < edges.Count; i++)
            {
                Debug.Log("hello");
                strings[i] += edges[i].index + separator
                    + edges[i].startVertex.index + separator
                    + edges[i].endVertex.index + separator
                    + $"{(edges[i].leftFace != null ? edges[i].leftFace.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].rightFace != null ? edges[i].rightFace.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].startCCWEdge != null ? edges[i].startCCWEdge.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].startCWEdge != null ? edges[i].startCWEdge.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].endCWEdge != null ? edges[i].endCWEdge.index.ToString() : "NULL")}" + separator
                    + $"{(edges[i].endCCWEdge != null ? edges[i].endCCWEdge.index.ToString() : "NULL")}" + separator
                    //+ edgePoints != null ? edgePoints[i].x.ToString("N03"): "NULL" + " "
                    //+ edgePoints != null ? edgePoints[i].y.ToString("N03"): "NULL" + " "
                    //+ edgePoints != null ? edgePoints[i].z.ToString("N03") : "NULL" 
                    + separator + separator;
            }
            //Faces
            for (int i = 0; i < faces.Count; i++)
            {

                //Edge in order
                WingedEdge edge = faces[i].edge;
                int nEdges = 4;
                List<int> edge_index = new List<int>();
                List<int> vertice_index = new List<int>();
                for (int j = 0; j < nEdges; j++)
                {
                    edge_index.Add(edge.index);
                    edge = (faces[i] == edge.rightFace) ? edge.endCCWEdge : edge.startCCWEdge;
                }
                foreach (var a in edge_index)
                    vertice_index.Add((edges[a].rightFace == faces[i]) ? edges[a].startVertex.index : edges[a].endVertex.index);

                int[] face_edges = new int[edge_index.Count];
                for (int j = 0; j < edge_index.Count; j++)
                    face_edges[j] = edge_index[j];


                strings[i] += faces[i].index + separator
                    + faces[i].edge.index + separator
                    + string.Join(" ", face_edges) + separator
                    + string.Join(" ", vertice_index) + separator
                    //+ facePoints != null ? facePoints[i].x.ToString("N03") : "NULL" + " "
                    //+ facePoints != null ? facePoints[i].y.ToString("N03") : "NULL" + " "
                    //+ facePoints != null ? facePoints[i].z.ToString("N03") : "NULL" 
                    + separator + separator;
            }

            string str = "Vertex" + separator + separator + separator + separator + separator + separator + separator + "WingedEdges" + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                + "Index" + separator + "Position" + separator + "Edge" + separator + "Edges" + separator + "Face Adj" + separator + "NewVertex" + separator + separator +
                "Index" + separator + "Start Vertex" + separator + "End Vertex" + separator + "Left Face" + separator + "Right Face" + separator + "Start CCW Edge" + separator + "Start CW Edge" + separator + "End CW Edge" + separator + "End CCW Edge" + separator + "EdgePoints" + separator + separator +
                "Index" + separator + "Edge" + separator + "Edges" + separator + "Vertices" + separator + "Center\n"
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
                for (int i = 0; i < faces.Count; i++)
                {
                    WingedEdge edge = faces[i].edge;
                    List<int> edge_index = new List<int>();
                    List<int> vertice_index = new List<int>();
                    //Edge CW
                    for (int j = 0; j < 4; j++)
                    {
                        edge_index.Add(edge.index);
                        edge = (faces[i] == edge.rightFace) ? edge.endCCWEdge : edge.startCCWEdge;
                    }
                    //Vertice CW
                    foreach (var a in edge_index) vertice_index.Add((edges[a].rightFace == faces[i]) ? edges[a].startVertex.index : edges[a].endVertex.index);

                    Vector3 C = new Vector3();
                    foreach (var index in vertice_index)
                        C += vertices[index].position;

                    Handles.Label(transform.TransformPoint(C / 4f), "F" + faces[i].index, style);
                }
            }




            //edges
            style.normal.textColor = Color.blue;
            for (int i=0; i < edges.Count; i++)
            {
                Vector3 start = transform.TransformPoint(edges[i].startVertex.position);
                Vector3 end = transform.TransformPoint(edges[i].endVertex.position);
                Vector3 pos = Vector3.Lerp(start, end, 0.5f);

                Gizmos.DrawLine(start, end);

                if (drawEdges)
                    Handles.Label(pos, "e" + edges[i].index, style);


            }


        }
    }
}
