using System.Collections.Generic;
using GMConsole;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameMasterConsoleEnv.Demo
{
    public sealed class ArenaDemo : MonoBehaviour
    {
        private const float MoveSpeed = 7f;
        private const float AttackReach = 2.55f;
        private const float AttackHalfAngle = 62f;
        private const float AttackCooldown = 0.36f;
        private const float SweepDuration = 0.20f;
        private const float SpawnInterval = 1.4f;
        private const int EnemyHealth = 64;
        private const int KillExperience = 25;
        private const int ArenaPopulation = 9;
        private const int CenterSpawnCount = 10;
        private const int SetLevelLimit = 1000;
        private const float EnemySpacing = 1.5f;
        private const string SpawnCommand = "SpawnEnemy";
        private const string ExperienceCommand = "GrantXp";
        private const string LevelCommand = "SetLevel";
        private const string CenterSpawnCommand = "SpawnCenterEnemies";
        private const string TeleportCommand = "TeleportToCenter";
        private static readonly Plane Ground = new(Vector3.up, Vector3.zero);

        [SerializeField] private bool enableGameMasterConsole = true;
        [SerializeField] private Material arenaMaterial;
        [SerializeField] private Mesh cubeMesh;
        [SerializeField] private Mesh sphereMesh;
        [SerializeField] private Mesh capsuleMesh;
        [SerializeField] private Mesh cylinderMesh;

        private readonly List<Enemy> _enemies = new();
        private ArenaWorld _world;
        private ArenaActorView _hero;
        private LineRenderer _sweep;
        private Vector3 _sweepDirection;
        private Vector3 _sweepOrigin;
        private float _nextAttack;
        private float _nextSpawn;
        private float _sweepUntil;
        private float _noticeUntil;
        private string _notice = string.Empty;
        private bool _commandsRegistered;
        private bool _previousRunInBackground;

        internal int Level { get; private set; } = 1;
        internal int Experience { get; private set; }
        internal int ExperienceToNextLevel => 50 + (Level - 1) * 25;
        internal int AttackDamage => 22 + (Level - 1) * 10;
        internal int Kills { get; private set; }
        internal int EnemiesAlive => _enemies.Count;
        internal string Notice => Time.time < _noticeUntil ? _notice : string.Empty;

        private void Awake()
        {
            ArenaArt.Initialize(arenaMaterial, cubeMesh, sphereMesh, capsuleMesh, cylinderMesh);
            _world = new ArenaWorld(transform);
            _hero = ArenaActorView.CreateHero(transform);
            _sweep = CreateSweep();
            gameObject.AddComponent<ArenaHud>().Initialize(this);

            _nextSpawn = Time.time + SpawnInterval;
            for (int i = 0; i < ArenaPopulation; i++)
            {
                if (!TrySpawnPadEnemy())
                    break;
            }
        }

        private void Start()
        {
            if (!enableGameMasterConsole || !Debug.isDebugBuild)
                return;

            GameMaster.RegisterCommand(ExperienceCommand, "Grant experience. Usage: GrantXp <amount>", GrantExperienceFromConsole);
            GameMaster.RegisterCommand(LevelCommand, "Set the player level and reset XP progress. Usage: SetLevel <level>", SetLevelFromConsole);
            GameMaster.RegisterCommand(SpawnCommand, "Spawn enemies in the arena. Usage: SpawnEnemy [count]", SpawnFromConsole);
            GameMaster.RegisterCommand(CenterSpawnCommand, $"Spawn {CenterSpawnCount} enemies in the center of the arena.", SpawnCenterFromConsole);
            GameMaster.RegisterCommand(TeleportCommand, "Move the player to the center of the arena.", TeleportToCenterFromConsole);
            _commandsRegistered = true;
            _previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            GameMaster.StartServer();
        }

        private void OnDestroy()
        {
            if (!_commandsRegistered)
                return;

            GameMaster.StopServer();
            GameMaster.UnregisterCommand(ExperienceCommand);
            GameMaster.UnregisterCommand(LevelCommand);
            GameMaster.UnregisterCommand(SpawnCommand);
            GameMaster.UnregisterCommand(CenterSpawnCommand);
            GameMaster.UnregisterCommand(TeleportCommand);
            Application.runInBackground = _previousRunInBackground;
        }

        private void Update()
        {
            if (!Application.isFocused)
                return;

            float now = Time.time;
            var movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            movement = Vector3.ClampMagnitude(movement, 1f);
            MoveHero(movement, Time.deltaTime);

            if (Input.GetMouseButton(0) && now >= _nextAttack)
                Attack(AimDirection(), now);
            else if (movement.sqrMagnitude > 0f && now >= _sweepUntil)
                Face(_hero.Root, movement, Time.deltaTime);

            float attackPulse = Mathf.Clamp01((_sweepUntil - now) / SweepDuration);
            _hero.AnimateHero(movement.magnitude, attackPulse, now);
            UpdateSweep(attackPulse);

            if (now >= _nextSpawn)
            {
                if (CountPadEnemies() < ArenaPopulation)
                    TrySpawnPadEnemy();
                _nextSpawn = now + SpawnInterval;
            }

            foreach (Enemy enemy in _enemies)
            {
                if (enemy.HurtUntil > 0f && now >= enemy.HurtUntil)
                {
                    enemy.View.SetEnemyHurt(false);
                    enemy.HurtUntil = 0f;
                }
            }
        }

        private void LateUpdate()
        {
            _world.Follow(_hero.Root.position);
        }

        private void MoveHero(Vector3 movement, float deltaTime)
        {
            Vector3 position = _hero.Root.position + movement * (MoveSpeed * deltaTime);
            _hero.Root.position = Vector3.ClampMagnitude(position, ArenaWorld.PlayableRadius);
        }

        private Vector3 AimDirection()
        {
            Ray ray = _world.Camera.ScreenPointToRay(Input.mousePosition);
            if (Ground.Raycast(ray, out float distance))
            {
                Vector3 offset = ray.GetPoint(distance) - _hero.Root.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > 0.01f)
                    return offset.normalized;
            }
            return _hero.Root.forward;
        }

        private void Attack(Vector3 direction, float now)
        {
            _nextAttack = now + AttackCooldown;
            _sweepUntil = now + SweepDuration;
            _sweepOrigin = _hero.Root.position;
            _sweepDirection = direction;
            _hero.Root.rotation = Quaternion.LookRotation(direction);

            int damage = AttackDamage;
            int defeated = 0;
            float cone = Mathf.Cos(AttackHalfAngle * Mathf.Deg2Rad);
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _enemies[i];
                Transform target = enemy.View.Root;
                Vector3 offset = target.position - _sweepOrigin;
                float distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > AttackReach * AttackReach ||
                    (distanceSquared > 0.16f && Vector3.Dot(direction, offset.normalized) < cone))
                    continue;

                enemy.Health -= damage;
                if (enemy.Health <= 0)
                {
                    target.gameObject.SetActive(false);
                    Destroy(target.gameObject);
                    _enemies.RemoveAt(i);
                    defeated++;
                    continue;
                }

                enemy.HurtUntil = now + 0.19f;
                enemy.View.SetHealth(enemy.Health / (float)EnemyHealth);
                enemy.View.SetEnemyHurt(true);
            }

            if (defeated > 0)
            {
                Kills += defeated;
                GrantExperience(defeated * KillExperience);
            }
        }

        private bool TrySpawnPadEnemy()
        {
            IReadOnlyList<Vector3> points = _world.SpawnPoints;
            int firstPoint = Random.Range(0, points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 position = points[(firstPoint + i) % points.Count];
                if (!IsOpenSpawnPoint(position, minimumPlayerDistance: 3f))
                    continue;

                SpawnEnemyAt(position, spawnedOnPad: true);
                return true;
            }
            return false;
        }

        private void SpawnEnemyAt(Vector3 position, bool spawnedOnPad)
        {
            ArenaActorView view = ArenaActorView.CreateEnemy(transform);
            view.Root.SetPositionAndRotation(position, Quaternion.LookRotation(-position));
            _enemies.Add(new Enemy(view, spawnedOnPad));
        }

        private int CountPadEnemies()
        {
            int count = 0;
            foreach (Enemy enemy in _enemies)
            {
                if (enemy.SpawnedOnPad)
                    count++;
            }
            return count;
        }

        private bool IsOpenSpawnPoint(Vector3 position, float minimumPlayerDistance)
        {
            if ((position - _hero.Root.position).sqrMagnitude < minimumPlayerDistance * minimumPlayerDistance)
                return false;

            foreach (Enemy enemy in _enemies)
            {
                if ((position - enemy.View.Root.position).sqrMagnitude < EnemySpacing * EnemySpacing)
                    return false;
            }
            return true;
        }

        private void GrantExperience(int amount)
        {
            Experience += amount;
            int previousLevel = Level;
            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
            }

            if (Level > previousLevel)
            {
                _notice = $"LEVEL UP  ·  {AttackDamage} DAMAGE";
                _noticeUntil = Time.time + 2.1f;
            }
            else if (Time.time >= _noticeUntil || !_notice.StartsWith("LEVEL UP", System.StringComparison.Ordinal))
            {
                _notice = $"+{amount} XP";
                _noticeUntil = Time.time + 1.25f;
            }
        }

        private LineRenderer CreateSweep()
        {
            var effect = new GameObject("Sword Sweep");
            effect.transform.SetParent(transform, false);
            var line = effect.AddComponent<LineRenderer>();
            line.sharedMaterial = ArenaArt.Material("Sword Sweep", Color.cyan, emission: new Color(0.16f, 1f, 1f));
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 17;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.06f), new Keyframe(0.45f, 1f), new Keyframe(1f, 0.06f));
            line.enabled = false;
            return line;
        }

        private void UpdateSweep(float pulse)
        {
            _sweep.enabled = pulse > 0f;
            if (!_sweep.enabled)
                return;

            _sweep.widthMultiplier = 0.15f * pulse;
            float sweepLead = Mathf.Lerp(-32f, 22f, 1f - pulse);
            float radius = Mathf.Lerp(1.45f, AttackReach, 1f - pulse);
            for (int i = 0; i < _sweep.positionCount; i++)
            {
                float angle = Mathf.Lerp(-AttackHalfAngle, AttackHalfAngle, i / (float)(_sweep.positionCount - 1)) + sweepLead;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * _sweepDirection;
                _sweep.SetPosition(i, _sweepOrigin + Vector3.up * 0.72f + direction * radius);
            }
        }

        private void SpawnFromConsole(GMArgs args)
        {
            if (!TryReadAmount(args, 1, int.MaxValue, out int count))
            {
                args.SetResult($"Usage: {SpawnCommand} [positive whole number]");
                return;
            }

            int spawned = 0;
            while (spawned < count && TrySpawnPadEnemy())
                spawned++;
            for (; spawned < count; spawned++)
                SpawnEnemyAt(FindOffPadSpawnPosition(), spawnedOnPad: false);

            args.SetResult($"Spawned {spawned}. Enemies in the arena: {EnemiesAlive}.");
        }

        private Vector3 FindOffPadSpawnPosition()
        {
            const float innerRadius = 8f;
            const float outerRadius = ArenaWorld.PlayableRadius - 0.8f;
            Vector3 position = Vector3.zero;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                float radius = Mathf.Sqrt(Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
                float yaw = Random.Range(0f, 360f);
                position = Quaternion.Euler(0f, yaw, 0f) * (Vector3.forward * radius);
                if (IsOpenSpawnPoint(position, minimumPlayerDistance: 3f))
                    return position;
            }

            // A crowded arena still accepts debug spawns.
            return position;
        }

        private void SpawnCenterFromConsole(GMArgs args)
        {
            if (args.GetRemainingStringCount() != 0 || args.GetRemainingFloatCount() != 0)
            {
                args.SetResult($"Usage: {CenterSpawnCommand}");
                return;
            }

            // Each ring has room between enemies and the outer rings handle repeated calls.
            var positions = new List<Vector3>(CenterSpawnCount);
            for (int ring = 0; ring < 3 && positions.Count < CenterSpawnCount; ring++)
            {
                float radius = 2.65f + 1.7f * ring;
                int slots = CenterSpawnCount + 6 * ring;
                for (int slot = 0; slot < slots && positions.Count < CenterSpawnCount; slot++)
                {
                    float yaw = (slot + ring * 0.5f) * (360f / slots);
                    Vector3 position = Quaternion.Euler(0f, yaw, 0f) * (Vector3.forward * radius);
                    if (IsOpenSpawnPoint(position, minimumPlayerDistance: EnemySpacing))
                        positions.Add(position);
                }
            }

            if (positions.Count < CenterSpawnCount)
            {
                args.SetResult($"The center needs room for {CenterSpawnCount} enemies. Clear some space and try again.");
                return;
            }

            foreach (Vector3 position in positions)
                SpawnEnemyAt(position, spawnedOnPad: false);
            args.SetResult($"Spawned {CenterSpawnCount} enemies in the center.");
        }

        private void GrantExperienceFromConsole(GMArgs args)
        {
            if (!TryReadAmount(args, 0, 10000, out int amount))
            {
                args.SetResult($"Usage: {ExperienceCommand} <whole number from 1 to 10000>");
                return;
            }
            GrantExperience(amount);
            args.SetResult($"Granted {amount} XP. Level {Level}, {Experience}/{ExperienceToNextLevel} XP.");
        }

        private void SetLevelFromConsole(GMArgs args)
        {
            if (!TryReadAmount(args, 0, SetLevelLimit, out int level))
            {
                args.SetResult($"Usage: {LevelCommand} <whole number from 1 to {SetLevelLimit}>");
                return;
            }

            Level = level;
            Experience = 0;
            _notice = $"LEVEL {Level}  ·  {AttackDamage} DAMAGE";
            _noticeUntil = Time.time + 2.1f;
            args.SetResult($"Set level to {Level}. Damage: {AttackDamage}. XP: {Experience}/{ExperienceToNextLevel}.");
        }

        private void TeleportToCenterFromConsole(GMArgs args)
        {
            if (args.GetRemainingStringCount() != 0 || args.GetRemainingFloatCount() != 0)
            {
                args.SetResult($"Usage: {TeleportCommand}");
                return;
            }

            _hero.Root.position = Vector3.zero;
            _sweepUntil = 0f;
            _sweep.enabled = false;
            _world.Follow(_hero.Root.position, snap: true);
            args.SetResult("Teleported the player to the center of the arena.");
        }

        private static bool TryReadAmount(GMArgs args, int defaultValue, int maximum, out int amount)
        {
            amount = defaultValue;
            if (args.GetRemainingStringCount() != 0)
                return false;
            if (args.GetRemainingFloatCount() == 0)
                return defaultValue > 0;
            if (args.GetRemainingFloatCount() != 1 || !args.TryGetFloat(out float value) ||
                !(value >= 1f && (double)value <= maximum) || value != Mathf.Floor(value))
                return false;

            amount = (int)value;
            return true;
        }

        private static void Face(Transform actor, Vector3 direction, float deltaTime)
        {
            actor.rotation = Quaternion.RotateTowards(actor.rotation, Quaternion.LookRotation(direction), 840f * deltaTime);
        }

        private sealed class Enemy
        {
            internal readonly ArenaActorView View;
            internal readonly bool SpawnedOnPad;
            internal int Health = EnemyHealth;
            internal float HurtUntil;

            internal Enemy(ArenaActorView view, bool spawnedOnPad)
            {
                View = view;
                SpawnedOnPad = spawnedOnPad;
            }
        }
    }
}
