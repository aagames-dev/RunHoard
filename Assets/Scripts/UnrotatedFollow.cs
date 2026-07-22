using UnityEngine;

public class UnrotatedFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = Vector3.zero;

    private void Start()
    {
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            // Follow the target's position exactly, but keep rotation strictly at identity
            transform.position = target.position + offset;
            transform.rotation = Quaternion.identity;
        }
    }
}
