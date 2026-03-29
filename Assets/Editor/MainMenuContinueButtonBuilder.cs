using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuContinueButtonBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/Add Continue Button To Main Menu")]
    public static void AddContinueButtonToMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        MainMenuManager manager = Object.FindFirstObjectByType<MainMenuManager>();
        if (manager == null)
            throw new MissingReferenceException("MainMenuManager was not found in MainMenu scene.");

        Button newGameButton = FindButton("NewGameButton");
        if (newGameButton == null)
            throw new MissingReferenceException("NewGameButton was not found in MainMenu scene.");

        Button continueButton = FindButton("ContinueButton");
        if (continueButton == null)
            continueButton = DuplicateContinueButton(newGameButton);

        WireContinueButton(manager, continueButton);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(continueButton);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static Button DuplicateContinueButton(Button newGameButton)
    {
        GameObject clonedButton = Object.Instantiate(newGameButton.gameObject, newGameButton.transform.parent);
        clonedButton.name = "ContinueButton";
        clonedButton.transform.SetSiblingIndex(newGameButton.transform.GetSiblingIndex());

        RectTransform newGameRect = newGameButton.GetComponent<RectTransform>();
        RectTransform continueRect = clonedButton.GetComponent<RectTransform>();
        Button settingsButton = FindButton("SettingsButton");

        float buttonSpacing = 80f;
        if (settingsButton != null)
        {
            RectTransform settingsRect = settingsButton.GetComponent<RectTransform>();
            buttonSpacing = Mathf.Abs(newGameRect.anchoredPosition.y - settingsRect.anchoredPosition.y);
            if (buttonSpacing < 1f)
                buttonSpacing = 80f;
        }

        continueRect.anchoredPosition = new Vector2(newGameRect.anchoredPosition.x, newGameRect.anchoredPosition.y + buttonSpacing);

        TMP_Text label = clonedButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "CONTINUE";

        return clonedButton.GetComponent<Button>();
    }

    private static void WireContinueButton(MainMenuManager manager, Button continueButton)
    {
        manager.continueButton = continueButton;

        ClearPersistentListeners(continueButton.onClick);
        UnityEventTools.AddPersistentListener(continueButton.onClick, manager.ContinueGame);
    }

    private static Button FindButton(string objectName)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (button != null && button.name == objectName)
                return button;
        }

        return null;
    }

    private static void ClearPersistentListeners(UnityEngine.Events.UnityEvent unityEvent)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(unityEvent, i);
    }
}
