float Epsilon = 1e-10;
 
float3 RGBtoHCV(in float3 RGB)
{
    float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
    float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
    return float3(H, C, Q.x);
}

float3 HUEtoRGB(in float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

float3 RGBtoHSV(in float3 RGB)
{
    float3 HCV = RGBtoHCV(RGB);
    float S = HCV.y / (HCV.z + Epsilon);
    return float3(HCV.x, S, HCV.z);
}


StructuredBuffer<float3> _ProximityColors;
StructuredBuffer<float2> _ProximityPositions;
StructuredBuffer<float> _ProximityRadiuses;
StructuredBuffer<float> _ProximitySaturationBoosts;

float _ProximityCount;
float _fallofExponential;
float _othersInfluenceOverMe;
float _maxIntensity;

float3 SampleProximityColor(float3 opaqueFragmentColor, float2 fragmentPosition2D)
{
    int count = (int)_ProximityCount;
    float3 totalInfluence = float3(0.0, 0.0, 0.0);

    for (int i = 0; i < count; i++)
    {
        float2 entityPos = _ProximityPositions[i];
        float d = distance(fragmentPosition2D, entityPos);
        float radius = max(_ProximityRadiuses[i], 1e-5);
        float normalizedDistance = saturate(d / radius);
        float influence = pow(1.0 - normalizedDistance, _fallofExponential);
        
        // Apply saturation boost to entity color
        float3 baseColor = _ProximityColors[i];
        float boost = _ProximitySaturationBoosts[i];
        float luminance = dot(baseColor, float3(0.2126, 0.7152, 0.0722));
        float3 saturatedColor = lerp(luminance, baseColor, boost);
        
        totalInfluence += influence * saturatedColor;
    }

    // Combine base color with proximity influences
    float3 finalColor = lerp(
        opaqueFragmentColor, 
        opaqueFragmentColor + totalInfluence,
        _othersInfluenceOverMe
    );

    // Apply intensity limiting
    float intensity = length(finalColor);
    intensity = min(intensity, _maxIntensity);
    finalColor = (intensity > 1e-5) ? normalize(finalColor) * intensity : 0;
    
    return finalColor;
}