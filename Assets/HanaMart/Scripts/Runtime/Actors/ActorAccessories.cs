using UnityEngine;

namespace HanaMart.Actors
{
    public static class ActorAccessories
    {
        public static GameObject Hood(Transform head,Quaternion facing,float scale,Material source)
        {
            const int columns=24,rows=14;
            var vertices=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[vertices.Length];
            for(int row=0;row<=rows;row++)for(int col=0;col<=columns;col++)
            {
                float theta=row/(float)rows*Mathf.PI*.7f,phi=.7f+col/(float)columns*(Mathf.PI*2-1.4f);
                int index=row*(columns+1)+col;
                vertices[index]=new Vector3(Mathf.Sin(phi)*Mathf.Sin(theta),Mathf.Cos(theta)*1.08f,Mathf.Cos(phi)*Mathf.Sin(theta)*1.1f)*.15f;
                uv[index]=new Vector2(col/(float)columns,row/(float)rows);
            }
            // Mixamo's Head origin is above the procedural Person head pivot.
            var go=Create("Rain hood",head,facing,scale,new Vector3(0,.06f,0),vertices,uv,Triangles(columns,rows),source);
            var material=go.GetComponent<Renderer>().sharedMaterial;material.SetFloat("_Raincoat",1);return go;
        }
        public static GameObject Mask(Transform head,Quaternion facing,float scale,Material source,string kind)
        {
            const int columns=24;var vertices=new Vector3[(columns+1)*2];var uv=new Vector2[vertices.Length];
            for(int row=0;row<=1;row++)for(int col=0;col<=columns;col++)
            {
                float phi=-1.2f+col/(float)columns*2.4f,radius=row==0?.104f:.1f;int index=row*(columns+1)+col;
                vertices[index]=new Vector3(Mathf.Sin(phi)*radius,(.5f-row)*.09f,Mathf.Cos(phi)*radius*1.1f);
                uv[index]=new Vector2(col/(float)columns,1-row);
            }
            var go=Create("Face mask "+kind,head,facing,scale,new Vector3(0,0,.05f),vertices,uv,Triangles(columns,1),source);
            var material=go.GetComponent<Renderer>().sharedMaterial;material.SetFloat("_Raincoat",0);material.SetFloat("_TintStrength",0);
            material.SetColor("_BaseColor",kind=="black"?(Color)new Color32(0x16,0x17,0x1a,255):kind=="smile"?Color.white:new Color32(0xf0,0xf0,0xec,255));
            if(kind=="smile")
            {
                var image=new Texture2D(256,128,TextureFormat.RGBA32,false);
                for(int y=0;y<128;y++)for(int x=0;x<256;x++)
                {
                    float dx=x+.5f-128,dy=128-y-.5f-20,dist=Mathf.Sqrt(dx*dx+dy*dy),angle=Mathf.Atan2(dy,dx);
                    bool red=dist>=86.5f && dist<=93.5f && angle>=.35f && angle<=Mathf.PI-.35f;
                    for(int tooth=0;tooth<9;tooth++)if(Mathf.Abs(angle-(.45f+tooth*.25f))<.016f && dist>80 && dist<98)red=true;
                    image.SetPixel(x,y,red?new Color32(0x8a,0x0f,0x16,255):new Color32(0xf2,0xf2,0xee,255));
                }
                image.Apply();material.SetTexture("_BaseMap",image);go.GetComponent<ActorAccessoryResources>().image=image;
            }
            return go;
        }
        static int[] Triangles(int columns,int rows)
        {
            var triangles=new int[columns*rows*6];int at=0;
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {int a=row*(columns+1)+col,b=a+columns+1;triangles[at++]=a;triangles[at++]=b;triangles[at++]=a+1;triangles[at++]=a+1;triangles[at++]=b;triangles[at++]=b+1;}
            return triangles;
        }
        static GameObject Create(string name,Transform head,Quaternion facing,float scale,Vector3 offset,Vector3[] vertices,Vector2[] uv,int[] triangles,Material source)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(ActorAccessoryResources));
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var material=new Material(source){name=name+" material"};material.SetFloat("_ObjectUnit",1);material.SetFloat("_ActorScale",scale);material.SetFloat("_ShellDistance",0);material.SetFloat("_Faceless",0);material.SetFloat("_Pale",0);
            material.DisableKeyword("_NORMALMAP");material.DisableKeyword("_SPECGLOSSMAP");material.DisableKeyword("_METALLICSPECGLOSSMAP");material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            var region=new Texture2D(1,1,TextureFormat.RGBA32,false,true);region.SetPixel(0,0,Color.red);region.Apply();material.SetTexture("_RegionMask",region);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
            var owner=go.GetComponent<ActorAccessoryResources>();owner.mesh=mesh;owner.material=material;owner.region=region;
            go.transform.SetPositionAndRotation(head.position+facing*offset*scale,facing);go.transform.localScale=Vector3.one*scale;go.transform.SetParent(head,true);return go;
        }
    }
}
