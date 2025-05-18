using UnityEngine;

public class FaceDirection : MonoBehaviour
{
    private Transform parent;

    void Start()
    {
        parent = transform.parent;
    }

    void LateUpdate()
    {
        if (parent != null)
        {
            // Follow position
            transform.position = parent.position;

            // Copy only Y rotation
            Vector3 parentEuler = parent.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, parentEuler.y, 0f);
        }
    }
}
