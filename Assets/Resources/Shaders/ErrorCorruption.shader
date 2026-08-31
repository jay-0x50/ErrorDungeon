Shader "ErrorDungeon/Corruption"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.04, 0.08, 0.1, 1)
        [HDR] _ErrorColor ("Error Light", Color) = (0, 1, 1.5, 1)
        _ErrorIntensity ("Error Intensity", Range(0, 3)) = 1.2
        _Metallic ("Metallic", Range(0, 1)) = 0.2
        _Smoothness ("Smoothness", Range(0, 1)) = 0.26
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 220

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _ErrorColor;
        half _ErrorIntensity;
        half _Metallic;
        half _Smoothness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        float Hash31(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 cell = floor(IN.worldPos * float3(2.2, 5.3, 2.2));
            float noise = Hash31(cell);
            float scan = step(0.82, frac(IN.worldPos.y * 7.0 - _Time.y * 1.7 + noise));
            float fracture = step(0.9, noise) + scan * step(0.63, noise);
            float pulse = 0.72 + sin(_Time.y * 3.4 + noise * 9.0) * 0.28;
            o.Albedo = _Color.rgb * (0.72 + noise * 0.35);
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Emission = _ErrorColor.rgb * fracture * pulse * _ErrorIntensity;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
