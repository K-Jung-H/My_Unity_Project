void ProcessEffect_float(
    UnityTexture2D MainTex,       
    UnityTexture2D MaskTex,       
    UnitySamplerState MainSampler,
    float2 UV, 
    float BlurSize,
    out float3 OutColor, 
    out float OutEdge
)
{
    float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

    float2 maskUV = UV;
    if (_ProjectionParams.x < 0.0)
    {
        maskUV.y = 1.0 - maskUV.y;
    }

    float t = SAMPLE_TEXTURE2D(MaskTex.tex, MainSampler.samplerstate, maskUV + float2(0, texelSize.y)).r;
    float b = SAMPLE_TEXTURE2D(MaskTex.tex, MainSampler.samplerstate, maskUV + float2(0, -texelSize.y)).r;
    float l = SAMPLE_TEXTURE2D(MaskTex.tex, MainSampler.samplerstate, maskUV + float2(-texelSize.x, 0)).r;
    float r = SAMPLE_TEXTURE2D(MaskTex.tex, MainSampler.samplerstate, maskUV + float2(texelSize.x, 0)).r;
    float edge = saturate(abs(t - b) + abs(l - r));

    float3 mainBlurSum = float3(0,0,0);
    float mainWeightSum = 0.0;
    int radius = 4; 

    for(int x = -radius; x <= radius; x++)
    {
        for(int y = -radius; y <= radius; y++)
        {
            float2 offset = float2(x, y) * texelSize * BlurSize;
            float weight = exp(-(x*x + y*y) / 8.0); 
            float3 texColor = SAMPLE_TEXTURE2D(MainTex.tex, MainSampler.samplerstate, UV + offset).rgb;
            mainBlurSum += texColor * weight;
            mainWeightSum += weight;
        }
    }
    float3 blurredMainColor = mainBlurSum / mainWeightSum;
    
    float3 originalColor = SAMPLE_TEXTURE2D(MainTex.tex, MainSampler.samplerstate, UV).rgb;
    float maskVal = SAMPLE_TEXTURE2D(MaskTex.tex, MainSampler.samplerstate, maskUV).r;
    
    OutColor = lerp(blurredMainColor, originalColor, maskVal);
    OutEdge = edge;
}