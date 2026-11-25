// A dyed rabbit layer on the paper-doll figure: the dye's hue at the fur's own brightness, ink, eyes and the
// darkest shading kept (the SpriteRenderer twin of "VuonTra/UI Dye Fur"). The dye comes in as the renderer's colour.
Shader "VuonTra/Sprite Dye Fur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Strength ("Dye strength", Range(0, 1)) = 0.75
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };
            sampler2D _MainTex;
            float _Strength;
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                half4 tex = tex2D(_MainTex, i.texcoord);
                half3 dye = i.color.rgb;
                const half3 luma = half3(0.299, 0.587, 0.114);
                half l = dot(tex.rgb, luma), dl = dot(dye, luma);
                half3 dyed = saturate(dye + (l - dl));
                half keep = saturate((0.32 - l) / 0.14);
                half amount = _Strength * (1 - keep) * saturate(length(1 - dye) * 8);
                return half4(lerp(tex.rgb, dyed, amount), tex.a * i.color.a);
            }
        ENDCG
        }
    }
}
