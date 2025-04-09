using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PerspectiveSwitcher : MonoBehaviour
{
    [Header("References")]
    public GameObject thresholdMapCanvasImage;
    public FirstPersonController controller;
    public StarterAssetsInputs inputs;
    public Camera mainCamera;
    
    [Header("2D Character Settings")]
    // The space the character should be spawned in
    public GameObject spcace2D;
    public GameObject character2DPrefab; // Prefab of the 2D character
    public Camera camera2D;
    private bool is2D = false;
    private ThresholdBasedMovement currentCharacter2D; // Reference to the currently spawned character
    
    [Header("Spawn Point Settings")]
    public GameObject spawnPoint; // The spawn point object
    public float maxViewDistance = 20f; // Maximum distance to check for line of sight
    public LayerMask obstacleLayerMask; // Layers that block line of sight
    public bool debugMode = false; // To enable debug visualization
    
    [Header("Out of View Settings")]
    public float checkInterval = 0.5f; // How often to check if player is out of view
    private float nextCheckTime = 0f;
    
    void Start()
    {
        // Make sure we start in 3D mode
        if (mainCamera == null) mainCamera = Camera.main;
        SwitchTo3D();
    }
    
    void Update()
    {
        // If in 2D mode, periodically check if player has fallen out of view
        if (is2D && Time.time > nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            
            if (currentCharacter2D != null && currentCharacter2D.IsOutOfCameraView())
            {
                Debug.Log("Player has fallen out of view! Switching back to 3D.");
                SwitchTo3D();
            }
        }
    }
    
    bool CanSwitchTo2D()
    {
        // Check if spawn point exists
        if (spawnPoint == null)
        {
            Debug.LogWarning("No spawn point assigned! Cannot switch to 2D view.");
            return false;
        }
        
        // 1. Check if spawn point is within view frustum
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(spawnPoint.transform.position);
        bool isInView = screenPoint.x > 0 && screenPoint.x < Screen.width &&
                         screenPoint.y > 0 && screenPoint.y < Screen.height &&
                         screenPoint.z > 0; // Make sure it's in front of camera
        
        if (!isInView)
        {
            if (debugMode) Debug.Log("Spawn point is not in camera view frustum!");
            return false;
        }
        
        // 2. Check for direct line of sight
        Vector3 directionToSpawnPoint = spawnPoint.transform.position - mainCamera.transform.position;
        float distanceToSpawnPoint = directionToSpawnPoint.magnitude;
        
        if (distanceToSpawnPoint > maxViewDistance)
        {
            if (debugMode) Debug.Log("Spawn point is too far away!");
            return false;
        }
        
        // Cast ray to check for obstacles
        bool hitObstacle = Physics.Raycast(
            mainCamera.transform.position, 
            directionToSpawnPoint.normalized, 
            out RaycastHit hitInfo,
            distanceToSpawnPoint,
            obstacleLayerMask
        );
        
        // Debug visualization
        if (debugMode)
        {
            if (hitObstacle)
            {
                Debug.DrawLine(mainCamera.transform.position, hitInfo.point, Color.red, 2f);
                Debug.Log("Line of sight blocked by: " + hitInfo.collider.name);
            }
            else
            {
                Debug.DrawLine(mainCamera.transform.position, spawnPoint.transform.position, Color.green, 2f);
            }
        }
        
        // If we hit something that's not the spawn point, our line of sight is blocked
        return !hitObstacle;
    }
    
    void SwitchTo2D()
    {
        // Check if we can switch to 2D based on spawn point visibility
        // if (!CanSwitchTo2D())
        // {
        //     Debug.LogWarning("Cannot switch to 2D: spawn point not visible!");
        //     return;
        // }
        
        // First make 2D view active
        thresholdMapCanvasImage.SetActive(true);
        
        // Disable first person controller while keeping input system enabled
        controller.enabled = false;
        
        // Spawn the 2D character using the spawn point
        SpawnCharacter2D();
        
        is2D = true;
    }
    
    void SwitchTo3D()
    {
        thresholdMapCanvasImage.SetActive(false);
        controller.enabled = true;
        
        // Destroy the 2D character if it exists
        if (currentCharacter2D != null)
        {
            Destroy(currentCharacter2D.gameObject);
            currentCharacter2D = null;
        }
        
        is2D = false;
    }
    
    void SpawnCharacter2D()
    {
        // Destroy any existing character first
        if (currentCharacter2D != null)
        {
            Destroy(currentCharacter2D.gameObject);
        }
        
        // Calculate the world position from the spawn point
        Vector3 spawnPointScreenPos = mainCamera.WorldToScreenPoint(spawnPoint.transform.position);
        
        // Map spawn point to 2D space
        Vector3 characterPosition;
        
        // Calculate the offset from the center of the screen to the spawn point in screen space
        Vector2 screenOffset = new Vector2(
            spawnPointScreenPos.x - Screen.width * 0.5f,
            spawnPointScreenPos.y - Screen.height * 0.5f
        );
        
        // Convert screen space offset to world space offset
        // Need to determine a scale factor to map screen pixels to world units
        // This depends on your specific camera setup
        float screenToWorldScale = camera2D.orthographicSize * 2 / Screen.height;
        Vector3 worldOffset = new Vector3(
            screenOffset.x * screenToWorldScale,
            screenOffset.y * screenToWorldScale,
            0
        );
        
        // Apply the offset to the 2D space
        characterPosition = spcace2D.transform.position + worldOffset;
        characterPosition.z = 0; // Ensure Z is 0 for 2D space
        
        // Instantiate the prefab at the calculated position
        GameObject characterObj = Instantiate(character2DPrefab);
        characterObj.SetActive(true);
        
        // Get the Character2D component
        currentCharacter2D = characterObj.GetComponent<ThresholdBasedMovement>();
        
        // If the character doesn't have the script, add it
        if (currentCharacter2D == null)
        {
            Debug.LogWarning("Character2D prefab is missing the ThresholdBasedMovement component! Adding it now.");
            currentCharacter2D = characterObj.AddComponent<ThresholdBasedMovement>();
        }
        
        // Set ThresholdMapGenerator reference if needed
        // if (currentCharacter2D.gameCamera == null)
        // {
        //     currentCharacter2D.gameCamera = camera2D;
        // }
        
        // Parent to the 2D view for organization
        characterObj.transform.parent = spcace2D.transform;
        characterObj.transform.localPosition = worldOffset; // Use local position relative to space2D
        
        if (debugMode)
        {
            Debug.Log("Spawned 2D character at: " + characterObj.transform.position);
            Debug.Log("Spawn point screen position: " + spawnPointScreenPos);
            Debug.Log("Screen offset: " + screenOffset);
            Debug.Log("World offset: " + worldOffset);
        }
    }
    
    void Switch() 
    {
        if (is2D) 
        {
            SwitchTo3D();
        } 
        else 
        {
            SwitchTo2D();
        }
    }
    
    public void OnSwitch(InputValue value) 
    {
        Switch();
    }
}