using UnityEngine;

/// <summary>
/// Placeholder class for MovableDetector - needs implementation
/// </summary>
public class MovableDetector : MonoBehaviour
{
    public static MovableDetector Instance { get; private set; }
    
    public Vector3 placePosition;
    public Vector3 normal;
    public bool isPlaceable;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}