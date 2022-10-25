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
            Debug.Log("#################      Constructeur de WingedEdgeMesh     #################");
            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            Dictionary<ulong, WingedEdge> dico = new Dictionary<ulong, WingedEdge>();
            WingedEdge e;

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
                        
                        if(faces[i].edge==null) faces[i].edge = edges[index];
                        
                        vertices[arr_index[j]].edge = edges[index];
                        vertices[arr_index[(j + 1) % 4]].edge = edges[index];
                        
                        dico.Add(key, edges[index]);
                        index++;
                    }
                    else
                    {
                        edges[e.index].leftFace = faces[i];
                        if (faces[i].edge == null) faces[i].edge = edges[e.index];
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
                            //edges[e.index].startCCWEdge = edges.Find(edge => e.startVertex.index == edge.startVertex.index && e.rightFace.index != edge.rightFace.index);
                            //p += "e" + e.index + " : SCW e" + edges.Find(x => x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j] || x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]).index + " (index " + arr_index[j] + arr_index[(j - 1 + 4) % 4] + ")"
                            //+ " - ECCW e" + edges.Find(x => (x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]) || (x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4])).index + " (index " + arr_index[(j + 2) % 4] + arr_index[(j + 1) % 4] + ")\n";
                        }

                        if (arr_index[j] == e.endVertex.index && arr_index[(j + 1) % 4] == e.startVertex.index)//CCW
                        {
                            edges[e.index].startCCWEdge = edges.Find(x => x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4] || x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]);
                            edges[e.index].endCWEdge = edges.Find(x => (x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]) || (x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j]));
                            //p += "e" + e.index + " : SCCW e" + edges.Find(x => x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4] || x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]).index + " (index " + arr_index[(j + 2) % 4] + arr_index[(j + 1) % 4] + ")"
                            //+ " - ECW e" + edges.Find(x => (x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]) || (x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j])).index + " (index " + arr_index[j] + arr_index[(j - 1 + 4) % 4] + ")\n";
                        }

                        if (e.leftFace == null && e.endCWEdge == null) edges[e.index].endCWEdge = edges.Find(edge => edge.startVertex == e.endVertex && edge.rightFace.index == (e.rightFace.index - 1 + faces.Count)%faces.Count);
                        if (e.leftFace == null && e.startCCWEdge== null) edges[e.index].startCCWEdge = edges.Find(edge => edge.endVertex == e.startVertex && edge.rightFace.index == (e.rightFace.index + 1) % faces.Count);
                        
                        if (e.leftFace == null && e.endCWEdge == null) edges[e.index].endCWEdge = edges[e.index].endCCWEdge;
                        if (e.leftFace == null && e.startCCWEdge== null) edges[e.index].startCCWEdge = edges[e.index].startCWEdge;


                    }
                }
            }
            //Debug.Log(p);


            //p = "Dictionary<ulong, WingedEdge> dico : \n";
            //foreach (var dic in dico)
            //{
            //    p += "key " + dic.Key.ToString() + " => value : e" + dic.Value.index.ToString() + "\n";
            //}
            //Debug.Log(p);
            //p = "Faces - Vertex : \n";
            //index = 0;
            //for (int i = 0; i < m_quads.Length; i += 4)
            //{
            //    p += "F" + index++ + " : " + "V" + m_quads[i] + " - " + "V" + m_quads[i + 1] + " - " + "V" + m_quads[i + 2] + " - " + "V" + m_quads[i + 3] + "\n";
            //}
            //Debug.Log(p);
            ////vertices
            //p = "Vertex - edges : \n";
            //foreach (var x in vertices)
            //{
            //    p += "V" + x.index.ToString() + ": " + x.position.ToString() + " | e" + x.edge.index + " \n";
            //}
            //Debug.Log(p);
            ////faces
            //p = "Faces - edges : \n";
            //foreach (var x in faces)
            //{
            //    p += x.index.ToString() + ": F" + x.index.ToString() + " - e" + x.edge.index + " \n";
            //}
            //Debug.Log(p);

            ////wingedEdge

            //p = "WingedEdges : \n";
            //foreach (var x in edges)
            //{
            //    p += $"e{x.index} : V{x.startVertex.index} - V{x.endVertex.index}| {(x.leftFace == null ? "NoLeftFace" : $"F{x.leftFace.index}")} | {(x.rightFace == null ? "NoRightFace" : $"F{x.rightFace.index}")} | SCCW : e{x.startCCWEdge.index} - SCW : e{x.startCWEdge.index} - ECW : e{x.endCWEdge.index} - ECCW : e{x.endCCWEdge.index}\n";
            //}
            //Debug.Log(p);





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
            {
                Vector3 start = edge.startVertex.position;
                Vector3 end = edge.endVertex.position;
                mid_point.Add((start + end) / 2f);
            }

            string p = "";

            foreach(var m in mid_point)
            {
                p += m + "\n";
            }
            Debug.Log(p);
            p = "";
            int index = 0;
            //vertices Points
            foreach (var vertice in vertices)
            {

                Vector3 Q = new Vector3();
                Vector3 R = new Vector3();
                List<WingedEdge> adjacents = edges.FindAll(edge => edge.startVertex == vertice || edge.endVertex == vertice);

                if (adjacents.FindAll(edge => edge.rightFace != vertice.edge.rightFace).Count > 0)
                {
                    p += "V" + vertice.index + " : ";
                    //p += $"V{vertice.index} : R = ";
                    foreach (var adjacent in adjacents)
                    {

                        p += "F" + adjacent.rightFace.index + /*$"{(adjacent.startVertex == vertice ? adjacent.rightFace.index : adjacent.leftFace.index)}" +*/ ", e" + adjacent.index + " - ";
                        Q += facePoints[adjacent.startVertex == vertice ? adjacent.rightFace.index : adjacent.leftFace!=null?adjacent.leftFace.index:adjacent.rightFace.index];
                        R += mid_point[adjacent.index];
                        p += $"R = { mid_point[adjacent.index] } - ";
                        //p += $"Q = { facePoints[edges.Exists(edge => adjacent.startVertex == vertice) ? edges.Find(edge => adjacent.startVertex == vertice && edge.index == adjacent.index).rightFace.index : edges.Find(edge => adjacent.endVertex == vertice && edge.index == adjacent.index).leftFace.index] } + ";
                    }
                    p += "\n";
                    index++;
                    Q = Q / (float)adjacents.Count;
                    R = R / (float)adjacents.Count;

                    vertexPoints.Add((1f / (float)adjacents.Count) * Q + (2f / (float)adjacents.Count) * R + (float)(adjacents.Count - 3) / (float)adjacents.Count * vertice.position);
                }
                else
                {
                    vertexPoints.Add((mid_point[adjacents[0].index] + mid_point[adjacents[1].index] + vertice.position) / 3f);
                }

                //p +=  $" R = {R.x}, {R.y}, {R.z}, {adjacents.Count}\n";
                //p += $" Q = {Q.x}, {Q.y}, {Q.z}, {adjacents.Count}\n";
            }
            Debug.Log(p);
            //Debug.Log(p);
            //p = "";
            //int cnt = 0;
            //foreach (var vp in vertexPoints)
            //{

            //    p += "new V" + cnt++ +" = " + vp + "\n";
            //}
            //Debug.Log(p);
            //cnt = 0;
            //p = "";
            //foreach(var e in edgePoints)
            //{
            //    p += "E" + cnt++ + " = " + e + "\n";
            //}
            //Debug.Log(p);
        }
        public void SplitEdge(WingedEdge edge, Vector3 splittingPoint)
        {

        }
        public void SplitFace(Face face, Vector3 splittingPoint)
        {

        }

        public string ConvertToCSVFormat(string separator = "\t")
        {
            if (this == null) return "";
            Debug.Log("#################      WindgedEdgeMesh ConvertTOCSVFormat     #################");
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
            Debug.Log(str);
            return str;
        }
        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform)
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
                    Vector3 worldPos = transform.TransformPoint(vertices[i].position);
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
                    Vector3 start = transform.TransformPoint(edge.startVertex.position);
                    Vector3 end = transform.TransformPoint(edge.endVertex.position);
                    Vector3 pos = Vector3.Lerp(start, end, 0.5f);

                    Handles.Label(pos, "e" + edge.index, style);

                }
            }
           
        }
    }
}
