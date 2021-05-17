// Decodes lightmaps:
// doubleLDR encoded on GLES
inline fixed3 SourceDecodeLightmap(fixed4 color)
{
#if defined(UNITY_COLORSPACE_GAMMA)
#if defined(SHADER_API_GLES) && defined(SHADER_API_MOBILE)
	return (1.75 * color.rgb;
#else
	return (1.75 * color.a) * color.rgb;
#endif
#else
#if defined(SHADER_API_GLES) && defined(SHADER_API_MOBILE)
	return 3.75 * color.rgb;
#else
	return (3.75 * color.a) * color.rgb;
#endif
#endif
}

// Needs to match NormalDecodeMode_t enum in imaterialsystem.h
#define NORM_DECODE_NONE			0
#define NORM_DECODE_ATI2N			1
#define NORM_DECODE_ATI2N_ALPHA		2

float4 DecompressNormal(sampler NormalSampler, float2 tc, int nDecompressionMode, sampler AlphaSampler)
{
	float4 normalTexel = tex2D(NormalSampler, tc);
	float4 result;

	if (nDecompressionMode == NORM_DECODE_NONE)
	{
		result = float4(normalTexel.xyz * 2.0f - 1.0f, normalTexel.a);
	}
	else if (nDecompressionMode == NORM_DECODE_ATI2N)
	{
		result.xy = normalTexel.xy * 2.0f - 1.0f;
		result.z = sqrt(1.0f - dot(result.xy, result.xy));
		result.a = 1.0f;
	}
	else // ATI2N plus ATI1N for alpha
	{
		result.xy = normalTexel.xy * 2.0f - 1.0f;
		result.z = sqrt(1.0f - dot(result.xy, result.xy));
		result.a = tex2D(AlphaSampler, tc).x;					// Note that this comes in on the X channel
	}

	return result;
}

float4 DecompressNormal(sampler NormalSampler, float2 tc, int nDecompressionMode)
{
	return DecompressNormal(NormalSampler, tc, nDecompressionMode, NormalSampler);
}

// texture combining modes for combining base and detail/basetexture2
#define TCOMBINE_RGB_EQUALS_BASE_x_DETAILx2 0				// original mode
#define TCOMBINE_RGB_ADDITIVE 1								// base.rgb+detail.rgb*fblend
#define TCOMBINE_DETAIL_OVER_BASE 2
#define TCOMBINE_FADE 3										// straight fade between base and detail.
#define TCOMBINE_BASE_OVER_DETAIL 4                         // use base alpha for blend over detail
#define TCOMBINE_RGB_ADDITIVE_SELFILLUM 5                   // add detail color post lighting
#define TCOMBINE_RGB_ADDITIVE_SELFILLUM_THRESHOLD_FADE 6
#define TCOMBINE_MOD2X_SELECT_TWO_PATTERNS 7				// use alpha channel of base to select between mod2x channels in r+a of detail
#define TCOMBINE_MULTIPLY 8
#define TCOMBINE_MASK_BASE_BY_DETAIL_ALPHA 9                // use alpha channel of detail to mask base
#define TCOMBINE_SSBUMP_BUMP 10								// use detail to modulate lighting as an ssbump
#define TCOMBINE_SSBUMP_NOBUMP 11					// detail is an ssbump but use it as an albedo. shader does the magic here - no user needs to specify mode 11

inline float4 TextureCombine(float4 baseColor, float4 detailColor, int combine_mode, float fBlendFactor)
{
	if (combine_mode == TCOMBINE_MOD2X_SELECT_TWO_PATTERNS)
	{
		float3 dc = lerp(detailColor.r, detailColor.a, baseColor.a);
		baseColor.rgb *= lerp(float3(1, 1, 1), 2.0 * dc, fBlendFactor);
	}
	if (combine_mode == TCOMBINE_RGB_EQUALS_BASE_x_DETAILx2)
		baseColor.rgb *= lerp(float3(1, 1, 1), 2.0 * detailColor.rgb, fBlendFactor);
	if (combine_mode == TCOMBINE_RGB_ADDITIVE)
		baseColor.rgb += fBlendFactor * detailColor.rgb;
	if (combine_mode == TCOMBINE_DETAIL_OVER_BASE)
	{
		float fblend = fBlendFactor * detailColor.a;
		baseColor.rgb = lerp(baseColor.rgb, detailColor.rgb, fblend);
	}
	if (combine_mode == TCOMBINE_FADE)
	{
		baseColor = lerp(baseColor, detailColor, fBlendFactor);
	}
	if (combine_mode == TCOMBINE_BASE_OVER_DETAIL)
	{
		float fblend = fBlendFactor * (1 - baseColor.a);
		baseColor.rgb = lerp(baseColor.rgb, detailColor.rgb, fblend);
		baseColor.a = detailColor.a;
	}
	if (combine_mode == TCOMBINE_MULTIPLY)
	{
		baseColor = lerp(baseColor, baseColor * detailColor, fBlendFactor);
	}

	if (combine_mode == TCOMBINE_MASK_BASE_BY_DETAIL_ALPHA)
	{
		baseColor.a = lerp(baseColor.a, baseColor.a * detailColor.a, fBlendFactor);
	}
	if (combine_mode == TCOMBINE_SSBUMP_NOBUMP)
	{
		baseColor.rgb = baseColor.rgb * dot(detailColor.rgb, 2.0 / 3.0);
	}
	return baseColor;
}