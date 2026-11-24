using UnityEngine;
using UnityEngine.Rendering;

namespace GameMasterConsoleEnv.Demo
{
    internal sealed class ArenaActorView
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static ActorMaterials materials;
        private static Mesh shellMesh;

        private readonly MaterialPropertyBlock hitProperties;
        private readonly Transform body;
        private readonly Transform[] heroLegs;
        private readonly Renderer crawlerShell;
        private readonly Transform weaponArm;
        private readonly Transform health;
        private readonly Transform healthFill;
        private bool wasHurt;

        internal Transform Root { get; }

        private ArenaActorView(Transform parent, bool isHero)
        {
            if (materials == null || !materials.Armor)
                materials = new ActorMaterials();

            Root = Group(isHero ? "Rift Warden" : "Rift Crawler", parent, Vector3.zero);
            body = Group("Body", Root, Vector3.zero);

            if (isHero)
            {
                heroLegs = new Transform[2];
                BuildHero(out weaponArm);
            }
            else
            {
                crawlerShell = BuildCrawler();
                hitProperties = new MaterialPropertyBlock();
                hitProperties.SetColor(ColorId, new Color(1f, 0.68f, 0.36f));
                health = Group("Health", Root, new Vector3(0f, 1.22f, 0f));
                Box("Health track", health, Vector3.zero, new Vector3(0.86f, 0.105f, 0.035f), materials.Track);
                healthFill = Box("Health remaining", health, new Vector3(0f, 0f, -0.024f),
                    new Vector3(0.78f, 0.051f, 0.012f), materials.Amber);
                foreach (var renderer in health.GetComponentsInChildren<Renderer>())
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                health.gameObject.SetActive(false);
            }
        }

        internal static ArenaActorView CreateHero(Transform parent) => new ArenaActorView(parent, true);

        internal static ArenaActorView CreateEnemy(Transform parent) => new ArenaActorView(parent, false);

        internal void AnimateHero(float speed01, float attackPulse, float time)
        {
            float stride = Mathf.Sin(time * 13f) * 27f * speed01;
            body.localPosition = Vector3.up * (Mathf.Abs(stride) * 0.0035f);
            heroLegs[0].localRotation = Quaternion.Euler(stride, 0f, 0f);
            heroLegs[1].localRotation = Quaternion.Euler(-stride, 0f, 0f);

            float swing = attackPulse > 0f
                ? Mathf.Lerp(-72f, 54f, Mathf.SmoothStep(0f, 1f, 1f - attackPulse))
                : -10f + stride * 0.2f;
            weaponArm.localRotation = Quaternion.Euler(12f - attackPulse * 22f, swing, -12f);
            body.localRotation = Quaternion.Euler(speed01 * 5f, swing * 0.16f, -stride * 0.09f);
        }

        internal void SetEnemyHurt(bool hurt)
        {
            if (hurt == wasHurt)
                return;

            wasHurt = hurt;
            crawlerShell.SetPropertyBlock(hurt ? hitProperties : null);
        }

        internal void SetHealth(float normalized)
        {
            if (!health)
                return;

            float fraction = Mathf.Clamp01(normalized);
            bool showHealth = fraction < 0.995f && fraction > 0f;
            health.gameObject.SetActive(showHealth);
            if (!showHealth)
                return;

            Camera camera = Camera.main;
            if (camera)
                health.rotation = camera.transform.rotation;

            healthFill.localScale = new Vector3(0.78f * fraction, 0.051f, 0.012f);
            healthFill.localPosition = new Vector3(0.39f * (fraction - 1f), 0f, -0.024f);
        }

