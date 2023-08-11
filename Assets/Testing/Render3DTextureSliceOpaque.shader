Shader "Custom/Render3DTextureSliceOpaque"
{
    Properties
    {
        _MainTex ("3D RenderTexture", 3D) = "" {}
        _Depth ("Depth", Range(0,1)) = 0.5
    }

    SubShader
    {
        Pass
        {
            HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE3D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Depth;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings MainVS(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 MainPS(Varyings input) : SV_Target
            {
                // Sample the 3D texture at the specified depth
                half4 col = SAMPLE_TEXTURE3D(_MainTex, sampler_MainTex, float3(input.uv, _Depth));
                col.a = 1.0; // Ensure the output is opaque
                return col;
            }

            ENDHLSL

            ZTest Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
        }
    }
}
