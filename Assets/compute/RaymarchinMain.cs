using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.VersionControl.Asset;

public class RaymarchinMain : MonoBehaviour
{
    struct ShapeData
    {
        public uint shapeType;
        public Vector3 pos;
        public Vector3 rot;
        public Vector3 scale;
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

    private int tempParentIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();

        renderTexture = new RenderTexture(1920, 1080, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        MainBlendContainer = GameObject.Find("Main Container").GetComponent<BlendContainer>();

        InitComputeBuffers();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        camPos.Set(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, 1.0f);

        UpdateComputeBuffer();
        raymarchingShader.SetBuffer(0, "Shapes", shapesComputeBuffer);
        raymarchingShader.SetInt("NumShapes", shapesData.Count);
        raymarchingShader.SetBuffer(0, "BlendContainers", blendContainersComputeBuffer);
        raymarchingShader.SetInt("NumBlendContainers", blendContainersData.Count);
        raymarchingShader.SetTexture(0, "Result", renderTexture);
        raymarchingShader.SetMatrix("CamToWorld", cam.cameraToWorldMatrix);
        raymarchingShader.SetMatrix("CamInverseProjection", cam.projectionMatrix.inverse);

        raymarchingShader.Dispatch(0, renderTexture.width / 6, renderTexture.height / 6, 1);

        Graphics.Blit(renderTexture, destination);
    }

    void InitShapesList(BlendContainer container)
    {
        for (int i = 0; i < container.shapes.Length; i++)
        {
            shapesData.Add(new ShapeData());
        }

        for (int i = 0; i < container.childContainers.Length; i++)
        {
            InitShapesList(container.childContainers[i]);
        }
    }

    void InitBlendContainersList(BlendContainer container)
    {
        blendContainersData.Add(new BlendContainerData());

        for (int i = 0; i < container.childContainers.Length; i++)
        {
            InitBlendContainersList(container.childContainers[i]);
        }
    }

    int UpdateShapes(BlendContainer container, int offset = 0)
    {
        int cOffset = offset;

        for (int i = 0; i < container.shapes.Length; i++)
        {
            int index = container.shapes.Length - 1 - i;

            ShapeData s = shapesData[cOffset + i];
            Shape cS = container.shapes[index];

            s.shapeType = (uint)cS.shapeType;
            s.pos = cS.transform.position;
            s.rot = cS.transform.rotation.eulerAngles;
            s.scale = cS.transform.localScale;
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

        cOffset += container.shapes.Length;
        tempParentIndex++;

        for (int i = 0; i < container.childContainers.Length; i++)
        {
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
        container.NOTUSEIndex = cOffset;

        blendContainersData[cOffset] = d;

        int cParentIndex = cOffset;
        cOffset++;

        for (int i = 0; i < container.childContainers.Length; i++)
        {
            int index = container.childContainers.Length - 1 - i;

            cOffset = UpdateBlendContainers(container.childContainers[index], cOffset, cParentIndex);
        }

        return cOffset;
    }

    void InitComputeBuffers()
    {
        InitBlendContainersList(MainBlendContainer);
        InitShapesList(MainBlendContainer);

        blendContainersComputeBuffer = new ComputeBuffer(blendContainersData.Count, Marshal.SizeOf<BlendContainerData>());
        shapesComputeBuffer = new ComputeBuffer(shapesData.Count, Marshal.SizeOf<ShapeData>());
    }

    void UpdateComputeBuffer()
    {
        tempParentIndex = 0;
        UpdateBlendContainers(MainBlendContainer);
        UpdateShapes(MainBlendContainer);

        blendContainersComputeBuffer.SetData(blendContainersData);
        shapesComputeBuffer.SetData(shapesData);
    }
}
