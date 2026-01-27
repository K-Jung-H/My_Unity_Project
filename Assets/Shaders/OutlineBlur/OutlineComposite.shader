Shader "Hidden/OutlineComposite"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _ObjectMaskTex ("Mask", 2D) = "black" {}
        _BlurredTex ("Blurred BG", 2D) = "black" {}
        _EdgeTex ("Edge", 2D) = "black" {}

        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        
        _BlurIntensity ("Blur Intensity", Range(0, 10)) = 1 
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "OutlineComposite"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_ObjectMaskTex);  SAMPLER(sampler_ObjectMaskTex);
            TEXTURE2D(_BlurredTex);     SAMPLER(sampler_BlurredTex);
            TEXTURE2D(_EdgeTex);        SAMPLER(sampler_EdgeTex);

            float4 _OutlineColor;

            half4 Frag (Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;

                half4 sharpCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 blurCol  = SAMPLE_TEXTURE2D(_BlurredTex, sampler_BlurredTex, uv);
                half  mask     = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv).r;
                half  isEdge   = SAMPLE_TEXTURE2D(_EdgeTex, sampler_EdgeTex, uv).r;

                half3 finalColor = blurCol.rgb;
                finalColor = lerp(finalColor, sharpCol.rgb, mask);
                finalColor = lerp(finalColor, _OutlineColor.rgb, isEdge);

                return half4(finalColor, sharpCol.a);
            }
            ENDHLSL
        }
    }
}