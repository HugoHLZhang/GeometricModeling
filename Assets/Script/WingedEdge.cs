using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;

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
        public WingedEdge FindBorderEndCW()
        {
            //Find Last endCCW with no leftFace
            WingedEdge endCW = this.endCCWEdge;
            
            while(endCW.leftFace != null)
                endCW = endCW.endVertex == this.endVertex ? endCW.endCCWEdge : endCW.startCCWEdge;
            
            return endCW;
        }
        public WingedEdge FindBorderStartCCW()
        {
            //Find Last startCW with no leftFace
            WingedEdge startCCW = this.startCWEdge;

            while(startCCW.leftFace != null)
                startCCW = startCCW.startVertex == this.startVertex ? startCCW.startCCWEdge : startCCW.endCWEdge;
            
            return startCCW;
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

            do
            {
                adjacentEdges.Add(wingedEdge);
                wingedEdge = (this == wingedEdge.startVertex) ? wingedEdge.startCWEdge : wingedEdge.endCWEdge;
            } while (wingedEdge != edge);
            
            return adjacentEdges;
        }
        public List<Face> GetAdjacentFaces()
        {
            List<WingedEdge> adjacentEdges = GetAdjacentEdges();
            List<WingedEdge> borderEdges = GetBorderEdges();
            List<Face> adjacentFaces = new List<Face>();
            //vertex not in border => nb adjacent Edges == nb adjacent Faces
            if(borderEdges.Count == 0)
            {
                for (int i = 0; i < adjacentEdges.Count; i++)
                    adjacentFaces.Add(this == adjacentEdges[i].startVertex ? adjacentEdges[i].rightFace : adjacentEdges[i].leftFace);
            }
            //vertex in border
            else
            {
                for (int i = 0; i < adjacentEdges.Count; i++)
                    if(!adjacentFaces.Contains(adjacentEdges[i].rightFace)) adjacentFaces.Add(adjacentEdges[i].rightFace);
            }
            return adjacentFaces;
        }
        public List<WingedEdge> GetBorderEdges()
        {
            List<WingedEdge> borderEdges = new List<WingedEdge>();
            List<WingedEdge> adjacentEdges = GetAdjacentEdges();
            for (int i = 0; i < adjacentEdges.Count; i++)
                if (adjacentEdges[i].leftFace == null) borderEdges.Add(adjacentEdges[i]);
            
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
            do
            {
                faceEdges.Add(wingedEdge);
                wingedEdge = (this == wingedEdge.rightFace) ? wingedEdge.endCCWEdge : wingedEdge.startCCWEdge;
            } while (wingedEdge != edge);

            return faceEdges;
        }
        public List<Vertex> GetFaceVertex()
        {
            List<WingedEdge> faceEdges = GetFaceEdges();
            List<Vertex> faceVertices = new List<Vertex>();
            //Vertice CW
            for (int i = 0; i < faceEdges.Count; i++)
                faceVertices.Add((faceEdges[i].rightFace == this) ? faceEdges[i].startVertex : faceEdges[i].endVertex);
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

            //Create Vertex
            for (int i = 0; i < mesh.vertexCount; i++)
                vertices.Add(new Vertex(i, m_vertices[i]));

            //Create Face and WingedEdge
            for (int i = 0; i < m_quads.Length/ nEdges; i++) {

                Face face = new Face(faces.Count);
                faces.Add(face);
                
                //quad's vertices index
                int[] quad_index = new int[nEdges];
                for(int j = 0; j < 4; j++)
                    quad_index[j] = m_quads[nEdges * i + j];

                List<WingedEdge> faceEdges = new List<WingedEdge>();

                for (int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nEdges];

                    ulong key = (ulong)Mathf.Min(start,end) + ((ulong)Mathf.Max(start, end) << 32);

                    //If not in dico create a newEdge
                    if (!dicoEdges.TryGetValue(key, out wingedEdge)) 
                    {
                        wingedEdge = new WingedEdge(edges.Count, vertices[start], vertices[end], face, null);
                        edges.Add(wingedEdge);

                        dicoEdges.Add(key, wingedEdge);
                    }
                    //otherwise complete the edge info
                    else 
                        wingedEdge.leftFace = face;

                    //update vertex edge
                    if (vertices[start].edge == null)   vertices[start].edge = wingedEdge;
                    if (vertices[end].edge == null)     vertices[end].edge = wingedEdge;
                    //update face edge
                    if (face.edge == null)              face.edge = wingedEdge;

                    faceEdges.Add(wingedEdge);
                    
                }
                //Update CCW and CW Edge
                for (int j = 0; j < faceEdges.Count; j++)
                {
                    if (faceEdges[j].rightFace == face)
                    {
                        faceEdges[j].startCWEdge    = faceEdges[(j - 1 + faceEdges.Count) % faceEdges.Count];
                        faceEdges[j].endCCWEdge     = faceEdges[(j + 1) % faceEdges.Count];
                    }
                    if (faceEdges[j].leftFace == face)
                    {
                        faceEdges[j].endCWEdge      = faceEdges[(j - 1 + faceEdges.Count) % faceEdges.Count];
                        faceEdges[j].startCCWEdge   = faceEdges[(j + 1) % faceEdges.Count];
                    }
                }

            }
            //Update CCW and CW Edge for borderEdges
            for (int i = 0; i < edges.Count; i++)
            {
                if(edges[i].leftFace == null)
                {
                    edges[i].startCCWEdge = edges[i].FindBorderStartCCW();
                    edges[i].endCWEdge = edges[i].FindBorderEndCW();
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
        public void SubdivideCatmullClark()
        {
            Debug.Log("#################                    WindgedEdgeMesh SubdivideCatmullClark                   #################");
            List<Vector3> facePoints;
            List<Vector3> edgePoints;
            List<Vector3> vertexPoints;

            CatmullClarkCreateNewPoints(out facePoints, out edgePoints, out vertexPoints);

            string p = "";
            for (int i = 0; i < edgePoints.Count; i++)
            {
                p += "e" + i + " = " + edgePoints[i] + "\n";
            }
            Debug.Log(p);

            string f = "";
            for (int i = 0; i < facePoints.Count; i++)
            {
                f += "F" + i + " = " + facePoints[i] + "\n";
            }
            Debug.Log(f);

            string v = "";
            for (int i = 0; i < vertexPoints.Count; i++)
            {
                v += "V" + i + " = " + vertexPoints[i] + "\n";
            }
            Debug.Log(v);


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
            List<Vector3> midPoints= new List<Vector3>();

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
                midPoints.Add((edges[i].startVertex.position + edges[i].endVertex.position) / 2f);
                edgePoints.Add(edges[i].leftFace != null ? (edges[i].startVertex.position + edges[i].endVertex.position + facePoints[edges[i].rightFace.index] + facePoints[edges[i].leftFace.index]) / 4f : midPoints[i]);
            }

            //Vertex Points
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 Q = new Vector3();
                Vector3 R = new Vector3();

                List<WingedEdge> adjacentEdges = vertices[i].GetAdjacentEdges();
                List<Face> adjacentFaces = vertices[i].GetAdjacentFaces();

                //toutes les vertices possédant autant d’edges incidentes que de faces adjacentes.
                if (adjacentEdges.Count == adjacentFaces.Count)
                {
                    float n = adjacentFaces.Count;
                    for (int j = 0; j < adjacentEdges.Count; j++)
                    {
                        Q += (vertices[i] == adjacentEdges[j].startVertex) ? facePoints[adjacentEdges[j].rightFace.index] : facePoints[adjacentEdges[j].leftFace.index];
                        R += midPoints[adjacentEdges[j].index];
                    }
                    Q = Q / n;
                    R = R / n;

                    vertexPoints.Add(( Q / n) + (2f * R  / n)  + ((n - 3f) * vertices[i].position / n) );
                }
                //pour les vertices en bordure
                else
                {
                    List<WingedEdge> borderEdges = vertices[i].GetBorderEdges();
                    Vector3 tot_m = new Vector3();

                    for (int j = 0; j < borderEdges.Count; j++)
                        tot_m += midPoints[borderEdges[j].index];

                    vertexPoints.Add((tot_m + vertices[i].position) / 3f);
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
            if (edge.endCWEdge.startCCWEdge == edge)    edge.endCWEdge.startCCWEdge = newEdge;
            if (edge.endCCWEdge.startCWEdge == edge)    edge.endCCWEdge.startCWEdge = newEdge;
            if (edge.endCWEdge.endCCWEdge == edge)      edge.endCWEdge.endCCWEdge   = newEdge;
            if (edge.endCCWEdge.endCWEdge == edge)      edge.endCCWEdge.endCWEdge   = newEdge;

            //update edge endVertex
            edge.endVertex      = newVertex;
            edge.endVertex.edge = newEdge;
            
            //update edge endCCW and CW
            edge.endCCWEdge     = newEdge;
            edge.endCWEdge      = newEdge;

            //update newVertex and newEdge
            newVertex.edge          = newEdge;
            newEdge.endVertex.edge  = newEdge;
        }
        public void SplitFace(Face face, Vector3 splittingPoint)
        {
            bool isRecycled = false;

            Face currentFace = face;

            Vertex newVertex = new Vertex(vertices.Count, splittingPoint);
            vertices.Add(newVertex);

            List<WingedEdge> faceEdges= face.GetFaceEdges();
            List<Vertex> faceVertex = face.GetFaceVertex();

            //Reorder Lists
            //Last to first : easier to split face once reordered to update recycleFace and create newFace  
            if (face.edge.rightFace == face)
            {
                faceVertex.Insert(0, faceVertex[faceVertex.Count-1]);
                faceVertex.RemoveAt(faceVertex.Count-1);

                faceEdges.Insert(0, faceEdges[faceEdges.Count - 1]);
                faceEdges.RemoveAt(faceEdges.Count - 1);
            }

            string v = "";
            for (int i = 0; i < faceVertex.Count; i++)
            {
                v += "V" + faceVertex[i].index;
            }
            v += "\n";
            for (int i = 0; i < faceEdges.Count; i++)
            {
                v += "e" + faceEdges[i].index;
            }
            Debug.Log(v);
            

            for (int i = 0; i < faceEdges.Count; i+=2)
            {
                
                //Add newFace if old isRecycled
                if (isRecycled)
                {
                    currentFace = new Face(faces.Count);
                    faces.Add(currentFace);
                }


                WingedEdge rightEdge = faceEdges[i];
                WingedEdge rightEdgePrevEdge = faceEdges[(i - 1 + faceEdges.Count) % faceEdges.Count];
                WingedEdge bottomEdge = faceEdges[(i + 1) % faceEdges.Count];
                WingedEdge bottomEdgeNextEdge = faceEdges[(i + 2) % faceEdges.Count];

                WingedEdge topEdge;
                WingedEdge leftEdge;
                switch (i)
                {
                    case 0:
                        topEdge = new WingedEdge(edges.Count, newVertex, faceVertex[i], currentFace, null, null, null, rightEdgePrevEdge, rightEdge);
                        edges.Add(topEdge);
                        leftEdge = new WingedEdge(edges.Count, newVertex, faceVertex[i + 2], null, currentFace, null, null, bottomEdge, bottomEdgeNextEdge);
                        edges.Add(leftEdge);

                        topEdge.startCWEdge = leftEdge;
                        topEdge.endCWEdge = rightEdgePrevEdge;
                        topEdge.endCCWEdge = rightEdge;



                        leftEdge.startCCWEdge = topEdge;
                        leftEdge.endCWEdge = bottomEdge;
                        leftEdge.endCCWEdge = bottomEdgeNextEdge;

                        if (rightEdgePrevEdge.startVertex == topEdge.endVertex) rightEdgePrevEdge.startCCWEdge = topEdge;
                        if (rightEdgePrevEdge.endVertex == topEdge.endVertex) rightEdgePrevEdge.endCCWEdge = topEdge;

                        if (rightEdge.startVertex == topEdge.endVertex) rightEdge.startCWEdge = topEdge;
                        if (rightEdge.endVertex == topEdge.endVertex) rightEdge.endCWEdge = topEdge;

                        if (bottomEdge.startVertex == leftEdge.endVertex) bottomEdge.startCCWEdge = leftEdge;
                        if (bottomEdge.endVertex == leftEdge.endVertex) bottomEdge.endCCWEdge = leftEdge;

                        if (bottomEdgeNextEdge.startVertex == leftEdge.endVertex) bottomEdgeNextEdge.startCWEdge = leftEdge;
                        if (bottomEdgeNextEdge.endVertex == leftEdge.endVertex) bottomEdgeNextEdge.endCWEdge = leftEdge;

                        newVertex.edge = topEdge;
                        isRecycled = true;
                        break;

                    case 6:

                        topEdge = edges[edges.Count - 1];
                        leftEdge = edges[edges.Count - 4];

                        topEdge.rightFace = currentFace;
                        leftEdge.leftFace = currentFace;

                        topEdge.startCWEdge = leftEdge;
                        leftEdge.startCCWEdge = topEdge;

                        if (rightEdge.startVertex == topEdge.endVertex) rightEdge.rightFace = currentFace;
                        if (rightEdge.endVertex == topEdge.endVertex) rightEdge.leftFace = currentFace;

                        if (bottomEdge.startVertex == leftEdge.endVertex) bottomEdge.leftFace = currentFace;
                        if (bottomEdge.endVertex == leftEdge.endVertex) bottomEdge.rightFace = currentFace;

                        currentFace.edge = topEdge;

                        break;
                    default:

                        topEdge = edges[edges.Count - 1];
                        topEdge.rightFace = currentFace;

                        leftEdge = new WingedEdge(edges.Count, newVertex, faceVertex[i + 2], null, currentFace, null, null, bottomEdge, bottomEdgeNextEdge);
                        edges.Add(leftEdge);

                        topEdge.startCWEdge = leftEdge;

                        leftEdge.startCCWEdge = topEdge;
                        leftEdge.endCWEdge = bottomEdge;
                        leftEdge.endCCWEdge = bottomEdgeNextEdge;

                        if (rightEdge.startVertex == topEdge.endVertex) rightEdge.rightFace = currentFace;
                        if (rightEdge.endVertex == topEdge.endVertex) rightEdge.leftFace = currentFace;

                        if (bottomEdge.startVertex == leftEdge.endVertex)
                        {
                            bottomEdge.startCCWEdge = leftEdge;
                            bottomEdge.leftFace = currentFace;
                        }
                        if (bottomEdge.endVertex == leftEdge.endVertex)
                        {
                            bottomEdge.endCCWEdge = leftEdge;
                            bottomEdge.rightFace = currentFace;
                        }

                        if (bottomEdgeNextEdge.startVertex == leftEdge.endVertex) bottomEdgeNextEdge.startCWEdge = leftEdge;
                        if (bottomEdgeNextEdge.endVertex == leftEdge.endVertex) bottomEdgeNextEdge.endCWEdge = leftEdge;

                        currentFace.edge = topEdge;
                        break;

                }



                Debug.Log(i);
                ////newEdge's usefull information
                //WingedEdge endCWEdge = faceEdges[(i - 1 + faceEdges.Count) % faceEdges.Count];
                //WingedEdge endCCWEdge = faceEdges[i];
                //WingedEdge parallelEdge = faceEdges[(i + 1) % faceEdges.Count];

                ////Create newEdge
                //WingedEdge newEdge = new WingedEdge(edges.Count, newVertex, faceVertex[i], currentFace, null, null, null, endCWEdge, endCCWEdge);
                //edges.Add(newEdge);


                ////Update for every split Face

                ////Update Vertex's and Face's edge
                //newVertex.edge = newEdge;
                //if (currentFace.edge == null) currentFace.edge = newEdge;


                ////Update des edges adjacentes à la endVertex de la nouvelle Edge
                //if (endCWEdge.endVertex == newEdge.endVertex) endCWEdge.endCCWEdge = newEdge;
                //if (endCWEdge.startVertex == newEdge.endVertex) endCWEdge.startCCWEdge = newEdge;


                //if (endCCWEdge.startVertex == newEdge.endVertex) endCCWEdge.startCWEdge = newEdge;
                //if (endCCWEdge.endVertex == newEdge.endVertex) endCCWEdge.endCWEdge = newEdge;


                ////Update for the newFace
                //if (isRecycled)
                //{
                //    //Update rightFace's & leftFace's edge
                //    if (endCCWEdge.startVertex == newEdge.endVertex)
                //    {
                //        endCCWEdge.rightFace = currentFace;
                //        if (parallelEdge.startVertex == endCCWEdge.endVertex) parallelEdge.rightFace = currentFace;
                //        if (parallelEdge.endVertex == endCCWEdge.endVertex) parallelEdge.leftFace = currentFace;
                //    }
                //    if (endCCWEdge.endVertex == newEdge.endVertex)
                //    {
                //        endCCWEdge.leftFace = currentFace;
                //        if (parallelEdge.startVertex == endCCWEdge.startVertex) parallelEdge.rightFace = currentFace;
                //        if (parallelEdge.endVertex == endCCWEdge.startVertex) parallelEdge.leftFace = currentFace;
                //    }

                //    //Update newEdge startCCW/startCWEdge and leftFace
                //    newEdge.startCCWEdge = edges[edges.Count - 2];
                //    edges[edges.Count - 2].startCWEdge = newEdge;
                //    newEdge.leftFace = edges[edges.Count - 2].rightFace;

                //    //Update the last split of the Face - complete missing information of the first newEdge created
                //    //startCCW/startCWEdge and leftFace 
                //    if (i == 6)
                //    {
                //        newEdge.startCWEdge = edges[edges.Count - 4];
                //        edges[edges.Count - 4].startCCWEdge = newEdge;
                //        edges[edges.Count - 4].leftFace = currentFace;
                //    }


                //}

                //isRecycled = true;
            }
        }
        public string ConvertToCSVFormat(string separator = "\t")
        {
            if (this == null) return "";

            List<string> strings = new List<string>();

            //Vertices
            for (int i = 0; i < vertices.Count; i++)
            {
                List<WingedEdge> adjacentEdges  = vertices[i].GetAdjacentEdges();
                List<Face>       adjacentFaces  = vertices[i].GetAdjacentFaces();
                List<WingedEdge> borderEdges    = vertices[i].GetBorderEdges();

                List<int> edgesIndex        = new List<int>();
                List<int> facesIndex        = new List<int>();
                List<int> borderEdgesIndex  = new List<int>();
                
                for (int j = 0; j < adjacentEdges.Count; j++)
                    edgesIndex.Add(adjacentEdges[j].index);

                for (int j = 0; j < adjacentFaces.Count; j++)
                    facesIndex.Add(adjacentFaces[j].index);

                for (int j = 0; j < borderEdges.Count; j++)
                    borderEdgesIndex.Add(borderEdges[j].index);

                strings.Add(vertices[i].index + separator
                            + vertices[i].position.x.ToString("N03") + " " 
                            + vertices[i].position.y.ToString("N03") + " " 
                            + vertices[i].position.z.ToString("N03") + separator
                            + vertices[i].edge.index + separator
                            + string.Join(" ", edgesIndex) + separator
                            + string.Join(" ", facesIndex) + separator
                            + string.Join(" ", borderEdgesIndex)
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
                            + $"{(edges[i].leftFace     != null ? edges[i].leftFace.index.ToString()    : "NULL")}" + separator
                            + $"{(edges[i].rightFace    != null ? edges[i].rightFace.index.ToString()   : "NULL")}" + separator
                            + $"{(edges[i].startCCWEdge != null ? edges[i].startCCWEdge.index.ToString(): "NULL")}" + separator
                            + $"{(edges[i].startCWEdge  != null ? edges[i].startCWEdge.index.ToString() : "NULL")}" + separator
                            + $"{(edges[i].endCWEdge    != null ? edges[i].endCWEdge.index.ToString()   : "NULL")}" + separator
                            + $"{(edges[i].endCCWEdge   != null ? edges[i].endCCWEdge.index.ToString()  : "NULL")}" + separator 
                            + separator;
            }

            //Faces
            for (int i = 0; i < faces.Count; i++)
            {
                List<WingedEdge> faceEdges = faces[i].GetFaceEdges();
                List<Vertex> faceVertex = faces[i].GetFaceVertex();

                List<int> edgesIndex = new List<int>();
                List<int> vertexIndex = new List<int>();
                //Edge CW
                for (int j = 0; j < faceEdges.Count; j++)
                    edgesIndex.Add(faceEdges[j].index);
                
                //Vertice CW
                for (int j = 0; j < faceVertex.Count; j++)
                    vertexIndex.Add(faceVertex[j].index);

                strings[i] += faces[i].index + separator
                            + faces[i].edge.index + separator
                            + string.Join(" ", edgesIndex) + separator
                            + string.Join(" ", vertexIndex) + separator 
                            + separator;
            }

            string str  = "Vertex" + separator + separator + separator + separator + separator + separator + separator + "WingedEdges" + separator + separator + separator + separator + separator + separator + separator + separator + separator + separator + "Faces\n"
                        + "Index" + separator + "Position" + separator + "Edge" + separator + "Edges Adj" + separator + "Face Adj" + separator + "Border Edges" + separator + separator 
                        + "Index" + separator + "Start Vertex" + separator + "End Vertex" + separator + "Left Face" + separator + "Right Face" + separator + "Start CCW Edge" + separator + "Start CW Edge" + separator + "End CW Edge" + separator + "End CCW Edge" + separator + separator 
                        + "Index" + separator + "Edge" + separator + "CW Edges" + separator + "CW Vertices\n"
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
                for (int i = 0; i < vertices.Count; i++)
                    Handles.Label(transform.TransformPoint(vertices[i].position), "V" + vertices[i].index, style);
            }

            //faces
            if (drawFaces)
            {
                style.normal.textColor = Color.magenta;
                for (int i = 0; i < faces.Count; i++)
                {
                    List<Vertex> faceVertex = faces[i].GetFaceVertex();
                    Vector3 C = new Vector3();
                    for (int j = 0; j < faceVertex.Count; j++)
                    {
                        Gizmos.DrawLine(transform.TransformPoint(faceVertex[j].position), transform.TransformPoint(faceVertex[(j + 1) % faceVertex.Count].position));
                        C += faceVertex[j].position;
                    }
                    Handles.Label(transform.TransformPoint(C / 4f), "F" + faces[i].index, style);
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
