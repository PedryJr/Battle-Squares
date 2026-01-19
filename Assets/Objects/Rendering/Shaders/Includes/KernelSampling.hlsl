float GetChannelValue(float4 color)
{
   #ifdef KERNEL_CHANNEL_RED
        return color.r;
   #endif
   #ifdef KERNEL_CHANNEL_GREEN
        return color.g;
            #endif
   #ifdef KERNEL_CHANNEL_BLUE
        return color.b;
            #endif
   #ifdef KERNEL_CHANNEL_ALPHA
        return color.a;
   #endif
    return color.r;
}

float LaplacianOfGaussian3x3(sampler2D tex, float2 uv, float2 texelSize)
{
    const float kernel[9] = {
        0.165448,  0.370172,  0.165448,
        0.370172, -2.142483,  0.370172,
        0.165448,  0.370172,  0.165448
    };
    
    float result = 0.0;
    int index = 0;
    
    [unroll]
    for(int y = -1; y <= 1; y++)
    {
        [unroll]
        for(int x = -1; x <= 1; x++)
        {
            float2 sampleUV = uv + float2(x, y) * texelSize;
            float sample = GetChannelValue(tex2D(tex, sampleUV));
            result += sample * kernel[index];
            index++;
        }
    }
    
    return result;
}

float LaplacianOfGaussian5x5(sampler2D tex, float2 uv, float2 texelSize)
{
    const float kernel[25] = {
        0.0094,  0.0308,  0.0398,  0.0308,  0.0094,
        0.0308,  0.0902,  0.1139,  0.0902,  0.0308,
        0.0398,  0.1139, -0.8025,  0.1139,  0.0398,
        0.0308,  0.0902,  0.1139,  0.0902,  0.0308,
        0.0094,  0.0308,  0.0398,  0.0308,  0.0094
    };
    
    float result = 0.0;
    int index = 0;
    
    for(int y = -2; y <= 2; y++)
    {
        for(int x = -2; x <= 2; x++)
        {
            float2 sampleUV = uv + float2(x, y) * texelSize;
            float sample = GetChannelValue(tex2D(tex, sampleUV));
            result += sample * kernel[index];
            index++;
        }
    }
    
    return result;
}

float LaplacianOfGaussian7x7(sampler2D tex, float2 uv, float2 texelSize)
{
    const float kernel[49] = {
        0.0028,  0.0064,  0.0104,  0.0122,  0.0104,  0.0064,  0.0028,
        0.0064,  0.0143,  0.0231,  0.0271,  0.0231,  0.0143,  0.0064,
        0.0104,  0.0231,  0.0372,  0.0436,  0.0372,  0.0231,  0.0104,
        0.0122,  0.0271,  0.0436, -0.3719,  0.0436,  0.0271,  0.0122,
        0.0104,  0.0231,  0.0372,  0.0436,  0.0372,  0.0231,  0.0104,
        0.0064,  0.0143,  0.0231,  0.0271,  0.0231,  0.0143,  0.0064,
        0.0028,  0.0064,  0.0104,  0.0122,  0.0104,  0.0064,  0.0028
    };
    
    float result = 0.0;
    int index = 0;
    
    for(int y = -3; y <= 3; y++)
    {
        for(int x = -3; x <= 3; x++)
        {
            float2 sampleUV = uv + float2(x, y) * texelSize;
            float sample = GetChannelValue(tex2D(tex, sampleUV));
            result += sample * kernel[index];
            index++;
        }
    }
    
    return result;
}

float LaplacianOfGaussian9x9(sampler2D tex, float2 uv, float2 texelSize)
{
    const float kernel[81] = {
        0.0011,  0.0019,  0.0028,  0.0036,  0.0039,  0.0036,  0.0028,  0.0019,  0.0011,
        0.0019,  0.0034,  0.0050,  0.0064,  0.0069,  0.0064,  0.0050,  0.0034,  0.0019,
        0.0028,  0.0050,  0.0074,  0.0094,  0.0102,  0.0094,  0.0074,  0.0050,  0.0028,
        0.0036,  0.0064,  0.0094,  0.0121,  0.0131,  0.0121,  0.0094,  0.0064,  0.0036,
        0.0039,  0.0069,  0.0102,  0.0131, -0.2176,  0.0131,  0.0102,  0.0069,  0.0039,
        0.0036,  0.0064,  0.0094,  0.0121,  0.0131,  0.0121,  0.0094,  0.0064,  0.0036,
        0.0028,  0.0050,  0.0074,  0.0094,  0.0102,  0.0094,  0.0074,  0.0050,  0.0028,
        0.0019,  0.0034,  0.0050,  0.0064,  0.0069,  0.0064,  0.0050,  0.0034,  0.0019,
        0.0011,  0.0019,  0.0028,  0.0036,  0.0039,  0.0036,  0.0028,  0.0019,  0.0011
    };
    
    float result = 0.0;
    int index = 0;
    
    for(int y = -4; y <= 4; y++)
    {
        for(int x = -4; x <= 4; x++)
        {
            float2 sampleUV = uv + float2(x, y) * texelSize;
            float sample = GetChannelValue(tex2D(tex, sampleUV));
            result += sample * kernel[index];
            index++;
        }
    }
    
    return result;
}

float ComputeLoGWeight(float2 offset, float sigma)
{
    float sigma2 = sigma * sigma;
    float sigma4 = sigma2 * sigma2;
    float r2 = dot(offset, offset);
    
    float gaussianTerm = exp(-r2 / (2.0 * sigma2));
    float laplacianTerm = 1.0 - (r2 / (2.0 * sigma2));
    
    return (-1.0 / (3.14159265359 * sigma4)) * laplacianTerm * gaussianTerm;
}

float LaplacianOfGaussianCustom(sampler2D tex, float2 uv, float2 texelSize, int kernelRadius, float sigma)
{
    float result = 0.0;
    
    for(int y = -kernelRadius; y <= kernelRadius; y++)
    {
        for(int x = -kernelRadius; x <= kernelRadius; x++)
        {
            float2 offset = float2(x, y);
            float2 sampleUV = uv + offset * texelSize;
            
            float weight = ComputeLoGWeight(offset, sigma);
            float sample = GetChannelValue(tex2D(tex, sampleUV));
            
            result += sample * weight;
        }
    }
    
    return result;
}