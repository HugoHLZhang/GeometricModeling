using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WingedEdge;
using HalfEdge;

public class MeshGeneratorQuads : MonoBehaviour
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

    [SerializeField] AnimationCurve m_Profil;
    /*
    private void Start()
    {
        m_Mf = GetComponent<MeshFilter>();
        //m_Mf.mesh = CreateStrip(7, new Vector3(4, 1, 3));
        //m_Mf.mesh = CreateNormalizedGridXZ(7, 4);


        //m_Mf.mesh = CreateBox(new Vector3(m_x, m_y, m_z));
        //m_Mf.mesh = CreateChips(new Vector3(m_x, m_y, m_z));
        //m_Mf.mesh = CreateRegularPolygon(new Vector3(m_x, m_y, m_z), m_nSectors);
        m_Mf.mesh = CreatePacman(new Vector3(m_x, m_y, m_z), m_nSectors);

        WingedEdgeMesh m = new WingedEdgeMesh(m_Mf.mesh);
        //Cylindre
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
    
    
        // Sph�re
        m_Mf.mesh = CreateNormalizedGridXZ(10, 5,
            (kX, kZ) =>
            {
                float rho, theta, phi;

                // coordinates mapping de (kX,kZ) -> (rho,theta,phi)
                theta = kX * 2 * Mathf.PI;
                phi = kZ * Mathf.PI;
                rho = 2 + .55f * Mathf.Cos(kX * 2 * Mathf.PI * 8)
                                * Mathf.Sin(kZ * 2 * Mathf.PI * 6);
                //rho = 3 + .25f * Mathf.Sin(kZ*2*Mathf.PI*4) ;
                //rho = m_Profile.Evaluate(kZ) * 2;

                return new Vector3(rho * Mathf.Cos(theta) * Mathf.Sin(phi),
                    rho * Mathf.Cos(phi),
                    rho * Mathf.Sin(theta) * Mathf.Sin(phi));
                //return new Vector3(Mathf.Lerp(-1.5f, 5.5f, kX), 1, Mathf.Lerp(-2, 4, kZ));
            }
        );
    
        Torus(Donut) Inner
        m_mf.mesh = createnormalizedgridxz(20 * 6, 10,
          (kx, kz) => {
            float theta = 6 * 2 * mathf.pi * kx;
            float r = 1;
            float r = 3;
            vector3 oomega = new vector3(r * mathf.cos(theta), 0, r * mathf.sin(theta));
            float alpha = mathf.pi * 2 * (1 - kz);
            vector3 omegap = r * mathf.cos(alpha) * oomega.normalized + r * mathf.sin(alpha) * vector3.up + vector3.up * kx * 2 * r * 6;
            return oomega + omegap;
            }
        );

        Torus(Donut) Outer
        m_Mf.mesh = CreateNormalizedGridXZ(20*6, 10,
          (kX, kZ) => {
            float theta = 6 * 2 * Mathf.PI * kX;
            float r = 1;
            float R = 3;
            Vector3 OOmega = new Vector3(R * Mathf.Cos(theta), 0, R * Mathf.Sin(theta));
            float alpha = Mathf.PI * 2 * kZ;
            Vector3 OmegaP = r * Mathf.Cos(alpha) * OOmega.normalized + r * Mathf.Sin(alpha) * Vector3.up + Vector3.up * kX * 2 * r * 6;
            return OOmega + OmegaP;
            }
        );


        GUIUtility.systemCopyBuffer = ConvertToCSV("\t");
        Debug.Log(ConvertToCSV("\t"));
    }

    */

    void Start()
    {
        m_Mf = GetComponent<MeshFilter>();

        //m_Mf.mesh = CreateStrip(7, new Vector3(4, 1, 3));
        //m_Mf.mesh = CreateNormalizedGridXZ(7, 4);


        //##############        TD1 Objet        ##############
        m_Mf.mesh = CreateBox(new Vector3(m_x, m_y, m_z));
        //m_Mf.mesh = CreateChips(new Vector3(m_x, m_y, m_z));
        //m_Mf.mesh = CreateRegularPolygon(new Vector3(m_x, m_y, m_z), m_nSectors);
        //m_Mf.mesh = CreatePacman(new Vector3(m_x, m_y, m_z), m_nSectors);
        Debug.Log($"#################      Create a {m_Mf.mesh.name}     #################");
        ConvertToCSV();

        //GUIUtility.systemCopyBuffer To Copy In Clipboard

        //##############        WingedEdge        ##############

        /*
        m_WingedEdgeMesh = new WingedEdgeMesh(m_Mf.mesh);

        m_WingedEdgeMesh.ConvertToCSVFormat();
        
        Debug.Log("#################      WindgedEdgeMesh ConvertToFaceVertexMesh     #################");
        Mesh tmp = m_WingedEdgeMesh.ConvertToFaceVertexMesh();
        m_Mf.mesh = tmp;
        ConvertToCSV("\t");
        m_WingedEdgeMesh = new WingedEdgeMesh(tmp);
        m_WingedEdgeMesh.ConvertToCSVFormat();
        */

        //##############        HalfEdge        ##############

        m_HalfEdgeMesh = new HalfEdgeMesh(m_Mf.mesh);
        /*
         * 
        m_HalfEdgeMesh.ConvertToCSVFormat();
        Debug.Log("#################      HalfEdgeMesh ConvertToFaceVertexMesh     #################");
        Mesh tmp = m_HalfEdgeMesh.ConvertToFaceVertexMesh();
        m_Mf.mesh = tmp;
        ConvertToCSV();
        */
        //Cylindre
        /* m_Mf.mesh = CreateNormalizedGridXZ(20, 40,
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
        */
        /*
        // Sph�re
        m_Mf.mesh = CreateNormalizedGridXZ(10, 5,
            (kX, kZ) =>
            {
                float rho, theta, phi;

                // coordinates mapping de (kX,kZ) -> (rho,theta,phi)
                theta = kX * 2 * Mathf.PI;
                phi = kZ * Mathf.PI;
                rho = 2 + .55f * Mathf.Cos(kX * 2 * Mathf.PI * 8)
                                * Mathf.Sin(kZ * 2 * Mathf.PI * 6);
                //rho = 3 + .25f * Mathf.Sin(kZ*2*Mathf.PI*4) ;
                //rho = m_Profile.Evaluate(kZ) * 2;

                return new Vector3(rho * Mathf.Cos(theta) * Mathf.Sin(phi),
                    rho * Mathf.Cos(phi),
                    rho * Mathf.Sin(theta) * Mathf.Sin(phi));
                //return new Vector3(Mathf.Lerp(-1.5f, 5.5f, kX), 1, Mathf.Lerp(-2, 4, kZ));
            }
            );
        */
        //Torus (Donut) Inner
        //m_mf.mesh = createnormalizedgridxz(20 * 6, 10,
        //   (kx, kz) => {
        //           float theta = 6*2 * mathf.pi * kx;
        //           float r = 1;
        //           float r = 3;
        //           vector3 oomega = new vector3(r * mathf.cos(theta), 0, r * mathf.sin(theta));
        //           float alpha = mathf.pi * 2 * (1-kz);
        //           vector3 omegap = r * mathf.cos(alpha) * oomega.normalized + r * mathf.sin(alpha) * vector3.up + vector3.up * kx * 2 * r * 6;
        //           return oomega + omegap;
        //        }
        //   );

        //Torus (Donut) Outer
        //m_Mf.mesh = CreateNormalizedGridXZ(20*6, 10,
        //   (kX, kZ) => {
        //       float theta = 6*2 * Mathf.PI * kX;
        //       float r = 1;
        //       float R = 3;
        //       Vector3 OOmega = new Vector3(R * Mathf.Cos(theta), 0, R * Mathf.Sin(theta));
        //       float alpha = Mathf.PI * 2 * kZ;
        //       Vector3 OmegaP = r * Mathf.Cos(alpha) * OOmega.normalized + r * Mathf.Sin(alpha) * Vector3.up + Vector3.up * kX * 2 * r * 6;
        //       return OOmega + OmegaP;
        //   }
        //   );



    }
    string ConvertToCSV(string separator = "\t")
    {
        if (!(m_Mf && m_Mf.mesh)) return "";
        Debug.Log("#################      FaceVertex ConvertTOCSVFormat     #################");
        string str = "";
        Vector3[] vertices = m_Mf.mesh.vertices;
        int[] quads = m_Mf.mesh.GetIndices(0);

        List<string> strings = new List<string>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            strings.Add(i.ToString() + separator
                + pos.x.ToString("N03") + " "
                + pos.y.ToString("N03") + " "
                + pos.z.ToString("N03") + separator + separator);
        }

        for (int i = vertices.Length; i < quads.Length / 4; i++)
            strings.Add(separator + separator + separator);

        for (int i = 0; i < quads.Length / 4; i++)
        {
            strings[i] += i.ToString() + separator
                + quads[4 * i + 0].ToString() + ","
                + quads[4 * i + 1].ToString() + ","
                + quads[4 * i + 2].ToString() + ","
                + quads[4 * i + 3].ToString();
        }

        str = "Vertices" + separator + separator + separator + "Faces\n"
            + "Index" + separator + "Position" + separator + separator +
            "Index" + separator + "Indices des vertices\n"
            + string.Join("\n", strings);
        Debug.Log(str);
        return str;
    }
    /*
    string ConvertToCSV(string separator)
    {
        if (!(m_Mf && m_Mf.mesh)) return "";

        Vector3[] vertices = m_Mf.mesh.vertices;
        int[] quads = m_Mf.mesh.GetIndices(0);

        List<string> strings = new List<string>();

        for(int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            strings.Add(i.ToString()+separator+pos.x.ToString("N03")+" "+ pos.y.ToString("N03") + " " + pos.z.ToString("N03") + separator + separator);

        }
        for(int i = vertices.Length; i < quads.Length/4; i++)
            strings.Add(separator + separator + separator);

        for (int i = 0; i < quads.Length/4; i++)
        {
            strings[i] += i.ToString() + separator 
                + quads[4 * i + 0].ToString() + ","
                + quads[4 * i + 1].ToString() + ","
                + quads[4 * i + 2].ToString() + ","
                + quads[4 * i + 3].ToString();
        }

        return "Vertices" +separator+separator+separator+"Faces\n"
            +"Index"+separator+"Position"+separator +separator+"Index" +separator+"Indices des vertices\n" 
            + string.Join("\n", strings);
    }
    */
    Mesh CreateStrip(int nSegments, Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "strip";

        Vector3[] vertices = new Vector3[(nSegments + 1) * 2];
        int[] quads = new int[nSegments * 4];

        int index = 0;
        Vector3 leftTopPos = new Vector3(-halfSize.x, 0, halfSize.z);
        Vector3 rightTopPos = new Vector3(halfSize.x, 0, halfSize.z);
        //bouble for pour remplir vertices
        for (int i = 0; i < nSegments + 1; i++)
        {
            float k = (float)i / nSegments;
            Vector3 tmpPos = Vector3.Lerp(leftTopPos, rightTopPos, k);
            vertices[index++] = tmpPos;
            vertices[index++] = tmpPos - 2 * halfSize.z * Vector3.forward;
        }
        index = 0;
        //boucle for pour remplir triangles
        for (int i = 0; i < nSegments; i++)
        {
            quads[index++] = 2 * i;
            quads[index++] = 2 * i + 2;
            quads[index++] = 2 * i + 3;
            quads[index++] = 2 * i + 1;
        }


        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads,0);

        return mesh;
    }

    Mesh CreateGridXZ(int nSegmentsX, int nSegmentsZ, Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "grid";

        Vector3[] vertices = new Vector3[(nSegmentsX + 1) * (nSegmentsZ + 1)];
        int[] quads = new int[nSegmentsX * nSegmentsZ * 4];

        int index = 0;

        for (int i = 0; i < nSegmentsZ + 1; i++)
        {
            float kz = (float)i / nSegmentsZ;
            for (int j = 0; j < nSegmentsX + 1; j++)
            {
                float kx = (float)j / nSegmentsX;
                vertices[index++] = new Vector3(Mathf.Lerp(-halfSize.x, halfSize.x, kx), 0, Mathf.Lerp(halfSize.z, -halfSize.z, kz));
            }
        }

        /*
        Vector3 leftTopPos = new Vector3(-halfSize.x, 0, halfSize.z);
        Vector3 leftBottomPos = new Vector3(-halfSize.x, 0, -halfSize.z);
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
        */

        index = 0;
        //boucle for pour remplir triangles
        for (int j = 0; j < nSegmentsZ; j++)
        {
            for (int i = 0; i < nSegmentsX; i++)
            {
                quads[index++] = i + j * (nSegmentsX + 1);
                quads[index++] = i + j * (nSegmentsX + 1) + 1;
                quads[index++] = i + (j + 1) * (nSegmentsX + 1) + 1;
                quads[index++] = i + (j + 1) * (nSegmentsX + 1);
            }
        }
        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }
    /*
    Mesh CreateNormalizedGridXZ(int nSegmentsX, int nSegmentsZ, ComputePosDelegate computePos = null)
    {
        Mesh mesh = new Mesh();
        mesh.name = "normalizedGrid";

        Vector3[] vertices = new Vector3[(nSegmentsX + 1) * (nSegmentsZ + 1)];
        int[] quads = new int[nSegmentsX * nSegmentsZ * 4];

        int index = 0;

        for (int i = 0; i < nSegmentsZ + 1; i++)
        {
            float kz = (float)i / nSegmentsZ;
            for (int j = 0; j < nSegmentsX + 1; j++)
            {
                float kx = (float)j / nSegmentsX;
                vertices[index++] = computePos!=null?computePos(kx,kz) : new Vector3(kx, 0, kz);
            }
        }


        index = 0;
        //boucle for pour remplir triangles
        for (int j = 0; j < nSegmentsZ; j++)
        {
            for (int i = 0; i < nSegmentsX; i++)
            {
                quads[index++] = i + j * (nSegmentsX + 1);
                quads[index++] = i + j * (nSegmentsX + 1) + 1;
                quads[index++] = i + (j + 1) * (nSegmentsX + 1) + 1;
                quads[index++] = i + (j + 1) * (nSegmentsX + 1);
            }
        }
        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }
    */
    Mesh CreateNormalizedGridXZ(int nSegmentsX, int nSegmentsZ, ComputePosDelegate computePos = null)
    {
        Mesh mesh = new Mesh();
        mesh.name = "normalizedGrid";

        Vector3[] vertices = new Vector3[(nSegmentsX + 1) * (nSegmentsZ + 1)];
        int[] quads = new int[nSegmentsX * nSegmentsZ * 4];

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
                quads[index++] = i * (nSegmentsX + 1) + j;
                quads[index++] = (i + 1) * (nSegmentsX + 1) + j;
                quads[index++] = (i + 1) * (nSegmentsX + 1) + j + 1;
                quads[index++] = i * (nSegmentsX + 1) + j + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

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

        Vector3[] vertices = new Vector3[nSectors*2 + 1];
        int[] quads = new int[nSectors * 4];

        //Vertices
        int index = 0;

        float step = (2 * Mathf.PI) / nSectors;

        for (int i = 0; i < nSectors; i++)
        {
            float rad = step * i;
            float rad2 = step * (i+1);
            float x = Mathf.Cos(rad) * halfSize.x;
            float z = Mathf.Sin(rad) * halfSize.z;
            float x2 = Mathf.Cos(rad2) * halfSize.x;
            float z2 = Mathf.Sin(rad2) * halfSize.z;
            vertices[index++] = new Vector3(x, 0, z);
            vertices[index++] = Vector3.Lerp(new Vector3(x, 0, z), new Vector3(x2, 0, z2),0.5f);
        }
        vertices[nSectors*2] = Vector3.zero;

        for (int i = 0; i < nSectors * 2 + 1; i++)
        {
            Debug.Log(i + " : " + vertices[i] + "\n");
        }

        index = 0;
        //Quads
        for (int i = 0; i < nSectors*2; i+=2)
        {
            quads[index++] = nSectors*2;
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
        vertices[nSectors * 2 +1] = Vector3.zero;

        //for (int i = 0; i < nSectors * 2 + 1; i++)
        //{
        //    Debug.Log(i + " : " + vertices[i] + "\n");
        //}

        index = 0;
        //Quads
        for (int i = 0; i < nSectors * 2 ; i += 2)
        {
            quads[index++] = nSectors * 2 + 1;
            quads[index++] = (i + 2) % (nSectors * 2 + 1 );
            quads[index++] = (i + 1) % (nSectors * 2 + 1);
            quads[index++] = i % (nSectors * 2 + 1);
        }
        //for (int i = 0; i < nSectors * 4; i++)
        //{
        //    Debug.Log(i + " : " + quads[i] + "\n");
        //}
        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }

    private void OnDrawGizmos()
    {
        
        if (!(m_Mf && m_Mf.mesh)) return;
        Mesh mesh = m_Mf.mesh;
        if(m_WingedEdgeMesh != null)
        {
            WingedEdgeMesh wingedEdgeMesh = m_WingedEdgeMesh;
            wingedEdgeMesh.DrawGizmos(m_DisplayMeshVertices, m_DisplayMeshEdges, m_DisplayMeshFaces, transform);
        }
        if (m_HalfEdgeMesh != null)
        {
            HalfEdgeMesh halfEdgeMesh = m_HalfEdgeMesh;
            halfEdgeMesh.DrawGizmos(m_DisplayMeshVertices, m_DisplayMeshEdges, m_DisplayMeshFaces, transform);
        }


        Vector3[] vertices = mesh.vertices;
        int[] quads = mesh.GetIndices(0);


        Gizmos.color = Color.black;
        GUIStyle style = new GUIStyle();
        style.fontSize = 12;

        

        style.normal.textColor = Color.red;
        if (m_DisplayMeshInfo)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(vertices[i]);
                Handles.Label(worldPos, i.ToString(), style);
            }
        }
       

        
        style.normal.textColor = Color.green;
        for (int i = 0; i < quads.Length / 4; i++)
        {
            int index1 = quads[4 * i];
            int index2 = quads[4 * i + 1];
            int index3 = quads[4 * i + 2];
            int index4 = quads[4 * i + 3];

            Vector3 pt1 = transform.TransformPoint(vertices[index1]);
            Vector3 pt2 = transform.TransformPoint(vertices[index2]);
            Vector3 pt3 = transform.TransformPoint(vertices[index3]);
            Vector3 pt4 = transform.TransformPoint(vertices[index4]);

            Gizmos.DrawLine(pt1, pt2);
            Gizmos.DrawLine(pt2, pt3);
            Gizmos.DrawLine(pt3, pt4);
            Gizmos.DrawLine(pt4, pt1);
            if (m_DisplayMeshInfo)
            {
                string str = string.Format("{0}:{1},{2},{3},{4}", i, index1, index2, index3, index4);

                Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, str, style);

            }
        }

       
        



    }

}
