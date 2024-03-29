HEADER
{
	Description = "Fog";
}

FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
	Default();
    VrForward();
}

COMMON
{
	#include "postprocess/shared.hlsl"
}

struct VS_INPUT
{
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
};

struct PS_INPUT
{
	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs		: SV_Position;
	#endif

	#if ( ( PROGRAM == VFX_PROGRAM_PS ) )
		float4 vPositionSs		: SV_ScreenPosition;
	#endif
};

VS
{
    PS_INPUT MainVs( VS_INPUT i )
    {
        PS_INPUT o;
        o.vPositionPs = float4(i.vPositionOs.xyz, 1.0f);

        return o;
    }
}

PS
{
    #include "postprocess/common.hlsl"

    struct PS_OUTPUT
    {
        float4 vColor : SV_Target0;
    };

	float4 fogColor < Attribute( "Color" ); Default4( 0.3, 0.55, 0.85, 1 ); >;
	float fogRadius < Attribute( "Radius" ); Default( 2500 ); >;
    
    CreateTexture2D( g_tColorBuffer ) < Attribute( "ColorBuffer" ); SrgbRead( true ); Filter( MIN_MAG_LINEAR_MIP_POINT ); AddressU( MIRROR ); AddressV( MIRROR ); >;
    CreateTexture2DMS( g_tDepthBuffer ) < Attribute( "DepthBuffer" ); SrgbRead( false ); Filter( MIN_MAG_MIP_POINT ); AddressU( CLAMP ); AddressV( CLAMP ); >;
    
	float fetchDepth( float2 coords )
	{
		float projectedDepth = 1.0f;

        float2 texelSize = TextureDimensions2D(g_tColorBuffer, 0);
        projectedDepth = Tex2DMS(g_tDepthBuffer, int2(coords.xy * texelSize), 0).r;
		projectedDepth = RemapValClamped(projectedDepth, g_flViewportMinZ, g_flViewportMaxZ, 0.0, 1.0);

		float depthRelativeToRay = 1.0 / ((projectedDepth * g_vInvProjRow3.z + g_vInvProjRow3.w));
		return depthRelativeToRay * 2.0f;
	}

    float4 MainPs( PS_INPUT i ) : SV_Target0
    { 		
        float3 col = Tex2D(g_tColorBuffer, i.vPositionSs.xy / g_vViewportSize.xy).rgb;
        float z = fetchDepth(i.vPositionSs.xy / g_vViewportSize.xy);
        float3 coords = float3((i.vPositionSs.xy / g_vViewportSize.xy * float2(g_vViewportSize.x / g_vViewportSize.y, 1.0f) - float2(g_vViewportSize.x / g_vViewportSize.y, 1.0f) / 2) * z, z);
        float dist = length(coords);

        return float4(lerp(col, fogColor.rgb, smoothstep(0.2, 1.5, dist / (fogRadius * 2))), 1);
    }
}