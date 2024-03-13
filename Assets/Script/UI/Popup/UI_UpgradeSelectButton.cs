using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_UpgradeSelectButton : UI_Popup
{
    [SerializeField] private List<TextMeshProUGUI> _buttonTextList;
    [SerializeField] private List<Image> _buttonGemImageList;
    [SerializeField] private List<GameObject> _button;
    [SerializeField] private TextMeshProUGUI _titleText;

    private UpgradeSceneController _uc;
    private bool isCanReset;

    public void Init(UpgradeSceneController uc, List<Upgrade> upgrades, string titleText)
    {
        _uc = uc;
        isCanReset = (titleText.Equals("Select Upgrade")) ? true : false;
        _titleText.SetText(GameManager.Locale.GetLocalizedEventScene(titleText));

        for (int i = 0; i < 3; i++)
        {
            if (i < upgrades.Count)
            {
                _buttonTextList[i].text = upgrades[i].UpgradeDescription;
                _buttonGemImageList[i].sprite = upgrades[i].UpgradeImage160;
            }
            else
            {
                _button[i].SetActive(false);
            }
        }
    }

    public void ResetUpgradeSelectButtons()
    {
        if (!isCanReset)
            return;

        var upgradeList = _uc.ResetUpgrade();
        Init(_uc, upgradeList, "Select Upgrade");
    }

    public void OnClick(int select)
    {
        _uc.OnUpgradeSelect(select);
    }

    public void QuitBtn()
    {
        this.transform.SetAsFirstSibling();
        this.gameObject.SetActive(false);
    }

}
