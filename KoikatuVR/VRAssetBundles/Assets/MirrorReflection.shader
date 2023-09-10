// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FX/MirrorReflection"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		[HideInInspector] _LeftReflectionTex("", 2D) = "white" {}
		[HideInInspector] _RightReflectionTex("", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

			float4 _MainTex_ST;
			sampler2D _MainTex;
			sampler2D _LeftReflectionTex;
			sampler2D _RightReflectionTex;

			struct VertexInput
			{
				float4 pos : POSITION;
				float2 uv: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 refl : TEXCOORD1;
				float4 pos : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// Same as standard ComputeScreenPos() except that it doesn't call TransformStereoScreenSpaceTex()
			// when stereo instance rendering is enabled. This is important because we need to be able to sample
			// from the entire reflection texture, and not just the left/right half, which is what the normal
			// ComputeScreenPos() would get us.
			inline float4 ComputeScreenPosIgnoreStereo(float4 pos) {
				float4 o = pos * 0.5f;
#if defined(UNITY_HALF_TEXEL_OFFSET)
				o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w * _ScreenParams.zw;
#else
				o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
#endif
				o.zw = pos.zw;
				return o;
			}

			v2f vert(VertexInput v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos = UnityObjectToClipPos(v.pos);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.refl = ComputeScreenPosIgnoreStereo(o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i)
				fixed4 tex = tex2D(_MainTex, i.uv);
				fixed4 refl;

				bool leftEye;

#ifdef UNITY_SINGLE_PASS_STEREO
				leftEye = unity_StereoEyeIndex == 0;
#else
				leftEye = (unity_CameraProjection[0][2] <= 0);
#endif
				if (leftEye)
				{
					refl = tex2Dproj(_LeftReflectionTex, UNITY_PROJ_COORD(i.refl));
				}
				else
				{
					refl = tex2Dproj(_RightReflectionTex, UNITY_PROJ_COORD(i.refl));
				}
				return tex * refl;
			}
			ENDCG
		}
	}
}