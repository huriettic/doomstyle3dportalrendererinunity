using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct Triangle
{
    public float3 v0, v1, v2;
    public float3 uv0, uv1, uv2;
};

public struct Edge
{
    public float3 v0;
    public float3 v1;
};

public struct MathematicalPlane
{
    public float3 normal;
    public float distance;
};

public struct StartPos
{
    public float3 start;
    public int sectorId;
};

public struct PolygonMeta
{
    public int lineStartIndex;
    public int lineCount;

    public int opaqueStartIndex;
    public int opaqueCount;

    public int collisionStartIndex;
    public int collisionCount;

    public int connectedSectorId;
    public int sectorId;

    public int plane;
};

public struct SectorMeta
{
    public int polygonStartIndex;
    public int polygonCount;

    public int planeStartIndex;
    public int planeCount;

    public int sectorId;
};

[BurstCompile]
public struct SectorsJob : IJobParallelFor
{
    [ReadOnly] public float3 point;
    [ReadOnly] public NativeList<SectorMeta> sectors;
    [ReadOnly] public NativeList<PolygonMeta> polygons;
    [ReadOnly] public NativeList<Triangle> opaques;
    [ReadOnly] public NativeList<Edge> edges;
    [ReadOnly] public NativeList<MathematicalPlane> planes;
    [ReadOnly] public NativeList<MathematicalPlane> originalFrustum;
    [ReadOnly] public NativeArray<MathematicalPlane> currentFrustums;
    [ReadOnly] public NativeList<SectorMeta> contains;
    [ReadOnly] public NativeList<SectorMeta> currentSectors;

    [NativeDisableParallelForRestriction]
    public NativeArray<float3> outedges;

    [NativeDisableParallelForRestriction]
    public NativeArray<float3> processvertices;

    [NativeDisableParallelForRestriction]
    public NativeArray<float3> processtextures;

    [NativeDisableParallelForRestriction]
    public NativeArray<bool> processbool;

    [NativeDisableParallelForRestriction]
    public NativeArray<float3> temporaryvertices;

    [NativeDisableParallelForRestriction]
    public NativeArray<float3> temporarytextures;

    [NativeDisableParallelForRestriction]
    public NativeArray<MathematicalPlane> nextFrustums;

    public NativeList<SectorMeta>.ParallelWriter nextSectors;
    public NativeList<Triangle>.ParallelWriter triangles;

