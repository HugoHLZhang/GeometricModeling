using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WingedEdge;
using HalfEdge;

[RequireComponent(typeof(MeshFilter))]

public class MeshGeneratorTriangles : MonoBehaviour
{
    delegate Vector3 ComputePosDelegate(float kx, float kz);
    MeshFilter m_Mf;
    WingedEdgeMesh m_WingedEdgeMesh;
    HalfEdgeMesh m_HalfEdgeMesh;
    [SerializeField] private float m_x;
    [SerializeField] private float m_y;
    [SerializeField] private float m_z;
    [SerializeField] private int m_nSectors;

    [SerializeField] bool m_DisplayMeshInfo = true;
    [SerializeField] bool m_DisplayMeshEdges = true;
    [SerializeField] bool m_DisplayMeshVertices = true;
    [SerializeField] bool m_DisplayMeshFaces = true;

    [SerializeField] AnimationCurve m_Profile;
    private void Start()
    {
        m_Mf = GetComponent<MeshFilter>();
        m_Mf.mesh = CreateGridXZ(3, 4, new Vector3(4, 0, 4));
        m_Mf.mesh = CreateNormalizedGridXZ(20, 40,
            (kX, kZ) =>
            {
                float rho, theta, y;

                // coordinates mapping de (kX,kZ) -> (rho,theta,y)
                theta = kX * 2 * Mathf.PI;
                y = kZ * 6;
                //rho = 3 + .25f * Mathf.Sin(kZ*2*Mathf.PI*4) ;
                rho = m_Profile.Evaluate(kZ) * 2;
                return new Vector3(rho * Mathf.Cos(theta), y, rho * Mathf.Sin(theta));
                //return new Vector3(Mathf.Lerp(-1.5f, 5.5f, kX), 1, Mathf.Lerp(-2, 4, kZ));
            }
        );

    }

