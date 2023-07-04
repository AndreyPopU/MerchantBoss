// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/SteelSkinShader"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _ColorToKeep("Color to keep", Color) = (0,0,0,1)
        _ReplaceColor("Replace color", Color) = (0,0,0,0)
    }

        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
                "PreviewType" = "Plane"
                "CanUseSpriteAtlas" = "True"
            }

            Cull Off
            Lighting Off
            ZWrite Off
            Fog { Mode Off }
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag          
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    fixed4 color : COLOR;
                    half2 texcoord  : TEXCOORD0;
                };

                fixed4 _ColorToKeep;
                fixed4 _ReplaceColor;

                v2f vert(appdata_t IN)
                {
                    v2f OUT;
                    OUT.vertex = UnityObjectToClipPos(IN.vertex);
                    OUT.texcoord = IN.texcoord;
                    OUT.color = IN.color;

                    return OUT;
                }

                sampler2D _MainTex;

                fixed4 frag(v2f IN) : COLOR
                {
                    // Color of the texture
                    float4 texColor = tex2D(_MainTex, IN.texcoord);

                    if (texColor.a > 0) texColor = all(texColor == _ColorToKeep) ? texColor : _ReplaceColor;

                    return texColor * IN.color;
                }
            ENDCG
            }
        }
}
