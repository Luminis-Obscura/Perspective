using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

/// <summary>
/// Interface for interactable object.
/// </summary>
public interface Interactable {
    /// <summary>
    /// Use this to store wetaher the object is interactable.
    /// </summary>
    bool isInteractable {
        get;
        set;
    }

    Outline outline {
        get;
        set;
    }

    /// <summary>
    /// Is called when object is intreacted with.
    /// </summary>
    /// <param name="isThreatened"> Indicate if is threatened </param>
    void OnInteract() {
        Debug.LogWarning("Interact() not implemented");
    }

    void OnStareEnter() {
        Debug.LogWarning("StareEnter() not implemented");
    }

    /// <summary>
    /// Is called when object is stared at.
    /// </summary>
    void OnStare() {
        Debug.LogWarning("Stared() not implemented");
    }

    /// <summary>
    /// Is called when object is no longer stared at.
    /// </summary>
    void OnStareExit() {
        Debug.LogWarning("StareExit() not implemented");
    }
}