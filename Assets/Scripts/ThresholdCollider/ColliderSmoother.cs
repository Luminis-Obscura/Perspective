using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implements smoothing algorithms for EdgeCollider2D points to create straight, high-quality collision surfaces.
/// </summary>
public class ColliderSmoother : MonoBehaviour
{
    [Tooltip("The EdgeCollider2D to smooth.")]
    public EdgeCollider2D targetCollider;

    [Tooltip("The smoothing method to use.")]
    public SmoothingMethod smoothingMethod = SmoothingMethod.DouglasPeucker;

    [Tooltip("The tolerance for the Douglas-Peucker algorithm (higher values = more simplification).")]
    [Range(0.01f, 5.0f)]
    public float douglasPeuckerTolerance = 0.1f;

    [Tooltip("The angle threshold for the angle-based smoothing (in degrees).")]
    [Range(0.1f, 45.0f)]
    public float angleThreshold = 5.0f;

    [Tooltip("The distance threshold for the angle-based smoothing.")]
    [Range(0.01f, 5.0f)]
    public float distanceThreshold = 0.1f;

    /// <summary>
    /// Enumeration of available smoothing methods.
    /// </summary>
    public enum SmoothingMethod
    {
        DouglasPeucker,
        AngleBased,
        Combined
    }

    /// <summary>
    /// Smooths the target EdgeCollider2D using the selected method.
    /// </summary>
    public void SmoothCollider()
    {
        if (targetCollider == null)
        {
            Debug.LogError("Target collider is not assigned!");
            return;
        }

        // Get the current points
        Vector2[] points = targetCollider.points;
        
        // Apply the selected smoothing method
        Vector2[] smoothedPoints;
        switch (smoothingMethod)
        {
            case SmoothingMethod.DouglasPeucker:
                smoothedPoints = ApplyDouglasPeucker(points);
                break;
            case SmoothingMethod.AngleBased:
                smoothedPoints = ApplyAngleBasedSmoothing(points);
                break;
            case SmoothingMethod.Combined:
                smoothedPoints = ApplyCombinedSmoothing(points);
                break;
            default:
                smoothedPoints = points;
                break;
        }

        // Update the collider with the smoothed points
        targetCollider.points = smoothedPoints;
        
        Debug.Log($"Smoothed collider from {points.Length} to {smoothedPoints.Length} points.");
    }

    /// <summary>
    /// Smooths all EdgeCollider2D components in the scene using the selected method.
    /// </summary>
    public void SmoothAllColliders()
    {
        EdgeCollider2D[] colliders = FindObjectsOfType<EdgeCollider2D>();
        foreach (EdgeCollider2D collider in colliders)
        {
            targetCollider = collider;
            SmoothCollider();
        }
        
        Debug.Log($"Smoothed {colliders.Length} colliders.");
    }

    /// <summary>
    /// Applies the Douglas-Peucker algorithm to simplify a set of points.
    /// </summary>
    private Vector2[] ApplyDouglasPeucker(Vector2[] points)
    {
        if (points.Length <= 2)
            return points;

        List<Vector2> pointsList = new List<Vector2>(points);
        List<Vector2> result = new List<Vector2>();
        
        // Apply the Douglas-Peucker algorithm
        DouglasPeuckerRecursive(pointsList, 0, pointsList.Count - 1, douglasPeuckerTolerance, result);
        
        // Sort the result by the original index
        result.Sort((a, b) => pointsList.IndexOf(a).CompareTo(pointsList.IndexOf(b)));
        
        return result.ToArray();
    }

    /// <summary>
    /// Recursive implementation of the Douglas-Peucker algorithm.
    /// </summary>
    private void DouglasPeuckerRecursive(List<Vector2> points, int startIndex, int endIndex, float epsilon, List<Vector2> result)
    {
        if (endIndex <= startIndex + 1)
        {
            // Add the start and end points
            if (!result.Contains(points[startIndex]))
                result.Add(points[startIndex]);
            if (endIndex > startIndex && !result.Contains(points[endIndex]))
                result.Add(points[endIndex]);
            return;
        }

        // Find the point with the maximum distance from the line segment
        float maxDistance = 0;
        int maxIndex = startIndex;
        
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            float distance = PerpendicularDistance(points[i], points[startIndex], points[endIndex]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                maxIndex = i;
            }
        }

        // If the maximum distance is greater than epsilon, recursively simplify
        if (maxDistance > epsilon)
        {
            DouglasPeuckerRecursive(points, startIndex, maxIndex, epsilon, result);
            DouglasPeuckerRecursive(points, maxIndex, endIndex, epsilon, result);
        }
        else
        {
            // Add the start and end points
            if (!result.Contains(points[startIndex]))
                result.Add(points[startIndex]);
            if (!result.Contains(points[endIndex]))
                result.Add(points[endIndex]);
        }
    }

    /// <summary>
    /// Calculates the perpendicular distance from a point to a line segment.
    /// </summary>
    private float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        if (lineStart == lineEnd)
            return Vector2.Distance(point, lineStart);

        // Calculate the perpendicular distance
        float area = Mathf.Abs(0.5f * (lineStart.x * (lineEnd.y - point.y) + 
                                       lineEnd.x * (point.y - lineStart.y) + 
                                       point.x * (lineStart.y - lineEnd.y)));
        float bottom = Mathf.Sqrt(Mathf.Pow(lineEnd.x - lineStart.x, 2) + 
                                 Mathf.Pow(lineEnd.y - lineStart.y, 2));
        
        return area / bottom * 2;
    }

    /// <summary>
    /// Applies angle-based smoothing to a set of points.
    /// </summary>
    private Vector2[] ApplyAngleBasedSmoothing(Vector2[] points)
    {
        if (points.Length <= 2)
            return points;

        List<Vector2> result = new List<Vector2>();
        result.Add(points[0]); // Always include the first point

        for (int i = 1; i < points.Length - 1; i++)
        {
            Vector2 prev = points[i - 1];
            Vector2 current = points[i];
            Vector2 next = points[i + 1];

            // Calculate the angle between the previous-current and current-next segments
            Vector2 dir1 = (current - prev).normalized;
            Vector2 dir2 = (next - current).normalized;
            float angle = Vector2.Angle(dir1, dir2);

            // Calculate the distance between points
            float distPrev = Vector2.Distance(prev, current);
            float distNext = Vector2.Distance(current, next);

            // Include the point if the angle is significant or the distance is large
            if (angle > angleThreshold || distPrev > distanceThreshold || distNext > distanceThreshold)
            {
                result.Add(current);
            }
        }

        result.Add(points[points.Length - 1]); // Always include the last point
        return result.ToArray();
    }

    /// <summary>
    /// Applies a combination of Douglas-Peucker and angle-based smoothing.
    /// </summary>
    private Vector2[] ApplyCombinedSmoothing(Vector2[] points)
    {
        // First apply Douglas-Peucker to get a simplified set of points
        Vector2[] simplifiedPoints = ApplyDouglasPeucker(points);
        
        // Then apply angle-based smoothing for further refinement
        return ApplyAngleBasedSmoothing(simplifiedPoints);
    }
}