    Mesh CreateTriangle()
    {
        Mesh mesh = new Mesh();
        mesh.name = "triangle";

        Vector3[] vertices = new Vector3[3];
        int[] triangles = new int[1 * 3];

        vertices[0] = Vector3.right; // (1,0,0)
        vertices[1] = Vector3.up; // (0,1,0)
        vertices[2] = Vector3.forward; // (0,0,1)

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    Mesh CreateQuad(Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "quad";

        Vector3[] vertices = new Vector3[4];
        int[] triangles = new int[2 * 3];

        vertices[0] = new Vector3(-halfSize.x, 0, -halfSize.z);
        vertices[1] = new Vector3(-halfSize.x, 0, halfSize.z);
        vertices[2] = new Vector3(halfSize.x, 0, halfSize.z);
        vertices[4] = new Vector3(halfSize.x, 0, -halfSize.z);

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    Mesh CreateStrip(int nSegments, Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "strip";

        Vector3[] vertices = new Vector3[(nSegments + 1) * 2];
        int[] triangles = new int[nSegments * 2 * 3];

        int index = 0;
        Vector3 leftTopPos = new Vector3(-halfSize.x, 0, halfSize.z); 
        Vector3 rightTopPos= new Vector3(halfSize.x, 0, halfSize.z); 
        //bouble for pour remplir vertices
        for(int i = 0; i < nSegments+1; i++)
        {
            float k = (float)i / nSegments;
            Vector3 tmpPos = Vector3.Lerp(leftTopPos, rightTopPos, k);
            vertices[index++] = tmpPos;
            vertices[index++] = tmpPos - 2 * halfSize.z * Vector3.forward;
        }
        index = 0;
        //boucle for pour remplir triangles
        for(int i = 0; i < nSegments; i++)
        {

            triangles[index++] = 2*i;
            triangles[index++] = 2*i+2;
            triangles[index++] = 2*i+1;
                                 
            triangles[index++] = 2*i+1;
            triangles[index++] = 2*i+2;
            triangles[index++] = 2 * i + 3;

        }


        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }
    Mesh CreateGridXZ(int nSegmentsX, int nSegmentsZ, Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "grid";

        Vector3[] vertices = new Vector3[ (nSegmentsX + 1) * (nSegmentsZ + 1)];
        int[] triangles = new int[nSegmentsX * 2 * nSegmentsZ * 3];


        int index = 0;
        Vector3 leftTopPos = new Vector3(-halfSize.x, 0, halfSize.z);
        //Vector3 rightTopPos = new Vector3(halfSize.x, 0, halfSize.z);
        Vector3 leftBottomPos = new Vector3(-halfSize.x, 0, -halfSize.z);
        //bouble for pour remplir vertices
        /*
        0   4   8   12
        1   5   9   13  
        2   6   10  14
        3   7   11  15
         */
        /*for (int i = 0; i < nSegmentsX + 1; i++)
        {
            float kx = (float)i / nSegmentsX;
            Vector3 tmpTopPos = Vector3.Lerp(leftTopPos, rightTopPos, kx);
            Vector3 tmpBottomPos = tmpTopPos - 2 * halfSize.z * Vector3.forward;
            for(int j = 0; j < nSegmentsZ + 1; j++)
            {
                float kz = (float)j / nSegmentsZ;
                Vector3 tmpPos = Vector3.Lerp(tmpTopPos, tmpBottomPos, kz);
                vertices[index++] = tmpPos;
            }
        }*/
        /*
        0   1   2   3
        4   5   6   7   
        8   9   10  11
        12  13  14  15
         */
        //
        for (int i = 0; i < nSegmentsZ + 1; i++)
        {
            float kz = (float)i / nSegmentsZ;
            Vector3 tmpLeftPos = Vector3.Lerp(leftTopPos, leftBottomPos, kz);
            Vector3 tmpRightPos = tmpLeftPos + 2 * halfSize.x * Vector3.right;
            for (int j = 0; j < nSegmentsX + 1; j++)
            {
                float kx = (float)j / nSegmentsX;
                Vector3 tmpPos = Vector3.Lerp(tmpLeftPos, tmpRightPos, kx);
                vertices[index++] = tmpPos;
            }
        }
        index = 0;
        //boucle for pour remplir triangles
        for(int j = 0; j < nSegmentsZ; j++)
        {
            for(int i = 0; i < nSegmentsX; i++)
            {
                triangles[index++] = i + j * (nSegmentsX+1);
                triangles[index++] = i + j * (nSegmentsX + 1) + 1;
                triangles[index++] = i + (j + 1) * (nSegmentsX + 1);

                triangles[index++] = i + (j + 1) * (nSegmentsX + 1);
                triangles[index++] = i + j * (nSegmentsX + 1) + 1;
                triangles[index++] = i + (j + 1) * (nSegmentsX + 1) + 1;
            }
        }


        mesh.vertices = vertices;
        mesh.triangles = triangles;
        return mesh;
    }

    Mesh CreateNormalizedGridXZ(int nSegmentsX, int nSegmentsZ, ComputePosDelegate computePos = null)
    {
        Mesh mesh = new Mesh();
        mesh.name = "normalizedGrid";

        Vector3[] vertices = new Vector3[(nSegmentsX + 1) * (nSegmentsZ + 1)];
        int[] triangles = new int[nSegmentsX * nSegmentsZ * 2 * 3];

        //Vertices
        int index = 0;
        for (int i = 0; i < nSegmentsZ + 1; i++)
        {
            float kZ = (float)i / nSegmentsZ;

            for (int j = 0; j < nSegmentsX + 1; j++)
            {
                float kX = (float)j / nSegmentsX;
                vertices[index++] = computePos != null ? computePos(kX, kZ) : new Vector3(kX, 0, kZ);
            }
        }

        index = 0;
        //Quads
        for (int i = 0; i < nSegmentsZ; i++)
        {
            for (int j = 0; j < nSegmentsX; j++)
            {
                triangles[index++] = i * (nSegmentsX + 1) + j;
                triangles[index++] = (i + 1) * (nSegmentsX + 1) + j;
                triangles[index++] = i * (nSegmentsX + 1) + j + 1;

                triangles[index++] = (i + 1) * (nSegmentsX + 1) + j;
                triangles[index++] = (i + 1) * (nSegmentsX + 1) + j + 1;
                triangles[index++] = i * (nSegmentsX + 1) + j + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }

    Mesh CreateBox(Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "box";


        Vector3[] vertices = new Vector3[8];
        int[] quads = new int[6 * 4];

        //vertices
        vertices[0] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        vertices[1] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[2] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        vertices[3] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);

        vertices[4] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        vertices[5] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        vertices[6] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        vertices[7] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);

        //quads

        quads[0] = 1;
        quads[1] = 2;
        quads[2] = 3;
        quads[3] = 4;

        quads[4] = 1;
        quads[5] = 5;
        quads[6] = 6;
        quads[7] = 2;

        quads[8] = 2;
        quads[9] = 6;
        quads[10] = 7;
        quads[11] = 3;

        quads[12] = 3;
        quads[13] = 7;
        quads[14] = 8;
        quads[15] = 4;

        quads[16] = 4;
        quads[17] = 8;
        quads[18] = 5;
        quads[19] = 1;

        quads[20] = 5;
        quads[21] = 8;
        quads[22] = 7;
        quads[23] = 6;

        for (int i = 0; i < quads.Length; i++)
        {
            quads[i]--;
        }
        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }

    Mesh CreateChips(Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "chips";

        Vector3[] vertices = new Vector3[8];
        int[] quads = new int[3 * 4];

        //vertices
        vertices[0] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        vertices[1] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[2] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        vertices[3] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);

        vertices[4] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        vertices[5] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        vertices[6] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        vertices[7] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);

        //quads

        quads[0] = 1;
        quads[1] = 2;
        quads[2] = 3;
        quads[3] = 4;

        quads[4] = 1;
        quads[5] = 5;
        quads[6] = 6;
        quads[7] = 2;

        quads[8] = 3;
        quads[9] = 7;
        quads[10] = 8;
        quads[11] = 4;

        for (int i = 0; i < quads.Length; i++)
        {
            quads[i]--;
        }
        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }

