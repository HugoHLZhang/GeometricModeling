using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
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
            //Cherche EndCW Edge en parcourant les edges dans le sens anti-horaire CCW.
            WingedEdge endCW = this.endCCWEdge;
            while (endCW.leftFace != null) { endCW = (endCW.endVertex == this.endVertex) ? endCW.endCCWEdge : endCW.startCCWEdge; }
            return endCW;
        }
        public WingedEdge FindBorderStartCCW()
        {
            //Cherche StartCCW Edge en parcourant les edges dans le sens horaire CW.
            WingedEdge startCCW = this.startCWEdge;
            while (startCCW.leftFace != null) { startCCW = (startCCW.startVertex == this.startVertex) ? startCCW.startCWEdge : startCCW.endCWEdge; }
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

        public List<WingedEdge> GetAdjacentEdges() //en cherchant dans le sens horaire CW
        {
            WingedEdge currentEdge = edge;
            List<WingedEdge> adjacentEdges = new List<WingedEdge>();
            do
            {
                adjacentEdges.Add(currentEdge);
                currentEdge = (this == currentEdge.startVertex) ? currentEdge.startCWEdge : currentEdge.endCWEdge;
            } while (currentEdge != edge);

            return adjacentEdges;
        }
        public List<Face> GetAdjacentFaces()
        {
            List<Face> adjacentFaces = new List<Face>();
            List<WingedEdge> adjacentEdges = GetAdjacentEdges();
            for (int i = 0; i < adjacentEdges.Count; i++)
            {
                //ignore les edges en bordure dirigés vers la vertice courante
                if ( !(this == adjacentEdges[i].endVertex && adjacentEdges[i].leftFace == null))
                {
                    //ajoute la bonne face en fonction de la direction de l'edge par rapport à la vertice courante
                    adjacentFaces.Add((this == adjacentEdges[i].startVertex) ? adjacentEdges[i].rightFace : adjacentEdges[i].leftFace);
                }
            }
            return adjacentFaces;
        }
        public List<WingedEdge> GetBorderEdges() //càd les edges sans leftFace
        {
            List<WingedEdge> borderEdges = new List<WingedEdge>();
            List<WingedEdge> adjacentEdges = GetAdjacentEdges();
            for (int i = 0; i < adjacentEdges.Count; i++)
            {
                if (adjacentEdges[i].leftFace == null) 
                { 
                    borderEdges.Add(adjacentEdges[i]); 
                }
            }
            
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
        public List<WingedEdge> GetFaceEdges() //en cherchant dans le sens horaire CW
        {
            WingedEdge currentEdge = edge;
            List<WingedEdge> faceEdges = new List<WingedEdge>();
            do
            {
                faceEdges.Add(currentEdge);
                currentEdge = (this == currentEdge.rightFace) ? currentEdge.endCCWEdge : currentEdge.startCCWEdge;
            } while (currentEdge != edge);

            return faceEdges;
        }
        public List<Vertex> GetFaceVertex() // dans le sens horaire
        {
            List<Vertex> faceVertices = new List<Vertex>();
            List<WingedEdge> faceEdges = GetFaceEdges();

            for (int i = 0; i < faceEdges.Count; i++)
            {
                faceVertices.Add((faceEdges[i].rightFace == this) ? faceEdges[i].startVertex : faceEdges[i].endVertex);
            }
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

            int nEdges= 4; //quads

            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();

            Vector3[] m_vertices = mesh.vertices;
            int[] m_quads = mesh.GetIndices(0);

            Dictionary<ulong, WingedEdge> dicoEdges = new Dictionary<ulong, WingedEdge>();
            WingedEdge wingedEdge;

            //Create Vertex
            for (int i = 0; i < mesh.vertexCount; i++) { vertices.Add(new Vertex(i, m_vertices[i])); }

            //Create Face and WingedEdge
            for (int i = 0; i < m_quads.Length/ nEdges; i++) 
            {
                Face face = new Face(faces.Count);
                faces.Add(face);
                
                //quad's vertices index
                int[] quad_index = new int[nEdges];

                for (int j = 0; j < nEdges; j++) { quad_index[j] = m_quads[nEdges * i + j]; }

                List<WingedEdge> faceEdges = new List<WingedEdge>();

                for (int j = 0; j < quad_index.Length; j++)
                {
                    int start = quad_index[j];
                    int end = quad_index[(j + 1) % nEdges];

                    ulong key = (ulong)Mathf.Min(start,end) + ((ulong)Mathf.Max(start, end) << 32);

                    if (!dicoEdges.TryGetValue(key, out wingedEdge)) 
                    {
                        //create wingedEdge
                        wingedEdge = new WingedEdge(edges.Count, vertices[start], vertices[end], face, null);
                        edges.Add(wingedEdge);
                        dicoEdges.Add(key, wingedEdge);
                    }
                    else
                    {
                        //complete wingedEdge information
                        wingedEdge.leftFace = face;
                    }

                    //Vertex
                    if (vertices[start].edge == null)   vertices[start].edge = wingedEdge;
                    if (vertices[end].edge == null)     vertices[end].edge = wingedEdge;
                    
                    //Faces
                    if(face.edge == null) face.edge = wingedEdge;

                    faceEdges.Add(wingedEdge);
                    
                }
                //Complete CCW and CW Edge
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
            //Complete borderEdges's CCW and CW Edge 
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
        public void RemoveFace()
        {
            //remove face inner only
            int face_index = Random.Range(0, faces.Count);
            bool canRemove = false;
            //every edge of the face has left and right face.
            Face currFace = null;
            List<WingedEdge> faceEdges = new List<WingedEdge>();
            while (canRemove == false)
            {
                currFace = faces[face_index];
                faceEdges = currFace.GetFaceEdges();
                List<Vertex> faceVertex = currFace.GetFaceVertex();

                for (int i = 0; i < faceVertex.Count; i++)
                {
                    if (faceVertex[i].GetBorderEdges().Count == 0)
                    {
                        canRemove = true;
                    }
                    else
                    {
                        canRemove = false;
                        face_index = Random.Range(0, faces.Count);
                        break;
                    }
                }

                //faceEdges = currFace.GetFaceEdges();


                //for (int i = 0; i < faceEdges.Count; i++)
                //{
                //    if (faceEdges[i].leftFace == null)
                //    {
                //        canRemove = false;
                //        face_index = Random.Range(0, faces.Count);
                //        break;
                //    }
                //    else
                //    {
                //        canRemove = true;
                //    }
                //}

            }



            for (int i = 0; i < faceEdges.Count; i++)
            {
                if (faceEdges[i].rightFace == currFace)
                {
                    faceEdges[i].rightFace = faceEdges[i].leftFace;
                    faceEdges[i].leftFace = null;
                    
                    
                    Vertex start = faceEdges[i].startVertex;
                    Vertex end = faceEdges[i].endVertex;

                    faceEdges[i].startVertex = end;
                    faceEdges[i].endVertex = start;

                    WingedEdge startCW = faceEdges[i].startCWEdge;
                    WingedEdge startCCW = faceEdges[i].startCCWEdge;
                    WingedEdge endCW= faceEdges[i].endCWEdge;
                    WingedEdge endCCW = faceEdges[i].endCCWEdge;
                    
                    faceEdges[i].startCWEdge = endCW;
                    faceEdges[i].startCCWEdge = endCCW;
                    faceEdges[i].endCCWEdge = startCCW;
                    faceEdges[i].endCWEdge = startCW;
                }
                if(faceEdges[i].leftFace ==currFace)
                {
                    faceEdges[i].leftFace = null;
                }
            }


            faces.Remove(currFace);

            for (int i = currFace.index; i < faces.Count; i++)
            {
                faces[i].index--;
            }


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

                //pour toutes les vertices possédant autant d’edges incidentes que de faces adjacentes.
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

            /* Raison du Reorder, on prend comme exemple la face à recycler:
             * - Cas 1 (face.edge.leftFace = face) :
             * La première edge de la liste faceEdges correspondra à la rightEdge et la seconde à la bottomEdge de la face à recycler.
             * Ici il n'y a pas de problème, l'ordre de la liste dans ce cas 1 nous facilite le travail pour recycler et créer les nouvellles faces.
             * - Cas 2 (face.edge.rightFace = face) :
             * Le dernier élément de la liste faceEdges correspondra à la rightEdge et le premier élément à la bottomEdge.
             * Ici l'ordre n'est pas idéal...
             * Pour simplifier l'utilisation des liste de faceEdges et faceVertex, on décale le dernier élément à la premiere position de la liste pour obtenir une liste dans le même ordre que le cas 1.
             */
            if (face.edge.rightFace == face)
            {
                faceVertex.Insert(0, faceVertex[faceVertex.Count-1]);
                faceVertex.RemoveAt(faceVertex.Count-1);

                faceEdges.Insert(0, faceEdges[faceEdges.Count - 1]);
                faceEdges.RemoveAt(faceEdges.Count - 1);
            }
            for (int i = 0; i < faceEdges.Count; i += 2)
            {
                //Add newFace if old isRecycled
                if (isRecycled)
                {
                    currentFace = new Face(faces.Count);
                    faces.Add(currentFace);
                }
                WingedEdge rightEdge = faceEdges[i];
                WingedEdge bottomEdge = faceEdges[(i + 1) % faceEdges.Count];
                WingedEdge topEdge;
                WingedEdge leftEdge;

                WingedEdge rightEdgePrevEdge = faceEdges[(i - 1 + faceEdges.Count) % faceEdges.Count];
                WingedEdge bottomEdgeNextEdge = faceEdges[(i + 2) % faceEdges.Count];

                if (!isRecycled) //edges de face recyclé
                {
                    topEdge = new WingedEdge(edges.Count, newVertex, faceVertex[i], currentFace, null, null, null, rightEdgePrevEdge, rightEdge);
                    edges.Add(topEdge);
                    leftEdge = new WingedEdge(edges.Count, newVertex, faceVertex[i + 2], null, currentFace, null, null, bottomEdge, bottomEdgeNextEdge);
                    edges.Add(leftEdge);
                }
                else
                {
                    if (i == 6) //edges de la last face
                    {
                        topEdge = edges[edges.Count - 1];
                        leftEdge = edges[edges.Count - 4];

                        topEdge.rightFace = currentFace;
                        leftEdge.leftFace = currentFace;
                    }
                    else //edges de la 2e et 3e face
                    {
                        topEdge = edges[edges.Count - 1];
                        topEdge.rightFace = currentFace;

                        leftEdge = new WingedEdge(edges.Count, newVertex, faceVertex[i + 2], null, currentFace, null, null, bottomEdge, bottomEdgeNextEdge);
                        edges.Add(leftEdge);
                    }
                }

                //connect topEdge and leftEdge
                topEdge.startCWEdge = leftEdge;
                leftEdge.startCCWEdge = topEdge;

                //update rightEdge
                if (rightEdge.startVertex == topEdge.endVertex) { rightEdge.startCWEdge = topEdge; } else { rightEdge.endCWEdge = topEdge; }
                if (rightEdge.startVertex == topEdge.endVertex) { rightEdge.rightFace = currentFace; } else { rightEdge.leftFace = currentFace; }

                //update bottomEdge
                if (bottomEdge.startVertex == leftEdge.endVertex) { bottomEdge.startCCWEdge = leftEdge; } else { bottomEdge.endCCWEdge = leftEdge; }
                if (bottomEdge.startVertex == leftEdge.endVertex) { bottomEdge.leftFace = currentFace; } else { bottomEdge.rightFace = currentFace; }

                //complete currentFace.edge and newVertex.edge
                if(currentFace.edge == null) currentFace.edge = topEdge;
                newVertex.edge = topEdge;

                isRecycled = true;
            }
        }
        public string ConvertToCSVFormat(string separator = "\t")
        {
            if (this == null) return "";
            List<string> strings = new List<string>();

            //Vertices
            for (int i = 0; i < vertices.Count; i++)
            {
                List<WingedEdge> adjacentEdges = vertices[i].GetAdjacentEdges();
                List<Face> adjacentFaces = vertices[i].GetAdjacentFaces();
                List<WingedEdge> borderEdges = vertices[i].GetBorderEdges();

                List<int> edgesIndex = new List<int>();
                List<int> facesIndex = new List<int>();
                List<int> borderEdgesIndex = new List<int>();

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
            {
                strings.Add(separator + separator + separator + separator + separator + separator + separator);
            }

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
