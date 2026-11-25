using UnityEngine;

namespace HanaMart.Actors
{
    public static class ActorAccessories
    {
        public static GameObject Hood(Transform head, Quaternion facing, float scale, Material source)
        {
            const int columns = 24, rows = 18;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];

            for (int row = 0; row <= rows; row++)
            {
                float t = row / (float)rows;
                float theta = t * Mathf.PI * 0.88f;
                float drape = Mathf.SmoothStep(0.62f, 1.0f, t);
                float rx = Mathf.Lerp(0.106f, 0.122f, drape);
                float ry = 0.120f;
                float rz = Mathf.Lerp(0.116f, 0.122f, drape);
                float dropY = -0.032f * drape;

                for (int col = 0; col <= columns; col++)
                {
                    float phi = 0.85f + col / (float)columns * (Mathf.PI * 2f - 1.70f);
                    int index = row * (columns + 1) + col;
                    vertices[index] = new Vector3(
                        Mathf.Sin(phi) * Mathf.Sin(theta) * rx,
                        Mathf.Cos(theta) * ry + dropY,
                        Mathf.Cos(phi) * Mathf.Sin(theta) * rz);
                    uv[index] = new Vector2(col / (float)columns, t);
                }
            }

            // Mixamo's Head origin is above the procedural Person head pivot.
            var go = Create("Rain hood", head, facing, scale, new Vector3(0f, 0.078f, 0.004f),
                vertices, uv, Triangles(columns, rows), source, Color.red);
            var material = go.GetComponent<Renderer>().sharedMaterial;
            bool isRaincoat = source && source.HasProperty("_Raincoat") && source.GetFloat("_Raincoat") > 0.5f;
            material.SetFloat("_Raincoat", isRaincoat ? 1f : 0f);
            return go;
        }

        public static GameObject Mask(Transform head, Quaternion facing, float scale, Material source, string kind)
        {
            const int columns = 24, rows = 8;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];

            for (int row = 0; row <= rows; row++)
            {
                float v = row / (float)rows;
                float widthScale = Mathf.Lerp(0.92f, 0.78f, v * v);
                float rx = 0.080f * widthScale;

                for (int col = 0; col <= columns; col++)
                {
                    float u = col / (float)columns;
                    float phi = -1.15f + u * 2.30f;
                    float centerBell = Mathf.Exp(-phi * phi * 4f);
                    float noseArch = Mathf.Exp(-phi * phi * 5f);

                    float rz = 0.088f
                             + 0.014f * centerBell * Mathf.Sin(v * Mathf.PI * 0.85f + 0.25f)
                             - 0.018f * Mathf.Pow(v, 2.0f);
                    float y = (0.5f - v) * 0.072f + 0.008f * noseArch * (1f - v);

                    int index = row * (columns + 1) + col;
                    vertices[index] = new Vector3(Mathf.Sin(phi) * rx, y, Mathf.Cos(phi) * rz);
                    uv[index] = new Vector2(u, 1f - v);
                }
            }

            var go = Create("Face mask " + kind, head, facing, scale, new Vector3(0f, 0.020f, 0.042f),
                vertices, uv, Triangles(columns, rows), source, Color.clear);
            var material = go.GetComponent<Renderer>().sharedMaterial;
            material.SetFloat("_Raincoat", 0);
            material.SetFloat("_TintStrength", 0);
            material.SetColor("_BaseColor", kind == "black"
                ? (Color)new Color32(0x16, 0x17, 0x1a, 255)
                : kind == "smile"
                    ? Color.white
                    : new Color32(0xf0, 0xf0, 0xec, 255));

            if (kind == "smile")
            {
                var image = new Texture2D(256, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float dx = x + 0.5f - 128f, dy = 128f - y - 0.5f - 20f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy), angle = Mathf.Atan2(dy, dx);
                        bool red = dist >= 86.5f && dist <= 93.5f && angle >= 0.35f && angle <= Mathf.PI - 0.35f;
                        for (int tooth = 0; tooth < 9; tooth++)
                            if (Mathf.Abs(angle - (0.45f + tooth * 0.25f)) < 0.016f && dist > 80f && dist < 98f)
                                red = true;
                        image.SetPixel(x, y, red ? new Color32(0x8a, 0x0f, 0x16, 255) : new Color32(0xf2, 0xf2, 0xee, 255));
                    }
                }
                image.Apply();
                material.SetTexture("_BaseMap", image);
                go.GetComponent<ActorAccessoryResources>().image = image;
            }
            return go;
        }

        static int[] Triangles(int columns, int rows)
        {
            var triangles = new int[columns * rows * 6];
            int at = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int a = row * (columns + 1) + col, b = a + columns + 1;
                    triangles[at++] = a;
                    triangles[at++] = b;
                    triangles[at++] = a + 1;
                    triangles[at++] = a + 1;
                    triangles[at++] = b;
                    triangles[at++] = b + 1;
                }
            }
            return triangles;
        }

        static GameObject Create(string name, Transform head, Quaternion facing, float scale, Vector3 offset,
            Vector3[] vertices, Vector2[] uv, int[] triangles, Material source, Color regionColor)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(ActorAccessoryResources));
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            var material = new Material(source) { name = name + " material" };
            material.SetFloat("_ObjectUnit", 1);
            material.SetFloat("_ActorScale", scale);
            material.SetFloat("_ShellDistance", 0);
            material.SetFloat("_Faceless", 0);
            material.SetFloat("_Pale", 0);
            material.SetFloat("_Cull", 0);
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_SPECGLOSSMAP");
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);

            var region = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            region.SetPixel(0, 0, regionColor);
            region.Apply();
            material.SetTexture("_RegionMask", region);

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var owner = go.GetComponent<ActorAccessoryResources>();
            owner.mesh = mesh;
            owner.material = material;
            owner.region = region;

            go.transform.SetPositionAndRotation(head.position + facing * offset * scale, facing);
            go.transform.localScale = Vector3.one * scale;
            go.transform.SetParent(head, true);
            return go;
        }
    }
}
