using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circle : Shape
{
    public float radius = 0;

    public override ShapeType GetShapeType()
    {
        return ShapeType.Circle;
    }
}
