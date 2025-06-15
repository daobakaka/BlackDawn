//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_CUSTOM_FUNCTION_TEXT
#define MYHLSLINCLUDE_CUSTOM_FUNCTION_TEXT
//#include "Assets/InnerSources/OutTextures/textAndShderAndMaterial/CustomFunctionText.hlsl"
void AddSelf_float(float a, float b, out float NEWc)
{
    NEWc = a + b;
}
//«·«…–¥∑®£¨±‹√‚≈–∂œ
void GetCharRect_float(
    float Index,
    float4 UV1, float4 UV2, float4 UV3,
    float4 UV4, float4 UV5, float4 UV6,
    out float4 Result)
{
    Result = UV1;
    Result = lerp(Result, UV2, step(0.5, Index));
    Result = lerp(Result, UV3, step(1.5, Index));
    Result = lerp(Result, UV4, step(2.5, Index));
    Result = lerp(Result, UV5, step(3.5, Index));
    Result = lerp(Result, UV6, step(4.5, Index));
}

#endif 


