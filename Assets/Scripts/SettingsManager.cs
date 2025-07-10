using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("UI References")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Slider volumeSlider;
    public Button closeButton;
    public GameObject settingsPanel;

    [Header("UI Settings")]
    public float scrollSensitivity = 50f; // 增加默认滚动速度

    [Header("Resolution Settings")]
    public Vector2Int[] customResolutions = new Vector2Int[]
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1366, 768),
        new Vector2Int(1280, 720)
    };

    private Resolution[] resolutions;
    private List<string> resolutionOptions = new List<string>();
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeResolutions();
        InitializeUI();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        // 设置下拉菜单的滚动速度
        if (resolutionDropdown != null && resolutionDropdown.template != null)
        {
            var scrollRect = resolutionDropdown.template.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.scrollSensitivity = scrollSensitivity;
            }
        }
    }

    private void InitializeResolutions()
    {
        // 获取当前显示器支持的所有分辨率
        Resolution[] allResolutions = Screen.resolutions;

        // 创建一个HashSet来存储唯一的分辨率（不考虑刷新率）
        HashSet<Vector2Int> uniqueResolutions = new HashSet<Vector2Int>();

        // 添加自定义分辨率
        foreach (Vector2Int res in customResolutions)
        {
            uniqueResolutions.Add(res);
        }

        // 添加系统支持的分辨率
        foreach (Resolution res in allResolutions)
        {
            uniqueResolutions.Add(new Vector2Int(res.width, res.height));
        }

        // 转换为列表并按分辨率大小排序
        List<Vector2Int> sortedResolutions = uniqueResolutions.OrderByDescending(r => r.x * r.y).ToList();

        // 清除现有选项
        resolutionDropdown.ClearOptions();
        resolutionOptions.Clear();

        // 找到当前分辨率的索引
        int currentResolutionIndex = 0;
        Vector2Int currentRes = new Vector2Int(Screen.width, Screen.height);

        // 添加分辨率选项
        for (int i = 0; i < sortedResolutions.Count; i++)
        {
            string option = $"{sortedResolutions[i].x} x {sortedResolutions[i].y}";
            resolutionOptions.Add(option);

            if (sortedResolutions[i].x == currentRes.x && sortedResolutions[i].y == currentRes.y)
            {
                currentResolutionIndex = i;
            }
        }

        // 更新下拉菜单
        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // 存储排序后的分辨率数组供后续使用
        resolutions = new Resolution[sortedResolutions.Count];
        for (int i = 0; i < sortedResolutions.Count; i++)
        {
            resolutions[i] = new Resolution
            {
                width = sortedResolutions[i].x,
                height = sortedResolutions[i].y
            };
        }

        isInitialized = true;
    }

    private void InitializeUI()
    {
        if (!isInitialized) return;

        // 初始化全屏设置
        fullscreenToggle.isOn = Screen.fullScreen;

        // 初始化音量设置
        volumeSlider.value = AudioListener.volume;

        // 添加事件监听
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (!isInitialized || resolutionIndex >= resolutions.Length) return;

        Resolution resolution = resolutions[resolutionIndex];

        // 保存当前全屏状态
        bool isFullscreen = Screen.fullScreen;

        try
        {
            // 设置新的分辨率
            Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
            Debug.Log($"[SettingsManager] 设置分辨率: {resolution.width}x{resolution.height}, 全屏: {isFullscreen}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SettingsManager] 设置分辨率失败: {e.Message}");
            // 如果设置失败，尝试回退到安全分辨率
            Screen.SetResolution(1280, 720, isFullscreen);
        }

        SaveSettings();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        SaveSettings();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        SaveSettings();
    }

    private void SaveSettings()
    {
        if (!isInitialized) return;

        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
        PlayerPrefs.SetFloat("Volume", AudioListener.volume);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        // 先设置一个安全的默认分辨率
        if (!PlayerPrefs.HasKey("ResolutionIndex"))
        {
            Screen.SetResolution(1280, 720, false); // 默认设置为窗口模式
        }

        // 其他设置会在InitializeUI中加载
        Screen.fullScreen = PlayerPrefs.GetInt("Fullscreen", 0) == 1; // 默认值改为0，表示非全屏
        AudioListener.volume = PlayerPrefs.GetFloat("Volume", 1f);

        // 强制设置为窗口模式，无论之前的设置如何
        Screen.fullScreen = false;
        Screen.fullScreenMode = FullScreenMode.Windowed;
    }

    public void ClosePanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
}