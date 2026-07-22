using UnityEngine;

public class SwingingPendulum : MonoBehaviour
{
    public float swingSpeed = 2.5f;
    public float maxAngle = 60f;

    private Transform pivotArm;

    private void Start()
    {
        pivotArm = transform.Find("PivotArm");
    }

    private void Update()
    {
        if (pivotArm != null)
        {
            // Swing back and forth on Z-axis to cross horizontal lanes
            float angle = Mathf.Sin(Time.time * swingSpeed) * maxAngle;
            pivotArm.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
