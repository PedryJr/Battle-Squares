float kernRand(in float2 uv)
{
    float noiseX = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
    float noiseY = sqrt(1 - noiseX * noiseX);
    return float2(noiseX, noiseY);
}

static const float2 PoissonSamples[64] =
{
    float2(-0.5119625f, -0.4827938f), float2(-0.2171264f, -0.4768726f),
    float2(-0.7552931f, -0.2426507f), float2(-0.7136765f, -0.4496614f),
    float2(-0.5938849f, -0.6895654f), float2(-0.3148003f, -0.7047654f),
    float2(-0.42215f, -0.2024607f), float2(-0.9466816f, -0.2014508f),
    float2(-0.8409063f, -0.03465778f), float2(-0.6517572f, -0.07476326f),
    float2(-0.1041822f, -0.02521214f), float2(-0.3042712f, -0.02195431f),
    float2(-0.5082307f, 0.1079806f), float2(-0.08429877f, -0.2316298f),
    float2(-0.9879128f, 0.1113683f), float2(-0.3859636f, 0.3363545f),
    float2(-0.1925334f, 0.1787288f), float2(0.003256182f, 0.138135f),
    float2(-0.8706837f, 0.3010679f), float2(-0.6982038f, 0.1904326f),
    float2(0.1975043f, 0.2221317f), float2(0.1507788f, 0.4204168f),
    float2(0.3514056f, 0.09865579f), float2(0.1558783f, -0.08460935f),
    float2(-0.0684978f, 0.4461993f), float2(0.3780522f, 0.3478679f),
    float2(0.3956799f, -0.1469177f), float2(0.5838975f, 0.1054943f),
    float2(0.6155105f, 0.3245716f), float2(0.3928624f, -0.4417621f),
    float2(0.1749884f, -0.4202175f), float2(0.6813727f, -0.2424808f),
    float2(-0.6707711f, 0.4912741f), float2(0.0005130528f, -0.8058334f),
    float2(0.02703013f, -0.6010728f), float2(-0.1658188f, -0.9695674f),
    float2(0.4060591f, -0.7100726f), float2(0.7713396f, -0.4713659f),
    float2(0.573212f, -0.51544f), float2(-0.3448896f, -0.9046497f),
    float2(0.1268544f, -0.9874692f), float2(0.7418533f, -0.6667366f),
    float2(0.3492522f, 0.5924662f), float2(0.5679897f, 0.5343465f),
    float2(0.5663417f, 0.7708698f), float2(0.7375497f, 0.6691415f),
    float2(0.2271994f, -0.6163502f), float2(0.2312844f, 0.8725659f),
    float2(0.4216993f, 0.9002838f), float2(0.4262091f, -0.9013284f),
    float2(0.2001408f, -0.808381f), float2(0.149394f, 0.6650763f),
    float2(-0.09640376f, 0.9843736f), float2(0.7682328f, -0.07273844f),
    float2(0.04146584f, 0.8313184f), float2(0.9705266f, -0.1143304f),
    float2(0.9670017f, 0.1293385f), float2(0.9015037f, -0.3306949f),
    float2(-0.5085648f, 0.7534177f), float2(0.9055501f, 0.3758393f),
    float2(0.7599946f, 0.1809109f), float2(-0.2483695f, 0.7942952f),
    float2(-0.4241052f, 0.5581087f), float2(-0.1020106f, 0.6724468f),
};

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

float4 PoissonAverage(sampler2D tex, float2 uv, float2 texelSize, int samples)
{
    float4 result = float4(0, 0, 0, 0); 
    float2 offsetUV = float2(0, 0);
    samples = clamp(samples, 1, 64);
    for (int i = 0; i < samples; i++)
    {
        offsetUV = PoissonSamples[kernRand(uv + offsetUV) * 63];
        result += tex2D(tex, uv + offsetUV * texelSize);
    }
    return result / samples;
}

float4 KernelAverage9x9(sampler2D tex, float2 uv, float2 texelSize)
{
    
    float4 result = float4(0, 0, 0, 0);
    
    for (int y = -4; y <= 4; y++)
    {
        for (int x = -4; x <= 4; x++)
        {
            float2 sampleUV = uv + float2(x, y) * texelSize;
            result += tex2D(tex, sampleUV);
        }
    }
    return result / 81;
}

float4 KernelAverage9x9Disc(sampler2D tex, float2 uv, float2 texelSize)
{
    
    const float W[9][9] =
    {
        { 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0 },
        { 0.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0 },
        { 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0 },
        { 0.5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.5 },
        { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 },
        { 0.5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.5 },
        { 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0 },
        { 0.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0 },
        { 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0 }
    };
    
    float4 result = float4(0, 0, 0, 0);
    
    for (int y = -4; y <= 4; y++)
    {
        for (int x = -4; x <= 4; x++)
        {
            float2 sampleUV = uv + float2(x, y) * texelSize;
            result += tex2D(tex, sampleUV) * W[x + 4][y + 4];
        }
    }
    return result / 81;
}