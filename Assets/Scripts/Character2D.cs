using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class Character2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float gravity = 20f;
    
    [Header("Ground Detection")]
    public Transform groundDetector; // Empty game object to place at the character's feet
    
    [Header("Behavior Settings")]
    public bool preventEdgeDropping = true; // Toggle to prevent falling off edges
    public bool ignoreCollisions = false; // Toggle to ignore texture collisions but still respect edge rules
    
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    
    private float verticalVelocity;
    private bool isGrounded;
    private ThresholdMapGenerator thresholdMap;
    private Texture2D collisionMap; // Stored collision map
    private StarterAssetsInputs _input; // Reference to the same input used in 3D
    private Camera mainCamera;
    private Vector3 initialPosition;
    private float screenMinY; // Lower screen bound
    
    void Awake()
    {
        thresholdMap = ThresholdMapGenerator.Instance;
        mainCamera = Camera.main;
        
        // Create ground detector if none exists
        if (groundDetector == null)
        {
            GameObject detector = new GameObject("GroundDetector");
            detector.transform.parent = transform;
            detector.transform.localPosition = new Vector3(0, -0.5f, 0); // Position at feet
            groundDetector = detector.transform;
        }
    }
    
    void Start()
    {
        // Generate and save the collision map once
        GenerateCollisionMap();
        
        // Get reference to input system
        _input = FindFirstObjectByType<StarterAssetsInputs>();
        if (_input == null)
        {
            Debug.LogError("Cannot find StarterAssetsInputs in the scene!");
        }
        
        // Store initial position for screen bounds reference
        initialPosition = transform.position;
        screenMinY = initialPosition.y - 5f; // Set minimum Y position (5 units below start)
    }
    
    void OnEnable()
    {
        // Reset position when enabled (when switching to 2D)
        ResetPosition();
        
        // Always regenerate the map when the character is enabled
        GenerateCollisionMap();
    }
    
    void GenerateCollisionMap()
    {
        if (thresholdMap != null)
        {
            // Generate the map once and save it
            collisionMap = thresholdMap.FastGenerateThresholdMap();
            Debug.Log("Generated collision map: " + collisionMap.width + "x" + collisionMap.height);
        }
        else
        {
            Debug.LogError("ThresholdMapGenerator instance not found!");
        }
    }
    
    void Update()
    {
        // Check ground collision by sampling the texture
        CheckGrounded();
        
        // Apply movement and physics
        HandleMovement();
        
        // Make sure character always faces the camera
        FaceCamera();
        
        // Strictly enforce screen bounds
        EnforceScreenBounds();
    }
    
    void CheckGrounded()
    {
        if (collisionMap == null || groundDetector == null)
        {
            isGrounded = false;
            return;
        }
        
        // Ignore collision check if toggle is on, but still respect edge rule
        if (ignoreCollisions)
        {
            isGrounded = true;
            return;
        }
        
        // Convert the ground detector position to screen coordinates
        Vector2 screenPos = mainCamera.WorldToScreenPoint(groundDetector.position);
        
        // Convert screen coordinates to texture coordinates
        int texX = Mathf.Clamp(Mathf.RoundToInt(screenPos.x), 0, collisionMap.width - 1);
        int texY = Mathf.Clamp(Mathf.RoundToInt(screenPos.y), 0, collisionMap.height - 1);
        
        // Sample the texture at the detection point
        Color pixelColor = collisionMap.GetPixel(texX, texY);
        
        // Black pixels (r,g,b values close to 0) are solid ground
        isGrounded = pixelColor.r < 0.1f && pixelColor.g < 0.1f && pixelColor.b < 0.1f;
        
        // Check for edge prevention if enabled
        if (preventEdgeDropping && isGrounded)
        {
            // Sample a bit ahead in the movement direction to detect edges
            float lookAheadDistance = 10f; // Pixels to look ahead
            Vector2 lookDirection = new Vector2(_input.move.x, 0).normalized;
            
            if (lookDirection.x != 0)
            {
                int edgeCheckX = Mathf.Clamp(Mathf.RoundToInt(screenPos.x + lookDirection.x * lookAheadDistance), 0, collisionMap.width - 1);
                int edgeCheckY = Mathf.Clamp(Mathf.RoundToInt(screenPos.y - 5), 0, collisionMap.height - 1); // Check slightly below
                
                Color edgePixel = collisionMap.GetPixel(edgeCheckX, edgeCheckY);
                bool isEdge = edgePixel.r > 0.1f || edgePixel.g > 0.1f || edgePixel.b > 0.1f; // White is air/edge
                
                if (isEdge && _input.move.x * lookDirection.x > 0)
                {
                    // Restrict movement in that direction
                    _input.move.x = 0;
                }
            }
        }
        
        // Visual debug - draw ray to show ground detection
        Debug.DrawRay(groundDetector.position, Vector3.down * 0.1f, isGrounded ? Color.green : Color.red);
    }
    
    void HandleMovement()
    {
        // Apply gravity if not grounded
        if (!isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            // Stop falling when grounded
            verticalVelocity = 0;
        }
        
        // Handle jump input
        if (isGrounded && _input.jump)
        {
            verticalVelocity = jumpForce;
            _input.jump = false; // Reset the jump input
        }
        
        // Calculate movement
        Vector3 position = transform.position;
        position.x += _input.move.x * moveSpeed * Time.deltaTime;
        position.y += verticalVelocity * Time.deltaTime;
        
        // Update position
        transform.position = position;
    }
    
    void EnforceScreenBounds()
    {
        if (mainCamera == null) return;
        
        // Get current screen bounds in world space
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z - mainCamera.transform.position.z));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, transform.position.z - mainCamera.transform.position.z));
        
        // Get the character's current position
        Vector3 pos = transform.position;
        
        // Get sprite width/height for offset calculation
        float spriteWidth = 0;
        float spriteHeight = 0;
        
        if (spriteRenderer != null)
        {
            spriteWidth = spriteRenderer.bounds.size.x / 2;
            spriteHeight = spriteRenderer.bounds.size.y / 2;
        }
        
        // Enforce horizontal bounds (with small margin for sprite width)
        pos.x = Mathf.Clamp(pos.x, bottomLeft.x + spriteWidth, topRight.x - spriteWidth);
        
        // Enforce bottom bound (never fall below bottom of screen)
        // Only clamp the bottom - we want to allow jumping
        pos.y = Mathf.Max(pos.y, bottomLeft.y + spriteHeight);
        
        // Apply the clamped position
        transform.position = pos;
        
        // If we hit the bottom of the screen, stop falling
        if (transform.position.y <= bottomLeft.y + spriteHeight + 0.01f)
        {
            verticalVelocity = 0;
            isGrounded = true;
        }
    }
    
    void FaceCamera()
    {
        if (mainCamera == null || spriteRenderer == null) return;
        
        // Get the camera's right vector in world space 
        // This is consistent even when the camera rotates
        Vector3 cameraRight = mainCamera.transform.right;
        
        // Determine sprite direction based on camera right vector
        // If camera right points in positive X, we don't flip
        // If camera right points in negative X, we flip
        // This ensures character always faces the way the camera is oriented
        spriteRenderer.flipX = cameraRight.x < 0;
    }
    
    public void ResetPosition()
    {
        // Reset to center of screen
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 10f);
        initialPosition = mainCamera.ScreenToWorldPoint(screenCenter);
        initialPosition.z = 0;
        transform.position = initialPosition;
        verticalVelocity = 0;
    }
}