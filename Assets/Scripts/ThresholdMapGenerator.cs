using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class ThresholdMapGenerator : MonoBehaviour
{
    // Singleton instance
    private static ThresholdMapGenerator _instance;

    // Public accessor for the singleton
    public static ThresholdMapGenerator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ThresholdMapGenerator>();
                
                if (_instance == null)
                {
                    Debug.LogError("No ThresholdMapGenerator found in the scene!");
                }
            }
            return _instance;
        }
    }

    // Make sure we only have one instance
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Camera sourceCamera;
    public float threshold = 0.5f; // 0-1 range, pixels darker than this become black
    public MarchingSquaresMeshGenerator countourGenerator;
    public RawImage debugImage;
    private RenderTexture renderTexture;
    private Texture2D resultMap;
    private Material thresholdMaterial;
    private static readonly int ThresholdProp = Shader.PropertyToID("_Threshold");
    
    // Track screen dimensions to detect changes
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Start()
    {
        // Initialize textures with current screen dimensions
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        
        CreateTextures(lastScreenWidth, lastScreenHeight);
        
        // Create shader material for fast processing
        Shader thresholdShader = Shader.Find("Hidden/ThresholdFilter");
        if (thresholdShader == null)
        {
            Debug.LogWarning("Threshold shader not found. Fast processing will be unavailable.");
        }
        else
        {
            thresholdMaterial = new Material(thresholdShader);
        }
    }
    

    // Create or recreate textures with specified dimensions
    private void CreateTextures(int width, int height)
    {
        // Clean up existing textures to prevent memory leaks
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        
        if (resultMap != null)
        {
            Destroy(resultMap);
        }
        
        // Create new textures with specified dimensions
        renderTexture = new RenderTexture(width, height, 24); // Add depth buffer (24-bit)
        resultMap = new Texture2D(width, height, TextureFormat.RGB24, false);
        
        // Update tracking variables
        lastScreenWidth = width;
        lastScreenHeight = height;
    }

    // Check if screen dimensions have changed and update textures if needed
    private bool UpdateTexturesIfNeeded()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            CreateTextures(Screen.width, Screen.height);
            return true;
        }
        return false;
    }

    void OnDestroy()
    {
        if (thresholdMaterial != null)
        {
            Destroy(thresholdMaterial);
        }
        
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        
        if (resultMap != null)
        {
            Destroy(resultMap);
        }
    }

    void Update()
    {
        if (debugImage != null)
        {
            // Check if screen dimensions changed and update textures if needed
            UpdateTexturesIfNeeded();
            
            Texture2D texture = FastGenerateThresholdMap();
            countourGenerator.ProcessTexture(texture);
            debugImage.texture = texture;
        }
    }

    public Texture2D SlowGenerateThresholdMap()
    {
        // Check if screen dimensions changed and update textures if needed
        UpdateTexturesIfNeeded();
        
        // Capture current frame
        RenderTexture prevTarget = sourceCamera.targetTexture;
        sourceCamera.targetTexture = renderTexture;
        sourceCamera.Render();
        sourceCamera.targetTexture = prevTarget;

        // Read pixels
        RenderTexture.active = renderTexture;
        resultMap.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        resultMap.Apply();
        RenderTexture.active = null;

        // Apply threshold filter
        Color[] pixels = resultMap.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            // Calculate brightness
            float luminance = 0.299f * pixels[i].r + 0.587f * pixels[i].g + 0.114f * pixels[i].b;
            // Apply threshold
            pixels[i] = luminance < threshold ? Color.black : Color.white;
        }
        resultMap.SetPixels(pixels);
        resultMap.Apply();
        return resultMap;
    }

    public Texture2D FastGenerateThresholdMap()
    {
        // Check if screen dimensions changed and update textures if needed
        UpdateTexturesIfNeeded();
        
        if (thresholdMaterial == null)
        {
            return SlowGenerateThresholdMap(); // Fallback
        }

        // Update threshold value
        thresholdMaterial.SetFloat(ThresholdProp, threshold);

        // Temporary render texture for output
        RenderTexture outputRT = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height, 0);

        // Capture current frame
        RenderTexture prevTarget = sourceCamera.targetTexture;
        sourceCamera.targetTexture = renderTexture;
        sourceCamera.Render();
        sourceCamera.targetTexture = prevTarget;

        // Apply threshold filter via shader
        Graphics.Blit(renderTexture, outputRT, thresholdMaterial);

        // Copy result to texture
        RenderTexture.active = outputRT;
        resultMap.ReadPixels(new Rect(0, 0, outputRT.width, outputRT.height), 0, 0);
        resultMap.Apply();
        RenderTexture.active = null;

        // Release temporary RT
        RenderTexture.ReleaseTemporary(outputRT);

        return resultMap;
    }

    // Example usage
    public void SaveMapForLaterUse()
    {
        Texture2D map = thresholdMaterial != null ? FastGenerateThresholdMap() : SlowGenerateThresholdMap();
        // Store as class variable, save to disk, or use as needed
    }

    public Texture2D getMap()
    {
        Texture2D map = thresholdMaterial != null ? FastGenerateThresholdMap() : SlowGenerateThresholdMap();
        return map;
        // Use the map as needed
    }
}