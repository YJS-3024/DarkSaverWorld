using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Prototype
{
    public sealed class DarkSaverPrototype : MonoBehaviour
    {
        private enum Mode { Title, Field, Battle, Result }
        private enum Command { None, Move, Attack, Skill, Magic }

        private const int Columns = 10;
        private const int Rows = 8;
        private const float ReferenceWidth = 960f;
        private const float ReferenceHeight = 540f;

        private readonly List<BattleUnit> enemies = new List<BattleUnit>();
        private readonly List<BattleUnit> party = new List<BattleUnit>();
        private readonly HashSet<Vector2Int> obstacles = new HashSet<Vector2Int>
        {
            new Vector2Int(4, 1), new Vector2Int(4, 2),
            new Vector2Int(4, 5), new Vector2Int(5, 5),
            new Vector2Int(7, 4)
        };
        private readonly Vector2Int[] fieldMonsters =
        {
            new Vector2Int(7, 2), new Vector2Int(10, 5), new Vector2Int(4, 7)
        };

        private Mode mode = Mode.Title;
        private Command command;
        private BattleUnit selectedUnit;
        private Vector2Int fieldPlayer = new Vector2Int(2, 5);
        private Vector2Int fieldDestination;
        private bool hasFieldDestination;
        private float nextFieldStepAt;
        private int defeated;
        private int gold;
        private int lastRewardGold;
        private int lastRewardExperience;
        private int healingPotionCount;
        private string message = "카오시아의 용병대가 새로운 의뢰를 기다립니다.";
        private bool victory;
        private float nextEnemyActionAt;

        private GUIStyle panel;
        private GUIStyle title;
        private GUIStyle body;
        private GUIStyle centered;
        private GUIStyle button;
        private GUIStyle tiny;
        private Texture2D panelTexture;
        private Texture2D buttonTexture;
        private Texture2D selectedTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<DarkSaverPrototype>() != null)
                return;

            var root = new GameObject("DarkSaver Prototype");
            DontDestroyOnLoad(root);
            root.AddComponent<DarkSaverPrototype>();
        }

        private void Update()
        {
            if (mode == Mode.Field)
                UpdateFieldInput();

            if (mode == Mode.Battle)
            {
                RechargeActionPoints(Time.unscaledDeltaTime);
                if (Time.unscaledTime >= nextEnemyActionAt)
                {
                    nextEnemyActionAt = Time.unscaledTime + .55f;
                    RunEnemyActions();
                }
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - ReferenceWidth * scale) * .5f,
                    (Screen.height - ReferenceHeight * scale) * .5f, 0f),
                Quaternion.identity, new Vector3(scale, scale, 1f));

            DrawBackdrop();
            switch (mode)
            {
                case Mode.Title: DrawTitle(); break;
                case Mode.Field: DrawField(); break;
                case Mode.Battle: DrawBattle(); break;
                case Mode.Result: DrawResult(); break;
            }

            GUI.matrix = old;
        }

        private void DrawBackdrop()
        {
            GUI.color = new Color(.06f, .075f, .07f);
            GUI.DrawTexture(new Rect(0, 0, ReferenceWidth, ReferenceHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawTitle()
        {
            GUI.color = new Color(.12f, .18f, .13f);
            GUI.DrawTexture(new Rect(0, 0, 960, 540), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(170, 90, 620, 80), "DARK SAVER WORLD", title);
            GUI.Label(new Rect(220, 165, 520, 36), "고전 온라인 SRPG 프로토타입", centered);
            GUI.Box(new Rect(280, 230, 400, 170), GUIContent.none, panel);
            GUI.Label(new Rect(310, 255, 340, 58),
                "카오시아 외곽에 출몰한 마물을 토벌하고\n용병대의 첫 임무를 완수하십시오.", centered);
            if (GUI.Button(new Rect(365, 330, 230, 48), "모험 시작", button))
                StartField();
            GUI.Label(new Rect(280, 458, 400, 28), "1990년대 PC RPG 화면 구성을 재해석한 독립 제작 프로토타입", tiny);
        }

        private void StartField()
        {
            mode = Mode.Field;
            fieldPlayer = new Vector2Int(2, 5);
            hasFieldDestination = false;
            defeated = 0;
            gold = 0;
            healingPotionCount = 3;
            InitializeParty();
            message = "방향키/WASD 또는 목적지 타일 클릭으로 이동할 수 있습니다.";
        }

        private void DrawField()
        {
            const float tile = 48f;
            var origin = new Vector2(92, 54);
            for (var y = 0; y < 9; y++)
            for (var x = 0; x < 13; x++)
            {
                var color = ((x + y) & 1) == 0
                    ? new Color(.20f, .34f, .20f)
                    : new Color(.17f, .29f, .18f);
                if (x == 0 || y == 0 || x == 12 || y == 8)
                    color = new Color(.16f, .20f, .13f);
                DrawCell(new Rect(origin.x + x * tile, origin.y + y * tile, tile - 1, tile - 1), color, "");
            }

            HandleFieldMouseInput(origin, tile);

            if (hasFieldDestination)
            {
                var destinationRect = new Rect(
                    origin.x + fieldDestination.x * tile + 4,
                    origin.y + fieldDestination.y * tile + 4,
                    tile - 9, tile - 9);
                GUI.color = new Color(1f, .78f, .18f, .38f);
                GUI.DrawTexture(destinationRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            GUI.Label(new Rect(105, 65, 260, 30), "엔트리아 외곽", title);
            DrawToken(origin, fieldPlayer, tile, new Color(.22f, .55f, .95f), "대");
            for (var i = 0; i < fieldMonsters.Length; i++)
            {
                if (i < defeated)
                    continue;
                DrawToken(origin, fieldMonsters[i], tile, new Color(.78f, .18f, .12f), "魔");
            }

            GUI.Box(new Rect(734, 54, 190, 432), GUIContent.none, panel);
            GUI.Label(new Rect(752, 70, 154, 30), "원정 정보", title);
            GUI.Label(new Rect(754, 115, 150, 120),
                $"지역  엔트리아\n단계  1단\n토벌  {defeated} / 3\n소지금  {gold} G\n\n이동\n방향키 / WASD\n또는 타일 클릭", body);
            GUI.Label(new Rect(754, 280, 150, 100), "심벌 엔카운트\n마물과 닿으면\n전술 전투 진입", centered);
            if (GUI.Button(new Rect(760, 420, 138, 40), "타이틀", button))
                mode = Mode.Title;
            DrawMessage();
        }

        private void UpdateFieldInput()
        {
            var delta = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) delta.x = -1;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) delta.x = 1;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) delta.y = -1;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) delta.y = 1;

            if (delta != Vector2Int.zero)
            {
                hasFieldDestination = false;
                MoveFieldPlayer(delta);
                return;
            }

            if (!hasFieldDestination || Time.unscaledTime < nextFieldStepAt)
                return;

            if (fieldPlayer == fieldDestination)
            {
                hasFieldDestination = false;
                return;
            }

            var remaining = fieldDestination - fieldPlayer;
            var step = Mathf.Abs(remaining.x) >= Mathf.Abs(remaining.y)
                ? new Vector2Int(remaining.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, remaining.y > 0 ? 1 : -1);
            MoveFieldPlayer(step);
            nextFieldStepAt = Time.unscaledTime + .12f;
        }

        private void HandleFieldMouseInput(Vector2 origin, float tile)
        {
            var current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0)
                return;

            var board = new Rect(origin.x + tile, origin.y + tile, tile * 11, tile * 7);
            if (!board.Contains(current.mousePosition))
                return;

            var local = current.mousePosition - origin;
            fieldDestination = new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(local.x / tile), 1, 11),
                Mathf.Clamp(Mathf.FloorToInt(local.y / tile), 1, 7));
            hasFieldDestination = fieldDestination != fieldPlayer;
            nextFieldStepAt = Time.unscaledTime;
            message = hasFieldDestination
                ? $"목적지 ({fieldDestination.x}, {fieldDestination.y})로 이동합니다."
                : "현재 위치입니다.";
            current.Use();
        }

        private void MoveFieldPlayer(Vector2Int delta)
        {
            fieldPlayer += delta;
            fieldPlayer.x = Mathf.Clamp(fieldPlayer.x, 1, 11);
            fieldPlayer.y = Mathf.Clamp(fieldPlayer.y, 1, 7);
            if (defeated < fieldMonsters.Length && fieldPlayer == fieldMonsters[defeated])
            {
                hasFieldDestination = false;
                StartBattle();
            }
        }

        private void StartBattle()
        {
            mode = Mode.Battle;
            command = Command.None;
            nextEnemyActionAt = Time.unscaledTime + .8f;
            if (party.Count == 0) InitializeParty();
            party[0].Cell = new Vector2Int(1, 4);
            party[1].Cell = new Vector2Int(1, 5);
            party[2].Cell = new Vector2Int(2, 4);
            foreach (var member in party)
            {
                if (!member.Alive) member.Hp = Mathf.Max(1, member.MaxHp / 2);
                member.Ap = member.MaxAp;
                member.ActionCharge = 0f;
            }
            selectedUnit = party[0].Alive ? party[0] : ClosestLivingPartyMember(Vector2Int.zero);
            enemies.Clear();
            enemies.AddRange(BattleRoster.CreateEnemies());
            message = "아군을 클릭해 선택한 뒤 명령과 대상 타일을 선택하세요.";
        }

        private void InitializeParty()
        {
            party.Clear();
            party.AddRange(BattleRoster.CreateParty());
            selectedUnit = party[0];
        }

        private void DrawBattle()
        {
            var board = new Rect(42, 64, 650, 416);
            GUI.Box(new Rect(24, 45, 686, 454), GUIContent.none, panel);
            var cellW = board.width / Columns;
            var cellH = board.height / Rows;

            for (var y = 0; y < Rows; y++)
            for (var x = 0; x < Columns; x++)
            {
                var cell = new Vector2Int(x, y);
                var valid = IsValidTarget(cell);
                var rect = new Rect(board.x + x * cellW, board.y + y * cellH, cellW - 1, cellH - 1);
                var color = ((x + y) & 1) == 0 ? new Color(.28f, .25f, .19f) : new Color(.23f, .21f, .17f);
                if (obstacles.Contains(cell)) color = new Color(.13f, .12f, .10f);
                if (valid) color = command == Command.Move ? new Color(.16f, .40f, .55f) : new Color(.55f, .22f, .13f);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    if (valid)
                        ExecuteCommand(cell);
                    else
                    {
                        var clicked = UnitAt(cell);
                        if (clicked != null && !clicked.Enemy && clicked.Alive)
                            SelectPartyUnit(clicked);
                    }
                }
                GUI.color = color;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (obstacles.Contains(cell)) GUI.Label(rect, "바위", tiny);
            }

            foreach (var member in party)
                if (member.Alive)
                    DrawBattleToken(board, cellW, cellH, member, member.Color, member.Mark);
            foreach (var enemy in enemies)
                if (enemy.Alive) DrawBattleToken(board, cellW, cellH, enemy, new Color(.78f, .18f, .12f), "적");

            GUI.Box(new Rect(728, 45, 208, 454), GUIContent.none, panel);
            GUI.Label(new Rect(744, 59, 176, 30), "실시간 전투", title);
            GUI.Label(new Rect(750, 88, 160, 18), $"Lv.{selectedUnit.Level}  {selectedUnit.Name}", tiny);
            DrawBar(new Rect(750, 131, 160, 14), "HP", selectedUnit.Hp, selectedUnit.MaxHp, new Color(.75f, .12f, .12f));
            DrawBar(new Rect(750, 163, 160, 14), "MP", selectedUnit.Mp, selectedUnit.MaxMp, new Color(.12f, .35f, .8f));
            DrawBar(new Rect(750, 195, 160, 14), "AP", selectedUnit.Ap, selectedUnit.MaxAp, new Color(.90f, .65f, .12f));

            GUI.Label(new Rect(746, 216, 170, 26), "행동 명령", title);
            CommandButton(new Rect(750, 250, 74, 38), "이동 4", Command.Move,
                selectedUnit.Ap >= BattleRules.MoveActionPointCost);
            CommandButton(new Rect(836, 250, 74, 38), "공격 2", Command.Attack,
                selectedUnit.Ap >= BattleRules.AttackActionPointCost);
            CommandButton(new Rect(750, 294, 74, 38), "스킬 5", Command.Skill,
                selectedUnit.CanUseSkill && selectedUnit.Ap >= BattleRules.SkillActionPointCost);
            CommandButton(new Rect(836, 294, 74, 38), "마법 6", Command.Magic,
                selectedUnit.CanMagic && selectedUnit.Ap >= BattleRules.MagicActionPointCost &&
                selectedUnit.Mp >= BattleRules.MagicManaCost);
            var oldEnabled = GUI.enabled;
            GUI.enabled = healingPotionCount > 0 && selectedUnit.Hp < selectedUnit.MaxHp &&
                selectedUnit.Ap >= BattleRules.ItemActionPointCost;
            if (GUI.Button(new Rect(750, 338, 74, 38), $"물약 {healingPotionCount}", button))
                UseHealingPotion();
            GUI.enabled = selectedUnit.Ap >= BattleRules.RestActionPointCost;
            if (GUI.Button(new Rect(836, 338, 74, 38), "휴식 4", button))
            {
                selectedUnit.Ap -= BattleRules.RestActionPointCost;
                selectedUnit.Hp = BattleRules.ApplyRecovery(
                    selectedUnit.Hp, selectedUnit.MaxHp, BattleRules.RestHealthAmount);
                selectedUnit.Mp = BattleRules.ApplyRecovery(
                    selectedUnit.Mp, selectedUnit.MaxMp, BattleRules.RestManaAmount);
                message = $"{selectedUnit.Name}이 휴식으로 체력과 마력을 회복했습니다.";
            }
            GUI.enabled = oldEnabled;
            if (GUI.Button(new Rect(750, 382, 160, 32), "명령 취소", button)) command = Command.None;
            GUI.Label(new Rect(750, 420, 160, 58),
                $"EXP {selectedUnit.Experience} / {selectedUnit.NextLevelExperience}\n" +
                $"공격 {selectedUnit.Attack}  방어 {selectedUnit.Defense}\n" +
                $"남은 적 {AliveEnemyCount()}  {CommandName(command)}", centered);
            DrawMessage();
        }

        private void CommandButton(Rect rect, string label, Command value, bool enabled)
        {
            var oldEnabled = GUI.enabled;
            var oldBackground = button.normal.background;
            GUI.enabled = enabled;
            if (command == value) button.normal.background = selectedTexture;
            if (GUI.Button(rect, label, button))
            {
                command = value;
                message = value == Command.Move ? "2칸 안의 빈 타일을 선택하세요." :
                    value == Command.Attack ? "인접한 적을 선택하세요." :
                    value == Command.Skill ? $"인접한 적에게 {selectedUnit.SkillName}을 사용합니다." :
                    "3칸 안의 적에게 화염 마법을 사용합니다.";
            }
            button.normal.background = oldBackground;
            GUI.enabled = oldEnabled;
        }

        private void UseHealingPotion()
        {
            if (healingPotionCount <= 0 || selectedUnit == null ||
                selectedUnit.Hp >= selectedUnit.MaxHp ||
                !BattleRules.CanAfford(selectedUnit.Ap, BattleRules.ItemActionPointCost))
                return;

            selectedUnit.Ap -= BattleRules.ItemActionPointCost;
            selectedUnit.Hp = BattleRules.ApplyRecovery(
                selectedUnit.Hp, selectedUnit.MaxHp, BattleRules.HealingPotionAmount);
            healingPotionCount--;
            command = Command.None;
            message = $"{selectedUnit.Name}이 회복 물약을 사용했습니다.";
        }

        private void SelectPartyUnit(BattleUnit member)
        {
            selectedUnit = member;
            command = Command.None;
            message = $"{member.Name}을 선택했습니다.";
        }

        private bool IsValidTarget(Vector2Int cell)
        {
            if (selectedUnit == null || !selectedUnit.Alive || command == Command.None) return false;
            var distance = Manhattan(selectedUnit.Cell, cell);
            if (command == Command.Move) return CanReach(selectedUnit.Cell, cell, 2);
            var target = UnitAt(cell);
            if (target == null || !target.Enemy || !target.Alive) return false;
            return command == Command.Magic ? distance <= 3 : distance == 1;
        }

        private void ExecuteCommand(Vector2Int cell)
        {
            if (command == Command.Move)
            {
                selectedUnit.Cell = cell;
                selectedUnit.Ap -= BattleRules.MoveActionPointCost;
                message = $"{selectedUnit.Name}이 진형을 이동했습니다.";
            }
            else
            {
                var target = UnitAt(cell);
                if (target == null) return;
                var magic = command == Command.Magic;
                var skill = command == Command.Skill;
                var rawDamage = magic ? BattleRules.MagicDamage :
                    skill ? selectedUnit.Attack + selectedUnit.SkillPower : selectedUnit.Attack;
                var damage = BattleRules.CalculateDamage(rawDamage, target.Defense, magic);
                target.Hp = Mathf.Max(0, target.Hp - damage);
                selectedUnit.Ap -= magic ? BattleRules.MagicActionPointCost :
                    skill ? BattleRules.SkillActionPointCost : BattleRules.AttackActionPointCost;
                if (magic) selectedUnit.Mp -= BattleRules.MagicManaCost;
                var actionName = skill ? selectedUnit.SkillName : magic ? "화염 마법" : "공격";
                message = $"{selectedUnit.Name}의 {actionName}: {target.Name}에게 {damage} 피해!" +
                    (target.Alive ? "" : "  격파했습니다.");
            }
            command = Command.None;
            if (AliveEnemyCount() == 0)
            {
                defeated++;
                AwardVictory();
                victory = true;
                mode = Mode.Result;
                message = "전투에서 승리했습니다.";
                return;
            }
        }

        private void RunEnemyActions()
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.Alive) continue;
                var target = ClosestLivingPartyMember(enemy.Cell);
                if (target == null)
                    break;
                var distance = Manhattan(enemy.Cell, target.Cell);
                if (distance == 1 && enemy.Ap >= BattleRules.AttackActionPointCost)
                {
                    enemy.Ap -= BattleRules.AttackActionPointCost;
                    var damage = BattleRules.CalculateDamage(enemy.Attack, target.Defense, false);
                    target.Hp = Mathf.Max(0, target.Hp - damage);
                    message = $"{enemy.Name}의 반격! {target.Name}이 {damage} 피해를 받았습니다.";
                }
                else if (distance > 1 && enemy.Ap >= BattleRules.MoveActionPointCost)
                {
                    enemy.Ap -= BattleRules.MoveActionPointCost;
                    var destination = FindNextStep(enemy.Cell, target.Cell);
                    if (destination != enemy.Cell) enemy.Cell = destination;
                }
            }
            if (LivingPartyCount() == 0)
            {
                victory = false;
                mode = Mode.Result;
                message = "용병대가 전투 불능 상태가 되었습니다.";
            }
            else if (selectedUnit == null || !selectedUnit.Alive)
            {
                selectedUnit = ClosestLivingPartyMember(Vector2Int.zero);
                command = Command.None;
            }
        }

        private void RechargeActionPoints(float deltaTime)
        {
            RechargeGroup(party, deltaTime, 1.15f);
            RechargeGroup(enemies, deltaTime, .85f);
        }

        private static void RechargeGroup(List<BattleUnit> units, float deltaTime, float rate)
        {
            foreach (var unit in units)
            {
                if (!unit.Alive || unit.Ap >= unit.MaxAp) continue;
                var totalCharge = unit.ActionCharge + Mathf.Max(0f, deltaTime) * Mathf.Max(0f, rate);
                var recovered = BattleRules.CalculateRecoveredActionPoints(
                    unit.ActionCharge, deltaTime, rate);
                if (recovered <= 0) continue;
                unit.Ap = Mathf.Min(unit.MaxAp, unit.Ap + recovered);
                unit.ActionCharge = totalCharge - recovered;
            }
        }

        private void DrawResult()
        {
            GUI.Box(new Rect(225, 92, 510, 350), GUIContent.none, panel);
            GUI.Label(new Rect(280, 125, 400, 58), victory ? "전투 승리" : "전투 패배", title);
            GUI.Label(new Rect(300, 205, 360, 70), victory
                ? $"마물 심벌을 격파했습니다.\nEXP +{lastRewardExperience}   {lastRewardGold} G\n원정 진척도 {defeated} / 3"
                : "진형과 행동력을 정비한 뒤\n다시 도전하십시오.", centered);
            if (GUI.Button(new Rect(325, 315, 140, 46), victory ? "필드 복귀" : "다시 도전", button))
            {
                if (victory)
                {
                    if (defeated >= fieldMonsters.Length)
                    {
                        defeated = 0;
                        fieldPlayer = new Vector2Int(2, 5);
                        message = "토벌 임무 완료! 새로운 원정을 시작할 수 있습니다.";
                    }
                    mode = Mode.Field;
                }
                else
                {
                    RecoverParty(true);
                    StartBattle();
                }
            }
            if (GUI.Button(new Rect(495, 315, 140, 46), "타이틀", button)) mode = Mode.Title;
        }

        private void AwardVictory()
        {
            lastRewardExperience = 35 + defeated * 5;
            lastRewardGold = 45 + defeated * 10;
            gold += lastRewardGold;

            foreach (var member in party)
            {
                member.Experience += lastRewardExperience;
                while (member.Experience >= member.NextLevelExperience)
                {
                    member.Experience -= member.NextLevelExperience;
                    member.Level++;
                    member.MaxHp += member.CanMagic ? 8 : 14;
                    member.MaxMp += member.CanMagic ? 10 : 4;
                    member.Attack += member.CanMagic ? 3 : 4;
                    member.Defense += member.CanMagic ? 1 : 2;
                }

                if (!member.Alive) member.Hp = Mathf.Max(1, member.MaxHp / 2);
            }
        }

        private void RecoverParty(bool full)
        {
            foreach (var member in party)
            {
                member.Hp = full ? member.MaxHp : Mathf.Max(member.Hp, member.MaxHp / 2);
                member.Mp = full ? member.MaxMp : Mathf.Max(member.Mp, member.MaxMp / 2);
                member.Ap = member.MaxAp;
            }
        }

        private void DrawMessage()
        {
            GUI.Box(new Rect(24, 505, 912, 28), GUIContent.none, panel);
            GUI.Label(new Rect(36, 507, 888, 24), message, tiny);
        }

        private void DrawBattleToken(Rect board, float cellW, float cellH, BattleUnit unit, Color color, string mark)
        {
            var rect = new Rect(board.x + unit.Cell.x * cellW + 8, board.y + unit.Cell.y * cellH + 7, cellW - 16, cellH - 14);
            if (unit == selectedUnit)
            {
                GUI.color = new Color(1f, .82f, .22f);
                GUI.DrawTexture(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8), Texture2D.whiteTexture);
            }
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, $"{mark}\nAP {unit.Ap}", tiny);
            if (unit.Enemy)
            {
                GUI.color = new Color(.08f, .08f, .08f);
                GUI.DrawTexture(new Rect(rect.x, rect.y - 5, rect.width, 4), Texture2D.whiteTexture);
                GUI.color = new Color(.82f, .12f, .1f);
                GUI.DrawTexture(new Rect(rect.x, rect.y - 5, rect.width * unit.Hp / unit.MaxHp, 4), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        private void DrawToken(Vector2 origin, Vector2Int cell, float size, Color color, string mark)
        {
            var rect = new Rect(origin.x + cell.x * size + 8, origin.y + cell.y * size + 8, size - 16, size - 16);
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, mark, centered);
        }

        private static void DrawCell(Rect rect, Color color, string mark)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (!string.IsNullOrEmpty(mark)) GUI.Label(rect, mark);
        }

        private void DrawBar(Rect rect, string label, int value, int max, Color color)
        {
            GUI.Label(new Rect(rect.x, rect.y - 20, rect.width, 18), $"{label}  {value} / {max}", tiny);
            GUI.color = new Color(.06f, .06f, .06f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01((float)value / max), rect.height - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private bool CanReach(Vector2Int start, Vector2Int destination, int range)
        {
            return BattleGridRules.CanReach(
                start, destination, range, Columns, Rows, obstacles, cell => UnitAt(cell) != null);
        }

        private Vector2Int FindNextStep(Vector2Int start, Vector2Int target)
        {
            return BattleGridRules.FindNextStep(
                start, target, Columns, Rows, obstacles, cell => UnitAt(cell) != null);
        }

        private BattleUnit UnitAt(Vector2Int cell)
        {
            foreach (var member in party) if (member.Alive && member.Cell == cell) return member;
            foreach (var enemy in enemies) if (enemy.Alive && enemy.Cell == cell) return enemy;
            return null;
        }

        private BattleUnit ClosestLivingPartyMember(Vector2Int origin)
        {
            BattleUnit closest = null;
            var bestDistance = int.MaxValue;
            foreach (var member in party)
            {
                if (!member.Alive) continue;
                var distance = Manhattan(origin, member.Cell);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                closest = member;
            }
            return closest;
        }

        private int LivingPartyCount()
        {
            var count = 0;
            foreach (var member in party) if (member.Alive) count++;
            return count;
        }

        private int AliveEnemyCount()
        {
            var count = 0;
            foreach (var enemy in enemies) if (enemy.Alive) count++;
            return count;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private static string CommandName(Command value)
        {
            switch (value)
            {
                case Command.Move: return "이동";
                case Command.Attack: return "공격";
                case Command.Skill: return "스킬";
                case Command.Magic: return "마법";
                default: return "대기";
            }
        }

        private void EnsureStyles()
        {
            if (panel != null) return;
            panelTexture = Solid(new Color(.09f, .085f, .065f, .96f));
            buttonTexture = Solid(new Color(.30f, .23f, .12f));
            selectedTexture = Solid(new Color(.58f, .37f, .10f));
            panel = new GUIStyle(GUI.skin.box) { normal = { background = panelTexture } };
            title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.96f, .77f, .35f) } };
            body = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(.9f, .88f, .78f) } };
            centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter };
            tiny = new GUIStyle(centered) { fontSize = 12 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { background = buttonTexture, textColor = Color.white }, hover = { textColor = new Color(1f, .82f, .3f) } };
        }

        private static Texture2D Solid(Color color)
        {
            var result = new Texture2D(1, 1);
            result.SetPixel(0, 0, color);
            result.Apply();
            return result;
        }

        private void OnDestroy()
        {
            if (panelTexture != null) Destroy(panelTexture);
            if (buttonTexture != null) Destroy(buttonTexture);
            if (selectedTexture != null) Destroy(selectedTexture);
        }
    }
}
