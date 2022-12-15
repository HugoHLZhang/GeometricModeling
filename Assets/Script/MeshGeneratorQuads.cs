using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WingedEdge;
using HalfEdge;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class MeshGeneratorQuads : MonoBehaviour
{
    delegate Vector3 ComputePosDelegate(float kx, float kz);
    delegate float3 ComputePosDelegate_SIMD(float3 k);
    MeshFilter m_Mf;
    WingedEdgeMesh m_WingedEdgeMesh;
    HalfEdgeMesh m_HalfEdgeMesh;
    public enum Objets { Strip, GridXZ, NormalizedGridXZ, NormalizedGridXZ_SIMD1, NormalizedGridXZ_SIMD2, Cyclindre, Sphere, Torus, Helix, Box, Chips, Cage, RegularPolygon, CylindrePoly, ConePoly, PacMan, PacMan3D, BlueLock };
    public enum MeshType { VertexFaceMesh, WingedEdgeMesh, HalfEdgeMesh};

    [Header("Mesh")]
    [SerializeField] public Objets Create;
    [SerializeField] public MeshType mesh_type;
    [SerializeField] private bool reverseMesh = false;
    [Range(0, 4)]
    [SerializeField] private int nb_subdivide = 0;
    [SerializeField] AnimationCurve m_Profile;

    [Header("Only for GridXZ and WingedEdgeMesh")]
    [Min(0)]
    [SerializeField] private int nb_holes = 0 ;
    [SerializeField] private bool addHoles = false;

    [Header("Size Simple Form")]
    [Min(0)]
    [SerializeField] private float m_x = 5;
    [Min(0)]
    [SerializeField] private float m_y = 5;
    [Min(0)]
    [SerializeField] private float m_z = 5;
    [Min(1)]
    [SerializeField] private int m_nSectors = 5;


    [Header("Size using GridXZ")]
    [Min(1)]
    [SerializeField] private int m_nSegmentsX = 5;
    [Min(1)]
    [SerializeField] private int m_nSegmentsZ = 5;

    [Header("Gizmos & CSV")]
    [SerializeField] bool m_DisplayMeshWires = true;
    [SerializeField] bool m_DisplayMeshInfos = true;
    [SerializeField] bool m_DisplayMeshEdges = true;
    [SerializeField] bool m_DisplayMeshVertices = true;
    [SerializeField] bool m_DisplayMeshFaces = true;

    

    void Start()
    {
        m_Mf = GetComponent<MeshFilter>();
        //---------------------Create Mesh---------------------
        switch (Create)
        {
            case Objets.Strip:
                m_Mf.mesh = CreateStrip(m_nSegmentsX, new Vector3(m_x, m_y, m_z));
                break;
            case Objets.GridXZ:
                m_Mf.mesh = CreateGridXZ(m_nSegmentsX, m_nSegmentsZ, new Vector3(m_x, m_y, m_z));
                break;
            case Objets.NormalizedGridXZ:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ);
                break;
            case Objets.Cyclindre:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ,
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
                break;
            case Objets.Sphere:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ,
                    (kX, kZ) =>
                    {
                        float rho, theta, phi;
                        // coordinates mapping de (kX,kZ) -> (rho,theta,phi)
                        theta = kX * 2 * Mathf.PI;
                        phi = kZ * Mathf.PI;
                        rho = 2 + .55f * Mathf.Cos(kX * 2 * Mathf.PI * 8) * Mathf.Sin(kZ * 2 * Mathf.PI * 6);
                        //rho = 3 + .25f * Mathf.Sin(kZ*2*Mathf.PI*4) ;
                        rho = m_Profile.Evaluate(kZ) * 2;

                        return new Vector3(rho * Mathf.Cos(theta) * Mathf.Sin(phi), rho * Mathf.Cos(phi), rho * Mathf.Sin(theta) * Mathf.Sin(phi));
                        //return new Vector3(Mathf.Lerp(-1.5f, 5.5f, kX), 1, Mathf.Lerp(-2, 4, kZ));
                    }
                    );
                break;
            case Objets.Torus:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ,
                    (kX, kZ) =>
                    {
                        float R = 3;
                        float r = 1;
                        float theta = 2 * Mathf.PI * kX;
                        Vector3 OOmega = new Vector3(R * Mathf.Cos(theta), 0, R * Mathf.Sin(theta));

                        float alpha = Mathf.PI * 2 * kZ;
                        Vector3 OmegaP = r * Mathf.Cos(alpha) * OOmega.normalized + r * Mathf.Sin(alpha) * Vector3.up;

                        return OOmega + OmegaP;
                    }
                );
                break;
            case Objets.Helix:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ,
                  (kX, kZ) =>
                  {
                      float theta = 4 * 2 * Mathf.PI * kX;
                      float r = 1;
                      float R = 2;
                      Vector3 OOmega = new Vector3(R * Mathf.Cos(theta), 0, R * Mathf.Sin(theta));
                      float alpha = Mathf.PI * 2 * kZ + 2;
                      Vector3 OmegaP = r * Mathf.Cos(alpha) * OOmega.normalized + r * Mathf.Sin(alpha) * Vector3.up + Vector3.up * kX * 2 * r * 4;
                      return OOmega + OmegaP;
                  }
                );

                break;
    
            // Unity.Mathematics
            case Objets.NormalizedGridXZ_SIMD1:
                m_Mf.mesh = CreateNormalizedGridXZ_SIMD(int3(100, 100, 1),
                    (k) =>
                    {
                        //return lerp(float3(-5f, 0, -5f), float3(5f, 0, 5f), k.xzy);
                        //return lerp(float3(-5, 1, -5), float3(5, 0, 5), float3(k.x, step(.2f, k.x), k.y)) ;
                        //return lerp(float3(-5, 1, -5), float3(5, 0, 5), float3(k.x, smoothstep(.2f - 0.05f, .2f + 0.05f, k.x), k.y)) ;
                        //return lerp(float3(-5, 1, -5), float3(5, 0, 5), float3(k.x, smoothstep(0.2f - .05f, .2f + .05f, k.x * k.y), k.y));
                        return lerp(float3(-5, 1, -5), float3(5, 0, 5), float3(
                            k.x,
                            0.5f * (sin(k.x * 2 * PI * 4) * cos(k.y * 2 * PI * 3) + 1),
                             //smoothstep(0.2f - .05f, .2f + .05f, 0.5f*(sin(k.x*2*PI*4) * cos(k.y*2*PI*3)+1))
                             k.y));
                    }
                    );
                break;
            case Objets.NormalizedGridXZ_SIMD2:
                // repeated pattern
                int3 nCells = int3(3, 3, 1);
                int3 nSegmentsPerCell = int3(100, 100, 1);
                float3 kStep = float3(1) / (nCells * nSegmentsPerCell);

                float3 cellSize = float3(1, .5f, 1);

                m_Mf.mesh = CreateNormalizedGridXZ_SIMD(
                    nCells * nSegmentsPerCell,
                    (k) =>
                    {
                        // calculs sur la grille normalisée
                        int3 index = (int3)floor(k / kStep);
                        int3 localIndex = index % nSegmentsPerCell;
                        int3 indexCell = index / nSegmentsPerCell;
                        float3 relIndexCell = (float3)indexCell / nCells;

                        // calculs sur les positions dans l'espace

                        float3 cellOriginPos = lerp(
                            -cellSize * nCells.xzy * .5f,
                            cellSize * nCells.xzy * .5f,
                            relIndexCell.xzy);

                        //float3 cellOriginPos = floor(k * nCells).xzy; // Theo's style ... ne prend pas en compte cellSize
                        k = frac(k * nCells);

                        return cellOriginPos + cellSize * float3(k.x, smoothstep(0.2f - .05f, .2f + .05f, k.x * k.y), k.y);
                    }
                    );
                break;

            //##############        TD1 Objet        ##############
            case Objets.Box:
                m_Mf.mesh = CreateBox(new Vector3(m_x, m_y, m_z));
                break;
            case Objets.Chips:
                m_Mf.mesh = CreateChips(new Vector3(m_x, m_y, m_z));
                break;
            case Objets.RegularPolygon:
                m_Mf.mesh = CreateRegularPolygon(new Vector3(m_x, m_y, m_z), m_nSectors);
                break;
            case Objets.PacMan:
                m_Mf.mesh = CreatePacman(new Vector3(m_x, m_y, m_z), m_nSectors);
                break;
            case Objets.Cage:
                m_Mf.mesh = CreateCage(new Vector3(m_x, m_y, m_z));
                break;
            case Objets.BlueLock:
                m_Mf.mesh = CreateBlueLock(new Vector3(m_x, m_y, m_z), 5);
                break;
            case Objets.PacMan3D:
                m_Mf.mesh = Create3DPacman(new Vector3(m_x, m_y, m_z), 5);
                break;
            case Objets.CylindrePoly:
                m_Mf.mesh = CreateCylindre(new Vector3(m_x, m_y, m_z), m_nSectors);
                break;
            case Objets.ConePoly:
                m_Mf.mesh = CreateCone(new Vector3(m_x, m_y, m_z), m_nSectors);
                break;
        }
        //---------------------Reverse Mesh---------------------
        if (reverseMesh) Reverse(m_Mf.mesh);
        if(Create != Objets.GridXZ &&  Create != Objets.NormalizedGridXZ && Create != Objets.NormalizedGridXZ_SIMD1 && Create != Objets.NormalizedGridXZ_SIMD2 && Create != Objets.Cyclindre && Create != Objets.Sphere && Create != Objets.Torus && Create != Objets.Helix) addHoles = false;
        int i;

        m_WingedEdgeMesh = new WingedEdgeMesh(m_Mf.mesh);
        m_HalfEdgeMesh = new HalfEdgeMesh(m_Mf.mesh);

        //---------------------Mesh Structure---------------------
        switch (mesh_type)
        {
            //---------------------WingedEdge  &&  TD 2 CatmullClark---------------------
            case MeshType.WingedEdgeMesh:
                //Only for GridXZ
                if (addHoles)
                {
                    if (nb_holes > math.floor(m_nSegmentsX / 2.5f) * (math.floor(m_nSegmentsZ / 2.5f) - 1)) nb_holes = (int)(math.floor(m_nSegmentsX / 2.5f) * (math.floor(m_nSegmentsZ / 2.5f) - 1));
                    for (int j = 0; j < nb_holes; j++)
                    {
                        m_WingedEdgeMesh.RemoveFace();
                    }
                }
                i = 0;
                while (i < nb_subdivide)
                {
                    if (m_Mf.mesh.GetIndices(0).Length > 10000)
                    {
                        Debug.Log("Only subdivided the mesh " + i + " times to avoid memory surcharge"); break;
                    }
                    m_WingedEdgeMesh.SubdivideCatmullClark();
                    i++;
                }
                m_Mf.mesh = m_WingedEdgeMesh.ConvertToFaceVertexMesh();
                nb_subdivide = i;
                break;
            //---------------------HalfEdge  &&  TD 2 CatmullClark---------------------
            case MeshType.HalfEdgeMesh:
                i = 0;
                while (i < nb_subdivide)
                {
                    if (m_Mf.mesh.GetIndices(0).Length > 10000)
                    {
                        Debug.Log("Only subdivided the mesh " + i + " times to avoid memory surcharge"); break;
                    }
                    m_HalfEdgeMesh.SubdivideCatmullClark();
                    i++;
                }
                nb_subdivide = i;
                GUIUtility.systemCopyBuffer = m_HalfEdgeMesh.ConvertToCSVFormat();
                break;
        }
    }
    void Update()
    {
        //---------------------Create Mesh---------------------
        switch (Create)
        {
            case Objets.Strip:
                m_Mf.mesh = CreateStrip(m_nSegmentsX, new Vector3(m_x, m_y, m_z));
                break;
            case Objets.GridXZ:
                m_Mf.mesh = CreateGridXZ(m_nSegmentsX, m_nSegmentsZ, new Vector3(m_x, m_y, m_z));
                break;
            case Objets.NormalizedGridXZ:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ);
                break;
            case Objets.Cyclindre:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ,
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
                break;
            case Objets.Sphere:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ,
                    (kX, kZ) =>
                    {
                        float rho, theta, phi;
                        // coordinates mapping de (kX,kZ) -> (rho,theta,phi)
                        theta = kX * 2 * Mathf.PI;
                        phi = kZ * Mathf.PI;
                        rho = 2 + .55f * Mathf.Cos(kX * 2 * Mathf.PI * 8) * Mathf.Sin(kZ * 2 * Mathf.PI * 6);
                        //rho = 3 + .25f * Mathf.Sin(kZ*2*Mathf.PI*4) ;
                        rho = m_Profile.Evaluate(kZ) * 2;

                        return new Vector3(rho * Mathf.Cos(theta) * Mathf.Sin(phi), rho * Mathf.Cos(phi), rho * Mathf.Sin(theta) * Mathf.Sin(phi));
                        //return new Vector3(Mathf.Lerp(-1.5f, 5.5f, kX), 1, Mathf.Lerp(-2, 4, kZ));
                    }
                    );
                break;
            case Objets.Torus:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ,
                    (kX, kZ) =>
                    {
                        float R = 3;
                        float r = 1;
                        float theta = 2 * Mathf.PI * kX;
                        Vector3 OOmega = new Vector3(R * Mathf.Cos(theta), 0, R * Mathf.Sin(theta));

                        float alpha = Mathf.PI * 2 * kZ;
                        Vector3 OmegaP = r * Mathf.Cos(alpha) * OOmega.normalized + r * Mathf.Sin(alpha) * Vector3.up;

                        return OOmega + OmegaP;
                    }
                );
                break;
            case Objets.Helix:
                m_Mf.mesh = CreateNormalizedGridXZ(m_nSegmentsX, m_nSegmentsZ,
                  (kX, kZ) =>
                  {
                      float theta = 4 * 2 * Mathf.PI * kX;
                      float r = 1;
                      float R = 2;
                      Vector3 OOmega = new Vector3(R * Mathf.Cos(theta), 0, R * Mathf.Sin(theta));
                      float alpha = Mathf.PI * 2 * kZ + 2;
                      Vector3 OmegaP = r * Mathf.Cos(alpha) * OOmega.normalized + r * Mathf.Sin(alpha) * Vector3.up + Vector3.up * kX * 2 * r * 4;
                      return OOmega + OmegaP;
                  }
                );

                break;

            // Unity.Mathematics
            case Objets.NormalizedGridXZ_SIMD1:
                m_Mf.mesh = CreateNormalizedGridXZ_SIMD(int3(m_nSegmentsX, m_nSegmentsZ, 1),
                    (k) =>
                    {
                        //return lerp(float3(-5f, 0, -5f), float3(5f, 0, 5f), k.xzy);
                        //return lerp(float3(-5, 1, -5), float3(5, 0, 5), float3(k.x, step(.2f, k.x), k.y)) ;
                        //return lerp(float3(-5, 1, -5), float3(5, 0, 5), float3(k.x, smoothstep(.2f - 0.05f, .2f + 0.05f, k.x), k.y)) ;
                        //return lerp(float3(-5, 1, -5), float3(5, 0, 5), float3(k.x, smoothstep(0.2f - .05f, .2f + .05f, k.x * k.y), k.y));
                        return lerp(float3(-5, 1, -5), float3(5, 0, 5), float3(
                            k.x,
                            0.5f * (sin(k.x * 2 * PI * 4) * cos(k.y * 2 * PI * 3) + 1),
                             //smoothstep(0.2f - .05f, .2f + .05f, 0.5f*(sin(k.x*2*PI*4) * cos(k.y*2*PI*3)+1))
                             k.y));
                    }
                    );
                break;
            case Objets.NormalizedGridXZ_SIMD2:
                // repeated pattern
                int3 nCells = int3(3, 3, 1);
                int3 nSegmentsPerCell = int3(10, 10, 1);
                float3 kStep = float3(1) / (nCells * nSegmentsPerCell);

                float3 cellSize = float3(1, .5f, 1);

                m_Mf.mesh = CreateNormalizedGridXZ_SIMD(
                    nCells * nSegmentsPerCell,
                    (k) =>
                    {
                        // calculs sur la grille normalisée
                        int3 index = (int3)floor(k / kStep);
                        int3 localIndex = index % nSegmentsPerCell;
                        int3 indexCell = index / nSegmentsPerCell;
                        float3 relIndexCell = (float3)indexCell / nCells;

                        // calculs sur les positions dans l'espace

                        float3 cellOriginPos = lerp(
                            -cellSize * nCells.xzy * .5f,
                            cellSize * nCells.xzy * .5f,
                            relIndexCell.xzy);

                        //float3 cellOriginPos = floor(k * nCells).xzy; // Theo's style ... ne prend pas en compte cellSize
                        k = frac(k * nCells);

                        return cellOriginPos + cellSize * float3(k.x, smoothstep(0.2f - .05f, .2f + .05f, k.x * k.y), k.y);
                    }
                    );
                break;

            //##############        TD1 Objet        ##############
            case Objets.Box:
                m_Mf.mesh = CreateBox(new Vector3(m_x, m_y, m_z));
                break;
            case Objets.Chips:
                m_Mf.mesh = CreateChips(new Vector3(m_x, m_y, m_z));
                break;
            case Objets.RegularPolygon:
                m_Mf.mesh = CreateRegularPolygon(new Vector3(m_x, m_y, m_z), m_nSectors);
                break;
            case Objets.PacMan:
                m_Mf.mesh = CreatePacman(new Vector3(m_x, m_y, m_z), m_nSectors);
                break;
            case Objets.Cage:
                m_Mf.mesh = CreateCage(new Vector3(m_x, m_y, m_z));
                break;
            case Objets.BlueLock:
                m_Mf.mesh = CreateBlueLock(new Vector3(m_x, m_y, m_z), 5);
                break;
            case Objets.PacMan3D:
                m_Mf.mesh = Create3DPacman(new Vector3(m_x, m_y, m_z), 5);
                break;
            case Objets.CylindrePoly:
                m_Mf.mesh = CreateCylindre(new Vector3(m_x, m_y, m_z), m_nSectors);
                break;
            case Objets.ConePoly:
                m_Mf.mesh = CreateCone(new Vector3(m_x, m_y, m_z), m_nSectors);
                break;
        }
        //---------------------Reverse Mesh---------------------
        if (reverseMesh) Reverse(m_Mf.mesh);
        if (Create != Objets.GridXZ && Create != Objets.NormalizedGridXZ && Create != Objets.NormalizedGridXZ_SIMD1 && Create != Objets.NormalizedGridXZ_SIMD2 && Create != Objets.Cyclindre && Create != Objets.Sphere && Create != Objets.Torus && Create != Objets.Helix) addHoles = false;


        int i;
        //---------------------Mesh Structure---------------------
        switch (mesh_type)
        {
            //---------------------WingedEdge  &&  TD 2 CatmullClark---------------------
            case MeshType.WingedEdgeMesh:
                m_WingedEdgeMesh = new WingedEdgeMesh(m_Mf.mesh);
                if (addHoles)
                {
                    if (nb_holes > math.floor(m_nSegmentsX / 2.5f) * (math.floor(m_nSegmentsZ / 2.5f) - 1)) nb_holes = (int)(math.floor(m_nSegmentsX / 2.5f) * (math.floor(m_nSegmentsZ / 2.5f) - 1)) - 1;
                    for (int j = 0; j < nb_holes; j++)
                    {
                        m_WingedEdgeMesh.RemoveFace();
                    }
                }
                i = 0;
                while (i < nb_subdivide)
                {
                    if (m_Mf.mesh.GetIndices(0).Length > 10000)
                    {
                        Debug.Log("Only subdivided the mesh " + i + " times to avoid memory surcharge"); break;
                    }
                    m_WingedEdgeMesh.SubdivideCatmullClark();
                    i++;
                }
                m_Mf.mesh = m_WingedEdgeMesh.ConvertToFaceVertexMesh();
                nb_subdivide = i;
                break;

            //---------------------HalfEdge  &&  TD 2 CatmullClark---------------------
            case MeshType.HalfEdgeMesh:
                m_HalfEdgeMesh = new HalfEdgeMesh(m_Mf.mesh);
                i = 0;
                while (i < nb_subdivide)
                {
                    if (m_Mf.mesh.GetIndices(0).Length > 10000)
                    {
                        Debug.Log("Only subdivided the mesh " + i + " times to avoid memory surcharge"); break;
                    }
                    m_HalfEdgeMesh.SubdivideCatmullClark();
                    i++;
                }
                m_Mf.mesh = m_HalfEdgeMesh.ConvertToFaceVertexMesh();
                nb_subdivide = i;
                break;
        }
    }
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
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }
    Mesh CreateGridXZ(int nSegmentsX, int nSegmentsZ, Vector3 halfSize)
    {
        Mesh mesh = new Mesh();
        mesh.name = "grid";

        Vector3[] vertices = new Vector3[(nSegmentsX + 1) * (nSegmentsZ + 1)];
        int[] quads = new int[nSegmentsX * nSegmentsZ * 4];

        //vertices
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

        //quads
        index = 0;
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
    Mesh CreateNormalizedGridXZ_SIMD(int3 nSegments, ComputePosDelegate_SIMD computePos = null)
    {
        Mesh mesh = new Mesh();
        mesh.name = "normalizedGridSIMD";

        Vector3[] vertices = new Vector3[(nSegments.x + 1) * (nSegments.y + 1)];
        int[] quads = new int[nSegments.x * nSegments.y * 4];

        //Vertices
        int index = 0;
        for (int i = 0; i < nSegments.y + 1; i++)
        {
            for (int j = 0; j < nSegments.x + 1; j++)
            {
                float3 k =  float3(j,i,0) / nSegments;
                vertices[index++] = computePos != null ? computePos(k) : k;
            }
        }

        index = 0;
        int offset = 0;
        int nextOffset = offset;
        //Quads
        for (int i = 0; i < nSegments.y; i++)
        {
            nextOffset = offset + nSegments.x + 1;
            for (int j = 0; j < nSegments.x; j++)
            {
                quads[index++] = offset + j;
                quads[index++] = nextOffset + j;
                quads[index++] = nextOffset + j + 1;
                quads[index++] = offset + j + 1;
            }
            offset = nextOffset;
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
        int[] quads = new int[] {
            1,2,3,4,
            1,5,6,2,
            2,6,7,3,
            3,7,8,4,
            4,8,5,1,
            5,8,7,6
        };


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
    Mesh CreateCage(Vector3 halfSize) //Dérivé de Box et Chips 
    {
        Mesh mesh = new Mesh();
        mesh.name = "cage";

        Vector3[] vertices = new Vector3[8];

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
        int[] quads = new int[] {
            4,3,2,1,
            2,6,5,1,
            4,8,7,3,
            1,5,8,4
        };



        for (int i = 0; i < quads.Length; i++)
        {
            quads[i]--;
        }
        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }
    Mesh Create3DPacman(Vector3 halfSize, int nSectors, float startAngle = Mathf.PI / 3, float endAngle = 5 * Mathf.PI / 3)
    {
        Mesh mesh = new Mesh();
        mesh.name = "3DPacman";

        Vector3[] vertices = new Vector3[12];

        //vertices
        vertices[0] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        vertices[1] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[2] = new Vector3(halfSize.x, halfSize.y/2, -halfSize.z);
        vertices[3] = new Vector3(-halfSize.x, halfSize.y/2, -halfSize.z);

        vertices[4] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        vertices[5] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        vertices[6] = new Vector3(halfSize.x, -halfSize.y/2, -halfSize.z);
        vertices[7] = new Vector3(-halfSize.x, -halfSize.y/2, -halfSize.z);

        vertices[8] = new Vector3(-halfSize.x, 0, halfSize.z);
        vertices[9] = new Vector3(halfSize.x, 0, halfSize.z);
        vertices[10] = new Vector3(halfSize.x, 0, halfSize.z/2);
        vertices[11] = new Vector3(-halfSize.x, 0, halfSize.z/2);

        //quads 10
        int[] quads = new int[] {
            0,1,2,3,
            4,7,6,5,
            0,8,9,1,
            8,4,5,9,
            1,9,10,2,
            9,5,6,10,
            0,3,11,8,
            8,11,7,4,


        };


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

        index = 0;
        //Quads
        for (int i = 0; i < nSectors * 2; i += 2)
        {
            quads[index++] = nSectors * 2;
            quads[index++] = (i + 1) % (nSectors * 2);
            quads[index++] = i % (nSectors * 2);
            quads[index++] = (nSectors * 2 + i - 1) % (nSectors * 2);
        }


        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }
    Mesh CreateCylindre(Vector3 halfSize, int nSectors)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Cylindre";
        Vector3[] vertices = new Vector3[nSectors * 4 + 2];
        int[] quads = new int[nSectors * 4 * 4];

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
            vertices[index++] = new Vector3(x, -halfSize.y, z);
            vertices[index++] = Vector3.Lerp(new Vector3(x, -halfSize.y, z), new Vector3(x2, -halfSize.y, z2), 0.5f);
            vertices[index++] = Vector3.Lerp(new Vector3(x, halfSize.y, z), new Vector3(x2, halfSize.y, z2), 0.5f);
            vertices[index++] = new Vector3(x, halfSize.y, z);
        }
        vertices[nSectors * 4] = Vector3.down * halfSize.y;
        vertices[nSectors * 4 + 1] = Vector3.up * halfSize.y;

        index = 0;
        //Quads
        for (int i = 0; i < nSectors * 4; i += 4)
        {
            //0-1-20-17
            //sol
            quads[index++] = i % (nSectors * 4);
            quads[index++] = (i + 1) % (nSectors * 4);
            quads[index++] = nSectors * 4;
            quads[index++] = (nSectors * 4 + i - 3) % (nSectors * 4);

            //0-17-18-3
            //mur gauche
            quads[index++] = i % (nSectors * 4);
            quads[index++] = (nSectors * 4 + i - 3) % (nSectors * 4);
            quads[index++] = (nSectors * 4 + i - 2) % (nSectors * 4);
            quads[index++] = (i + 3) % (nSectors * 4);

            //0-3-2-1
            //mur droit
            quads[index++] = i % (nSectors * 4);
            quads[index++] = (i + 3) % (nSectors * 4);
            quads[index++] = (i + 2) % (nSectors * 4);
            quads[index++] = (i + 1) % (nSectors * 4);

            //21-2-3-18
            //toit droit
            quads[index++] = nSectors * 4 + 1;
            quads[index++] = (i + 2) % (nSectors * 4);
            quads[index++] = (i + 3) % (nSectors * 4);
            quads[index++] = (nSectors * 4 + i - 2) % (nSectors * 4);

        }


        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }
    Mesh CreateCone(Vector3 halfSize, int nSectors)
    {
        Mesh mesh = new Mesh();
        mesh.name = "regularPolygon";

        Vector3[] vertices = new Vector3[nSectors * 2 + 2];
        int[] quads = new int[nSectors * 4 * 2];

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
            vertices[index++] = new Vector3(x, -halfSize.y, z);
            vertices[index++] = Vector3.Lerp(new Vector3(x, -halfSize.y, z), new Vector3(x2, -halfSize.y, z2), 0.5f);
        }
        vertices[nSectors * 2] = Vector3.down * halfSize.y;
        vertices[nSectors * 2 + 1] = Vector3.up * halfSize.y;

        index = 0;
        //Quads
        for (int i = 0; i < nSectors * 2; i += 2)
        {
            quads[index++] = nSectors * 2;
            quads[index++] = (nSectors * 2 + i - 1) % (nSectors * 2);
            quads[index++] = i % (nSectors * 2);
            quads[index++] = (i + 1) % (nSectors * 2);

            quads[index++] = nSectors * 2 + 1;
            quads[index++] = (i + 1) % (nSectors * 2);
            quads[index++] = i % (nSectors * 2);
            quads[index++] = (nSectors * 2 + i - 1) % (nSectors * 2);
        }


        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }
    Mesh CreateBlueLock(Vector3 halfSize, int nSectors) //Dérivé de CreateRegularPolygon 
    {
        Mesh mesh = new Mesh();
        mesh.name = "BlueLock";
        Vector3[] vertices = new Vector3[nSectors * 6 + 1];
        int[] quads = new int[nSectors * 5 * 4];

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
            vertices[index++] = Vector3.Lerp(new Vector3(x, halfSize.y, z), new Vector3(x2, halfSize.y, z2), 0.5f);
            vertices[index++] = new Vector3(x, halfSize.y, z);
            vertices[index++] = Vector3.Lerp(new Vector3(0, halfSize.y, 0), new Vector3(x, halfSize.y, z), 0.4f);
            vertices[index++] = Vector3.Lerp(new Vector3(0, halfSize.y, 0), Vector3.Lerp(new Vector3(x, halfSize.y, z), new Vector3(x2, halfSize.y, z2), 0.5f), 0.4f);
        }
        vertices[nSectors * 6] = Vector3.zero;

        index = 0;
        //Quads
        for (int i = 0; i < nSectors * 6; i += 6)
        {
            //30-1-0-25
            //sol
            quads[index++] = nSectors * 6; 
            quads[index++] = (i + 1) % (nSectors * 6);
            quads[index++] = i % (nSectors * 6);
            quads[index++] = (nSectors * 6 + i - 5) % (nSectors * 6);

            //25-0-3-26
            //mur droit
            quads[index++] = (nSectors * 6 + i - 5) % (nSectors * 6);
            quads[index++] = i % (nSectors * 6);
            quads[index++] = (i + 3) % (nSectors * 6);
            quads[index++] = (nSectors * 6 + i - 4) % (nSectors * 6);

            //0-1-2-3
            //mur gauche
            quads[index++] = i % (nSectors * 6);
            quads[index++] = (i + 1) % (nSectors * 6);
            quads[index++] = (i + 2) % (nSectors * 6);
            quads[index++] = (i + 3) % (nSectors * 6);

            //
            //toit droit

            quads[index++] = (i + 3) % (nSectors * 6);
            quads[index++] = (i + 4) % (nSectors * 6);
            quads[index++] = (nSectors * 6 + i - 1) % (nSectors * 6);
            quads[index++] = (nSectors * 6 + i - 4) % (nSectors * 6);

            ////toit gauche

            quads[index++] = (i + 5) % (nSectors * 6);
            quads[index++] = (i + 4) % (nSectors * 6);
            quads[index++] = (i + 3) % (nSectors * 6);
            quads[index++] = (i + 2) % (nSectors * 6);
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
    void Reverse(Mesh mesh)
    {
        Mesh reverseMesh = new Mesh();
        mesh.name = "reverse"+mesh.name;

        Vector3[] vertices = mesh.vertices;

        int[] quads = mesh.GetIndices(0);

        int[] reverse_quads = new int[quads.Length];

        int index = 0;
        for (int i = 0; i < quads.Length/4; i++)
        {
            //quad's vertices index
            int[] quad_index = new int[4];
            for (int j = 0; j < 4; j++)
                quad_index[j] = quads[4 * i + j];

            reverse_quads[index++] = quad_index[0];
            reverse_quads[index++] = quad_index[3];
            reverse_quads[index++] = quad_index[2];
            reverse_quads[index++] = quad_index[1];
        }


        reverseMesh.vertices = vertices;
        reverseMesh.SetIndices(reverse_quads, MeshTopology.Quads, 0);
        m_Mf.mesh = reverseMesh;

    }
    public string ConvertToCSV(string separator = "\t")
    {
        if (!(m_Mf && m_Mf.mesh)) return "";
        string str = "";
        switch (mesh_type)
        {
            case MeshType.WingedEdgeMesh:
                str = m_WingedEdgeMesh.ConvertToCSVFormat();
                break;
            case MeshType.HalfEdgeMesh:
                str = m_HalfEdgeMesh.ConvertToCSVFormat();
                break;
            case MeshType.VertexFaceMesh:
                Vector3[] vertices = m_Mf.mesh.vertices;
                int[] quads = m_Mf.mesh.GetIndices(0);

                List<string> strings = new List<string>();

                for (int i = 0; i < vertices.Length; i++)
                    strings.Add(i.ToString() + separator + vertices[i].x.ToString("N03") + " " + vertices[i].y.ToString("N03") + " " + vertices[i].z.ToString("N03") + separator + separator);

                for (int i = vertices.Length; i < quads.Length / 4; i++)
                    strings.Add(separator + separator + separator);

                for (int i = 0; i < quads.Length / 4; i++)
                    strings[i] += i.ToString() + separator + quads[4 * i + 0].ToString() + "," + quads[4 * i + 1].ToString() + "," + quads[4 * i + 2].ToString() + "," + quads[4 * i + 3].ToString();

                str = "Vertices" + separator + separator + separator + "Faces\n" + "Index" + separator + "Position" + separator + separator + "Index" + separator + "Indices des vertices\n" + string.Join("\n", strings);

                break;
        }
        Debug.Log(str);
        return str;
    }
    private void OnDrawGizmos()
    {

        if (!(m_Mf && m_Mf.mesh)) return;
        Mesh mesh = m_Mf.mesh;

        //WingedEdgeDrawGizmos
        if (mesh_type == MeshType.WingedEdgeMesh)
        {
            WingedEdgeMesh wingedEdgeMesh = m_WingedEdgeMesh;
            wingedEdgeMesh.DrawGizmos(m_DisplayMeshVertices, m_DisplayMeshEdges, m_DisplayMeshFaces, transform);
        }
        //HalfEdgeDrawGizmos
        if (mesh_type == MeshType.HalfEdgeMesh)
        {
            HalfEdgeMesh halfEdgeMesh = m_HalfEdgeMesh;
            halfEdgeMesh.DrawGizmos(m_DisplayMeshVertices, m_DisplayMeshEdges, m_DisplayMeshFaces, transform);
        }

        Vector3[] vertices = mesh.vertices;
        int[] quads = mesh.GetIndices(0);

        Gizmos.color = Color.black;
        GUIStyle style = new GUIStyle();
        style.fontSize = 12;

        if (m_DisplayMeshInfos)
        {
            style.normal.textColor = Color.red;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(vertices[i]);
                Handles.Label(worldPos, i.ToString(), style);
            }
        }
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

            if (m_DisplayMeshWires)
            {
                Gizmos.DrawLine(pt1, pt2);
                Gizmos.DrawLine(pt2, pt3);
                Gizmos.DrawLine(pt3, pt4);
                Gizmos.DrawLine(pt4, pt1);

            }
            if (m_DisplayMeshInfos)
            {
                style.normal.textColor = Color.green;
                string str = string.Format("{0}:{1},{2},{3},{4}", i, index1, index2, index3, index4);
                Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, str, style);
            }
        
        }


    }

}
