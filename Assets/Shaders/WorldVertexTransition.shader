Shader "Custom/WorldVertexTransition" 
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_BlendTex("Detail (RGB)", 2D) = "gray" {}
	}
	SubShader
	{

		Pass
		{
			Tags{ "LightMode" = "Vertex" }

			BindChannels
			{
				Bind "Vertex", vertex
				Bind "Color", color
				Bind "texcoord", texcoord0 // lightmap uses 2nd uv
				Bind "texcoord", texcoord1 // unused
			}

			Lighting Off
			Fog{ Mode Off }

			SetTexture[_MainTex]{
				combine texture
			}
		
			SetTexture[_BlendTex]
			{
					combine  texture lerp(primary) previous, previous
			}
		}

		Pass
		{
			Tags{ "LightMode" = "VertexLM" }

			BindChannels
			{
				Bind "Vertex", vertex
				Bind "Color", color
				Bind "texcoord", texcoord0 // lightmap uses 2nd uv
				Bind "texcoord", texcoord1 // main uses 1st uv
				Bind "texcoord1", texcoord2 // main uses 1st uv
			}

			ColorMaterial AmbientAndDiffuse
			Lighting Off
			Fog{ Mode Off }

			SetTexture[_MainTex]
			{
				combine texture
			}

			SetTexture[_BlendTex]
			{
				combine texture lerp(primary) previous, previous
			}

			SetTexture[unity_Lightmap]
			{
				matrix[unity_LightmapMatrix]
				combine previous * texture Double, previous
			}

			SetTexture[_]{
				constantColor[_Color]
				combine previous * constant, previous
			}
		}

		// Lightmapped, encoded as RGBM
		Pass{
			Tags{ "LightMode" = "VertexLMRGBM" }

			BindChannels
			{
				Bind "Vertex", vertex
				Bind "Color", color
				Bind "texcoord", texcoord0 // lightmap uses 2nd uv
				Bind "texcoord", texcoord1 // unused
				Bind "texcoord1", texcoord2 // main uses 1st uv
			}

			Lighting Off
			Fog{ Mode Off }
	
			SetTexture[_MainTex]
			{
				combine texture
			}
			SetTexture[_BlendTex]
			{
				combine  texture lerp(primary) previous, previous
			}

			SetTexture[unity_Lightmap]
			{
				matrix[unity_LightmapMatrix]
				combine previous * texture alpha QUAD, previous
			}

			SetTexture[_]
			{
				constantColor[_Color]
				combine previous * constant
			}
		}

	}
}