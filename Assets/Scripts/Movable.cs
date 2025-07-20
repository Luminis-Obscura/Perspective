using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Movable : MonoBehaviour, Interactable
{
    [SerializeField] private bool _isInteractable = true;

    private new Collider collider;
    private Outline _outline;
    private MeshRenderer meshRenderer;

    [HideInInspector]
    public bool _isTrigger;
    [HideInInspector]
    public LayerMask _layerMask;
    [HideInInspector]
    public Vector3 avoidanceFlags = Vector3.zero; // New: 0 allows both directions, -1 disallows negative, +1 disallows positive

    private Color originalOutlineColor;
    private bool originalColliderTrigger;
    private int originalLayer;
    private bool originalColliderEnabled;
    private Vector3 originalPosition;
    private Vector3 offset;

    private bool isMovingInitialized = false;
    // public InputSystemActions playerInputs; // TODO: Implement InputSystemActions
    private InputAction interact;
    public float lerpSpeed = 10f; // Speed of interpolation for smooth movement

    public List<Movable> neighborMovable = new List<Movable>();

    public Outline outline
    {
        get { return _outline; }
        set { _outline = value; }
    }

    public bool isInteractable
    {
        get { return _isInteractable; }
        set { _isInteractable = value; }
    }

    public bool _isBeingMoved = false;
    public bool isBeingMoved
    {
        get { return _isBeingMoved; }
        set
        {
            if (_isBeingMoved != value)
            {
                _isBeingMoved = value;
                if (_isBeingMoved)
                {
                    StartMoving();
                }
                else
                {
                    StopMoving();
                }
            }
        }
    }

    private Transform playerTransform;
    private Quaternion initialRotationOffset;

    // Add the new public Transform
    public Transform overwriteTransform;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        collider = GetComponent<Collider>();
        _isTrigger = collider.isTrigger;
        outline = gameObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }
        // playerInputs = new InputSystemActions(); // TODO: Implement InputSystemActions

        // Save original states
        originalOutlineColor = outline.OutlineColor;
        originalColliderTrigger = collider.isTrigger;
        originalLayer = gameObject.layer;
        originalColliderEnabled = collider.enabled;
        originalPosition = transform.position;

        // Get the player's transform (assuming the player has the tag "Player")
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        Vector3 meshCenter = CalculateMeshCenter();
        offset = transform.position - meshCenter;
    }

    void OnEnable()
    {
        InteractEnable();
    }

    void OnDisable()
    {
        InteractDisable();
    }

    void Update()
    {
        if (isBeingMoved)
        {
            // Vector3 targetPosition = MovableDetector.Instance.placePosition; // TODO: Implement MovableDetector
            Vector3 targetPosition = Vector3.zero;

            // Always move the object to the target position
            Vector3 adjustedPosition;
            Quaternion adjustedRotation = playerTransform.rotation * initialRotationOffset;

            // if (MovableDetector.Instance.normal == Vector3.zero) // TODO: Implement MovableDetector
            if (true)
            {
                adjustedPosition = targetPosition;
            }
            else
            {
                // Valid hit, perform collision avoidance
                adjustedPosition = GetAdjustedPosition(targetPosition);
            }

            if (overwriteTransform != null)
            {
                // Snap to overwriteTransform's position and rotation
                adjustedPosition = overwriteTransform.position;
                adjustedRotation = overwriteTransform.rotation;
                Vector3 overridePosition = overwriteTransform.position;
                // if (Vector3.Distance(transform.position, overridePosition) < 0.1f)
                // {
                //     MovableDetector.Instance.isPlaceable = true; // TODO: Implement MovableDetector
                // }
                Debug.Log("SnapController: Snapping to target");   
            }

            // Lerp towards the adjusted target position
            transform.position = Vector3.Lerp(transform.position, adjustedPosition, lerpSpeed * Time.deltaTime);

            // Update the object's rotation to follow the player 
            transform.rotation = Quaternion.Lerp(transform.rotation, adjustedRotation, lerpSpeed * Time.deltaTime);

            // Update the outline color based on the isPlaceable state
            // if (MovableDetector.Instance.isPlaceable) // TODO: Implement MovableDetector
            // {
            //     outline.OutlineColor = new Color(0.4196f, 0.8706f, 0.4392f); // Green color
            // }
            // else
            // {
            //     outline.OutlineColor = Color.red;
            // }
        }
    }

    public virtual void OnInteractCallback(InputAction.CallbackContext context)
    {
        if (isBeingMoved && this.enabled)
        {
            // Try to place the object
            // if (MovableDetector.Instance.isPlaceable) // TODO: Implement MovableDetector
            // {
            //     // Place the object and stop moving
                isBeingMoved = false;
            // }
        }
    }

    public void OnStareEnter()
    {
        // Optional implementation
    }

    public void OnInteract()
    {
        if (!isBeingMoved && this.enabled)
        {
            // Start moving the object
            isBeingMoved = true;
        }
    }

    public void OnStare()
    {
        // Since this can be called via interface, thus bypassing rendering loop, we need to check if the component is enabled
        if (this.enabled && !isBeingMoved)
        {
            outline.enabled = true;
        }
    }

    public void OnStareExit()
    {
        // Since this can be called via interface, thus bypassing rendering loop, we need to check if the component is enabled
        if (this.enabled && !isBeingMoved)
        {
            outline.enabled = false;
        }
    }

    public virtual void StartMoving()
    {
        if (!isMovingInitialized)
        {
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // PlayerManager.Instance.inventory = gameObject; // TODO: Implement PlayerManager
            ChangeLayer("Overlay");
            outline.enabled = true;
            collider.isTrigger = true; // Set collider to trigger during movement
            isMovingInitialized = true;
            // Destroy(gameObject.GetComponent<FallingController>()); // TODO: Implement FallingController
            InteractEnable();

            if (neighborMovable.Count > 0)
            {
                foreach (Movable movable in neighborMovable)
                {
                    // movable.gameObject.AddComponent<FallingController>(); // TODO: Implement FallingController
                }
            }
            overwriteTransform = null; // Reset overwriteTransform when movement starts

            // Compute the initial rotation offset
            initialRotationOffset = Quaternion.Inverse(playerTransform.rotation) * transform.rotation;
        }
    }

    public virtual void StopMoving()
    {
        if (meshRenderer != null)
        {
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        // Restore original states
        // PlayerManager.Instance.inventory = null; // TODO: Implement PlayerManager
        ChangeLayer("Default");
        outline.OutlineColor = originalOutlineColor;
        outline.enabled = false;
        collider.isTrigger = false;
        collider.enabled = true;
        isMovingInitialized = false;
        OnStopMoving();
        InteractDisable();
        overwriteTransform = null; // Reset overwriteTransform when movement stops
    }

    public virtual void OnStopMoving()
    {
        // gameObject.AddComponent<FallingController>(); // TODO: Implement FallingController
    }

    private Vector3 GetAdjustedPosition(Vector3 targetPosition)
    {
        Collider objectCollider = GetComponent<Collider>();
        if (objectCollider != null)
        {
            // Get the normal of the hit surface
            // Vector3 surfaceNormal = MovableDetector.Instance.normal; // TODO: Implement MovableDetector
            Vector3 surfaceNormal = Vector3.up;

            if (surfaceNormal == Vector3.zero)
            {
                // No valid surface normal, skip collision avoidance
                // MovableDetector.Instance.isPlaceable = false; // TODO: Implement MovableDetector
                return targetPosition;
            }

            // Ensure the normal is normalized
            surfaceNormal = surfaceNormal.normalized;

            // Apply avoidance flags
            if (!IsDirectionAllowed(surfaceNormal))
            {
                // Modify the normal based on avoidance flags
                surfaceNormal = AdjustNormalBasedOnAvoidance(surfaceNormal);
            }

            // Draw the surface normal
            Debug.DrawRay(targetPosition, surfaceNormal, Color.magenta);

            // Get the collider's bounds extents
            Vector3 boundsExtents = objectCollider.bounds.extents;

            // Compute the extent along the normal
            float extentAlongNormal =
                Mathf.Abs(boundsExtents.x * surfaceNormal.x) +
                Mathf.Abs(boundsExtents.y * surfaceNormal.y) +
                Mathf.Abs(boundsExtents.z * surfaceNormal.z);

            // Adjust the target position along the normal to prevent sinking into the surface
            targetPosition += surfaceNormal * extentAlongNormal;

            Vector3 objectSize = objectCollider.bounds.size;
            Vector3 halfSize = objectSize / 2f;
            int maxIterations = 10; // Maximum number of adjustments
            int iterations = 0;
            float checkDistance = 0.1f; // Distance to move along normal each iteration

            while (IsColliding(targetPosition - offset, halfSize) && iterations < maxIterations)
            {
                // Move the target position along the normal by checkDistance
                targetPosition += surfaceNormal * checkDistance;
                iterations++;
            }

            // If the object is still colliding, set isPlaceable to false
            if (IsColliding(targetPosition, halfSize))
            {
                // Perform another check with a small tolerance
                Vector3 smallHalfSize = halfSize * 0.9f;

                // Allow placement if the object is near overrideTransform

                if (IsColliding(targetPosition, smallHalfSize))
                {
                    // MovableDetector.Instance.isPlaceable = false; // TODO: Implement MovableDetector
                }
                else
                {
                    // MovableDetector.Instance.isPlaceable = true; // TODO: Implement MovableDetector
                }
            }
            else
            {
                // MovableDetector.Instance.isPlaceable = true; // TODO: Implement MovableDetector
            }
        }

        // Determine whether to add or subtract the offset based on orientation
        float upDot = Vector3.Dot(transform.up, Vector3.up);
        Vector3 finalPosition = upDot >= 0 ? targetPosition + offset : targetPosition - offset;
        // Vector3 finalPosition = targetPosition + offset;
        // Optional: Add debug log
        // Debug.Log($"Object orientation upDot: {upDot}. Final Position: {finalPosition}");

        return finalPosition;
    }

    /// <summary>
    /// Checks if the movement direction based on the surface normal is allowed.
    /// </summary>
    /// <param name="normal">The surface normal.</param>
    /// <returns>True if allowed, false otherwise.</returns>
    private bool IsDirectionAllowed(Vector3 normal)
    {
        // Check each axis based on avoidanceFlags
        if (normal.x < 0 && avoidanceFlags.x == -1)
            return false;
        if (normal.x > 0 && avoidanceFlags.x == 1)
            return false;

        if (normal.y < 0 && avoidanceFlags.y == -1)
            return false;
        if (normal.y > 0 && avoidanceFlags.y == 1)
            return false;

        if (normal.z < 0 && avoidanceFlags.z == -1)
            return false;
        if (normal.z > 0 && avoidanceFlags.z == 1)
            return false;

        return true;
    }

    /// <summary>
    /// Adjusts the surface normal based on the avoidance flags.
    /// Disallows movement in specified directions by zeroing out the corresponding component.
    /// </summary>
    /// <param name="normal">The original surface normal.</param>
    /// <returns>The adjusted surface normal.</returns>
    private Vector3 AdjustNormalBasedOnAvoidance(Vector3 normal)
    {
        Vector3 adjustedNormal = normal;

        // Zero out the components based on avoidanceFlags
        if (normal.x < 0 && avoidanceFlags.x == -1)
            adjustedNormal.x = 0;
        if (normal.x > 0 && avoidanceFlags.x == 1)
            adjustedNormal.x = 0;

        if (normal.y < 0 && avoidanceFlags.y == -1)
            adjustedNormal.y = 0;
        if (normal.y > 0 && avoidanceFlags.y == 1)
            adjustedNormal.y = 0;

        if (normal.z < 0 && avoidanceFlags.z == -1)
            adjustedNormal.z = 0;
        if (normal.z > 0 && avoidanceFlags.z == 1)
            adjustedNormal.z = 0;

        // Normalize the adjusted normal to maintain direction
        if (adjustedNormal != Vector3.zero)
        {
            adjustedNormal = adjustedNormal.normalized;
        }

        return adjustedNormal;
    }

    private bool IsColliding(Vector3 position, Vector3 halfSize)
    {
        // Check for any colliders using OverlapBox
        Collider[] colliders = Physics.OverlapBox(position, halfSize, Quaternion.identity);

        // Exclude this object's own collider
        foreach (Collider c in colliders)
        {
            if (c != collider && !c.gameObject.CompareTag("AvoidanceIgnore"))
            {
                return true;
            }
        }

        return false;
    }

    void InteractEnable()
    {
        // interact = playerInputs.Player.Interact; // TODO: Implement InputSystemActions
        // interact.Enable();
        // interact.performed += OnInteractCallback;
    }

    void InteractDisable()
    {
        // interact.performed -= OnInteractCallback; // TODO: Implement InputSystemActions
        // interact.Disable();
    }

    public virtual void OnDrawGizmos()
    {
        if (isBeingMoved)
        {
            // Optional: Draw a gizmo to show where the overlap box is checking
            Gizmos.color = Color.red;
            Vector3 objectSize = collider != null ? collider.bounds.size : Vector3.one;
            // Gizmos.DrawWireCube(MovableDetector.Instance.placePosition - offset, objectSize); // TODO: Implement MovableDetector
        }
    }

    public void ChangeLayer(string name)
    {
        gameObject.layer = LayerMask.NameToLayer(name);
        foreach (Transform child in transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer(name);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Movable movable = collision.gameObject.GetComponent<Movable>();
        if (movable != null)
        {
            neighborMovable.Add(movable);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        Movable movable = collision.gameObject.GetComponent<Movable>();
        if (movable != null)
        {
            neighborMovable.Remove(movable);
        }
    }

    Vector3 CalculateMeshCenter()
    {
        Vector3 center = Vector3.zero;
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            center += meshFilter.transform.TransformPoint(meshFilter.mesh.bounds.center);
        }

        center /= meshFilters.Length;
        return center;
    }
}