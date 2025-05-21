using UnityEngine;

public class BlindLine : MonoBehaviour
{
    public Transform player; 
    public float maxDistance = 5f; 
    private Material material; 
    private Color originalColor; 

    void Start()
    {
        material = GetComponent<Renderer>().material;
        originalColor = material.color;
    }

    void Update()
    {
        if (player == null) return;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        Vector3 closestPoint = renderer.bounds.ClosestPoint(player.position);
        float distance = Vector3.Distance(closestPoint, player.position);

        if (distance < maxDistance)
        {
            float alpha = Mathf.Lerp(0, 1, 1 - (distance / maxDistance)); 
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }
        else
        {
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        }
    }
}

