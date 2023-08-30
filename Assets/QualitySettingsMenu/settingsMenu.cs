using UnityEngine;
using UnityEngine.UI;
using System.IO;
using FidelityFX;

public class settingsMenu : MonoBehaviour
{
    public enum saveFormat
    {
        playerprefs,
        iniFile
    };

    public saveFormat saveAs;

    public bool pauseTimeWhenMenuOpen; //if Checked in inspector - Sets TimeScale to 0 when menu is open.

    public GameObject menuTransform;

    //if you use the prefab "_QualitySettingsMenu" they should all be assigned for you;
    public Slider qualityLevelSlider, antiAliasSlider, anisotropicModeSlider, anisotropicLevelSlider, fsrSlider;
    public Text qualityText, antiAliasText, anisotropicModeText, anisotropicLevelText, fpsCounterText, fsrSliderText;
    public GameObject resolutionsPanel, resButtonPrefab, upscalePanel, percentPanel;
    public Text currentResolutionText, curUpscaleText, curPercentText;
    public Toggle FPSToggle, windowedModeToggle, vSyncToggle;


    private GameObject resolutionsPanelParent, upscaleTransform, percentTrans;
    private Camera canvasCamera;
    private Resolution[] resolutions;
    private string[] outPut = new string[8], splitLine = new string[2], inPut = new string[8];
    private string lineToRead;
    private int lineCounter;

    private bool setMenu,
        openMenu,
        showFPS,
        fullScreenMode,
        toggleVSync,
        openMenuScale,
        setScale,
        openPercent,
        setPercent;

    private const float fpsMeasurePeriod = 0.2f;
    private float fpsNextPeriod = 0;
    private int fpsAccumulator = 0, currentFps, wantedResX, wantedResY;
    private int fpsCount, avgFps, maxFps;
    int avg = 0;

    /// <summary>
    /// ////////////////////////////////////////
    /// </summary>
    private bool init = false;

    public const string prefix1 = "GameOptions.";

    string[] Methods = new string[6]
    {
        "UltraQuality",
        "Quality",
        "Balanced",
        "Performance",
        "UltraPerformance",
        "Off"
    };

    int[] Percentages = new int[6] { 100, 90, 80, 70, 60, 50 };

    public class Preferences
    {
        public const string antialiasing = prefix + "AntiAliasing";
        public const string prefix = prefix1 + "DLSS.";
        public const string screenPercentage = prefix + "ScreenPercentage";
        public const string keyboardScheme = prefix + "FPSKeyboardScheme";
        public const string upsamplingMethod = prefix + "UpsamplingMethod";
    }

    public enum UpsamplingMethod
    {
        UltraQuality = 0,
        Quality = 1,
        Balanced = 2,
        Performance = 3,
        UltraPerformance = 4,
        Off = 5
    }


    public UpsamplingMethod upsamplingMethod
    {
        get => (UpsamplingMethod)PlayerPrefs.GetInt(Preferences.upsamplingMethod,
            (int)UpsamplingMethod.UltraPerformance);
        set => PlayerPrefs.SetInt(Preferences.upsamplingMethod, (int)value);
    }

    private int m_ScreenPercentage = -1;
    public GameObject vcm;

    public int screenPercentage
    {
        get
        {
            if (m_ScreenPercentage == -1)
                m_ScreenPercentage = PlayerPrefs.GetInt(Preferences.screenPercentage, 100);

            return m_ScreenPercentage;
        }
        set
        {
            m_ScreenPercentage = value;
            PlayerPrefs.SetInt(Preferences.screenPercentage, m_ScreenPercentage);
        }
    }

    float SetDynamicResolutionScale()
    {
        return m_ScreenPercentage;
    }

    public void Apply()
    {
        if (!init)
        {
            init = true;
        }

        UpdateUpscalingMethod();
    }

