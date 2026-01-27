Shader "Hidden/OutlineBlurVertical"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "OutlineBlurVertical"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_BlitTexture);
            float _BlurScale;

            half4 Frag (Varyings i) : SV_Target
            {
                float scale = max(0.0, _BlurScale);
                float2 texel = float2(0, _BlitTexture_TexelSize.y * scale);
                float2 uv = i.texcoord;

                half3 sum = half3(0,0,0);
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv - texel * 2).rgb * 0.1216216;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv - texel).rgb     * 0.2332432;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv).rgb             * 0.2902703;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + texel).rgb     * 0.2332432;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + texel * 2).rgb * 0.1216216;

                return half4(sum, 1);
            }
            ENDHLSL
        }
    }
}