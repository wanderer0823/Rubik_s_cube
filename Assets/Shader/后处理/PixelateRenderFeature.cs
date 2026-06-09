using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelateRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PixelateSettings
    {
        public Material pixelateMaterial;
        [Range(1f, 11f)]
        public float pixelSize = 1f;
    }

    public PixelateSettings settings = new PixelateSettings();
    private PixelateRenderPass renderPass;

    public static PixelateRenderFeature instance;

    public override void Create()
    {
        instance = this;
        renderPass = new PixelateRenderPass(settings);
        renderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.pixelateMaterial == null)
            return;

        renderPass.Setup(renderer);
        renderer.EnqueuePass(renderPass);
    }

    private class PixelateRenderPass : ScriptableRenderPass
    {
        private PixelateSettings settings;
        private int tempRT;
        private ScriptableRenderer renderer;

        public PixelateRenderPass(PixelateSettings settings)
        {
            this.settings = settings;
            tempRT = Shader.PropertyToID("_TempPixelateRT");
        }

        public void Setup(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.pixelateMaterial == null)
                return;

            var cmd = CommandBufferPool.Get("Pixelate");
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            float downWidth = cameraTargetDescriptor.width / settings.pixelSize;
            float downHeight = cameraTargetDescriptor.height / settings.pixelSize;

            int downWidthInt = Mathf.Max(1, Mathf.RoundToInt(downWidth));
            int downHeightInt = Mathf.Max(1, Mathf.RoundToInt(downHeight));

            var source = renderer.cameraColorTargetHandle;

            cmd.GetTemporaryRT(tempRT, downWidthInt, downHeightInt, 0, FilterMode.Bilinear, cameraTargetDescriptor.colorFormat);

            cmd.Blit(source, tempRT);
            cmd.Blit(tempRT, source, settings.pixelateMaterial);

            cmd.ReleaseTemporaryRT(tempRT);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
