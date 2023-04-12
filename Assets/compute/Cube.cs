using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : Shape
{
    public Vector3 size = Vector2.zero;

    public override ShapeType GetShapeType()
    {
        return ShapeType.Cube;
    }
}
