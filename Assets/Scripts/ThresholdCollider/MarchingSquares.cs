using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implements the Marching Squares algorithm to extract collision boundaries from a black and white texture.
/// </summary>
public class MarchingSquares : MonoBehaviour
{
    [Tooltip("The texture to extract collision boundaries from. Black areas are walls, white areas are void.")]
    // public Texture2D sourceTexture;

    // [Tooltip("The threshold value to determine if a pixel is considered solid (0-1).")]
    [Range(0, 1)]
    public float threshold = 0.5f;

    [Tooltip("The scale factor to apply to the generated colliders.")]
    public float colliderScale = 1.0f;

    [Tooltip("Whether to simplify the generated colliders by removing redundant points.")]
    public bool simplifyColliders = true;

    [Tooltip("The maximum distance between points for simplification (higher values = more simplification).")]
    [Range(0.01f, 5.0f)]
    public float simplificationTolerance = 0.1f;

    [Tooltip("Whether to align the colliders with the main camera viewport.")]
    public bool alignWithCamera = true;

    // Cached data
    private Color32[] texturePixels;
    private int textureWidth;
    private int textureHeight;
    private List<EdgeCollider2D> generatedColliders = new List<EdgeCollider2D>();

    void Update()
    {
        // Check if the screen size has changed
        if ((Screen.width != textureWidth || Screen.height != textureHeight) && Input.GetKeyDown(KeyCode.R))
        {
            // Clear existing colliders
            ClearGeneratedColliders();

            // Update the texture dimensions
            textureWidth = Screen.width;
            textureHeight = Screen.height;

            // Recreate the texture with the new dimensions
            GenerateColliders();
        }
    }

    /// <summary>
    /// Extracts collision boundaries from the source texture and generates EdgeCollider2D components.
    /// </summary>
    public void GenerateColliders()
    {
        // if (sourceTexture == null)
        // {
        //     Debug.LogError("Source texture is not assigned!");
        //     return;
        // }
        Texture2D sourceTexture = ThresholdMapGenerator.Instance.getMap();

        // Clear any previously generated colliders
        ClearGeneratedColliders();

        // Cache texture data
        texturePixels = sourceTexture.GetPixels32();
        textureWidth = sourceTexture.width;
        textureHeight = sourceTexture.height;

        // Extract boundaries using Marching Squares
        List<List<Vector2>> boundaries = ExtractBoundaries();

        // Generate colliders from boundaries
        GenerateEdgeColliders(boundaries);

        Debug.Log($"Generated {generatedColliders.Count} EdgeCollider2D components.");
    }

    /// <summary>
    /// Clears all previously generated colliders.
    /// </summary>
    public void ClearGeneratedColliders()
    {
        foreach (EdgeCollider2D collider in generatedColliders)
        {
            if (collider != null)
            {
                DestroyImmediate(collider);
            }
        }
        generatedColliders.Clear();
    }

