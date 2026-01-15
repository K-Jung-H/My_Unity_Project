using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

public class OutlineBlurFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public LayerMask targetLayer;

        public Material maskMaterial;
        public Material blurHorizontalMaterial;
        public Material blurVerticalMaterial;
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
        if (settings.maskMaterial == null ||
            settings.blurHorizontalMaterial == null ||
            settings.blurVerticalMaterial == null ||
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

        public OutlineBlurPass(Settings settings)
        {
            this.settings = settings;
        }

        class MaskPassData
        {
            public RendererListHandle list;
        }

        class BlitPassData
        {
            public TextureHandle src;
        }

        class CompositePassData
        {
            public Material mat;
            public TextureHandle src;
            public TextureHandle mask;
            public TextureHandle blur;
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            var cameraDesc = cameraData.cameraTargetDescriptor;
            var cameraColor = resources.activeColorTexture;

            // -----------------------
            // 1. Mask Texture
            // -----------------------
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
                builder.SetRenderAttachment(maskTex, 0);

                var filtering = new FilteringSettings(RenderQueueRange.opaque, settings.targetLayer);
                var drawing = CreateDrawingSettings(shaderTags, cameraData);
                drawing.overrideMaterial = settings.maskMaterial;

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

            // -----------------------
            // 2. Blur Temp (Horizontal Result)
            // -----------------------

            TextureDesc blurTempDesc = new(cameraDesc.width, cameraDesc.height)
            {
                colorFormat = GraphicsFormat.R8_UNorm,
                depthBufferBits = 0,
                msaaSamples = MSAASamples.None,
                name = "_OutlineBlurTemp_Horizontal" 
            };
            var blurTempTex = graph.CreateTexture(blurTempDesc);

            using (var builder = graph.AddRasterRenderPass<BlitPassData>("Outline Blur Horizontal", out var data))
            {
                data.src = maskTex;

                builder.UseTexture(data.src);
                builder.SetRenderAttachment(blurTempTex, 0);

                builder.SetRenderFunc((BlitPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(
                        ctx.cmd,
                        d.src,
                        new Vector4(1, 1, 0, 0),
                        settings.blurHorizontalMaterial,
                        0
                    );
                });
            }

            // -----------------------
            // 3. Blur Final (Vertical Result)
            // -----------------------
            TextureDesc blurFinalDesc = new(cameraDesc.width, cameraDesc.height)
            {
                colorFormat = GraphicsFormat.R8_UNorm,
                depthBufferBits = 0,
                msaaSamples = MSAASamples.None,
                name = "_OutlineBlurFinal_Vertical"
            };
            var blurTex = graph.CreateTexture(blurFinalDesc);

            using (var builder = graph.AddRasterRenderPass<BlitPassData>("Outline Blur Vertical", out var data))
            {
                data.src = blurTempTex;

                builder.UseTexture(data.src);
                builder.SetRenderAttachment(blurTex, 0);

                builder.SetRenderFunc((BlitPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(
                        ctx.cmd,
                        d.src,
                        new Vector4(1, 1, 0, 0),
                        settings.blurVerticalMaterial,
                        0
                    );
                });
            }

            // -----------------------
            // 4. Composite
            // -----------------------
            TextureDesc compositeDesc = new(cameraDesc.width, cameraDesc.height)
            {
                colorFormat = cameraDesc.graphicsFormat,
                depthBufferBits = 0,
                msaaSamples = MSAASamples.None,
                name = "_OutlineComposite"
            };
            var compositeTex = graph.CreateTexture(compositeDesc);

            using (var builder = graph.AddRasterRenderPass<CompositePassData>("Outline Composite", out var data))
            {
                data.mat = settings.compositeMaterial;
                data.src = cameraColor;
                data.mask = maskTex;
                data.blur = blurTex;

                builder.UseTexture(data.src);
                builder.UseTexture(data.mask);
                builder.UseTexture(data.blur);
                builder.SetRenderAttachment(compositeTex, 0);

                builder.SetRenderFunc((CompositePassData d, RasterGraphContext ctx) =>
                {
                    d.mat.SetTexture("_MainTex", d.src);
                    d.mat.SetTexture("_ObjectMaskTex", d.mask);
                    d.mat.SetTexture("_BlurredTex", d.blur);

                    Blitter.BlitTexture(
                        ctx.cmd,
                        d.src,
                        new Vector4(1, 1, 0, 0),
                        d.mat,
                        0
                    );
                });
            }

            // -----------------------
            // 5. Final Blit to Camera
            // -----------------------
            using (var builder = graph.AddRasterRenderPass<BlitPassData>("Outline Final Blit", out var data))
            {
                data.src = compositeTex;

                builder.UseTexture(data.src);
                builder.SetRenderAttachment(cameraColor, 0);

                builder.SetRenderFunc((BlitPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(
                        ctx.cmd,
                        d.src,
                        new Vector4(1, 1, 0, 0),
                        0,
                        false
                    );
                });
            }
        }

        static DrawingSettings CreateDrawingSettings(
            List<ShaderTagId> tags,
            UniversalCameraData cameraData)
        {
            var settings = new DrawingSettings(tags[0], new SortingSettings(cameraData.camera));
            for (int i = 1; i < tags.Count; i++)
                settings.SetShaderPassName(i, tags[i]);
            return settings;
        }
    }
}