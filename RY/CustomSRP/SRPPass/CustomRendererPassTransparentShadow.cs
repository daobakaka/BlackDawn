using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

/// <summary>
/// ͸��������Ӱ
/// </summary>
public class CustomRendererPassTransparentShadow : ScriptableRenderPass
{
    private string _profilerTag;
    private FilteringSettings _filteringSettings;
    private RenderTargetIdentifier _cameraColorTarget;
    private SortingCriteria _sort;

    // �޸Ĺ��캯��������һ�� filterRange ����
    public CustomRendererPassTransparentShadow(string tag, RenderPassEvent passEvent, RenderQueueRange filterRange)
    {
        _profilerTag = tag;
        renderPassEvent = passEvent;
        _filteringSettings = new FilteringSettings(filterRange);

    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

        // ʹ���Զ���� LightMode ��ǩ"CustomPassTest"
        var drawSettings = CreateDrawingSettings(
            new ShaderTagId(_profilerTag),
            ref renderingData,

        // ���� filterRange �����������׼������͸�������� CommonTransparent�������� CommonOpaque
        (_filteringSettings.renderQueueRange == RenderQueueRange.transparent) ?
            SortingCriteria.CommonTransparent : SortingCriteria.CommonOpaque
        );

        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}