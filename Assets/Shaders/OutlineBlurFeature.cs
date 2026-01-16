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

        if (settings.maskMaterial == null || 
            settings.blurHorizontalMaterial == null || 
            settings.blurVerticalMaterial == null || 
            settings.edgeMaterial == null ||
            settings.compositeMaterial == null)
            return;

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
        class CompositePassData
        {
            public Material mat;
            public TextureHandle src;
            public TextureHandle mask;
            public TextureHandle blur;
            public TextureHandle edge;
        }
        class BlitPassData { public TextureHandle src; }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            var cameraDesc = cameraData.cameraTargetDescriptor;
            var cameraColor = resources.activeColorTexture;

            float thickness = settings.edgeMaterial.GetFloat("_OutlineThickness");
            float blurIntensity = settings.compositeMaterial.GetFloat("_BlurIntensity");
            blurIntensity = Mathf.Max(0.0f, blurIntensity);

            TextureDesc maskDesc = new(cameraDesc.width, cameraDesc.height)
            {
                colorFormat = GraphicsFormat.R8_UNorm,
                depthBufferBits = 0,
                msaaSamples = MSAASamples.None,
                name = "_OutlineMask"
            };
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

            TextureDesc colorDesc = new(cameraDesc.width, cameraDesc.height)
            {
                colorFormat = cameraDesc.graphicsFormat,
                depthBufferBits = 0,
                name = "_OutlineBlurTemp"
            };
            var blurTempTex = graph.CreateTexture(colorDesc);
            var blurTex = graph.CreateTexture(colorDesc);

            using (var builder = graph.AddRasterRenderPass<BlurPassData>("Outline Blur Horizontal", out var data))
            {
                data.mat = settings.blurHorizontalMaterial;
                data.src = cameraColor; 
                builder.UseTexture(data.src);
                builder.SetRenderAttachment(blurTempTex, 0, AccessFlags.Write);
                builder.SetRenderFunc((BlurPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, Color.black);
                    d.mat.SetTexture("_BlitTexture", d.src);
                    d.mat.SetVector("_BlitTexture_TexelSize", new Vector4(1f/cameraDesc.width, 1f/cameraDesc.height, cameraDesc.width, cameraDesc.height));
                    d.mat.SetFloat("_BlurScale", blurIntensity);
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, 0);
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
                    d.mat.SetVector("_BlitTexture_TexelSize", new Vector4(1f/cameraDesc.width, 1f/cameraDesc.height, cameraDesc.width, cameraDesc.height));
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

            var compositeTex = graph.CreateTexture(colorDesc);
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