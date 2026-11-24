"""Fork only used URP 17.3 Lit passes; retain installed lighting and map contracts."""
from pathlib import Path
import re

root=Path(__file__).resolve().parents[1]
packages=list((root/'Library/PackageCache').glob('com.unity.render-pipelines.universal@*'))
if len(packages)!=1: raise RuntimeError('Expected one URP package')
source=packages[0]/'Shaders'
output=root/'Assets/HanaMart/Art/Shaders/Character'
output.mkdir(parents=True,exist_ok=True)
original=(source/'Lit.shader').read_text()
properties=original[original.index('    Properties'):original.index('    SubShader')]
properties=properties[:properties.rfind('}')]+'''
        _RegionMask("Region mask (data)", 2D) = "black" {}
        _CoatColor("Coat", Color) = (1,1,1,1)
        _PantsColor("Pants", Color) = (1,1,1,1)
        _SkinColor("Skin", Color) = (0.85,0.83,0.8,1)
        _TintStrength("Clothing tint", Range(0,1)) = 0
        _Pale("Pale", Range(0,1)) = 0
        _Faceless("Faceless", Range(0,1)) = 0
        _Reveal("Reveal", Range(0,1)) = 0
        _EffectTime("Effect time (seconds)", Float) = 0
        _ObjectUnit("Object units to metres", Float) = 1
        _ActorScale("Actor scale", Float) = 1
        _Raincoat("Rain shell", Float) = 0
        _ShellDistance("Shell distance (metres)", Float) = 0
    }
'''
passes=[]
for match in re.finditer(r'\bPass\s*\{',original):
    start=match.start();depth=1;end=match.end()
    while depth and end<len(original):
        depth += (original[end]=='{')-(original[end]=='}');end+=1
    text=original[start:end]
    name=re.search(r'Name\s+"([^"]+)"',text)
    if name and name.group(1) in ['ForwardLit','ShadowCaster','DepthOnly','DepthNormals']:
        text=text.replace('#pragma target 2.0','#pragma target 3.5')
        text=text.replace('HLSLPROGRAM','HLSLPROGRAM\n            #pragma shader_feature_local_fragment _CLEARCOAT')
        for a,b in [('LitInput.hlsl','CharacterInput.hlsl'),('LitForwardPass.hlsl','CharacterForwardPass.hlsl'),('ShadowCasterPass.hlsl','CharacterShadowCasterPass.hlsl'),('DepthOnlyPass.hlsl','CharacterDepthOnlyPass.hlsl'),('LitDepthNormalsPass.hlsl','CharacterDepthNormalsPass.hlsl')]:
            text=text.replace('Packages/com.unity.render-pipelines.universal/Shaders/'+a,b)
        passes.append(text)
(output/'Character.shader').write_text('// Based on installed URP 17.3 Lit shader; forward renderer only.\nShader "HanaMart/Character"\n{\n'+properties+'\nSubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "UniversalMaterialType"="Lit" }\n'+'\n'.join(passes)+'\n}\nFallback "Hidden/Universal Render Pipeline/FallbackError"\n}\n')
text=(source/'LitInput.hlsl').read_text()
text=text.replace('CBUFFER_END','''half4 _CoatColor, _PantsColor, _SkinColor;
float _TintStrength, _Pale, _Faceless, _Reveal, _EffectTime, _ObjectUnit, _ActorScale, _Raincoat, _ShellDistance;
CBUFFER_END''',1)
text += '\n#include "CharacterEffect.hlsl"\n'
(output/'CharacterInput.hlsl').write_text(text)

text=(source/'LitForwardPass.hlsl').read_text()
text=text.replace('float4 positionCS               : SV_POSITION;','float3 characterPositionOS       : TEXCOORD11;\n    float4 positionCS               : SV_POSITION;')
text=text.replace('VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);','output.characterPositionOS = input.positionOS.xyz * _ObjectUnit;\n    input.positionOS.xyz = CharacterDeform(input.positionOS.xyz, input.normalOS);\n    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);')
text=text.replace('InitializeStandardLitSurfaceData(input.uv, surfaceData);','InitializeStandardLitSurfaceData(input.uv, surfaceData);\n    CharacterSurface(input.uv, input.characterPositionOS, surfaceData);')
(output/'CharacterForwardPass.hlsl').write_text(text)

for file,out in [('ShadowCasterPass.hlsl','CharacterShadowCasterPass.hlsl'),('DepthOnlyPass.hlsl','CharacterDepthOnlyPass.hlsl'),('LitDepthNormalsPass.hlsl','CharacterDepthNormalsPass.hlsl')]:
    text=(source/file).read_text()
    text=re.sub(r'#if defined\(_ALPHATEST_ON\)\s*(float2 uv\s*: TEXCOORD0;)\s*#endif',r'\1',text)
    text=re.sub(r'#if defined\(_ALPHATEST_ON\)\s*(output.uv = TRANSFORM_TEX\(input.texcoord, _BaseMap\);)\s*#endif',r'\1',text)
    if file=='DepthOnlyPass.hlsl':
        text=text.replace('float4 position     : POSITION;','float4 position     : POSITION;\n    float3 normalOS : NORMAL;')
        text=text.replace('output.positionCS = TransformObjectToHClip(input.position.xyz);','input.position.xyz = CharacterDeform(input.position.xyz, input.normalOS);\n    output.positionCS = TransformObjectToHClip(input.position.xyz);')
    elif file=='ShadowCasterPass.hlsl':
        text=text.replace('output.positionCS = GetShadowPositionHClip(input);','input.positionOS.xyz = CharacterDeform(input.positionOS.xyz, input.normalOS);\n    output.positionCS = GetShadowPositionHClip(input);')
    else:
        text=text.replace('struct Attributes','\n#ifndef REQUIRES_UV_INTERPOLATOR\n#define REQUIRES_UV_INTERPOLATOR\n#endif\n\nstruct Attributes',1)
        text=text.replace('output.positionCS = TransformObjectToHClip(input.positionOS.xyz);','input.positionOS.xyz = CharacterDeform(input.positionOS.xyz, input.normal);\n    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);')
    # Clip the shell from bare skin in every depth/shadow path, independent of alpha-test variants.
    fragment=text.index('Fragment(')
    opening=text.index('{',fragment)
    text=text[:opening+1]+'\n    CharacterClip(input.uv);'+text[opening+1:]
    (output/out).write_text(text)
print('Generated character shader from '+packages[0].name)
