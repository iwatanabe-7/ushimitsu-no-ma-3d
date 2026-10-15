using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ushimitsu.EditorTools
{
    // Geometry and surface detail for SceneBuilder: real-size meshes with UVs in
    // metres plus lightmap UVs, and the generated textures from
    // Tools/generate_textures.py.
    public static partial class SceneBuilder
    {
        const string MeshAssetPath = "Assets/Generated/SceneMeshes.asset";
        const string TextureDir = "Assets/Textures";
        const string ScreenGradeShaderPath = "Assets/Shaders/ScreenGrade.shader";

        // Mean albedo brightness each generated texture is normalised to (mirrors
        // ALBEDO_MEAN in Tools/generate_textures.py). Dividing a material colour by
        // this gives a tint that keeps the material's average tone unchanged.
        static readonly Dictionary<string, float> TextureMean = new Dictionary<string, float>
        {
            { "Plaster", 0.5f },
            { "Wood", 0.5f },
            { "Tatami", 0.5f },
            { "TatamiEdge", 0.5f },
            { "Paper", 0.75f },
            { "Stone", 0.5f },
        };

        // Room roots whose whole hierarchy is static scenery for the lightmap bake.
        static readonly List<GameObject> environmentRoots = new List<GameObject>();

        static Mesh meshAssetRoot;
        static readonly Dictionary<string, Mesh> cylinderCache = new Dictionary<string, Mesh>();

        // ---------------- Mesh asset ----------------

        // Generated meshes go into one sub-asset file rather than being embedded in
        // Main.unity, which keeps the scene file small and readable in diffs.
        static void BeginMeshAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            {
                AssetDatabase.CreateFolder("Assets", "Generated");
            }
            AssetDatabase.DeleteAsset(MeshAssetPath);
            cylinderCache.Clear();

            meshAssetRoot = new Mesh { name = "SceneMeshes" };
            AssetDatabase.CreateAsset(meshAssetRoot, MeshAssetPath);
        }

        static void EndMeshAsset()
        {
            AssetDatabase.SaveAssets();
        }

        static Mesh FinishMesh(Mesh mesh)
        {
            mesh.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(mesh);
            mesh.RecalculateTangents();
            AssetDatabase.AddObjectToAsset(mesh, meshAssetRoot);
            return mesh;
        }

        // ---------------- Box ----------------

        static readonly Vector3[] FaceNormals =
        {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back,
        };

        static Mesh BoxMesh(Vector3 size, Vector3 seed)
        {
            var vertices = new List<Vector3>(24);
            var normals = new List<Vector3>(24);
            var uvs = new List<Vector2>(24);
            var triangles = new List<int>(36);

            // A per-box offset so repeated pieces (tatami, planks, slats) don't all
            // show the identical patch of texture.
            Vector2 offset = UvOffset(seed);
            Vector3 half = size * 0.5f;

            foreach (Vector3 n in FaceNormals)
            {
                Vector3 axisA = Mathf.Abs(n.x) > 0f ? Vector3.forward : Vector3.right;
                Vector3 axisB = Mathf.Abs(n.y) > 0f ? Vector3.forward : Vector3.up;
                float lenA = Vector3.Scale(size, axisA).magnitude;
                float lenB = Vector3.Scale(size, axisB).magnitude;

                // u always runs along the face's longer side, so wood grain follows
                // the length of a beam or board and tatami rush runs across the mat.
                Vector3 uAxis = lenA >= lenB ? axisA : axisB;
                Vector3 vAxis = lenA >= lenB ? axisB : axisA;
                float uLen = Mathf.Max(lenA, lenB);
                float vLen = Mathf.Min(lenA, lenB);

                Vector3 faceCenter = Vector3.Scale(n, half);
                int start = vertices.Count;
                for (int i = 0; i < 4; i++)
                {
                    float su = (i == 0 || i == 1) ? -1f : 1f;
                    float sv = (i == 0 || i == 3) ? -1f : 1f;
                    vertices.Add(faceCenter + uAxis * (su * uLen * 0.5f) + vAxis * (sv * vLen * 0.5f));
                    normals.Add(n);
                    uvs.Add(new Vector2((su + 1f) * 0.5f * uLen, (sv + 1f) * 0.5f * vLen) + offset);
                }

                // Unity treats clockwise-from-the-front as front-facing; flip the
                // quad if this axis pairing came out wound the other way.
                Vector3 faceDir = Vector3.Cross(vertices[start + 1] - vertices[start], vertices[start + 2] - vertices[start]);
                if (Vector3.Dot(faceDir, n) > 0f)
                {
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                }
                else
                {
                    triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                }
            }

            var mesh = new Mesh { name = "Box_" + size.x.ToString("0.###") + "x" + size.y.ToString("0.###") + "x" + size.z.ToString("0.###") };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            return FinishMesh(mesh);
        }

        static Vector2 UvOffset(Vector3 seed)
        {
            int h = Mathf.RoundToInt(seed.x * 1000f) * 73856093
                ^ Mathf.RoundToInt(seed.y * 1000f) * 19349663
                ^ Mathf.RoundToInt(seed.z * 1000f) * 83492791;
            var rng = new System.Random(h);
            return new Vector2((float)rng.NextDouble() * 8f, (float)rng.NextDouble() * 8f);
        }

        // ---------------- Cylinder ----------------

        // Y-up cylinder of real size, centred on the origin. u wraps the
        // circumference and v runs along the height, both in metres, so a pillar's
        // grain runs up its length.
        static Mesh CylinderMesh(float radius, float height)
        {
            string key = radius.ToString("0.####") + "_" + height.ToString("0.####");
            if (cylinderCache.TryGetValue(key, out Mesh cached)) return cached;

            const int Segments = 16;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float h = height * 0.5f;
            float circumference = 2f * Mathf.PI * radius;

            for (int i = 0; i <= Segments; i++)
            {
                float a = 2f * Mathf.PI * i / Segments;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float u = circumference * i / Segments;
                vertices.Add(dir * radius + Vector3.down * h);
                vertices.Add(dir * radius + Vector3.up * h);
                normals.Add(dir);
                normals.Add(dir);
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, height));
            }
            for (int i = 0; i < Segments; i++)
            {
                int b = i * 2;
                // Viewed from outside, (bottom_i, top_i, top_i+1) runs clockwise.
                triangles.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 });
            }

            AddCap(vertices, normals, uvs, triangles, radius, h, Segments, true);
            AddCap(vertices, normals, uvs, triangles, radius, -h, Segments, false);

            var mesh = new Mesh { name = "Cylinder_" + key };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            FinishMesh(mesh);
            cylinderCache[key] = mesh;
            return mesh;
        }

        static void AddCap(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles,
            float radius, float y, int segments, bool top)
        {
            Vector3 n = top ? Vector3.up : Vector3.down;
            int center = vertices.Count;
            vertices.Add(new Vector3(0f, y, 0f));
            normals.Add(n);
            uvs.Add(new Vector2(radius, radius));
            for (int i = 0; i <= segments; i++)
            {
                float a = 2f * Mathf.PI * i / segments;
                vertices.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
                normals.Add(n);
                uvs.Add(new Vector2(radius + Mathf.Cos(a) * radius, radius + Mathf.Sin(a) * radius));
            }
            for (int i = 0; i < segments; i++)
            {
                int a = center + 1 + i;
                int b = a + 1;
                Vector3 faceDir = Vector3.Cross(vertices[a] - vertices[center], vertices[b] - vertices[center]);
                if (Vector3.Dot(faceDir, n) > 0f)
                {
                    triangles.AddRange(new[] { center, a, b });
                }
                else
                {
                    triangles.AddRange(new[] { center, b, a });
                }
            }
        }

        // ---------------- Textures ----------------

        static string AlbedoPath(string surface) => TextureDir + "/T_" + surface + "_Albedo.png";
        static string NormalPath(string surface) => TextureDir + "/T_" + surface + "_Normal.png";

        static void ConfigureTextureImporters()
        {
            foreach (string surface in TextureMean.Keys)
            {
                ConfigureTexture(AlbedoPath(surface), false);
                ConfigureTexture(NormalPath(surface), true);
            }
        }

        // 512px keeps the WebGL download small; anisotropic filtering keeps floors
        // crisp when viewed at a low angle.
        static void ConfigureTexture(string path, bool normalMap)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[SceneBuilder] " + path + " not found. Run `python3 Tools/generate_textures.py`.");
                return;
            }

            TextureImporterType type = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType == type && importer.sRGBTexture == !normalMap
                && importer.wrapMode == TextureWrapMode.Repeat && importer.mipmapEnabled
                && importer.anisoLevel == 8 && importer.maxTextureSize == 512)
            {
                return;
            }

            importer.textureType = type;
            importer.sRGBTexture = !normalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        // tileMeters: how many metres one repeat of the texture covers.
        static void ApplySurface(Material m, string surface, float tileMeters, float bumpScale)
        {
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath(surface));
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath(surface));
            if (albedo == null || normal == null)
            {
                Debug.LogError("[SceneBuilder] Missing textures for '" + surface + "'; " + m.name + " stays untextured.");
                return;
            }

            float mean = TextureMean[surface];
            Color c = m.color;
            m.color = new Color(c.r / mean, c.g / mean, c.b / mean, c.a);
            m.mainTexture = albedo;
            m.mainTextureScale = Vector2.one / tileMeters;

            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", bumpScale);
            m.EnableKeyword("_NORMALMAP");

            if (m.IsKeywordEnabled("_EMISSION"))
            {
                // Let the paper's fibres show through its glow too.
                m.SetTexture("_EmissionMap", albedo);
                Color e = m.GetColor("_EmissionColor");
                m.SetColor("_EmissionColor", new Color(e.r / mean, e.g / mean, e.b / mean, e.a));
            }
        }
    }
}
