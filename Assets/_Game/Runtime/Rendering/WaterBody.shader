Shader "DarkSaver/WaterBody"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [PerRendererData] _WaterClipEnabled ("Water clipping", Float) = 0
        [PerRendererData] _WaterClipLocalY ("Local cutoff", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex WaterBodyVert
            #pragma fragment WaterBodyFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _WaterClipEnabled;
            float _WaterClipLocalY;
            struct WaterOutput { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float localY : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            WaterOutput WaterBodyVert(appdata_t input)
            {
                v2f standard = SpriteVert(input);
                WaterOutput output;
                UNITY_INITIALIZE_OUTPUT(WaterOutput, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = standard.vertex;
                output.color = standard.color;
                output.texcoord = standard.texcoord;
                output.localY = input.vertex.y;
                return output;
            }
            fixed4 WaterBodyFrag(WaterOutput input) : SV_Target
            {
                if (_WaterClipEnabled > 0.5) clip(input.localY - _WaterClipLocalY);
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
