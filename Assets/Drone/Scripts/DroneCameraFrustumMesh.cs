using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DroneCameraFrustumMeshWithEdges : MonoBehaviour
{
    [Range(1f, 179f)]
    public float fieldOfView = 60f;
    [Range(1f, 100f)]
    public float farClipPlane = 10f;
    public float aspectRatio = 16f / 9f;

    public Color faceColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
    public Color edgeColor = new Color(0f, 0f, 0f, 1f);

    private Material lineMat;

    private Vector3[] frustumCorners = new Vector3[4]; // Far clip corners

    void Start()
    {
        GenerateFrustumMesh();
        CreateLineMaterial();
    }

    void OnValidate()
    {
        GenerateFrustumMesh();
        CreateLineMaterial();
    }

    void GenerateFrustumMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        float halfFOV = fieldOfView * 0.5f * Mathf.Deg2Rad;
        float height = Mathf.Tan(halfFOV) * farClipPlane;
        float width = height * aspectRatio;

        Vector3 apex = Vector3.zero;

        frustumCorners[0] = new Vector3(-width, -height, farClipPlane); // bottomLeft
        frustumCorners[1] = new Vector3(-width, height, farClipPlane);  // topLeft
        frustumCorners[2] = new Vector3(width, height, farClipPlane);   // topRight
        frustumCorners[3] = new Vector3(width, -height, farClipPlane);  // bottomRight

        mesh.vertices = new Vector3[]
        {
            apex,               // 0
            frustumCorners[1],  // 1 (topLeft)
            frustumCorners[2],  // 2 (topRight)
            frustumCorners[3],  // 3 (bottomRight)
            frustumCorners[0]   // 4 (bottomLeft)
        };

        mesh.triangles = new int[]
        {
            0,1,2, // Top face
            0,2,3, // Right face
            0,3,4, // Bottom face
            0,4,1  // Left face
        };

        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

    }

    void CreateLineMaterial()
    {
        if (lineMat == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMat = new Material(shader);
            lineMat.hideFlags = HideFlags.HideAndDontSave;
            lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMat.SetInt("_ZWrite", 0);
        }
    }

    void OnRenderObject()
    {
        if (lineMat == null || frustumCorners.Length != 4)
            return;

        lineMat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);

        GL.Begin(GL.LINES);

        DrawEdge(Vector3.zero, frustumCorners[0]); // BottomLeft
        DrawEdge(Vector3.zero, frustumCorners[1]); // TopLeft
        DrawEdge(Vector3.zero, frustumCorners[2]); // TopRight
        DrawEdge(Vector3.zero, frustumCorners[3]); // BottomRight


        GL.End();
        GL.PopMatrix();
    }

    void DrawEdge(Vector3 from, Vector3 to)
    {
        GL.Color(edgeColor); // Opaque near
        GL.Vertex(from);

        Color transparent = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0f);
        GL.Color(transparent); // Transparent far
        GL.Vertex(to);
    }
}
