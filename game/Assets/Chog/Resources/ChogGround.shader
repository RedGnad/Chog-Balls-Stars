// What is drawn flat on the arena's floor under a hero or a structure (Chog.Match.ChogTeamRing): a
// soft round shadow and the ring in the colour of its side, both carried by the vertex colours of
// one small mesh. Drawn after the opaque arena and before the transparent effects, without writing
// depth, pulled toward the camera so that it never fights with the floor.
Shader "Chog/Ground"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "AlphaTest+20" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
