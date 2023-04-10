using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class RaymarchinMain : MonoBehaviour
{
    public ComputeShader raymarchingShader;
    public RenderTexture renderTexture;
    public Vector3 objectPos = Vector3.zero;
    public Vector3 objectRot = Vector3.zero;
    public float RotSpeed = 0.0f;

    private Camera cam;
    private Vector4 camPos = Vector4.zero;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();

        renderTexture = new RenderTexture(1920, 1080, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
    }

    Matrix4x4 GetRotMatrix(Vector3 angles)
    {
        Matrix4x4 rotMatrix = Matrix4x4.Rotate(Quaternion.Euler(angles.x, angles.y, angles.z));

        return rotMatrix;
    }

    void AnimRotate()
    {
        objectRot.x += RotSpeed * Time.deltaTime;
        objectRot.y += RotSpeed * Time.deltaTime;
        objectRot.z += RotSpeed * Time.deltaTime;

        if (objectRot.x > 360.0f)
            objectRot.x -= 360.0f;

        if (objectRot.y > 360.0f)
            objectRot.y -= 360.0f;

        if (objectRot.z > 360.0f)
            objectRot.z -= 360.0f;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        AnimRotate();

        camPos.Set(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1.0f);

        raymarchingShader.SetTexture(0, "Result", renderTexture);
        raymarchingShader.SetMatrix("CamToWorld", cam.cameraToWorldMatrix);
        raymarchingShader.SetMatrix("CamInverseProjection", cam.projectionMatrix.inverse);
        raymarchingShader.SetMatrix("ObjectRotateInverseMatrix", GetRotMatrix(objectRot).inverse);

        raymarchingShader.SetVector("ObjectPos", objectPos);

        raymarchingShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);

        Graphics.Blit(renderTexture, destination);
    }
}
