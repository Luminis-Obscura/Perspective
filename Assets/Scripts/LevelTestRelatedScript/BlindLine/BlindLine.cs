using UnityEngine;

public class BlindLine : MonoBehaviour
{
    public Transform player; // 玩家对象
    public float maxDistance = 5f; // 最大检测距离
    private Material material; // 当前物体的材质
    private Color originalColor; // 材质的原始颜色

    void Start()
    {
        // 获取物体的材质
        material = GetComponent<Renderer>().material;
        originalColor = material.color;
    }

    void Update()
    {
        if (player == null) return;

        // 获取物体的包围盒
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        // 计算玩家到物体包围盒最近点的距离
        Vector3 closestPoint = renderer.bounds.ClosestPoint(player.position);
        float distance = Vector3.Distance(closestPoint, player.position);

        // 如果距离小于最大距离，则调整透明度
        if (distance < maxDistance)
        {
            float alpha = Mathf.Lerp(0, 1, 1 - (distance / maxDistance)); // 根据距离计算Alpha值
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }
        else
        {
            // 超出最大距离时，保持完全透明
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        }
    }
}

