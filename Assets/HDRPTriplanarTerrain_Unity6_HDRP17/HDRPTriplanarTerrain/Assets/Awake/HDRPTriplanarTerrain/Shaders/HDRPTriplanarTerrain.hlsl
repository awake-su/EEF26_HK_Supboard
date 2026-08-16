#ifndef AWAKE_HDRP_TRIPLANAR_TERRAIN_INCLUDED
#define AWAKE_HDRP_TRIPLANAR_TERRAIN_INCLUDED

float3 AwakeSafePowWeights(float3 n, float sharpness)
{
    float3 w = pow(max(abs(n), 1e-4), max(sharpness, 1.0));
    return w / max(w.x + w.y + w.z, 1e-4);
}

float4 AwakeSampleTriplanar(UnityTexture2D map, float3 p, float3 n, float sharpness)
{
    float3 w = AwakeSafePowWeights(n, sharpness);
    float sx = n.x < 0.0 ? -1.0 : 1.0;
    float sy = n.y < 0.0 ? -1.0 : 1.0;
    float sz = n.z < 0.0 ? -1.0 : 1.0;
    float4 x = SAMPLE_TEXTURE2D(map.tex, map.samplerstate, p.zy * float2(sx, 1.0));
    float4 y = SAMPLE_TEXTURE2D(map.tex, map.samplerstate, p.xz * float2(sy, 1.0));
    float4 z = SAMPLE_TEXTURE2D(map.tex, map.samplerstate, p.xy * float2(-sz, 1.0));
    return x * w.x + y * w.y + z * w.z;
}

float3 AwakeUnpackNormal(float4 packedNormal, float strength)
{
    float3 n = UnpackNormal(packedNormal);
    n.xy *= strength;
    return normalize(float3(n.xy, max(1e-4, n.z)));
}

float3 AwakeSampleTriplanarNormal(
    UnityTexture2D map, float3 p, float3 geometricNormal, float sharpness, float strength)
{
    float3 w = AwakeSafePowWeights(geometricNormal, sharpness);
    float sx = geometricNormal.x < 0.0 ? -1.0 : 1.0;
    float sy = geometricNormal.y < 0.0 ? -1.0 : 1.0;
    float sz = geometricNormal.z < 0.0 ? -1.0 : 1.0;

    float3 nx = AwakeUnpackNormal(
        SAMPLE_TEXTURE2D(map.tex, map.samplerstate, p.zy * float2(sx, 1.0)), strength);
    float3 ny = AwakeUnpackNormal(
        SAMPLE_TEXTURE2D(map.tex, map.samplerstate, p.xz * float2(sy, 1.0)), strength);
    float3 nz = AwakeUnpackNormal(
        SAMPLE_TEXTURE2D(map.tex, map.samplerstate, p.xy * float2(-sz, 1.0)), strength);

    // Each sampled tangent normal is rotated into the projection's world basis.
    float3 wx = float3(nx.z * sx, nx.y, nx.x * sx);
    float3 wy = float3(ny.x, ny.z * sy, ny.y * sy);
    float3 wz = float3(-nz.x * sz, nz.y, nz.z * sz);
    return normalize(wx * w.x + wy * w.y + wz * w.z);
}

float AwakeSoftBand(float value, float center, float softness)
{
    float halfWidth = max(softness, 1e-4);
    return 1.0 - smoothstep(halfWidth, halfWidth * 2.0, abs(value - center));
}

