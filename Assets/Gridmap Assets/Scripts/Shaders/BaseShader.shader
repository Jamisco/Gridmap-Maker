  Shader "Example/BaseShader" 
  { 
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _UseColor("UseColor", Float) = 0
        _Color ("Color", Color) = (1,1,1,1)
        
    }
    
    SubShader 
    {
       Tags { "RenderType" = "Opaque"}
        
        Pass
        {

            HLSLPROGRAM   
            
            #include "UnityCG.cginc"
            
            #pragma vertex vert
            #pragma fragment frag
       
            float _UseColor;
            float4 _Color;

            sampler2D _MainTex;
            
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
                if(_UseColor == 1)
				{
					float4 final = i.color * _Color;
                    return final;
				}
                
                fixed4 col = tex2D(_MainTex, i.uv);

                return col;
            }   
            
            ENDHLSL
        }  
    }
}