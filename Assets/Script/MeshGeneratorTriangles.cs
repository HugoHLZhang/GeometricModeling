using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]

public class MeshGeneratorTriangles : MonoBehaviour
{
    MeshFilter m_Mf;

    private void Start()
    {
        m_Mf = GetComponent<MeshFilter>();
        m_Mf.mesh = CreateGridXZ( 3, 4, new Vector3(4,0,4));
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
}
