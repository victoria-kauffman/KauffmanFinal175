#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Controls;
#endif

using UnityEngine;

public class PropertiesChanger : MonoBehaviour
{
    [SerializeField]
    WavesSettings wavesSettings;
    void Update()
    {
        if (Input.GetKey(KeyCode.P))
        {
            wavesSettings.local.windSpeed += 0.005f;
        }
        else if (Input.GetKey(KeyCode.O) && wavesSettings.local.windSpeed > 0.005f)
        {
            wavesSettings.local.windSpeed -= 0.005f;
        }
        else if (Input.GetKey(KeyCode.L))
        {
            wavesSettings.swell.windSpeed += 0.005f;
        } 
        else if (Input.GetKey(KeyCode.K) && wavesSettings.swell.windSpeed > 0.005f)
        {
            wavesSettings.swell.windSpeed -= 0.005f;


        } else if (Input.GetKey(KeyCode.I))
        {
            wavesSettings.local.windDirection += 0.05f;
        }
        else if (Input.GetKey(KeyCode.U))
        {
            wavesSettings.local.windDirection -= 0.05f;
        }
        else if (Input.GetKey(KeyCode.J))
        {
            wavesSettings.swell.windDirection += 0.05f;
        } 
        else if (Input.GetKey(KeyCode.H))
        {
            wavesSettings.swell.windDirection -= 0.05f;
        } 
    }
}