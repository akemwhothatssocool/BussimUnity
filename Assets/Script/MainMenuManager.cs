using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject storyPanel; // พาเนลเล่าเรื่อง
    public string firstSceneName = "GameScene";

    void Start()
    {
        if (storyPanel != null) storyPanel.SetActive(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        if (storyPanel != null) storyPanel.SetActive(true);
        else LoadFirstLevel();
    }

    public void LoadFirstLevel()
    {
        // 🌟 ตั้งค่าเงินเริ่มต้นที่ 100 บาท ก่อนเข้าฉากเกม
        PlayerPrefs.SetInt("PlayerMoney", 100);
        SceneManager.LoadScene(firstSceneName);
    }

    public void QuitGame() => Application.Quit();
}