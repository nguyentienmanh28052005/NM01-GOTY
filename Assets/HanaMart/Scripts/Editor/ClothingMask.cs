using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace HanaMart.EditorTools
{
    public static class ClothingMask
    {
        // R coat, G pants, B skin, A head/face. Input texture samples are sRGB.
        public static Color Region(string bone, Color sample,string meshName=null)
        {
            bone=bone.ToLowerInvariant();
            string mesh=(meshName??"").ToLowerInvariant();
            if(mesh.Contains("body"))return new Color(0,0,1,bone.Contains("head")?1:0);
            if(mesh.Contains("pants") || mesh.Contains("shorts"))return new Color(0,1,0,0);
            if(mesh.Contains("shirt") || mesh.Contains("sweater") || mesh.Contains("hoodie") || mesh.Contains("collar"))return new Color(1,0,0,0);
            if(mesh.Contains("shoe") || mesh.Contains("sneaker") || mesh.Contains("hair"))return Color.clear;
            if(bone.Contains("head"))return new Color(0,0,1,1);
            if(bone.Contains("neck") || bone.Contains("hand") || bone.Contains("finger"))return new Color(0,0,1,0);
            if(bone.Contains("foot") || bone.Contains("toe"))return Color.clear;
            if(bone.Contains("leg"))return new Color(0,1,0,0);
            Color.RGBToHSV(sample,out float hue,out float saturation,out float value);
            bool skin=sample.r>sample.g+.025f && sample.g>sample.b+.005f && hue<.14f && saturation>.12f && saturation<.65f && value>.18f;
            return skin ? new Color(0,0,1,0) : new Color(1,0,0,0);
        }

        public static Texture2D Bake(SkinnedMeshRenderer renderer,int materialIndex,Texture source,string outputPath,int size=1024)
        {
            var mesh=renderer.sharedMesh;
            if(!mesh.isReadable)throw new System.InvalidOperationException("Mask bake requires readable mesh: "+mesh.name);
            var uv=mesh.uv;var weights=mesh.boneWeights;var bones=renderer.bones;
            var triangles=mesh.GetTriangles(materialIndex);var pixels=new Color32[size*size];
            var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            ImageConversion.LoadImage(image,File.ReadAllBytes(AssetDatabase.GetAssetPath(source)));
            for(int t=0;t<triangles.Length;t+=3)
            {
                int ia=triangles[t],ib=triangles[t+1],ic=triangles[t+2];
                var a=uv[ia]*size;var b=uv[ib]*size;var c=uv[ic]*size;
                float area=(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);if(Mathf.Abs(area)<.00001f)continue;
                string bone=weights.Length>ia ? bones[Dominant(weights[ia])].name : "Spine";
                var sample=(image.GetPixelBilinear(uv[ia].x,uv[ia].y)+image.GetPixelBilinear(uv[ib].x,uv[ib].y)+image.GetPixelBilinear(uv[ic].x,uv[ic].y)+image.GetPixelBilinear((uv[ia].x+uv[ib].x+uv[ic].x)/3,(uv[ia].y+uv[ib].y+uv[ic].y)/3))/4;
                Color32 region=Region(bone,sample,renderer.name);
                int x0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))),0,size-1),x1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))),0,size-1);
                int y0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))),0,size-1),y1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))),0,size-1);
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    float px=x+.5f,py=y+.5f;
                    float wb=((px-a.x)*(c.y-a.y)-(py-a.y)*(c.x-a.x))/area;
                    float wc=((b.x-a.x)*(py-a.y)-(b.y-a.y)*(px-a.x))/area;
                    if(wb>=-.005f && wc>=-.005f && wb+wc<=1.005f)pixels[y*size+x]=region;
                }
            }
            // Pad two texels around UV islands; fixed passes prevent runaway fills.
            for(int pass=0;pass<2;pass++)
            {
                var next=(Color32[])pixels.Clone();
                var offsets=new[]{-1,1,-size,size};
                for(int y=1;y<size-1;y++)for(int x=1;x<size-1;x++)
                {
                    int i=y*size+x;if(pixels[i].r+pixels[i].g+pixels[i].b>0)continue;
                    foreach(int offset in offsets){int n=i+offset;if(pixels[n].r+pixels[n].g+pixels[n].b>0){next[i]=pixels[n];break;}}
                }
                pixels=next;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true);texture.SetPixels32(pixels);texture.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));File.WriteAllBytes(outputPath,texture.EncodeToPNG());
            Object.DestroyImmediate(texture);Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(outputPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(outputPath);importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }
        static int Dominant(BoneWeight w)
        {
            float best=w.weight0;int index=w.boneIndex0;
            if(w.weight1>best){best=w.weight1;index=w.boneIndex1;}if(w.weight2>best){best=w.weight2;index=w.boneIndex2;}if(w.weight3>best)index=w.boneIndex3;
            return index;
        }
    }
}
