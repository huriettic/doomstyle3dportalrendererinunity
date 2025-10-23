using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Edge
{
    public Vector3 start;
    public Vector3 end;
};

[Serializable]
public struct MathematicalPlane
{
    public Vector3 normal;
    public float distance;
};

[Serializable]
public struct Triangle
{
    public Vector3 v0, v1, v2;
    public Vector3 uv0, uv1, uv2;
    public Vector3 n0, n1, n2;
};

[Serializable]
public struct StartPos
{
    public Vector3 Position;
    public int SectorID;
};

[Serializable]
public struct FrustumMeta
{
    public int planeStartIndex;
    public int planeCount;

    public int frustumID;
};

[Serializable]
public struct PortalMeta
{
    public int lineStartIndex;
    public int lineCount;

    public int portalPlane;
    public int portalID;

    public int sectorID;
    public int connectedSectorID;
};

[Serializable]
public struct SectorMeta
{
    public int planeStartIndex;
    public int planeCount;

    public int opaqueStartIndex;
    public int opaqueCount;

    public int collisionStartIndex;
    public int collisionCount;

    public int portalStartIndex;
    public int portalCount;

    public int sectorID;
};

public class LevelLoader : MonoBehaviour
{
    public string Name = "map-clear";

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
    private GameObject OpaqueObjects;
    private Material opaquematerial;
    private Mesh opaquemesh;
    private Material linematerial;
    private List<MeshCollider> CollisionSectors = new List<MeshCollider>();
    private List<Vector3> OpaqueVertices = new List<Vector3>();
    private List<Vector3> OpaqueNormals = new List<Vector3>();
    private List<int> OpaqueTriangles = new List<int>();
    private List<Mesh> CollisionMesh = new List<Mesh>();
    private List<Mesh> OpaqueMesh = new List<Mesh>();
    private GameObject CollisionObjects;
    private List<GameObject> OpaqueSectors = new List<GameObject>();
    private List<Mesh> EdgeMesh = new List<Mesh>();
    private GameObject EdgeObjects;
    private List<GameObject> Edges = new List<GameObject>();
    private bool[] processbool;
    private Vector3[] processvertices;
    private Vector3[] processtextures;
    private Vector3[] processnormals;
    private Vector3[] temporaryvertices;
    private Vector3[] temporarytextures;
    private Vector3[] temporarynormals;
    private List<MathematicalPlane> MathematicalCamPlanes = new List<MathematicalPlane>();
    private Camera Cam;
    private Vector3 CamPoint;
    private SectorMeta CurrentSector;
    private List<SectorMeta> Sectors = new List<SectorMeta>();
    private List<SectorMeta> OldSectors = new List<SectorMeta>();
    private List<Vector3> OutTriangleVertices = new List<Vector3>();
    private List<Vector3> OutTriangleTextures = new List<Vector3>();
    private List<Vector3> OutTriangleNormals = new List<Vector3>();
    private List<Vector3> OutEdgeVertices = new List<Vector3>();
    private bool radius;
    private bool check;
    private int combinedTriangles;
    private float planeDistance;
    private double Ceiling;
    private double Floor;
    private int MaxDepth;
    private Plane LeftPlane;
    private Plane TopPlane;
    private List<Vector3> uvs = new List<Vector3>();
    private List<Vector3> flooruvs = new List<Vector3>();
    private List<Vector3> ceilinguvs = new List<Vector3>();
    private List<Vector3> OpaqueTextures = new List<Vector3>();
    private Queue<(FrustumMeta, SectorMeta)> PortalQueue = new Queue<(FrustumMeta, SectorMeta)>();
    private GameObject RenderMesh;

    [System.Serializable]
    public class Sector
    {
        public float floorHeight;
        public float ceilingHeight;
        public List<int> vertexIndices = new List<int>();
        public List<int> wallTypes = new List<int>(); // -1 for solid, sector index for portal
    }

    public class PlayerStart
    {
        public Vector3 location;
        public float angle;
        public int sector;
    }

    [Serializable]
    public class TopLevelLists
    {
        public List<SectorMeta> sectors = new List<SectorMeta>();
        public List<PortalMeta> portals = new List<PortalMeta>();
        public List<StartPos> positions = new List<StartPos>();
        public List<Triangle> opaques = new List<Triangle>();
        public List<Triangle> collisions = new List<Triangle>();
        public List<FrustumMeta> frustums = new List<FrustumMeta>();
        public List<MathematicalPlane> planes = new List<MathematicalPlane>();
        public List<Edge> edges = new List<Edge>();
    }

