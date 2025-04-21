using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("The next spawn point in the sequence. Leave null if this is the last spawn point.")]
    [SerializeField] private SpawnPoint nextSpawnPoint;

    // This reference is set up at runtime by the SpawnPointManager
    private SpawnPoint prevSpawnPoint;
    
    // Reference to the manager, set during initialization
    private SpawnPointManager manager;
    
    // Flag to track if this is the active spawn point
    private bool isActive = false;

    // Public properties for external access
    public SpawnPoint Next => nextSpawnPoint;
    public SpawnPoint Previous => prevSpawnPoint;
    public bool IsActive => isActive;

    // Method to set the previous spawn point (called by the manager during initialization)
    public void SetPreviousSpawnPoint(SpawnPoint prev)
    {
        prevSpawnPoint = prev;
    }
    
    // Method to set the manager reference
    public void SetManager(SpawnPointManager mgr)
    {
        manager = mgr;
        
        // Subscribe to the manager's active spawn point changed event
        if (manager != null)
        {
            manager.OnActiveSpawnPointChanged += HandleActiveSpawnPointChanged;
        }
    }
    
    // Event handler for when the active spawn point changes
    private void HandleActiveSpawnPointChanged(SpawnPoint newActive)
    {
        // Update our active status
        isActive = (newActive == this);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (manager != null)
        {
            manager.OnActiveSpawnPointChanged -= HandleActiveSpawnPointChanged;
        }
    }

    // Helper method to check if this is the first spawn point
    public bool IsFirst()
    {
        return prevSpawnPoint == null;
    }

    // Helper method to check if this is the last spawn point
    public bool IsLast()
    {
        return nextSpawnPoint == null;
    }

    // For debugging purposes
    void OnDrawGizmos()
    {
        // Choose color based on active status - red if active, blue otherwise
        Gizmos.color = isActive ? Color.red : Color.blue;
        Gizmos.DrawSphere(transform.position, 0.5f);

        // Draw an arrow to the next spawn point, if there is one
        if (nextSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            // Vector3 direction = nextSpawnPoint.transform.position - transform.position;
            Gizmos.DrawLine(transform.position, nextSpawnPoint.transform.position);
            // Draw arrow head
            // Vector3 arrowPos = transform.position + direction * 0.8f;
            // Gizmos.DrawRay(arrowPos, Quaternion.Euler(0, 30, 0) * direction.normalized * direction.magnitude * 0.2f);
            // Gizmos.DrawRay(arrowPos, Quaternion.Euler(0, -30, 0) * direction.normalized * direction.magnitude * 0.2f);
        }
    }
    
    // For runtime gizmos when the game is playing
    void OnDrawGizmosSelected()
    {
        // Draw a larger sphere when selected in the editor
        Gizmos.color = isActive ? new Color(1, 0, 0, 0.5f) : new Color(0, 0, 1, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.7f);
    }
}