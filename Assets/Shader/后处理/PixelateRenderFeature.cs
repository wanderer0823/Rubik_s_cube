using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelateRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PixelateSettings
    {
        public Material pixelateMaterial;

        [Range(1, 16)]
        public int pixelSize = 4;

        [Range(0, 1)]
        public float blurStrength = 0.25f;
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
        if (settings.pixelateMaterial == null) return;
        renderPass.Setup(renderer);
        renderer.EnqueuePass(renderPass);
    }

    public override void SetupRenderPasses(
    ScriptableRenderer renderer,
    in RenderingData renderingData)
    {
        renderPass.Setup(renderer);
    }

    private class PixelateRenderPass : ScriptableRenderPass
    {
        private PixelateSettings settings;
        private int tempRT;
        private ScriptableRenderer renderer;
        private RTHandle source;

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
            if (settings.pixelateMaterial == null) return;
            if (renderer == null) return;

            var source = renderer.cameraColorTargetHandle;
            if (source == null) return;

            settings.pixelateMaterial.SetFloat("_PixelSize", settings.pixelSize);
            settings.pixelateMaterial.SetFloat("_BlurStrength", settings.blurStrength);

            var cmd = CommandBufferPool.Get("Pixelate");

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            int w = Mathf.Max(1, Mathf.RoundToInt(desc.width / settings.pixelSize));
            int h = Mathf.Max(1, Mathf.RoundToInt(desc.height / settings.pixelSize));

            cmd.GetTemporaryRT(tempRT, w, h, 0, FilterMode.Point, desc.colorFormat);
            if (renderer == null)
            {
                Debug.LogError("renderer null");
                return;
            }

            if (source == null)
            {
                Debug.LogError("source null");
                return;
            }

            if (settings.pixelateMaterial == null)
            {
                Debug.LogError("material null");
                return;
            }
            RenderTargetIdentifier src = source;
            RenderTargetIdentifier dst = source;

            cmd.Blit(src, tempRT);
            cmd.Blit(tempRT, dst, settings.pixelateMaterial);
            cmd.ReleaseTemporaryRT(tempRT);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
