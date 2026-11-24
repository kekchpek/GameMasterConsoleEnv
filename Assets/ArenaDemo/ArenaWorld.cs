using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameMasterConsoleEnv.Demo
{
    internal sealed class ArenaWorld
    {
        internal const float PlayableRadius = 18f;
        private const float FloorRadius = PlayableRadius + 1.45f;
        private static readonly Quaternion CameraAngle = Quaternion.Euler(52f, 0f, 0f);
        private readonly Transform _stage;
        private readonly List<Vector3> _spawnPoints = new(22);
        private Vector3 _cameraTarget;

        internal Camera Camera { get; }
        internal IReadOnlyList<Vector3> SpawnPoints => _spawnPoints;

        internal ArenaWorld(Transform parent)
        {
            _stage = new GameObject("Rift arena").transform;
            _stage.SetParent(parent, false);
            CreateSurface();
            CreatePerimeter();
            CreateSpawnPads();
            CreateLighting();

            var cameraObject = new GameObject("Arena camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            Camera = cameraObject.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color32(17, 26, 43, 255);
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 65f;
            Follow(Vector3.zero, true);
        }

        internal void Follow(Vector3 playerPosition, bool snap = false)
        {
            var goal = new Vector3(playerPosition.x * 0.82f, 0.25f, playerPosition.z * 0.82f);
            float blend = snap ? 1f : 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime);
            _cameraTarget = Vector3.Lerp(_cameraTarget, goal, blend);
            Camera.orthographicSize = Mathf.Max(9.8f, 11.5f / Camera.aspect);
            Camera.transform.SetPositionAndRotation(
                _cameraTarget + CameraAngle * (Vector3.back * 22f), CameraAngle);
        }

        private void CreateSurface()
        {
            var ground = ArenaArt.Material("Deep bedrock", new Color32(21, 30, 49, 255));
            var foundation = ArenaArt.Material("Basalt foundation", new Color32(32, 48, 67, 255), 0.18f);
            var stone = ArenaArt.Material("Blue slate", new Color32(55, 72, 91, 255), 0.08f);
            var inset = ArenaArt.Material("Slate inlay", new Color32(48, 64, 83, 255));
            var grid = ArenaArt.Material("Carved alignment marks", new Color32(67, 89, 106, 255));
            var bronze = ArenaArt.Material("Dull bronze", new Color32(123, 102, 75, 255), 0.25f);
            var aqua = ArenaArt.Material("Cool copper inlay", new Color32(68, 151, 157, 255), 0.18f,
                0.36f, new Color(0.03f, 0.13f, 0.14f));

            Shape(PrimitiveType.Cube, "Bedrock", new Vector3(0f, -1.3f, 0f),
                new Vector3(80f, 0.35f, 80f), ground);
            float foundationDiameter = FloorRadius * 2f + 0.9f;
            Shape(PrimitiveType.Cylinder, "Foundation", new Vector3(0f, -0.58f, 0f),
                new Vector3(foundationDiameter, 0.52f, foundationDiameter), foundation);
            Band("Arena floor", Vector3.zero, 0f, FloorRadius, stone);
            Band("Center seal", Vector3.up * 0.004f, 0f, 2.32f, inset);
            Band("Seal border", Vector3.up * 0.008f, 2.26f, 2.32f, aqua);
            Band("Outer seal border", Vector3.up * 0.008f, 2.51f, 2.55f, grid);
            Band("Inner track", Vector3.up * 0.008f, 7.55f, 7.59f, grid);
            Band("Outer track", Vector3.up * 0.008f, 13.45f, 13.49f, grid);
            Band("Safety line", Vector3.up * 0.009f, PlayableRadius - 0.03f, PlayableRadius + 0.03f, aqua);
            Band("Perimeter inset", Vector3.up * 0.007f, PlayableRadius + 0.37f, FloorRadius, inset);
            Band("Perimeter trim", Vector3.up * 0.011f, FloorRadius - 0.49f, FloorRadius - 0.44f, bronze);

            for (int i = 0; i < 8; i++)
            {
                Quaternion direction = Quaternion.Euler(0f, i * 45f, 0f);
                Transform spoke = Shape(PrimitiveType.Cube, "Track division",
                    direction * new Vector3(0f, 0.002f, 9.47f), new Vector3(0.024f, 0.016f, 13.8f), grid);
                spoke.localRotation = direction;

                Transform sealMark = Shape(PrimitiveType.Cube, "Seal marking",
                    direction * new Vector3(0f, 0.015f, 1.53f), new Vector3(0.11f, 0.025f, 0.4f), aqua);
                sealMark.localRotation = direction;
            }

            Transform center = Shape(PrimitiveType.Cube, "Rift emblem", Vector3.up * 0.013f,
                new Vector3(0.78f, 0.024f, 0.78f), aqua);
            center.localRotation = Quaternion.Euler(0f, 45f, 0f);
        }

        private void CreatePerimeter()
        {
            var baseStone = ArenaArt.Material("Edge stone", new Color32(40, 57, 76, 255), 0.15f);
            var cap = ArenaArt.Material("Cut stone", new Color32(75, 97, 116, 255), 0.14f);
            var rail = ArenaArt.Material("Copper marker", new Color32(70, 168, 172, 255), 0.15f,
                0.34f, new Color(0.05f, 0.22f, 0.22f));

            for (int i = 0; i < 8; i++)
            {
                Quaternion direction = Quaternion.Euler(0f, i * 45f, 0f);
                Vector3 rim = direction * (Vector3.forward * (FloorRadius - 0.7f));
                Transform plinth = Shape(PrimitiveType.Cube, "Edge plinth", rim + Vector3.up * 0.13f,
                    new Vector3(1.88f, 0.27f, 0.61f), baseStone);
                plinth.localRotation = direction;
                Transform trim = Shape(PrimitiveType.Cube, "Plinth accent", rim + Vector3.up * 0.283f,
                    new Vector3(1.69f, 0.04f, 0.19f), rail);
                trim.localRotation = direction;

                Quaternion postDirection = direction * Quaternion.Euler(0f, 22.5f, 0f);
                Vector3 post = postDirection * (Vector3.forward * (FloorRadius - 0.53f));
                Shape(PrimitiveType.Cylinder, "Beacon footing", post + Vector3.up * 0.09f,
                    new Vector3(0.86f, 0.11f, 0.86f), baseStone);
                Shape(PrimitiveType.Cylinder, "Beacon pillar", post + Vector3.up * 0.39f,
                    new Vector3(0.45f, 0.38f, 0.45f), cap);
                Shape(PrimitiveType.Cylinder, "Beacon light", post + Vector3.up * 0.78f,
                    new Vector3(0.40f, 0.045f, 0.40f), rail);
                Shape(PrimitiveType.Cylinder, "Beacon crown", post + Vector3.up * 0.83f,
                    new Vector3(0.52f, 0.04f, 0.52f), baseStone);
            }
        }

        private void CreateSpawnPads()
        {
            var pad = ArenaArt.Material("Spawn pad stone", new Color32(40, 57, 76, 255), 0.15f);
            var amber = ArenaArt.Material("Rift amber", new Color32(232, 154, 71, 255), 0.1f,
                0.3f, new Color(0.45f, 0.17f, 0.025f));

            CreateSpawnRing(4.8f, 6, 0f, pad, amber);
            CreateSpawnRing(10.3f, 8, 22.5f, pad, amber);
            CreateSpawnRing(16.1f, 8, 0f, pad, amber);
        }

        private void CreateSpawnRing(float radius, int count, float offset, Material pad, Material amber)
        {
            for (int i = 0; i < count; i++)
            {
                Quaternion direction = Quaternion.Euler(0f, offset + i * 360f / count, 0f);
                Vector3 position = direction * (Vector3.forward * radius);
                _spawnPoints.Add(position);
                Band("Spawn pad", position + Vector3.up * 0.012f, 0f, 0.72f, pad, 24);
                Band("Spawn ring", position + Vector3.up * 0.016f, 0.56f, 0.63f, amber, 24);
                Transform marker = Shape(PrimitiveType.Cube, "Spawn marker", position + Vector3.up * 0.028f,
                    new Vector3(0.13f, 0.025f, 0.3f), amber);
                marker.localRotation = direction;
            }
        }

        private void CreateLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color32(145, 161, 190, 255);
            var lightObject = new GameObject("Arena sunlight");
            lightObject.transform.SetParent(_stage, false);
            lightObject.transform.localRotation = Quaternion.Euler(47f, -34f, 0f);

            Light sunlight = lightObject.AddComponent<Light>();
            sunlight.type = LightType.Directional;
            sunlight.color = new Color32(231, 242, 255, 255);
            sunlight.intensity = 1.22f;
            sunlight.shadows = LightShadows.Soft;
            sunlight.shadowStrength = 0.77f;
        }

        private Transform Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            return ArenaArt.Shape(type, name, _stage, position, scale, material);
        }

        private void Band(string name, Vector3 position, float inner, float outer, Material material, int segments = 96)
        {
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                var unit = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                vertices[i * 2] = unit * inner;
                vertices[i * 2 + 1] = unit * outer;
                if (i == segments)
                    break;

                int vertex = i * 2;
                int index = i * 6;
                triangles[index] = vertex;
                triangles[index + 1] = vertex + 1;
                triangles[index + 2] = vertex + 2;
                triangles[index + 3] = vertex + 1;
                triangles[index + 4] = vertex + 3;
                triangles[index + 5] = vertex + 2;
            }

            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var band = new GameObject(name);
            band.transform.SetParent(_stage, false);
            band.transform.localPosition = position;
            band.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = band.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    internal static class ArenaArt
    {
        private static Material _template;
        private static Mesh _cube;
        private static Mesh _sphere;
        private static Mesh _capsule;
        private static Mesh _cylinder;

        internal static void Initialize(Material template, Mesh cube, Mesh sphere, Mesh capsule, Mesh cylinder)
        {
            _template = template;
            _cube = cube;
            _sphere = sphere;
            _capsule = capsule;
            _cylinder = cylinder;
        }

        internal static Material Material(string name, Color color, float metallic = 0f, float smoothness = 0.2f,
            Color? emission = null)
        {
            var material = new Material(_template) { name = name, color = color };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            material.SetColor("_EmissionColor", emission ?? Color.black);

            return material;
        }

        internal static Transform Shape(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material)
        {
            Mesh mesh = type switch
            {
                PrimitiveType.Cube => _cube,
                PrimitiveType.Sphere => _sphere,
                PrimitiveType.Capsule => _capsule,
                PrimitiveType.Cylinder => _cylinder,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type))
            };

            var shape = new GameObject(name);
            shape.AddComponent<MeshFilter>().sharedMesh = mesh;
            shape.AddComponent<MeshRenderer>().sharedMaterial = material;
            Transform shapeTransform = shape.transform;
            shapeTransform.SetParent(parent, false);
            shapeTransform.localPosition = localPosition;
            shapeTransform.localScale = localScale;
            return shapeTransform;
        }
    }
}
