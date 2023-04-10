using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

public class RotateAroundPoint : MonoBehaviour
{
    public Vector3 RotatePoint = Vector3.zero;
    public float RotateSpeed = 1.0f;

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(RotatePoint, new Vector3(0.0f, 1.0f, 0.0f), RotateSpeed * Time.deltaTime);
    }
}
