using UnityEngine;

public class DebugGizmos : MonoBehaviour
{
    void OnDrawGizmos()
    {
        // Draw a red sphere (point)
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.1f);
    
        // Draw a line
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, new Vector3(3, 3, 0));
    }
}
