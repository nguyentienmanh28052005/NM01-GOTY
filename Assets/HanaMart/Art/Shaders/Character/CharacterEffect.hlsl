#ifndef HANAMART_CHARACTER_EFFECT_INCLUDED
#define HANAMART_CHARACTER_EFFECT_INCLUDED
TEXTURE2D(_RegionMask); SAMPLER(sampler_RegionMask);

float CharacterHash(float3 p) { return frac(sin(dot(p,float3(12.9898,78.233,37.719)))*43758.5453); }
float CharacterNoise(float3 p)
{
    float3 i=floor(p),f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(lerp(CharacterHash(i),CharacterHash(i+float3(1,0,0)),f.x),lerp(CharacterHash(i+float3(0,1,0)),CharacterHash(i+float3(1,1,0)),f.x),f.y),
                lerp(lerp(CharacterHash(i+float3(0,0,1)),CharacterHash(i+float3(1,0,1)),f.x),lerp(CharacterHash(i+float3(0,1,1)),CharacterHash(i+float3(1,1,1)),f.x),f.y),f.z);
}
float CharacterFbm(float3 p) { return CharacterNoise(p)*.55+CharacterNoise(p*2.1)*.28+CharacterNoise(p*4.3)*.17; }
float3 CharacterDeform(float3 positionOS,float3 normalOS)
{
    float3 rp=positionOS*_ObjectUnit;
    float jn=CharacterHash(floor(rp*26)+floor(_EffectTime*14));
    float jitter=(jn-.5)*.045*_Reveal*step(.55,CharacterHash(float3(1,1,1)*floor(_EffectTime*9)));
    jitter+=(CharacterFbm(rp*9+_EffectTime*.6)-.5)*.03*_Reveal;
    float3 offsetWS=TransformObjectToWorldNormal(normalOS)*(jitter+_ShellDistance)*_ActorScale;
    return positionOS+mul((float3x3)GetWorldToObjectMatrix(),offsetWS);
}
void CharacterClip(float2 uv)
{
    float4 region=SAMPLE_TEXTURE2D(_RegionMask,sampler_RegionMask,uv);
    if(_Raincoat>.5)clip(region.r-.4);
}
void CharacterSurface(float2 uv,float3 position,inout SurfaceData surface)
{
    half4 region=SAMPLE_TEXTURE2D(_RegionMask,sampler_RegionMask,uv);
    CharacterClip(uv);
    half luminance=dot(surface.albedo,half3(.2126,.7152,.0722));
    surface.albedo=lerp(surface.albedo,luminance*_CoatColor.rgb*2,region.r*_TintStrength);
    surface.albedo=lerp(surface.albedo,luminance*_PantsColor.rgb*2,region.g*_TintStrength);
    surface.albedo=lerp(surface.albedo,surface.albedo*half3(.62,.66,.66)*1.2,region.b*_Pale);
    surface.albedo=lerp(surface.albedo,_SkinColor.rgb,region.a*_Faceless);
    if(_Raincoat>.5)
    {
        surface.albedo=_CoatColor.rgb;surface.smoothness=.72;
        surface.normalTS=lerp(surface.normalTS,half3(0,0,1),.75);
        surface.clearCoatMask=.8;surface.clearCoatSmoothness=.8;
    }
    float vein=CharacterFbm(position*22+float3(0,_EffectTime*.05,0));
    float vm=smoothstep(.46,.5,vein)*(1-smoothstep(.5,.56,vein));
    surface.albedo=lerp(surface.albedo,surface.albedo*half3(.62,.66,.66)*1.2,region.b*_Reveal);
    surface.albedo=lerp(surface.albedo,half3(.05,0,.01),saturate(vm*1.6+smoothstep(.62,.75,vein)*.8)*_Reveal);
}
#endif
