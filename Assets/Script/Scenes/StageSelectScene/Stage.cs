using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum StageType
{
    Store,
    Event,
    Battle
}

[Serializable]
public class StageData
{
    public int ID;
    public StageType Type;
    public StageName Name;
    public int StageLevel;
    public int StageID;
}

public class Stage : MonoBehaviour
{
    [SerializeField] Animation Anim;
    [Space(10f)]
    [SerializeField] public List<Stage> NextStage;
    [Space(10f)]
    [Header("StageInfo")]
    [SerializeField] public StageData Datas;

    private Coroutine coro;
    private float ZoomSpeed = 0.05f;


    private void Awake()
    {
        StageManager.Instance.InputStageList(this);

        coro = null;
        Anim.Stop();
    }

    public void OnMouseUp() => StageManager.Instance.StageMove(Datas.ID);

    public void OnMouseEnter()
    {
        if (coro != null)
            StopCoroutine(coro);
        coro = StartCoroutine(SizeUp());
    }

    public void OnMouseExit()
    {
        if (coro != null)
            StopCoroutine(coro);
        coro = StartCoroutine(SizeDown());
    }

    public StageData SetBattleStage(int a, int b)
    {
        Datas.StageLevel = a;
        Datas.StageID = b;

        return Datas;
    }

    public void StartBlink()
    {
        Anim.Play();
    }

    IEnumerator SizeUp()
    {
        while (transform.localScale.x < 1.5f)
        {
            transform.localScale += new Vector3(ZoomSpeed, ZoomSpeed);
            yield return null;
        }

        transform.localScale = new Vector3(1.5f, 1.5f, 1);
    }

    IEnumerator SizeDown()
    {
        while (transform.localScale.x > 1)
        {
            transform.localScale -= new Vector3(ZoomSpeed, ZoomSpeed);
            yield return null;
        }

        transform.localScale = new Vector3(1, 1, 1);
    }
}