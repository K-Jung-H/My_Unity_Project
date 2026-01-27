using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

public class OutlineBlurFeature : ScriptableRendererFeature
{
    public static bool IsActive = false;
    public static uint ExtensionLayerMask = 0xFFFFFFFF;

    [System.Serializable]
    public class Settings
    {
        public LayerMask targetLayer;
        [Range(0, 2)] public int downsample = 1; 
        public Material maskMaterial;
        public Material blurHorizontalMaterial;
        public Material blurVerticalMaterial;
        public Material edgeMaterial;
        public Material compositeMaterial;
    }

    public Settings settings = new();
    OutlineBlurPass pass;

    public override void Create()
    {
        pass = new OutlineBlurPass(settings);
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!IsActive) return;
        if (settings.maskMaterial == null || settings.compositeMaterial == null) return;
        renderer.EnqueuePass(pass);
    }

    class OutlineBlurPass : ScriptableRenderPass
    {
        readonly Settings settings;
        static readonly List<ShaderTagId> shaderTags = new()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        public OutlineBlurPass(Settings settings) { this.settings = settings; }

        class MaskPassData { public RendererListHandle list; }
        class BlurPassData { public Material mat; public TextureHandle src; }
        class EdgePassData { public Material mat; public TextureHandle mask; }
        class CompositePassData { public Material mat; public TextureHandle src; public TextureHandle mask; public TextureHandle blur; public TextureHandle edge; }
        class BlitPassData { public TextureHandle src; }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            if (cameraData.cameraType == CameraType.Preview || !resources.activeColorTexture.IsValid())
                return;

            var cameraDesc = cameraData.cameraTargetDescriptor;
            var cameraColor = resources.activeColorTexture;

            float thickness = settings.edgeMaterial.GetFloat("_OutlineThickness");
            float blurIntensity = settings.compositeMaterial.GetFloat("_BlurIntensity");
            blurIntensity = Mathf.Max(0.0f, blurIntensity);

            TextureDesc maskDesc = new TextureDesc(cameraDesc.width, cameraDesc.height);
            maskDesc.colorFormat = GraphicsFormat.R8_UNorm;
            maskDesc.depthBufferBits = DepthBits.None;
            maskDesc.msaaSamples = MSAASamples.None;
            maskDesc.name = "_OutlineMask";
            
            var maskTex = graph.CreateTexture(maskDesc);

            using (var builder = graph.AddRasterRenderPass<MaskPassData>("Outline Mask Pass", out var data))
            {
                builder.SetRenderAttachment(maskTex, 0, AccessFlags.Write);
                var drawing = CreateDrawingSettings(shaderTags, cameraData);
                drawing.overrideMaterial = settings.maskMaterial;
                var filtering = new FilteringSettings(RenderQueueRange.opaque, settings.targetLayer, ExtensionLayerMask);
                var listParams = new RendererListParams(renderingData.cullResults, drawing, filtering);
                data.list = graph.CreateRendererList(listParams);
                builder.UseRendererList(data.list);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((MaskPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, Color.black);
                    ctx.cmd.DrawRendererList(d.list);
                });
            }

            int blurW = Mathf.Max(1, cameraDesc.width >> settings.downsample);
            int blurH = Mathf.Max(1, cameraDesc.height >> settings.downsample);

            TextureDesc blurDesc = new TextureDesc(blurW, blurH);
            blurDesc.colorFormat = cameraDesc.graphicsFormat;
            blurDesc.depthBufferBits = DepthBits.None;
            blurDesc.msaaSamples = MSAASamples.None;
            blurDesc.filterMode = FilterMode.Bilinear;
            blurDesc.name = "_OutlineBlurTemp";

            var blurTempTex = graph.CreateTexture(blurDesc);
            var blurTex = graph.CreateTexture(blurDesc);

            using (var builder = graph.AddRasterRenderPass<BlurPassData>("Outline Blur Horizontal", out var data))
            {
                data.mat = settings.blurHorizontalMaterial;
                data.src = cameraColor; 
                builder.UseTexture(data.src);
                builder.SetRenderAttachment(blurTempTex, 0, AccessFlags.Write);
                builder.SetRenderFunc((BlurPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, Color.black);
                    if (d.src.IsValid())
                    {
                        d.mat.SetTexture("_BlitTexture", d.src);
                        d.mat.SetVector("_BlitTexture_TexelSize", new Vector4(1f/blurW, 1f/blurH, blurW, blurH));
                        d.mat.SetFloat("_BlurScale", blurIntensity);
                        Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, 0);
                    }
                });
            }

            using (var builder = graph.AddRasterRenderPass<BlurPassData>("Outline Blur Vertical", out var data))
            {
                data.mat = settings.blurVerticalMaterial;
                data.src = blurTempTex;
                builder.UseTexture(data.src);
                builder.SetRenderAttachment(blurTex, 0, AccessFlags.Write);
                builder.SetRenderFunc((BlurPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, Color.black);
                    d.mat.SetTexture("_BlitTexture", d.src);
                    d.mat.SetVector("_BlitTexture_TexelSize", new Vector4(1f/blurW, 1f/blurH, blurW, blurH));
                    d.mat.SetFloat("_BlurScale", blurIntensity);
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, 0);
                });
            }

            var edgeTex = graph.CreateTexture(maskDesc); 
            using (var builder = graph.AddRasterRenderPass<EdgePassData>("Outline Edge Gen", out var data))
            {
                data.mat = settings.edgeMaterial;
                data.mask = maskTex;
                builder.UseTexture(data.mask);
                builder.SetRenderAttachment(edgeTex, 0, AccessFlags.Write);
                builder.SetRenderFunc((EdgePassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, Color.black);
                    d.mat.SetTexture("_ObjectMaskTex", d.mask);
                    d.mat.SetFloat("_OutlineThickness", thickness);
                    Blitter.BlitTexture(ctx.cmd, d.mask, new Vector4(1, 1, 0, 0), d.mat, 0);
                });
            }

            TextureDesc compositeDesc = new TextureDesc(cameraDesc.width, cameraDesc.height);
            compositeDesc.colorFormat = cameraDesc.graphicsFormat;
            compositeDesc.depthBufferBits = DepthBits.None;
            compositeDesc.msaaSamples = MSAASamples.None;
            compositeDesc.name = "_OutlineComposite";
            
            var compositeTex = graph.CreateTexture(compositeDesc);

            using (var builder = graph.AddRasterRenderPass<CompositePassData>("Outline Composite", out var data))
            {
                data.mat = settings.compositeMaterial;
                data.src = cameraColor;
                data.mask = maskTex;
                data.blur = blurTex;
                data.edge = edgeTex;

                builder.UseTexture(data.src);
                builder.UseTexture(data.mask);
                builder.UseTexture(data.blur);
                builder.UseTexture(data.edge);
                builder.SetRenderAttachment(compositeTex, 0, AccessFlags.Write);

                builder.SetRenderFunc((CompositePassData d, RasterGraphContext ctx) =>
                {
                    d.mat.SetTexture("_MainTex", d.src);
                    d.mat.SetTexture("_ObjectMaskTex", d.mask);
                    d.mat.SetTexture("_BlurredTex", d.blur);
                    d.mat.SetTexture("_EdgeTex", d.edge);
                    
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, 0);
                });
            }

            using (var builder = graph.AddRasterRenderPass<BlitPassData>("Outline Final Blit", out var data))
            {
                data.src = compositeTex;
                builder.UseTexture(data.src);
                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((BlitPassData d, RasterGraphContext ctx) => {
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }        

        static DrawingSettings CreateDrawingSettings(List<ShaderTagId> tags, UniversalCameraData cameraData)
        {
            var settings = new DrawingSettings(tags[0], new SortingSettings(cameraData.camera));
            for (int i = 1; i < tags.Count; i++) settings.SetShaderPassName(i, tags[i]);
            return settings;
        }
    }
}