//
// For build instructions see file ".\Compiling Shaders.txt"
//

float loThreshold : register(C0);
float hiThreshold : register(C1);
float palletteIndex: register(C2);
float invertPallette: register(C3);
float flipImage: register(C4);
float shaderIndex: register(C5);
float2 pixelOffset: register(C6);

sampler2D implicitInput : register(S0);
sampler2D PalletteTexture : register(S1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    uv.x = ( flipImage > 0.5f ) ? 1.0f - uv.x : uv.x ; 
    float4 color = tex2D ( implicitInput, uv ) ;

    if ( color.r < ( 1.0f / 512.0f ) ) 
    {
        color.a = 0.0f ; 
        return color ; 
    } 

    if ( shaderIndex > 0 ) 
    { 
        float pixelOffsetX = pixelOffset.x; 
        float pixelOffsetY = pixelOffset.y; 

        float2 uvTop = uv ;
        uvTop.y -= pixelOffsetY ; 
        float4 top = tex2D ( implicitInput, uvTop ) ;
    
        float2 uvLeft = uv ;
        uvLeft.x -= pixelOffsetX ;  
        uvLeft.y += pixelOffsetY ; 
        float4 left = tex2D ( implicitInput, uvLeft ) ;
    
        float2 uvRight = uv ;
        uvRight.x += pixelOffsetX ;  
        uvRight.y += pixelOffsetY ; 
        float4 right = tex2D ( implicitInput, uvRight ) ;

        if ( shaderIndex > 2.5f ) 
        { 
            // Edges
            color.r -= right.r ; 
            color.r -= left.r  ; 
            color.r += color.b ; 
            color.r += color.b ; 
        } 
        else if ( shaderIndex > 1.5f ) 
        { 
            // Sharpen 
            color.r = 8.0f * ( color.r / 5.0f ) ; 
            color.r -= top.r / 5.0f ; 
            color.r -= right.r / 5.0f ; 
            color.r -= left.r / 5.0f ; 
        } 
        else 
        {
            // Smooth 
            color.r = 7.0f * ( color.r / 10.0f ) ; 
            color.r += top.r / 10.0f ; 
            color.r += right.r / 10.0f ; 
            color.r += left.r / 10.0f ; 
        } 
    } 

    color.r = 
        color.r <= loThreshold ? 
            0.0f : ( color.r - loThreshold ) / ( hiThreshold - loThreshold );

    float2 palletteUV ; 
    palletteUV.x = ( invertPallette > 0.5f )  ? ( 1.0f - color.r ) : color.r ;  
    palletteUV.y = palletteIndex ;  

    color = tex2D ( PalletteTexture, palletteUV ) ;
    color.a = 1.0;
    return color;
}

