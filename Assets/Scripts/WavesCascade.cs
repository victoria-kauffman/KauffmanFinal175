using System;
using UnityEngine;

public class WavesCascade
{
    public RenderTexture Disp => disp;
    public RenderTexture Deriv => deriv;
    public RenderTexture Turb => turb;

    public Texture2D GaussianNoise => gaussianNoise;
    public RenderTexture PrecomputedData => precomputedData;
    public RenderTexture InitialSpectrum => initialSpectrum;

    readonly int size;
    readonly ComputeShader oceanographicShader;
    readonly ComputeShader heightsShader;
    readonly ComputeShader derivDispTurbShader;
    readonly FastFourierTransform fft;
    readonly Texture2D gaussianNoise;
    readonly ComputeBuffer paramsBuffer;
    readonly RenderTexture initialSpectrum;
    readonly RenderTexture precomputedData;
    
    readonly RenderTexture buffer;
    readonly RenderTexture DxDz;
    readonly RenderTexture DyDxz;
    readonly RenderTexture DyxDyz;
    readonly RenderTexture DxxDzz;

    readonly RenderTexture disp;
    readonly RenderTexture deriv;
    readonly RenderTexture turb;

    float lambda;

    public WavesCascade(int size,
                        ComputeShader oceanographicShader,
                        ComputeShader heightsShader,
                        ComputeShader derivDispTurbShader,
                        FastFourierTransform fft,
                        Texture2D gaussianNoise) {
        this.size = size;
        this.oceanographicShader = oceanographicShader;
        this.heightsShader = heightsShader;
        this.derivDispTurbShader = derivDispTurbShader;
        this.fft = fft;
        this.gaussianNoise = gaussianNoise;

        KERNEL_INITIAL_SPECTRUM = oceanographicShader.FindKernel("getH0k");
        KERNEL_CONJUGATE_SPECTRUM = oceanographicShader.FindKernel("getH0");
        KERNEL_TIME_DEPENDENT_SPECS = heightsShader.FindKernel("CalculateAmplitudes");
        KERNEL_RESULT_TEXTURES = derivDispTurbShader.FindKernel("FillResultTextures");

        initialSpectrum = FastFourierTransform.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat);
        precomputedData = FastFourierTransform.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat);
        disp = FastFourierTransform.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat);
        deriv = FastFourierTransform.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat, true);
        turb = FastFourierTransform.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat, true);
        paramsBuffer = new ComputeBuffer(2, 6 * sizeof(float));

        buffer = FastFourierTransform.CreateRenderTexture(size);
        DxDz = FastFourierTransform.CreateRenderTexture(size);
        DyDxz = FastFourierTransform.CreateRenderTexture(size);
        DyxDyz = FastFourierTransform.CreateRenderTexture(size);
        DxxDzz = FastFourierTransform.CreateRenderTexture(size);
    }

    public void Dispose()
    {
        paramsBuffer?.Release();
    }

    public void CalculateInitials(WavesSettings wavesSettings, float lengthScale,
                                  float minVal, float maxVal)
    {
        lambda = wavesSettings.lambda;

        oceanographicShader.SetInt(SIZE_PROP, size);
        oceanographicShader.SetFloat(LENGTH_SCALE_PROP, lengthScale);
        oceanographicShader.SetFloat(CUTOFF_HIGH_PROP, maxVal);
        oceanographicShader.SetFloat(CUTOFF_LOW_PROP, minVal);
        wavesSettings.SetParametersToShader(oceanographicShader, KERNEL_INITIAL_SPECTRUM, paramsBuffer);

        oceanographicShader.SetTexture(KERNEL_INITIAL_SPECTRUM, H0K_PROP, buffer);
        oceanographicShader.SetTexture(KERNEL_INITIAL_SPECTRUM, PRECOMPUTED_DATA_PROP, precomputedData);
        oceanographicShader.SetTexture(KERNEL_INITIAL_SPECTRUM, NOISE_PROP, gaussianNoise);
        oceanographicShader.Dispatch(KERNEL_INITIAL_SPECTRUM, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);

        oceanographicShader.SetTexture(KERNEL_CONJUGATE_SPECTRUM, H0_PROP, initialSpectrum);
        oceanographicShader.SetTexture(KERNEL_CONJUGATE_SPECTRUM, H0K_PROP, buffer);
        oceanographicShader.Dispatch(KERNEL_CONJUGATE_SPECTRUM, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
    }

    public void CalculateWavesAtTime(float time)
    {
        // Calculating complex amplitudes
        heightsShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECS, Dxdz_PROP, DxDz);
        heightsShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECS, DyDxz_PROP, DyDxz);
        heightsShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECS, DyxDyz_PROP, DyxDyz);
        heightsShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECS, DxxDzz_PROP, DxxDzz);
        heightsShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECS, H0_PROP, initialSpectrum);
        heightsShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECS, PRECOMPUTED_DATA_PROP, precomputedData);
        heightsShader.SetFloat(TIME_PROP, time);
        heightsShader.Dispatch(KERNEL_TIME_DEPENDENT_SPECS, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);

        // Calculating IFFTs of complex amplitudes
        fft.IFFT2D(DxDz, buffer, true, false, true);
        fft.IFFT2D(DyDxz, buffer, true, false, true);
        fft.IFFT2D(DyxDyz, buffer, true, false, true);
        fft.IFFT2D(DxxDzz, buffer, true, false, true);

        // Filling disp and normals textures
        derivDispTurbShader.SetFloat("DeltaTime", Time.deltaTime);

        derivDispTurbShader.SetTexture(KERNEL_RESULT_TEXTURES, Dxdz_PROP, DxDz);
        derivDispTurbShader.SetTexture(KERNEL_RESULT_TEXTURES, DyDxz_PROP, DyDxz);
        derivDispTurbShader.SetTexture(KERNEL_RESULT_TEXTURES, DyxDyz_PROP, DyxDyz);
        derivDispTurbShader.SetTexture(KERNEL_RESULT_TEXTURES, DxxDzz_PROP, DxxDzz);
        derivDispTurbShader.SetTexture(KERNEL_RESULT_TEXTURES, DISP_PROP, disp);
        derivDispTurbShader.SetTexture(KERNEL_RESULT_TEXTURES, DERIV_PROP, deriv);
        derivDispTurbShader.SetTexture(KERNEL_RESULT_TEXTURES, TURB_PROP, turb);
        derivDispTurbShader.SetFloat(LAMBDA_PROP, lambda);
        derivDispTurbShader.Dispatch(KERNEL_RESULT_TEXTURES, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);

        deriv.GenerateMips();
        turb.GenerateMips();
    }

    const int LOCAL_WORK_GROUPS_X = 8;
    const int LOCAL_WORK_GROUPS_Y = 8;

    // Kernel IDs:
    int KERNEL_INITIAL_SPECTRUM;
    int KERNEL_CONJUGATE_SPECTRUM;
    int KERNEL_TIME_DEPENDENT_SPECS;
    int KERNEL_RESULT_TEXTURES;

    // Property IDs
    readonly int SIZE_PROP = Shader.PropertyToID("Size");
    readonly int LENGTH_SCALE_PROP = Shader.PropertyToID("LengthScale");
    readonly int CUTOFF_HIGH_PROP = Shader.PropertyToID("MaxVal");
    readonly int CUTOFF_LOW_PROP = Shader.PropertyToID("MinVal");

    readonly int NOISE_PROP = Shader.PropertyToID("Noise");
    readonly int H0_PROP = Shader.PropertyToID("H0");
    readonly int H0K_PROP = Shader.PropertyToID("H0K");
    readonly int PRECOMPUTED_DATA_PROP = Shader.PropertyToID("WavesData");
    readonly int TIME_PROP = Shader.PropertyToID("Time");

    readonly int Dxdz_PROP = Shader.PropertyToID("Dxdz");
    readonly int DyDxz_PROP = Shader.PropertyToID("DyDxz");
    readonly int DyxDyz_PROP = Shader.PropertyToID("DyxDyz");
    readonly int DxxDzz_PROP = Shader.PropertyToID("DxxDzz");
    readonly int LAMBDA_PROP = Shader.PropertyToID("Lambda");

    readonly int DISP_PROP = Shader.PropertyToID("Disp");
    readonly int DERIV_PROP = Shader.PropertyToID("Deriv");
    readonly int TURB_PROP = Shader.PropertyToID("Turb"); 
}
