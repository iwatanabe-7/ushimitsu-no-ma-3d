// Full-screen grade used by Ushimitsu.Rendering.ScreenGrade. See that script
// for what each parameter is for.
Shader "Hidden/Ushimitsu/ScreenGrade"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half _Vignette;
            half _Grain;
            float _GrainSeed;
            half _Saturation;
            half _Contrast;
            half4 _ShadowTint;
            half4 _HighlightTint;
            half _Aberration;

            // Cheap stable hash (Dave Hoskins' hash12) for per-pixel grain.
            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            half4 frag(v2f_img i) : SV_Target
            {
                float2 d = i.uv - 0.5;
                // Distance from centre corrected for aspect, so the vignette is round.
                float2 da = d * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                float r2 = dot(da, da);

                // Lens fringing: red and blue drift apart toward the corners only.
                float2 shift = d * _Aberration * r2;
                half4 src = tex2D(_MainTex, i.uv);
                half3 col = half3(tex2D(_MainTex, i.uv - shift).r, src.g, tex2D(_MainTex, i.uv + shift).b);

                half lum = dot(col, half3(0.299, 0.587, 0.114));
                col = lerp(lum.xxx, col, _Saturation);

                // Contrast pivots low: this scene lives in the shadows, and a mid-grey
                // pivot would crush most of it straight to black.
                col = max(0.0, (col - 0.18) * _Contrast + 0.18);

                col *= lerp(_ShadowTint.rgb, _HighlightTint.rgb, smoothstep(0.0, 0.5, lum));

                col *= 1.0 - _Vignette * smoothstep(0.35, 1.05, sqrt(r2));

                // Grain is strongest in the shadows and midtones, like real film.
                float n = Hash12(i.uv * _ScreenParams.xy + _GrainSeed) - 0.5;
                col += n * _Grain * (1.0 - saturate(lum) * 0.6);

                return half4(saturate(col), src.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
