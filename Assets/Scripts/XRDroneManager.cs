using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class XRDroneManager : MonoBehaviour
{
    [Header("Ray Interactors")]
    public XRRayInteractor leftRayInteractor;
    public XRRayInteractor rightRayInteractor;

    [Header("Input Actions")]
    public InputActionProperty removeAction;
    public InputActionProperty spawnAction;
    public InputActionProperty playAction;
    public InputActionProperty switchAction;

    [Header("Spawn Settings")]
    public GameObject waypointPrefab;
    public Transform spawnPoint;
    public float spawnDistance = 0.5f;

    [Header("Camera Fly Settings")]
    public GameObject droneCamera;
    public float droneSpeed = 5f;

    [Header("Waypoints")]
    public List<GameObject> waypoints = new List<GameObject>();

    private Camera xrMainCamera;
    private bool isPlaying = false;
    private GameObject currentDroneCam;
    private Coroutine playPathCoroutine;

    void Start()
    {
        xrMainCamera = Camera.main;
    }

    void Update()
    {
        HandleRightHand();
        HandleLeftHand();

        if (playAction.action.WasPerformedThisFrame())
        {
            if (!isPlaying && waypoints.Count > 1)
            {
                playPathCoroutine = StartCoroutine(PlayCameraPath());
            }
            else if (isPlaying)
            {
                StopCoroutine(playPathCoroutine);
                CleanupCameraPath();
            }
        }

        if (switchAction.action.WasPerformedThisFrame() && isPlaying)
        {
            if (xrMainCamera.enabled)
            {
                xrMainCamera.enabled = false;
                if (currentDroneCam != null)
                {
                    Camera droneCam = currentDroneCam.GetComponent<Camera>();
                    if (droneCam != null)
                        droneCam.enabled = true;
                    AudioListener listener = currentDroneCam.GetComponent<AudioListener>();
                    if (listener != null)
                        listener.enabled = true;
                }
            }
            else
            {
                xrMainCamera.enabled = true;
                if (currentDroneCam != null)
                {
                    Camera droneCam = currentDroneCam.GetComponent<Camera>();
                    if (droneCam != null)
                        droneCam.enabled = false;
                    AudioListener listener = currentDroneCam.GetComponent<AudioListener>();
                    if (listener != null)
                        listener.enabled = false;
                }
            }
        }
    }

    private void HandleRightHand()
    {
        if (spawnAction.action.WasPerformedThisFrame())
        {
            if (!rightRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                Vector3 spawnPosition = spawnPoint.position + spawnPoint.forward * spawnDistance;
                Quaternion spawnRotation = Quaternion.Euler(0, spawnPoint.eulerAngles.y, 0);

                GameObject newWaypoint = Instantiate(waypointPrefab, spawnPosition, spawnRotation);
                waypoints.Add(newWaypoint);
                UpdateObjectLabels();

                Transform gimbal = newWaypoint.transform.Find("Gimbal");
                if (gimbal != null)
                {
                    Vector3 gimbalEuler = gimbal.localEulerAngles;
                    gimbal.localEulerAngles = new Vector3(spawnPoint.eulerAngles.x, gimbalEuler.y, gimbalEuler.z);
                }
            }
        }
    }

    private void HandleLeftHand()
    {
        if (removeAction.action.WasPerformedThisFrame())
        {
            if (leftRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                XRBaseInteractable interactable = hit.collider.GetComponent<XRBaseInteractable>();
                if (interactable != null)
                {
                    GameObject objToRemove = interactable.gameObject;

                    if (waypoints.Contains(objToRemove))
                    {
                        waypoints.Remove(objToRemove);
                        Destroy(objToRemove);
                        UpdateObjectLabels();
                    }
                }
            }
        }
    }

    private void UpdateObjectLabels()
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            GameObject obj = waypoints[i];
            TextMeshPro tmp = obj.GetComponentInChildren<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = (i + 1).ToString();
            }
        }
    }

    private void CleanupCameraPath()
    {
        if (currentDroneCam != null)
            Destroy(currentDroneCam);

        if (xrMainCamera != null)
            xrMainCamera.enabled = true;

        foreach (GameObject waypoint in waypoints)
        {
            Transform gimbal = waypoint.transform.Find("Gimbal");
            if (gimbal != null)
            {
                Transform camera = gimbal.Find("Camera");
                if (camera != null)
                {
                    camera.gameObject.SetActive(true);
                }
            }
        }

        isPlaying = false;
        currentDroneCam = null;
    }

    // 🆕 Coroutine to fly a camera through the waypoints
    private IEnumerator PlayCameraPath()
    {
        isPlaying = true;

        foreach (GameObject waypoint in waypoints)
        {
            Transform gimbal = waypoint.transform.Find("Gimbal");
            if (gimbal != null)
            {
                Transform camera = gimbal.Find("Camera"); 
                if (camera != null)
                {
                    // Found both Gimbal and Camera
                    camera.gameObject.SetActive(false);
                }
            }
        }

        // Disable XR main camera
        if (xrMainCamera != null)
            xrMainCamera.enabled = false;

        // Instantiate flying camera at first waypoint
        Vector3 startPos = waypoints[0].transform.position;
        float startY = waypoints[0].transform.eulerAngles.y;
        float startX = waypoints[0].transform.Find("Gimbal").localEulerAngles.x;
        Quaternion startRot = Quaternion.Euler(startX, startY, 0);
        currentDroneCam = Instantiate(droneCamera, startPos, startRot);
        Camera flyingCam = currentDroneCam.GetComponent<Camera>();
        if (flyingCam != null)
            flyingCam.enabled = true;

        AudioListener listener = currentDroneCam.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = true;

        for (int i = 1; i < waypoints.Count; i++)
        {
            Vector3 fromPos = currentDroneCam.transform.position;
            Quaternion fromRot = currentDroneCam.transform.rotation;

            Vector3 toPos = waypoints[i].transform.position;

            Quaternion toRot;

            float toY = waypoints[i].transform.eulerAngles.y;
            float toX = waypoints[i].transform.Find("Gimbal").localEulerAngles.x;
            toRot = Quaternion.Euler(toX, toY, 0);

            float distance = Vector3.Distance(fromPos, toPos);
            float duration = distance / droneSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                currentDroneCam.transform.position = Vector3.Lerp(fromPos, toPos, t);
                currentDroneCam.transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            currentDroneCam.transform.position = toPos;
            currentDroneCam.transform.rotation = toRot;
            yield return new WaitForSeconds(0.2f);
        }

        CleanupCameraPath();
    }
}
