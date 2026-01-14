using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

public class HighlightEffectFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public LayerMask targetLayer;
        public Material maskMaterial;
        public Material compositeMaterial;
    }

    public Settings settings = new Settings();
    HighlightRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new HighlightRenderPass(settings);
        m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.maskMaterial == null || settings.compositeMaterial == null) return;
        renderer.EnqueuePass(m_ScriptablePass);
    }

    class HighlightRenderPass : ScriptableRenderPass
    {
        Settings settings;
        FilteringSettings filteringSettings;
        

        static readonly int MainTexID = Shader.PropertyToID("_MainTex"); 
        static readonly int MaskTexID = Shader.PropertyToID("_OutlineMaskTex"); 

        class PassData
        {
            public TextureHandle maskTexture;
            public TextureHandle sourceTexture;
            public TextureHandle tempTexture;
            public RendererListHandle rendererList;
            public Material compositeMaterial;
        }

        public HighlightRenderPass(Settings settings)
        {
            this.settings = settings;
            filteringSettings = new FilteringSettings(RenderQueueRange.all, settings.targetLayer);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            int width = cameraData.cameraTargetDescriptor.width;
            int height = cameraData.cameraTargetDescriptor.height;
            

            TextureDesc maskDesc = new TextureDesc(width, height);
            maskDesc.colorFormat = cameraData.cameraTargetDescriptor.graphicsFormat;
            maskDesc.depthBufferBits = 0;
            maskDesc.msaaSamples = MSAASamples.None;
            maskDesc.name = "_OutlineMaskTex";

            TextureDesc tempDesc = new TextureDesc(width, height);
            tempDesc.colorFormat = cameraData.cameraTargetDescriptor.graphicsFormat;
            tempDesc.depthBufferBits = 0;
            tempDesc.msaaSamples = MSAASamples.None;
            tempDesc.name = "_HighlightTempTexture";

            TextureHandle maskTextureHandle = renderGraph.CreateTexture(maskDesc);
            TextureHandle tempTextureHandle = renderGraph.CreateTexture(tempDesc);


            SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                new ShaderTagId("UniversalForward"),
                renderingData,
                cameraData,
                frameData.Get<UniversalLightData>(),
                sortingCriteria
            );
            
            drawingSettings.SetShaderPassName(1, new ShaderTagId("SRPDefaultUnlit"));
            drawingSettings.SetShaderPassName(2, new ShaderTagId("UniversalForwardOnly"));
            drawingSettings.SetShaderPassName(3, new ShaderTagId("LightweightForward"));

            drawingSettings.overrideMaterial = settings.maskMaterial;
            drawingSettings.overrideMaterialPassIndex = 0;

            RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            RendererListHandle rendererListHandle = renderGraph.CreateRendererList(listParams);


            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Highlight_DrawMask", out var passData))
            {
                passData.maskTexture = maskTextureHandle;
                passData.rendererList = rendererListHandle;

                builder.UseRendererList(rendererListHandle);
                builder.SetRenderAttachment(maskTextureHandle, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(false, true, Color.black);
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }


            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Highlight_ApplyEffect", out var passData))
            {
                passData.sourceTexture = resourceData.activeColorTexture; 
                passData.tempTexture = tempTextureHandle;                 
                passData.maskTexture = maskTextureHandle;
                passData.compositeMaterial = settings.compositeMaterial;

                builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
                builder.UseTexture(maskTextureHandle, AccessFlags.Read);
                builder.SetRenderAttachment(tempTextureHandle, 0); 

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {

                    MaterialPropertyBlock props = new MaterialPropertyBlock();
                    
                    props.SetTexture(MainTexID, data.sourceTexture);          
                    props.SetTexture(MaskTexID, data.maskTexture);

                    CoreUtils.DrawFullScreen(context.cmd, data.compositeMaterial, props);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Highlight_CopyBack", out var passData))
            {
                passData.sourceTexture = resourceData.activeColorTexture; 
                passData.tempTexture = tempTextureHandle;                 

                builder.UseTexture(tempTextureHandle, AccessFlags.Read);
                builder.SetRenderAttachment(passData.sourceTexture, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.tempTexture, new Vector4(1,1,0,0), 0.0f, false);
                });
            }
        }
        
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
    }
}