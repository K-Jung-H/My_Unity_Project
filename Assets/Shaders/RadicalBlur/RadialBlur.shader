Shader "Hidden/Custom/RadialBlur"
{
    Properties
    {
        _BlurIntensity ("Intensity", Float) = 0.05
        _SampleCount ("Sample Count", Int) = 10
    }
    
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off
        
        Pass
        {
            Name "RadialBlurPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_BlitTexture);
        
            float _BlurIntensity;
            int _SampleCount;

            half4 Frag (Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float2 center = float2(0.5, 0.5);
                float2 dir = center - uv;
                
                half4 color = half4(0, 0, 0, 0);
                
                float fSampleCount = float(_SampleCount);
                float invSampleCountMinusOne = 1.0 / (fSampleCount - 1.0);

                for(int j = 0; j < _SampleCount; j++)
                {
                    float scale = 1.0 - _BlurIntensity * (float(j) * invSampleCountMinusOne);
                    
                    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, center - dir * scale);
                }

                return color / fSampleCount;
            }
            ENDHLSL
        }
    }
}