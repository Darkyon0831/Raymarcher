using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlendFunc
{
    Union,
    Intersection,
    Difference
}

public class BlendContainer : MonoBehaviour
{
    [Space(5)]
    [Header("Blend Functions")]
    public BlendFunc blendFunc = BlendFunc.Union;
    public BlendFunc blendWithParentFunc = BlendFunc.Union;

    [Space(5)]
    [Header("Scene")]
    public Shape[] shapes;
    public BlendContainer[] childContainers;

    [Space(5)]
    [Header("Smooth Blend")]
    public bool isSmoothBlend;

    [Range(0, 1)]
    public float smoothFactor;

    public bool isParentSmoothBlend;
   
    [Range(0, 1)]
    public float parentSmoothFactor;
    [HideInInspector] public int NOTUSEIndex;
}
