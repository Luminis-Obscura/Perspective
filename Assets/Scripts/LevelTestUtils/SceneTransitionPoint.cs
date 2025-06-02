using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionPoint : MonoBehaviour
{
    [Tooltip("Name of the scene to load when player overlaps this point.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Touch radius in 2D world space units.")]
    [SerializeField] private float touchRadius = 0.5f;

    [Tooltip("Reference to the 2D parent space (same as used in PerspectiveSwitcher)")]
    public Transform space2D;

    [Tooltip("Reference to the 2D camera used for projection")]
    public Camera camera2D;

    [Tooltip("Main 3D camera (for WorldToScreen projection)")]
    public Camera mainCamera;

    private ThresholdBasedMovement player;
    private bool hasTriggered = false;

    public bool switchTo3DOnTransition = true;

    void Update()
    {
        if (hasTriggered || space2D == null || camera2D == null || mainCamera == null)
            return;

        if (player == null)
        {
            player = FindFirstObjectByType<ThresholdBasedMovement>();
            if (player == null) return;
        }

        Vector2 characterPosition = player.transform.position;

        Vector3 spawnPointScreenPos = mainCamera.WorldToScreenPoint(transform.position);
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
        Vector2 spawnPoint2D = (Vector2)space2D.position + (Vector2)worldOffset;

        float distance = Vector2.Distance(characterPosition, spawnPoint2D);
        if (distance < touchRadius)
        {
            Debug.Log($"[SceneTransitionPoint2D] Triggered! Loading scene: {targetSceneName}");
            hasTriggered = true;
            if (switchTo3DOnTransition)
            {
                FindFirstObjectByType<PerspectiveSwitcher>().SwitchTo3D();
            }
            
            SceneManager.LoadScene(targetSceneName);
        }


        Debug.DrawLine(characterPosition, spawnPoint2D, Color.magenta, 0.1f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
