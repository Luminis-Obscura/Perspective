using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MarchingSquaresContour : MonoBehaviour
{
    [Header("Input")]
    public Texture2D inputTexture;
    
    [Header("Visualization Settings")]
    public Material lineMaterial;
    public Material meshMaterial;
    public float lineWidth = 0.01f;
    public Color contourColor = Color.red;
    public float threshold = 0.5f; // Threshold for black/white detection
    public bool showContourLines = true;
    public bool createMeshes = true;
    
    [Header("Algorithm Settings")]
    public float stepSize = 1f; // Grid cell size in pixels
    public bool smoothContours = true;
    public int smoothingIterations = 2;
    public float worldScale = 10f; // Scale factor for world space
    
    [Header("Camera Alignment")]
    public Camera targetCamera;          // leave empty to auto-use Camera.main
    public bool alignToCamera = true;
    
    private List<List<Vector2>> contours = new List<List<Vector2>>();
    private GameObject contoursParent;
    private GameObject meshesParent;
    
    // Marching squares lookup table
    private static readonly int[,] edgeTable = new int[,] {
        {-1, -1, -1, -1, -1}, // 0
        {3, 0, -1, -1, 4},    // 1
        {0, 1, -1, -1, 4},    // 2
        {3, 1, -1, -1, 2},    // 3
        {1, 2, -1, -1, 4},    // 4
        {3, 0, 1, 2, 4},      // 5 - saddle point
        {0, 2, -1, -1, 2},    // 6
        {3, 2, -1, -1, 1},    // 7
        {2, 3, -1, -1, 4},    // 8
        {2, 0, -1, -1, 2},    // 9
        {0, 1, 2, 3, 4},      // 10 - saddle point
        {2, 1, -1, -1, 1},    // 11
        {1, 3, -1, -1, 2},    // 12
        {1, 0, -1, -1, 1},    // 13
        {0, 3, -1, -1, 1},    // 14
        {-1, -1, -1, -1, -1}  // 15
    };
    
    void Start()
    {
        if (inputTexture != null)
        {
            ExtractContours();
            if (showContourLines) VisualizeContours();
            if (createMeshes) CreateMeshesFromContours();
        }
    }
    
    public void ProcessTexture(Texture2D texture)
    {
        inputTexture = texture;
        ExtractContours();
        if (showContourLines) VisualizeContours();
        if (createMeshes) CreateMeshesFromContours();
    }
    
    void ExtractContours()
    {
        if (inputTexture == null) return;
        
        contours.Clear();
        
        // Convert texture to grayscale values
        Color[] pixels = inputTexture.GetPixels();
        int width = inputTexture.width;
        int height = inputTexture.height;
        
        float[,] grid = new float[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * width + x];
                grid[x, y] = pixel.grayscale;
            }
        }
        
        // Track visited edges to avoid duplicates
        HashSet<string> visitedEdges = new HashSet<string>();
        
        // Process each cell in the grid
        for (int y = 0; y < height - 1; y += (int)stepSize)
        {
            for (int x = 0; x < width - 1; x += (int)stepSize)
            {
                ProcessCell(x, y, grid, width, height, visitedEdges);
            }
        }
        
        // Merge and clean up contours
        MergeNearbyContours(stepSize * 2);
        
        // Smooth contours if enabled
        if (smoothContours)
        {
            for (int i = 0; i < smoothingIterations; i++)
            {
                SmoothContours();
            }
        }
    }
    
    void ProcessCell(int x, int y, float[,] grid, int width, int height, HashSet<string> visitedEdges)
    {
        // Get the configuration index based on corner values
        int config = 0;
        if (grid[x, y] > threshold) config |= 1;
        if (grid[Mathf.Min(x + 1, width - 1), y] > threshold) config |= 2;
        if (grid[Mathf.Min(x + 1, width - 1), Mathf.Min(y + 1, height - 1)] > threshold) config |= 4;
        if (grid[x, Mathf.Min(y + 1, height - 1)] > threshold) config |= 8;
        
        // Skip if no contour in this cell
        if (config == 0 || config == 15) return;
        
        // Get edge vertices based on configuration
        List<Vector2> cellEdges = new List<Vector2>();
        int numEdges = edgeTable[config, 4];
        
        for (int i = 0; i < numEdges; i++)
        {
            int edge = edgeTable[config, i];
            Vector2 point = GetEdgePoint(x, y, edge, grid, width, height);
            cellEdges.Add(point);
        }
        
        // Handle saddle points (configs 5 and 10)
        if (config == 5 || config == 10)
        {
            // For saddle points, check center value to determine connection
            float center = (grid[x, y] + grid[x + 1, y] + grid[x + 1, y + 1] + grid[x, y + 1]) / 4f;
            bool centerHigh = center > threshold;
            
            if ((config == 5 && centerHigh) || (config == 10 && !centerHigh))
            {
                // Connect 0-1 and 2-3
                AddEdgeToContour(cellEdges[0], cellEdges[1], visitedEdges);
                AddEdgeToContour(cellEdges[2], cellEdges[3], visitedEdges);
            }
            else
            {
                // Connect 0-3 and 1-2
                AddEdgeToContour(cellEdges[0], cellEdges[3], visitedEdges);
                AddEdgeToContour(cellEdges[1], cellEdges[2], visitedEdges);
            }
        }
        else if (cellEdges.Count >= 2)
        {
            // Normal case: connect the two edge points
            AddEdgeToContour(cellEdges[0], cellEdges[1], visitedEdges);
        }
    }
    
    Vector2 GetEdgePoint(int x, int y, int edge, float[,] grid, int width, int height)
    {
        float x1 = x, y1 = y, x2 = x, y2 = y;
        
        switch (edge)
        {
            case 0: // Top edge
                x2 = x + 1;
                break;
            case 1: // Right edge
                x1 = x + 1;
                x2 = x + 1;
                y2 = y + 1;
                break;
            case 2: // Bottom edge
                y1 = y + 1;
                y2 = y + 1;
                x2 = x + 1;
                break;
            case 3: // Left edge
                y2 = y + 1;
                break;
        }
        
        // Clamp coordinates
        x1 = Mathf.Min(x1, width - 1);
        x2 = Mathf.Min(x2, width - 1);
        y1 = Mathf.Min(y1, height - 1);
        y2 = Mathf.Min(y2, height - 1);
        
        // Linear interpolation to find exact edge position
        float v1 = grid[(int)x1, (int)y1];
        float v2 = grid[(int)x2, (int)y2];
        float t = (threshold - v1) / (v2 - v1);
        t = Mathf.Clamp01(t);
        
        return new Vector2(Mathf.Lerp(x1, x2, t), Mathf.Lerp(y1, y2, t));
    }
    
    void AddEdgeToContour(Vector2 p1, Vector2 p2, HashSet<string> visitedEdges)
    {
        // Create unique key for this edge
        string edgeKey = $"{p1.x},{p1.y}-{p2.x},{p2.y}";
        string reverseKey = $"{p2.x},{p2.y}-{p1.x},{p1.y}";
        
        if (visitedEdges.Contains(edgeKey) || visitedEdges.Contains(reverseKey))
            return;
        
        visitedEdges.Add(edgeKey);
        
        // Try to add to existing contour
        bool added = false;
        float connectionThreshold = stepSize * 0.5f;
        
        foreach (var contour in contours)
        {
            if (contour.Count == 0) continue;
            
            if (Vector2.Distance(contour[contour.Count - 1], p1) < connectionThreshold)
            {
                contour.Add(p2);
                added = true;
                break;
            }
            else if (Vector2.Distance(contour[contour.Count - 1], p2) < connectionThreshold)
            {
                contour.Add(p1);
                added = true;
                break;
            }
            else if (Vector2.Distance(contour[0], p1) < connectionThreshold)
            {
                contour.Insert(0, p2);
                added = true;
                break;
            }
            else if (Vector2.Distance(contour[0], p2) < connectionThreshold)
            {
                contour.Insert(0, p1);
                added = true;
                break;
            }
        }
        
        // Create new contour if not added to existing
        if (!added)
        {
            List<Vector2> newContour = new List<Vector2> { p1, p2 };
            contours.Add(newContour);
        }
    }
    
    void SmoothContours()
    {
        foreach (var contour in contours)
        {
            if (contour.Count < 3) continue;
            
            List<Vector2> smoothed = new List<Vector2>();
            bool isClosed = IsContourClosed(contour);
            
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 prev = i > 0 ? contour[i - 1] : (isClosed ? contour[contour.Count - 1] : contour[i]);
                Vector2 curr = contour[i];
                Vector2 next = i < contour.Count - 1 ? contour[i + 1] : (isClosed ? contour[0] : contour[i]);
                
                // Simple averaging filter
                Vector2 smooth = (prev + curr * 2 + next) / 4f;
                smoothed.Add(smooth);
            }
            
            // Replace with smoothed version
            for (int i = 0; i < contour.Count; i++)
            {
                contour[i] = smoothed[i];
            }
        }
    }
    
    Vector3 TextureToLocalSpace(Vector2 tex)
    {
        // Normalised 0-1 coordinates inside the texture
        float u = tex.x / inputTexture.width;
        float v = tex.y / inputTexture.height;

        // --- Path 1: camera-aligned ------------------------------------------
        if (alignToCamera)
        {
            // Use Camera.main by default
            if (targetCamera == null) targetCamera = Camera.main;

            // Make sure we actually have an orthographic camera before using it
            if (targetCamera != null && targetCamera.orthographic)
            {
                float halfH = targetCamera.orthographicSize;   // world units
                float halfW = halfH * targetCamera.aspect;     // world units

                float worldX = Mathf.Lerp(-halfW , halfW , u);
                float worldY = Mathf.Lerp(-halfH, halfH, v);

                return new Vector3(worldX, worldY, 0f);
            }
        }

        // --- Path 2: legacy centred quad with uniform scale ------------------
        float x = (u - 0.5f) * worldScale;
        float y = (v - 0.5f) * worldScale;
        return new Vector3(x, y, 0f);
    }
    
    void VisualizeContours()
    {
        // Clean up previous visualization
        if (contoursParent != null)
            DestroyImmediate(contoursParent);
        
        contoursParent = new GameObject("Contours");
        contoursParent.transform.SetParent(transform);
        contoursParent.transform.localPosition = Vector3.zero;
        contoursParent.transform.localRotation = Quaternion.identity;
        contoursParent.transform.localScale = Vector3.one;
        contoursParent.layer = LayerMask.NameToLayer("2D");
        
        int contourIndex = 0;
        foreach (var contour in contours)
        {
            if (contour.Count < 2) continue;
            
            GameObject contourObj = new GameObject($"Contour_{contourIndex++}");
            contourObj.transform.SetParent(contoursParent.transform);
            contourObj.transform.localPosition = Vector3.zero;
            contourObj.transform.localRotation = Quaternion.identity;
            contourObj.transform.localScale = Vector3.one;
            contourObj.layer = LayerMask.NameToLayer("2D");
            
            LineRenderer lr = contourObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startColor = contourColor;
            lr.endColor = contourColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = contour.Count;
            lr.useWorldSpace = false; // Use local space
            
            // Convert 2D points to 3D positions in local space
            Vector3[] positions = new Vector3[contour.Count];
            for (int i = 0; i < contour.Count; i++)
            {
                positions[i] = TextureToLocalSpace(contour[i]);
            }
            
            lr.SetPositions(positions);
            lr.loop = IsContourClosed(contour);
        }
    }
    
    void CreateMeshesFromContours()
    {
        // Clean up previous meshes
        if (meshesParent != null)
            DestroyImmediate(meshesParent);
        
        meshesParent = new GameObject("Meshes");
        meshesParent.transform.SetParent(transform);
        meshesParent.transform.localPosition = Vector3.zero;
        meshesParent.transform.localRotation = Quaternion.identity;
        meshesParent.transform.localScale = Vector3.one;
        meshesParent.layer = LayerMask.NameToLayer("2D");
        
        int meshIndex = 0;
        foreach (var contour in contours)
        {
            if (contour.Count < 3) continue;
            if (!IsContourClosed(contour)) continue; // Only create meshes for closed contours
            
            GameObject meshObj = new GameObject($"Mesh_{meshIndex++}");
            meshObj.transform.SetParent(meshesParent.transform);
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.transform.localRotation = Quaternion.identity;
            meshObj.transform.localScale = Vector3.one;
            meshObj.layer = LayerMask.NameToLayer("2D");
            
            MeshFilter mf = meshObj.AddComponent<MeshFilter>();
            MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
            
            Mesh mesh = CreateMeshFromContour(contour);
            mf.mesh = mesh;
            mr.material = meshMaterial != null ? meshMaterial : new Material(Shader.Find("Sprites/Default"));
        }
    }
    
    Mesh CreateMeshFromContour(List<Vector2> contour)
    {
        Mesh mesh = new Mesh();
        
        // Convert contour points to vertices
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        foreach (var point in contour)
        {
            vertices.Add(TextureToLocalSpace(point));
            uvs.Add(new Vector2(point.x / inputTexture.width, point.y / inputTexture.height));
        }
        
        // Triangulate the polygon using ear clipping algorithm
        List<int> triangles = TriangulatePolygon(vertices);
        
        // Assign to mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    List<int> TriangulatePolygon(List<Vector3> vertices)
    {
        List<int> triangles = new List<int>();
        List<int> indices = new List<int>();
        
        // Initialize indices
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }
        
        // Determine if polygon is clockwise or counter-clockwise
        float area = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            int j = (i + 1) % vertices.Count;
            area += vertices[i].x * vertices[j].y;
            area -= vertices[j].x * vertices[i].y;
        }
        bool isClockwise = area < 0;
        
        // Ear clipping algorithm
        while (indices.Count > 3)
        {
            bool earFound = false;
            
            for (int i = 0; i < indices.Count; i++)
            {
                int prevIndex = (i - 1 + indices.Count) % indices.Count;
                int nextIndex = (i + 1) % indices.Count;
                
                int prev = indices[prevIndex];
                int curr = indices[i];
                int next = indices[nextIndex];
                
                Vector3 v0 = vertices[prev];
                Vector3 v1 = vertices[curr];
                Vector3 v2 = vertices[next];
                
                // Check if this forms a valid ear
                if (IsEar(vertices, indices, prevIndex, i, nextIndex, isClockwise))
                {
                    // Add triangle
                    if (isClockwise)
                    {
                        triangles.Add(prev);
                        triangles.Add(curr);
                        triangles.Add(next);
                    }
                    else
                    {
                        triangles.Add(prev);
                        triangles.Add(next);
                        triangles.Add(curr);
                    }
                    
                    // Remove the ear vertex
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            
            // Fallback if no ear found (shouldn't happen with valid polygons)
            if (!earFound)
            {
                Debug.LogWarning("No ear found in polygon triangulation. Breaking to avoid infinite loop.");
                break;
            }
        }
        
        // Add the last triangle
        if (indices.Count == 3)
        {
            if (isClockwise)
            {
                triangles.Add(indices[0]);
                triangles.Add(indices[1]);
                triangles.Add(indices[2]);
            }
            else
            {
                triangles.Add(indices[0]);
                triangles.Add(indices[2]);
                triangles.Add(indices[1]);
            }
        }
        
        return triangles;
    }
    
    bool IsEar(List<Vector3> vertices, List<int> indices, int prevIndex, int currIndex, int nextIndex, bool isClockwise)
    {
        int prev = indices[prevIndex];
        int curr = indices[currIndex];
        int next = indices[nextIndex];
        
        Vector3 v0 = vertices[prev];
        Vector3 v1 = vertices[curr];
        Vector3 v2 = vertices[next];
        
        // Check if the triangle is convex
        Vector3 cross = Vector3.Cross(v1 - v0, v2 - v1);
        bool isConvex = isClockwise ? cross.z < 0 : cross.z > 0;
        
        if (!isConvex) return false;
        
        // Check if any other vertex is inside this triangle
        for (int i = 0; i < indices.Count; i++)
        {
            if (i == prevIndex || i == currIndex || i == nextIndex) continue;
            
            Vector3 p = vertices[indices[i]];
            if (PointInTriangle(p, v0, v1, v2))
            {
                return false;
            }
        }
        
        return true;
    }
    
    bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = c - a;
        Vector3 v1 = b - a;
        Vector3 v2 = p - a;
        
        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);
        
        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;
        
        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }
    
    bool IsContourClosed(List<Vector2> contour)
    {
        if (contour.Count < 3) return false;
        return Vector2.Distance(contour[0], contour[contour.Count - 1]) < stepSize;
    }
    
    public void MergeNearbyContours(float mergeDistance = 1f)
    {
        List<List<Vector2>> mergedContours = new List<List<Vector2>>();
        HashSet<int> processed = new HashSet<int>();
        
        for (int i = 0; i < contours.Count; i++)
        {
            if (processed.Contains(i) || contours[i].Count == 0) continue;
            
            List<Vector2> merged = new List<Vector2>(contours[i]);
            processed.Add(i);
            
            bool foundMerge;
            do
            {
                foundMerge = false;
                for (int j = 0; j < contours.Count; j++)
                {
                    if (processed.Contains(j) || contours[j].Count == 0) continue;
                    
                    // Try to merge contours
                    List<Vector2> mergedResult = TryMergeContours(merged, contours[j], mergeDistance);
                    if (mergedResult != null)
                    {
                        merged = mergedResult;
                        processed.Add(j);
                        foundMerge = true;
                    }
                }
            } while (foundMerge);
            
            mergedContours.Add(merged);
        }
        
        contours = mergedContours;
    }
    
    List<Vector2> TryMergeContours(List<Vector2> c1, List<Vector2> c2, float maxDist)
    {
        if (c1.Count == 0 || c2.Count == 0) return null;
        
        Vector2 c1Start = c1[0];
        Vector2 c1End = c1[c1.Count - 1];
        Vector2 c2Start = c2[0];
        Vector2 c2End = c2[c2.Count - 1];
        
        List<Vector2> result = new List<Vector2>();
        
        if (Vector2.Distance(c1End, c2Start) < maxDist)
        {
            result.AddRange(c1);
            result.AddRange(c2);
            return result;
        }
        else if (Vector2.Distance(c1End, c2End) < maxDist)
        {
            result.AddRange(c1);
            for (int i = c2.Count - 1; i >= 0; i--)
                result.Add(c2[i]);
            return result;
        }
        else if (Vector2.Distance(c1Start, c2End) < maxDist)
        {
            result.AddRange(c2);
            result.AddRange(c1);
            return result;
        }
        else if (Vector2.Distance(c1Start, c2Start) < maxDist)
        {
            for (int i = c2.Count - 1; i >= 0; i--)
                result.Add(c2[i]);
            result.AddRange(c1);
            return result;
        }
        
        return null;
    }
}