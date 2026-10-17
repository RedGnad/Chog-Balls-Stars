// The 3D shapes the specials are drawn with (Chog.Match.GenesisSpecial3D): the tubes of a laser, the
// fan of light of a visor, the tear and its puddle, the eyeballs of a stare or a burst. One shader for
// two materials (Chog.Match.ChogEffectMaterials): "solid", opaque and written to depth as the heroes
// are, and "glow", see-through, drawn after the opaque scene without writing depth. The colours are in
// the vertices; the first set of texture coordinates says how each vertex is drawn
// (Chog.Match.EffectPaint):
//   x  lit: 1 shades the colour with the heroes' diffuse matcap (ChogHero.shader, the same globals of
//      Chog.Match.ChogMatchLook), 0 leaves it flat;
//   y  edge: how much more opaque a see-through surface gets where it turns away from the viewer, so
//      that a tube or a wall of light reads as a volume (below 0: fainter there, a soft glow);
//   z  core: how much the colour of the second set (rgb) covers the part of the surface that faces
//      the viewer, the lighter core the collection draws down a laser;
//   w  shell: 1 for an ink outline shell, kept only where its inside faces the camera (behind the shape
//      it surrounds), in its own colour, exactly (an enemy's attack is outlined in red).
// The second set carries the core colour (rgb) and, in w, whether the vertex wears _Picture: a ball
// centred on its object's origin, looking along +Z, wears that picture on its front, flat in its
// colours, the picture's rim reaching PictureRim around the ball (Chog.Match.EffectMeshDraft.
// PictureDirection). Worked out for each pixel from the ball's own position, it needs no geometry
// laid over the ball, so nothing fights for depth.
// _Fade scales the opacity of the glow (one value per renderer); _Flash lays white over everything.
Shader "Chog/Effect"
{
    Properties
    {
        _Fade ("Fade", Range(0, 1)) = 1
        _Flash ("Flash (white over everything)", Range(0, 1)) = 0
        _Picture ("Picture worn on the front of a ball", 2D) = "black" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Write depth", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            half _Fade;
            half _Flash;
            sampler2D _Picture;
            // 72 degrees: Chog.Match.EffectMeshDraft.PictureRimDegrees.
            #define PictureRim 1.2566371
            // Globals of the heroes' look (see ChogHero.shader).
            half4 _ChogKey;
            half4 _ChogLightView;
            half4 _ChogHeroShade;
            half4 _ChogHeroRamp;
            half4 _ChogHeroRim;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float4 flags : TEXCOORD0;
                float4 core : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float4 flags : TEXCOORD0;
                float4 core : TEXCOORD1;
                float3 viewNormal : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                float3 toCamera : TEXCOORD4;
                float3 local : TEXCOORD5;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.flags = v.flags;
                o.core = v.core;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewNormal = mul((float3x3)UNITY_MATRIX_V, o.worldNormal);
                o.toCamera = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz;
                o.local = v.vertex.xyz;
                return o;
            }

            // The heroes' matcap (ChogHero.shader): the multiplier of the colour, and the rim toward the key.
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
                if (i.flags.w > 0.5)
                {
                    // The outline shell: kept only where its inside faces the camera.
                    if (facing < 0)
                        discard;
                    return fixed4(lerp(i.color.rgb, fixed3(1, 1, 1), _Flash), i.color.a * _Fade);
                }

                half side = facing < 0 ? -1.0 : 1.0;
                half3 colour = i.color.rgb;
                if (i.flags.x > 0.001)
                {
                    half rim;
                    half3 matcap = Matcap(normalize(i.viewNormal) * side, rim);
                    half3 key = _ChogKey.a > 0.5 ? _ChogKey.rgb : half3(1, 1, 1);
                    colour = lerp(colour, colour * matcap * key + rim * _ChogHeroRim.rgb, i.flags.x);
                }

                // The picture on the front of a ball: as far from its middle as the pixel is around the ball.
                float3 around = normalize(i.local + float3(0, 0, 1e-5));
                float2 across = float2(-around.x, around.y);
                float reach = acos(clamp(around.z, -1.0, 1.0)) / PictureRim;
                float2 at = across / max(length(across), 1e-5) * reach;
                fixed4 picture = tex2D(_Picture, saturate(at * 0.5 + 0.5));
                colour = lerp(colour, picture.rgb, picture.a * step(0.5, i.core.w) * step(reach, 1.0));

                // How squarely the surface faces the viewer: 1 in the middle of a tube, 0 at its sides.
                half toward = saturate(abs(dot(normalize(i.worldNormal), normalize(i.toCamera))));
                colour = lerp(colour, i.core.rgb, i.flags.z * smoothstep(0.84, 0.92, toward));
                half away = 1 - toward;
                half alpha = saturate(i.color.a + i.flags.y * away * away) * _Fade;
                colour = lerp(colour, fixed3(1, 1, 1), _Flash);
                return fixed4(colour, alpha);
            }
            ENDCG
        }
    }
}
