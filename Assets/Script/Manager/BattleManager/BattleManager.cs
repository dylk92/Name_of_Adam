using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

// 전투를 담당하는 매니저
// 필드와 턴의 관리
// 필드에 올라와있는 캐릭터의 제어를 배틀매니저에서 담당

public class BattleManager : MonoBehaviour
{
    private static BattleManager s_instance;
    public static BattleManager Instance { get { Init(); return s_instance; } }

    private SoundManager _sound;
    public static SoundManager Sound => Instance._sound;

    [SerializeField] BattleCutSceneController _battlecutScene;
    public static BattleCutSceneController BattleCutScene => Instance._battlecutScene;

    [SerializeField] UnitSpawner _spawner;
    public static UnitSpawner Spawner => Instance._spawner;

    private BattleDataManager _battleData;
    public static BattleDataManager Data => Instance._battleData;

    private BattleUIManager _battleUI;
    public static BattleUIManager BattleUI => Instance._battleUI;

    private PlayerSkillController _playerSkillController;
    public static PlayerSkillController PlayerSkillController => Instance._playerSkillController;

    private Field _field;
    public static Field Field => Instance._field;

    private Mana _mana;
    public static Mana Mana => Instance._mana;

    private PhaseController _phase;
    public static PhaseController Phase => Instance._phase;

    [SerializeField] List<GameObject> Background;

    private void Awake()
    {
        _battleData = Util.GetOrAddComponent<BattleDataManager>(gameObject);
        _battleUI = Util.GetOrAddComponent<BattleUIManager>(gameObject);
        _mana = Util.GetOrAddComponent<Mana>(gameObject);
        _phase = new PhaseController();
        _playerSkillController = Util.GetOrAddComponent<PlayerSkillController>(gameObject);

        SetBackground();
    }

    private void Update()
    {
        _phase.OnUpdate();
    }

    private static void Init()
    {
        if (s_instance == null)
        {
            GameObject go = GameObject.Find("@BattleManager");

            if (go == null)
            {
                //go = new GameObject("@BattleManager");
                //go.AddComponent<BattleManager>();
                return;
            }

            s_instance = go.GetComponent<BattleManager>();
        }
    }

    public void TurnStart()
    {
        //턴 시작 시 체크
        foreach (BattleUnit unit in _battleData.BattleUnitList)
        {
            ActiveTimingCheck(ActiveTiming.TURN_START, unit);
        }
    }

    public void TurnEnd()
    {
        foreach (BattleUnit unit in _battleData.BattleUnitList)
        {
            //턴 종료 시 체크
            ActiveTimingCheck(ActiveTiming.TURN_END, unit);
        }
    }

    public void SetupField()
    {
        GameObject fieldObject = GameObject.Find("Field");

        if (fieldObject == null)
            fieldObject = GameManager.Resource.Instantiate("Field");

        _field = fieldObject.GetComponent<Field>();
    }

    public void SpawnInitialUnit()
    {
        if (SceneChanger.GetSceneName() == "BattleTestScene")
            return;

        PlayAfterCoroutine(() => {
            _spawner.SpawnInitialUnit();
        }, 0.5f);

        PlayAfterCoroutine(() => {
            if (GameManager.Data.Map.GetCurrentStage().StageLevel == 10)
            {
                if(GameManager.Data.Map.GetCurrentStage().StageID == 0)
                {
                    string scriptKey = "엘리우스_야나전_입장";

                    EventConversation(scriptKey);
                }
                else if (GameManager.Data.Map.GetCurrentStage().StageID == 1)
                {
                    string scriptKey = "투발카인전_입장";

                    EventConversation(scriptKey);
                }
            }
            else if (GameManager.Data.Map.GetCurrentStage().StageLevel == 20)
            {
                if (GameManager.Data.Map.GetCurrentStage().StageID == 0)
                {
                    string scriptKey = "니므롯전_입장";

                    EventConversation(scriptKey);
                }
            }
            else
            {
                _phase.ChangePhase(_phase.Prepare);
            }
        }, 1f);
    }

