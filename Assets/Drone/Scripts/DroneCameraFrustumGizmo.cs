using UnityEngine;

[ExecuteAlways]
public class DroneCameraFrustumGizmo : MonoBehaviour
{
    [Range(1f, 179f)]
    public float fieldOfView = 60f;
    [Range(1f, 100f)]
    public float farClipPlane = 10f;
    public float aspectRatio = 16f / 9f;

    public Color faceColor = new Color(0.5f, 0.5f, 0.5f, 0.4f); 
    public Color edgeColor = new Color(0f, 0f, 0f, 0.7f); 

    private void OnRenderObject()
    {
        Transform camTransform = transform.parent;
        if (camTransform == null) return;

        Matrix4x4 m = camTransform.localToWorldMatrix;

        float fov = fieldOfView;
        float far = farClipPlane;
        float aspect = aspectRatio;

        Vector3[] frustumCorners = new Vector3[4];
        CalculateFrustumCorners(fov, aspect, far, ref frustumCorners);

        Vector3 camPos = camTransform.position;

        Vector3 farTopLeft = m.MultiplyPoint(frustumCorners[1]);
        Vector3 farTopRight = m.MultiplyPoint(frustumCorners[2]);
        Vector3 farBottomRight = m.MultiplyPoint(frustumCorners[3]);
        Vector3 farBottomLeft = m.MultiplyPoint(frustumCorners[0]);

        // Draw faces
        DrawTransparentQuad(camPos, farTopLeft, farTopRight);
        DrawTransparentQuad(camPos, farBottomRight, farBottomLeft);
        DrawTransparentQuad(camPos, farTopRight, farBottomRight);
        DrawTransparentQuad(camPos, farBottomLeft, farTopLeft);

        // Draw edges
        DrawGradientLine(camPos, farTopLeft, edgeColor);
        DrawGradientLine(camPos, farTopRight, edgeColor);
        DrawGradientLine(camPos, farBottomRight, edgeColor);
        DrawGradientLine(camPos, farBottomLeft, edgeColor);
    }

    private void DrawGradientLine(Vector3 from, Vector3 to, Color color)
    {
        if (!Application.isPlaying && Camera.current == null) return;

        Material mat = GetGizmoMaterial();
        mat.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.LINES);

        GL.Color(color); // opaque near camera
        GL.Vertex(from);

        Color transparentColor = new Color(color.r, color.g, color.b, 0f);
        GL.Color(transparentColor); // transparent at far end
        GL.Vertex(to);

        GL.End();
        GL.PopMatrix();
    }

    private void CalculateFrustumCorners(float fov, float aspect, float distance, ref Vector3[] corners)
    {
        float halfFOV = fov * 0.5f * Mathf.Deg2Rad;
        float height = Mathf.Tan(halfFOV) * distance;
        float width = height * aspect;

        corners[0] = new Vector3(-width, -height, distance); // BottomLeft
        corners[1] = new Vector3(-width, height, distance);  // TopLeft
        corners[2] = new Vector3(width, height, distance);   // TopRight
        corners[3] = new Vector3(width, -height, distance);  // BottomRight
    }

    private void DrawTransparentQuad(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        if (!Application.isPlaying && Camera.current == null) return;

        Material mat = GetGizmoMaterial();
        mat.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.TRIANGLES);

        // First triangle: p1 (camera) - p2 - p3
        GL.Color(new Color(faceColor.r, faceColor.g, faceColor.b, faceColor.a)); // p1: opaque
        GL.Vertex(p1);

        GL.Color(new Color(faceColor.r, faceColor.g, faceColor.b, 0f)); // p2: transparent
        GL.Vertex(p2);

        GL.Color(new Color(faceColor.r, faceColor.g, faceColor.b, 0f)); // p3: transparent
        GL.Vertex(p3);

        GL.End();
        GL.PopMatrix();
    }

    private Material _gizmoMat;
    private Material GetGizmoMaterial()
    {
        if (_gizmoMat == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            _gizmoMat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _gizmoMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _gizmoMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _gizmoMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _gizmoMat.SetInt("_ZWrite", 0);
        }
        return _gizmoMat;
    }
}
