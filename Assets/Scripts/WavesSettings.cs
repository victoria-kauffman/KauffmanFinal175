using UnityEngine;

public struct Specsettings
{
    public float scale;
    public float spreadBlend;
    public float alpha;
    public float peakOmega;
    public float gamma;
    public float shortWavesFade;
}

[System.Serializable]
public struct DisplaySpecsettings
{
    [Range(0, 1)]
    public float scale;
    public float windSpeed;
    public float windDirection;
    public float fetch;
    public float peakEnhancement;
    [Range(0, 1)]
    public float spreadBlend;
    public float shortWavesFade;
}

[CreateAssetMenu(fileName = "New waves settings", menuName = "Ocean/Waves Settings")]
public class WavesSettings : ScriptableObject
{
    public float g;
    public float depth;
    [Range(0, 1)]
    public float lambda;
    public DisplaySpecsettings local;
    public DisplaySpecsettings swell;

    Specsettings[] specs = new Specsettings[2];

    public void SetParametersToShader(ComputeShader shader, int kernelIndex, ComputeBuffer paramsBuffer) {
        shader.SetFloat(G_PROP, g);
        shader.SetFloat(DEPTH_PROP, depth);

        FillSettingsStruct(local, ref specs[0]);
        FillSettingsStruct(swell, ref specs[1]);

        paramsBuffer.SetData(specs);
        shader.SetBuffer(kernelIndex, SPECS_PROP, paramsBuffer);
    }

    void FillSettingsStruct(DisplaySpecsettings display, ref Specsettings settings) {
        settings.scale = display.scale;
        settings.spreadBlend = display.spreadBlend;
        settings.alpha = alphaF(g, display.fetch, display.windSpeed);
        settings.peakOmega = omegaPF(g, display.fetch, display.windSpeed);
        settings.gamma = display.peakEnhancement;
        settings.shortWavesFade = display.shortWavesFade;
    }

    float alphaF(float g, float F, float U) {
        return 0.076f * Mathf.Pow( U * U / (F * g), 0.22f);
    }

    float omegaPF(float g, float F, float U) {
        return 22 * Mathf.Pow(g * g / (U * F), 0.33f);
    }

    readonly int G_PROP = Shader.PropertyToID("Gravity");
    readonly int DEPTH_PROP = Shader.PropertyToID("Depth");
    readonly int SPECS_PROP = Shader.PropertyToID("Specs");
}