    private void UpdateUpscalingMethod()
    {
        Fsr2ImageEffect fsr = vcm.GetComponent<Fsr2ImageEffect>();
        if (fsr == null)
        {
            return;
        }

        if (upsamplingMethod == UpsamplingMethod.Off)
        {
            fsr.enabled = false;
        }
        else
        {
            fsr.enabled = true;
            fsr.qualityMode = (Fsr2.QualityMode)upsamplingMethod;
        }
    }

    public void SetScale()
    {
        float scale = fsrSlider.value;
        fsrSliderText.text = scale.ToString("f2");
        PlayerPrefs.SetFloat("fsrSlider", fsrSlider.value);

        Fsr2ImageEffect fsr = vcm.GetComponent<Fsr2ImageEffect>();
        if (fsr == null)
        {
            return;
        }

        fsr.GenerateReactiveParams.scale = scale;
    }


    // Use this for initialization
    void Start()
    {
        // menuTransform = transform.Find("Menu_QualitySettings").gameObject;
        fpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;
        resolutionsPanelParent = resolutionsPanel.transform.parent.parent.gameObject;
        upscaleTransform = upscalePanel.transform.parent.parent.gameObject;
        percentTrans = percentPanel.transform.parent.parent.gameObject;

        //assigns the main camera to all canvasis that are not set to "Screen Space-Overlay".
        OnLevelWasLoaded();

        //this reads all the values of the sliders and toggles and sets the Graphic settings accordingly.
        //(if the settings were saved before, they wil all be set to the saved setting before reading them)
        //(if this is the first time the game starts the toggles and sliders wil be where they were when the game was build)
        //(if you want the game to start at certain settings the first time, make sure to set everyting before you build)
        SetValues();

        Apply();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) //the Key used to open this menu.
        {
            Cursor.visible = true;

            //if you want to use a UI button to open the menu put this function on it.
            OpenQualitySettingMenu();

            if (resolutionsPanelParent.activeSelf)
            {
                resolutionsPanelParent.SetActive(false);
            }

            if (upscaleTransform.activeSelf)
                upscaleTransform.SetActive(false);
            if (percentTrans.activeSelf)
                percentTrans.SetActive(false);

            Apply();
        }

        if (openMenu)
        {
            if (!setMenu)
            {
                maxFps = 0;
                avgFps = 0;
                fpsCount = 0;
                
                menuTransform.gameObject.SetActive(true);
                setMenu = true;

                if (pauseTimeWhenMenuOpen)
                    Time.timeScale = 0;
            }
        }
        else
        {

            if (!setMenu)
            {
                maxFps = 0;
                avgFps = 0;
                fpsCount = 0;
                Cursor.visible = false;
                
                menuTransform.gameObject.SetActive(false);
                SavePlayerprefs();
                setMenu = true;

                if (pauseTimeWhenMenuOpen)
                    Time.timeScale = 1;
            }
        }

        if (openMenuScale)
        {
            if (!setScale)
            {
                upscaleTransform.gameObject.SetActive(true);
                setScale = true;
            }
        }
        else
        {
            if (!setScale)
            {
                upscaleTransform.gameObject.SetActive(false);
                SavePlayerprefs();
                setScale = true;
            }
        }

        if (openPercent)
        {
            if (!setPercent)
            {
                percentTrans.gameObject.SetActive(true);
                setScale = true;
            }
        }
        else
        {
            if (!setScale)
            {
                percentTrans.gameObject.SetActive(false);
                SavePlayerprefs();
                setScale = true;
            }
        }

