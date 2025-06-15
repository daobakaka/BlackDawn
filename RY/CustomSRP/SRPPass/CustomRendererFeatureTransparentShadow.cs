using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
/// <summary>
/// 透视阴影
/// </summary>
public class CustomRendererFeatureTransparentShadow : ScriptableRendererFeature
{


    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    public CustomRenderQueueOption renderQueueOption = CustomRenderQueueOption.Transparent;
    public string passName;

    CustomRendererPassTransparentShadow _customPass;

    public override void Create()
    {
        // 根据枚举选择转换为 RenderQueueRange
        RenderQueueRange range = (renderQueueOption == CustomRenderQueueOption.Opaque) ?
                                    RenderQueueRange.opaque : RenderQueueRange.transparent;
        _customPass = new CustomRendererPassTransparentShadow(passName, renderPassEvent, range);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_customPass);
    }
}
