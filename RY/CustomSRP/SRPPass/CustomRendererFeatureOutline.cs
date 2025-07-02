using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public enum CustomRenderQueueOption
{
    Opaque,
    Transparent
}
/// <summary>
/// 描边
/// </summary>
public class CustomRendererFeatureOutline : ScriptableRendererFeature
{
    // 定义一个自定义枚举,选择透明或不透明的渲染队列   
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public CustomRenderQueueOption renderQueueOption = CustomRenderQueueOption.Transparent;
        public string passName;
    
    CustomRendererPassOutline _customPass;

    public override void Create()
    {
        // 根据枚举选择转换为 RenderQueueRange
        RenderQueueRange range = (renderQueueOption == CustomRenderQueueOption.Opaque) ?
                                    RenderQueueRange.opaque : RenderQueueRange.transparent;
        _customPass = new CustomRendererPassOutline(passName, renderPassEvent, range);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_customPass);
    }
}



