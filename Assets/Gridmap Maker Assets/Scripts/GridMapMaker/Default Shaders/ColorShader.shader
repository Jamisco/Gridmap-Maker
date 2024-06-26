// A simple shader that will set the mesh to the color of the color property

  Shader "GridMapMaker/ColorShader" 
  { 
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        
    }
    
    SubShader 
    {
       Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {

            HLSLPROGRAM   
            
            #include "UnityCG.cginc"
            
            #pragma vertex vert
            #pragma fragment frag
       
            float4 _Color;
            
            struct appdata {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float y : float;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                
                o.y = mul(unity_ObjectToWorld, v.vertex).y;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target 
            {                     
                return i.color;
            }   
            
            ENDHLSL
        }  
    }
}