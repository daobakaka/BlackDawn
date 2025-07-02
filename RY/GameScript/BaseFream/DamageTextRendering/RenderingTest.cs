using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class RenderingTest : MonoBehaviour
{

    [Header("目标数量 & 范围")]
    public int targetCount = 1000;
    public Vector3 bounds = new Vector3(10, 10, 10);

    [Header("更新频率（秒）")]
    public float updateInterval = 1.0f;

    private VisualEffect vfx;
    private GraphicsBuffer targetBuffer;
    private Vector3[] targetPositions;
    private float timer;

    public Transform testTransform;

    //private  readonly int TargetBufferID = Shader.PropertyToID("_LinkedTargets");

    

    void Start()
    {

        DevDebug.LogError("!!!!批量GPUBuffer渲染逻辑");
        vfx = GetComponent<VisualEffect>();

        

        DevDebug.LogError("名称"+vfx.initialEventName);


        GenerateRandomTargets();
        UploadToVFX();
        DevDebug.LogError(targetPositions[10].x + "y" + targetPositions[10].y + "Z" + targetPositions[10].z);


    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {


            DevDebug.LogError("开始更新批量GPUBuffer渲染逻辑");
            GenerateRandomTargets();
            UploadToVFX();
            timer = 0f;
        }


        //vfx.SetVector3("_Target1",testTransform.position);
    }

    void GenerateRandomTargets()
    {

        // 初始化 buffer 和数据
        targetPositions = new Vector3[targetCount];


        targetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, targetCount, sizeof(float) * 3);

        for (int i = 0; i < targetCount; i++)
        {
            targetPositions[i] = new Vector3(
                Random.Range(-bounds.x, bounds.x),
                Random.Range(-bounds.y, bounds.y),
                Random.Range(-bounds.z, bounds.z)
            );
        }
    }

    void UploadToVFX()
    {
        vfx.SendEvent("Custom4");
        vfx.SetInt("_LinkedTargetsCount", targetCount);
        targetBuffer.SetData(targetPositions);
        vfx.SetGraphicsBuffer("_LinkedTargets", targetBuffer);
    }

    void OnDestroy()
    {
        if (targetBuffer != null)
            targetBuffer.Dispose();
    }
}


