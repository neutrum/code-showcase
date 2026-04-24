using System;
using Meta.XR.MRUtilityKit;
using UnityEngine;

[Serializable]
public class ExtendedAnchorPrefabGroup
{
    public AnchorPrefabSpawner.AnchorPrefabGroup OriginalGroup;

    [SerializeField, Tooltip("Ignore axis when calculating scale or closest size")]
    public string NewProperty; // Your new property here

    // You can add methods to manipulate the original group if needed
}