        //this FPScounter is a standard Unity asset (thought it was handy to put it in).
        if (showFPS)
        {
            fpsAccumulator++;
            if (Time.realtimeSinceStartup > fpsNextPeriod)
            {
                currentFps = (int)(fpsAccumulator / fpsMeasurePeriod);
                fpsAccumulator = 0;
                fpsNextPeriod += fpsMeasurePeriod;
                avgFps += currentFps;
                fpsCount++;
                avg = (avgFps / fpsCount);
                if (maxFps < currentFps)
                {
                    maxFps = currentFps;
                }
            }

            fpsCounterText.text = "FPS:" + currentFps + " avg:" + avg + " max:" + maxFps;
        }
        else
        {
            maxFps = 0;
            avgFps = 0;
            fpsCount = 0;
            fpsCounterText.text = "";
        }
    }

    public void OpenQualitySettingMenu() //opens the menu.
    {
        openMenu = !openMenu;
        setMenu = false;
    }


    public void OpenScaleSettingMenu() //opens the menu.
    {
        openMenuScale = !openMenuScale;
        setScale = false;
    }


    public void OpenPercentSettingMenu() //opens the menu.
    {
        openPercent = !openPercent;
        setPercent = false;
    }

    public void
        SetQuality() //changes the general Quality setting without changing the Vsync,Antialias or Anisotropic settings.
    {
        int graphicSetting = Mathf.RoundToInt(qualityLevelSlider.value);
        QualitySettings.SetQualityLevel(graphicSetting, true);
        qualityText.text = QualitySettings.names[graphicSetting];
        //keep settings the way the Sliders and Toggels are set.
        SetWindowedMode();
        SetVSync();
        SetAntiAlias();
        SetAnisotropicFiltering();
        SetAnisotropicFilteringLevel();
    }

    public void ShowFPS()
    {
        showFPS = !showFPS;
    }

    public void SetWindowedMode()
    {
        if (windowedModeToggle.isOn)
            fullScreenMode = false;
        else fullScreenMode = true;
        Screen.SetResolution(wantedResX, wantedResY, fullScreenMode);
    }

    public void SetVSync()
    {
        if (vSyncToggle.isOn)
            QualitySettings.vSyncCount = 1;
        else QualitySettings.vSyncCount = 0;
    }

    public void SetAntiAlias()
    {
        int sliderValue = Mathf.RoundToInt(antiAliasSlider.value);
        switch (sliderValue)
        {
            case 0:
                QualitySettings.antiAliasing = 0;
                antiAliasText.text = "Off";
                break;
            case 1:
                QualitySettings.antiAliasing = 2;
                antiAliasText.text = QualitySettings.antiAliasing.ToString() + "x Multi Sampling";
                break;
            case 2:
                QualitySettings.antiAliasing = 4;
                antiAliasText.text = QualitySettings.antiAliasing.ToString() + "x Multi Sampling";
                break;
            case 3:
                QualitySettings.antiAliasing = 8;
                antiAliasText.text = QualitySettings.antiAliasing.ToString() + "x Multi Sampling";
                break;
        }
    }

    public void SetAnisotropicFiltering()
    {
        switch (Mathf.RoundToInt(anisotropicModeSlider.value))
        {
            case 0:
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                anisotropicModeText.text = "Disabled";
                break;
            case 1:
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                anisotropicModeText.text = "Enabled";
                break;
            case 2:
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                anisotropicModeText.text = "ForceEnabled";
                break;
        }
    }

    public void SetAnisotropicFilteringLevel()
    {
        int SliderValue = Mathf.RoundToInt(anisotropicLevelSlider.value);
        Texture.SetGlobalAnisotropicFilteringLimits(SliderValue, SliderValue);
        anisotropicLevelText.text = SliderValue.ToString();
    }

    public void SetShadows()
    {
        //not possible to acces in Unity at the moment i believe.
    }

    public void QuitGame()
    {
        SavePlayerprefs();
        Application.Quit();
    }

    private void SetValues() //set all settings according to the menu buttons.
    {
        //////////////////////////////////////////
        /// $"{percentage}% {(percentage == 100?"(Native)":"")}")
        curPercentText.text = $"{screenPercentage}% {(screenPercentage == 100 ? "(Native)" : "")}";
        for (int i = 0; i < Percentages.Length; i++)
        {
            GameObject button = (GameObject)Instantiate(resButtonPrefab); //the button prefab.
            button.GetComponentInChildren<Text>().text =
                $"{Percentages[i]}% {(Percentages[i] == 100 ? "(Native)" : "")}";
            int index = i;
            button.GetComponent<Button>().onClick
                .AddListener(() =>
                {
                    SetPercent(index);
                }); //adding a "On click" SetResolution() function to the button.
            button.transform.SetParent(percentPanel.transform, false);
        }

        ///////////////////////
        curUpscaleText.text = Methods[(int)upsamplingMethod];

        for (int i = 0; i < Methods.Length; i++)
        {
            GameObject button = (GameObject)Instantiate(resButtonPrefab); //the button prefab.
            button.GetComponentInChildren<Text>().text = Methods[i];
            int index = i;
            button.GetComponent<Button>().onClick
                .AddListener(() =>
                {
                    SetScaling(index);
                }); //adding a "On click" SetResolution() function to the button.
            button.transform.SetParent(upscalePanel.transform, false);
        }

        //his reads how many Quality levels your "Game" has and sices the slider accordingly.
        qualityLevelSlider.maxValue = QualitySettings.names.Length - 1;
        qualityLevelSlider.minValue = 0;

        resolutions = Screen.resolutions; //checking the available resolution options.
        currentResolutionText.text =
            Screen.currentResolution.width + "x" +
            Screen.currentResolution.height; //sets the text of the Screen Resolution button to the res we start with.
        //filling the Screen Resolution option menu with buttons, one for every available resolution option your monitor has.
        for (int i = 0; i < resolutions.Length; i++)
        {
            // if (resolutions[i].refreshRate != 120)
            // {
            //     continue;
            // }

            GameObject button = (GameObject)Instantiate(resButtonPrefab); //the button prefab.
            button.GetComponentInChildren<Text>().text = resolutions[i].width + "x" + resolutions[i].height;
            int index = i;
            button.GetComponent<Button>().onClick
                .AddListener(() =>
                {
                    SetResolution(index);
                }); //adding a "On click" SetResolution() function to the button.
            button.transform.SetParent(resolutionsPanel.transform, false);
        }

        LoadPlayerprefs(); // if any settings were saved before, this is where they are loaded and Sliders and toggles are set to the saved position.

        //reading Sliders and toggles and setting everything accordingly.
        int graphicSetting = Mathf.RoundToInt(qualityLevelSlider.value);
        QualitySettings.SetQualityLevel(graphicSetting, true);
        qualityText.text = QualitySettings.names[graphicSetting];
        SetVSync();
        SetWindowedMode();
        SetAntiAlias();
        SetAnisotropicFiltering();
        SetAnisotropicFilteringLevel();
    }

    public void SetResolution(int index) //the "On click" function on the resolutions buttons.
    {
        wantedResX = resolutions[index].width;
        wantedResY = resolutions[index].height;
        Screen.SetResolution(wantedResX, wantedResY, fullScreenMode);
        currentResolutionText.text = wantedResX + "x" + wantedResY;
    }


    public void SetScaling(int index) //the "On click" function on the resolutions buttons.
    {
        upsamplingMethod = (UpsamplingMethod)index;

        if (upsamplingMethod == UpsamplingMethod.Balanced)
        {
            // antiAliasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
        }

        curUpscaleText.text = Methods[index];
        Apply();
    }

    public void SetPercent(int index) //the "On click" function on the resolutions buttons.
    {
        int val = Percentages[index];

        screenPercentage = val;

        curPercentText.text = $"{val}% {(val == 100 ? "(Native)" : "")}";

        Apply();
    }

    public void ShowResolutionOptions() //opens the dropdown menu with available resolution options.
    {
        if (resolutionsPanelParent.activeSelf == false)
            resolutionsPanelParent.SetActive(true);
        else resolutionsPanelParent.SetActive(false);
    }

    public void ShowUpscaleOptions() //opens the dropdown menu with available resolution options.
    {
        if (upscaleTransform.activeSelf == false)
            upscaleTransform.SetActive(true);
        else
            upscaleTransform.SetActive(false);
    }

    public void ShowPercentOptions() //opens the dropdown menu with available resolution options.
    {
        if (percentTrans.activeSelf == false)
            percentTrans.SetActive(true);
        else
            percentTrans.SetActive(false);
    }

    private void SavePlayerprefs()
    {
        if (saveAs == saveFormat.playerprefs)
        {
            PlayerPrefs.SetInt("prefsSaved", 1);

            PlayerPrefs.SetInt("graphicsSlider", Mathf.RoundToInt(qualityLevelSlider.value));
            PlayerPrefs.SetInt("antiAliasSlider", Mathf.RoundToInt(antiAliasSlider.value));
            PlayerPrefs.SetInt("anisotropicModeSlider", Mathf.RoundToInt(anisotropicModeSlider.value));
            PlayerPrefs.SetInt("anisotropicLevelSlider", Mathf.RoundToInt(anisotropicLevelSlider.value));
            PlayerPrefs.SetFloat("fsrSlider", fsrSlider.value);

            PlayerPrefs.SetInt("wantedResolutionX", wantedResX);
            PlayerPrefs.SetInt("wantedResolutionY", wantedResY);


            int toggle = 0;
            if (!showFPS)
                toggle = 0;
            else toggle = 1;
            PlayerPrefs.SetInt("FPSToggle", toggle);

            if (vSyncToggle.isOn)
                toggle = 1;
            else toggle = 0;
            PlayerPrefs.SetInt("vSyncToggle", toggle);

            if (windowedModeToggle.isOn)
                toggle = 1;
            else toggle = 0;
            PlayerPrefs.SetInt("windowedModeToggle", toggle);
        }
        else if (saveAs == saveFormat.iniFile)
        {
            StreamWriter wr = new StreamWriter(Application.dataPath + "/QualitySettings.ini");

            string graphicsSliderV = Mathf.RoundToInt(qualityLevelSlider.value).ToString();
            string antiAliasSliderV = Mathf.RoundToInt(antiAliasSlider.value).ToString();
            string anisotropicModeSliderV = Mathf.RoundToInt(anisotropicModeSlider.value).ToString();
            string anisotropicLevelSliderV = Mathf.RoundToInt(anisotropicLevelSlider.value).ToString();

            string wantedResolutionX = wantedResX.ToString();
            string wantedResolutionY = wantedResY.ToString();

            outPut[0] = string.Format("Quality level={0}", graphicsSliderV);
            outPut[1] = string.Format("Anti Alias level={0}", antiAliasSliderV);
            outPut[2] = string.Format("Anisotropic Mode={0}", anisotropicModeSliderV);
            outPut[3] = string.Format("Anisotropic Level={0}", anisotropicLevelSliderV);

            int toggle = 0;
            if (!showFPS)
                toggle = 0;
            else toggle = 1;
            outPut[4] = string.Format("Show FPS={0}", toggle);

            if (windowedModeToggle.isOn)
                toggle = 1;
            else toggle = 0;
            outPut[5] = string.Format("Windowed Mode={0}", toggle);

            if (vSyncToggle.isOn)
                toggle = 1;
            else toggle = 0;
            outPut[6] = string.Format("V Sync={0}", toggle);

            outPut[7] = string.Format("Resolution={0}x{1}", wantedResolutionX, wantedResolutionY);

            for (int i = 0; i < outPut.Length; i++)
            {
                wr.WriteLine(outPut[i]);
            }

            wr.Close();
        }
    }

    private void LoadPlayerprefs()
    {
        if (saveAs == saveFormat.playerprefs)
        {
            if (PlayerPrefs.GetInt("prefsSaved") == 1) //to check if there are any.
            {
                qualityLevelSlider.value = PlayerPrefs.GetInt("graphicsSlider");
                antiAliasSlider.value = PlayerPrefs.GetInt("antiAliasSlider");
                anisotropicModeSlider.value = PlayerPrefs.GetInt("anisotropicModeSlider");
                anisotropicLevelSlider.value = PlayerPrefs.GetInt("anisotropicLevelSlider");
                fsrSlider.value = PlayerPrefs.GetFloat("fsrSlider");
                SetScale();

                wantedResX = PlayerPrefs.GetInt("wantedResolutionX");
                wantedResY = PlayerPrefs.GetInt("wantedResolutionY");
                currentResolutionText.text = wantedResX + "x" + wantedResY;

                int toggle = PlayerPrefs.GetInt("FPSToggle");
                if (toggle == 1)
                {
                    FPSToggle.isOn = true;
                    showFPS = true;
                }
                else
                {
                    FPSToggle.isOn = false;
                    showFPS = false;
                }

                toggle = PlayerPrefs.GetInt("windowedModeToggle");
                if (toggle == 1)
                    windowedModeToggle.isOn = true;
                else windowedModeToggle.isOn = false;

                toggle = PlayerPrefs.GetInt("vSyncToggle");
                if (toggle == 1)
                    vSyncToggle.isOn = true;
                else vSyncToggle.isOn = false;
            }
            else //no player prefs are saved.
            {
                //if nothing was saved use the full screen resolutions
                wantedResX = Screen.width;
                wantedResY = Screen.height;
            }
        }
        else if (saveAs == saveFormat.iniFile)
        {
            if (System.IO.File.Exists(Application.dataPath + "/QualitySettings.ini")) //to check if there are any.
            {
                StreamReader sr = new StreamReader(Application.dataPath + "/QualitySettings.ini");

                lineCounter = 0;
                while ((lineToRead = sr.ReadLine()) != null)
                {
                    splitLine = lineToRead.Split('=');
                    inPut[lineCounter] = splitLine[1];
                    lineCounter++;
                }

                sr.Close();

                qualityLevelSlider.value = int.Parse(inPut[0]);
                antiAliasSlider.value = int.Parse(inPut[1]);
                anisotropicModeSlider.value = int.Parse(inPut[2]);
                anisotropicLevelSlider.value = int.Parse(inPut[3]);

                splitLine = inPut[7].Split('x');
                wantedResX = int.Parse(splitLine[0]);
                wantedResY = int.Parse(splitLine[1]);
                currentResolutionText.text = splitLine[0] + "x" + splitLine[1];

                int toggle = int.Parse(inPut[4]);
                if (toggle == 1)
                {
                    FPSToggle.isOn = true;
                    showFPS = true;
                }
                else
                {
                    FPSToggle.isOn = false;
                    showFPS = false;
                }

                toggle = int.Parse(inPut[5]);
                if (toggle == 1)
                    windowedModeToggle.isOn = true;
                else windowedModeToggle.isOn = false;

                toggle = int.Parse(inPut[6]);
                if (toggle == 1)
                    vSyncToggle.isOn = true;
                else vSyncToggle.isOn = false;
            }
            else //no player prefs are saved.
            {
                //if nothing was saved use the full screen resolutions
                wantedResX = Screen.width;
                wantedResY = Screen.height;
            }
        }
    }

    //for testing/Debugging.
    public void DeletePlayerprefs()
    {
        PlayerPrefs.DeleteKey("prefsSaved");
        PlayerPrefs.DeleteKey("FPSToggle");
        PlayerPrefs.DeleteKey("graphicsSlider");
        PlayerPrefs.DeleteKey("antiAliasSlider");
        PlayerPrefs.DeleteKey("anisotropicModeSlider");
        PlayerPrefs.DeleteKey("anisotropicLevelSlider");
        PlayerPrefs.DeleteKey("wantedResolutionX");
        PlayerPrefs.DeleteKey("wantedResolutionY");
        PlayerPrefs.DeleteKey("windowedModeToggle");
        PlayerPrefs.DeleteKey("vSyncToggle");
    }


    //assigns the main camera to all canvasis that are not set to "Screen Space-Overlay".
    void OnLevelWasLoaded()
    {
        canvasCamera = Camera.main;
        menuTransform.gameObject.SetActive(true);
        Canvas[] X = transform.GetComponentsInChildren<Canvas>();
        foreach (Canvas x in X)
        {
            if (x.worldCamera == null)
                x.worldCamera = canvasCamera;
        }

        menuTransform.gameObject.SetActive(false);
        openMenu = false;
        setMenu = false;
    }
}