using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffSelectUI : MonoBehaviour
{
    public GameObject rootPanel;
    public Button[] optionButtons;
    public Text[] optionTexts;

    GameManager GM;
    List<GameManager.BuffOption> currentOptions = new List<GameManager.BuffOption>();

    public void Show(GameManager gameManager, List<GameManager.BuffOption> options)
    {
        GM = gameManager;
        currentOptions = options;

        if (rootPanel != null)
            rootPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (i < currentOptions.Count && optionTexts[i] != null)
                optionTexts[i].text = currentOptions[i].GetText();
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => SelectOption(index));
        }
    }

    public void Hide()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    void SelectOption(int index)
    {
        if (GM == null) return;
        if (index < 0 || index >= currentOptions.Count) return;

        GM.ApplyBuff(currentOptions[index]);
        GM.CloseBuffSelection();
    }
}