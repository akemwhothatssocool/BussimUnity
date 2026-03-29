using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class MainMenuManager : MonoBehaviour
{
    public GameObject storyPanel;
    public GameObject settingsPanel;
    public string firstSceneName = "GameScene";
    public int newGameStartingMoney = 100;
    public Selectable firstSelected;
    public Button continueButton;

    void Start()
    {
        if (storyPanel != null) storyPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        RefreshContinueButton();

        Selectable defaultSelection = continueButton != null && continueButton.interactable ? continueButton : firstSelected;
        if (EventSystem.current != null && defaultSelection != null)
            EventSystem.current.SetSelectedGameObject(defaultSelection.gameObject);
    }

    void OnEnable()
    {
        RefreshContinueButton();
    }

    void RefreshContinueButton()
    {
        bool hasSave = SaveSystem.HasSave();

        if (continueButton == null)
            continueButton = FindButtonByName("continue");

        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
            continueButton.gameObject.SetActive(true);

            if (!HasPersistentContinueBinding(continueButton))
            {
                continueButton.onClick.RemoveListener(ContinueGame);
                continueButton.onClick.AddListener(ContinueGame);
            }
        }
    }

    public void StartGame()
    {
        if (storyPanel != null)
        {
            storyPanel.SetActive(true);
            return;
        }

        LoadFirstLevel();
    }

    public void LoadFirstLevel()
    {
        SaveSystem.DeleteSave();
        PlayerPrefs.SetInt("PlayerMoney", newGameStartingMoney);
        PlayerPrefs.Save();
        SceneManager.LoadScene(firstSceneName);
    }

    public void ContinueGame()
    {
        if (!SaveSystem.HasSave()) return;

        SaveSystem.PrepareContinue();
        SceneManager.LoadScene(SaveSystem.GetSavedSceneName(firstSceneName));
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (EventSystem.current != null && firstSelected != null)
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    Button FindButtonByName(string token)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null) continue;
            if (button.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return button;
        }

        return null;
    }

    bool HasPersistentContinueBinding(Button button)
    {
        if (button == null) return false;

        int listenerCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < listenerCount; i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == nameof(ContinueGame))
            {
                return true;
            }
        }

        return false;
    }
}
