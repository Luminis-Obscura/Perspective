using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(SpriteRenderer))]
public class ThresholdBasedMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float gravityForce = 9.8f;
    [SerializeField] private Vector2 collisionPointOffset = new Vector2(0, -0.5f); // Adjustable feet position
    
    [Header("Collision Box Settings")]
    [SerializeField] private Vector2 collisionBoxSize = new Vector2(0.5f, 0.8f); // Width and height of collision box
    [SerializeField] private Vector2 collisionBoxOffset = new Vector2(0, 0); // Offset from sprite center for the collision box
    
    [Header("Camera Settings")]
    [SerializeField] private Camera gameCamera;
    
    private SpriteRenderer spriteRenderer;
    private Vector2 velocity;
    private bool isGrounded;
    private Color defaultSpriteColor;
    private Vector2 collisionPoint;
    private ThresholdMapGenerator thresholdGenerator;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultSpriteColor = Color.white;
        
        // Find the camera if not set
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                Debug.LogError("No camera found! Please assign a camera in the inspector.");
            }
        }
        
        // Debug info
        Debug.Log("Movement script initialized. Jump key is: " + 
                 #if UNITY_EDITOR
                 Keyboard.current.spaceKey.displayName
                 #else
                 "Space or Button 0"
                 #endif
                 );
    }
    
    private void Start()
    {
        // Get reference to the ThresholdMapGenerator singleton
        thresholdGenerator = ThresholdMapGenerator.Instance;
        if (thresholdGenerator == null)
        {
            Debug.LogError("No ThresholdMapGenerator instance found in the scene!");
        }
    }
    
    [System.Flags]
    private enum CollisionDirection
    {
        None = 0,
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8
    }
    
    private CollisionDirection collisionDirections;
    
    private void Update()
    {
        // Calculate the current collision point and box center
        collisionPoint = (Vector2)transform.position + collisionPointOffset;
        Vector2 boxCenter = (Vector2)transform.position + collisionBoxOffset;
        
        // Get input
        float horizontalInput = Input.GetAxis("Horizontal");
        
        // Handle jumping
        if (Input.GetButtonDown("Jump"))
        {
            Debug.Log("Jump button pressed! isGrounded=" + isGrounded);
            
            if (isGrounded)
            {
                velocity.y = jumpForce;
                isGrounded = false;
                Debug.Log("Jump initiated! Velocity: " + velocity.y);
            }
        }
        
        // Calculate movement
        velocity.x = horizontalInput * movementSpeed;
        
        // Apply gravity
        velocity.y -= gravityForce * Time.deltaTime;
        
        // Reset collision flags
        collisionDirections = CollisionDirection.None;
        
        // Reset sprite color each frame
        spriteRenderer.color = defaultSpriteColor;
        
        // Perform movement with collision detection
        MoveWithCollisions();
        
        // Update sprite color based on collision
        if (collisionDirections != CollisionDirection.None)
        {
            spriteRenderer.color = Color.red;
        }
        
        // Ensure isGrounded is properly set for camera bounds
        CheckIfGrounded();
    }
    
    private void MoveWithCollisions()
    {
        Vector2 originalPosition = transform.position;
        
        // Calculate the full movement vector
        Vector2 movement = velocity * Time.deltaTime;
        Vector2 targetPosition = originalPosition + movement;
        
        // Get current threshold map
        Texture2D thresholdMap = thresholdGenerator.FastGenerateThresholdMap();
        
        // Check camera bounds for the full movement
        Vector2 adjustedPosition = targetPosition;
        bool inCameraBounds = CheckCameraBounds(ref adjustedPosition);
        
        // Calculate collision box at the target position
        Vector2 boxCenter = adjustedPosition + collisionBoxOffset;
        Vector2 halfSize = collisionBoxSize * 0.5f;
        Rect targetCollisionBox = new Rect(boxCenter - halfSize, collisionBoxSize);
        
        // Check precise collision directions against threshold map
        CollisionDirection hitDirections = GetCollisionDirections(targetCollisionBox, thresholdMap);
        
        // Store collision directions for rendering changes
        collisionDirections = hitDirections;
        
        if (hitDirections != CollisionDirection.None)
        {
            // Handle horizontal collisions
            if ((hitDirections & CollisionDirection.Left) != 0 && velocity.x < 0)
            {
                // Left collision while moving left
                velocity.x = 0;
                Debug.Log("Hit left wall");
            }
            
            if ((hitDirections & CollisionDirection.Right) != 0 && velocity.x > 0)
            {
                // Right collision while moving right
                velocity.x = 0;
                Debug.Log("Hit right wall");
            }
            
            // Handle vertical collisions
            if ((hitDirections & CollisionDirection.Top) != 0 && velocity.y > 0)
            {
                // Top collision while moving up (ceiling)
                velocity.y = 0;
                Debug.Log("Hit ceiling");
            }
            
            if ((hitDirections & CollisionDirection.Bottom) != 0 && velocity.y < 0)
            {
                // Bottom collision while moving down (ground)
                velocity.y = 0;
                isGrounded = true;
                Debug.Log("Hit ground from threshold map");
            }
            
            // Recalculate movement with corrected velocity
            movement = velocity * Time.deltaTime;
            targetPosition = originalPosition + movement;
            
            // Try to slide along surfaces when hitting walls
            if ((hitDirections & (CollisionDirection.Left | CollisionDirection.Right)) != 0 && 
                (hitDirections & (CollisionDirection.Top | CollisionDirection.Bottom)) == 0)
            {
                // Hit wall but not ceiling/floor, try vertical movement
                targetPosition = originalPosition + new Vector2(0, movement.y);
            }
            else if ((hitDirections & (CollisionDirection.Top | CollisionDirection.Bottom)) != 0 && 
                     (hitDirections & (CollisionDirection.Left | CollisionDirection.Right)) == 0)
            {
                // Hit ceiling/floor but not walls, try horizontal movement
                targetPosition = originalPosition + new Vector2(movement.x, 0);
            }
        }
        
        // Final camera bounds check
        inCameraBounds = CheckCameraBounds(ref targetPosition);
        
        if (!inCameraBounds)
        {
            // Handle camera bounds collisions if needed
            HandleCameraBoundsCollisions(ref targetPosition);
        }
        
        // Apply final position
        transform.position = targetPosition;
    }
    
    private bool CheckThresholdCollision(Vector2 position)
    {
        if (thresholdGenerator == null)
        {
            return false; // No threshold generator available
        }
        
        // Get the current threshold map from generator
        Texture2D thresholdMap = thresholdGenerator.FastGenerateThresholdMap();
        
        // Calculate the collision box based on position
        Vector2 boxCenter = position + collisionBoxOffset - collisionPointOffset; // Adjust for offset difference
        Vector2 halfSize = collisionBoxSize * 0.5f;
        Rect collisionBox = new Rect(boxCenter - halfSize, collisionBoxSize);
        
        // Use AABB collision detection instead of single point
        return AABBCollisionCheck(collisionBox, thresholdMap);
    }
    
    private bool PointCollidesWithThreshold(Vector2 worldPoint, Texture2D thresholdMap)
    {
        // Convert world position to screen position
        Vector3 screenPoint = gameCamera.WorldToScreenPoint(worldPoint);
        
        // Convert to normalized coordinates (0-1) for texture sampling
        float u = screenPoint.x / Screen.width;
        float v = screenPoint.y / Screen.height;
        
        // Handle points that are outside the screen
        if (u < 0 || u > 1 || v < 0 || v > 1)
        {
            return false;
        }
        
        // Convert normalized coordinates to pixel coordinates
        int pixelX = Mathf.RoundToInt(u * thresholdMap.width);
        int pixelY = Mathf.RoundToInt(v * thresholdMap.height);
        
        // Clamp to texture bounds
        pixelX = Mathf.Clamp(pixelX, 0, thresholdMap.width - 1);
        pixelY = Mathf.Clamp(pixelY, 0, thresholdMap.height - 1);
        
        // Sample pixel color
        Color pixelColor = thresholdMap.GetPixel(pixelX, pixelY);
        
        // FIXED: Inverted threshold check - now dark areas (< 0.5) are platforms
        return pixelColor.r < 0.5f;
    }
    
    private bool AABBCollisionCheck(Rect worldSpaceBox, Texture2D thresholdMap)
    {
        // Instead of checking every pixel, we'll check multiple sample points
        // around the collision box to detect collisions more efficiently
        
        // Calculate world space coordinates for sampling points
        Vector2 min = worldSpaceBox.min;
        Vector2 max = worldSpaceBox.max;
        Vector2 center = worldSpaceBox.center;
        
        // Sample points along edges (5 points per edge)
        Vector2[] samplePoints = new Vector2[]
        {
            // Top edge (left to right)
            new Vector2(min.x + worldSpaceBox.width * 0.0f, max.y),
            new Vector2(min.x + worldSpaceBox.width * 0.25f, max.y),
            new Vector2(min.x + worldSpaceBox.width * 0.5f, max.y),
            new Vector2(min.x + worldSpaceBox.width * 0.75f, max.y),
            new Vector2(min.x + worldSpaceBox.width * 1.0f, max.y),
            
            // Right edge (top to bottom)
            new Vector2(max.x, min.y + worldSpaceBox.height * 1.0f),
            new Vector2(max.x, min.y + worldSpaceBox.height * 0.75f),
            new Vector2(max.x, min.y + worldSpaceBox.height * 0.5f),
            new Vector2(max.x, min.y + worldSpaceBox.height * 0.25f),
            new Vector2(max.x, min.y + worldSpaceBox.height * 0.0f),
            
            // Bottom edge (right to left)
            new Vector2(min.x + worldSpaceBox.width * 1.0f, min.y),
            new Vector2(min.x + worldSpaceBox.width * 0.75f, min.y),
            new Vector2(min.x + worldSpaceBox.width * 0.5f, min.y),
            new Vector2(min.x + worldSpaceBox.width * 0.25f, min.y),
            new Vector2(min.x + worldSpaceBox.width * 0.0f, min.y),
            
            // Left edge (bottom to top)
            new Vector2(min.x, min.y + worldSpaceBox.height * 0.0f),
            new Vector2(min.x, min.y + worldSpaceBox.height * 0.25f),
            new Vector2(min.x, min.y + worldSpaceBox.height * 0.5f),
            new Vector2(min.x, min.y + worldSpaceBox.height * 0.75f),
            new Vector2(min.x, min.y + worldSpaceBox.height * 1.0f)
        };
        
        // Check each sample point
        foreach (Vector2 point in samplePoints)
        {
            if (PointCollidesWithThreshold(point, thresholdMap))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private CollisionDirection GetCollisionDirections(Rect worldSpaceBox, Texture2D thresholdMap)
    {
        CollisionDirection result = CollisionDirection.None;
        Vector2 min = worldSpaceBox.min;
        Vector2 max = worldSpaceBox.max;
        Vector2 center = worldSpaceBox.center;
        
        // Get box dimensions
        float width = worldSpaceBox.width;
        float height = worldSpaceBox.height;
        
        // CHANGED: Use fewer sample points for walls, and position them higher up
        // so they don't get confused with ground
        
        // Modified wall detection locations (higher up on the sides)
        // Top edge (unchanged)
        bool topCollision = false;
        for (float t = 0; t <= 1.0f; t += 0.25f)
        {
            Vector2 point = new Vector2(min.x + width * t, max.y);
            if (PointCollidesWithThreshold(point, thresholdMap))
            {
                topCollision = true;
                break;
            }
        }
        
        // Right wall checks (only check top 2/3 of the wall)
        bool rightCollision = false;
        for (float t = 0.33f; t <= 1.0f; t += 0.25f)  // Start from 1/3 up the wall
        {
            Vector2 point = new Vector2(max.x, min.y + height * t);
            if (PointCollidesWithThreshold(point, thresholdMap))
            {
                rightCollision = true;
                break;
            }
        }
        
        // Bottom detection (unchanged)
        bool bottomCollision = false;
        for (float t = 0; t <= 1.0f; t += 0.25f)
        {
            Vector2 point = new Vector2(min.x + width * t, min.y);
            if (PointCollidesWithThreshold(point, thresholdMap))
            {
                bottomCollision = true;
                break;
            }
        }
        
        // Left wall checks (only check top 2/3 of the wall)
        bool leftCollision = false;
        for (float t = 0.33f; t <= 1.0f; t += 0.25f)  // Start from 1/3 up the wall
        {
            Vector2 point = new Vector2(min.x, min.y + height * t);
            if (PointCollidesWithThreshold(point, thresholdMap))
            {
                leftCollision = true;
                break;
            }
        }
        
        // Debug visualization
        if (rightCollision) Debug.DrawLine(new Vector2(max.x, min.y + height * 0.5f), new Vector2(max.x + 0.2f, min.y + height * 0.5f), Color.red);
        if (leftCollision) Debug.DrawLine(new Vector2(min.x, min.y + height * 0.5f), new Vector2(min.x - 0.2f, min.y + height * 0.5f), Color.red);
        if (topCollision) Debug.DrawLine(new Vector2(center.x, max.y), new Vector2(center.x, max.y + 0.2f), Color.red);
        if (bottomCollision) Debug.DrawLine(new Vector2(center.x, min.y), new Vector2(center.x, min.y - 0.2f), Color.red);
        
        if (topCollision) result |= CollisionDirection.Top;
        if (rightCollision) result |= CollisionDirection.Right;
        if (bottomCollision) result |= CollisionDirection.Bottom;
        if (leftCollision) result |= CollisionDirection.Left;
        
        return result;
    }
    
    private bool CheckCameraBounds(ref Vector2 position)
    {
        bool withinBounds = true;
        
        // Calculate the AABB for collision checking
        Vector2 boxCenter = position + collisionBoxOffset;
        Vector2 halfSize = collisionBoxSize * 0.5f;
        Rect collisionBox = new Rect(boxCenter - halfSize, collisionBoxSize);
        
        // Calculate camera bounds in world space
        float cameraHeight = 2f * gameCamera.orthographicSize;
        float cameraWidth = cameraHeight * gameCamera.aspect;
        
        Vector2 cameraPosition = gameCamera.transform.position;
        
        float minX = cameraPosition.x - (cameraWidth / 2);
        float maxX = cameraPosition.x + (cameraWidth / 2);
        float minY = cameraPosition.y - (cameraHeight / 2);
        float maxY = cameraPosition.y + (cameraHeight / 2);
        
        // Create camera bounds rect
        Rect cameraBounds = new Rect(minX, minY, cameraWidth, cameraHeight);
        
        // Check if the collision box extends beyond any camera boundary
        if (collisionBox.xMin < cameraBounds.xMin)
        {
            position.x += (cameraBounds.xMin - collisionBox.xMin);
            withinBounds = false;
        }
        else if (collisionBox.xMax > cameraBounds.xMax)
        {
            position.x -= (collisionBox.xMax - cameraBounds.xMax);
            withinBounds = false;
        }
        
        if (collisionBox.yMin < cameraBounds.yMin)
        {
            position.y += (cameraBounds.yMin - collisionBox.yMin);
            withinBounds = false;
        }
        else if (collisionBox.yMax > cameraBounds.yMax)
        {
            position.y -= (collisionBox.yMax - cameraBounds.yMax);
            withinBounds = false;
        }
        
        return withinBounds;
    }
    
    private void HandleCameraBoundsCollisions(ref Vector2 position)
    {
        // Calculate the AABB for collision checking
        Vector2 boxCenter = position + collisionBoxOffset;
        Vector2 halfSize = collisionBoxSize * 0.5f;
        Rect collisionBox = new Rect(boxCenter - halfSize, collisionBoxSize);
        
        // Calculate camera bounds in world space
        float cameraHeight = 2f * gameCamera.orthographicSize;
        float cameraWidth = cameraHeight * gameCamera.aspect;
        
        Vector2 cameraPosition = gameCamera.transform.position;
        
        float minX = cameraPosition.x - (cameraWidth / 2);
        float maxX = cameraPosition.x + (cameraWidth / 2);
        float minY = cameraPosition.y - (cameraHeight / 2);
        float maxY = cameraPosition.y + (cameraHeight / 2);
        
        // Create camera bounds rect
        Rect cameraBounds = new Rect(minX, minY, cameraWidth, cameraHeight);
        
        // Handle bottom boundary - treat as ground
        if (collisionBox.yMin < cameraBounds.yMin)
        {
            position.y += (cameraBounds.yMin - collisionBox.yMin);
            velocity.y = 0;
            isGrounded = true; // Set grounded when hitting bottom
            collisionDirections |= CollisionDirection.Bottom; // Add bottom collision flag
            Debug.Log("Hit camera ground!");
        }
        
        // Handle top boundary
        if (collisionBox.yMax > cameraBounds.yMax)
        {
            position.y -= (collisionBox.yMax - cameraBounds.yMax);
            velocity.y = 0;
            collisionDirections |= CollisionDirection.Top;
        }
        
        // Handle left boundary
        if (collisionBox.xMin < cameraBounds.xMin)
        {
            position.x += (cameraBounds.xMin - collisionBox.xMin);
            velocity.x = 0;
            collisionDirections |= CollisionDirection.Left;
        }
        
        // Handle right boundary
        if (collisionBox.xMax > cameraBounds.xMax)
        {
            position.x -= (collisionBox.xMax - cameraBounds.xMax);
            velocity.x = 0;
            collisionDirections |= CollisionDirection.Right;
        }
    }
    
    // ENABLED: Full collision handling implementation
    private void HandleCollisions(ref Vector2 position, bool hitThreshold, bool inCameraBounds)
    {
        // Handle threshold collision
        if (hitThreshold)
        {
            // Hit a threshold in the map
            // Allow sliding along surfaces
            TrySlideMovement(ref position);
        }
        
        if (!inCameraBounds)
        {
            HandleCameraBoundsCollisions(ref position);
        }
        
        // Update transform position
        transform.position = position;
    }
    
    private void TrySlideMovement(ref Vector2 position)
    {
        // Try horizontal movement only
        Vector2 horizontalMove = new Vector2(velocity.x * Time.deltaTime, 0);
        Vector2 horizontalPoint = collisionPoint + horizontalMove;
        
        if (!CheckThresholdCollision(horizontalPoint))
        {
            // Can move horizontally
            Vector2 newSpritePosition = horizontalPoint - collisionPointOffset;
            position = newSpritePosition;
            velocity.y = 0; // Stop vertical movement
        }
        else
        {
            // Try vertical movement only
            Vector2 verticalMove = new Vector2(0, velocity.y * Time.deltaTime);
            Vector2 verticalPoint = collisionPoint + verticalMove;
            
            if (!CheckThresholdCollision(verticalPoint))
            {
                // Can move vertically
                Vector2 newSpritePosition = verticalPoint - collisionPointOffset;
                position = newSpritePosition;
                velocity.x = 0; // Stop horizontal movement
                
                // Check if landing on ground
                if (velocity.y < 0)
                {
                    isGrounded = true;
                    velocity.y = 0;
                    Debug.Log("Landed on threshold platform during slide");
                }
            }
            else
            {
                // Cannot move in either direction
                velocity = Vector2.zero;
            }
        }
    }
    
    private void CheckIfGrounded()
    {
        // Calculate camera bounds in world space
        float cameraHeight = 2f * gameCamera.orthographicSize;
        Vector2 cameraPosition = gameCamera.transform.position;
        float minY = cameraPosition.y - (cameraHeight / 2);
        
        // Get the collision box
        Vector2 boxCenter = (Vector2)transform.position + collisionBoxOffset;
        Vector2 halfSize = collisionBoxSize * 0.5f;
        Rect collisionBox = new Rect(boxCenter - halfSize, collisionBoxSize);
        
        // Check if the bottom of the collision box is very close to the ground (camera bottom)
        float groundCheckThreshold = 0.1f;
        bool onCameraGround = Mathf.Abs(collisionBox.yMin - minY) < groundCheckThreshold;
        
        // Check if the player's vertical velocity is close to zero or negative (falling/landed)
        bool stoppedOrFalling = velocity.y <= 0.1f;
        
        // ALWAYS check for threshold ground
        Vector2 groundCheckCenter = boxCenter - new Vector2(0, halfSize.y + 0.1f);
        bool onThresholdGround = CheckThresholdCollision(groundCheckCenter);
        
        // Set grounded state if on camera ground OR on threshold ground
        if ((onCameraGround || onThresholdGround) && stoppedOrFalling)
        {
            if (!isGrounded)
            {
                if (onCameraGround) {
                    Debug.Log("Camera ground detected!");
                } else {
                    Debug.Log("Threshold ground detected!");
                }
                isGrounded = true;
                velocity.y = 0;
            }
        }
        else if (isGrounded)
        {
            // We were grounded but now we're not on any ground
            Debug.Log("Left ground!");
            isGrounded = false;
        }
    }
    
    // Utility method to visualize the collision point and box in the editor
    private void OnDrawGizmosSelected()
    {
        // Draw collision point
        Gizmos.color = Color.red;
        Vector2 collPoint = (Vector2)transform.position + collisionPointOffset;
        Gizmos.DrawWireSphere(collPoint, 0.1f);
        
        // Draw AABB collision box
        Gizmos.color = Color.yellow;
        Vector2 boxCenter = (Vector2)transform.position + collisionBoxOffset;
        Gizmos.DrawWireCube(boxCenter, collisionBoxSize);
        
        // Draw camera bounds for debugging
        Camera debugCamera = gameCamera != null ? gameCamera : Camera.main;
        if (debugCamera != null)
        {
            Gizmos.color = Color.blue;
            float cameraHeight = 2f * debugCamera.orthographicSize;
            float cameraWidth = cameraHeight * debugCamera.aspect;
            
            Vector3 cameraPosition = debugCamera.transform.position;
            
            // Calculate the corners
            Vector3 bottomLeft = new Vector3(cameraPosition.x - cameraWidth/2, cameraPosition.y - cameraHeight/2, 0);
            Vector3 bottomRight = new Vector3(cameraPosition.x + cameraWidth/2, cameraPosition.y - cameraHeight/2, 0);
            Vector3 topLeft = new Vector3(cameraPosition.x - cameraWidth/2, cameraPosition.y + cameraHeight/2, 0);
            Vector3 topRight = new Vector3(cameraPosition.x + cameraWidth/2, cameraPosition.y + cameraHeight/2, 0);
            
            // Draw the lines
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}