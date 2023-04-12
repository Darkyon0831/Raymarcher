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
    public BlendFunc blendFunc = BlendFunc.Union;
    public BlendFunc blendWithParentFunc = BlendFunc.Union;
    public Shape[] shapes;
    public BlendContainer[] childContainers;
    public bool isSmoothBlend;
    public float smoothFactor;
    [HideInInspector] public int NOTUSEIndex;
}
