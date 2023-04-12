using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.VersionControl.Asset;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RaymarchinMain : MonoBehaviour
{
    struct ShapeData
    {
        public uint shapeType;
        public Vector3 pos;
        public Matrix4x4 inverseRotMatrix;
        public Matrix4x4 inverseScaleMatrix;
        public Vector4 color;
        public Vector3 metadata;
        public uint parentIndex;
    }

    struct BlendContainerData
    {
        public uint blendFunc;
        public uint parentBlendFunc;
        public uint numChilds;
        public uint numShapes;
        public uint parentIndex;
        public uint isSmoothBlend;
        public float smoothFactor;
    }

    public ComputeShader raymarchingShader;
    public RenderTexture renderTexture;
    public BlendContainer MainBlendContainer = null;

    private ComputeBuffer shapesComputeBuffer;
    private ComputeBuffer blendContainersComputeBuffer;
    private List<ShapeData> shapesData = new List<ShapeData>();
    private List<BlendContainerData> blendContainersData = new List<BlendContainerData>();
    private Camera cam;
    private Vector4 camPos = Vector4.zero;

    private int tempCurrentBlends;
    private int tempCurrentShapes;

    private const int MAX_BLEND_CONTAINERS = 25;
    private const int START_SHAPE_COUNT = 50;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();

        renderTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        InitComputeBuffers();
    }

    void OnResize(RenderTexture source)
    {
        if (renderTexture != null && renderTexture.width != source.width && renderTexture.height != source.height)
        {
            renderTexture.Release();
            renderTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        camPos.Set(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1.0f);

        OnResize(source);

        UpdateComputeBuffer();
        raymarchingShader.SetBuffer(0, "Shapes", shapesComputeBuffer);
        raymarchingShader.SetInt("NumShapes", tempCurrentShapes);
        raymarchingShader.SetBuffer(0, "BlendContainers", blendContainersComputeBuffer);
        raymarchingShader.SetInt("NumBlendContainers", tempCurrentBlends);
        raymarchingShader.SetTexture(0, "Result", renderTexture);
        raymarchingShader.SetTexture(0, "Source", source);
        raymarchingShader.SetMatrix("CamToWorld", cam.cameraToWorldMatrix);
        raymarchingShader.SetMatrix("CamInverseProjection", cam.projectionMatrix.inverse);

        raymarchingShader.Dispatch(0, renderTexture.width / 6, renderTexture.height / 6, 1);

        Graphics.Blit(renderTexture, destination);
    }

    private void OnDestroy()
    {
        shapesComputeBuffer.Release();
        blendContainersComputeBuffer.Release();
    }

    Matrix4x4 GetRotMatrix(Quaternion q)
    {
        return Matrix4x4.Rotate(q);
    }

    Matrix4x4 GetScaleMatrix(Vector3 localScale)
    {
        return Matrix4x4.Scale(localScale);
    }

    void InitShapesList()
    {
        for (int i = 0; i < START_SHAPE_COUNT; i++)
        {
            shapesData.Add(new ShapeData());
        }
    }

    void InitBlendContainersList()
    {
        for (int i = 0; i < MAX_BLEND_CONTAINERS; i++)
        {
            blendContainersData.Add(new BlendContainerData());
        }
    }

    void ResizeShapes()
    {
        for (int i = 0; i < shapesData.Count; i++)
        {
            shapesData.Add(new ShapeData());
        }

        shapesComputeBuffer.Release();
        shapesComputeBuffer = new ComputeBuffer(shapesData.Count, Marshal.SizeOf<ShapeData>());
    }

    int UpdateShapes(BlendContainer container, int offset = 0)
    {
        int cOffset = offset;

        for (int i = 0; i < container.shapes.Length; i++)
        {
            if (container.shapes[i] != null)
            {
                int index = container.shapes.Length - 1 - i;

                if (cOffset + i > shapesData.Count)
                    ResizeShapes();

                ShapeData s = shapesData[cOffset + i];
                Shape cS = container.shapes[index];
                tempCurrentShapes++;

                s.shapeType = (uint)cS.shapeType;
                s.pos = cS.transform.position;
                s.inverseRotMatrix = GetRotMatrix(cS.transform.rotation).inverse;
                s.inverseScaleMatrix = GetScaleMatrix(cS.transform.localScale).inverse;
                s.color = cS.color;
                s.parentIndex = (uint)container.NOTUSEIndex;

                if (cS.shapeType == ShapeType.Circle)
                    s.metadata.x = ((Circle)cS).radius;
                else if (cS.shapeType == ShapeType.Cube)
                {
                    s.metadata.x = ((Cube)cS).size.x;
                    s.metadata.y = ((Cube)cS).size.y;
                    s.metadata.z = ((Cube)cS).size.z;
                }
                else if (cS.shapeType == ShapeType.Cylinder)
                {
                    s.metadata.x = ((Cylinder)cS).h.x;
                    s.metadata.y = ((Cylinder)cS).h.y;
                }

                shapesData[cOffset + i] = s;
            }
        }

        cOffset += container.shapes.Length;

        for (int i = 0; i < container.childContainers.Length; i++)
        {
            if (container.childContainers[i] != null)
                cOffset = UpdateShapes(container.childContainers[i], cOffset);
        }

        return cOffset;
    }

    int UpdateBlendContainers(BlendContainer container, int offset = 0, int parentIndex = 0)
    {
        int cOffset = offset;

        BlendContainerData d = blendContainersData[cOffset];

        d.blendFunc = (uint)container.blendFunc;
        d.parentBlendFunc = (uint)container.blendWithParentFunc;
        d.numChilds = (uint)container.childContainers.Length;
        d.numShapes = (uint)container.shapes.Length;
        d.parentIndex = (uint)parentIndex;
        d.isSmoothBlend = (uint)(container.isSmoothBlend ? 1 : 0);
        d.smoothFactor = container.smoothFactor;
        container.NOTUSEIndex = cOffset;

        blendContainersData[cOffset] = d;

        int cParentIndex = cOffset;
        cOffset++;
        tempCurrentBlends++;

        for (int i = 0; i < container.childContainers.Length; i++)
        {
            if (container.childContainers[i] != null)
            {
                int index = container.childContainers.Length - 1 - i;

                cOffset = UpdateBlendContainers(container.childContainers[index], cOffset, cParentIndex);
            }
        }

        return cOffset;
    }

    void InitComputeBuffers()
    {
        InitBlendContainersList();
        InitShapesList();

        blendContainersComputeBuffer = new ComputeBuffer(MAX_BLEND_CONTAINERS, Marshal.SizeOf<BlendContainerData>());
        shapesComputeBuffer = new ComputeBuffer(START_SHAPE_COUNT, Marshal.SizeOf<ShapeData>());
    }

    void UpdateComputeBuffer()
    {
        tempCurrentBlends = 0;
        tempCurrentShapes = 0;
        UpdateBlendContainers(MainBlendContainer);
        UpdateShapes(MainBlendContainer);

        blendContainersComputeBuffer.SetData(blendContainersData);
        shapesComputeBuffer.SetData(shapesData);
    }
}
