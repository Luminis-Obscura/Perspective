using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    [Tooltip("The first spawn point in the sequence")]
    [SerializeField] private SpawnPoint root;
    
    [Tooltip("The currently active spawn point")]
    private SpawnPoint activeSpawnPoint;

    // Events that can be subscribed to
    public delegate void SpawnPointChanged(SpawnPoint newSpawnPoint);
    public event SpawnPointChanged OnActiveSpawnPointChanged;

    private void Awake()
    {
        InitializeSpawnPointLinks();
    }

    private void Start()
    {
        // Set the first spawn point as active by default
        if (root != null)
        {
            SetActiveSpawnPoint(root);
        }
        else
        {
            Debug.LogWarning("SpawnPointManager: No root spawn point set!");
        }
    }

    // Initialize the prev references for all spawn points
    private void InitializeSpawnPointLinks()
    {
        if (root == null)
        {
            Debug.LogError("Root spawn point not set in SpawnPointManager!");
            return;
        }

        // Set previous to null for the root
        root.SetPreviousSpawnPoint(null);
        // Set the manager reference for the root
        root.SetManager(this);

        // Iterate through all spawn points and set their prev references
        SpawnPoint current = root;
        SpawnPoint next = current.Next;

        while (next != null)
        {
            // Set the previous reference for the next spawn point
            next.SetPreviousSpawnPoint(current);
            // Set the manager reference
            next.SetManager(this);
            
            // Move to the next spawn point
            current = next;
            next = current.Next;
        }

        // Validate the linked list
        ValidateSpawnPointList();
    }

    // Validate that the linked list is properly set up
    private void ValidateSpawnPointList()
    {
        // Check for cycles in the linked list
        HashSet<SpawnPoint> visited = new HashSet<SpawnPoint>();
        SpawnPoint current = root;

        while (current != null)
        {
            if (visited.Contains(current))
            {
                Debug.LogError("Cycle detected in spawn point linked list! Check your references.");
                break;
            }
            
            visited.Add(current);
            current = current.Next;
        }
    }

    // Public property to get the current active spawn point
    public SpawnPoint ActiveSpawnPoint => activeSpawnPoint;

    // Set a specific spawn point as active
    public void SetActiveSpawnPoint(SpawnPoint spawnPoint)
    {
        if (spawnPoint != activeSpawnPoint)
        {
            activeSpawnPoint = spawnPoint;
            OnActiveSpawnPointChanged?.Invoke(activeSpawnPoint);
        }
    }

    // Advance to the next spawn point
    public bool AdvanceToNextSpawnPoint()
    {
        if (activeSpawnPoint == null)
        {
            Debug.LogWarning("Cannot advance: No active spawn point set!");
            return false;
        }

        if (activeSpawnPoint.Next == null)
        {
            Debug.Log("Cannot advance: Already at the last spawn point!");
            return false;
        }

        SetActiveSpawnPoint(activeSpawnPoint.Next);
        return true;
    }

    // Reverse to the previous spawn point
    public bool ReverseToPreviousSpawnPoint()
    {
        if (activeSpawnPoint == null)
        {
            Debug.LogWarning("Cannot reverse: No active spawn point set!");
            return false;
        }

        if (activeSpawnPoint.Previous == null)
        {
            Debug.Log("Cannot reverse: Already at the first spawn point!");
            return false;
        }

        SetActiveSpawnPoint(activeSpawnPoint.Previous);
        return true;
    }

    // Check if there is a next spawn point
    public bool HasNextSpawnPoint()
    {
        return activeSpawnPoint != null && activeSpawnPoint.Next != null;
    }

    // Check if there is a previous spawn point
    public bool HasPreviousSpawnPoint()
    {
        return activeSpawnPoint != null && activeSpawnPoint.Previous != null;
    }

    // Get all spawn points as a list (useful for debugging or UI purposes)
    public List<SpawnPoint> GetAllSpawnPoints()
    {
        List<SpawnPoint> allSpawnPoints = new List<SpawnPoint>();
        SpawnPoint current = root;

        while (current != null)
        {
            allSpawnPoints.Add(current);
            current = current.Next;
        }

        return allSpawnPoints;
    }

    // For debugging purposes
    void OnDrawGizmos()
    {
        if (root == null) return;

        // Draw all connections in the editor even when not playing
        SpawnPoint current = root;
        while (current != null)
        {
            if (current.Next != null)
            {
                // This is just to make the manager-drawn lines different from the individual SpawnPoint gizmos
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(current.transform.position + Vector3.up * 0.1f, 
                               current.Next.transform.position + Vector3.up * 0.1f);
            }
            current = current.Next;
        }
    }
}