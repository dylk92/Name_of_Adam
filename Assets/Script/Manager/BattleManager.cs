using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//컷씬에 사용될 데이터를 모아둔 클래스
class CutSceneData
{
    // 확대 할 대상
    public Vector3 ZoomLocation;
    // 얼마나 줌할 것인지
    public float DefaultZoomSize = 60;
    public float ZoomSize;
    // 어느 캐릭터가 어느 위치에 있는지
    public Character LeftChar;
    public Character RightChar;
    // 어느 캐릭터가 공격자인지
    public Character AttackChar;
    public Character HitChar;
}

// 전투를 담당하는 매니저
// 필드와 턴의 관리
// 필드에 올라와있는 캐릭터의 제어를 배틀매니저에서 담당

public class BattleManager : MonoBehaviour
{
    // 턴 시작이 가능한 상태인가?
    bool CanTurnStart = true;
    // 컷씬이 실행중인가?
    bool isCutScene = false;

    // 스킬의 타겟 지정을 위한 임시 변수
    public Character SelectedChar;

    #region TurnFlow
    // 턴 진행
    public void TurnStart()
    {
        if (CanTurnStart)
        {
            CanTurnStart = false;
            GameManager.Instance.DataMNG.BattleOrderReplace();

            StartCoroutine(CharUse());
        }
    }
    IEnumerator CharUse()
    {
        List<Character> BattleCharList = GameManager.Instance.DataMNG.BattleCharList;

        for (int i = 0; i < BattleCharList.Count; i++)
        {
            if (BattleCharList[i] == null)
                break;

            BattleCharList[i].use();

            yield return new WaitForSeconds(BattleCharList[i].characterSO.SkillLength() * 0.5f);
        }

        TurnEnd();
    }

    void TurnEnd()
    {
        GameManager.Instance.DataMNG.AddMana(2);
        CanTurnStart = true;
    }

    #endregion

    #region CutScene

    public void BattleCutScene(Transform ZoomLocation, Character AttackChar, Character HitChar)
    {
        // 줌 인, 줌 아웃하는데 들어가는 시간
        float zoomTime = 0.5f;

        // 어느 캐릭터가 어느 방향에 있나 확인 후 각 위치에 할당
        Character LeftChar, RightChar;

        #region Set Char LR
        if(AttackChar.LocX < HitChar.LocX)
        {
            LeftChar = AttackChar;
            RightChar = HitChar;
        }
        else if (HitChar.LocX < AttackChar.LocX)
        {
            LeftChar = HitChar;
            RightChar = AttackChar;
        }
        else
        {
            if(AttackChar.characterSO.team == Team.Player)
            {
                LeftChar = AttackChar;
                RightChar = HitChar;
            }
            else
            {
                LeftChar = HitChar;
                RightChar = AttackChar;
            }
        }
        #endregion

        #region Create CutSceneData

        CutSceneData CSData = new CutSceneData();

        CSData.ZoomLocation = ZoomLocation.position;
        CSData.ZoomLocation.z = Camera.main.transform.position.z;
        CSData.DefaultZoomSize = Camera.main.fieldOfView;
        CSData.ZoomSize = 30; // 얘는 나중에 유동적으로 받기
        CSData.LeftChar = LeftChar;
        CSData.RightChar = RightChar;
        CSData.AttackChar = AttackChar;
        CSData.HitChar = HitChar;

        #endregion

        StartCoroutine(ZoomIn(CSData, zoomTime));
    }
    
    IEnumerator ZoomIn(CutSceneData CSData, float duration)
    {
        float time = 0;

        while (time <= duration)
        {
            time += Time.deltaTime;

            Camera.main.transform.position = Vector3.Lerp(new Vector3(0, 0, Camera.main.transform.position.z), CSData.ZoomLocation, time / duration);
            Camera.main.fieldOfView = Mathf.Lerp(CSData.DefaultZoomSize, CSData.ZoomSize, time / duration);
            yield return null;
        }
        StartCoroutine(PlayCutScene(CSData, duration));
    }

    IEnumerator PlayCutScene(CutSceneData CSData, float duration)
    {
        // 공격하고 모션바뀌고 기타 등등 여기서 처리

        yield return new WaitForSeconds(1);

        StartCoroutine(ZoomOut(CSData, duration));

    }

    IEnumerator ZoomOut(CutSceneData CSData, float duration)
    {
        float time = 0;

        while (time <= duration)
        {
            time += Time.deltaTime;

            Camera.main.transform.position = Vector3.Lerp(CSData.ZoomLocation, new Vector3(0, 0, Camera.main.transform.position.z), time / duration);
            Camera.main.fieldOfView = Mathf.Lerp(CSData.ZoomSize, CSData.DefaultZoomSize, time / duration);

            yield return null;
        }
    }

    #endregion
}