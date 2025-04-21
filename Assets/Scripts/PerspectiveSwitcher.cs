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
    public SpawnPointManager spawnPointManager; // The spawn point manager
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

        // Verify that we have a spawn point manager
        if (spawnPointManager == null)
        {
            spawnPointManager = GetComponent<SpawnPointManager>();
            if (spawnPointManager == null)
            {
                Debug.LogError("No SpawnPointManager found in the scene! Please assign one in the inspector.");
            }
        }
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

        // Additional input handling in 2D mode for navigating spawn points
        if (is2D)
        {   
            // Check if 2D character is touching any adjacent spawn points
            CheckSpawnPointTouching();
        }
    }
    
    // Check if the 2D character is touching any adjacent spawn points in 2D screen space
    private void CheckSpawnPointTouching()
    {
        if (currentCharacter2D == null || spawnPointManager == null || spawnPointManager.ActiveSpawnPoint == null)
            return;
            
        // Get the current active spawn point
        SpawnPoint activeSpawnPoint = spawnPointManager.ActiveSpawnPoint;
        
        // Get the character's collision box (using reflection since we don't have direct access to ThresholdBasedMovement's internal variables)
        Vector2 characterPosition = currentCharacter2D.transform.position;
        
        // Get the character's collision box size and offset (using default values if not accessible)
        Vector2 collisionBoxSize = new Vector2(0.5f, 0.8f); // Default size from ThresholdBasedMovement
        Vector2 collisionBoxOffset = Vector2.zero; // Default offset from ThresholdBasedMovement
        
        // Try to get the actual values using reflection
        System.Reflection.FieldInfo sizeField = currentCharacter2D.GetType().GetField("collisionBoxSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo offsetField = currentCharacter2D.GetType().GetField("collisionBoxOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (sizeField != null) collisionBoxSize = (Vector2)sizeField.GetValue(currentCharacter2D);
        if (offsetField != null) collisionBoxOffset = (Vector2)offsetField.GetValue(currentCharacter2D);
        
        // Calculate the character's AABB in 2D world space
        Vector2 boxCenter = characterPosition + collisionBoxOffset;
        Vector2 halfSize = collisionBoxSize * 0.5f;
        Rect characterBox = new Rect(boxCenter - halfSize, collisionBoxSize);
        
        // Check if touching previous spawn point
        if (spawnPointManager.HasPreviousSpawnPoint())
        {
            SpawnPoint prevPoint = activeSpawnPoint.Previous;
            if (IsCharacterTouchingSpawnPoint(characterBox, prevPoint))
            {
                // Player is touching the previous spawn point - set it as active
                Debug.Log("Player touched previous spawn point: " + prevPoint.name);
                spawnPointManager.SetActiveSpawnPoint(prevPoint);
            }
        }
        
        // Check if touching next spawn point
        if (spawnPointManager.HasNextSpawnPoint())
        {
            SpawnPoint nextPoint = activeSpawnPoint.Next;
            if (IsCharacterTouchingSpawnPoint(characterBox, nextPoint))
            {
                // Player is touching the next spawn point - set it as active
                Debug.Log("Player touched next spawn point: " + nextPoint.name);
                spawnPointManager.SetActiveSpawnPoint(nextPoint);
            }
        }
    }
    
    // Check if the character's 2D AABB is touching a spawn point in 2D screen space
    private bool IsCharacterTouchingSpawnPoint(Rect characterBox, SpawnPoint spawnPoint)
    {
        if (spawnPoint == null || camera2D == null)
            return false;
            
        // Calculate spawn point position in 2D screen space
        Vector3 spawnPoint3DPos = spawnPoint.transform.position;
        Vector3 spawnPointScreenPos = mainCamera.WorldToScreenPoint(spawnPoint3DPos);
        
        // Convert screen space to 2D world space
        Vector2 screenOffset = new Vector2(
            spawnPointScreenPos.x - Screen.width * 0.5f,
            spawnPointScreenPos.y - Screen.height * 0.5f
        );
        
        float screenToWorldScale = camera2D.orthographicSize * 2 / Screen.height;
        Vector3 worldOffset = new Vector3(
            screenOffset.x * screenToWorldScale,
            screenOffset.y * screenToWorldScale,
            0
        );
        
        // Calculate spawn point position in 2D world space
        Vector2 spawnPointIn2D = (Vector2)spcace2D.transform.position + (Vector2)worldOffset;
        
        // Create a small AABB around the spawn point position for touch detection
        float touchRadius = 0.5f; // Adjust this value based on your needs
        Rect spawnPointBox = new Rect(spawnPointIn2D - new Vector2(touchRadius, touchRadius), 
                                      new Vector2(touchRadius * 2, touchRadius * 2));
        
        // Check for AABB overlap
        bool isTouching = characterBox.Overlaps(spawnPointBox);
        
        // Draw debug visualization if enabled
        if (debugMode)
        {
            Debug.DrawLine(new Vector3(spawnPointBox.xMin, spawnPointBox.yMin, 0), 
                           new Vector3(spawnPointBox.xMax, spawnPointBox.yMin, 0), 
                           isTouching ? Color.green : Color.yellow, 0.1f);
            Debug.DrawLine(new Vector3(spawnPointBox.xMax, spawnPointBox.yMin, 0), 
                           new Vector3(spawnPointBox.xMax, spawnPointBox.yMax, 0), 
                           isTouching ? Color.green : Color.yellow, 0.1f);
            Debug.DrawLine(new Vector3(spawnPointBox.xMax, spawnPointBox.yMax, 0), 
                           new Vector3(spawnPointBox.xMin, spawnPointBox.yMax, 0), 
                           isTouching ? Color.green : Color.yellow, 0.1f);
            Debug.DrawLine(new Vector3(spawnPointBox.xMin, spawnPointBox.yMax, 0), 
                           new Vector3(spawnPointBox.xMin, spawnPointBox.yMin, 0), 
                           isTouching ? Color.green : Color.yellow, 0.1f);
        }
        
        return isTouching;
    }

    bool CanSwitchTo2D()
    {
        // Ensure we have an active spawn point
        if (spawnPointManager == null || spawnPointManager.ActiveSpawnPoint == null)
        {
            Debug.LogWarning("No active spawn point available! Cannot switch to 2D view.");
            return false;
        }

        SpawnPoint activeSpawnPoint = spawnPointManager.ActiveSpawnPoint;
        
        // 1. Check if spawn point is within view frustum
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(activeSpawnPoint.transform.position);
        bool isInView = screenPoint.x > 0 && screenPoint.x < Screen.width &&
                        screenPoint.y > 0 && screenPoint.y < Screen.height &&
                        screenPoint.z > 0; // Make sure it's in front of camera
        
        if (!isInView)
        {
            if (debugMode) Debug.Log("Spawn point is not in camera view frustum!");
            return false;
        }

        // 2. Check for direct line of sight
        Vector3 directionToSpawnPoint = activeSpawnPoint.transform.position - mainCamera.transform.position;
        float distanceToSpawnPoint = directionToSpawnPoint.magnitude;

        // Cast ray to check for obstacles
        // bool hitObstacle = Physics.Raycast(
        //     mainCamera.transform.position,
        //     directionToSpawnPoint.normalized,
        //     out RaycastHit hitInfo,
        //     distanceToSpawnPoint,
        //     obstacleLayerMask
        // );

        // // Debug visualization
        // if (debugMode)
        // {
        //     if (hitObstacle)
        //     {
        //         Debug.DrawLine(mainCamera.transform.position, hitInfo.point, Color.red, 2f);
        //         Debug.Log("Line of sight blocked by: " + hitInfo.collider.name);
        //     }
        //     else
        //     {
        //         Debug.DrawLine(mainCamera.transform.position, activeSpawnPoint.transform.position, Color.green, 2f);
        //     }
        // }

        // // If we hit something that's not the spawn point, our line of sight is blocked
        // return !hitObstacle;
        return true;
    }

    void SwitchTo2D()
    {
        // Check if we can switch to 2D based on spawn point visibility
        if (!CanSwitchTo2D())
        {
            Debug.LogWarning("Cannot switch to 2D: active spawn point not visible!");
            return;
        }

        // First make 2D view active
        thresholdMapCanvasImage.SetActive(true);
        
        // Disable first person controller while keeping input system enabled
        controller.enabled = false;
        
        // Spawn the 2D character using the active spawn point
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
        // Ensure we have an active spawn point
        if (spawnPointManager == null || spawnPointManager.ActiveSpawnPoint == null)
        {
            Debug.LogError("No active spawn point available! Cannot spawn 2D character.");
            return;
        }

        // Destroy any existing character first
        if (currentCharacter2D != null)
        {
            Destroy(currentCharacter2D.gameObject);
        }

        SpawnPoint activeSpawnPoint = spawnPointManager.ActiveSpawnPoint;
        
        // Calculate the world position from the active spawn point
        Vector3 spawnPointScreenPos = mainCamera.WorldToScreenPoint(activeSpawnPoint.transform.position);
        
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

    // Switch between 2D and 3D modes
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

    // Move to the next spawn point (if available)
    void MoveToNextSpawnPoint()
    {
        if (spawnPointManager != null && spawnPointManager.HasNextSpawnPoint())
        {
            if (spawnPointManager.AdvanceToNextSpawnPoint())
            {
                // Respawn the 2D character at the new active spawn point
                if (is2D)
                {
                    SpawnCharacter2D();
                }
                
                if (debugMode)
                {
                    Debug.Log("Moved to next spawn point: " + spawnPointManager.ActiveSpawnPoint.name);
                }
            }
        }
        else if (debugMode)
        {
            Debug.Log("No next spawn point available!");
        }
    }

    // Move to the previous spawn point (if available)
    void MoveToPreviousSpawnPoint()
    {
        if (spawnPointManager != null && spawnPointManager.HasPreviousSpawnPoint())
        {
            if (spawnPointManager.ReverseToPreviousSpawnPoint())
            {
                // Respawn the 2D character at the new active spawn point
                if (is2D)
                {
                    SpawnCharacter2D();
                }
                
                if (debugMode)
                {
                    Debug.Log("Moved to previous spawn point: " + spawnPointManager.ActiveSpawnPoint.name);
                }
            }
        }
        else if (debugMode)
        {
            Debug.Log("No previous spawn point available!");
        }
    }

    // Input action binding for switching between 2D and 3D
    public void OnSwitch(InputValue value)
    {
        Switch();
    }

    // You can add methods for direct input bindings for next/previous spawn points
    public void OnNextSpawnPoint(InputValue value)
    {
        if (is2D)
        {
            MoveToNextSpawnPoint();
        }
    }

    public void OnPreviousSpawnPoint(InputValue value)
    {
        if (is2D)
        {
            MoveToPreviousSpawnPoint();
        }
    }
}