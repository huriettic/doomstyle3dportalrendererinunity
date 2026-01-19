using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Triangle
{
    public Vector3 v0, v1, v2;
    public Vector3 uv0, uv1, uv2;
    public Vector3 n0, n1, n2;
};

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
public struct PolygonMeta
{
    public int lineStartIndex;
    public int lineCount;

    public int opaqueStartIndex;
    public int opaqueCount;

    public int collisionStartIndex;
    public int collisionCount;

    public int connectedSectorID;
    public int sectorID;

    public int plane;
};

[Serializable]
public struct SectorMeta
{
    public int polygonStartIndex;
    public int polygonCount;

    public int planeStartIndex;
    public int planeCount;

    public int sectorID;
};

[Serializable]
public struct StartPos
{
    public Vector3 Position;
    public int SectorID;
};

public class LevelLoader : MonoBehaviour
{
    public string Name = "twohallways-clear";

    public bool debug = false;

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
    private List<List<MathematicalPlane>> ListOfPlaneLists = new List<List<MathematicalPlane>>();
    private List<List<SectorMeta>> ListOfSectorLists = new List<List<SectorMeta>>();
    private Camera Cam;
    private Vector3 CamPoint;
    private SectorMeta CurrentSector;
    private SectorMeta NextSector;
    private List<SectorMeta> Sectors = new List<SectorMeta>();
    private List<SectorMeta> OldSectors = new List<SectorMeta>();
    private List<Vector3> OutEdgeVertices = new List<Vector3>();
    private bool radius;
    private bool check;
    private int combinedTriangles;
    private float planeDistance;
    private double Ceiling;
    private double Floor;
    private Plane LeftPlane;
    private Plane TopPlane;
    private List<Vector3> uvs = new List<Vector3>();
    private List<Vector3> flooruvs = new List<Vector3>();
    private List<Vector3> ceilinguvs = new List<Vector3>();
    private List<Vector3> OpaqueTextures = new List<Vector3>();
    private GameObject RenderMesh;

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

    [Serializable]
    public class TopLevelLists
    {
        public List<Edge> edges = new List<Edge>();
        public List<Triangle> opaques = new List<Triangle>();
        public List<Triangle> collisions = new List<Triangle>();
        public List<MathematicalPlane> planes = new List<MathematicalPlane>();
        public List<PolygonMeta> polygons = new List<PolygonMeta>();
        public List<SectorMeta> sectors = new List<SectorMeta>();
        public List<StartPos> positions = new List<StartPos>();
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

        BuildGeometry();

        BuildLists();

        BuildObjects();

        BuildColliders();

        if (debug == true)
        {
            BuildEdges();

            BuildOpaques();
        }

        Playerstart();

        processbool = new bool[256];

        processvertices = new Vector3[256];

        processtextures = new Vector3[256];

        processnormals = new Vector3[256];

        temporaryvertices = new Vector3[256];

        temporarytextures = new Vector3[256];

        temporarynormals = new Vector3[256];

        for (int i = 0; i < 2; i++)
        {
            ListOfPlaneLists.Add(new List<MathematicalPlane>());
        }

