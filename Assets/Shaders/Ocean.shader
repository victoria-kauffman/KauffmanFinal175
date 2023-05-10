Shader "Ocean/Ocean"
{
    Properties
    {

        _Col ("Color", Color) = (0.035, 0.122, 0.2, 1)
        _SSSCol ("Subsurface Scattering (SSS) Color", Color) = (0.153, 0.886, 0.992, 1)
        _SSSStrength ("SSS Strength", Range(0, 1)) = 0.133
        _SSSScale ("SSS Scale", range(0.1, 50)) = 4.8
        _SSSBase("SSS Base", Range(-5,1)) = -0.1
        _ScaleLOD("Level Of Detail (LOD) Scale", Range(1, 10)) = 7.13

        _MaxGloss("Max Gloss", Range(0,1)) = 0.9
        _Roughness("Distant Roughness", Range(0,1)) = 0.311
        _RoughnessScale("Roughness Scale", Range(0, 0.01)) = 0.0044

        _FoamCol("Foam Color", Color) = (1,1,1,1)
        _FoamTex("Foam Texture", 2D) = "grey" {}
        _FoamBiasA("Foam Bias LOD A", Range(0,7)) = 0.84
        _FoamBiasB("Foam Bias LOD B", Range(0,7)) = 1.83
        _FoamBiasC("Foam Bias LOD C", Range(0,7)) = 2.7
        _FoamScale("Foam Scale", Range(0,20)) = 2.4
        _ContactFoam("Contact Foam", Range(0,1)) = 1 


        [Header(Cascade A)]
        [HideInInspector]_DispCA("Disp Cascade A", 2D) = "black" {}
        [HideInInspector]_DerivCA("Deriv Cascade A", 2D) = "black" {}
        [HideInInspector]_TurbCA("Turb Cascade A", 2D) = "white" {}
        [Header(Cascade B)]
        [HideInInspector]_DispCB("Disp Cascade B", 2D) = "black" {}
        [HideInInspector]_DerivCB("Deriv Cascade B", 2D) = "black" {}
        [HideInInspector]_TurbCB("Turb Cascade B", 2D) = "white" {}
        [Header(Cascade C)]
        [HideInInspector]_DispCC("Disp Cascade C", 2D) = "black" {}
        [HideInInspector]_DerivCC("Deriv Cascade C", 2D) = "black" {}
        [HideInInspector]_TurbCC("Turb Cascade C", 2D) = "white" {}
    }
        SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma multi_compile _ MID CLOSE
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 4.0


        struct Input
        {
            float2 worldUV;
            float4 lodScales;
            float3 viewVector;
            float3 worldNormal;
            float4 screenPos;
            INTERNAL_DATA
        };

        sampler2D _DispCA;
        sampler2D _DerivCA;
        sampler2D _TurbCA;

        sampler2D _DispCB;
        sampler2D _DerivCB;
        sampler2D _TurbCB;

        sampler2D _DispCC;
        sampler2D _DerivCC;
        sampler2D _TurbCC;

        float LengthScale0, LengthScale1, LengthScale2,
              _ScaleLOD, _SSSBase, _SSSScale;


        fixed4 _Col, _FoamCol, _SSSCol;
        float _SSSStrength;
        float _Roughness, _RoughnessScale, _MaxGloss;
        float _FoamBiasA, _FoamBiasB, _FoamBiasC, _FoamScale, _ContactFoam;
        sampler2D _CameraDepthTexture;
        sampler2D _FoamTex;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float4 worldUV = float4(worldPos.xz, 0, 0);
            o.worldUV = worldUV.xy;

            o.viewVector = _WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz;
            float viewDist = length(o.viewVector);
            
            float lod_c0 = min(_ScaleLOD * LengthScale0 / viewDist, 1);
            float lod_c1 = min(_ScaleLOD * LengthScale1 / viewDist, 1);
            float lod_c2 = min(_ScaleLOD * LengthScale2 / viewDist, 1);
            

            float3 disp = 0;
            float largeWavesBias = 0;

            
            disp += tex2Dlod(_DispCA, worldUV / LengthScale0) * lod_c0;
            largeWavesBias = disp.y;
            #if defined(MID) || defined(CLOSE)
            disp += tex2Dlod(_DispCB, worldUV / LengthScale1) * lod_c1;
            #endif
            #if defined(CLOSE)
            disp += tex2Dlod(_DispCC, worldUV / LengthScale2) * lod_c2;
            #endif
            v.vertex.xyz += mul(unity_WorldToObject,disp);

            o.lodScales = float4(lod_c0, lod_c1, lod_c2, max(disp.y - largeWavesBias * 0.8 - _SSSBase, 0) / _SSSScale);
        }

        float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1, 0, 0));
            float3 t2w1 = WorldNormalVector(IN, float3(0, 1, 0));
            float3 t2w2 = WorldNormalVector(IN, float3(0, 0, 1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }

        float pow5(float f)
        {
            return f * f * f * f * f;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float4 deriv = tex2D(_DerivCA, IN.worldUV / LengthScale0);
            #if defined(MID) || defined(CLOSE)
            deriv += tex2D(_DerivCB, IN.worldUV / LengthScale1) * IN.lodScales.y;
            #endif

            #if defined(CLOSE)
            deriv += tex2D(_DerivCC, IN.worldUV / LengthScale2) * IN.lodScales.z;
            #endif

            float2 slope = float2(deriv.x / (1 + deriv.z),
                deriv.y / (1 + deriv.w));
            float3 worldNormal = normalize(float3(-slope.x, 1, -slope.y));

            o.Normal = WorldToTangentNormalVector(IN, worldNormal);
            
            #if defined(CLOSE)
            float jacobian = tex2D(_TurbCA, IN.worldUV / LengthScale0).x
                + tex2D(_TurbCB, IN.worldUV / LengthScale1).x
                + tex2D(_TurbCC, IN.worldUV / LengthScale2).x;
            jacobian = min(1, max(0, (-jacobian + _FoamBiasC) * _FoamScale));
            #elif defined(MID)
            float jacobian = tex2D(_TurbCA, IN.worldUV / LengthScale0).x
                + tex2D(_TurbCB, IN.worldUV / LengthScale1).x;
            jacobian = min(1, max(0, (-jacobian + _FoamBiasB) * _FoamScale));
            #else
            float jacobian = tex2D(_TurbCA, IN.worldUV / LengthScale0).x;
            jacobian = min(1, max(0, (-jacobian + _FoamBiasA) * _FoamScale));
            #endif

            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            float backgroundDepth =
                LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
            float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(IN.screenPos.z);
            float depthDifference = max(0, backgroundDepth - surfaceDepth - 0.1);
            float foam = tex2D(_FoamTex, IN.worldUV * 0.5 + _Time.r).r;
            jacobian += _ContactFoam * saturate(max(0, foam - depthDifference) * 5) * 0.9;

            o.Albedo = lerp(0, _FoamCol, jacobian);
            float distanceGloss = lerp(1 - _Roughness, _MaxGloss, 1 / (1 + length(IN.viewVector) * _RoughnessScale));
            o.Smoothness = lerp(distanceGloss, 0, jacobian);
            o.Metallic = 0;

            float3 viewDir = normalize(IN.viewVector);
            float3 H = normalize(-worldNormal + _WorldSpaceLightPos0);
            float ViewDotH = pow5(saturate(dot(viewDir, -H))) * 30 * _SSSStrength;
            fixed3 color = lerp(_Col, saturate(_Col + _SSSCol.rgb * ViewDotH * IN.lodScales.w), IN.lodScales.z);

            float fresnel = dot(worldNormal, viewDir);
            fresnel = saturate(1 - fresnel);
            fresnel = pow5(fresnel);

            o.Emission = lerp(color * (1 - fresnel), 0, jacobian);
        }
        ENDCG
    }
        FallBack "Diffuse"
}