void AwakeHDRPTriplanarTerrain_float(
    UnityTexture2D SandAlbedo,
    UnityTexture2D SandNormal,
    UnityTexture2D SandMask,
    UnityTexture2D GrassAlbedo,
    UnityTexture2D GrassNormal,
    UnityTexture2D GrassMask,
    UnityTexture2D RockAlbedo,
    UnityTexture2D RockNormal,
    UnityTexture2D RockMask,
    float3 PositionWS,
    float3 PositionOS,
    float3 NormalWS,
    float3 NormalOS,
    float UseObjectSpace,
    float SandTiling,
    float GrassTiling,
    float RockTiling,
    float TriplanarSharpness,
    float SandNormalStrength,
    float GrassNormalStrength,
    float RockNormalStrength,
    float SandHeight,
    float SandHeightBlend,
    float GrassHeight,
    float GrassHeightBlend,
    float RockSlopeStart,
    float RockSlopeBlend,
    out float3 BaseColor,
    out float3 NormalWorld,
    out float Metallic,
    out float Occlusion,
    out float Smoothness,
    out float3 LayerWeights)
{
    float objectSpace = saturate(UseObjectSpace);
    float3 n = normalize(lerp(NormalWS, NormalOS, objectSpace));
    float3 mappingPosition = lerp(PositionWS, PositionOS, objectSpace);
    float heightValue = mappingPosition.y;

    float sandHeightMask = 1.0 - smoothstep(
        SandHeight - max(SandHeightBlend, 1e-4),
        SandHeight + max(SandHeightBlend, 1e-4),
        heightValue);
    float grassHeightMask = AwakeSoftBand(heightValue, GrassHeight, GrassHeightBlend);

    // 0 on horizontal surfaces, 1 on vertical surfaces.
    float slope = 1.0 - saturate(abs(n.y));
    float rockSlopeMask = smoothstep(
        RockSlopeStart - max(RockSlopeBlend, 1e-4),
        RockSlopeStart + max(RockSlopeBlend, 1e-4),
        slope);

    float3 weights;
    weights.x = sandHeightMask * (1.0 - rockSlopeMask);
    weights.z = rockSlopeMask;
    weights.y = max(grassHeightMask * (1.0 - rockSlopeMask), 1.0 - weights.x - weights.z);
    weights = max(weights, 0.0);
    weights /= max(weights.x + weights.y + weights.z, 1e-4);
    LayerWeights = weights;

    float4 sandColor = AwakeSampleTriplanar(
        SandAlbedo, mappingPosition * max(SandTiling, 1e-4), n, TriplanarSharpness);
    float4 grassColor = AwakeSampleTriplanar(
        GrassAlbedo, mappingPosition * max(GrassTiling, 1e-4), n, TriplanarSharpness);
    float4 rockColor = AwakeSampleTriplanar(
        RockAlbedo, mappingPosition * max(RockTiling, 1e-4), n, TriplanarSharpness);

    float4 sandMask = AwakeSampleTriplanar(
        SandMask, mappingPosition * max(SandTiling, 1e-4), n, TriplanarSharpness);
    float4 grassMask = AwakeSampleTriplanar(
        GrassMask, mappingPosition * max(GrassTiling, 1e-4), n, TriplanarSharpness);
    float4 rockMask = AwakeSampleTriplanar(
        RockMask, mappingPosition * max(RockTiling, 1e-4), n, TriplanarSharpness);

    float3 sandN = AwakeSampleTriplanarNormal(
        SandNormal, mappingPosition * max(SandTiling, 1e-4), n, TriplanarSharpness, SandNormalStrength);
    float3 grassN = AwakeSampleTriplanarNormal(
        GrassNormal, mappingPosition * max(GrassTiling, 1e-4), n, TriplanarSharpness, GrassNormalStrength);
    float3 rockN = AwakeSampleTriplanarNormal(
        RockNormal, mappingPosition * max(RockTiling, 1e-4), n, TriplanarSharpness, RockNormalStrength);

    BaseColor = sandColor.rgb * weights.x + grassColor.rgb * weights.y + rockColor.rgb * weights.z;
    float3 mappedNormal = normalize(sandN * weights.x + grassN * weights.y + rockN * weights.z);
    NormalWorld = normalize(lerp(mappedNormal, TransformObjectToWorldNormal(mappedNormal), objectSpace));
    Metallic = saturate(sandMask.r * weights.x + grassMask.r * weights.y + rockMask.r * weights.z);
    Occlusion = saturate(sandMask.g * weights.x + grassMask.g * weights.y + rockMask.g * weights.z);
    Smoothness = saturate(sandMask.a * weights.x + grassMask.a * weights.y + rockMask.a * weights.z);
}

#endif