        private void BuildHero(out Transform swordArm)
        {
            for (int i = 0; i < heroLegs.Length; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Transform leg = Group("Leg", body, new Vector3(side * 0.16f, 0.66f, 0f));
                Box("Undersuit", leg, new Vector3(0f, -0.21f, 0f), new Vector3(0.215f, 0.43f, 0.215f), materials.Suit);
                Box("Shin plate", leg, new Vector3(0f, -0.37f, 0.077f), new Vector3(0.205f, 0.27f, 0.15f), materials.Armor);
                Box("Boot", leg, new Vector3(0f, -0.55f, 0.04f), new Vector3(0.25f, 0.19f, 0.38f), materials.Edge);
                Box("Boot stripe", leg, new Vector3(0f, -0.463f, 0.113f), new Vector3(0.14f, 0.025f, 0.19f), materials.Cyan);
                heroLegs[i] = leg;
            }

            Box("Belt", body, new Vector3(0f, 0.735f, 0f), new Vector3(0.47f, 0.18f, 0.36f), materials.Suit);
            ArenaArt.Shape(PrimitiveType.Capsule, "Torso armor", body, new Vector3(0f, 1.04f, 0f),
                new Vector3(0.60f, 0.32f, 0.405f), materials.Armor);
            Box("Chest light", body, new Vector3(0f, 1.13f, 0.193f), new Vector3(0.245f, 0.065f, 0.05f), materials.Cyan);
            Box("Back plate", body, new Vector3(0f, 1.025f, -0.165f), new Vector3(0.33f, 0.34f, 0.14f), materials.Edge);
            Box("Back light", body, new Vector3(0f, 1.025f, -0.242f), new Vector3(0.085f, 0.245f, 0.028f), materials.Cyan);

            ArenaArt.Shape(PrimitiveType.Sphere, "Helmet", body, new Vector3(0f, 1.53f, 0.015f),
                new Vector3(0.465f, 0.49f, 0.44f), materials.Armor);
            Box("Face shield", body, new Vector3(0f, 1.525f, 0.195f), new Vector3(0.35f, 0.175f, 0.10f), materials.Suit);
            Box("Visor light", body, new Vector3(0f, 1.55f, 0.25f), new Vector3(0.286f, 0.047f, 0.029f), materials.Cyan);
            Box("Helmet stripe", body, new Vector3(0f, 1.765f, 0.005f), new Vector3(0.082f, 0.034f, 0.26f), materials.Edge);

            swordArm = null;
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Transform arm = Group("Arm", body, new Vector3(side * 0.355f, 1.23f, 0f));
                Transform shoulder = Box("Shoulder armor", arm, new Vector3(side * 0.035f, 0f, 0f),
                    new Vector3(0.255f, 0.215f, 0.34f), materials.Armor);
                shoulder.localRotation = Quaternion.Euler(0f, 0f, -side * 20f);
                Box("Shoulder light", shoulder, new Vector3(0f, 0.56f, 0f), new Vector3(0.23f, 0.08f, 0.84f), materials.Cyan);
                Box("Arm", arm, new Vector3(side * 0.045f, -0.20f, 0.034f),
                    new Vector3(0.17f, 0.38f, 0.185f), materials.Suit);
                Box("Gauntlet", arm, new Vector3(side * 0.054f, -0.36f, 0.093f),
                    new Vector3(0.18f, 0.21f, 0.255f), materials.Edge);
                if (i == 1)
                    swordArm = arm;
            }

