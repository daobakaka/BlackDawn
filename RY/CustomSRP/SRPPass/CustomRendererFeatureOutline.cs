using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public enum CustomRenderQueueOption
{
    Opaque,
    Transparent
}
/// <summary>
/// ���
/// </summary>
public class CustomRendererFeatureOutline : ScriptableRendererFeature
{
    // ����һ���Զ���ö��,ѡ��͸����͸������Ⱦ����   
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public CustomRenderQueueOption renderQueueOption = CustomRenderQueueOption.Transparent;
        public string passName;
    
    CustomRendererPassOutline _customPass;

    public override void Create()
    {
        // ����ö��ѡ��ת��Ϊ RenderQueueRange
        RenderQueueRange range = (renderQueueOption == CustomRenderQueueOption.Opaque) ?
                                    RenderQueueRange.opaque : RenderQueueRange.transparent;
        _customPass = new CustomRendererPassOutline(passName, renderPassEvent, range);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_customPass);
    }
}



