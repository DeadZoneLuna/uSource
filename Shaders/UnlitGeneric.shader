Shader "USource/UnlitGeneric"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_Detail("Base Detail (RGB)", 2D) = "white" {}
		_DetailFactor("Factor of detail", Range(0,1)) = 0
		_DetailBlendMode("Blend mode of detail", Int) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "Lightmapped/SourceCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			sampler2D _MainTex;
			sampler2D _Detail;
			float4 _MainTex_ST;
			float4 _Detail_ST;
			half _DetailFactor;
			float _DetailBlendMode;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;
				fixed4 detailColor = tex2D(_Detail, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, baseColor);

				baseColor.rgb *= lerp(float3(1, 1, 1), 2.0 * detailColor.rgb, _DetailFactor);

				return baseColor;
			}
			ENDCG
		}
	}
}
