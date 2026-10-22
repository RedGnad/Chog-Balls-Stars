// The 3D heroes (ChogHeroModel): one material for every hero, the colours carried by the vertices.
// The alpha of a vertex colour says how the vertex is drawn (ChogHeroModel.LightOf):
//   1    lit: the colour times a diffuse matcap, white where the surface faces the viewer and the
//        key, a lavender crescent toward the lower right of the silhouette, with a light rim on
//        the upper left edge: the toy-like look of arena brawlers. It reads the normal in view
//        space, not a light of the scene, so both teams (whose cameras face each other) see the
//        same shading;
//   0.6  unlit: the colour whatever the light (eyes, mouths, lenses, beams);
//   0.4  glass (a space helmet): drawn on part of the pixels only, in a fixed screen pattern, more of
//        them toward its edges, so what is inside it shows through without any sorting of
//        transparent surfaces; the pixels kept are its colour with a light rim;
//   0.2  the outline shell: unlit, drawn only from its inside, as Blender culls it, in a dark
//        violet rather than black (or its own colour times 0.35 when the library gives it one).
// Nothing is culled by the pipeline: open shapes (a brim, a lens) show from both sides, and the
// outline shells, whose faces point inwards, are dropped here when seen from outside.
// The numbers of the matcap are globals set by Chog.Match.ChogMatchLook (MatchLookTuning), so a
// page can try others without a build:
//   _ChogLookOld     1 draws the look of before 17 October 2026 (shaded by the main light over a
//                    strong ambient), for a comparison on the same build ("?matchlook=0");
//   _ChogKey         the warmth of the key light over lit colours (alpha 1 when set, else white);
//   _ChogLightView   the key's direction in view space;
//   _ChogHeroShade   the crescent's colour; _ChogHeroRamp x where it starts, y its softness;
//   _ChogHeroRim     the rim's colour and, in alpha, its strength; _ChogHeroOutline the outline.
// _Flash lays white over everything, whichever way a vertex is drawn (the hit flash).
Shader "Chog/Hero"
{
    Properties
    {
        _Ambient ("Ambient (look of before)", Range(0, 1)) = 0.62
        _Diffuse ("Diffuse (look of before)", Range(0, 1)) = 0.45
        _Flash ("Flash (white over everything)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _LightColor0;
            half _Ambient;
            half _Diffuse;
            half _Flash;
            // Globals (see the header).
            half _ChogLookOld;
            half4 _ChogKey;
            half4 _ChogLightView;
            half4 _ChogHeroShade;
            half4 _ChogHeroRamp;
            half4 _ChogHeroRim;
            half4 _ChogHeroOutline;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float3 normal : TEXCOORD0;
                float3 viewNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewNormal = mul((float3x3)UNITY_MATRIX_V, o.normal);
                return o;
            }

            // A diffuse matcap drawn by arithmetic instead of a texture: the light is how much the
            // surface faces the viewer and the key; under the ramp it turns to the crescent's colour.
            // Returns the multiplier of the colour; the rim, on the edge toward the key, comes on top.
            half3 Matcap(half3 viewNormal, out half rim)
            {
                half facing = saturate(viewNormal.z);
                half key = dot(viewNormal, _ChogLightView.xyz) * 0.5 + 0.5;
                half light = key * 0.6 + facing * 0.5;
                half shade = 1 - smoothstep(_ChogHeroRamp.x - _ChogHeroRamp.y, _ChogHeroRamp.x + _ChogHeroRamp.y, light);
                half edge = 1 - facing;
                half towardKey = saturate(dot(viewNormal.xy, normalize(_ChogLightView.xy + 1e-4)));
                rim = edge * edge * edge * towardKey * _ChogHeroRim.a;
                return lerp(half3(1, 1, 1), _ChogHeroShade.rgb, shade);
            }

            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                // The outline shell: kept only where its inside faces the camera.
                if (i.color.a < 0.3 && facing < 0)
                    discard;

                // Glass: a fixed screen pattern (interleaved gradient noise, Jimenez 2014, float only so
                // every target compiles it) keeps the share of pixels its coverage says, from a third
                // where it faces the camera to most of them at its edges.
                half glass = 0;
                if (i.color.a >= 0.3 && i.color.a < 0.5)
                {
                    half towardCamera = saturate(abs(normalize(i.viewNormal).z));
                    half coverage = lerp(0.9, 0.3, towardCamera * towardCamera);
                    float pattern = frac(52.9829189 * frac(dot(floor(i.pos.xy), float2(0.06711056, 0.00583715))));
                    if (pattern > coverage)
                        discard;
                    glass = 1 - towardCamera;
                }

                half side = facing < 0 ? -1.0 : 1.0;
                fixed3 colour;
                if (_ChogLookOld > 0.5)
                {
                    float3 normal = normalize(i.normal) * side;
                    half light = saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                    fixed3 lit = i.color.rgb * (_Ambient + _Diffuse * light * _LightColor0.rgb);
                    colour = i.color.a > 0.8 ? lit : i.color.rgb;
                }
                else if (i.color.a > 0.8)
                {
                    half rim;
                    half3 matcap = Matcap(normalize(i.viewNormal) * side, rim);
                    half3 key = _ChogKey.a > 0.5 ? _ChogKey.rgb : half3(1, 1, 1);
                    // The rim as a screen over the shaded colour (22 October 2026): added plainly it
                    // turned the cream of the faces to pure white along their edge; a screen lights a dark
                    // colour as before and can never pass white.
                    half3 shaded = i.color.rgb * matcap * key;
                    colour = shaded + rim * _ChogHeroRim.rgb * (1 - shaded);
                }
                else if (i.color.a < 0.3)
                    colour = max(i.color.rgb * 0.35, _ChogHeroOutline.rgb);
                else
                    colour = i.color.rgb;
                colour = lerp(colour, fixed3(1, 1, 1), glass * glass * 0.6);

                colour = lerp(colour, fixed3(1, 1, 1), _Flash);
                return fixed4(colour, 1);
            }
            ENDCG
        }

        // Casts a shadow like any opaque model, outline shells included.
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