    private void EventConversation(string scriptKey)
    {
        List<Script> scripts = GameManager.Data.ScriptData[scriptKey];
        GameManager.UI.ShowPopup<UI_Conversation>().Init(scripts, true, true);
    }

    private void SetBackground()
    {
        // string str = GameManager.Data.CurrentStageData.FactionName;

        for (int i = 0; i < 3; i++)
        {
            Background[i].SetActive(false);
            
            // if (((Faction)i + 1).ToString() == str)
            if (i == 0)
                Background[i].SetActive(true);
        }
    }

    #region Click 관련
    public void OnClickTile(Tile tile)
    {
        Vector2 coord = _field.GetCoordByTile(tile);
        _phase.OnClickEvent(coord);
    }

    public void PreparePhaseClick(Vector2 coord)
    {
        if (!_field.TileDict[coord].IsColored)
        {
            if (!PlayerSkillController.IsSkillOn)
                _battleUI.CancelAllSelect();
            return;
        }

        if (PlayerSkillController.IsSkillOn)
        {
            PlayerSkillController.ActionSkill(ActiveTiming.TURN_START, coord);
            return;
        }

        if (_field.FieldType == FieldColorType.UnitSpawn)
        {
            SpawnUnitOnField(coord);
        }
        else if (_field.FieldType == FieldColorType.PlayerSkill)
        {
            _playerSkillController.PlayerSkillUse(coord);
        }
        else if (_field.FieldType == FieldColorType.UltimatePlayerSkill)
        {
            if (GameManager.Data.PlayerSkillCountChage(-1))
            {
                _playerSkillController.PlayerSkillUse(coord);
            }
        }
    }

    private void SpawnUnitOnField(Vector2 coord)
    {
        DeckUnit unit = _battleUI.UI_hands.GetSelectedUnit();
        if (!_field.TileDict[coord].IsColored)
            return;

        _mana.ChangeMana(-unit.DeckUnitTotalStat.ManaCost); //마나 사용가능 체크

        unit.FirstTurnDiscountUndo();

        _spawner.DeckSpawn(unit, coord);
        GameManager.VisualEffect.StartVisualEffect(
            Resources.Load<AnimationClip>("Arts/EffectAnimation/VisualEffect/UnitSpawnBackEffect"),
            _field.GetTilePosition(coord) + new Vector3(0f, 3.5f, 0f));
        GameManager.VisualEffect.StartVisualEffect(
            Resources.Load<AnimationClip>("Arts/EffectAnimation/VisualEffect/UnitSpawnFrontEffect"),
            _field.GetTilePosition(coord) + new Vector3(0f, 3.5f, 0f));

        _battleUI.RemoveHandUnit(unit); //유닛 리필
        GameManager.UI.ClosePopup();
        _field.ClearAllColor();
    }

    public void MovePhaseClick(Vector2 coord)
    {
        if (!_field.TileDict[coord].IsColored)
        {
            return;
        }

        BattleUnit unit = _battleData.GetNowUnit();
        foreach (ConnectedUnit connectUnit in unit.ConnectedUnits)
        {
            if (connectUnit.Location == coord)
                return;
        }

        if (MoveUnit(unit, coord))
            PlayAfterCoroutine(() =>_phase.ChangePhase(_phase.Action), 1f);
    }

