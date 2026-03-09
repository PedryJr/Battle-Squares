float3 RGBtoHSV(float3 rgb)
{
    float3 hsv;
    float minVal = min(min(rgb.r, rgb.g), rgb.b);
    float maxVal = max(max(rgb.r, rgb.g), rgb.b);
    float delta = maxVal - minVal;
    
    hsv.z = maxVal;
    
    if (maxVal != 0.0)
        hsv.y = delta / maxVal;
    else
        hsv.y = 0.0;
    
    if (delta == 0.0)
    {
        hsv.x = 0.0;
    }
    else
    {
        if (rgb.r == maxVal)
            hsv.x = (rgb.g - rgb.b) / delta;
        else if (rgb.g == maxVal)
            hsv.x = 2.0 + (rgb.b - rgb.r) / delta;
        else
            hsv.x = 4.0 + (rgb.r - rgb.g) / delta;
        
        hsv.x /= 6.0;
        
        if (hsv.x < 0.0)
            hsv.x += 1.0;
    }
    
    return hsv;
}

float3 HSVtoRGB(float3 hsv)
{
    float3 rgb;
    
    if (hsv.y == 0.0)
    {
        rgb = float3(hsv.z, hsv.z, hsv.z);
    }
    else
    {
        float h = hsv.x * 6.0;
        int i = floor(h);
        float f = h - i;
        float p = hsv.z * (1.0 - hsv.y);
        float q = hsv.z * (1.0 - hsv.y * f);
        float t = hsv.z * (1.0 - hsv.y * (1.0 - f));
        
        if (i == 0)
            rgb = float3(hsv.z, t, p);
        else if (i == 1)
            rgb = float3(q, hsv.z, p);
        else if (i == 2)
            rgb = float3(p, hsv.z, t);
        else if (i == 3)
            rgb = float3(p, q, hsv.z);
        else if (i == 4)
            rgb = float3(t, p, hsv.z);
        else
            rgb = float3(hsv.z, p, q);
    }
    
    return rgb;
}

float3 RGBtoHSV(float r, float g, float b)
{
    return RGBtoHSV(float3(r, g, b));
}

float3 HSVtoRGB(float h, float s, float v)
{
    return HSVtoRGB(float3(h, s, v));
}