using UnityEngine;

public class SpinningCoin : MonoBehaviour
{
    public float rotationSpeed = 120f;

    private void Update()
    {
        // Continuously rotate on the Y-axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
