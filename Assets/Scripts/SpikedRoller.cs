using UnityEngine;

public class SpikedRoller : MonoBehaviour
{
    public float spinSpeed = 90f;

    private void Update()
    {
        // Rotate slowly on its X-axis to signal danger
        transform.Rotate(Vector3.right, spinSpeed * Time.deltaTime, Space.Self);
    }
}