    void Start()
    {
        LoadFromFile();

        triangles = new List<int>()
        {
            0, 1, 2, 0, 2, 3
        };

        LevelLists = new TopLevelLists();

        CreateGameObjects();

        buildGeometry();

        buildLists();

        buildObjects();

        buildCollsionSectors();

        //OpaqueObjects = new GameObject("Opaque Meshes");

        //EdgeObjects = new GameObject("Portal Meshes");

        //linematerial = new Material(Shader.Find("Standard"));

        //linematerial.color = Color.cyan;

        //buildEdges();

        //buildOpaques();

        processbool = new bool[256];

        processvertices = new Vector3[256];

        processtextures = new Vector3[256];

        processnormals = new Vector3[256];

        temporaryvertices = new Vector3[256];

        temporarytextures = new Vector3[256];

        temporarynormals = new Vector3[256];

        Player.GetComponent<CharacterController>().enabled = true;

        Cursor.lockState = CursorLockMode.Locked;

        Playerstart();

        FrustumMeta temp = LevelLists.frustums[LevelLists.frustums.Count - 1];

        temp.planeStartIndex = 0;

        temp.planeCount = 4;

        LevelLists.frustums[LevelLists.frustums.Count - 1] = temp;

        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[LevelLists.sectors[i].sectorID], true);
        }
    }

    void Update()
    {
        PlayerInput();

        if (Cam.transform.hasChanged)
        {
            CamPoint = Cam.transform.position;

            Sectors.Clear();

            GetSectors(CurrentSector);

            MathematicalCamPlanes.Clear();

            ReadFrustumPlanes(Cam, MathematicalCamPlanes);

            MathematicalCamPlanes.RemoveAt(5);

            MathematicalCamPlanes.RemoveAt(4);

            OpaqueVertices.Clear();

            OpaqueTextures.Clear();

            OpaqueTriangles.Clear();

            OpaqueNormals.Clear();

            combinedTriangles = 0;

            MaxDepth = 0;

            GetPortals(LevelLists.frustums[LevelLists.frustums.Count - 1], CurrentSector);

            SetRenderMesh();

            Cam.transform.hasChanged = false;
        }
    }

    void Awake()
    {
        Player = GameObject.Find("Player").GetComponent<CharacterController>();

        Cam = Camera.main;
    }

    void FixedUpdate()
    {
        if (!Player.isGrounded)
        {
            currentForce.y -= gravity * Time.deltaTime;
        }
    }

    public void CreateGameObjects()
    {
        CollisionObjects = new GameObject("Collision Meshes");

        Shader shader = Resources.Load<Shader>("TEXARRAYSHADER");

        opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>("Textures");

        opaquemesh = new Mesh();

        opaquemesh.MarkDynamic();

        RenderMesh = new GameObject("Render Mesh");

        RenderMesh.AddComponent<MeshFilter>();
        RenderMesh.AddComponent<MeshRenderer>();

        Renderer MeshRend = RenderMesh.GetComponent<Renderer>();
        MeshRend.sharedMaterial = opaquematerial;
        MeshRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        RenderMesh.GetComponent<MeshFilter>().mesh = opaquemesh;
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

    public void SetFrustumPlanes(List<MathematicalPlane> planes, Matrix4x4 m)
    {
        if (planes == null)
            return;
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

    public void ReadFrustumPlanes(Camera cam, List<MathematicalPlane> planes)
    {
        SetFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
    }

    public void SetClippingPlanes(List<Vector3> vertices, int portalnumber, Vector3 viewPos)
    {
        int StartIndex = MathematicalCamPlanes.Count;

        int IndexCount = 0;

        int count = vertices.Count;
        for (int i = 0; i < count; i += 2)
        {
            Vector3 p1 = vertices[i];
            Vector3 p2 = vertices[i + 1];
            Vector3 normal = Vector3.Cross(p1 - p2, viewPos - p2);
            float magnitude = normal.magnitude;
            Vector3 normalized = normal / magnitude;

            if (magnitude > 0.01f)
            {
                MathematicalCamPlanes.Add(new MathematicalPlane { normal = normalized, distance = -Vector3.Dot(normalized, p1) });
                IndexCount += 1;
            }
        }

        FrustumMeta temp = LevelLists.frustums[portalnumber];

        temp.planeStartIndex = StartIndex;
        temp.planeCount = IndexCount;

        LevelLists.frustums[portalnumber] = temp;
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
        return Vector3.Dot(plane.normal, point) + plane.distance;
    }

    public void ClipTrianglesWithPlanes(FrustumMeta planes, List<Triangle> verttexnorm, int startIndex, int count)
    {
        OutTriangleVertices.Clear();
        OutTriangleTextures.Clear();
        OutTriangleNormals.Clear();

        for (int a = startIndex; a < count; a++)
        {
            Triangle tri = verttexnorm[a];
            Vector3 Edge1 = tri.v1 - tri.v0;
            Vector3 Edge2 = tri.v2 - tri.v0;
            Vector3 Normal = Vector3.Cross(Edge1, Edge2).normalized;
            Vector3 CamDirection = (CamPoint - tri.v0).normalized;
            float triangleDirection = Vector3.Dot(Normal, CamDirection);

            if (triangleDirection < 0)
            {
                continue;
            }

            int processverticescount = 0;
            int processtexturescount = 0;
            int processnormalscount = 0;
            int processboolcount = 0;

            processvertices[processverticescount] = tri.v0;
            processvertices[processverticescount + 1] = tri.v1;
            processvertices[processverticescount + 2] = tri.v2;
            processverticescount += 3;
            processtextures[processtexturescount] = tri.uv0;
            processtextures[processtexturescount + 1] = tri.uv1;
            processtextures[processtexturescount + 2] = tri.uv2;
            processtexturescount += 3;
            processnormals[processnormalscount] = tri.n0;
            processnormals[processnormalscount + 1] = tri.n1;
            processnormals[processnormalscount + 2] = tri.n2;
            processnormalscount += 3;
            processbool[processboolcount] = true;
            processbool[processboolcount + 1] = true;
            processbool[processboolcount + 2] = true;
            processboolcount += 3;

            for (int b = planes.planeStartIndex; b < planes.planeStartIndex + planes.planeCount; b++)
            {
                int AddTriangles = 0;

                int temporaryverticescount = 0;
                int temporarytexturescount = 0;
                int temporarynormalscount = 0;

                for (int c = 0; c < processverticescount; c += 3)
                {
                    if (processbool[c] == false && processbool[c + 1] == false && processbool[c + 2] == false)
                    {
                        continue;
                    }

                    Vector3 v0 = processvertices[c];
                    Vector3 v1 = processvertices[c + 1];
                    Vector3 v2 = processvertices[c + 2];

                    Vector4 uv0 = processtextures[c];
                    Vector4 uv1 = processtextures[c + 1];
                    Vector4 uv2 = processtextures[c + 2];

                    Vector3 n0 = processnormals[c];
                    Vector3 n1 = processnormals[c + 1];
                    Vector3 n2 = processnormals[c + 2];

                    float d0 = GetPlaneSignedDistanceToPoint(MathematicalCamPlanes[b], processvertices[c]);
                    float d1 = GetPlaneSignedDistanceToPoint(MathematicalCamPlanes[b], processvertices[c + 1]);
                    float d2 = GetPlaneSignedDistanceToPoint(MathematicalCamPlanes[b], processvertices[c + 2]);

                    bool b0 = d0 >= 0;
                    bool b1 = d1 >= 0;
                    bool b2 = d2 >= 0;

                    if (b0 && b1 && b2)
                    {
                        continue;
                    }
                    else if ((b0 && !b1 && !b2) || (!b0 && b1 && !b2) || (!b0 && !b1 && b2))
                    {
                        Vector3 inV, outV1, outV2;
                        Vector4 inUV, outUV1, outUV2;
                        Vector3 inN, outN1, outN2;
                        float inD, outD1, outD2;

                        if (b0)
                        {
                            inV = v0;
                            inUV = uv0;
                            inN = n0;
                            inD = d0;
                            outV1 = v1;
                            outUV1 = uv1;
                            outN1 = n1;
                            outD1 = d1;
                            outV2 = v2;
                            outUV2 = uv2;
                            outN2 = n2;
                            outD2 = d2;
                        }
                        else if (b1)
                        {
                            inV = v1;
                            inUV = uv1;
                            inN = n1;
                            inD = d1;
                            outV1 = v2;
                            outUV1 = uv2;
                            outN1 = n2;
                            outD1 = d2;
                            outV2 = v0;
                            outUV2 = uv0;
                            outN2 = n0;
                            outD2 = d0;
                        }
                        else
                        {
                            inV = v2;
                            inUV = uv2;
                            inN = n2;
                            inD = d2;
                            outV1 = v0;
                            outUV1 = uv0;
                            outN1 = n0;
                            outD1 = d0;
                            outV2 = v1;
                            outUV2 = uv1;
                            outN2 = n1;
                            outD2 = d1;
                        }

                        float t1 = inD / (inD - outD1);
                        float t2 = inD / (inD - outD2);

                        temporaryvertices[temporaryverticescount] = inV;
                        temporaryvertices[temporaryverticescount + 1] = Vector3.Lerp(inV, outV1, t1);
                        temporaryvertices[temporaryverticescount + 2] = Vector3.Lerp(inV, outV2, t2);
                        temporaryverticescount += 3;
                        temporarytextures[temporarytexturescount] = inUV;
                        temporarytextures[temporarytexturescount + 1] = Vector4.Lerp(inUV, outUV1, t1);
                        temporarytextures[temporarytexturescount + 2] = Vector4.Lerp(inUV, outUV2, t2);
                        temporarytexturescount += 3;
                        temporarynormals[temporarynormalscount] = inN;
                        temporarynormals[temporarynormalscount + 1] = Vector3.Lerp(inN, outN1, t1).normalized;
                        temporarynormals[temporarynormalscount + 2] = Vector3.Lerp(inN, outN2, t2).normalized;
                        temporarynormalscount += 3;
                        processbool[c] = false;
                        processbool[c + 1] = false;
                        processbool[c + 2] = false;

                        AddTriangles += 1;
                    }
                    else if ((!b0 && b1 && b2) || (b0 && !b1 && b2) || (b0 && b1 && !b2))
                    {
                        Vector3 inV1, inV2, outV;
                        Vector4 inUV1, inUV2, outUV;
                        Vector3 inN1, inN2, outN;
                        float inD1, inD2, outD;

                        if (!b0)
                        {
                            outV = v0;
                            outUV = uv0;
                            outN = n0;
                            outD = d0;
                            inV1 = v1;
                            inUV1 = uv1;
                            inN1 = n1;
                            inD1 = d1;
                            inV2 = v2;
                            inUV2 = uv2;
                            inN2 = n2;
                            inD2 = d2;
                        }
                        else if (!b1)
                        {
                            outV = v1;
                            outUV = uv1;
                            outN = n1;
                            outD = d1;
                            inV1 = v2;
                            inUV1 = uv2;
                            inN1 = n2;
                            inD1 = d2;
                            inV2 = v0;
                            inUV2 = uv0;
                            inN2 = n0;
                            inD2 = d0;
                        }
                        else
                        {
                            outV = v2;
                            outUV = uv2;
                            outN = n2;
                            outD = d2;
                            inV1 = v0;
                            inUV1 = uv0;
                            inN1 = n0;
                            inD1 = d0;
                            inV2 = v1;
                            inUV2 = uv1;
                            inN2 = n1;
                            inD2 = d1;
                        }

                        float t1 = inD1 / (inD1 - outD);
                        float t2 = inD2 / (inD2 - outD);

                        Vector3 vA = Vector3.Lerp(inV1, outV, t1);
                        Vector3 vB = Vector3.Lerp(inV2, outV, t2);

                        Vector4 uvA = Vector4.Lerp(inUV1, outUV, t1);
                        Vector4 uvB = Vector4.Lerp(inUV2, outUV, t2);

                        Vector3 nA = Vector3.Lerp(inN1, outN, t1).normalized;
                        Vector3 nB = Vector3.Lerp(inN2, outN, t2).normalized;

                        temporaryvertices[temporaryverticescount] = inV1;
                        temporaryvertices[temporaryverticescount + 1] = inV2;
                        temporaryvertices[temporaryverticescount + 2] = vA;
                        temporaryverticescount += 3;
                        temporarytextures[temporarytexturescount] = inUV1;
                        temporarytextures[temporarytexturescount + 1] = inUV2;
                        temporarytextures[temporarytexturescount + 2] = uvA;
                        temporarytexturescount += 3;
                        temporarynormals[temporarynormalscount] = inN1;
                        temporarynormals[temporarynormalscount + 1] = inN2;
                        temporarynormals[temporarynormalscount + 2] = nA;
                        temporarynormalscount += 3;
                        temporaryvertices[temporaryverticescount] = vA;
                        temporaryvertices[temporaryverticescount + 1] = inV2;
                        temporaryvertices[temporaryverticescount + 2] = vB;
                        temporaryverticescount += 3;
                        temporarytextures[temporarytexturescount] = uvA;
                        temporarytextures[temporarytexturescount + 1] = inUV2;
                        temporarytextures[temporarytexturescount + 2] = uvB;
                        temporarytexturescount += 3;
                        temporarynormals[temporarynormalscount] = nA;
                        temporarynormals[temporarynormalscount + 1] = inN2;
                        temporarynormals[temporarynormalscount + 2] = nB;
                        temporarynormalscount += 3;
                        processbool[c] = false;
                        processbool[c + 1] = false;
                        processbool[c + 2] = false;

                        AddTriangles += 2;
                    }
                    else
                    {
                        processbool[c] = false;
                        processbool[c + 1] = false;
                        processbool[c + 2] = false;
                    }
                }

                if (AddTriangles > 0)
                {
                    for (int d = 0; d < temporaryverticescount; d += 3)
                    {
                        processvertices[processverticescount] = temporaryvertices[d];
                        processvertices[processverticescount + 1] = temporaryvertices[d + 1];
                        processvertices[processverticescount + 2] = temporaryvertices[d + 2];
                        processverticescount += 3;
                        processtextures[processtexturescount] = temporarytextures[d];
                        processtextures[processtexturescount + 1] = temporarytextures[d + 1];
                        processtextures[processtexturescount + 2] = temporarytextures[d + 2];
                        processtexturescount += 3;
                        processnormals[processnormalscount] = temporarynormals[d];
                        processnormals[processnormalscount + 1] = temporarynormals[d + 1];
                        processnormals[processnormalscount + 2] = temporarynormals[d + 2];
                        processnormalscount += 3;
                        processbool[processboolcount] = true;
                        processbool[processboolcount + 1] = true;
                        processbool[processboolcount + 2] = true;
                        processboolcount += 3;
                    }
                }
            }

            for (int e = 0; e < processboolcount; e += 3)
            {
                if (processbool[e] == true && processbool[e + 1] == true && processbool[e + 2] == true)
                {
                    OutTriangleVertices.Add(processvertices[e]);
                    OutTriangleVertices.Add(processvertices[e + 1]);
                    OutTriangleVertices.Add(processvertices[e + 2]);
                    OutTriangleTextures.Add(processtextures[e]);
                    OutTriangleTextures.Add(processtextures[e + 1]);
                    OutTriangleTextures.Add(processtextures[e + 2]);
                    OutTriangleNormals.Add(processnormals[e]);
                    OutTriangleNormals.Add(processnormals[e + 1]);
                    OutTriangleNormals.Add(processnormals[e + 2]);
                }
            }
        }
    }

    public void ClipEdgesWithPlanes(FrustumMeta planes, PortalMeta portal)
    {
        OutEdgeVertices.Clear();

        int processverticescount = 0;
        int processboolcount = 0;

        for (int a = portal.lineStartIndex; a < portal.lineStartIndex + portal.lineCount; a++)
        {
            Edge line = LevelLists.edges[a];
            processvertices[processverticescount] = line.start;
            processvertices[processverticescount + 1] = line.end;
            processverticescount += 2;
            processbool[processboolcount] = true;
            processbool[processboolcount + 1] = true;
            processboolcount += 2;
        }

        for (int b = planes.planeStartIndex; b < planes.planeStartIndex + planes.planeCount; b++)
        {
            int intersection = 0;

            int temporaryverticescount = 0;

            Vector3 intersectionPoint1 = Vector3.zero;
            Vector3 intersectionPoint2 = Vector3.zero;

            for (int c = 0; c < processverticescount; c += 2)
            {
                if (processbool[c] == false && processbool[c + 1] == false)
                {
                    continue;
                }

                Vector3 p1 = processvertices[c];
                Vector3 p2 = processvertices[c + 1];

                float d1 = GetPlaneSignedDistanceToPoint(MathematicalCamPlanes[b], processvertices[c]);
                float d2 = GetPlaneSignedDistanceToPoint(MathematicalCamPlanes[b], processvertices[c + 1]);

                bool b1 = d1 >= 0;
                bool b2 = d2 >= 0;

                if (b1 && b2)
                {
                    continue;
                }
                else if ((b1 && !b2) || (!b1 && b2))
                {
                    Vector3 point1;
                    Vector3 point2;

                    float t = d1 / (d1 - d2);

                    Vector3 intersectionPoint = Vector3.Lerp(p1, p2, t);

                    if (b1)
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

                    temporaryvertices[temporaryverticescount] = point1;
                    temporaryvertices[temporaryverticescount + 1] = point2;
                    temporaryverticescount += 2;

                    processbool[c] = false;
                    processbool[c + 1] = false;

                    intersection += 1;
                }
                else
                {
                    processbool[c] = false;
                    processbool[c + 1] = false;
                }
            }

            if (intersection == 2)
            {
                for (int d = 0; d < temporaryverticescount; d += 2)
                {
                    processvertices[processverticescount] = temporaryvertices[d];
                    processvertices[processverticescount + 1] = temporaryvertices[d + 1];
                    processverticescount += 2;
                    processbool[processboolcount] = true;
                    processbool[processboolcount + 1] = true;
                    processboolcount += 2;
                }

                processvertices[processverticescount] = intersectionPoint1;
                processvertices[processverticescount + 1] = intersectionPoint2;
                processverticescount += 2;
                processbool[processboolcount] = true;
                processbool[processboolcount + 1] = true;
                processboolcount += 2;
            }
        }

        for (int e = 0; e < processboolcount; e += 2)
        {
            if (processbool[e] == true && processbool[e + 1] == true)
            {
                OutEdgeVertices.Add(processvertices[e]);
                OutEdgeVertices.Add(processvertices[e + 1]);
            }
        }
    }

    public void SetRenderMesh()
    {
        opaquemesh.Clear();

        opaquemesh.SetVertices(OpaqueVertices);

        opaquemesh.SetUVs(0, OpaqueTextures);

        opaquemesh.SetTriangles(OpaqueTriangles, 0);

        opaquemesh.SetNormals(OpaqueNormals);
    }

    public bool CheckRadius(SectorMeta asector, Vector3 campoint)
    {
        for (int i = asector.planeStartIndex; i < asector.planeStartIndex + asector.planeCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(LevelLists.planes[i], campoint) < -0.6f)
            {
                return false;
            }
        }
        return true;
    }

    public bool CheckSector(SectorMeta asector, Vector3 campoint)
    {
        for (int i = asector.planeStartIndex; i < asector.planeStartIndex + asector.planeCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(LevelLists.planes[i], campoint) < 0)
            {
                return false;
            }
        }
        return true;
    }

    public bool SectorsContains(int sectorID)
    {
        for (int i = 0; i < Sectors.Count; i++)
        {
            if (Sectors[i].sectorID == sectorID)
            {
                return true;
            }
        }
        return false;
    }

    public bool SectorsDoNotEqual()
    {
        if (Sectors.Count != OldSectors.Count)
        {
            return true;
        }

        for (int i = 0; i < Sectors.Count; i++)
        {
            if (Sectors[i].sectorID != OldSectors[i].sectorID)
            {
                return true;
            }
        }
        return false;
    }

    public void GetSectors(SectorMeta ASector)
    {
        Sectors.Add(ASector);

        for (int i = ASector.portalStartIndex; i < ASector.portalStartIndex + ASector.portalCount; i++)
        {
            int portalnumber = LevelLists.portals[i].connectedSectorID;

            SectorMeta portalsector = LevelLists.sectors[portalnumber];

            if (SectorsContains(portalsector.sectorID))
            {
                continue;
            }

            radius = CheckRadius(portalsector, CamPoint);

            if (radius)
            {
                GetSectors(portalsector);
            }
        }

        check = CheckSector(ASector, CamPoint);

        if (check)
        {
            CurrentSector = ASector;

            if (SectorsDoNotEqual())
            {
                for (int i = 0; i < OldSectors.Count; i++)
                {
                    Physics.IgnoreCollision(Player, CollisionSectors[OldSectors[i].sectorID], true);
                }

                for (int i = 0; i < Sectors.Count; i++)
                {
                    Physics.IgnoreCollision(Player, CollisionSectors[Sectors[i].sectorID], false);
                }

                OldSectors.Clear();

                for (int i = 0; i < Sectors.Count; i++)
                {
                    OldSectors.Add(Sectors[i]);
                }
            }
        }
    }

    public void GetTriangles(FrustumMeta APlanes, SectorMeta BSector)
    {
        ClipTrianglesWithPlanes(APlanes, LevelLists.opaques, BSector.opaqueStartIndex, BSector.opaqueStartIndex + BSector.opaqueCount);

        for (int e = 0; e < OutTriangleVertices.Count; e++)
        {
            OpaqueVertices.Add(OutTriangleVertices[e]);
            OpaqueTextures.Add(OutTriangleTextures[e]);
            OpaqueNormals.Add(OutTriangleNormals[e]);
            OpaqueTriangles.Add(e + combinedTriangles);
        }

        combinedTriangles += OutTriangleVertices.Count;
    }

    public void GetPortals(FrustumMeta APlanes, SectorMeta BSector)
    {
        PortalQueue.Enqueue((APlanes, BSector));

        while (PortalQueue.Count > 0)
        {
            (FrustumMeta frustum, SectorMeta sector) = PortalQueue.Dequeue();

            GetTriangles(frustum, sector);

            for (int i = sector.portalStartIndex; i < sector.portalStartIndex + sector.portalCount; i++)
            {
                if (MaxDepth > 4096)
                {
                    continue;
                }

                planeDistance = GetPlaneSignedDistanceToPoint(LevelLists.planes[LevelLists.portals[i].portalPlane], CamPoint);

                if (planeDistance <= 0)
                {
                    continue;
                }

                int sectornumber = LevelLists.portals[i].connectedSectorID;

                int portalnumber = LevelLists.portals[i].portalID;

                if (SectorsContains(LevelLists.sectors[sectornumber].sectorID))
                {
                    MaxDepth += 1;

                    PortalQueue.Enqueue((frustum, LevelLists.sectors[sectornumber]));

                    continue;
                }

                ClipEdgesWithPlanes(frustum, LevelLists.portals[i]);

                if (OutEdgeVertices.Count < 6 || OutEdgeVertices.Count % 2 == 1)
                {
                    continue;
                }

                SetClippingPlanes(OutEdgeVertices, portalnumber, CamPoint);

                MaxDepth += 1;

                PortalQueue.Enqueue((LevelLists.frustums[portalnumber], LevelLists.sectors[sectornumber]));
            }
        }
    }

    public void Playerstart()
    {
        if (LevelLists.positions.Count == 0)
        {
            Debug.LogError("No player starts available.");

            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, LevelLists.positions.Count);

        StartPos selectedPosition = LevelLists.positions[randomIndex];

        CurrentSector = LevelLists.sectors[selectedPosition.SectorID];

        Player.transform.position = new Vector3(selectedPosition.Position.z, selectedPosition.Position.y + 1.10f, selectedPosition.Position.x);
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

    public void buildGeometry()
    {
        for (int i = 0; i < sectors.Count; i++)
        {
            for (int e = 0; e < sectors[i].vertexIndices.Count; e++)
            {
                int current = sectors[i].vertexIndices[e];
                int next = sectors[i].vertexIndices[(e + 1) % sectors[i].vertexIndices.Count];

                int wall = sectors[i].wallTypes[(e + 1) % sectors[i].wallTypes.Count];

                double X1 = vertices[current].x / 2 * 2.5f;
                double Z1 = vertices[current].y / 2 * 2.5f;

                double X0 = vertices[next].x / 2 * 2.5f;
                double Z0 = vertices[next].y / 2 * 2.5f;

                if (wall == -1)
                {
                    double V0 = sectors[i].floorHeight / 8 * 2.5f;
                    double V1 = sectors[i].ceilingHeight / 8 * 2.5f;

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
                    transformedmesh.RecalculateNormals();

                    meshes.Add(transformedmesh);
                }
                else
                {
                    if (sectors[i].ceilingHeight > sectors[wall].ceilingHeight)
                    {
                        if (sectors[i].floorHeight < sectors[wall].ceilingHeight)
                        {
                            double C0 = sectors[i].ceilingHeight / 8 * 2.5f;

                            if (sectors[i].ceilingHeight > sectors[wall].ceilingHeight)
                            {
                                Ceiling = sectors[wall].ceilingHeight / 8 * 2.5f;
                            }
                            else
                            {
                                Ceiling = sectors[i].ceilingHeight / 8 * 2.5f;
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
                            transformedmesh.RecalculateNormals();

                            meshes.Add(transformedmesh);
                        }
                        else
                        {
                            double C0 = sectors[i].ceilingHeight / 8 * 2.5f;
                            double C1 = sectors[i].floorHeight / 8 * 2.5f;

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
                            transformedmesh.RecalculateNormals();

                            meshes.Add(transformedmesh);
                        }
                    }
                    if (sectors[wall].ceilingHeight != sectors[wall].floorHeight)
                    {
                        if (sectors[i].ceilingHeight > sectors[wall].ceilingHeight)
                        {
                            Ceiling = sectors[wall].ceilingHeight / 8 * 2.5f;
                        }
                        else
                        {
                            Ceiling = sectors[i].ceilingHeight / 8 * 2.5f;
                        }
                        if (sectors[i].floorHeight > sectors[wall].floorHeight)
                        {
                            Floor = sectors[i].floorHeight / 8 * 2.5f;
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
                        transformedmesh.RecalculateNormals();

                        meshes.Add(transformedmesh);
                    }

                    if (sectors[i].floorHeight < sectors[wall].floorHeight)
                    {
                        if (sectors[i].ceilingHeight > sectors[wall].floorHeight)
                        {
                            double F0 = sectors[i].floorHeight / 8 * 2.5f;

                            if (sectors[i].floorHeight > sectors[wall].floorHeight)
                            {
                                Floor = sectors[i].floorHeight / 8 * 2.5f;
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
                            transformedmesh.RecalculateNormals();

                            meshes.Add(transformedmesh);
                        }
                        else
                        {
                            double F0 = sectors[i].floorHeight / 8 * 2.5f;
                            double F1 = sectors[i].ceilingHeight / 8 * 2.5f;

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
                            transformedmesh.RecalculateNormals();

                            meshes.Add(transformedmesh);
                        }
                    }
                }
            }

            if (sectors[i].floorHeight != sectors[i].ceilingHeight)
            {
                floorverts.Clear();
                ceilingverts.Clear();
                flooruvs.Clear();
                ceilinguvs.Clear();

                for (int e = 0; e < sectors[i].vertexIndices.Count; ++e)
                {
                    double YF = sectors[i].floorHeight / 8 * 2.5f;
                    double YC = sectors[i].ceilingHeight / 8 * 2.5f;
                    double X = vertices[sectors[i].vertexIndices[e]].x / 2 * 2.5f;
                    double Z = vertices[sectors[i].vertexIndices[e]].y / 2 * 2.5f;

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
                transformedfloormesh.RecalculateNormals();

                meshes.Add(transformedfloormesh);

                Plane.Add(i);
                Render.Add(i);
                Portal.Add(-1);
                Collision.Add(i);

                Mesh transformedceilingmesh = new Mesh();

                transformedceilingmesh.SetVertices(ceilingverts);
                transformedceilingmesh.SetUVs(0, ceilinguvs);
                transformedceilingmesh.SetTriangles(ceilingtri, 0, true);
                transformedceilingmesh.RecalculateNormals();

                meshes.Add(transformedceilingmesh);
            }
        }
    }

    public void buildEdges()
    {
        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            OpaqueVertices.Clear();

            OpaqueTriangles.Clear();

            int lineCount = 0;

            for (int e = LevelLists.sectors[i].portalStartIndex; e < LevelLists.sectors[i].portalStartIndex + LevelLists.sectors[i].portalCount; e++)
            {
                for (int f = LevelLists.portals[e].lineStartIndex; f < LevelLists.portals[e].lineStartIndex + LevelLists.portals[e].lineCount; f++)
                {
                    OpaqueVertices.Add(LevelLists.edges[f].start);
                    OpaqueVertices.Add(LevelLists.edges[f].end);
                    OpaqueTriangles.Add(lineCount);
                    OpaqueTriangles.Add(lineCount + 1);
                    lineCount += 2;
                }
            }

            Mesh combinedmesh = new Mesh();

            EdgeMesh.Add(combinedmesh);

            combinedmesh.SetVertices(OpaqueVertices);

            combinedmesh.SetIndices(OpaqueTriangles, MeshTopology.Lines, 0);

            GameObject meshObject = new GameObject("Edges " + i);

            Edges.Add(meshObject);

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = linematerial;

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();

            meshFilter.sharedMesh = combinedmesh;

            meshObject.transform.SetParent(EdgeObjects.transform);
        }
    }

    public void buildOpaques()
    {
        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            OpaqueVertices.Clear();

            OpaqueTextures.Clear();

            OpaqueNormals.Clear();

            OpaqueTriangles.Clear();

            int triangleCount = 0;

            for (int e = LevelLists.sectors[i].opaqueStartIndex; e < LevelLists.sectors[i].opaqueStartIndex + LevelLists.sectors[i].opaqueCount; e++)
            {
                OpaqueVertices.Add(LevelLists.opaques[e].v0);
                OpaqueVertices.Add(LevelLists.opaques[e].v1);
                OpaqueVertices.Add(LevelLists.opaques[e].v2);
                OpaqueTextures.Add(LevelLists.opaques[e].uv0);
                OpaqueTextures.Add(LevelLists.opaques[e].uv1);
                OpaqueTextures.Add(LevelLists.opaques[e].uv2);
                OpaqueNormals.Add(LevelLists.opaques[e].n0);
                OpaqueNormals.Add(LevelLists.opaques[e].n1);
                OpaqueNormals.Add(LevelLists.opaques[e].n2);
                OpaqueTriangles.Add(triangleCount);
                OpaqueTriangles.Add(triangleCount + 1);
                OpaqueTriangles.Add(triangleCount + 2);
                triangleCount += 3;
            }

            Mesh combinedmesh = new Mesh();

            OpaqueMesh.Add(combinedmesh);

            combinedmesh.SetVertices(OpaqueVertices);

            combinedmesh.SetUVs(0, OpaqueTextures);

            combinedmesh.SetTriangles(OpaqueTriangles, 0);

            combinedmesh.SetNormals(OpaqueNormals);

            GameObject meshObject = new GameObject("Opaque " + i);

            OpaqueSectors.Add(meshObject);

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = opaquematerial;

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();

            meshFilter.sharedMesh = combinedmesh;

            meshObject.transform.SetParent(OpaqueObjects.transform);
        }
    }

    public void buildCollsionSectors()
    {
        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            OpaqueVertices.Clear();

            OpaqueTriangles.Clear();

            int triangleCount = 0;

            for (int e = LevelLists.sectors[i].collisionStartIndex; e < LevelLists.sectors[i].collisionStartIndex + LevelLists.sectors[i].collisionCount; e++)
            {
                OpaqueVertices.Add(LevelLists.collisions[e].v0);
                OpaqueVertices.Add(LevelLists.collisions[e].v1);
                OpaqueVertices.Add(LevelLists.collisions[e].v2);
                OpaqueTriangles.Add(triangleCount);
                OpaqueTriangles.Add(triangleCount + 1);
                OpaqueTriangles.Add(triangleCount + 2);
                triangleCount += 3;
            }

            Mesh combinedmesh = new Mesh();

            CollisionMesh.Add(combinedmesh);

            combinedmesh.SetVertices(OpaqueVertices);

            combinedmesh.SetTriangles(OpaqueTriangles, 0);

            GameObject meshObject = new GameObject("Collision " + i);

            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = combinedmesh;

            CollisionSectors.Add(meshCollider);

            meshObject.transform.SetParent(CollisionObjects.transform);
        }
    }

    public void buildObjects()
    {
        for (int i = 0; i < starts.Count; i++)
        {
            StartPos Start = new StartPos();

            Start.Position = new Vector3(starts[i].location.x / 2 * 2.5f, sectors[starts[i].sector].floorHeight / 8 * 2.5f, starts[i].location.y / 2 * 2.5f);

            Start.SectorID = starts[i].sector;

            LevelLists.positions.Add(Start);
        }
    }

    public void buildLists()
    {
        int planeStart = 0;

        int opaqueStart = 0;

        int collisionStart = 0;

        int portalStart = 0;

        int edgeStart = 0;

        int portalnumber = 0;

        int portalPlaneCount = 0;

        for (int h = 0; h < sectors.Count; h++)
        {
            int planeCount = 0;

            int portalCount = 0;

            int rendersCount = 0;

            int collideCount = 0;

            for (int e = 0; e < Plane.Count; e++)
            {
                if (Plane[e] == h)
                {
                    Mesh mesh = meshes[e];

                    MathematicalPlane sectorplane = new MathematicalPlane();

                    sectorplane.normal = mesh.normals[0];
                    sectorplane.distance = -Vector3.Dot(mesh.normals[0], mesh.vertices[0]);

                    LevelLists.planes.Add(sectorplane);

                    planeCount += 1;
                }

                if (Plane[e] == h && Portal[e] != -1)
                {
                    int edgeCount = 0;

                    Mesh mesh = meshes[e];

                    PortalMeta portalMeta = new PortalMeta();

                    FrustumMeta portalfrustum = new FrustumMeta();

                    for (int x = 0; x < mesh.vertices.Length; x++)
                    {
                        int y = (x + 1) % mesh.vertices.Length;

                        Edge line = new Edge();

                        line.start = mesh.vertices[x];
                        line.end = mesh.vertices[y];

                        LevelLists.edges.Add(line);

                        edgeCount += 1;
                    }

                    portalMeta.lineStartIndex = edgeStart;

                    portalMeta.lineCount = edgeCount;

                    portalMeta.portalPlane = portalPlaneCount;

                    portalMeta.sectorID = h;

                    portalMeta.connectedSectorID = Portal[e];

                    portalMeta.portalID = portalnumber;

                    LevelLists.portals.Add(portalMeta);

                    portalfrustum.planeStartIndex = 0;

                    portalfrustum.planeCount = 0;

                    portalfrustum.frustumID = portalnumber;

                    LevelLists.frustums.Add(portalfrustum);

                    edgeStart += edgeCount;

                    portalCount += 1;

                    portalnumber += 1;
                }

                if (Plane[e] == h)
                {
                    portalPlaneCount += 1;
                }

                if (Render[e] == h)
                {
                    Mesh mesh = meshes[e];

                    uvVector3.Clear();

                    mesh.GetUVs(0, uvVector3);

                    for (int i = 0; i < mesh.triangles.Length; i += 3)
                    {
                        Triangle otriangle = new Triangle();

                        otriangle.v0 = mesh.vertices[mesh.triangles[i]];
                        otriangle.v1 = mesh.vertices[mesh.triangles[i + 1]];
                        otriangle.v2 = mesh.vertices[mesh.triangles[i + 2]];
                        otriangle.uv0 = uvVector3[mesh.triangles[i]];
                        otriangle.uv1 = uvVector3[mesh.triangles[i + 1]];
                        otriangle.uv2 = uvVector3[mesh.triangles[i + 2]];
                        otriangle.n0 = mesh.normals[mesh.triangles[i]];
                        otriangle.n1 = mesh.normals[mesh.triangles[i + 1]];
                        otriangle.n2 = mesh.normals[mesh.triangles[i + 2]];

                        LevelLists.opaques.Add(otriangle);

                        rendersCount += 1;
                    }
                }

                if (Collision[e] == h)
                {
                    Mesh mesh = meshes[e];

                    for (int i = 0; i < mesh.triangles.Length; i += 3)
                    {
                        Triangle ctriangle = new Triangle();

                        ctriangle.v0 = mesh.vertices[mesh.triangles[i]];
                        ctriangle.v1 = mesh.vertices[mesh.triangles[i + 1]];
                        ctriangle.v2 = mesh.vertices[mesh.triangles[i + 2]];

                        LevelLists.collisions.Add(ctriangle);

                        collideCount += 1;
                    }
                }
            }

            SectorMeta sectorMeta = new SectorMeta();

            sectorMeta.planeStartIndex = planeStart;
            sectorMeta.planeCount = planeCount;

            sectorMeta.opaqueStartIndex = opaqueStart;
            sectorMeta.opaqueCount = rendersCount;

            sectorMeta.collisionStartIndex = collisionStart;
            sectorMeta.collisionCount = collideCount;

            sectorMeta.portalStartIndex = portalStart;
            sectorMeta.portalCount = portalCount;

            sectorMeta.sectorID = h;

            LevelLists.sectors.Add(sectorMeta);

            planeStart += planeCount;

            opaqueStart += rendersCount;

            portalStart += portalCount;

            collisionStart += collideCount;
        }

        FrustumMeta camfrustum = new FrustumMeta();

        camfrustum.planeStartIndex = 0;

        camfrustum.planeCount = 0;

        camfrustum.frustumID = portalnumber + 1;

        LevelLists.frustums.Add(camfrustum);

        Debug.Log("Level built successfully!");
    }
}
