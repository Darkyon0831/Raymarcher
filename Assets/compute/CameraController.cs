using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float MovementSpeed;
    public float MouseSensitivity;

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * Input.GetAxis("Vertical") * MovementSpeed * Time.deltaTime;
        transform.position += transform.right * Input.GetAxis("Horizontal") * MovementSpeed * Time.deltaTime;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        transform.eulerAngles += new Vector3(-mouseY * MouseSensitivity, mouseX * MouseSensitivity, 0);
    }
}
