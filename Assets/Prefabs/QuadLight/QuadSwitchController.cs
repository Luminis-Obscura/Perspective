using UnityEngine;
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

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactDistance && Input.GetKeyDown(KeyCode.E))
        {
            isControlling = !isControlling;
            Debug.Log("Control mode toggled: " + isControlling);
        }

        if (isControlling)
        {
            // Z（前后）由 vertical 输入，Y（上下）由 horizontal 输入
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            Vector3 move = new Vector3(0, moveY, -moveX).normalized;

            quadToControl.transform.position += move * quadMoveSpeed * Time.deltaTime;
        }
    }
}

