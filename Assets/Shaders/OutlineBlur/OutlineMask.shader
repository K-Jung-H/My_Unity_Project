Shader "Hidden/OutlineMask"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct V
            {
                float4 positionOS : POSITION;
            };

            struct F
            {
                float4 pos : SV_POSITION;
            };

            F vert (V v)
            {
                F o;
                o.pos = TransformObjectToHClip(v.positionOS);
                return o;
            }

            half4 frag (F i) : SV_Target
            {
                return 1;
            }
            ENDHLSL
        }
    }
}
