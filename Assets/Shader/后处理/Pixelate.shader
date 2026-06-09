Shader "Unlit/Pixelate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 10.0
        _BlurStrength("Blur Strength",Range(0,1))=0.25
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Pixelate"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _PixelSize;
            float _BlurStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return output;
            }

            float4 frag(Varyings input):SV_Target
            {
                float2 uv=input.uv;
                float2 texel=1.0/_ScreenParams.xy;
                float4 blur=(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+texel*float2(-1,-1))+2*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+texel*float2(0,-1))+SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+texel*float2(1,-1))+2*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+texel*float2(-1,0))+4*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv)+2*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+texel*float2(1,0))+SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+texel*float2(-1,1))+2*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+texel*float2(0,1))+SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+texel*float2(1,1)))/16;
                float2 p=floor(uv*_ScreenParams.xy/_PixelSize)*_PixelSize/_ScreenParams.xy;
                float4 pixel=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,p);
                return lerp(pixel,blur,_BlurStrength);
            }
            ENDHLSL
        }
    }
}
