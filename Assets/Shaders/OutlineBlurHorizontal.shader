Shader "Hidden/OutlineBlurVertical"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "OutlineBlurVertical"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_BlitTexture);

            half4 Frag (Varyings i) : SV_Target
            {
                float2 texel = float2(0, _BlitTexture_TexelSize.y);
                float2 uv = i.texcoord;

                half sum = 0;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv - texel * 2).r * 0.1216216;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv - texel).r * 0.2332432;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv).r * 0.2902703;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + texel).r * 0.2332432;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + texel * 2).r * 0.1216216;

                return half4(sum, sum, sum, 1);
            }
            ENDHLSL
        }
    }
}