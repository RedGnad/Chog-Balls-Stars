// The arena's floor, walls, crates and plants (ChogArenaSkin puts it on the sample's materials when
// the arena scene loads): the material's colour times a diffuse matcap read in view space, as the
// heroes are drawn (tops facing the key lit, faces turned away from it toward a violet; the same
// for both teams, whose cameras look from opposite ends), darkened toward violet where the key
// light's shadow falls (to about 75%), and a little at the foot of walls and crates (a contact
// shade from the height, in place of baked occlusion). Its numbers are globals set by
// Chog.Match.ChogMatchLook (MatchLookTuning):
//   _ChogLightView    the key's direction in view space (shared with the heroes);
//   _ChogArenaShade   the colour faces turn to away from the key;
//   _ChogArenaRamp    x slope and y offset of the light (saturate(cosine * x + y)), z exposure;
//   _ChogArenaShadow  what a full shadow multiplies a colour by;
//   _ChogArenaFoot    x how much the foot of a wall darkens, y up to what height;
//   _ChogKey          the warmth of the key over what it reaches (alpha 1 when set, else white).
Shader "Chog/Arena"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        _ChogFloorTex ("Floor slabs", 2D) = "white" {}
        _ChogFloorPattern ("Floor pattern weight", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _ChogFloorTex;
            half _ChogFloorPattern;
            half4 _ChogLightView;
            half4 _ChogArenaShade;
            half4 _ChogArenaRamp;
            half4 _ChogArenaShadow;
            float4 _ChogArenaFoot;
            half4 _ChogKey;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 light : TEXCOORD1;
                // x: height in the world, y: how much the surface stands upright (0 on a floor).
                float2 foot : TEXCOORD2;
                float2 groundUv : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                float3 normal = UnityObjectToWorldNormal(v.normal);
                half3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_V, normal));
                // Per vertex: the arena's shapes are flat faces, so the vertex is enough and costs a
                // phone nothing per pixel.
                half light = saturate(dot(viewNormal, _ChogLightView.xyz) * _ChogArenaRamp.x + _ChogArenaRamp.y);
                o.light = lerp(_ChogArenaShade.rgb, half3(1, 1, 1), light) * _ChogArenaRamp.z;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.foot = float2(world.y, 1 - abs(normal.y));
                o.groundUv = world.xz * 0.055;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                fixed3 floor = tex2D(_ChogFloorTex, i.groundUv).rgb;
                albedo = lerp(albedo, floor, _ChogFloorPattern);
                half shadow = SHADOW_ATTENUATION(i);
                half3 key = _ChogKey.a > 0.5 ? _ChogKey.rgb : half3(1, 1, 1);
                half3 light = i.light * lerp(_ChogArenaShadow.rgb, key, shadow);
                half foot = 1 - _ChogArenaFoot.x * i.foot.y * (1 - saturate(i.foot.x / _ChogArenaFoot.y));
                return fixed4(albedo * light * foot, 1);
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }

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
