using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

[System.Serializable]
public class LayerMaskParameter : VolumeParameter<LayerMask>
{
    public LayerMaskParameter(LayerMask value, bool overrideState = false) : base(value, overrideState) { }
}

[System.Serializable, VolumeComponentMenu("Custom/Outline Blur")]
public class OutlineBlurVolume : VolumeComponent, IPostProcessComponent
{
    public BoolParameter isActive = new BoolParameter(false);

    public LayerMaskParameter optimizeLayer = new LayerMaskParameter(-1);
    public ClampedIntParameter downsample = new ClampedIntParameter(1, 0, 2);
    public ClampedIntParameter targetLightLayer = new ClampedIntParameter(1, 0, 32); 

    public ColorParameter outlineColor = new ColorParameter(Color.yellow);
    public ClampedFloatParameter outlineThickness = new ClampedFloatParameter(1f, 0f, 10f);
    public ClampedFloatParameter blurIntensity = new ClampedFloatParameter(1f, 0f, 5f);

    public bool IsActive() => isActive.value && active;
    public bool IsTileCompatible() => false;
}

public class OutlineBlurFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
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

        class PassData
        {
            public TextureHandle mask;
            public TextureHandle blur;
            public TextureHandle edge;
            public TextureHandle composite;
            public TextureHandle source;
            public RendererListHandle rendererList;
            
            public Material material; 
            public float thickness;
            public Color color;
            public float blurIntensity;
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            
            if (cameraData.renderType != CameraRenderType.Base) 
                return;

            var volume = VolumeManager.instance.stack.GetComponent<OutlineBlurVolume>();

            if (volume == null || !volume.IsActive()) return;
            if (resourceData.activeColorTexture.IsValid() == false) return;

            TextureHandle sourceTexture = resourceData.activeColorTexture;
            var cameraTargetDesc = cameraData.cameraTargetDescriptor;

            int downsample = volume.downsample.value;

            TextureDesc maskDesc = new TextureDesc(cameraTargetDesc.width, cameraTargetDesc.height);
            maskDesc.colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm;
            maskDesc.depthBufferBits = 0;
            maskDesc.name = "OutlineMask";

            TextureDesc blurDesc = new TextureDesc(cameraTargetDesc.width / (downsample + 1), 
                                                   cameraTargetDesc.height / (downsample + 1));
            blurDesc.colorFormat = cameraTargetDesc.graphicsFormat;
            blurDesc.depthBufferBits = 0;
            blurDesc.name = "OutlineBlur";

            TextureDesc compositeDesc = new TextureDesc(cameraTargetDesc.width, cameraTargetDesc.height);
            compositeDesc.colorFormat = cameraTargetDesc.graphicsFormat;
            compositeDesc.depthBufferBits = 0;
            compositeDesc.name = "OutlineComposite";

            TextureHandle maskTex = graph.CreateTexture(maskDesc);
            TextureHandle edgeTex = graph.CreateTexture(maskDesc);
            TextureHandle blurTex = graph.CreateTexture(blurDesc);
            TextureHandle tempBlurTex = graph.CreateTexture(blurDesc);
            TextureHandle compositeTex = graph.CreateTexture(compositeDesc);

            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, volume.optimizeLayer.value);
            filteringSettings.renderingLayerMask = (uint)volume.targetLightLayer.value;

            var sortingSettings = new SortingSettings(cameraData.camera);
            var drawingSettings = new DrawingSettings(shaderTags[0], sortingSettings)
            {
                perObjectData = PerObjectData.None,
                enableDynamicBatching = true,
                enableInstancing = true,
                overrideMaterial = settings.maskMaterial
            };
            
            for (int i = 1; i < shaderTags.Count; ++i)
                drawingSettings.SetShaderPassName(i, shaderTags[i]);

            RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            RendererListHandle rendererListHandle = graph.CreateRendererList(listParams);

            using (var builder = graph.AddRasterRenderPass<PassData>("Outline Mask", out var data))
            {
                data.mask = maskTex;
                data.rendererList = rendererListHandle;
                
                builder.UseRendererList(data.rendererList);
                builder.SetRenderAttachment(data.mask, 0, AccessFlags.Write);
                
                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(false, true, Color.black);
                    ctx.cmd.DrawRendererList(d.rendererList);
                });
            }

            using (var builder = graph.AddRasterRenderPass<PassData>("Outline Blur H", out var data))
            {
                data.source = sourceTexture; 
                data.blur = tempBlurTex;     
                data.material = settings.blurHorizontalMaterial;
                data.blurIntensity = volume.blurIntensity.value;

                builder.UseTexture(data.source);
                builder.SetRenderAttachment(data.blur, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetFloat("_BlurScale", d.blurIntensity); 
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
                });
            }

            using (var builder = graph.AddRasterRenderPass<PassData>("Outline Blur V", out var data))
            {
                data.source = tempBlurTex; 
                data.blur = blurTex;       
                data.material = settings.blurVerticalMaterial;
                data.blurIntensity = volume.blurIntensity.value;

                builder.UseTexture(data.source);
                builder.SetRenderAttachment(data.blur, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetFloat("_BlurScale", d.blurIntensity);
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
                });
            }

            using (var builder = graph.AddRasterRenderPass<PassData>("Outline Edge", out var data))
            {
                data.mask = maskTex;
                data.edge = edgeTex;
                data.material = settings.edgeMaterial;
                data.thickness = volume.outlineThickness.value;

                builder.UseTexture(data.mask);
                builder.SetRenderAttachment(data.edge, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetTexture("_ObjectMaskTex", d.mask);
                    d.material.SetFloat("_OutlineThickness", d.thickness);
                    Blitter.BlitTexture(ctx.cmd, d.mask, new Vector4(1, 1, 0, 0), d.material, 0);
                });
            }

            using (var builder = graph.AddRasterRenderPass<PassData>("Outline Composite", out var data))
            {
                data.source = sourceTexture;
                data.mask = maskTex;
                data.blur = blurTex;
                data.edge = edgeTex;
                data.composite = compositeTex;
                
                data.material = settings.compositeMaterial;
                data.color = volume.outlineColor.value;

                builder.UseTexture(data.source);
                builder.UseTexture(data.mask);
                builder.UseTexture(data.blur);
                builder.UseTexture(data.edge);
                builder.SetRenderAttachment(data.composite, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetTexture("_MainTex", d.source);
                    d.material.SetTexture("_ObjectMaskTex", d.mask);
                    d.material.SetTexture("_BlurredTex", d.blur);
                    d.material.SetTexture("_EdgeTex", d.edge);
                    
                    d.material.SetColor("_OutlineColor", d.color);

                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
                });
            }

            using (var builder = graph.AddRasterRenderPass<PassData>("Outline Final Copy", out var data))
            {
                data.source = compositeTex;
                builder.UseTexture(data.source);
                builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
    }
}