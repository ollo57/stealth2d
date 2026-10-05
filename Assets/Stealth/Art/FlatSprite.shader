Shader "AlleyStealth/FlatSprite"
{
    Properties { [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(UnityFlipSprite(input.positionOS.xyz, unity_SpriteProps.xy));
                output.uv = input.uv; output.color = input.color * unity_SpriteColor;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            { return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color; }
            ENDHLSL
        }
    }
}
