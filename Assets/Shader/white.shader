Shader "Unlit/white"
{
    Properties
    {
        _Color ("颜色", Color) = (1, 1, 1, 1)
        _ShadowOffset ("阴影偏移", Range(-0.5, 0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        ZWrite On

        // 原颜色 Pass
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Color;

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 cPos : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.cPos = TransformObjectToHClip(v.vertex);
                return o;
            }

            half4 frag(Varyings f) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }

        // 阴影投射 Pass (支持用户调节偏移)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            ENDHLSL
        }
    }
}