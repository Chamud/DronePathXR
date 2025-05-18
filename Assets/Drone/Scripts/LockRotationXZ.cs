using UnityEngine;

public class LockRotationXZ : MonoBehaviour
{
    void LateUpdate()
    {
        Vector3 rot = transform.rotation.eulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        transform.rotation = Quaternion.Euler(rot);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
        }
    }
}