    Mesh CreateRegularPolygon(Vector3 halfSize, int nSectors)
    {
        Mesh mesh = new Mesh();
        mesh.name = "regularPolygon";

        Vector3[] vertices = new Vector3[nSectors * 2 + 1];
        int[] quads = new int[nSectors * 4];

        //Vertices
        int index = 0;

        float step = (2 * Mathf.PI) / nSectors;

        for (int i = 0; i < nSectors; i++)
        {
            float rad = step * i;
            float rad2 = step * (i + 1);
            float x = Mathf.Cos(rad) * halfSize.x;
            float z = Mathf.Sin(rad) * halfSize.z;
            float x2 = Mathf.Cos(rad2) * halfSize.x;
            float z2 = Mathf.Sin(rad2) * halfSize.z;
            vertices[index++] = new Vector3(x, 0, z);
            vertices[index++] = Vector3.Lerp(new Vector3(x, 0, z), new Vector3(x2, 0, z2), 0.5f);
        }
        vertices[nSectors * 2] = Vector3.zero;

        for (int i = 0; i < nSectors * 2 + 1; i++)
        {
            Debug.Log(i + " : " + vertices[i] + "\n");
        }

        index = 0;
        //Quads
        for (int i = 0; i < nSectors * 2; i += 2)
        {
            quads[index++] = nSectors * 2;
            quads[index++] = (i + 1) % (nSectors * 2);
            quads[index++] = i % (nSectors * 2);
            quads[index++] = (nSectors * 2 + i - 1) % (nSectors * 2);
        }
        for (int i = 0; i < nSectors * 4; i++)
        {
            Debug.Log(i + " : " + quads[i] + "\n");
        }
        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }

    Mesh CreatePacman(Vector3 halfSize, int nSectors, float startAngle = Mathf.PI / 3, float endAngle = 5 * Mathf.PI / 3)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Pacman";

        Vector3[] vertices = new Vector3[nSectors * 2 + 2];
        int[] quads = new int[nSectors * 4];

        //Vertices
        int index = 0;
        float step = (2 * Mathf.PI - (2 * Mathf.PI - (endAngle - startAngle))) / nSectors;

        for (int i = 0; i < nSectors + 1; i++)
        {
            float rad = step * i + startAngle;
            float rad2 = step * (i + 1) + startAngle;
            float x = Mathf.Cos(rad) * halfSize.x;
            float z = Mathf.Sin(rad) * halfSize.z;
            float x2 = Mathf.Cos(rad2) * halfSize.x;
            float z2 = Mathf.Sin(rad2) * halfSize.z;
            vertices[index++] = new Vector3(x, 0, z);
            vertices[index++] = Vector3.Lerp(new Vector3(x, 0, z), new Vector3(x2, 0, z2), 0.5f);
        }
        vertices[nSectors * 2 + 1] = Vector3.zero;

        index = 0;
        //Quads
        for (int i = 0; i < nSectors * 2; i += 2)
        {
            quads[index++] = nSectors * 2 + 1;
            quads[index++] = (i + 2) % (nSectors * 2 + 1);
            quads[index++] = (i + 1) % (nSectors * 2 + 1);
            quads[index++] = i % (nSectors * 2 + 1);
        }

        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }

}