    public void Execute(int index)
    {
        int planeStartIndex = 0;

        int baseIndex = index * 256;

        SectorMeta sector = currentSectors[index];

        for (int a = sector.polygonStartIndex; a < sector.polygonStartIndex + sector.polygonCount; a++)
        {
            PolygonMeta polygon = polygons[a];

            float planeDistance =  math.dot(planes[polygon.plane].normal, point) + planes[polygon.plane].distance;

            if (planeDistance <= 0)
            {
                continue;
            }

            int connectedsector = polygon.connectedSectorId;

            if (connectedsector == -1)
            {
                for (int b = polygon.opaqueStartIndex; b < polygon.opaqueStartIndex + polygon.opaqueCount; b++)
                {
                    int processverticescount = 0;
                    int processtexturescount = 0;
                    int processboolcount = 0;

                    Triangle inTri = opaques[b];

                    processvertices[baseIndex + processverticescount] = inTri.v0;
                    processvertices[baseIndex + processverticescount + 1] = inTri.v1;
                    processvertices[baseIndex + processverticescount + 2] = inTri.v2;
                    processverticescount += 3;
                    processtextures[baseIndex + processtexturescount] = inTri.uv0;
                    processtextures[baseIndex + processtexturescount + 1] = inTri.uv1;
                    processtextures[baseIndex + processtexturescount + 2] = inTri.uv2;
                    processtexturescount += 3;
                    processbool[baseIndex + processboolcount] = true;
                    processbool[baseIndex + processboolcount + 1] = true;
                    processbool[baseIndex + processboolcount + 2] = true;
                    processboolcount += 3;

                    for (int c = sector.planeStartIndex; c < sector.planeStartIndex + sector.planeCount; c++)
                    {
                        int addTriangles = 0;

                        int temporaryverticescount = 0;
                        int temporarytexturescount = 0;

                        for (int d = baseIndex; d < baseIndex + processverticescount; d += 3)
                        {
                            if (processbool[d] == false && processbool[d + 1] == false && processbool[d + 2] == false)
                            {
                                continue;
                            }

                            float3 v0 = processvertices[d];
                            float3 v1 = processvertices[d + 1];
                            float3 v2 = processvertices[d + 2];

                            float3 uv0 = processtextures[d];
                            float3 uv1 = processtextures[d + 1];
                            float3 uv2 = processtextures[d + 2];

                            float d0 = math.dot(currentFrustums[c].normal, v0) + currentFrustums[c].distance;
                            float d1 = math.dot(currentFrustums[c].normal, v1) + currentFrustums[c].distance;
                            float d2 = math.dot(currentFrustums[c].normal, v2) + currentFrustums[c].distance;

                            bool b0 = d0 >= 0;
                            bool b1 = d1 >= 0;
                            bool b2 = d2 >= 0;

                            if (b0 && b1 && b2)
                            {
                                continue;
                            }
                            else if ((b0 && !b1 && !b2) || (!b0 && b1 && !b2) || (!b0 && !b1 && b2))
                            {
                                float3 inV, outV1, outV2;
                                float3 inUV, outUV1, outUV2;
                                float inD, outD1, outD2;

                                if (b0)
                                {
                                    inV = v0;
                                    inUV = uv0;
                                    inD = d0;
                                    outV1 = v1;
                                    outUV1 = uv1;
                                    outD1 = d1;
                                    outV2 = v2;
                                    outUV2 = uv2;
                                    outD2 = d2;
                                }
                                else if (b1)
                                {
                                    inV = v1;
                                    inUV = uv1;
                                    inD = d1;
                                    outV1 = v2;
                                    outUV1 = uv2;
                                    outD1 = d2;
                                    outV2 = v0;
                                    outUV2 = uv0;
                                    outD2 = d0;
                                }
                                else
                                {
                                    inV = v2;
                                    inUV = uv2;
                                    inD = d2;
                                    outV1 = v0;
                                    outUV1 = uv0;
                                    outD1 = d0;
                                    outV2 = v1;
                                    outUV2 = uv1;
                                    outD2 = d1;
                                }

                                float t1 = inD / (inD - outD1);
                                float t2 = inD / (inD - outD2);

                                temporaryvertices[baseIndex + temporaryverticescount] = inV;
                                temporaryvertices[baseIndex + temporaryverticescount + 1] = math.lerp(inV, outV1, t1);
                                temporaryvertices[baseIndex + temporaryverticescount + 2] = math.lerp(inV, outV2, t2);
                                temporaryverticescount += 3;
                                temporarytextures[baseIndex + temporarytexturescount] = inUV;
                                temporarytextures[baseIndex + temporarytexturescount + 1] = math.lerp(inUV, outUV1, t1);
                                temporarytextures[baseIndex + temporarytexturescount + 2] = math.lerp(inUV, outUV2, t2);
                                temporarytexturescount += 3;
                                processbool[d] = false;
                                processbool[d + 1] = false;
                                processbool[d + 2] = false;

                                addTriangles += 1;
                            }
                            else if ((!b0 && b1 && b2) || (b0 && !b1 && b2) || (b0 && b1 && !b2))
                            {
                                float3 inV1, inV2, outV;
                                float3 inUV1, inUV2, outUV;
                                float inD1, inD2, outD;

                                if (!b0)
                                {
                                    outV = v0;
                                    outUV = uv0;
                                    outD = d0;
                                    inV1 = v1;
                                    inUV1 = uv1;
                                    inD1 = d1;
                                    inV2 = v2;
                                    inUV2 = uv2;
                                    inD2 = d2;
                                }
                                else if (!b1)
                                {
                                    outV = v1;
                                    outUV = uv1;
                                    outD = d1;
                                    inV1 = v2;
                                    inUV1 = uv2;
                                    inD1 = d2;
                                    inV2 = v0;
                                    inUV2 = uv0;
                                    inD2 = d0;
                                }
                                else
                                {
                                    outV = v2;
                                    outUV = uv2;
                                    outD = d2;
                                    inV1 = v0;
                                    inUV1 = uv0;
                                    inD1 = d0;
                                    inV2 = v1;
                                    inUV2 = uv1;
                                    inD2 = d1;
                                }

                                float t1 = inD1 / (inD1 - outD);
                                float t2 = inD2 / (inD2 - outD);

                                float3 vA = math.lerp(inV1, outV, t1);
                                float3 vB = math.lerp(inV2, outV, t2);

                                float3 uvA = math.lerp(inUV1, outUV, t1);
                                float3 uvB = math.lerp(inUV2, outUV, t2);

                                temporaryvertices[baseIndex + temporaryverticescount] = inV1;
                                temporaryvertices[baseIndex + temporaryverticescount + 1] = inV2;
                                temporaryvertices[baseIndex + temporaryverticescount + 2] = vA;
                                temporaryverticescount += 3;
                                temporarytextures[baseIndex + temporarytexturescount] = inUV1;
                                temporarytextures[baseIndex + temporarytexturescount + 1] = inUV2;
                                temporarytextures[baseIndex + temporarytexturescount + 2] = uvA;
                                temporarytexturescount += 3;
                                temporaryvertices[baseIndex + temporaryverticescount] = vA;
                                temporaryvertices[baseIndex + temporaryverticescount + 1] = inV2;
                                temporaryvertices[baseIndex + temporaryverticescount + 2] = vB;
                                temporaryverticescount += 3;
                                temporarytextures[baseIndex + temporarytexturescount] = uvA;
                                temporarytextures[baseIndex + temporarytexturescount + 1] = inUV2;
                                temporarytextures[baseIndex + temporarytexturescount + 2] = uvB;
                                temporarytexturescount += 3;
                                processbool[d] = false;
                                processbool[d + 1] = false;
                                processbool[d + 2] = false;

                                addTriangles += 2;
                            }
                            else
                            {
                                processbool[d] = false;
                                processbool[d + 1] = false;
                                processbool[d + 2] = false;
                            }
                        }

                        if (addTriangles > 0)
                        {
                            for (int e = baseIndex; e < baseIndex + temporaryverticescount; e += 3)
                            {
                                processvertices[baseIndex + processverticescount] = temporaryvertices[e];
                                processvertices[baseIndex + processverticescount + 1] = temporaryvertices[e + 1];
                                processvertices[baseIndex + processverticescount + 2] = temporaryvertices[e + 2];
                                processverticescount += 3;
                                processtextures[baseIndex + processtexturescount] = temporarytextures[e];
                                processtextures[baseIndex + processtexturescount + 1] = temporarytextures[e + 1];
                                processtextures[baseIndex + processtexturescount + 2] = temporarytextures[e + 2];
                                processtexturescount += 3;
                                processbool[baseIndex + processboolcount] = true;
                                processbool[baseIndex + processboolcount + 1] = true;
                                processbool[baseIndex + processboolcount + 2] = true;
                                processboolcount += 3;
                            }
                        }
                    }

                    for (int f = baseIndex; f < baseIndex + processboolcount; f += 3)
                    {
                        if (processbool[f] == true && processbool[f + 1] == true && processbool[f + 2] == true)
                        {
                            Triangle outTri = new Triangle
                            {
                                v0 = processvertices[f],
                                v1 = processvertices[f + 1],
                                v2 = processvertices[f + 2],
                                uv0 = processtextures[f],
                                uv1 = processtextures[f + 1],
                                uv2 = processtextures[f + 2]
                            };

                            triangles.AddNoResize(outTri);
                        }
                    }
                }

                continue;
            }

            SectorMeta sectorpolygon = sectors[connectedsector];

            int connectedstart = sectorpolygon.polygonStartIndex;
            int connectedcount = sectorpolygon.polygonCount;

            bool contact = false;

            for (int g = 0; g < contains.Length; g++)
            {
                if (contains[g].sectorId == sectorpolygon.sectorId)
                {
                    contact = true;
                }
            }

            if (contact)
            {
                int contactIndex = baseIndex + planeStartIndex;

                nextFrustums[contactIndex] = originalFrustum[0];
                nextFrustums[contactIndex + 1] = originalFrustum[1];
                nextFrustums[contactIndex + 2] = originalFrustum[2];
                nextFrustums[contactIndex + 3] = originalFrustum[3];

                SectorMeta contactnext = new SectorMeta
                {
                    polygonStartIndex = connectedstart,
                    polygonCount = connectedcount,
                    planeStartIndex = contactIndex,
                    planeCount = originalFrustum.Length,
                    sectorId = connectedsector
                };

                planeStartIndex += 4;

                nextSectors.AddNoResize(contactnext);

                continue;
            }

            int outedgescount = 0;
            int processedgescount = 0;
            int processedgesboolcount = 0;

            for (int h = polygon.lineStartIndex; h < polygon.lineStartIndex + polygon.lineCount; h++)
            {
                Edge line = edges[h];
                processvertices[baseIndex + processedgescount] = line.v0;
                processvertices[baseIndex + processedgescount + 1] = line.v1;
                processedgescount += 2;
                processbool[baseIndex + processedgesboolcount] = true;
                processbool[baseIndex + processedgesboolcount + 1] = true;
                processedgesboolcount += 2;
            }

            for (int i = sector.planeStartIndex; i < sector.planeStartIndex + sector.planeCount; i++)
            {
                int intersection = 0;
                int temporaryverticescount = 0;

                float3 intersectionPoint1 = float3.zero;
                float3 intersectionPoint2 = float3.zero;

                for (int j = baseIndex; j < baseIndex + processedgescount; j += 2)
                {
                    if (processbool[j] == false && processbool[j + 1] == false)
                    {
                        continue;
                    }

                    float3 p1 = processvertices[j];
                    float3 p2 = processvertices[j + 1];

                    float d1 = math.dot(currentFrustums[i].normal, p1) + currentFrustums[i].distance;
                    float d2 = math.dot(currentFrustums[i].normal, p2) + currentFrustums[i].distance;

                    bool b0 = d1 >= 0;
                    bool b1 = d2 >= 0;

                    if (b0 && b1)
                    {
                        continue;
                    }
                    else if ((b0 && !b1) || (!b0 && b1))
                    {
                        float3 point1;
                        float3 point2;

                        float t = d1 / (d1 - d2);

                        float3 intersectionPoint = math.lerp(p1, p2, t);

                        if (b0)
                        {
                            point1 = p1;
                            point2 = intersectionPoint;
                            intersectionPoint1 = intersectionPoint;
                        }
                        else
                        {
                            point1 = intersectionPoint;
                            point2 = p2;
                            intersectionPoint2 = intersectionPoint;
                        }

                        temporaryvertices[baseIndex + temporaryverticescount] = point1;
                        temporaryvertices[baseIndex + temporaryverticescount + 1] = point2;
                        temporaryverticescount += 2;

                        processbool[j] = false;
                        processbool[j + 1] = false;

                        intersection += 1;
                    }
                    else
                    {
                        processbool[j] = false;
                        processbool[j + 1] = false;
                    }
                }

                if (intersection == 2)
                {
                    for (int k = baseIndex; k < baseIndex + temporaryverticescount; k += 2)
                    {
                        processvertices[baseIndex + processedgescount] = temporaryvertices[k];
                        processvertices[baseIndex + processedgescount + 1] = temporaryvertices[k + 1];
                        processedgescount += 2;
                        processbool[baseIndex + processedgesboolcount] = true;
                        processbool[baseIndex + processedgesboolcount + 1] = true;
                        processedgesboolcount += 2;
                    }

                    processvertices[baseIndex + processedgescount] = intersectionPoint1;
                    processvertices[baseIndex + processedgescount + 1] = intersectionPoint2;
                    processedgescount += 2;
                    processbool[baseIndex + processedgesboolcount] = true;
                    processbool[baseIndex + processedgesboolcount + 1] = true;
                    processedgesboolcount += 2;
                }
            }

            for (int l = baseIndex; l < baseIndex + processedgesboolcount; l += 2)
            {
                if (processbool[l] == true && processbool[l + 1] == true)
                {
                    outedges[baseIndex + outedgescount] = processvertices[l];
                    outedges[baseIndex + outedgescount + 1] = processvertices[l + 1];
                    outedgescount += 2;
                }
            }

            if (outedgescount < 6 || outedgescount % 2 == 1)
            {
                continue;
            }

            int StartIndex = baseIndex + planeStartIndex;

            int IndexCount = 0;

            for (int m = baseIndex; m < baseIndex + outedgescount; m += 2)
            {
                float3 p1 = outedges[m];
                float3 p2 = outedges[m + 1];
                float3 normal = math.cross(p1 - p2, point - p2);
                float magnitude = math.length(normal);

                if (magnitude < 0.01f)
                {
                    continue;
                }

                float3 normalized = normal / magnitude;

                nextFrustums[StartIndex + IndexCount] = new MathematicalPlane { normal = normalized, distance = -math.dot(normalized, p1) };
                planeStartIndex += 1;
                IndexCount += 1;
            }

            SectorMeta next = new SectorMeta
            {
                polygonStartIndex = connectedstart,
                polygonCount = connectedcount,
                planeStartIndex = StartIndex,
                planeCount = IndexCount,
                sectorId = connectedsector
            };

            nextSectors.AddNoResize(next);
        }
    }
}

