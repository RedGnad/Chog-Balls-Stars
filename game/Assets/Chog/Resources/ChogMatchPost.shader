// The light post-processing of a match on a computer (Chog.Match.ChogMatchPost), in the spirit of a
// fast bloom, an LDR grading, a vignette and FXAA, in five short passes:
//   0  the bright parts of the picture, at half size;
//   1  a smaller picture (4 taps, half each way);
//   2  a larger picture (a tent of 4 taps), added to what that size held;
//   3  the final picture: edge smoothing (FXAA, its short form), the glow added, a light grading
//      (contrast, saturation, warm highlights and violet shadows) and a violet vignette.
// The project draws in gamma space: the grading works on the colours as shown.
Shader "Chog/MatchPost"
{
    Properties
    {
        _MainTex ("Picture", 2D) = "white" {}
        _Bloom ("Glow", 2D) = "black" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _Bloom;
    half _Threshold;
    half _Knee;
    half _BloomIntensity;
    half _Contrast;
    half _Saturation;
    half4 _Lift;
    half4 _Gain;
    half _Vignette;
    half4 _VignetteColor;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    half3 Box4(float2 uv, float spread)
    {
        float4 d = _MainTex_TexelSize.xyxy * float4(-1, -1, 1, 1) * spread;
        return (tex2D(_MainTex, uv + d.xy).rgb + tex2D(_MainTex, uv + d.zy).rgb
              + tex2D(_MainTex, uv + d.xw).rgb + tex2D(_MainTex, uv + d.zw).rgb) * 0.25;
    }

    half Luma(half3 c) { return dot(c, half3(0.299, 0.587, 0.114)); }

    half4 Prefilter(v2f i) : SV_Target
    {
        half3 c = Box4(i.uv, 1.0);
        // By luminance, so that the arena's lavenders (a blue channel near 1) do not glow; only what
        // is really bright does (beams, sparks, white flashes).
        half brightness = Luma(c);
        // A soft knee: what lies just under the threshold glows a little.
        half soft = clamp(brightness - _Threshold + _Knee, 0, 2 * _Knee);
        soft = soft * soft / (4 * _Knee + 1e-4);
        half contribution = max(soft, brightness - _Threshold) / max(brightness, 1e-4);
        return half4(c * contribution, 1);
    }

    half4 Down(v2f i) : SV_Target
    {
        return half4(Box4(i.uv, 1.0), 1);
    }

    half4 Up(v2f i) : SV_Target
    {
        return half4(Box4(i.uv, 0.5), 1);
    }

    half3 Fxaa(float2 uv)
    {
        float2 texel = _MainTex_TexelSize.xy;
        half3 rgbNW = tex2D(_MainTex, uv + float2(-1, -1) * texel).rgb;
        half3 rgbNE = tex2D(_MainTex, uv + float2(1, -1) * texel).rgb;
        half3 rgbSW = tex2D(_MainTex, uv + float2(-1, 1) * texel).rgb;
        half3 rgbSE = tex2D(_MainTex, uv + float2(1, 1) * texel).rgb;
        half3 rgbM = tex2D(_MainTex, uv).rgb;
        half lumaNW = Luma(rgbNW);
        half lumaNE = Luma(rgbNE);
        half lumaSW = Luma(rgbSW);
        half lumaSE = Luma(rgbSE);
        half lumaM = Luma(rgbM);
        half lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
        half lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
        // Flat areas keep their pixel: no blur where there is no edge.
        if (lumaMax - lumaMin < max(0.0312, lumaMax * 0.125))
            return rgbM;

        float2 dir;
        dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
        dir.y = ((lumaNW + lumaSW) - (lumaNE + lumaSE));
        half reduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * 0.125), 1.0 / 128.0);
        half rcpMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + reduce);
        dir = clamp(dir * rcpMin, -8.0, 8.0) * texel;
        half3 rgbA = 0.5 * (tex2D(_MainTex, uv + dir * (1.0 / 3.0 - 0.5)).rgb + tex2D(_MainTex, uv + dir * (2.0 / 3.0 - 0.5)).rgb);
        half3 rgbB = rgbA * 0.5 + 0.25 * (tex2D(_MainTex, uv - dir * 0.5).rgb + tex2D(_MainTex, uv + dir * 0.5).rgb);
        half lumaB = Luma(rgbB);
        return (lumaB < lumaMin || lumaB > lumaMax) ? rgbA : rgbB;
    }

    half4 Final(v2f i) : SV_Target
    {
        half3 c = Fxaa(i.uv);
        c += tex2D(_Bloom, i.uv).rgb * _BloomIntensity;
        // Lift the shadows toward violet, warm the highlights, then contrast and saturation.
        c = c * _Gain.rgb + _Lift.rgb * (1 - c);
        c = (c - 0.5) * _Contrast + 0.5;
        half l = Luma(c);
        c = lerp(half3(l, l, l), c, _Saturation);
        // The vignette: the corners darken toward a deep violet.
        float2 d = (i.uv - 0.5) * float2(_MainTex_TexelSize.y * _MainTex_TexelSize.z, 1);
        half v = smoothstep(0.25, 1.1, dot(d, d));
        c = lerp(c, c * _VignetteColor.rgb, v * _Vignette);
        return half4(saturate(c), 1);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment Prefilter
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment Down
            ENDCG }
        Pass { Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment Up
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment Final
            #pragma target 3.0
            ENDCG }
    }
}
