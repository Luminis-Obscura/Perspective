using UnityEngine;

public class BlindLine : MonoBehaviour
{
    public Transform player; // ��Ҷ���
    public float maxDistance = 5f; // ��������
    private Material material; // ��ǰ����Ĳ���
    private Color originalColor; // ���ʵ�ԭʼ��ɫ

    void Start()
    {
        // ��ȡ����Ĳ���
        material = GetComponent<Renderer>().material;
        originalColor = material.color;
    }

    void Update()
    {
        if (player == null) return;

        // ��ȡ����İ�Χ��
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        // ������ҵ������Χ�������ľ���
        Vector3 closestPoint = renderer.bounds.ClosestPoint(player.position);
        float distance = Vector3.Distance(closestPoint, player.position);

        // �������С�������룬�����͸����
        if (distance < maxDistance)
        {
            float alpha = Mathf.Lerp(0, 1, 1 - (distance / maxDistance)); // ���ݾ������Alphaֵ
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }
        else
        {
            // ����������ʱ��������ȫ͸��
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        }
    }
}

