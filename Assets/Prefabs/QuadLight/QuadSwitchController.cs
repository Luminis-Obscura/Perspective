using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using StarterAssets;

public class QuadSwitchController : MonoBehaviour
{
    [Header("References")]
    public Transform player;              // 玩家 Transform
    public GameObject quadToControl;      // 要控制的 Quad

    [Header("Control Settings")]
    public float interactDistance = 3f;   // 控制距离
    public float quadMoveSpeed = 2f;      // 移动速度

    private bool isControlling = false;
    private StarterAssetsInputs playerInputs;

    void Start()
    {
        if (player != null)
        {
            playerInputs = player.GetComponent<StarterAssetsInputs>();
            if (playerInputs == null)
                Debug.LogError("StarterAssetsInputs not found on player!");
        }
        else
        {
            Debug.LogError("Player reference not assigned.");
        }
    }

    void Update()
    {
        if (player == null || quadToControl == null || playerInputs == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

#if ENABLE_INPUT_SYSTEM
        // 玩家靠近并按 E 键切换控制状态
        if (distance <= interactDistance && Keyboard.current.eKey.wasPressedThisFrame)
        {
            isControlling = !isControlling;
            Debug.Log("Control mode toggled: " + isControlling);
        }
#endif

        if (isControlling)
        {
            Vector2 moveInput = playerInputs.move;

            // Z（前后）由 vertical 输入，Y（上下）由 horizontal 输入
            Vector3 move = new Vector3(0f, moveInput.x, moveInput.y); // Y = 左右, Z = 上下

            quadToControl.transform.position += move * quadMoveSpeed * Time.deltaTime;
        }
    }
}

