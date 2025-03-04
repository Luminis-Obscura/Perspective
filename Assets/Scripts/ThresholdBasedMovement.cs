using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ThresholdBasedMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float gravityForce = 9.8f;
    [SerializeField] private Vector2 collisionPointOffset = new Vector2(0, -0.5f); // Adjustable feet position
    
    [Header("Render Settings")]
    [SerializeField] private RenderTexture thresholdMap;
    [SerializeField] private RenderTexture outputRenderTexture;
    [SerializeField] private Camera outputCamera;
    
    private SpriteRenderer spriteRenderer;
    private Vector2 velocity;
    private Texture2D thresholdMapTexture;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Create a readable texture from the RenderTexture
        thresholdMapTexture = new Texture2D(thresholdMap.width, thresholdMap.height, TextureFormat.RGB24, false);
        UpdateThresholdMapTexture();
        
        // Setup output camera if not set
        if (outputCamera == null)
        {
            Debug.LogWarning("Output camera not set, creating one");
            GameObject cameraObject = new GameObject("OutputCamera");
            outputCamera = cameraObject.AddComponent<Camera>();
            outputCamera.clearFlags = CameraClearFlags.SolidColor;
            outputCamera.backgroundColor = Color.black;
            outputCamera.orthographic = true;
            outputCamera.targetTexture = outputRenderTexture;
        }
    }
    
    private void Update()
    {
        // Update our readable copy of the threshold map
        UpdateThresholdMapTexture();
        
        // Get input
        float horizontalInput = Input.GetAxis("Horizontal");
        
        // Calculate movement
        velocity.x = horizontalInput * movementSpeed;
        
        // Apply gravity
        velocity.y -= gravityForce * Time.deltaTime;
        
        // Calculate attempted new position
        Vector2 newPosition = (Vector2)transform.position + velocity * Time.deltaTime;
        
        // Check collision with threshold map
        Vector2 collisionPoint = newPosition + collisionPointOffset;
        
        // Convert collision point to threshold map texture coordinates
        int pixelX = Mathf.RoundToInt(collisionPoint.x * thresholdMapTexture.width);
        int pixelY = Mathf.RoundToInt(collisionPoint.y * thresholdMapTexture.height);
        
        // Clamp pixel coordinates to texture bounds
        pixelX = Mathf.Clamp(pixelX, 0, thresholdMapTexture.width - 1);
        pixelY = Mathf.Clamp(pixelY, 0, thresholdMapTexture.height - 1);
        
        // Sample pixel color
        Color pixelColor = thresholdMapTexture.GetPixel(pixelX, pixelY);
        
        // Check if pixel is black (movable area) or white (wall)
        bool canMove = pixelColor.r < 0.5f; // If pixel is closer to black (0) than white (1)
        
        if (canMove)
        {
            // Move player
            transform.position = newPosition;
        }
        else
        {
            // Hit wall, stop movement
            velocity = Vector2.zero;
        }
        
        // Update output render texture
        if (outputCamera != null && outputRenderTexture != null)
        {
            outputCamera.Render();
        }
    }
    
    private void UpdateThresholdMapTexture()
    {
        // Create a temporary RenderTexture
        RenderTexture tempRT = RenderTexture.GetTemporary(
            thresholdMap.width,
            thresholdMap.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );
        
        // Copy the threshold map to the temporary RenderTexture
        Graphics.Blit(thresholdMap, tempRT);
        
        // Set the temporary RenderTexture as active
        RenderTexture.active = tempRT;
        
        // Read pixels from the active RenderTexture to our Texture2D
        thresholdMapTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        thresholdMapTexture.Apply();
        
        // Reset active RenderTexture and release temporary one
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempRT);
    }
    
    // Utility method to visualize the collision point in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + collisionPointOffset, 0.1f);
    }
}