            Transform sword = Group("Rift blade", swordArm, new Vector3(0.06f, -0.385f, 0.165f));
            Box("Grip", sword, new Vector3(0f, 0f, 0.08f), new Vector3(0.12f, 0.099f, 0.33f), materials.Suit);
            Box("Guard", sword, new Vector3(0f, 0f, 0.24f), new Vector3(0.35f, 0.128f, 0.12f), materials.Edge);
            Box("Blade", sword, new Vector3(0f, 0f, 0.67f), new Vector3(0.17f, 0.083f, 0.82f), materials.Cyan);
            Box("Blade core", sword, new Vector3(0f, 0.045f, 0.66f), new Vector3(0.065f, 0.03f, 0.84f), materials.Armor);
            Transform point = Box("Blade point", sword, new Vector3(0f, 0f, 1.078f),
                new Vector3(0.125f, 0.083f, 0.125f), materials.Cyan);
            point.localRotation = Quaternion.Euler(0f, 45f, 0f);
        }

        private Renderer BuildCrawler()
        {
            shellMesh = shellMesh ? shellMesh : BuildShell();
            MeshPart("Underbelly", shellMesh, body, new Vector3(0f, 0.37f, 0.11f), new Vector3(0.48f, 0.65f, 0.6f),
                materials.CrawlerJoint);
            Transform shell = MeshPart("Faceted carapace", shellMesh, body, new Vector3(0f, 0.57f, -0.07f),
                new Vector3(0.57f, 0.73f, 0.67f), materials.Shell);
            MeshPart("Rift crystal", shellMesh, body, new Vector3(0f, 0.81f, -0.10f),
                new Vector3(0.27f, 0.47f, 0.4f), materials.Amber);
            MeshPart("Face", shellMesh, body, new Vector3(0f, 0.46f, 0.52f), new Vector3(0.37f, 0.46f, 0.29f),
                materials.CrawlerJoint);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Transform eye = Box("Eye", body, new Vector3(side * 0.178f, 0.53f, 0.749f),
                    new Vector3(0.15f, 0.095f, 0.07f), materials.Amber);
                eye.localRotation = Quaternion.Euler(0f, side * 19f, side * 18f);
                Transform claw = Box("Mandible", body, new Vector3(side * 0.24f, 0.34f, 0.71f),
                    new Vector3(0.14f, 0.14f, 0.32f), materials.Shell);
                claw.localRotation = Quaternion.Euler(0f, -side * 24f, 0f);

                for (int j = 0; j < 3; j++)
                {
                    Transform leg = Group("Crawler leg", body, new Vector3(side * 0.37f, 0.35f, 0.31f - j * 0.32f));
                    leg.localRotation = Quaternion.Euler(0f, side * (j - 1) * 23f, 0f);
                    Transform thigh = Box("Upper segment", leg, new Vector3(side * 0.18f, 0.02f, 0f),
                        new Vector3(0.40f, 0.15f, 0.17f), materials.Shell);
                    thigh.localRotation = Quaternion.Euler(0f, 0f, side * 19f);
                    Transform foot = Box("Lower segment", leg, new Vector3(side * 0.37f, -0.13f, 0f),
                        new Vector3(0.13f, 0.39f, 0.13f), materials.CrawlerJoint);
                    foot.localRotation = Quaternion.Euler(0f, 0f, side * 29f);
                }
            }
            return shell.GetComponent<Renderer>();
        }

        private static Transform Group(string name, Transform parent, Vector3 position)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            return group;
        }

        private static Transform Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            return ArenaArt.Shape(PrimitiveType.Cube, name, parent, position, scale, material);
        }

        private static Transform MeshPart(string name, Mesh mesh, Transform parent, Vector3 position, Vector3 scale,
            Material material)
        {
            Transform part = Group(name, parent, position);
            part.localScale = scale;
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        private static Mesh BuildShell()
        {
            const int sides = 8;
            float[] radius = { 0.56f, 1f, 0.53f, 0f };
            float[] height = { -0.42f, -0.06f, 0.41f, 0.52f };
            var vertices = new System.Collections.Generic.List<Vector3>(sides * 24);
            for (int ring = 0; ring < radius.Length - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    Vector3 a = ShellPoint(side, sides, radius[ring], height[ring]);
                    Vector3 b = ShellPoint(side + 1, sides, radius[ring], height[ring]);
                    Vector3 c = ShellPoint(side, sides, radius[ring + 1], height[ring + 1]);
                    Vector3 d = ShellPoint(side + 1, sides, radius[ring + 1], height[ring + 1]);
                    vertices.Add(a);
                    vertices.Add(b);
                    vertices.Add(c);
                    if (radius[ring + 1] > 0f)
                    {
                        vertices.Add(b);
                        vertices.Add(d);
                        vertices.Add(c);
                    }
                }
            }
            int[] indices = new int[vertices.Count];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = i;
            var mesh = new Mesh { name = "Faceted Crawler Shell" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Vector3 ShellPoint(int side, int sides, float radius, float height)
        {
            float angle = (side + 0.5f) * (Mathf.PI * 2f / sides);
            return new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);
        }

        private sealed class ActorMaterials
        {
            internal readonly Material Armor = ArenaArt.Material("Porcelain armor", new Color32(197, 228, 235, 255), 0.18f);
            internal readonly Material Edge = ArenaArt.Material("Gunmetal armor", new Color32(64, 103, 122, 255), 0.21f);
            internal readonly Material Suit = ArenaArt.Material("Warden undersuit", new Color32(27, 45, 64, 255));
            internal readonly Material Cyan = ArenaArt.Material("Rift blade energy", new Color32(95, 239, 238, 255),
                emission: new Color(0.13f, 0.68f, 0.68f));
            internal readonly Material Shell = ArenaArt.Material("Crawler carapace", new Color32(203, 78, 66, 255), 0.09f);
            internal readonly Material CrawlerJoint = ArenaArt.Material("Crawler joints", new Color32(83, 41, 54, 255));
            internal readonly Material Amber = ArenaArt.Material("Crawler crystal", new Color32(255, 173, 66, 255),
                emission: new Color(0.75f, 0.25f, 0.04f));
            internal readonly Material Track = ArenaArt.Material("Health track", new Color32(36, 23, 33, 255));
        }
    }
}
