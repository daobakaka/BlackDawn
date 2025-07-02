using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
/// <summary>
/// 描边
/// </summary>
public class CustomRendererPassOutline : ScriptableRenderPass
{
    private string _profilerTag;
    private FilteringSettings _filteringSettings;
    private RenderTargetIdentifier _cameraColorTarget;
    private SortingCriteria _sort;

    // 修改构造函数，增加一个 filterRange 参数
    public CustomRendererPassOutline(string tag, RenderPassEvent passEvent, RenderQueueRange filterRange)
    {
        _profilerTag = tag;
        renderPassEvent = passEvent;
        _filteringSettings = new FilteringSettings(filterRange);

    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

        // 使用自定义的 LightMode 标签"CustomPassTest"
        var drawSettings = CreateDrawingSettings(
            new ShaderTagId(_profilerTag),
            ref renderingData,
           
        // 根据 filterRange 来决定排序标准：若是透明，则用 CommonTransparent；否则用 CommonOpaque
        (_filteringSettings.renderQueueRange == RenderQueueRange.transparent) ?
            SortingCriteria.CommonTransparent : SortingCriteria.CommonOpaque
        );

        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
