Shader "Unlit/Outline"
{
    Properties
    {
        _outlineWidth("_outlineWidth",Range(0,2))=0.5
        _OutlineColor("_OutlineColor",Color)=(0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"
        "RenderPipeline"="UniversalPipeline"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        float _outlineWidth;
        half4 _OutlineColor;
        ENDHLSL

        Pass{
            Tags{"LightMode"="front"}
            Cull Back
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            struct a2v{
                float4 vertex:POSITION;
                };

            struct v2f{
                float4 pos:SV_POSITION;
                };

            v2f vert(a2v v){
                v2f o;
                o.pos=TransformObjectToHClip(v.vertex);
                return o;
                }

            half4 frag(v2f i):SV_Target{
                
                return half4(1,1,1,1.0);
            }
           
            ENDHLSL
            }

        Pass
        {
            Tags{"LightMode"="edge"}
            Cull Front

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            

            struct Attributes
            {
                float4 vertex:POSITION;
                float3 normal:NORMAL;
            };

            struct Varyings
            {
                float4 pos:SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.pos=TransformObjectToHClip(float4(v.vertex.xyz+normalize(v.normal)*_outlineWidth*0.02,1));
                return o;
            }

            half4 frag(Varyings i):SV_Target
            {
               return half4(_OutlineColor.rgb,1);
            }
            ENDHLSL
        }

        
        

    }
}
