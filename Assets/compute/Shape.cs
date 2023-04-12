using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeType
{
    Circle,
    Cube,
    Cylinder
}

public abstract class Shape : MonoBehaviour
{
    public Color color = Color.white;

    public abstract ShapeType GetShapeType();
}
