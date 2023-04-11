using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeType
{
    Circle,
    Cube,
    Cylinder
}

public class Shape : MonoBehaviour
{
    public ShapeType shapeType = ShapeType.Circle;
    public Color color = Color.white;
}
