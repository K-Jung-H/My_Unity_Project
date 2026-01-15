Shader "Hidden/OutlineComposite"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _ObjectMaskTex ("Mask", 2D) = "black" {}
        _BlurredTex ("Blur", 2D) = "black" {}

        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineThickness ("Thickness (px)", Range(0, 10)) = 2 
        _OutlineIntensity ("Intensity", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "OutlineComposite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_ObjectMaskTex);
            SAMPLER(sampler_ObjectMaskTex);

            TEXTURE2D(_BlurredTex);
            SAMPLER(sampler_BlurredTex);

            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineIntensity;

            half4 Frag (Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half mask = SAMPLE_TEXTURE2D(_ObjectMaskTex, sampler_ObjectMaskTex, uv).r;
                half blur = SAMPLE_TEXTURE2D(_BlurredTex, sampler_BlurredTex, uv).r;
                half outline = saturate((blur - mask) * _OutlineIntensity);
                half3 color = lerp(src.rgb, _OutlineColor.rgb, outline);

                return half4(color, src.a);
            }
            ENDHLSL
        }
    }
}