    public void ActionPhaseClick(Vector2 coord)
    {
        if (!_field.TileDict[coord].IsColored)
            return;

        BattleUnit nowUnit = _battleData.GetNowUnit();
        BattleUnit selectUnit = _field.GetUnit(coord);
        if (selectUnit == null || selectUnit.Team == Team.Player)
            return;

        List<Vector2> attackCoords = new();
        List<BattleUnit> unitList = new();
        attackCoords.Add(coord);
        
        if (nowUnit.DeckUnit.CheckStigma(new Stigma_Additional_Punishment()))
        {
            List<BattleUnit> additionalEnemies = _field.GetUnitsInRange(nowUnit.Location, nowUnit.GetAttackRange(), Team.Enemy);
            additionalEnemies.Remove(selectUnit);
            if (additionalEnemies.Count > 0)
            {
                BattleUnit additionalEnemy = additionalEnemies[UnityEngine.Random.Range(0, additionalEnemies.Count)];
                attackCoords.Add(additionalEnemy.Location);
            }
        }

        foreach (var attackCoord in attackCoords)
        {
            if (_field.GetUnit(attackCoord) != nowUnit)
            {
                List<Vector2> splashRange = nowUnit.GetSplashRange(attackCoord, nowUnit.Location);
                
                foreach (Vector2 splash in splashRange)
                {
                    BattleUnit targetUnit = _field.GetUnit(attackCoord + splash);

                    if (targetUnit == null)
                        continue;

                    // 힐러의 예외처리 필요
                    if (targetUnit.Team != nowUnit.Team)
                        unitList.Add(targetUnit);
                }
            }
        }

        if (!nowUnit.Action.ActionStart(nowUnit, unitList, coord))
            return;
    }

    #endregion

    public void AttackStart(BattleUnit caster, BattleUnit hit)
    {
        List<BattleUnit> hits = new ();
        hits.Add(hit);

        AttackStart(caster, hits);
    }

    public void AttackStart(BattleUnit caster, List<BattleUnit> hits)
    {
        BattleCutSceneData CSData = new(caster, hits);
        _battlecutScene.InitBattleCutScene(CSData);
        caster.AttackUnitNum = hits.Count;

        StartCoroutine(_battlecutScene.AttackCutScene(CSData));
    }

    public void EndUnitAction()
    {
        _field.ClearAllColor();
        _battleData.BattleOrderRemove(Data.GetNowUnit());
        _battleUI.UI_darkEssence.Refresh();
        _phase.ChangePhase(_phase.Engage);
    }

    public void StigmaSelectEvent(Corruption cor)
    {
        BattleUnit targetUnit = cor.GetTargetUnit();

        if (targetUnit.Fall.IsEdified)
        {
            cor.LoopExit();
            targetUnit.DeckUnit.ClearStigma();
        }
        else
        {
            GameObject.Find("@UI_Root").transform.Find("UI_StigmaSelectBlocker").gameObject.SetActive(true);
            GameManager.UI.ShowPopup<UI_StigmaSelectButtonPopup>().Init(targetUnit.DeckUnit, null, 2, cor.LoopExit);
        }
    }

    public void DirectAttack()
    {
        //핸드에 있는 유닛을 하나 무작위로 제거하고 배틀 종료 체크
        Debug.Log("Direct Attack");

        if (_battleData.PlayerHands.Count == 0)
        {
            BattleOverCheck();
            return;
        }

        int randNum = UnityEngine.Random.Range(0, Data.PlayerHands.Count);
        _battleUI.RemoveHandUnit(Data.PlayerHands[randNum]);

        BattleOverCheck();
    }

    public void UnitDeadEvent(BattleUnit unit)
    {
        _battleData.BattleUnitList.Remove(unit);
        _field.ExitTile(unit.Location);

        if (unit.IsConnectedUnit)
            return;

        _battleData.BattleOrderRemove(unit);

        if (unit.Team == Team.Enemy && !unit.IsConnectedUnit)
        {
            if(unit.Data.Rarity == Rarity.Normal)
            {
                GameManager.Data.GameData.Progress.NormalKill++;
            }
            else if(unit.Data.Rarity == Rarity.Elite)
            {
                GameManager.Data.GameData.Progress.EliteKill++;
            }
            
            GameManager.Data.DarkEssenseChage(unit.Data.DarkEssenseDrop);
        }

        FieldActiveEventCheck(ActiveTiming.FIELD_UNIT_DEAD, unit);
    }

    public void FieldActiveEventCheck(ActiveTiming timing, BattleUnit parameterUnit = null)
    {
        List<BattleUnit> checkEndList = new();

        int startCount = _battleData.BattleUnitList.Count;

        for (int i = 0; i < _battleData.BattleUnitList.Count; i++)
        {
            if (startCount != _battleData.BattleUnitList.Count)
            {
                i = -1;
                startCount = _battleData.BattleUnitList.Count;
                continue;
            }

            if (checkEndList.Contains(_battleData.BattleUnitList[i]))
                continue;

            checkEndList.Add(_battleData.BattleUnitList[i]);
            ActiveTimingCheck(timing, _battleData.BattleUnitList[i], parameterUnit);
        }
    }

