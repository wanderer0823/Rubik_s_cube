Shader "Unlit/line2"
{
    Properties
    {
        _MainTex ("BaseTex", 2D) = "white" {}
        _LineWidth("_LineWidth",Range(0,5))=0.1
        _Count("细分数",int)=1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM

            #pragma geometry geom//注意声明
            #pragma vertex vert
            #pragma fragment frag
            #pragma hull HS
            #pragma domain DS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            float _LineWidth;
            int _Count;

            struct appdata
            {
                float4 vertex:POSITION;
                };

            struct VertexOut{
                float4 positionOS:POSITION;
                float3 normal:NORMAL;
                };

            VertexOut vert(appdata v)
            {
                VertexOut o;
                o.positionOS=v.vertex;
                return o;
                }

            //常量外壳着色器
            struct PatchTess{
                float edgeTess[3]:SV_TessFactor;     // 三条边的细分因子
                float insideTess:SV_InsideTessFactor;// 内部细分因子
                };
            
            
            PatchTess ConstantHS(InputPatch<VertexOut,3>patch,//处理三个控制点的面片
                uint patchID:SV_PrimitiveID){
                    PatchTess pt;

                    //将该面片从各方面均匀地镶嵌处理为3等分
                    pt.edgeTess[0]=_Count;
                    pt.edgeTess[1]=_Count;
                    pt.edgeTess[2]=_Count;

                    pt.insideTess=_Count;//三角形内部的细分份数
                    return pt;
                    }

            //控制点外壳着色器
            struct HullOut{
                float3 positionOS:TEXCOORD0;
                };

            [domain("tri")]                  //patch的类型
            [partitioning("integer")]        //曲面细分的模式；integer是突变均分
            [outputtopology("triangle_cw")]  //通过细分所创的三角形的绕序，CW=Clockwise = 顺时针;CCW = Counter-Clockwise = 逆时针
            [outputcontrolpoints(3)]         //外壳着色器的执行次数，每次执行都输出一个控制点
            [patchconstantfunc("ConstantHS")]//指定常量外壳着色器函数名称的字符串
            [maxtessfactor(64.0)]            //告知驱动程序该着色器所用的最大细分因子
            HullOut HS(InputPatch<VertexOut,3> input,
                uint controlPointId:SV_OutputControlPointID,uint patchId:SV_PrimitiveID){
                    HullOut output;

                    output.positionOS=input[controlPointId].positionOS;

                    return output;
            }

            //域着色器
            struct DomainOut{
                float4 vertex:POSITION;
                };

            [domain("tri")]
            DomainOut DS(PatchTess patchTess,float3 bary:SV_DomainLocation,//当前处理的细分顶点在原始三角形面片中的重心坐标
                const OutputPatch<HullOut,3>patch)//patch[]是原本三角形三顶点的坐标
                {
                    DomainOut o;
                    //使用重心坐标插值出每个细分顶点的实际位置
                    float3 p=patch[0].positionOS*bary.x+patch[1].positionOS*bary.y+patch[2].positionOS*bary.z;
                    o.vertex=float4(p,1);
                    return o;
                    }

            
            //几何着色器
            struct GeomOut{
                float4 vertex:SV_POSITION;
                float3 barycentric:TEXCOORD0;
                };


            [maxvertexcount(3)]                     //最大输出顶点数
            void geom(
                triangle DomainOut input[3],              //输入一个三角形的三个顶点
                inout TriangleStream<GeomOut> triStream  //输出三角形流
                )
            {
                GeomOut o;

                o.barycentric=float3(1,0,0);
                o.vertex=TransformObjectToHClip(input[0].vertex);
                triStream.Append(o);

                o.barycentric=float3(0,1,0);
                o.vertex=TransformObjectToHClip(input[1].vertex);
                triStream.Append(o);

                o.barycentric=float3(0,0,1);
                o.vertex=TransformObjectToHClip(input[2].vertex);
                triStream.Append(o);

                triStream.RestartStrip();
                }
                
                half4 frag(GeomOut i):SV_Target
                {
                    float3 dis=i.barycentric;

                    float nearest=min(min(dis.x,dis.y),dis.z);

                    if(nearest<_LineWidth)
                        return half4(0,0,0,1);

                    return half4(1,1,1,1);
                    }

            ENDHLSL
        }
    }
}
