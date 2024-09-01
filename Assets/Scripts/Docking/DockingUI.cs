using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using UMol.Docking;

namespace UMol {
public class DockingUI : MonoBehaviour {
    public Text elecText;
    public Text vdwText;
    public Text totalText;

    public Canvas mainCanvas;
    public bool disableWhenIDLE = false;

    private float bigValue = 9999.9f;

    private float reasonableValue = 500.0f;


    private Color badColor = Color.red;
    private Color goodColor = Color.green;

    bool stateDockingMode;
    public DockingManager dockingManager;

    void Start() {
        stateDockingMode = false;
        dockingManager = UnityMolMain.getDockingManager();
    }
    void Update() {

        stateDockingMode = dockingManager.isRunning;

        if (disableWhenIDLE)
            mainCanvas.enabled = stateDockingMode;

        if (stateDockingMode) {
            float elecScaled = dockingManager.calcNBEnergy.nbEnergies.elec * dockingManager.ElecUIScaling;
            float vdwScaled = dockingManager.calcNBEnergy.nbEnergies.vdw * dockingManager.VDWUIScaling;

            elecText.text = TruncateEnergyValue(elecScaled);
            elecText.color = GetEnergyTextColor(elecScaled);

            vdwText.text = TruncateEnergyValue(vdwScaled);
            vdwText.color = GetEnergyTextColor(vdwScaled);

            float total = elecScaled + vdwScaled;
            totalText.text = TruncateEnergyValue(total);
            totalText.color = GetEnergyTextColor(total);
        }
    }
    string TruncateEnergyValue(float energy) {
        string res = "";
        if (energy > bigValue) {
            res = bigValue.ToString("F2", CultureInfo.InvariantCulture);
        }
        else if (energy < -bigValue) {
            res = (-bigValue).ToString("F2", CultureInfo.InvariantCulture);
        }
        else {
            res = energy.ToString("F2", CultureInfo.InvariantCulture);
        }
        return res;
    }

    Color GetEnergyTextColor(float energy) {

        Color textColor = Color.white;

        if (energy >= 0.0f) {
            float scaledEnergy = energy / reasonableValue;
            textColor = Color.Lerp(Color.white, badColor, scaledEnergy);
        }
        else {
            float scaledEnergy = -energy / reasonableValue;
            textColor = Color.Lerp(Color.white, goodColor, scaledEnergy);
        }

        return textColor;
    }
}
}