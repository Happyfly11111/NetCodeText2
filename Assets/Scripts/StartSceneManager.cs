using UnityEngine;
using UnityEngine.UI;

public class StartSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public Button settingsButton;
    public GameObject settingsPanel;

    private void Start()
    {
        // 确保设置面板初始时是隐藏的
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // 添加设置按钮点击事件
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
        }
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
}