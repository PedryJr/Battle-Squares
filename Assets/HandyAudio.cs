using System;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

public static class HandyAudio
{
    static int sampleRate = 44100;
    static int fftSize = 1024;
    static float noiseEstimateLevel = 0.01f; // Level of noise to subtract

    public static byte[] SpectralSubtraction(byte[] audioSamples, float noiseEstimateLevel)
    {
        // Convert byte array to float array (assuming 16-bit PCM, little-endian)
        float[] floatSamples = BytesToFloats(audioSamples);

        // Perform FFT (in chunks of 'fftSize')
        int numChunks = floatSamples.Length / fftSize;
        for (int i = 0; i < numChunks; i++)
        {
            // Extract the chunk
            float[] chunk = new float[fftSize];
            Array.Copy(floatSamples, i * fftSize, chunk, 0, fftSize);

            // Apply FFT
            Complex[] fftBuffer = new Complex[fftSize];
            for (int j = 0; j < fftSize; j++)
                fftBuffer[j] = new Complex(chunk[j], 0);

            Fourier.Forward(fftBuffer, FourierOptions.Matlab);

            // Spectral subtraction (subtract estimated noise from magnitude)
            for (int j = 0; j < fftBuffer.Length; j++)
            {
                // Subtract noise estimate (magnitude)
                float magnitude = (float)fftBuffer[j].Magnitude;
                magnitude -= noiseEstimateLevel;

                if (magnitude < 0) magnitude = 0; // Avoid negative values

                // Reapply new magnitude but keep the original phase
                fftBuffer[j] = Complex.FromPolarCoordinates(magnitude, fftBuffer[j].Phase);
            }

            // Apply inverse FFT
            Fourier.Inverse(fftBuffer, FourierOptions.Matlab);

            // Store the cleaned chunk back into floatSamples
            for (int j = 0; j < fftSize; j++)
                floatSamples[i * fftSize + j] = (float)fftBuffer[j].Real;
        }

        // Convert float samples back to byte array
        return FloatsToBytes(floatSamples);
    }

    private static float[] BytesToFloats(byte[] byteSamples)
    {
        int floatArrayLength = byteSamples.Length / 2; // Assuming 16-bit PCM audio
        float[] floatSamples = new float[floatArrayLength];

        for (int i = 0; i < floatArrayLength; i++)
        {
            short sample = BitConverter.ToInt16(byteSamples, i * 2);
            floatSamples[i] = sample / 32768f; // Normalize 16-bit PCM
        }

        return floatSamples;
    }

    private static byte[] FloatsToBytes(float[] floatSamples)
    {
        byte[] byteSamples = new byte[floatSamples.Length * 2]; // 16-bit PCM output
        for (int i = 0; i < floatSamples.Length; i++)
        {
            short sample = (short)(floatSamples[i] * 32768f); // Convert back to 16-bit
            byte[] sampleBytes = BitConverter.GetBytes(sample);
            byteSamples[i * 2] = sampleBytes[0];
            byteSamples[i * 2 + 1] = sampleBytes[1];
        }
        return byteSamples;
    }
}