    public void BattleOverCheck()
    {
        if (SceneChanger.GetSceneName() == "BattleTestScene")
            return;

        int MyUnit = 0;
        int EnemyUnit = 0;

        foreach (BattleUnit unit in Data.BattleUnitList)
        {
            if (unit.Team == Team.Player)//아군이면
                MyUnit++;
            else
                EnemyUnit++;
        }

        MyUnit += _battleData.PlayerDeck.Count;
        MyUnit += _battleData.PlayerHands.Count;

        if (MyUnit == 0)
        {
            BattleOverLose();
        }
        else if (EnemyUnit == 0)
        {
            BattleOverWin();
            if (GameManager.Data.StageAct == 0 && GameManager.Data.Map.CurrentTileID == 3)
            {
                GameManager.OutGameData.DoneTutorial(true);
                Debug.Log("Tutorial Clear!");

                //튜토리얼 마지막 창 12/20 프로토타입에선 우선 제외
                /*
                if (GameManager.Data.Tutorial_Stage_Trigger == true)
                {
                    GameObject.Find("UI_Tutorial").GetComponent<UI_Tutorial>().TutorialActive(14);
                    GameManager.Data.Tutorial_Stage_Trigger = false;
                }
                */
            }
        }
    }

    private void BattleOverWin()
    {
        Debug.Log("YOU WIN");
        _battleData.OnBattleOver();
        _phase.ChangePhase(new BattleOverPhase());
        StageData data = GameManager.Data.Map.GetCurrentStage();

        if (data.StageLevel >= 10)
        {
            if (data.StageLevel == 20)
            {
                GameManager.Data.GameData.Progress.BossWin++;

                foreach (DeckUnit unit in Data.PlayerDeck)
                {
                    if (unit.Data.Rarity == Rarity.Boss)
                    {
                        GameManager.Data.GameData.Progress.SurvivedBoss++;
                    }
                    else if (unit.Data.Rarity == Rarity.Elite)
                    {
                        GameManager.Data.GameData.Progress.SurvivedElite++;
                    }
                    else
                    {
                        GameManager.Data.GameData.Progress.SurvivedNormal++;
                    }
                }

                GameManager.UI.ShowScene<UI_BattleOver>().SetImage("elite win");
                GameManager.SaveManager.DeleteSaveData();
            }
            else if (data.StageLevel == 10)
            {
                GameManager.Data.GameData.Progress.EliteWin++;
                GameManager.UI.ShowScene<UI_BattleOver>().SetImage("elite win");
            }
            else
            {
                GameManager.Data.GameData.Progress.NormalWin++;
                GameManager.UI.ShowScene<UI_BattleOver>().SetImage("win");
            }

            return;
        }
        else
        {
            GameManager.Data.GameData.Progress.NormalWin++;
            GameManager.UI.ShowScene<UI_BattleOver>().SetImage("win");
        }

        GameManager.OutGameData.SaveData();
        GameManager.SaveManager.SaveGame();
    }

    private void BattleOverLose()
    {
        Debug.Log("YOU LOSE");
        _phase.ChangePhase(new BattleOverPhase());
        GameManager.UI.ShowSingleScene<UI_BattleOver>().SetImage("lose");
        GameManager.SaveManager.DeleteSaveData();
    }

