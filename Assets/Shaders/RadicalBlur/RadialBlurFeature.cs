using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class RadialBlurFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    private Material material;
    private RadialBlurPass renderPass;

    public override void Create()
    {
        if (shader == null) shader = Shader.Find("Hidden/Custom/RadialBlur");
        if (shader != null) material = CoreUtils.CreateEngineMaterial(shader);

        renderPass = new RadialBlurPass(material);
        renderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material != null) renderer.EnqueuePass(renderPass);
    }

    protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); }

    class RadialBlurPass : ScriptableRenderPass
    {
        private Material material;
        public RadialBlurPass(Material mat) { this.material = mat; }

        private class PassData
        {
            public TextureHandle source;
            public TextureHandle temp;
            public Material material;
            public float intensity;
            public int sampleCount;
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var volume = stack.GetComponent<RadialBlurVolume>();
            if (volume == null || !volume.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.cameraType == CameraType.Preview || !resourceData.activeColorTexture.IsValid())
                return;

            TextureHandle sourceTexture = resourceData.activeColorTexture;
            var cameraDesc = cameraData.cameraTargetDescriptor;
            
            int divider = volume.downsample.value;
            int width = Mathf.Max(1, cameraDesc.width >> divider);
            int height = Mathf.Max(1, cameraDesc.height >> divider);

            TextureDesc blurTexDesc = new TextureDesc(width, height);
            blurTexDesc.colorFormat = cameraDesc.graphicsFormat;
            blurTexDesc.depthBufferBits = DepthBits.None;
            blurTexDesc.msaaSamples = MSAASamples.None;
            blurTexDesc.filterMode = FilterMode.Bilinear; 
            blurTexDesc.name = "_RadialBlurTemp";
            
            TextureHandle tempTexture = graph.CreateTexture(blurTexDesc);

            using (var builder = graph.AddRasterRenderPass<PassData>("Radial Blur Apply", out var data))
            {
                data.source = sourceTexture;
                data.temp = tempTexture;
                data.material = material;
                data.intensity = volume.intensity.value;
                data.sampleCount = volume.sampleCount.value;

                builder.UseTexture(data.source);
                builder.SetRenderAttachment(data.temp, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetTexture("_BlitTexture", d.source);
                    d.material.SetFloat("_BlurIntensity", d.intensity);
                    d.material.SetInt("_SampleCount", d.sampleCount);
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
                });
            }

            using (var builder = graph.AddRasterRenderPass<PassData>("Radial Blur Copy Back", out var data))
            {
                data.source = sourceTexture;
                data.temp = tempTexture;

                builder.UseTexture(data.temp);
                builder.SetRenderAttachment(data.source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.temp, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
    }
}