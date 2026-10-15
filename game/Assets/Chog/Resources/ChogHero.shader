// The 3D heroes (ChogHeroModel): one material for every hero, the colours carried by the vertices.
// The alpha of a vertex colour says how the vertex is drawn (ChogHeroModel.LightOf):
//   1    lit: the colour, shaded by the main light over a strong ambient, as the renders of
//        tools/blender/render_chog_traits.py are (flat colours that keep a shade);
//   0.6  unlit: the colour whatever the light (eyes, mouths, lenses, beams);
//   0.2  the outline shell: unlit, and drawn only from its inside, as Blender culls it.
// Nothing is culled by the pipeline: open shapes (a brim, a lens) show from both sides, and the
// outline shells, whose faces point inwards, are dropped here when seen from outside.
Shader "Chog/Hero"
{
    Properties
    {
        _Ambient ("Ambient", Range(0, 1)) = 0.62
        _Diffuse ("Diffuse", Range(0, 1)) = 0.45
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
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                // The outline shell: kept only where its inside faces the camera.
                if (i.color.a < 0.4 && facing < 0)
                    discard;

                float3 normal = normalize(i.normal) * (facing < 0 ? -1.0 : 1.0);
                half light = saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                fixed3 lit = i.color.rgb * (_Ambient + _Diffuse * light * _LightColor0.rgb);
                fixed3 colour = i.color.a > 0.8 ? lit : i.color.rgb;
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