        for (int i = 0; i < 2; i++)
        {
            ListOfSectorLists.Add(new List<SectorMeta>());
        }

        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[LevelLists.sectors[i].sectorID], true);
        }
    }

    void Update()
    {
        if (debug == false)
        {
            PlayerInput();

            if (Cam.transform.hasChanged)
            {
                CamPoint = Cam.transform.position;

                GetSectors(CurrentSector);

                ListOfPlaneLists[0].Clear();

                ReadFrustumPlanes(Cam, ListOfPlaneLists[0]);

                ListOfPlaneLists[0].RemoveAt(5);

                ListOfPlaneLists[0].RemoveAt(4);

                OpaqueVertices.Clear();

                OpaqueTextures.Clear();

                OpaqueTriangles.Clear();

                OpaqueNormals.Clear();

                combinedTriangles = 0;

                GetPolygons(CurrentSector);

                SetRenderMesh();

                Cam.transform.hasChanged = false;
            }
        }  
    }

    void Awake()
    {
        Player = GameObject.Find("Player").GetComponent<CharacterController>();

        Player.GetComponent<CharacterController>().enabled = true;

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

    public void CreateGameObjects()
    {
        Shader shader = Resources.Load<Shader>("TEXARRAYSHADER");

        opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>("Textures");

        CollisionObjects = new GameObject("Collision Meshes");

        if (debug == true)
        {
            OpaqueObjects = new GameObject("Opaque Meshes");

            EdgeObjects = new GameObject("Portal Meshes");

            linematerial = new Material(shader);

            linematerial.color = Color.cyan;
        }
        else
        {
            opaquemesh = new Mesh();

            opaquemesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            opaquemesh.MarkDynamic();

            RenderMesh = new GameObject("Render Mesh");

            RenderMesh.AddComponent<MeshFilter>();
            RenderMesh.AddComponent<MeshRenderer>();

            Renderer MeshRend = RenderMesh.GetComponent<Renderer>();
            MeshRend.sharedMaterial = opaquematerial;
            MeshRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            RenderMesh.GetComponent<MeshFilter>().mesh = opaquemesh;
        }   
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
        {
            return;
        }
        
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

    public void SetClippingPlanes(bool contains, int portalnumber, int polygonStart, int polygonCount, int side, Vector3 viewPos)
    {
        int StartIndex = ListOfPlaneLists[side].Count;

        int IndexCount = 0;

        if (contains)
        {
            ReadFrustumPlanes(Cam, ListOfPlaneLists[side]);

            ListOfPlaneLists[side].RemoveAt(ListOfPlaneLists[side].Count - 1);

            ListOfPlaneLists[side].RemoveAt(ListOfPlaneLists[side].Count - 1);

            NextSector.polygonStartIndex = polygonStart;
            NextSector.polygonCount = polygonCount;

            NextSector.planeStartIndex = StartIndex;
            NextSector.planeCount = ListOfPlaneLists[side].Count - StartIndex;

            NextSector.sectorID = portalnumber;
        }
        else
        {
            for (int i = 0; i < OutEdgeVertices.Count; i += 2)
            {
                Vector3 p1 = OutEdgeVertices[i];
                Vector3 p2 = OutEdgeVertices[i + 1];
                Vector3 normal = Vector3.Cross(p1 - p2, viewPos - p2);
                float magnitude = normal.magnitude;

                if (magnitude < 0.01f)
                {
                    continue;
                }

                Vector3 normalized = normal / magnitude;

                ListOfPlaneLists[side].Add(new MathematicalPlane { normal = normalized, distance = -Vector3.Dot(normalized, p1) });
                IndexCount += 1;
            }

            NextSector.polygonStartIndex = polygonStart;
            NextSector.polygonCount = polygonCount;

            NextSector.planeStartIndex = StartIndex;
            NextSector.planeCount = IndexCount;

            NextSector.sectorID = portalnumber;
        }
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

    public void ClipTrianglesWithPlanes(SectorMeta planes, List<Triangle> verttexnorm, int startIndex, int count, int side)
    {
        for (int a = startIndex; a < count; a++)
        {
            int processverticescount = 0;
            int processtexturescount = 0;
            int processnormalscount = 0;
            int processboolcount = 0;

            Triangle tri = verttexnorm[a];

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

                    float d0 = GetPlaneSignedDistanceToPoint(ListOfPlaneLists[side][b], v0);
                    float d1 = GetPlaneSignedDistanceToPoint(ListOfPlaneLists[side][b], v1);
                    float d2 = GetPlaneSignedDistanceToPoint(ListOfPlaneLists[side][b], v2);

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
                    OpaqueVertices.Add(processvertices[e]);
                    OpaqueVertices.Add(processvertices[e + 1]);
                    OpaqueVertices.Add(processvertices[e + 2]);
                    OpaqueTextures.Add(processtextures[e]);
                    OpaqueTextures.Add(processtextures[e + 1]);
                    OpaqueTextures.Add(processtextures[e + 2]);
                    OpaqueNormals.Add(processnormals[e]);
                    OpaqueNormals.Add(processnormals[e + 1]);
                    OpaqueNormals.Add(processnormals[e + 2]);
                    OpaqueTriangles.Add(combinedTriangles);
                    OpaqueTriangles.Add(combinedTriangles + 1);
                    OpaqueTriangles.Add(combinedTriangles + 2);
                    combinedTriangles += 3;
                }
            }
        }
    }

    public void ClipEdgesWithPlanes(SectorMeta planes, PolygonMeta portal, int side)
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

                float d1 = GetPlaneSignedDistanceToPoint(ListOfPlaneLists[side][b], p1);
                float d2 = GetPlaneSignedDistanceToPoint(ListOfPlaneLists[side][b], p2);

                bool b0 = d1 >= 0;
                bool b1 = d2 >= 0;

                if (b0 && b1)
                {
                    continue;
                }
                else if ((b0 && !b1) || (!b0 && b1))
                {
                    Vector3 point1;
                    Vector3 point2;

                    float t = d1 / (d1 - d2);

                    Vector3 intersectionPoint = Vector3.Lerp(p1, p2, t);

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
        int sidea = 0;
        int sideb = 1;

        Sectors.Clear();

        ListOfSectorLists[sidea].Clear();

        ListOfSectorLists[sidea].Add(ASector);

        for (int a = 0; a < OldSectors.Count; a++)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[OldSectors[a].sectorID], true);
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

                Sectors.Add(sector);

                Physics.IgnoreCollision(Player, CollisionSectors[sector.sectorID], false);

                for (int d = sector.polygonStartIndex; d < sector.polygonStartIndex + sector.polygonCount; d++)
                {
                    int connectedsector = LevelLists.polygons[d].connectedSectorID;

                    if (connectedsector == -1)
                    {
                        continue;
                    }

                    SectorMeta portalsector = LevelLists.sectors[connectedsector];

                    if (SectorsContains(portalsector.sectorID))
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
            OldSectors.Clear();

            for (int e = 0; e < Sectors.Count; e++)
            {
                OldSectors.Add(Sectors[e]);
            }
        }
    }

    public void GetPolygons(SectorMeta ASector)
    {
        int sidea = 0;
        int sideb = 1;

        ListOfSectorLists[sidea].Clear();

        ListOfSectorLists[sidea].Add(ASector);

        for (int a = 0; a < 4096; a++)
        {
            if (a % 2 == 0)
            {
                sidea = 0;
                sideb = 1;
            }
            else
            {
                sidea = 1;
                sideb = 0;
            }

            ListOfPlaneLists[sideb].Clear();

            ListOfSectorLists[sideb].Clear();

            if (ListOfSectorLists[sidea].Count == 0)
            {
                break;
            }

            for (int b = 0; b < ListOfSectorLists[sidea].Count; b++)
            {
                SectorMeta sector = ListOfSectorLists[sidea][b];

                for (int c = sector.polygonStartIndex; c < sector.polygonStartIndex + sector.polygonCount; c++)
                {
                    PolygonMeta polygon = LevelLists.polygons[c];

                    planeDistance = GetPlaneSignedDistanceToPoint(LevelLists.planes[polygon.plane], CamPoint);

                    if (planeDistance <= 0)
                    {
                        continue;
                    }

                    int connectedsector = polygon.connectedSectorID;

                    if (connectedsector == -1)
                    {
                        ClipTrianglesWithPlanes(sector, LevelLists.opaques, polygon.opaqueStartIndex, polygon.opaqueStartIndex + polygon.opaqueCount, sidea);

                        continue;
                    }

                    SectorMeta sectorpolygon = LevelLists.sectors[connectedsector];

                    int connectedstart = sectorpolygon.polygonStartIndex;

                    int connectedcount = sectorpolygon.polygonCount;

                    if (SectorsContains(sectorpolygon.sectorID))
                    {
                        SetClippingPlanes(true, connectedsector, connectedstart, connectedcount, sideb, CamPoint);

                        ListOfSectorLists[sideb].Add(NextSector);

                        continue;
                    }

                    ClipEdgesWithPlanes(sector, polygon, sidea);

                    if (OutEdgeVertices.Count < 6 || OutEdgeVertices.Count % 2 == 1)
                    {
                        continue;
                    }

                    SetClippingPlanes(false, connectedsector, connectedstart, connectedcount, sideb, CamPoint);

                    ListOfSectorLists[sideb].Add(NextSector);
                }
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

    public void BuildGeometry()
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

    public void BuildEdges()
    {
        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            OpaqueVertices.Clear();

            OpaqueTriangles.Clear();

            int lineCount = 0;

            for (int e = LevelLists.sectors[i].polygonStartIndex; e < LevelLists.sectors[i].polygonStartIndex + LevelLists.sectors[i].polygonCount; e++)
            {
                if (LevelLists.polygons[e].lineCount != -1)
                {
                    for (int f = LevelLists.polygons[e].lineStartIndex; f < LevelLists.polygons[e].lineStartIndex + LevelLists.polygons[e].lineCount; f++)
                    {
                        OpaqueVertices.Add(LevelLists.edges[f].start);
                        OpaqueVertices.Add(LevelLists.edges[f].end);
                        OpaqueTriangles.Add(lineCount);
                        OpaqueTriangles.Add(lineCount + 1);
                        lineCount += 2;
                    }
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

    public void BuildOpaques()
    {
        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            OpaqueVertices.Clear();

            OpaqueTextures.Clear();

            OpaqueNormals.Clear();

            OpaqueTriangles.Clear();

            int triangleCount = 0;

            for (int e = LevelLists.sectors[i].polygonStartIndex; e < LevelLists.sectors[i].polygonStartIndex + LevelLists.sectors[i].polygonCount; e++)
            {
                if (LevelLists.polygons[e].opaqueCount != -1)
                {
                    for (int f = LevelLists.polygons[e].opaqueStartIndex; f < LevelLists.polygons[e].opaqueStartIndex + LevelLists.polygons[e].opaqueCount; f++)
                    {
                        OpaqueVertices.Add(LevelLists.opaques[f].v0);
                        OpaqueVertices.Add(LevelLists.opaques[f].v1);
                        OpaqueVertices.Add(LevelLists.opaques[f].v2);
                        OpaqueTextures.Add(LevelLists.opaques[f].uv0);
                        OpaqueTextures.Add(LevelLists.opaques[f].uv1);
                        OpaqueTextures.Add(LevelLists.opaques[f].uv2);
                        OpaqueNormals.Add(LevelLists.opaques[f].n0);
                        OpaqueNormals.Add(LevelLists.opaques[f].n1);
                        OpaqueNormals.Add(LevelLists.opaques[f].n2);
                        OpaqueTriangles.Add(triangleCount);
                        OpaqueTriangles.Add(triangleCount + 1);
                        OpaqueTriangles.Add(triangleCount + 2);
                        triangleCount += 3;
                    }
                }
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

    public void BuildColliders()
    {
        for (int i = 0; i < LevelLists.sectors.Count; i++)
        {
            OpaqueVertices.Clear();

            OpaqueTriangles.Clear();

            int triangleCount = 0;

            for (int e = LevelLists.sectors[i].polygonStartIndex; e < LevelLists.sectors[i].polygonStartIndex + LevelLists.sectors[i].polygonCount; e++)
            {
                if (LevelLists.polygons[e].collisionCount != -1)
                {
                    for (int f = LevelLists.polygons[e].collisionStartIndex; f < LevelLists.polygons[e].collisionStartIndex + LevelLists.polygons[e].collisionCount; f++)
                    {
                        OpaqueVertices.Add(LevelLists.collisions[f].v0);
                        OpaqueVertices.Add(LevelLists.collisions[f].v1);
                        OpaqueVertices.Add(LevelLists.collisions[f].v2);
                        OpaqueTriangles.Add(triangleCount);
                        OpaqueTriangles.Add(triangleCount + 1);
                        OpaqueTriangles.Add(triangleCount + 2);
                        triangleCount += 3;
                    }
                } 
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

    public void BuildObjects()
    {
        for (int i = 0; i < starts.Count; i++)
        {
            StartPos Start = new StartPos();

            Start.Position = new Vector3(starts[i].location.x / 2 * 2.5f, sectors[starts[i].sector].floorHeight / 8 * 2.5f, starts[i].location.y / 2 * 2.5f);

            Start.SectorID = starts[i].sector;

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
                            start = mesh.vertices[c],
                            end = mesh.vertices[d]
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

                MathematicalPlane plane = new MathematicalPlane
                {
                    normal = mesh.normals[0],
                    distance = -Vector3.Dot(mesh.normals[0], mesh.vertices[0])
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
                            uv2 = uvVector3[mesh.triangles[c + 2]],
                            n0 = mesh.normals[mesh.triangles[c]],
                            n1 = mesh.normals[mesh.triangles[c + 1]],
                            n2 = mesh.normals[mesh.triangles[c + 2]]
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
                            uv0 = Vector3.zero,
                            uv1 = Vector3.zero,
                            uv2 = Vector3.zero,
                            n0 = Vector3.zero,
                            n1 = Vector3.zero,
                            n2 = Vector3.zero
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

                meta.sectorID = a;
                meta.connectedSectorID = Portal[b];

                LevelLists.polygons.Add(meta);
                polygonCount += 1;
            }

            SectorMeta sectorMeta = new SectorMeta
            {
                sectorID = a,
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
