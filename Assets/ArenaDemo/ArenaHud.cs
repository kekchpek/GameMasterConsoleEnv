using UnityEngine;

namespace GameMasterConsoleEnv.Demo
{
    internal sealed class ArenaHud : MonoBehaviour
    {
        private static readonly Color Surface = new(0.04f, 0.07f, 0.12f, 0.95f);
        private static readonly Color Edge = new(0.20f, 0.34f, 0.44f, 0.78f);
        private static readonly Color Well = new(0.13f, 0.20f, 0.27f);
        private static readonly Color Text = new(0.92f, 0.97f, 1f);
        private static readonly Color Secondary = new(0.63f, 0.76f, 0.83f);
        private static readonly Color Cyan = new(0.38f, 0.92f, 0.91f);
        private static readonly Color Amber = new(1f, 0.74f, 0.38f);
        private const float Margin = 18f;

        private ArenaDemo _game;
        private GUIStyle _label;

        internal void Initialize(ArenaDemo game)
        {
            _game = game;
        }

        private void OnGUI()
        {
            if (_game == null || Event.current.type != EventType.Repaint)
                return;

            _label ??= new GUIStyle(GUI.skin.label)
            {
                padding = new RectOffset(),
                clipping = TextClipping.Clip,
                wordWrap = false
            };

            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1024f, Screen.height / 576f), 0.60f, 1.6f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Matrix4x4 originalMatrix = GUI.matrix;
            Color originalColor = GUI.color;
            GUI.matrix = originalMatrix * Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.color = Color.white;

            try
            {
                var player = new Rect(Margin, Margin, 340f, 86f);
                var arena = new Rect(width - Margin - 155f, Margin, 155f, 62f);
                if (arena.xMin < player.xMax + Margin)
                    arena.y = player.yMax + 8f;

                DrawPlayer(player);
                DrawArena(arena);

                float controlsWidth = Mathf.Min(320f, width - Margin * 2f);
                DrawControls(new Rect((width - controlsWidth) / 2f, height - Margin - 42f, controlsWidth, 42f));

                string notice = _game.Notice;
                if (!string.IsNullOrEmpty(notice))
                    DrawNotice(width, player, arena, notice);
            }
            finally
            {
                GUI.matrix = originalMatrix;
                GUI.color = originalColor;
            }
        }

        private void DrawPlayer(Rect rect)
        {
            Panel(rect);
            Fill(new Rect(rect.x, rect.y + 12f, 3f, 24f), Cyan);
            Label(new Rect(rect.x + 14f, rect.y + 10f, 130f, 28f), "RIFT ARENA", 17, Text, bold: true);

            DrawBadge(new Rect(rect.xMax - 192f, rect.y + 8f, 105f, 32f), "DAMAGE", _game.AttackDamage, Amber);
            DrawBadge(new Rect(rect.xMax - 81f, rect.y + 8f, 67f, 32f), "LVL", _game.Level, Cyan);
            DrawExperience(rect);
        }

        private void DrawBadge(Rect rect, string label, int value, Color tint)
        {
            Fill(rect, Well);
            Label(new Rect(rect.x + 8f, rect.y, rect.width - 40f, rect.height), label, 11, Secondary, bold: true);
            string number = value.ToString();
            int size = number.Length <= 2 ? 21 : Mathf.Clamp(48 / number.Length, 10, 16);
            Label(new Rect(rect.xMax - 40f, rect.y, 32f, rect.height), number, size, tint, TextAnchor.MiddleRight, bold: true);
        }

        private void DrawExperience(Rect panel)
        {
            var row = new Rect(panel.x + 14f, panel.y + 45f, panel.width - 28f, 20f);
            int value = _game.Experience;
            int max = _game.ExperienceToNextLevel;
            Label(row, "EXPERIENCE", 11, Secondary, bold: true);
            Label(row, $"{value} / {max}", 13, Text, TextAnchor.MiddleRight);

            var track = new Rect(row.x, row.yMax + 3f, row.width, 6f);
            Fill(track, Well);
            float fraction = Mathf.Clamp01(value / (float)Mathf.Max(1, max));
            Fill(new Rect(track.x, track.y, track.width * fraction, track.height), Amber);
        }

        private void DrawArena(Rect rect)
        {
            Panel(rect);
            float half = rect.width / 2f;
            Fill(new Rect(rect.center.x, rect.y + 11f, 1f, rect.height - 22f), Edge);
            DrawStat(new Rect(rect.x, rect.y, half, rect.height), "DEFEATED", _game.Kills, Text);
            DrawStat(new Rect(rect.center.x, rect.y, half, rect.height), "ENEMIES", _game.EnemiesAlive, Amber);
        }

        private void DrawStat(Rect rect, string label, int value, Color tint)
        {
            Label(new Rect(rect.x, rect.y + 7f, rect.width, 16f), label, 10, Secondary,
                TextAnchor.MiddleCenter, bold: true);
            Label(new Rect(rect.x, rect.y + 23f, rect.width, 29f), value.ToString(), 22, tint,
                TextAnchor.MiddleCenter, bold: true);
        }

        private void DrawControls(Rect rect)
        {
            Panel(rect);
            bool compact = rect.width < 300f;
            float x = rect.x + 12f;
            float y = rect.y + 9f;
            float moveKeyWidth = compact ? 53f : 113f;

            DrawKey(new Rect(x, y, moveKeyWidth, 24f), compact ? "WASD" : "WASD / ARROWS");
            x += moveKeyWidth + 9f;
            Label(new Rect(x, y, 45f, 24f), "Move", 14, Text);
            x += 52f;
            Fill(new Rect(x, y + 4f, 1f, 16f), Edge);
            x += 13f;
            DrawKey(new Rect(x, y, 39f, 24f), "LMB");
            x += 48f;
            Label(new Rect(x, y, 55f, 24f), "Attack", 14, Text);
        }

        private void DrawKey(Rect rect, string text)
        {
            Fill(rect, Well);
            Label(rect, text, 11, Cyan, TextAnchor.MiddleCenter, bold: true);
        }

        private void DrawNotice(float width, Rect player, Rect arena, string notice)
        {
            float gapMin = player.xMax + 12f;
            float gapMax = arena.xMin - 12f;
            float noticeWidth = Mathf.Min(286f, gapMax - gapMin);
            float x = (gapMin + gapMax - noticeWidth) / 2f;
            float y = Margin;
            if (noticeWidth < 184f)
            {
                noticeWidth = Mathf.Min(286f, width - Margin * 2f);
                x = (width - noticeWidth) / 2f;
                y = Mathf.Max(player.yMax, arena.yMax) + 9f;
            }

            var rect = new Rect(x, y, noticeWidth, 38f);
            Panel(rect);
            Label(new Rect(rect.x + 8f, rect.y, rect.width - 16f, rect.height), notice, 14, Amber,
                TextAnchor.MiddleCenter, bold: true);
        }

        private static void Panel(Rect rect)
        {
            Fill(new Rect(rect.x + 2f, rect.y + 4f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.23f));
            Fill(rect, Edge);
            Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), Surface);
        }

        private static void Fill(Rect rect, Color color)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void Label(Rect rect, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false)
        {
            _label.fontSize = size;
            _label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            _label.alignment = alignment;
            _label.normal.textColor = color;
            GUI.Label(rect, text, _label);
        }
    }
}
