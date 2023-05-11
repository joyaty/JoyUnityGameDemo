Shader "Joy/Tool/TextureMappingShader"
{
    Properties
    {
        [MainTexture] _BaseMap ("BaseMap", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "TextureMapping"
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            Varyings vert (Attributes input)
            {
                Varyings output;
                // UV坐标系[0,1]，NDC坐标系[-1,1]，UV坐标与NDC坐标的Y可能需要翻转（DX的UV坐标原点在左上角，屏幕空间坐标原点在左下角）
                float2 uvToNDC = input.uv * 2 - 1;
                uvToNDC.y = uvToNDC.y * _ProjectionParams.x;
                output.positionHCS = float4(uvToNDC, 0, 1);
                // 顶点坐标的NDC空间坐标作为UV坐标
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
                output.uv = float2(positionInputs.positionNDC.x / positionInputs.positionNDC.w, positionInputs.positionNDC.y / positionInputs.positionNDC.w);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // sample the texture
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
