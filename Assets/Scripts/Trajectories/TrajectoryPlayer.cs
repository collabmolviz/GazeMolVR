using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UMol {

public class TrajectoryPlayer : MonoBehaviour {

    public UnityMolStructure s;

    public bool play = false;
    public bool forward = true;
    public bool looping = true;
    public bool forwardAndBack = false;
    public float trajFramerate = 3.0f;
    public bool average = false;
    public int windowSize = 5;

    public bool smoothing = false;

    public List<GameObject> trajUIs;

    private float timeperiod = 0.0f;

    private bool prevLoop = true;
    private bool prevForward = true;
    private float prevFrameRate = 3.0f;
    private bool sliderChanged = false;
    private int prevWindowSize = 5;


    private List<Text> frameTexts;
    private List<Slider> frameSliders;
    private List<Text> frameCountTexts;
    private List<Toggle> loopToggles;
    private List<Toggle> forwardToggles;
    private List<Toggle> forwardAndBackToggles;
    private List<Slider> frameRateSliders;
    private List<Text> framerateTexts;
    private List<Toggle> smoothToggles;
    private List<Toggle> averageToggles;
    private List<Text> windowSizeTexts;

    void OnDestroy() {
        foreach (GameObject trajui in trajUIs) {
            trajui.SetActive(false);
            Canvas.ForceUpdateCanvases();
            //Force update Canvas of LoadedMoleculesUI parent
            Transform t = trajui.transform.parent.parent.parent;
            if (t != null && t.GetComponent<RectTransform>() != null) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(t.GetComponent<RectTransform>());
            }
        }
        trajUIs.Clear();
        frameTexts.Clear();
        frameSliders.Clear();
        frameCountTexts.Clear();
        loopToggles.Clear();
        forwardToggles.Clear();
        forwardAndBackToggles.Clear();
        frameRateSliders.Clear();
        framerateTexts.Clear();
        smoothToggles.Clear();
        averageToggles.Clear();
        windowSizeTexts.Clear();
    }

    void Start() {
        trajUIs = new List<GameObject>();

        frameTexts = new List<Text>();
        frameSliders = new List<Slider>();
        frameCountTexts = new List<Text>();
        loopToggles = new List<Toggle>();
        forwardToggles = new List<Toggle>();
        forwardAndBackToggles = new List<Toggle>();
        frameRateSliders = new List<Slider>();
        framerateTexts = new List<Text>();
        smoothToggles = new List<Toggle>();
        averageToggles = new List<Toggle>();
        windowSizeTexts = new List<Text>();

#if !DISABLE_MAINUI
        //Delay the creation of the reader to let time for the UI to be created
        this.Invoke(doCreateTrajP, 0.1f);
#endif
    }
    void doCreateTrajP() {
        var uiMans = GameObject.FindObjectsOfType<UIManager>();
        if (uiMans.Length == 0) {
            return;
        }
        foreach (UIManager uiMan in uiMans) {
            GameObject curTrajUI = uiMan.structureNameToUIObject[s.name].transform.Find("Trajectory Menu").gameObject;
            trajUIs.Add(curTrajUI);
            curTrajUI.SetActive(true);
            Text frameText = curTrajUI.transform.Find("Row 1/Current Frame").GetComponent<Text>();
            frameTexts.Add(frameText);
            Text frameCountText = curTrajUI.transform.Find("Row 1/Frame Count").GetComponent<Text>();
            frameCountTexts.Add(frameCountText);
            Slider frameSlider = curTrajUI.transform.Find("Row 2/Timeline").GetComponent<Slider>();
            frameSliders.Add(frameSlider);
            Toggle loopToggle = curTrajUI.transform.Find("Row 5/Loop").GetComponent<Toggle>();
            loopToggles.Add(loopToggle);
            Toggle forwardToggle = curTrajUI.transform.Find("Row 5/ForwardSwitch").GetComponent<Toggle>();
            forwardToggles.Add(forwardToggle);
            Toggle forwardAndBackToggle = curTrajUI.transform.Find("Row 5/BackForth").GetComponent<Toggle>();
            forwardAndBackToggles.Add(forwardAndBackToggle);
            Slider frameRateSlider = curTrajUI.transform.Find("Row 4/FrameRate").GetComponent<Slider>();
            frameRateSliders.Add(frameRateSlider);
            Text framerateText = curTrajUI.transform.Find("Row 4/Current FrameRate").GetComponent<Text>();
            framerateTexts.Add(framerateText);
            Toggle smoothToggle = curTrajUI.transform.Find("Row 5/Smooth").GetComponent<Toggle>();
            smoothToggles.Add(smoothToggle);

            Toggle averageToggle = curTrajUI.transform.Find("Row 6/DoAverage").GetComponent<Toggle>();
            averageToggles.Add(averageToggle);

            curTrajUI.transform.Find("Row 3/Play").GetComponent<Button>().onClick.AddListener(switchPlay);
            loopToggle.onValueChanged.AddListener((value) => {switchLoop(value);});
            curTrajUI.transform.Find("Row 5/Smooth").GetComponent<Toggle>().onValueChanged.AddListener((value) => {switchSmooth(value);});

            forwardToggle.onValueChanged.AddListener((value) => {switchForward(value);});

            curTrajUI.transform.Find("Row 5/BackForth").GetComponent<Toggle>().onValueChanged.AddListener((value) => {switchBackForth(value);});

            curTrajUI.transform.Find("Row 3/Backward").GetComponent<Button>().onClick.AddListener(forcePrevFrame);
            curTrajUI.transform.Find("Row 3/Forward").GetComponent<Button>().onClick.AddListener(forceNextFrame);

            curTrajUI.transform.Find("Row 3/Unload").GetComponent<Button>().onClick.AddListener(unloadTrajectory);

            frameSlider.onValueChanged.AddListener(setFrame);

            curTrajUI.transform.Find("Row 4/FrameRate").GetComponent<Slider>().onValueChanged.AddListener(changeFrameRate);

            curTrajUI.transform.Find("Row 6/DoAverage").GetComponent<Toggle>().onValueChanged.AddListener((value) => {switchAverage(value);});

            Text windowSizeText = curTrajUI.transform.Find("Row 6/WindowSize/Image/Size").GetComponent<Text>();
            windowSizeTexts.Add(windowSizeText);
            curTrajUI.transform.Find("Row 6/WindowSize/ButtonMinus").GetComponent<Button>().onClick.AddListener(() => {changeWindowSize(windowSize - 1);});
            curTrajUI.transform.Find("Row 6/WindowSize/ButtonPlus").GetComponent<Button>().onClick.AddListener(() => {changeWindowSize(windowSize + 1);});


            updateFrameCount();
            updateFrameNumber();
            updateFramerateValue();
            updateWindowSizeValue();


            LayoutRebuilder.ForceRebuildLayoutImmediate(curTrajUI.transform.parent.parent.gameObject.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(curTrajUI.transform.parent.parent.parent.gameObject.GetComponent<RectTransform>());
        }
    }

    void switchPlay() {
        play = !play;
    }
    void switchLoop(bool newL) {
        looping = newL;
        foreach (var l in loopToggles) {
            l.SetValue(newL);
        }
    }
    void switchForward(bool newF) {
        forward = newF;
        foreach (var l in forwardToggles) {
            l.SetValue(newF);
        }
    }
    void switchBackForth(bool newS) {
        forwardAndBack = newS;
        foreach (var l in forwardAndBackToggles) {
            l.SetValue(newS);
        }
    }
    void switchSmooth(bool newSmooth) {
        if (newSmooth == true)
            switchAverage(false);
        smoothing = newSmooth;
        foreach (var l in smoothToggles) {
            l.SetValue(newSmooth);
        }
    }
    void switchAverage(bool newAv) {
        if (newAv == true)
            switchSmooth(false);
        average = newAv;
        foreach (var l in averageToggles) {
            l.SetValue(newAv);
        }
    }

    void Update() {

        if (s.xdr != null && play) {
            sliderChanged = false;
            float invFramerate = 1.0f / trajFramerate;

            if (!forwardAndBack && !looping) {
                if ((forward && s.xdr.currentFrame + 1 >= s.xdr.numberFrames) || (!forward && s.xdr.currentFrame - 1 < 0)) {
                    play = false;
                }
            }

            if (!smoothing) {
                if (timeperiod > invFramerate) {
                    timeperiod = 0.0f;

                    if (forwardAndBack) {
                        if ((forward && s.xdr.currentFrame + 1 >= s.xdr.numberFrames) || (!forward && s.xdr.currentFrame - 1 < 0)) {
                            forward = !forward;
                        }
                    }

                    s.trajNext(forward, looping, average, windowSize);
                }
            }
            else {
                bool newFrame = false;
                if (timeperiod > invFramerate) {
                    timeperiod = 0.0f;
                    newFrame = true;
                }

                if (newFrame && forwardAndBack) {
                    if ((forward && s.xdr.currentFrame + 2 >= s.xdr.numberFrames) || (!forward && s.xdr.currentFrame - 2 < 0)) {
                        forward = !forward;
                    }
                }
                float t = trajFramerate * timeperiod;
                s.trajNextSmooth(t, forward, looping, newFrame);
            }

            //Update UI Part
            if (trajUIs != null && trajUIs.Count > 0) {
                updateFrameNumber();

                if (prevLoop != looping) {
                    updateLoopToggle();
                }
                if (prevFrameRate != trajFramerate) {
                    updateFramerateValue();
                }
                if (prevForward != forward) {
                    updateForwardToggle();
                }
                if (prevWindowSize != windowSize)
                    updateWindowSizeValue();

                sliderChanged = true;
            }
            timeperiod += UnityEngine.Time.deltaTime;

        }
    }

    void updateFrameCount() {
        if (frameCountTexts == null || frameSliders == null) {
            return;
        }
        foreach (var fct in frameCountTexts) {
            fct.text = s.xdr.numberFrames + " frames";
        }
        foreach (var fs in frameSliders) {
            fs.maxValue = s.xdr.numberFrames;
        }
    }

    void updateFrameNumber() {
        if (frameTexts == null || frameSliders == null) {
            return;
        }
        foreach (var ft in frameTexts) {
            ft.text = String.Format("Frame {0}", s.xdr.currentFrame);
        }
        foreach (var fs in frameSliders) {
            fs.value = s.xdr.currentFrame;
        }
    }
    void updateLoopToggle() {
        if (loopToggles == null) {
            return;
        }
        foreach (var lt in loopToggles) {
            lt.isOn = looping;
        }
        prevLoop = looping;
    }

    void updateForwardToggle() {
        if (forwardToggles == null) {
            return;
        }
        foreach (var ft in forwardToggles) {
            ft.isOn = forward;
        }
        prevForward = forward;
    }
    void updateFramerateValue() {
        if (frameRateSliders == null) {
            return;
        }
        foreach (var frs in frameRateSliders)
            frs.value = trajFramerate;

        foreach (var ft in framerateTexts)
            ft.text = "Speed : " + trajFramerate.ToString("F1");

        prevFrameRate = trajFramerate;
    }
    void updateWindowSizeValue() {
        foreach (var wst in windowSizeTexts)
            wst.text = windowSize.ToString();

        prevWindowSize = windowSize;
    }
    void forceNextFrame() {
        s.trajNext(true, looping);
        play = false;
        updateFrameNumber();
    }
    void forcePrevFrame() {
        s.trajNext(false, looping);
        play = false;
        updateFrameNumber();
    }
    void changeFrameRate(float newF) {
        trajFramerate = newF;
    }
    void changeWindowSize(int newS) {
        newS = Mathf.Max(0, newS);
        windowSize = newS;
    }
    void setFrame(float val) {
        if (sliderChanged) {
            float frameNumber = val;
            int idF = (int) frameNumber;
            play = false;
            s.trajSetFrame(idF);
            updateFrameNumber();
        }
    }

    void unloadTrajectory() {
        API.APIPython.unloadTraj(s.name);
        GameObject.Destroy(this);
    }
}
}