public class LevelLoader : MonoBehaviour
{
    public string Name = "twohallways-clear";

    public float speed = 7f;
    public float jumpHeight = 2f;
    public float gravity = 5f;
    public float sensitivity = 10f;
    public float clampAngle = 90f;
    public float smoothFactor = 25f;

    private Vector2 targetRotation;
    private Vector3 targetMovement;
    private Vector2 currentRotation;
    private Vector3 currentForce;

    private CharacterController Player;
    private Collider playerCollider;
    private TopLevelLists LevelLists;
    private List<Vector2> vertices = new List<Vector2>();
    private List<int> triangles = new List<int>();
    private List<Sector> sectors = new List<Sector>();
    private List<PlayerStart> starts = new List<PlayerStart>();
    private List<Vector3> transformedvertices = new List<Vector3>();
    private List<Mesh> meshes = new List<Mesh>();
    private List<int> Plane = new List<int>();
    private List<int> Portal = new List<int>();
    private List<int> Render = new List<int>();
    private List<int> Collision = new List<int>();
    private List<Vector3> uvVector3 = new List<Vector3>();
    private List<Vector3> ceilingverts = new List<Vector3>();
    private List<int> ceilingtri = new List<int>();
    private List<Vector3> floorverts = new List<Vector3>();
    private List<int> floortri = new List<int>();
    private Material opaquematerial;
    private List<MeshCollider> ColliderSectors = new List<MeshCollider>();
    private List<Vector3> ColliderVertices = new List<Vector3>();
    private List<int> ColliderTriangles = new List<int>();
    private List<Mesh> CollisionMesh = new List<Mesh>();
    private GameObject CollisionObjects;
    private NativeArray<bool> processbool;
    private NativeArray<float3> processvertices;
    private NativeArray<float3> processtextures;
    private NativeArray<float3> temporaryvertices;
    private NativeArray<float3> temporarytextures;
    private NativeArray<float3> outedges;
    private NativeArray<MathematicalPlane> planeA;
    private NativeArray<MathematicalPlane> planeB;
    private NativeList<SectorMeta> sideA;
    private NativeList<SectorMeta> sideB;
    private NativeList<Triangle> outTriangles;
    private NativeList<SectorMeta> contains;
    private NativeList<MathematicalPlane> OriginalFrustum;
    private List<List<SectorMeta>> ListOfSectorLists = new List<List<SectorMeta>>();
    private Camera Cam;
    private float3 CamPoint;
    private SectorMeta CurrentSector;
    private NativeList<SectorMeta> oldContains;
    private bool radius;
    private bool check;
    private double Ceiling;
    private double Floor;
    private Plane LeftPlane;
    private Plane TopPlane;
    private List<Vector3> uvs = new List<Vector3>();
    private List<Vector3> flooruvs = new List<Vector3>();
    private List<Vector3> ceilinguvs = new List<Vector3>();
    private GraphicsBuffer triBuffer;

    [Serializable]
    public class Sector
    {
        public float floorHeight;
        public float ceilingHeight;
        public List<int> vertexIndices = new List<int>();
        public List<int> wallTypes = new List<int>(); // -1 for solid, sector index for portal
    }

