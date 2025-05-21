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

    [Header("Section Distances")]
    public float section1Distance = 1f;
    public float section2Distance = 5f;

    [Header("Colors")]
    public Color faceColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
    public Color edgeColor = new Color(0f, 0f, 0f, 1f);

    private Material lineMat;

    private void Start()
    {
        GenerateFrustumMesh();
        CreateLineMaterial();
    }

    private void OnValidate()
    {
        GenerateFrustumMesh();
        CreateLineMaterial();
    }

    void GenerateFrustumMesh()
    {
        section1Distance = Mathf.Clamp(section1Distance, 0.01f, farClipPlane);
        section2Distance = Mathf.Clamp(section2Distance, section1Distance + 0.01f, farClipPlane);

        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.subMeshCount = 3;

        Vector3 apex = Vector3.zero;

        Vector3[] vertices = new Vector3[13];

        void SetCorners(float z, int baseIndex)
        {
            float halfFOV = fieldOfView * 0.5f * Mathf.Deg2Rad;
            float height = Mathf.Tan(halfFOV) * z;
            float width = height * aspectRatio;

            vertices[baseIndex + 0] = new Vector3(-width, -height, z); // bottomLeft
            vertices[baseIndex + 1] = new Vector3(-width, height, z);  // topLeft
            vertices[baseIndex + 2] = new Vector3(width, height, z);   // topRight
            vertices[baseIndex + 3] = new Vector3(width, -height, z);  // bottomRight
        }

        vertices[0] = apex;               // 0
        SetCorners(section1Distance, 1);  // 1-4
        SetCorners(section2Distance, 5);  // 5-8
        SetCorners(farClipPlane, 9);      // 9-12

        mesh.vertices = vertices;

        // Submesh 0: pyramid (apex to section1)
        int[] triangles0 = new int[]
        {
            0, 2, 1,
            0, 3, 2,
            0, 4, 3,
            0, 1, 4
        };

        // Submesh 1: section1 to section2
        int[] triangles1 = new int[]
        {
            1, 2, 6,  1, 6, 5, // Top
            2, 3, 7,  2, 7, 6, // Right
            3, 4, 8,  3, 8, 7, // Bottom
            4, 1, 5,  4, 5, 8  // Left
        };

        // Submesh 2: section2 to farClip
        int[] triangles2 = new int[]
        {
            5, 6,10,  5,10, 9, // Top
            6, 7,11,  6,11,10, // Right
            7, 8,12,  7,12,11, // Bottom
            8, 5, 9,  8, 9,12  // Left
        };

        mesh.SetTriangles(triangles0, 0);
        mesh.SetTriangles(triangles1, 1);
        mesh.SetTriangles(triangles2, 2);

        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

        // Optional: assign materials for each submesh
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterials.Length < 3)
        {
            Material defaultMat = new Material(Shader.Find("Standard"));
            defaultMat.color = faceColor;

            Material mat1 = new Material(defaultMat);
            mat1.color = new Color(1f, 0.5f, 0.5f, 0.4f);
            Material mat2 = new Material(defaultMat);
            mat2.color = new Color(0.5f, 1f, 0.5f, 0.4f);

            mr.sharedMaterials = new Material[] { defaultMat, mat1, mat2 };
        }
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
        if (lineMat == null)
            return;

        lineMat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);

        Vector3[] corners = new Vector3[12];
        System.Array.Copy(GetComponent<MeshFilter>().sharedMesh.vertices, 1, corners, 0, 12);

        // Draw lines between all levels (3 levels: 1-4, 5-8, 9-12)
        for (int i = 0; i < 4; i++)
        {
            GL.Color(new Color(edgeColor.r, edgeColor.g, edgeColor.b, 1f));
            GL.Vertex(Vector3.zero);
            GL.Color(new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.7f));
            GL.Vertex(corners[i]);

            GL.Color(new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.7f));
            GL.Vertex(corners[i]);
            GL.Color(new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.3f));
            GL.Vertex(corners[i + 4]);

            GL.Color(new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.3f));
            GL.Vertex(corners[i + 4]);
            GL.Color(new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0f));
            GL.Vertex(corners[i + 8]);
        }

        GL.End();
        GL.PopMatrix();
    }

    void DrawEdge(Vector3 from, Vector3 to)
    {
        GL.Color(edgeColor);
        GL.Vertex(from);
        GL.Color(new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0f));
        GL.Vertex(to);
    }
}
