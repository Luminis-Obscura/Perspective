using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PerspectiveSwitcher : MonoBehaviour
{
    [Header("References")]
    public GameObject View2D;
    public FirstPersonController controller;
    public StarterAssetsInputs inputs;
    public Camera mainCamera;
    
    [Header("2D Character Settings")]
    public GameObject character2DPrefab; // Prefab of the 2D character
    public bool preventEdgeDropping = true; // Toggle to prevent falling off edges
    public bool ignoreCollisions = false; // Toggle to ignore texture collisions
    
    private bool is2D = false;
    private Character2D currentCharacter2D; // Reference to the currently spawned character
    
    void Start()
    {
        // Make sure we start in 3D mode
        if (mainCamera == null) mainCamera = Camera.main;
        SwitchTo3D();
    }
    
    void SwitchTo2D()
    {
        // First make 2D view active
        View2D.SetActive(true);
        
        // Disable first person controller while keeping input system enabled
        controller.enabled = false;
        
        // Spawn the 2D character at the center of the screen
        SpawnCharacter2D();
        
        is2D = true;
    }
    
    void SwitchTo3D()
    {
        View2D.SetActive(false);
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
        
        // Calculate the center of the screen in world coordinates
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenCenter);
        worldPosition.z = 0; // Set appropriate z-depth for 2D character
        
        // Instantiate the prefab at the center position
        GameObject characterObj = Instantiate(character2DPrefab, worldPosition, Quaternion.identity);
        
        // Get the Character2D component
        currentCharacter2D = characterObj.GetComponent<Character2D>();
        
        // If the character doesn't have the script, add it
        if (currentCharacter2D == null)
        {
            Debug.LogWarning("Character2D prefab is missing the Character2D component! Adding it now.");
            currentCharacter2D = characterObj.AddComponent<Character2D>();
        }
        
        // Apply settings from PerspectiveSwitcher
        currentCharacter2D.preventEdgeDropping = this.preventEdgeDropping;
        currentCharacter2D.ignoreCollisions = this.ignoreCollisions;
        
        // Parent to the 2D view for organization
        characterObj.transform.parent = View2D.transform;
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