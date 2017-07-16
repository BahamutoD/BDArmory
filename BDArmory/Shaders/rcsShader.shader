// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/rcsShader" {

	Properties{
		_RCSCOLOR("RCSCOLOR", Color) = (1, 1, 1, 1)
	}

		SubShader{

			Pass{

			CGPROGRAM

			// DEFINES
			#pragma vertex RCSVertexShader
			#pragma fragment RCSFragmentShader

			// INCLUDES
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "UnityStandardBRDF.cginc"


			// uniforms from properties
			fixed4 _RCSCOLOR;

			struct VertexData {
				float4 position : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct Interpolators {
				float4 position : SV_POSITION;
				float3 normal : TEXCOORD1;
			};



			// Main Vertex Program
			Interpolators RCSVertexShader(VertexData v) {
				Interpolators i;
				i.position = UnityObjectToClipPos(v.position);
				i.normal = UnityObjectToWorldNormal(v.normal);
				//i.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return i;
			}



			// Main Fragment Program
			float4 RCSFragmentShader(Interpolators i) : SV_TARGET{
				float3 lightDir = _WorldSpaceLightPos0.xyz;
				float3 lightColor = _RCSCOLOR.rgb;
				float3 reflectionDir = reflect(-lightDir, i.normal);
				return DotClamped(lightDir, reflectionDir);
			}


			ENDCG

			} //PASS
		} //SUBSHADER

}