    [Serializable]
    public class PlayerStart
    {
        public Vector3 location;
        public float angle;
        public int sector;
    }

    public class TopLevelLists
    {
        public NativeList<Edge> edges;
        public NativeList<Triangle> opaques;
        public NativeList<Triangle> collisions;
        public NativeList<MathematicalPlane> planes;
        public NativeList<PolygonMeta> polygons;
        public NativeList<SectorMeta> sectors;
        public NativeList<StartPos> positions;
    }

    void Start()
    {
        LoadFromFile();

        triangles = new List<int>()
        {
            0, 1, 2, 0, 2, 3
        };

        LevelLists = new TopLevelLists();

        LevelLists.edges = new NativeList<Edge>(Allocator.Persistent);
        LevelLists.opaques = new NativeList<Triangle>(Allocator.Persistent);
        LevelLists.collisions = new NativeList<Triangle>(Allocator.Persistent);
        LevelLists.sectors = new NativeList<SectorMeta>(Allocator.Persistent);
        LevelLists.planes = new NativeList<MathematicalPlane>(Allocator.Persistent);
        LevelLists.polygons = new NativeList<PolygonMeta>(Allocator.Persistent);
        LevelLists.positions = new NativeList<StartPos>(Allocator.Persistent);

        CollisionObjects = new GameObject("Collision Meshes");

        CreateMaterial();

        BuildGeometry();

        BuildLists();

        BuildObjects();

        BuildColliders();

        Playerstart();

        processbool = new NativeArray<bool>(256 * 256, Allocator.Persistent);
        processvertices = new NativeArray<float3>(256 * 256, Allocator.Persistent);
        processtextures = new NativeArray<float3>(256 * 256, Allocator.Persistent);
        temporaryvertices = new NativeArray<float3>(256 * 256, Allocator.Persistent);
        temporarytextures = new NativeArray<float3>(256 * 256, Allocator.Persistent);
        outedges = new NativeArray<float3>(256 * 256, Allocator.Persistent);
        planeA = new NativeArray<MathematicalPlane>(256 * 256, Allocator.Persistent);
        planeB = new NativeArray<MathematicalPlane>(256 * 256, Allocator.Persistent);
        contains = new NativeList<SectorMeta>(Allocator.Persistent);
        sideA = new NativeList<SectorMeta>(LevelLists.sectors.Length, Allocator.Persistent);
        sideB = new NativeList<SectorMeta>(LevelLists.sectors.Length, Allocator.Persistent);
        outTriangles = new NativeList<Triangle>(LevelLists.opaques.Length * 4, Allocator.Persistent);
        OriginalFrustum = new NativeList<MathematicalPlane>(Allocator.Persistent);
        oldContains = new NativeList<SectorMeta>(Allocator.Persistent);

        int strideTriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));

        triBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, LevelLists.opaques.Length * 4, strideTriangle);

        for (int i = 0; i < 2; i++)
        {
            ListOfSectorLists.Add(new List<SectorMeta>());
        }

        for (int i = 0; i < LevelLists.sectors.Length; i++)
        {
            Physics.IgnoreCollision(playerCollider, ColliderSectors[LevelLists.sectors[i].sectorId], true);
        }
    }

    void Update()
    {
        PlayerInput();

        if (Cam.transform.hasChanged)
        {
            CamPoint = Cam.transform.position;

            GetSectors(CurrentSector);

            OriginalFrustum.Clear();

            ReadFrustumPlanes(Cam, OriginalFrustum);

            OriginalFrustum.RemoveAt(5);

            OriginalFrustum.RemoveAt(4);

            GetPolygons(CurrentSector);

            SetGPURenderer();

            Cam.transform.hasChanged = false;
        }
    }

    void Awake()
    {
        Player = GameObject.Find("Player").GetComponent<CharacterController>();
        playerCollider = Player.GetComponent<Collider>();

        Player.enabled = true;
        playerCollider.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cam = Camera.main;
    }

    void FixedUpdate()
    {
        if (!Player.isGrounded)
        {
            currentForce.y -= gravity * Time.deltaTime;
        }
    }

    void OnDestroy()
    {
        triBuffer?.Dispose();

        if (LevelLists.sectors.IsCreated)
        {
            LevelLists.sectors.Dispose();
        }
        if (LevelLists.polygons.IsCreated)
        {
            LevelLists.polygons.Dispose();
        }
        if (LevelLists.opaques.IsCreated)
        {
            LevelLists.opaques.Dispose();
        }
        if (LevelLists.collisions.IsCreated)
        {
            LevelLists.collisions.Dispose();
        }
        if (LevelLists.edges.IsCreated)
        {
            LevelLists.edges.Dispose();
        }
        if (LevelLists.positions.IsCreated)
        {
            LevelLists.positions.Dispose();
        }
        if (LevelLists.planes.IsCreated)
        {
            LevelLists.planes.Dispose();
        }
        if (contains.IsCreated)
        {
            contains.Dispose();
        }
        if (processbool.IsCreated)
        {
            processbool.Dispose();
        }
        if (processvertices.IsCreated)
        {
            processvertices.Dispose();
        }
        if (processtextures.IsCreated)
        {
            processtextures.Dispose();
        }
        if (temporaryvertices.IsCreated)
        {
            temporaryvertices.Dispose();
        }
        if (temporarytextures.IsCreated)
        {
            temporarytextures.Dispose();
        }
        if (outedges.IsCreated)
        {
            outedges.Dispose();
        }
        if (planeA.IsCreated)
        {
            planeA.Dispose();
        }
        if (planeB.IsCreated)
        {
            planeB.Dispose();
        }
        if (sideA.IsCreated)
        {
            sideA.Dispose();
        }
        if (sideB.IsCreated)
        {
            sideB.Dispose();
        }
        if (outTriangles.IsCreated)
        {
            outTriangles.Dispose();
        }
        if (OriginalFrustum.IsCreated)
        {
            OriginalFrustum.Dispose();
        }
        if (oldContains.IsCreated)
        {
            oldContains.Dispose();
        }
    }

    public void CreateMaterial()
    {
        Shader shader = Resources.Load<Shader>("TriangleTexArray");

        opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>("Textures");
    }

    private MathematicalPlane FromVec4(Vector4 aVec)
    {
        Vector3 n = new Vector3(aVec.x, aVec.y, aVec.z);
        float l = n.magnitude;
        return new MathematicalPlane
        {
            normal = n / l,
            distance = aVec.w / l
        };
    }

    public void SetFrustumPlanes(NativeList<MathematicalPlane> planes, Matrix4x4 m)
    {   
        var r0 = m.GetRow(0);
        var r1 = m.GetRow(1);
        var r2 = m.GetRow(2);
        var r3 = m.GetRow(3);

        planes.Add(FromVec4(r3 - r0)); // Right
        planes.Add(FromVec4(r3 + r0)); // Left
        planes.Add(FromVec4(r3 - r1)); // Top
        planes.Add(FromVec4(r3 + r1)); // Bottom
        planes.Add(FromVec4(r3 - r2)); // Far
        planes.Add(FromVec4(r3 + r2)); // Near
    }

    public void ReadFrustumPlanes(Camera cam, NativeList<MathematicalPlane> planes)
    {
        SetFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
    }

    public void PlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if (Input.GetKeyDown(KeyCode.Space) && Player.isGrounded)
        {
            currentForce.y = jumpHeight;
        }

        float mousex = Input.GetAxisRaw("Mouse X");
        float mousey = Input.GetAxisRaw("Mouse Y");

        targetRotation.x -= mousey * sensitivity;
        targetRotation.y += mousex * sensitivity;

        targetRotation.x = Mathf.Clamp(targetRotation.x, -clampAngle, clampAngle);

        currentRotation = Vector2.Lerp(currentRotation, targetRotation, smoothFactor * Time.deltaTime);

        Cam.transform.localRotation = Quaternion.Euler(currentRotation.x, 0f, 0f);
        Player.transform.rotation = Quaternion.Euler(0f, currentRotation.y, 0f);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        targetMovement = (Player.transform.right * horizontal + Player.transform.forward * vertical).normalized;

        Player.Move((targetMovement + currentForce) * speed * Time.deltaTime);
    }

    public float GetPlaneSignedDistanceToPoint(MathematicalPlane plane, Vector3 point)
    {
        return math.dot(plane.normal, point) + plane.distance;
    }

    public void SetGPURenderer()
    {
        triBuffer.SetData(outTriangles.AsArray());
        opaquematerial.SetBuffer("outputTriangleBuffer", triBuffer);
    }

    void OnRenderObject()
    {
        opaquematerial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, outTriangles.Length * 3);
    }

    public bool CheckRadius(SectorMeta asector, Vector3 campoint)
    {
        for (int i = asector.polygonStartIndex; i < asector.polygonStartIndex + asector.polygonCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(LevelLists.planes[LevelLists.polygons[i].plane], campoint) < -0.6f)
            {
                return false;
            }
        }
        return true;
    }

    public bool CheckSector(SectorMeta asector, Vector3 campoint)
    {
        for (int i = asector.polygonStartIndex; i < asector.polygonStartIndex + asector.polygonCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(LevelLists.planes[LevelLists.polygons[i].plane], campoint) < 0)
            {
                return false;
            }
        }
        return true;
    }

    public bool SectorsContains(int sectorID)
    {
        for (int i = 0; i < contains.Length; i++)
        {
            if (contains[i].sectorId == sectorID)
            {
                return true;
            }
        }
        return false;
    }

    public bool SectorsDoNotEqual()
    {
        if (contains.Length != oldContains.Length)
        {
            return true;
        }

        for (int i = 0; i < contains.Length; i++)
        {
            if (contains[i].sectorId != oldContains[i].sectorId)
            {
                return true;
            }
        }
        return false;
    }

    public void GetSectors(SectorMeta ASector)
    {
        int sidea = 0;
        int sideb = 1;

        contains.Clear();

        ListOfSectorLists[sidea].Clear();

        ListOfSectorLists[sidea].Add(ASector);

        for (int a = 0; a < oldContains.Length; a++)
        {
            Physics.IgnoreCollision(playerCollider, ColliderSectors[oldContains[a].sectorId], true);
        }

        for (int b = 0; b < 4096; b++)
        {
            if (b % 2 == 0)
            {
                sidea = 0;
                sideb = 1;
            }
            else
            {
                sidea = 1;
                sideb = 0;
            }

            ListOfSectorLists[sideb].Clear();

            if (ListOfSectorLists[sidea].Count == 0)
            {
                break;
            }

            for (int c = 0; c < ListOfSectorLists[sidea].Count; c++)
            {
                SectorMeta sector = ListOfSectorLists[sidea][c];

                contains.Add(sector);

                Physics.IgnoreCollision(playerCollider, ColliderSectors[sector.sectorId], false);

                for (int d = sector.polygonStartIndex; d < sector.polygonStartIndex + sector.polygonCount; d++)
                {
                    int connectedsector = LevelLists.polygons[d].connectedSectorId;

                    if (connectedsector == -1)
                    {
                        continue;
                    }

                    SectorMeta portalsector = LevelLists.sectors[connectedsector];

                    if (SectorsContains(portalsector.sectorId))
                    {
                        continue;
                    }

                    radius = CheckRadius(portalsector, CamPoint);

                    if (radius)
                    {
                        ListOfSectorLists[sideb].Add(portalsector);
                    }
                }

                check = CheckSector(sector, CamPoint);

                if (check)
                {
                    CurrentSector = sector;
                }
            }    
        }

        if (SectorsDoNotEqual())
        {
            oldContains.Clear();

            for (int e = 0; e < contains.Length; e++)
            {
                oldContains.Add(contains[e]);
            }
        }
    }

    public void GetPolygons(SectorMeta ASector)
    {
        sideA.Clear();
        sideB.Clear();
        outTriangles.Clear();

        int jobCompleted = 0;

        planeA[0] = OriginalFrustum[0];
        planeA[1] = OriginalFrustum[1];
        planeA[2] = OriginalFrustum[2];
        planeA[3] = OriginalFrustum[3];

        sideA.Add(ASector);

        NativeList<SectorMeta> current = sideA;
        NativeList<SectorMeta> next = sideB;
        NativeArray<MathematicalPlane> currentFrustuns = planeA;
        NativeArray<MathematicalPlane> nextFrustums = planeB;

        while (current.Length > 0)
        {
            next.Clear();

            SectorsJob job = new SectorsJob
            {
                point = CamPoint,
                contains = contains,
                originalFrustum = OriginalFrustum,
                planes = LevelLists.planes,
                sectors = LevelLists.sectors,
                polygons = LevelLists.polygons,
                opaques = LevelLists.opaques,
                edges = LevelLists.edges,
                processvertices = processvertices,
                processtextures = processtextures,
                processbool = processbool,
                temporaryvertices = temporaryvertices,
                temporarytextures = temporarytextures,
                outedges = outedges,
                currentFrustums = currentFrustuns,
                currentSectors = current,
                nextSectors = next.AsParallelWriter(),
                nextFrustums = nextFrustums,
                triangles = outTriangles.AsParallelWriter(),
            };

            job.Schedule(current.Length, 32).Complete();

            jobCompleted += 1;

            if (jobCompleted % 2 == 0)
            {
                current = sideA;
                next = sideB;
                currentFrustuns = planeA;
                nextFrustums = planeB;
            }
            else
            {
                current = sideB;
                next = sideA;
                currentFrustuns = planeB;
                nextFrustums = planeA;
            }
        }
    }

    public void Playerstart()
    {
        if (LevelLists.positions.Length == 0)
        {
            Debug.LogError("No player starts available.");

            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, LevelLists.positions.Length);

        StartPos selectedPosition = LevelLists.positions[randomIndex];

        CurrentSector = LevelLists.sectors[selectedPosition.sectorId];

        Player.transform.position = new Vector3(selectedPosition.start.z, selectedPosition.start.y + 1.10f, selectedPosition.start.x);
    }

    public void LoadFromFile()
    {
        TextAsset file = Resources.Load<TextAsset>(Name);
        if (file == null)
        {
            Debug.LogError("File not found in Resources!");
            return;
        }

        string[] lines = file.text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("vertex"))
            {
                string[] parts = lines[i].Split('\t');

                if (parts.Length == 3)
                {
                    float y = float.Parse(parts[1]);

                    string[] xValues = parts[2].Split(' ');

                    for (int e = 0; e < xValues.Length; e++)
                    {
                        if (float.TryParse(xValues[e], out float x))
                        {
                            vertices.Add(new Vector2(x, y));
                        }
                    }
                }
            }

            if (lines[i].StartsWith("sector"))
            {
                Sector sector = new Sector();

                string[] parts = lines[i].Split('\t');

                if (parts.Length == 3)
                {
                    string[] heightParts = parts[1].Split(' ');

                    if (heightParts.Length == 2)
                    {
                        sector.floorHeight = float.Parse(heightParts[0]);

                        sector.ceilingHeight = float.Parse(heightParts[1]);
                    }

                    string[] values = parts[2].Split(' ');

                    int half = values.Length / 2;

                    for (int e = 0; e < values.Length; e++)
                    {
                        if (int.TryParse(values[e], out int val))
                        {
                            if (e < half)
                            {
                                sector.vertexIndices.Add(val);
                            }
                            else
                            {
                                sector.wallTypes.Add(val);
                            }
                        }
                    }
                }

                sectors.Add(sector);
            }

            if (lines[i].StartsWith("player"))
            {
                PlayerStart start = new PlayerStart();

                string[] parts = lines[i].Split('\t');

                if (parts.Length == 4)
                {
                    string[] locationParts = parts[1].Split(' ');

                    if (locationParts.Length == 2)
                    {
                        float x = float.Parse(locationParts[0]);

                        float y = float.Parse(locationParts[1]);

                        start.location = new Vector2(x, y);
                    }

                    start.angle = float.Parse(parts[2]);

                    start.sector = int.Parse(parts[3]);
                }

                starts.Add(start);
            }
        }

        Debug.Log($"Loaded {vertices.Count} vertices.");

        Debug.Log($"Loaded {sectors.Count} sectors.");

        Debug.Log($"Player start: location={starts[0].location}, angle={starts[0].angle}, sector={starts[0].sector}");
    }

    public void BuildGeometry()
    {
        for (int i = 0; i < sectors.Count; i++)
        {
            Sector sector = sectors[i];

            for (int e = 0; e < sector.vertexIndices.Count; e++)
            {
                int current = sector.vertexIndices[e];
                int next = sector.vertexIndices[(e + 1) % sector.vertexIndices.Count];

                int wall = sector.wallTypes[(e + 1) % sector.wallTypes.Count];

                double X1 = vertices[current].x / 2 * 2.5f;
                double Z1 = vertices[current].y / 2 * 2.5f;

                double X0 = vertices[next].x / 2 * 2.5f;
                double Z0 = vertices[next].y / 2 * 2.5f;

                if (wall == -1)
                {
                    double V0 = sector.floorHeight / 8 * 2.5f;
                    double V1 = sector.ceilingHeight / 8 * 2.5f;

                    transformedvertices.Clear();

                    transformedvertices.Add(new Vector3((float)Z1, (float)V0, (float)X1));
                    transformedvertices.Add(new Vector3((float)Z1, (float)V1, (float)X1));
                    transformedvertices.Add(new Vector3((float)Z0, (float)V1, (float)X0));
                    transformedvertices.Add(new Vector3((float)Z0, (float)V0, (float)X0));

                    LeftPlane = new Plane((transformedvertices[2] - transformedvertices[1]).normalized, transformedvertices[1]);
                    TopPlane = new Plane((transformedvertices[1] - transformedvertices[0]).normalized, transformedvertices[1]);

                    uvs.Clear();

                    uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, 3));
                    uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, 3));
                    uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, 3));
                    uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, 3));

                    Plane.Add(i);
                    Render.Add(i);
                    Portal.Add(-1);
                    Collision.Add(i);

                    Mesh transformedmesh = new Mesh();

                    transformedmesh.SetVertices(transformedvertices);
                    transformedmesh.SetUVs(0, uvs);
                    transformedmesh.SetTriangles(triangles, 0, true);

                    meshes.Add(transformedmesh);
                }
                else
                {
                    if (sector.ceilingHeight > sectors[wall].ceilingHeight)
                    {
                        if (sector.floorHeight < sectors[wall].ceilingHeight)
                        {
                            double C0 = sector.ceilingHeight / 8 * 2.5f;

                            if (sector.ceilingHeight > sectors[wall].ceilingHeight)
                            {
                                Ceiling = sectors[wall].ceilingHeight / 8 * 2.5f;
                            }
                            else
                            {
                                Ceiling = sector.ceilingHeight / 8 * 2.5f;
                            }

                            transformedvertices.Clear();

                            transformedvertices.Add(new Vector3((float)Z1, (float)Ceiling, (float)X1));
                            transformedvertices.Add(new Vector3((float)Z1, (float)C0, (float)X1));
                            transformedvertices.Add(new Vector3((float)Z0, (float)C0, (float)X0));
                            transformedvertices.Add(new Vector3((float)Z0, (float)Ceiling, (float)X0));

                            LeftPlane = new Plane((transformedvertices[2] - transformedvertices[1]).normalized, transformedvertices[1]);
                            TopPlane = new Plane((transformedvertices[1] - transformedvertices[0]).normalized, transformedvertices[1]);

                            uvs.Clear();

                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, 3));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, 3));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, 3));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, 3));

                            Plane.Add(i);
                            Render.Add(i);
                            Portal.Add(-1);
                            Collision.Add(i);

                            Mesh transformedmesh = new Mesh();

                            transformedmesh.SetVertices(transformedvertices);
                            transformedmesh.SetUVs(0, uvs);
                            transformedmesh.SetTriangles(triangles, 0, true);

                            meshes.Add(transformedmesh);
                        }
                        else
                        {
                            double C0 = sector.ceilingHeight / 8 * 2.5f;
                            double C1 = sector.floorHeight / 8 * 2.5f;

                            transformedvertices.Clear();

                            transformedvertices.Add(new Vector3((float)Z1, (float)C1, (float)X1));
                            transformedvertices.Add(new Vector3((float)Z1, (float)C0, (float)X1));
                            transformedvertices.Add(new Vector3((float)Z0, (float)C0, (float)X0));
                            transformedvertices.Add(new Vector3((float)Z0, (float)C1, (float)X0));

                            LeftPlane = new Plane((transformedvertices[2] - transformedvertices[1]).normalized, transformedvertices[1]);
                            TopPlane = new Plane((transformedvertices[1] - transformedvertices[0]).normalized, transformedvertices[1]);

                            uvs.Clear();

                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, 3));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, 3));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, 3));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, 3));

                            Plane.Add(i);
                            Render.Add(i);
                            Portal.Add(-1);
                            Collision.Add(i);

                            Mesh transformedmesh = new Mesh();

                            transformedmesh.SetVertices(transformedvertices);
                            transformedmesh.SetUVs(0, uvs);
                            transformedmesh.SetTriangles(triangles, 0, true);

                            meshes.Add(transformedmesh);
                        }
                    }
                    if (sectors[wall].ceilingHeight != sectors[wall].floorHeight)
                    {
                        if (sector.ceilingHeight > sectors[wall].ceilingHeight)
                        {
                            Ceiling = sectors[wall].ceilingHeight / 8 * 2.5f;
                        }
                        else
                        {
                            Ceiling = sector.ceilingHeight / 8 * 2.5f;
                        }
                        if (sector.floorHeight > sectors[wall].floorHeight)
                        {
                            Floor = sector.floorHeight / 8 * 2.5f;
                        }
                        else
                        {
                            Floor = sectors[wall].floorHeight / 8 * 2.5f;
                        }
                        
                        transformedvertices.Clear();

                        transformedvertices.Add(new Vector3((float)Z1, (float)Floor, (float)X1));
                        transformedvertices.Add(new Vector3((float)Z1, (float)Ceiling, (float)X1));
                        transformedvertices.Add(new Vector3((float)Z0, (float)Ceiling, (float)X0));
                        transformedvertices.Add(new Vector3((float)Z0, (float)Floor, (float)X0));

                        LeftPlane = new Plane((transformedvertices[2] - transformedvertices[1]).normalized, transformedvertices[1]);
                        TopPlane = new Plane((transformedvertices[1] - transformedvertices[0]).normalized, transformedvertices[1]);

                        uvs.Clear();

                        uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, 0));
                        uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, 0));
                        uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, 0));
                        uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, 0));

                        Plane.Add(i);
                        Render.Add(-1);
                        Portal.Add(wall);
                        Collision.Add(-1);

                        Mesh transformedmesh = new Mesh();

                        transformedmesh.SetVertices(transformedvertices);
                        transformedmesh.SetUVs(0, uvs);
                        transformedmesh.SetTriangles(triangles, 0, true);

                        meshes.Add(transformedmesh);
                    }

                    if (sector.floorHeight < sectors[wall].floorHeight)
                    {
                        if (sector.ceilingHeight > sectors[wall].floorHeight)
                        {
                            double F0 = sector.floorHeight / 8 * 2.5f;

                            if (sector.floorHeight > sectors[wall].floorHeight)
                            {
                                Floor = sector.floorHeight / 8 * 2.5f;
                            }
                            else
                            {
                                Floor = sectors[wall].floorHeight / 8 * 2.5f;
                            }

                            transformedvertices.Clear();

                            transformedvertices.Add(new Vector3((float)Z1, (float)F0, (float)X1));
                            transformedvertices.Add(new Vector3((float)Z1, (float)Floor, (float)X1));
                            transformedvertices.Add(new Vector3((float)Z0, (float)Floor, (float)X0));
                            transformedvertices.Add(new Vector3((float)Z0, (float)F0, (float)X0));

                            LeftPlane = new Plane((transformedvertices[2] - transformedvertices[1]).normalized, transformedvertices[1]);
                            TopPlane = new Plane((transformedvertices[1] - transformedvertices[0]).normalized, transformedvertices[1]);

                            uvs.Clear();

                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, 2));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, 2));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, 2));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, 2));

                            Plane.Add(i);
                            Render.Add(i);
                            Portal.Add(-1);
                            Collision.Add(i);

                            Mesh transformedmesh = new Mesh();

                            transformedmesh.SetVertices(transformedvertices);
                            transformedmesh.SetUVs(0, uvs);
                            transformedmesh.SetTriangles(triangles, 0, true);

                            meshes.Add(transformedmesh);
                        }
                        else
                        {
                            double F0 = sector.floorHeight / 8 * 2.5f;
                            double F1 = sector.ceilingHeight / 8 * 2.5f;

                            transformedvertices.Clear();

                            transformedvertices.Add(new Vector3((float)Z1, (float)F0, (float)X1));
                            transformedvertices.Add(new Vector3((float)Z1, (float)F1, (float)X1));
                            transformedvertices.Add(new Vector3((float)Z0, (float)F1, (float)X0));
                            transformedvertices.Add(new Vector3((float)Z0, (float)F0, (float)X0));

                            LeftPlane = new Plane((transformedvertices[2] - transformedvertices[1]).normalized, transformedvertices[1]);
                            TopPlane = new Plane((transformedvertices[1] - transformedvertices[0]).normalized, transformedvertices[1]);

                            uvs.Clear();

                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[0]) / 2.5f, 2));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[1]) / 2.5f, 2));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[2]) / 2.5f, 2));
                            uvs.Add(new Vector3(LeftPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, TopPlane.GetDistanceToPoint(transformedvertices[3]) / 2.5f, 2));

                            Plane.Add(i);
                            Render.Add(i);
                            Portal.Add(-1);
                            Collision.Add(i);

                            Mesh transformedmesh = new Mesh();

                            transformedmesh.SetVertices(transformedvertices);
                            transformedmesh.SetUVs(0, uvs);
                            transformedmesh.SetTriangles(triangles, 0, true);

                            meshes.Add(transformedmesh);
                        }
                    }
                }
            }

            if (sector.floorHeight != sector.ceilingHeight)
            {
                floorverts.Clear();
                ceilingverts.Clear();
                flooruvs.Clear();
                ceilinguvs.Clear();

                for (int e = 0; e < sector.vertexIndices.Count; ++e)
                {
                    double YF = sector.floorHeight / 8 * 2.5f;
                    double YC = sector.ceilingHeight / 8 * 2.5f;
                    double X = vertices[sector.vertexIndices[e]].x / 2 * 2.5f;
                    double Z = vertices[sector.vertexIndices[e]].y / 2 * 2.5f;

                    float OX = (float)X / 2.5f * -1;
                    float OY = (float)Z / 2.5f;

                    floorverts.Add(new Vector3((float)Z, (float)YF, (float)X));
                    ceilingverts.Add(new Vector3((float)Z, (float)YC, (float)X));
                    flooruvs.Add(new Vector3(OY, OX, 0));
                    ceilinguvs.Add(new Vector3(OY, OX, 1));
                }

                floortri.Clear();

                for (int f = 0; f < floorverts.Count - 2; f++)
                {
                    floortri.Add(0);
                    floortri.Add(f + 1);
                    floortri.Add(f + 2);
                }

                ceilingverts.Reverse();
                ceilinguvs.Reverse();

                ceilingtri.Clear();

                for (int c = 0; c < ceilingverts.Count - 2; c++)
                {
                    ceilingtri.Add(0);
                    ceilingtri.Add(c + 1);
                    ceilingtri.Add(c + 2);
                }

                Plane.Add(i);
                Render.Add(i);
                Portal.Add(-1);
                Collision.Add(i);

                Mesh transformedfloormesh = new Mesh();

                transformedfloormesh.SetVertices(floorverts);
                transformedfloormesh.SetUVs(0, flooruvs);
                transformedfloormesh.SetTriangles(floortri, 0, true);

                meshes.Add(transformedfloormesh);

                Plane.Add(i);
                Render.Add(i);
                Portal.Add(-1);
                Collision.Add(i);

                Mesh transformedceilingmesh = new Mesh();

                transformedceilingmesh.SetVertices(ceilingverts);
                transformedceilingmesh.SetUVs(0, ceilinguvs);
                transformedceilingmesh.SetTriangles(ceilingtri, 0, true);

                meshes.Add(transformedceilingmesh);
            }
        }
    }

    public void BuildColliders()
    {
        for (int i = 0; i < LevelLists.sectors.Length; i++)
        {
            ColliderVertices.Clear();

            ColliderTriangles.Clear();

            int triangleCount = 0;

            for (int e = LevelLists.sectors[i].polygonStartIndex; e < LevelLists.sectors[i].polygonStartIndex + LevelLists.sectors[i].polygonCount; e++)
            {
                if (LevelLists.polygons[e].collisionCount != -1)
                {
                    for (int f = LevelLists.polygons[e].collisionStartIndex; f < LevelLists.polygons[e].collisionStartIndex + LevelLists.polygons[e].collisionCount; f++)
                    {
                        ColliderVertices.Add(LevelLists.collisions[f].v0);
                        ColliderVertices.Add(LevelLists.collisions[f].v1);
                        ColliderVertices.Add(LevelLists.collisions[f].v2);
                        ColliderTriangles.Add(triangleCount);
                        ColliderTriangles.Add(triangleCount + 1);
                        ColliderTriangles.Add(triangleCount + 2);
                        triangleCount += 3;
                    }
                } 
            }

            Mesh combinedmesh = new Mesh();

            CollisionMesh.Add(combinedmesh);

            combinedmesh.SetVertices(ColliderVertices);

            combinedmesh.SetTriangles(ColliderTriangles, 0);

            GameObject meshObject = new GameObject("Collision " + i);

            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = combinedmesh;

            ColliderSectors.Add(meshCollider);

            meshObject.transform.SetParent(CollisionObjects.transform);
        }
    }

    public void BuildObjects()
    {
        for (int i = 0; i < starts.Count; i++)
        {
            StartPos Start = new StartPos
            {
                start = new float3(starts[i].location.x / 2 * 2.5f, sectors[starts[i].sector].floorHeight / 8 * 2.5f, starts[i].location.y / 2 * 2.5f),

                sectorId = starts[i].sector
            };

            LevelLists.positions.Add(Start);
        }
    }

    public void BuildLists()
    {
        int opaqueStart = 0;
        int collisionStart = 0;
        int edgeStart = 0;
        int planeStart = 0;
        int polygonStart = 0;

        for (int a = 0; a < sectors.Count; a++)
        {
            int polygonCount = 0;

            for (int b = 0; b < Plane.Count; b++)
            {
                if (Plane[b] != a)
                {
                    continue;
                }
                    
                PolygonMeta meta = new PolygonMeta();
                Mesh mesh = meshes[b];

                if (Portal[b] != -1)
                {
                    int edgeCount = 0;

                    for (int c = 0; c < mesh.vertexCount; c++)
                    {
                        int d = (c + 1) % mesh.vertexCount;

                        Edge line = new Edge
                        {
                            v0 = mesh.vertices[c],
                            v1 = mesh.vertices[d]
                        };

                        LevelLists.edges.Add(line);

                        edgeCount += 1;
                    }

                    meta.lineStartIndex = edgeStart;
                    meta.lineCount = edgeCount;
                    edgeStart += edgeCount;
                }
                else
                {
                    meta.lineStartIndex = -1;
                    meta.lineCount = -1;
                }

                float3 v0 = mesh.vertices[0];
                float3 v1 = mesh.vertices[1];
                float3 v2 = mesh.vertices[2];
                float3 n = math.normalize(math.cross(v1 - v0, v2 - v0));

                MathematicalPlane plane = new MathematicalPlane
                {
                    normal = n,
                    distance = -math.dot(n, v0)
                };

                LevelLists.planes.Add(plane);
                meta.plane = planeStart;
                planeStart += 1;

                if (Render[b] == a)
                {
                    int count = 0;
                    uvVector3.Clear();
                    mesh.GetUVs(0, uvVector3);

                    for (int c = 0; c < mesh.triangles.Length; c += 3)
                    {
                        Triangle t = new Triangle
                        {
                            v0 = mesh.vertices[mesh.triangles[c]],
                            v1 = mesh.vertices[mesh.triangles[c + 1]],
                            v2 = mesh.vertices[mesh.triangles[c + 2]],
                            uv0 = uvVector3[mesh.triangles[c]],
                            uv1 = uvVector3[mesh.triangles[c + 1]],
                            uv2 = uvVector3[mesh.triangles[c + 2]]
                        };

                        LevelLists.opaques.Add(t);
                        count += 1;
                    }

                    meta.opaqueStartIndex = opaqueStart;
                    meta.opaqueCount = count;
                    opaqueStart += count;
                }
                else
                {
                    meta.opaqueStartIndex = -1;
                    meta.opaqueCount = -1;
                }

                if (Collision[b] == a)
                {
                    int count = 0;

                    for (int c = 0; c < mesh.triangles.Length; c += 3)
                    {
                        Triangle t = new Triangle
                        {
                            v0 = mesh.vertices[mesh.triangles[c]],
                            v1 = mesh.vertices[mesh.triangles[c + 1]],
                            v2 = mesh.vertices[mesh.triangles[c + 2]],
                            uv0 = float3.zero,
                            uv1 = float3.zero,
                            uv2 = float3.zero
                        };

                        LevelLists.collisions.Add(t);
                        count += 1;
                    }

                    meta.collisionStartIndex = collisionStart;
                    meta.collisionCount = count;
                    collisionStart += count;
                }
                else
                {
                    meta.collisionStartIndex = -1;
                    meta.collisionCount = -1;
                }

                meta.sectorId = a;
                meta.connectedSectorId = Portal[b];

                LevelLists.polygons.Add(meta);
                polygonCount += 1;
            }

            SectorMeta sectorMeta = new SectorMeta
            {
                sectorId = a,
                polygonStartIndex = polygonStart,
                polygonCount = polygonCount,
                planeStartIndex = 0,
                planeCount = 4
            };

            LevelLists.sectors.Add(sectorMeta);
            polygonStart += polygonCount;
        }

        Debug.Log("Level built successfully!");
    }
}
