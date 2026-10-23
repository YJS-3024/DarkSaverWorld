using DarkSaver.Combat;
using DarkSaver.Maps;
using UnityEngine;

namespace DarkSaver.Core
{
    public sealed class DarkSaverGame : MonoBehaviour
    {
        private const float ReferenceWidth = 640f;
        private const float ReferenceHeight = 480f;
        private readonly CombatWorld world = new();
        private CombatFlow combat;
        private string status;
        private string mapTitle = "DARK SAVER";

        private void Awake()
        {
            world.Add(new CombatUnit
            {
                Id = 100, Name = "용병대장", X = 2, Y = 5, Hp = 120, MaxHp = 120,
                Mp = 30, MaxMp = 30, Attack = 26, Defense = 7
            });
            world.Add(new CombatUnit
            {
                Id = 800, Name = "감옥 수비병", Enemy = true, X = 6, Y = 3,
                Hp = 75, MaxHp = 75, Mp = 20, MaxMp = 20, Attack = 18, Defense = 5
            });
            combat = new CombatFlow(world, new LocalCombatAuthority());
            combat.ActionStarted += request => status = $"행동 시작: {request.Action}";
            combat.ResultApplied += result =>
                status = $"결과 적용: 피해 {result.Damage}, 대상 HP {result.AbsoluteHp}";
            var definition = FindAnyObjectByType<MapSceneDefinition>();
            mapTitle = definition != null && !string.IsNullOrWhiteSpace(definition.DisplayName)
                ? $"{definition.MapKey}  {definition.DisplayName}"
                : "DARK SAVER";
            status = "구형 시연 씬입니다. Unity 타일맵으로 재구성되기 전까지 실제 맵을 표시하지 않습니다.";
        }

        private void OnGUI()
        {
            var oldMatrix = GUI.matrix;
            GUI.matrix = Rendering.GameScreen.GuiMatrix;
            GUI.color = new Color(.07f, .08f, .07f);
            GUI.DrawTexture(new Rect(0, 0, ReferenceWidth, ReferenceHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawBoard(new Rect(16, 48, 416, 352));
            DrawPanel(new Rect(448, 48, 176, 352));
            GUI.Label(new Rect(16, 12, 608, 28), $"DARK SAVER — {mapTitle}");
            GUI.Label(new Rect(16, 420, 608, 44), status ?? string.Empty);
            GUI.matrix = oldMatrix;
        }

        private void DrawBoard(Rect rect)
        {
            GUI.Box(rect, GUIContent.none);
            const int columns = 10;
            const int rows = 8;
            var width = rect.width / columns;
            var height = rect.height / rows;
            for (var y = 0; y < rows; y++)
            for (var x = 0; x < columns; x++)
            {
                GUI.color = ((x + y) & 1) == 0 ? new Color(.24f, .25f, .19f) : new Color(.19f, .20f, .15f);
                GUI.DrawTexture(new Rect(rect.x + x * width + 1, rect.y + y * height + 1, width - 2, height - 2),
                    Texture2D.whiteTexture);
            }

            foreach (var unit in world.Units) DrawUnit(rect, width, height, unit);
            GUI.color = Color.white;
        }

        private static void DrawUnit(Rect board, float cellWidth, float cellHeight, CombatUnit unit)
        {
            if (unit.LifeState == UnitLifeState.Removed) return;
            var rect = new Rect(board.x + unit.X * cellWidth + 6, board.y + unit.Y * cellHeight + 6,
                cellWidth - 12, cellHeight - 12);
            GUI.color = unit.LifeState == UnitLifeState.Corpse ? new Color(.35f, .35f, .35f) :
                unit.Enemy ? new Color(.72f, .16f, .12f) : new Color(.18f, .48f, .88f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, unit.LifeState == UnitLifeState.Corpse ? "시체" : unit.Enemy ? "적" : "대장");
        }

        private void DrawPanel(Rect rect)
        {
            GUI.Box(rect, GUIContent.none);
            var actor = world.Get(100);
            var target = world.Get(800);
            GUI.Label(new Rect(rect.x + 10, rect.y + 12, 156, 100),
                $"{actor.Name}\nHP {actor.Hp}/{actor.MaxHp}\nMP {actor.Mp}/{actor.MaxMp}\n\n{target.Name}\nHP {target.Hp}/{target.MaxHp}");
            GUI.enabled = actor.Alive && target.Alive;
            if (GUI.Button(new Rect(rect.x + 10, rect.y + 190, 156, 38), "일반 공격"))
                combat.Execute(new CombatRequest(actor.Id, target.Id, CombatActionType.Attack));
            GUI.enabled = actor.Alive && target.Alive && actor.Mp >= 10;
            if (GUI.Button(new Rect(rect.x + 10, rect.y + 238, 156, 38), "마법"))
                combat.Execute(new CombatRequest(actor.Id, target.Id, CombatActionType.Magic, 101));
            GUI.enabled = true;
            if (GUI.Button(new Rect(rect.x + 10, rect.y + 300, 156, 30), "재시작"))
            {
                actor.Hp = actor.MaxHp;
                actor.Mp = actor.MaxMp;
                actor.LifeState = UnitLifeState.Active;
                target.Hp = target.MaxHp;
                target.LifeState = UnitLifeState.Active;
                status = "전투 상태를 초기화했습니다.";
            }
        }
    }
}
