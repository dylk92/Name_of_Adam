using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    private static GameManager s_instance;
    public static GameManager Instance { get { Init(); return s_instance; } }

    [SerializeField] private UIManager _ui;
    public static UIManager UI => Instance._ui;

    [SerializeField] DataManager _data;
    public static DataManager Data => Instance._data;

    private SoundManager _sound;
    public static SoundManager Sound => Instance._sound;

    private ResourceManager _resource = new ResourceManager();
    public static ResourceManager Resource => Instance._resource;

    private InputManager _input = new InputManager();
    public static InputManager InputManager => Instance._input;

    void Awake()
    {
        //if (s_instance != null)
        //    Destroy(gameObject); // 이미 GameManager가 있으면 이 오브젝트를 제거
        //else
        //    Init();
        _sound = new SoundManager();
        Data.Init();
        Sound.Init();
    }

    private static void Init()
    {
        if (s_instance == null)
        {
            GameObject go = GameObject.Find("@GameManager");

            if (go == null)
            {
                go = new GameObject("@GameManager");
                go.AddComponent<GameManager>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<GameManager>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
            SceneChanger.SceneChange("StageSelectScene");
        //SceneChanger.SceneChange("StigmaScene");
        //if(SceneChanger.GetSceneName() != "Battle")
        //{
        //    Sound.Play("BattleBGMA", Sounds.BGM);
        //}
        //else
        //{
        //    Sound.Play("BattleBGMB", Sounds.BGM);
        //}
    }
}
