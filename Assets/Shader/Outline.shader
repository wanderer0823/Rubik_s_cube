Shader "Unlit/Outline"
{
    Properties
    {
        _OutlineWidth("Outline Width", Range(0,2)) = 0.5
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlinePow("Outline Power", Range(0.5, 5)) = 2
        _OutlineStrength("Outline Strength", Range(0, 10)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Queue" }

        Pass
        {
            Tags { "LightMode" = "MeshOutline"}
            Name "MeshOutline"
            Cull Front
            Blend SrcAlpha One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _OutlineWidth;
            half4 _OutlineColor;
            float _OutlinePow;
            float _OutlineStrength;

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 posWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 offset = normalize(v.normal) * _OutlineWidth * 0.02;
                float4 newPos = float4(v.vertex.xyz + offset, 1);
                o.pos = TransformObjectToHClip(newPos);
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.posWS = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                i.normalWS = normalize(i.normalWS);
                float3 viewDir = normalize(i.posWS - GetCameraPositionWS());
                half ndv = 1-saturate(dot(viewDir, i.normalWS));
                half alpha = pow(ndv, _OutlinePow) * ndv * _OutlineStrength;
                half4 color = _OutlineColor;
                color.a = alpha;
                return color;
            }
            ENDHLSL
        }
    }
}