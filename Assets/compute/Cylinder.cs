using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cylinder : Shape
{
    public Vector2 h = Vector2.zero;

    public override ShapeType GetShapeType()
    {
        return ShapeType.Cylinder;
    }
}
