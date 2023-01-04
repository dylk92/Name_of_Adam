using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Instance
    static GameManager instance = null;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
            }
            return instance;
        }
    }
    #endregion
    #region BattleManager
    [SerializeField] BattleManager _battleMNG;
    public BattleManager BattleMNG => _battleMNG;
    #endregion
    #region StageManager
    [SerializeField] StageManager _StageMNG;
    public StageManager StageMNG => _StageMNG;
    #endregion
    #region DataManger
    DataManager _DataMNG;
    public DataManager DataMNG => _DataMNG;
    #endregion


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        _DataMNG = new DataManager();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            BattleMNG.TurnStart();
        }
        else if (Input.GetMouseButtonDown(2))
        {
            StageMNG.StageSign.SetNextStageSign();
        }
    }
}
