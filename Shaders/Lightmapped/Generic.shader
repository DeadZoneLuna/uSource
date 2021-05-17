Shader "USource/Lightmapped/Generic" 
{
	//Support future:
	//Basic
	//$basetexture - Yes
	//$surfaceprop - Maybe?
	//$detail - 56% (4 / 7)
	//	-$detailtexturetransform Maybe?
	//	-$detailscale TODO
	//	-$detailtint TODO (It's supported but didn't use at the moment)
	//	-$detailframe TODO
	//  -$detail_alpha_mask_base_texture TODO
	//$model - Maybe?
	//Adjust
	//$basetexturetransform - Nope
	//$color - Yes
	//$seamless_scale - Nope
	//Transparency
	//$alpha - Nope
	//$alphatest - Yes
	//$distancealpha - Maybe?
	//$nocull - Yes
	//Lighting
	//$bumpmap - Nope
	//$ssbump - Nope
	//$selfillum - Nope
	//$lightwarptexture - Nope
	//Reflection
	//$reflectivity - Nope
	//$envmap - Nope
	Properties 
	{
		_Color ("Main Color (RGBA)", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_IsTranslucent("Is Translucent", Int) = 0
		_Cull("Cull State", Int) = 2
		_ZState("Z-Buffer State", Int) = 1
		_AlphaTest("Is AlphaTest", Int) = 0
		_DetailColor ("Detail Tint (RGBA)", Color) = (1,1,1,1)
		_Detail ("Base Detail (RGB)", 2D) = "white" {}
		_DetailFactor ("Factor of detail", Range(0,1)) = 0
		_DetailBlendMode ("Blend mode of detail", Int) = 0
	}

	//TODO:
	//Optimize shader passes!
	SubShader 
	{
		Tags 
		{ 
			"Queue" = "Geometry" 
			//"IgnoreProjector" = "True" 
			"RenderType" = "Transparent" 
		}

		//Lighting off

		ZWrite [_ZState]
		// in order not to occlude other objects
		Blend SrcAlpha OneMinusSrcAlpha

		Pass 
		{
			Tags { "LightMode" = "ForwardBase" }

			ZTest Less
			Cull [_Cull]

			CGPROGRAM
			// Must be a vert/frag shader, not a surface shader: the necessary variables
			// won't be defined yet for surface shaders.
			#pragma vertex vert
			#pragma fragment frag
			//#pragma multi_compile_fwdbase

			#include "UnityCG.cginc"
			//#include "Lighting.cginc"
			#include "SourceCG.cginc"

			// shadow helper functions and macros
			//#include "AutoLight.cginc"

			struct v2f 
			{
				float4 pos : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
			};

			struct appdata_lightmap 
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MainTex;
			sampler2D _Detail;
			float4 _Detail_ST;
			fixed4 _Color;
			fixed4 _DetailColor;
			float4 _MainTex_ST;
			half _DetailFactor;
			float _DetailBlendMode;
			float _IsTranslucent;
			float _AlphaTest;

			v2f vert(appdata_lightmap i) 
			{
				v2f o;
				o.pos = UnityObjectToClipPos(i.vertex);
				o.uv0 = TRANSFORM_TEX(i.texcoord, _MainTex);
				o.uv1 = i.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				o.uv2 = TRANSFORM_TEX(i.texcoord, _Detail);

				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				fixed4 baseColor = tex2D(_MainTex, i.uv0) * _Color;
				fixed4 detailColor = tex2D(_Detail, i.uv2) * _DetailColor;

				baseColor = TextureCombine(baseColor, detailColor, _DetailBlendMode, _DetailFactor);
				baseColor.rgb *= SourceDecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1));

				if (_IsTranslucent != 1 && _AlphaTest != 1)
					baseColor.a = _Color.a;

				if (_AlphaTest == 1 && _IsTranslucent != 1)
					clip(baseColor.a - 0.5);
				
				//if(_IsTranslucent == 1 && _AlphaTest != 1)
				//	clip(baseColor.a);

				return baseColor;
			}

			ENDCG
		}
	}

	Fallback "VertexLit"
}