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
        public WingedEdge(int index, Vertex startVertex, Vertex endVertex, Face rightFace, Face leftFace)
        {
            this.index = index;
            this.startVertex = startVertex;
            this.endVertex = endVertex;
            this.rightFace = rightFace;
            this.leftFace = leftFace;
        }
        public Face LeftFace { set { leftFace = value; } }
        public WingedEdge StartCWEdge { set { startCWEdge = value; } }
        public WingedEdge StartCCWEdge { set { startCCWEdge = value; } }
        public WingedEdge EndCWEdge { set { endCWEdge = value; } }
        public WingedEdge EndCCWEdge { set { endCCWEdge = value; } }
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
        public WingedEdge Edge { set { edge = value; } }
           
    }
    public class Face
    {
        public int index;
        public WingedEdge edge;

        public Face(int index)
        {
            this.index = index;
        }
        public WingedEdge Edge { set { edge = value; } }
    }
    public class WingedEdgeMesh
    {
        public List<Vertex> vertices;
        public List<WingedEdge> edges;
        public List<Face> faces;
        public WingedEdgeMesh(Mesh mesh)
        {// constructeur prenant un mesh Vertex-Face en paramètre 
         // magic happens 

            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();
            Vector3[] mVertices = mesh.vertices;
            int[] quads = mesh.GetIndices(0);
            
            //dictionnaire Edge
            Dictionary<ulong, WingedEdge> dico = new Dictionary<ulong, WingedEdge>();


            //key = min(index 0, index 1) + (max(index 0, index 1) << 32 ); //cast
            Debug.Log(mesh.name);

            //vertices
            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, mVertices[i]));
                


            int index = 0;
            //pour toute les faces
            for (int i = 0; i < quads.Length/4; i++) {
                WingedEdge e;
                faces.Add(new Face(i));
                int[] arr_index = new int[]{ quads[4 * i],quads[4 * i + 1],quads[4 * i + 2],quads[4 * i + 3] };  

                
                for(int j = 0; j < arr_index.Length; j++)
                {
                    ulong key = (ulong)Mathf.Min(arr_index[j], arr_index[(j + 1) % 4]) + ((ulong)Mathf.Max(arr_index[j], arr_index[(j + 1) % 4]) << 32);
                    if (!dico.TryGetValue(key, out e))
                    {
                        edges.Add(new WingedEdge(index, vertices[arr_index[j]], vertices[arr_index[(j + 1) % 4]], faces[i], new Face(-1)));
                        faces[i].Edge = edges[index];
                        vertices[arr_index[j]].Edge = edges[index];
                        vertices[arr_index[(j + 1) % 4]].Edge = edges[index];
                        dico.Add(key, edges[index]);
                        index++;
                    }
                    else
                    {
                        edges[e.index].LeftFace = faces[i];
                        faces[i].Edge = edges[e.index];
                    }
                }


            }
            string p = "";
            for (int i = 0; i < quads.Length / 4; i++)
            {
                WingedEdge e;
                int[] arr_index = new int[] { quads[4 * i], quads[4 * i + 1], quads[4 * i + 2], quads[4 * i + 3] };
                for (int j = 0; j < arr_index.Length; j++)
                {
                    ulong key = (ulong)Mathf.Min(arr_index[j], arr_index[(j + 1) % 4]) + ((ulong)Mathf.Max(arr_index[j], arr_index[(j + 1) % 4]) << 32);
                    if (dico.TryGetValue(key, out e))
                    {
                        if (arr_index[j] == e.startVertex.index && arr_index[(j + 1) % 4] == e.endVertex.index)//CW
                        {
                            edges[e.index].StartCWEdge = edges.Find(x => x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j] || x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]);
                            edges[e.index].EndCCWEdge = edges.Find(x => (x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]) || (x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4]));
                            p += "e" + e.index + " : SCW e" + edges.Find(x => x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j] || x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]).index + " (index " + arr_index[j] + arr_index[(j - 1 + 4) % 4] + ")"
                            + " - ECCW e" + edges.Find(x => (x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]) || (x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4])).index + " (index " + arr_index[(j + 2) % 4] + arr_index[(j + 1) % 4] + ")\n";
                        }

                        else if (arr_index[j] == e.endVertex.index && arr_index[(j + 1) % 4] == e.startVertex.index)//CCW
                        {
                            edges[e.index].StartCCWEdge = edges.Find(x => x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4] || x.startVertex.index == arr_index[(j + 2) % 4] && x.endVertex.index == arr_index[(j + 1) % 4]);
                            edges[e.index].EndCWEdge = edges.Find(x => (x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]) || (x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[j]));
                            p += "e" + e.index + " : SCCW e" + edges.Find(x => x.startVertex.index == arr_index[(j + 1) % 4] && x.endVertex.index == arr_index[(j + 2) % 4] || x.startVertex.index == arr_index[(j+2)%4] && x.endVertex.index == arr_index[(j+1)%4]).index + " (index " + arr_index[(j + 2) % 4] + arr_index[(j + 1) % 4] + ")"
                            + " - ECW e" + edges.Find(x => (x.startVertex.index == arr_index[j] && x.endVertex.index == arr_index[(j - 1 + 4) % 4]) || (x.startVertex.index == arr_index[(j - 1 + 4) % 4] && x.endVertex.index == arr_index[ j ])).index + " (index " + arr_index[j] + arr_index[(j - 1 + 4) % 4] + ")\n";
                        }
                        if (e.startCWEdge == null) edges[e.index].StartCWEdge = edges[e.index].startCCWEdge;
                        if (e.startCCWEdge == null) edges[e.index].StartCCWEdge = edges[e.index].startCWEdge;
                        if (e.endCWEdge == null) edges[e.index].EndCWEdge = edges[e.index].endCCWEdge;
                        if (e.endCCWEdge == null) edges[e.index].EndCCWEdge = edges[e.index].endCWEdge;
                    }
                }
            }
            Debug.Log(p);
                //Try to find CCW and CW
                //foreach (var edge1 in edges)
                //{
                //    foreach(var edge2 in edges)
                //    {
                //        if (edge1.startVertex.index == edge2.startVertex.index )
                //            edges[edge1.index].StartCCWEdge = edge2;
                //        else if (edge1.startVertex.index == edge2.endVertex.index)
                //            edges[edge1.index].StartCWEdge = edge2;
                //        else if (edge1.endVertex.index == edge2.endVertex.index )
                //            edges[edge1.index].EndCWEdge = edge2;
                //        else if (edge1.endVertex.index == edge2.startVertex.index )
                //            edges[edge1.index].EndCCWEdge = edge2;

                //    }
                //}
                //for(int i = 0; i < 11; i++)
                //{
                //    Debug.Log("e" + edges[i].startCCWEdge.index + " - e" + edges[i].startCWEdge.index + " - e" + edges[i].endCWEdge.index + " - e" + edges[i].endCCWEdge.index);
                //}

            p = "";
            foreach ( var dic in dico)
            {
                p += "index " + dic.Key.ToString() + " => e" + dic.Value.index.ToString() + "\n";
            }
            Debug.Log(p);
            p = "";
            index = 0;
            for (int i = 0; i < quads.Length; i+=4)
            {
                p += "F"+index++ + " : " + "V"+quads[i] + " - " + "V" + quads[i + 1] + " - " + "V" + quads[i+2] + " - " + "V" + quads[i+3] + "\n";
            }
            Debug.Log(p);
            //vertices
            p = "";
            foreach (var x in vertices)
            {
                p += "V"+x.index.ToString() + ": " + x.position.ToString() + " | e" + x.edge.index +  " \n";
            }
            Debug.Log(p);
            //faces
            p = "";
            foreach (var x in faces)
            {
                p += x.index.ToString() + ": F" + x.index.ToString() + " - e" + x.edge.index + " \n";
            }
            Debug.Log(p);

            //wingedEdge

            p = "";
            foreach (var x in edges)
            {
                p += "e"+x.index.ToString() + ": V" + x.startVertex.index + " - V" + x.endVertex.index + " | F" + x.leftFace.index + " - F" + x.rightFace.index + " | SCCW : e" + x.startCCWEdge.index + " - SCW : e" + x.startCWEdge.index +" - ECW : e" + x.endCWEdge.index + " - ECCW : e" + x.endCCWEdge.index + "\n";
            }
            Debug.Log(p);





        }
        public Mesh ConvertToFaceVertexMesh()
        {
            Mesh faceVertexMesh = new Mesh();
            // magic happens 
            return faceVertexMesh;
        }
        public string ConvertToCSVFormat(string separator = "\t")
        {
            string str = "";
            //magic happens 
            return str;
        }
        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces)
        {
            //magic happens 
        }
    }
}
