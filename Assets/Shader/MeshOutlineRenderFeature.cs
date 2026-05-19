using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MeshOutlineRenderFeature : ScriptableRendererFeature
{
    public string PassName = "MeshOutline";
    public RenderPassEvent Event=RenderPassEvent.AfterRenderingOpaques;
    public LayerMask OutlineLayer = -1;         //渲染层过滤
    MeshOutlineRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new MeshOutlineRenderPass(PassName,OutlineLayer);

        m_ScriptablePass.renderPassEvent = Event;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    class MeshOutlineRenderPass : ScriptableRenderPass
    {
        ShaderTagId shaderTags;
        FilteringSettings m_FilteringSettings;
        public MeshOutlineRenderPass(string Tags,LayerMask layerMask)
        {
            shaderTags=new ShaderTagId(Tags);       //pass标签字符转为Tag
            m_FilteringSettings=new FilteringSettings(RenderQueueRange.opaque,layerMask);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 创建绘制设置
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTags, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

            // 获取命令缓冲区
            CommandBuffer cmd = CommandBufferPool.Get("Mesh Outline");
            // 开启一个性能分析标签
            using (new ProfilingScope(cmd, new ProfilingSampler("Mesh Outline")))
            {
                context.ExecuteCommandBuffer(cmd);  // 执行命令缓冲区命令
                cmd.Clear();                        // 清空命令缓冲区

                // 绘制pass
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);      // 执行命令缓冲区命令
            CommandBufferPool.Release(cmd);         // 释放命令缓冲区
        }


    }
}