    /// <summary>
    /// Extracts boundaries from the texture using the Marching Squares algorithm.
    /// </summary>
    private List<List<Vector2>> ExtractBoundaries()
    {
        // Create a grid representing solid/empty cells
        bool[,] grid = new bool[textureWidth, textureHeight];
        
        // Fill the grid based on pixel luminance
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                Color32 pixel = texturePixels[y * textureWidth + x];
                float luminance = (pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f) / 255f;
                grid[x, y] = luminance < threshold; // True for solid (black), false for void (white)
            }
        }

        // Use Marching Squares to extract boundaries
        return MarchSquares(grid);
    }

    /// <summary>
    /// Implements the Marching Squares algorithm to extract boundaries from a binary grid.
    /// </summary>
    private List<List<Vector2>> MarchSquares(bool[,] grid)
    {
        List<List<Vector2>> allBoundaries = new List<List<Vector2>>();
        bool[,] visited = new bool[textureWidth - 1, textureHeight - 1];

        // Scan the grid for boundary cells
        for (int y = 0; y < textureHeight - 1; y++)
        {
            for (int x = 0; x < textureWidth - 1; x++)
            {
                if (!visited[x, y] && HasBoundary(grid, x, y))
                {
                    // Found a new boundary, trace it
                    List<Vector2> boundary = TraceBoundary(grid, visited, x, y);
                    if (boundary.Count > 2) // Only add boundaries with at least 3 points
                    {
                        allBoundaries.Add(boundary);
                    }
                }
            }
        }

        return allBoundaries;
    }

    /// <summary>
    /// Checks if a cell contains a boundary.
    /// </summary>
    private bool HasBoundary(bool[,] grid, int x, int y)
    {
        // Get the 4 corners of the cell
        bool topLeft = grid[x, y + 1];
        bool topRight = grid[x + 1, y + 1];
        bool bottomRight = grid[x + 1, y];
        bool bottomLeft = grid[x, y];

        // If all corners are the same (all solid or all void), there's no boundary
        return !(topLeft == topRight && topRight == bottomRight && bottomRight == bottomLeft);
    }

    /// <summary>
    /// Traces a complete boundary starting from a given cell.
    /// </summary>
    private List<Vector2> TraceBoundary(bool[,] grid, bool[,] visited, int startX, int startY)
    {
        List<Vector2> boundary = new List<Vector2>();
        int x = startX;
        int y = startY;
        int direction = 0; // 0: right, 1: up, 2: left, 3: down

        do
        {
            visited[x, y] = true;

            // Get the 4 corners of the current cell
            bool topLeft = grid[x, y + 1];
            bool topRight = grid[x + 1, y + 1];
            bool bottomRight = grid[x + 1, y];
            bool bottomLeft = grid[x, y];

            // Calculate the case index (0-15) based on the 4 corners
            int caseIndex = 0;
            if (bottomLeft) caseIndex |= 1;
            if (bottomRight) caseIndex |= 2;
            if (topRight) caseIndex |= 4;
            if (topLeft) caseIndex |= 8;

            // Get the boundary points for this case
            Vector2[] points = GetBoundaryPoints(x, y, caseIndex);
            
            // Add the points to the boundary
            foreach (Vector2 point in points)
            {
                boundary.Add(point);
            }

            // Move to the next cell based on the current direction
            switch (direction)
            {
                case 0: // Right
                    if (x < textureWidth - 2 && HasBoundary(grid, x + 1, y) && !visited[x + 1, y])
                    {
                        x++;
                    }
                    else
                    {
                        direction = 1;
                    }
                    break;
                case 1: // Up
                    if (y < textureHeight - 2 && HasBoundary(grid, x, y + 1) && !visited[x, y + 1])
                    {
                        y++;
                    }
                    else
                    {
                        direction = 2;
                    }
                    break;
                case 2: // Left
                    if (x > 0 && HasBoundary(grid, x - 1, y) && !visited[x - 1, y])
                    {
                        x--;
                    }
                    else
                    {
                        direction = 3;
                    }
                    break;
                case 3: // Down
                    if (y > 0 && HasBoundary(grid, x, y - 1) && !visited[x, y - 1])
                    {
                        y--;
                    }
                    else
                    {
                        direction = 0;
                    }
                    break;
            }

            // Continue until we've visited all connected boundary cells
        } while (x != startX || y != startY);

        // Simplify the boundary if requested
        if (simplifyColliders)
        {
            boundary = SimplifyBoundary(boundary);
        }

        return boundary;
    }

    /// <summary>
    /// Gets the boundary points for a specific Marching Squares case.
    /// </summary>
    private Vector2[] GetBoundaryPoints(int x, int y, int caseIndex)
    {
        // Convert grid coordinates to world coordinates
        float worldX = x - textureWidth / 2f;
        float worldY = y - textureHeight / 2f;

        // Define the 4 corners of the cell in world coordinates
        Vector2 bottomLeft = new Vector2(worldX, worldY);
        Vector2 bottomRight = new Vector2(worldX + 1, worldY);
        Vector2 topRight = new Vector2(worldX + 1, worldY + 1);
        Vector2 topLeft = new Vector2(worldX, worldY + 1);

        // Define the midpoints of the 4 edges
        Vector2 midBottom = new Vector2(worldX + 0.5f, worldY);
        Vector2 midRight = new Vector2(worldX + 1, worldY + 0.5f);
        Vector2 midTop = new Vector2(worldX + 0.5f, worldY + 1);
        Vector2 midLeft = new Vector2(worldX, worldY + 0.5f);

        // Return the appropriate boundary points based on the case index
        switch (caseIndex)
        {
            case 0: return new Vector2[0]; // All corners are void
            case 1: return new Vector2[] { midBottom, midLeft }; // Bottom-left is solid
            case 2: return new Vector2[] { midRight, midBottom }; // Bottom-right is solid
            case 3: return new Vector2[] { midRight, midLeft }; // Bottom edge is solid
            case 4: return new Vector2[] { midTop, midRight }; // Top-right is solid
            case 5: return new Vector2[] { midTop, midRight, midBottom, midLeft }; // Diagonal case
            case 6: return new Vector2[] { midTop, midBottom }; // Right edge is solid
            case 7: return new Vector2[] { midTop, midLeft }; // Top-right, bottom-right, bottom-left are solid
            case 8: return new Vector2[] { midLeft, midTop }; // Top-left is solid
            case 9: return new Vector2[] { midBottom, midTop }; // Left edge is solid
            case 10: return new Vector2[] { midLeft, midTop, midRight, midBottom }; // Diagonal case
            case 11: return new Vector2[] { midRight, midTop }; // Top-left, bottom-left, bottom-right are solid
            case 12: return new Vector2[] { midLeft, midRight }; // Top edge is solid
            case 13: return new Vector2[] { midBottom, midRight }; // Top-left, top-right, bottom-left are solid
            case 14: return new Vector2[] { midLeft, midBottom }; // Top-left, top-right, bottom-right are solid
            case 15: return new Vector2[0]; // All corners are solid
            default: return new Vector2[0];
        }
    }

    /// <summary>
    /// Simplifies a boundary by removing redundant points.
    /// </summary>
    private List<Vector2> SimplifyBoundary(List<Vector2> boundary)
    {
        if (boundary.Count <= 2)
            return boundary;

        List<Vector2> simplified = new List<Vector2>();
        simplified.Add(boundary[0]);

        for (int i = 1; i < boundary.Count - 1; i++)
        {
            Vector2 prev = boundary[i - 1];
            Vector2 current = boundary[i];
            Vector2 next = boundary[i + 1];

            // Check if the current point is on the same line as the previous and next points
            Vector2 dir1 = (current - prev).normalized;
            Vector2 dir2 = (next - current).normalized;

            // If the directions are nearly the same, the point is redundant
            if (Vector2.Dot(dir1, dir2) < 0.99f || Vector2.Distance(prev, current) > simplificationTolerance * 5)
            {
                simplified.Add(current);
            }
        }

        simplified.Add(boundary[boundary.Count - 1]);
        return simplified;
    }

    /// <summary>
    /// Generates EdgeCollider2D components from the extracted boundaries.
    /// </summary>
    private void GenerateEdgeColliders(List<List<Vector2>> boundaries)
    {
        foreach (List<Vector2> boundary in boundaries)
        {
            if (boundary.Count < 2)
                continue;

            // Create a new GameObject for the collider
            GameObject colliderObj = new GameObject("EdgeCollider");
            colliderObj.transform.parent = transform;
            colliderObj.transform.localPosition = Vector3.zero;
            colliderObj.transform.localRotation = Quaternion.identity;
            colliderObj.transform.localScale = Vector3.one * colliderScale;

            // Add an EdgeCollider2D component
            EdgeCollider2D edgeCollider = colliderObj.AddComponent<EdgeCollider2D>();
            
            // Set the points
            edgeCollider.points = boundary.ToArray();

            // Align with camera if requested
            if (alignWithCamera && Camera.main != null)
            {
                AlignWithCamera(colliderObj);
            }

            // Add to the list of generated colliders
            generatedColliders.Add(edgeCollider);
        }
    }

    /// <summary>
    /// Aligns a collider with the main camera viewport.
    /// </summary>
    private void AlignWithCamera(GameObject colliderObj)
    {
        if (Camera.main == null)
            return;

        // Get the camera's transform
        Transform cameraTransform = Camera.main.transform;
        
        // Set the collider's position to match the camera's position in the XY plane
        Vector3 position = colliderObj.transform.position;
        position.x = cameraTransform.position.x;
        position.y = cameraTransform.position.y;
        colliderObj.transform.position = position;
        
        // Set the collider's rotation to match the camera's rotation around the Z axis
        Vector3 rotation = colliderObj.transform.eulerAngles;
        rotation.z = cameraTransform.eulerAngles.z;
        colliderObj.transform.eulerAngles = rotation;
    }
}