    // 이동 경로를 받아와 이동시킨다
    public bool MoveUnit(BattleUnit moveUnit, Vector2 dest, float moveSpeed = 1)
    {
        Vector2 current = moveUnit.Location;

        if (!_field.IsInRange(dest) || current == dest)
            return false;

        if (moveUnit.ConnectedUnits.Count > 0)
        {
            if (_field.GetUnit(dest) != null)
            {
                return false;
            }

            foreach (ConnectedUnit unit in moveUnit.ConnectedUnits)
            {
                Vector2 unitDest = unit.Location + dest - current;

                if (!_field.IsInRange(unitDest))
                    return false;

                if (_field.GetUnit(unitDest) != null && _field.GetUnit(unitDest).DeckUnit != moveUnit.DeckUnit)
                    return false;
            }

            _field.ExitTile(current);
            _field.EnterTile(moveUnit, dest);
            moveUnit.UnitMove(dest, moveSpeed);

            foreach (ConnectedUnit unit in moveUnit.ConnectedUnits)
            {
                MoveUnit(unit, unit.Location + dest - current);
            }
        }
        else
        {
            if (_field.TileDict[dest].UnitExist)
            {
                BattleUnit destUnit = _field.TileDict[dest].Unit;

                if (Switchable(moveUnit, destUnit))
                {
                    _field.ExitTile(current);
                    _field.ExitTile(dest);

                    moveUnit.UnitMove(dest, moveSpeed);
                    _field.EnterTile(moveUnit, dest);

                    destUnit.UnitMove(current, moveSpeed);
                    _field.EnterTile(destUnit, current);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                _field.ExitTile(current);
                _field.EnterTile(moveUnit, dest);
                moveUnit.UnitMove(dest, moveSpeed);
            }
        }

        GameManager.Sound.Play("Move/MoveSFX");
        return true;
    }

    private bool Switchable(BattleUnit moveUnit, BattleUnit destUnit) =>
        moveUnit.Team == destUnit.Team &&
        moveUnit.GetMoveRange().Contains(destUnit.Location - moveUnit.Location);

    public bool UnitSpawnReady(FieldColorType colorType, DeckUnit deckUnit = null)
    {
        if (!_phase.CurrentPhaseCheck(_phase.Prepare))
            return false;

        if (colorType == FieldColorType.none)
        {
            _field.ClearAllColor();
        }
        else if (colorType == FieldColorType.UnitSpawn)
        {
            _field.SetSpawnTileColor(colorType, deckUnit);
        }

        return true;
    }

    public void BenedictionCheck()
    {
        BattleUnit lastUnit = null;

        foreach (BattleUnit unit in Data.BattleUnitList)
        {
            if (unit.Team == Team.Enemy)
            {
                if (lastUnit == null)
                {
                    lastUnit = unit;
                }
                else
                {
                    lastUnit = null;
                    break;
                }
            }
        }

        if (lastUnit != null)
        {
            if (lastUnit.Buff.CheckBuff(BuffEnum.Benediction))
                return;

            if(GameManager.Data.StageAct == 0 && GameManager.Data.Map.CurrentTileID == 1)
                return;
            if (!GameManager.OutGameData.isTutorialClear())
            {
                if (GameManager.Data.StageAct == 0 && GameManager.Data.Tutorial_Benediction_Trigger == true)
                {
                    GameObject.Find("UI_Tutorial").GetComponent<UI_Tutorial>().TutorialActive(13);
                    GameManager.Data.Tutorial_Benediction_Trigger = false;
                }
            }
            
            lastUnit.SetBuff(new Buff_Benediction());
        }
    }

    public void PlayAfterCoroutine(Action action, float time) => StartCoroutine(PlayCoroutine(action, time));

    private IEnumerator PlayCoroutine(Action action, float time)
    {
        yield return new WaitForSeconds(time);

        action();
    }

    public bool ActiveTimingCheck(ActiveTiming activeTiming, BattleUnit caster, BattleUnit receiver = null)
    {
        bool skipNextAction = false;

        foreach (Stigma stigma in caster.StigmaList)
        {
            if ((activeTiming & stigma.ActiveTiming) == activeTiming)
            {
                stigma.Use(caster);
            }
        }

        foreach (Buff buff in caster.Buff.CheckActiveTiming(activeTiming))
        {
            skipNextAction = buff.Active(receiver);
        }

        caster.Buff.CheckCountDownTiming(activeTiming);

        caster.BattleUnitChangedStat = caster.Buff.GetBuffedStat();

        caster.RefreshHPBar();

        skipNextAction |= caster.Action.ActionTimingCheck(activeTiming, caster, receiver);

        return skipNextAction;
    }
}