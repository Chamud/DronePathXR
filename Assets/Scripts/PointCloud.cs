using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PointCloudRaycaster : MonoBehaviour
{
    public float scanRadius = 50f;
    public float stepSize = 0.5f;  // Increased step for performance; adjust as needed
    public string outputFileName = "pointcloud.ply";
    public float scanDelay = 15f;

    private List<Vector3> points = new List<Vector3>();

    void Start()
    {
        Debug.Log($"Waiting {scanDelay} seconds before starting scan...");
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(scanDelay);
        yield return StartCoroutine(ScanEnvironment());
    }

    IEnumerator ScanEnvironment()
    {
        Vector3 origin = transform.position;
        float half = scanRadius;

        Debug.Log("Starting scan...");

        string[] directions = { "X+", "X-", "Y+", "Y-", "Z+", "Z-" };

        foreach (string dir in directions)
        {
            Debug.Log($"Scanning direction: {dir} → outward");
            yield return StartCoroutine(RaycastFromCenter(origin, dir));
            Debug.Log($"Scanning direction: {dir} → inward");
            yield return StartCoroutine(RaycastFromBounds(origin, dir));
        }

        Debug.Log($"Scan complete. Points collected: {points.Count}");
        SaveToPLY(points);
    }

    IEnumerator RaycastFromCenter(Vector3 origin, string axis)
    {
        float half = scanRadius;

        for (float u = -half; u <= half; u += stepSize)
        {
            for (float v = -half; v <= half; v += stepSize)
            {
                Vector3 dir = Vector3.zero;

                switch (axis)
                {
                    case "X+":
                        dir = new Vector3(1, u / half, v / half).normalized;
                        break;
                    case "X-":
                        dir = new Vector3(-1, u / half, v / half).normalized;
                        break;
                    case "Y+":
                        dir = new Vector3(u / half, 1, v / half).normalized;
                        break;
                    case "Y-":
                        dir = new Vector3(u / half, -1, v / half).normalized;
                        break;
                    case "Z+":
                        dir = new Vector3(u / half, v / half, 1).normalized;
                        break;
                    case "Z-":
                        dir = new Vector3(u / half, v / half, -1).normalized;
                        break;
                }

                if (Physics.Raycast(origin, dir, out RaycastHit hit, scanRadius))
                {
                    points.Add(hit.point);
                }
            }

            yield return null; // Yield to avoid freeze
        }
    }

    IEnumerator RaycastFromBounds(Vector3 origin, string axis)
    {
        float half = scanRadius;

        for (float u = -half; u <= half; u += stepSize)
        {
            for (float v = -half; v <= half; v += stepSize)
            {
                Vector3 start = Vector3.zero;
                Vector3 dir = Vector3.zero;

                switch (axis)
                {
                    case "X+":
                        start = origin + new Vector3(half, u, v);
                        dir = (origin - start).normalized;
                        break;
                    case "X-":
                        start = origin + new Vector3(-half, u, v);
                        dir = (origin - start).normalized;
                        break;
                    case "Y+":
                        start = origin + new Vector3(u, half, v);
                        dir = (origin - start).normalized;
                        break;
                    case "Y-":
                        start = origin + new Vector3(u, -half, v);
                        dir = (origin - start).normalized;
                        break;
                    case "Z+":
                        start = origin + new Vector3(u, v, half);
                        dir = (origin - start).normalized;
                        break;
                    case "Z-":
                        start = origin + new Vector3(u, v, -half);
                        dir = (origin - start).normalized;
                        break;
                }

                if (Physics.Raycast(start, dir, out RaycastHit hit, scanRadius))
                {
                    points.Add(hit.point);
                }
            }

            yield return null; // Yield to avoid freeze
        }
    }

    void SaveToPLY(List<Vector3> pointCloud)
    {
        string path = Path.Combine(Application.persistentDataPath, outputFileName);
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("ply");
            writer.WriteLine("format ascii 1.0");
            writer.WriteLine($"element vertex {pointCloud.Count}");
            writer.WriteLine("property float x");
            writer.WriteLine("property float y");
            writer.WriteLine("property float z");
            writer.WriteLine("end_header");

            foreach (Vector3 point in pointCloud)
            {
                writer.WriteLine($"{point.x} {point.y} {point.z}");
            }
        }

        Debug.Log($"✅ Point cloud saved to: {path}");
    }
}
