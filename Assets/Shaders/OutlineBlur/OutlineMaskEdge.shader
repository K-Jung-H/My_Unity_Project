Shader "Hidden/OutlineMaskEdge"
{
    Properties
    {
        _ObjectMaskTex ("Mask Texture", 2D) = "black" {}

        [IntRange] _OutlineThickness ("Thickness", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "OutlineSobelEdge"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_ObjectMaskTex);
            SAMPLER(sampler_ObjectMaskTex);
            
            float _OutlineThickness;

            half4 Frag (Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                
                float2 texel = _ScreenParams.zw * _OutlineThickness;

                float tl = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv + float2(-texel.x, texel.y)).r;
                float t  = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv + float2(0, texel.y)).r;
                float tr = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv + float2(texel.x, texel.y)).r;

                float l  = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv + float2(-texel.x, 0)).r;
                float r  = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv + float2(texel.x, 0)).r;

                float bl = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv + float2(-texel.x, -texel.y)).r;
                float b  = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv + float2(0, -texel.y)).r;
                float br = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv + float2(texel.x, -texel.y)).r;

                float gx = (-1 * tl) + (1 * tr) + (-2 * l) + (2 * r) + (-1 * bl) + (1 * br);
                float gy = (-1 * tl) + (-2 * t) + (-1 * tr) + (1 * bl) + (2 * b) + (1 * br);

                float edge = sqrt(gx * gx + gy * gy);

                return saturate(edge);
            }
            ENDHLSL
        }
    }
}