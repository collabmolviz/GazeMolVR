using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UMol {
[RequireComponent(typeof(Camera))]
public class RaytracerManager : MonoBehaviour {

    public static RaytracerManager Instance;

    public delegate void RTActivate();
    ///Called on activation of the raytracing mode
    public static event RTActivate OnRTActivate;

    public delegate void RTDeactivate();
    ///Called on activation of the raytracing mode
    public static event RTDeactivate OnRTDeactivate;

    ///Texture holding the raytraced image
    public Texture2D _texture;
    ///Material to blit the texture to the screen
    Material _blitMat;
    int texWidth;
    int texHeight;

    [DllImport("UnityPathTracer")]
    static extern bool UnityPathTracerIsInit(IntPtr RTObj);

    [DllImport("UnityPathTracer")]
    static extern bool UnityPathTracerIsReady(IntPtr RTObj);

    [DllImport("UnityPathTracer")]
    static extern IntPtr UnityPathTracerInit(int w, int h, string libpath);

    [DllImport("UnityPathTracer")]
    static extern void UnityPathTracerRender(IntPtr URT);

    [DllImport("UnityPathTracer")]
    static extern void UnityPathTracerGetFrame(IntPtr URT, byte[] texture);

    [DllImport("UnityPathTracer")]
    static extern void UnityPathTracerResize(IntPtr URT, int w, int h);

    [DllImport("UnityPathTracer")]
    static extern void UnityPathTracerRelease(IntPtr URT);

    [DllImport("UnityPathTracer")]
    static extern int RT_GetFrameId(IntPtr URT);

    [DllImport("UnityPathTracer")]
    static extern bool RT_DenoiserHasRan(IntPtr URT);

    [DllImport("UnityPathTracer")]
    static extern bool UnityPathTracerForceDenoiserOff(IntPtr RTObj, bool d);

    [DllImport("UnityPathTracer")]
    static extern bool UnityPathTracerSetDenoiserStart(IntPtr RTObj, int f);

    [DllImport("UnityPathTracer")]
    static extern void UnityPathTracerRestartFrame(IntPtr RTObj);

    [DllImport("UnityPathTracer")]
    static extern void RT_SetAmbientLightIntensity(IntPtr RTObj, float i);

    [DllImport("UnityPathTracer")]
    static extern void RT_SetCameraEnvMap(IntPtr RTObj, int idTexture);//Use idTexture = -1 to disable HDRI map


    public IntPtr URT;//Pointer to the Raytracer object in C++

    ///Array of byte to copy from the raytracer to Unity
    private byte[] texData;

    ///Is the raytracer running
    private bool _isRunning = false;

    ///Initialized correctly
    public bool allOK = false;

    ///Is denoiser on
    public bool rtDenoiser = true;

    private Camera cam;
    private int savedCullingMask = 0;
    private int onlyUIMask = 0;
    private bool hasStarted = false;
    int currentScreenResoW;
    int currentScreenResoH;

    ///Check for UnityMolMain.raytracingMode changes
    bool rtModeOn = false;

 
    void doStart()
    {

        ///This is paraview material library as a json format
        //string matLibURL = "https://gitlab.kitware.com/paraview/materials/-/archive/master.zip";
        // Alternative version: string matLibURL = "https://owncloud.si.ibpc.fr/owncloud/index.php/s/f0WXRHqnNuvHlH8/download";
        // StartCoroutine(GetMaterialLib(matLibURL));

        if (Instance != null)
            Debug.LogError("Something is wrong, there are more than one RaytracerManager instance");

        Instance = this;
        NativeLogger.Initialize();

        cam = GetComponent<Camera>();
        currentScreenResoW = Screen.width;
        currentScreenResoH = Screen.height;
        savedCullingMask = cam.cullingMask;
        onlyUIMask = (1 << LayerMask.NameToLayer("UI")) | (1 << LayerMask.NameToLayer("3DUI"));

        _blitMat = new Material(Shader.Find("Custom/BlitTexture"));
        _blitMat.hideFlags = HideFlags.HideAndDontSave;
        _texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
        _texture.wrapMode = TextureWrapMode.Clamp;
        _texture.filterMode = FilterMode.Bilinear;

        texWidth = _texture.width;
        texHeight = _texture.height;

        texData = new byte[_texture.width * _texture.height * sizeof(int)];

#if UNITY_EDITOR
        string relativedllPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("UnityPathTracer")[0]);
        relativedllPath = relativedllPath.Replace("Assets/", "/");
        string absoPath = System.IO.Path.GetDirectoryName(Application.dataPath + relativedllPath);
        URT = UnityPathTracerInit(Screen.width, Screen.height, absoPath);
#else
#if UNITY_STANDALONE_WIN
        string absoPath = Application.dataPath + "/Plugins/x86_64/";
        Debug.Log("absoPath = " + absoPath);
        URT = UnityPathTracerInit(Screen.width, Screen.height, absoPath);
#else
        URT = UnityPathTracerInit(Screen.width, Screen.height, "");
#endif
#endif


        if (URT == IntPtr.Zero) {
            Debug.LogError("Failed to init Raytracer");
            _isRunning = false;
            allOK = false;
            return;
        }
        allOK = true;
        _isRunning = true;
        hasStarted = true;

        if (OnRTActivate != null)
            OnRTActivate();

        rtModeOn = true;
    }

    void Update() {

        if (UnityMolMain.raytracingMode && !hasStarted) {
            doStart();
        }
        if (!hasStarted)
            return;

        if (rtModeOn != UnityMolMain.raytracingMode) {
            rtModeOn = UnityMolMain.raytracingMode;
            if (rtModeOn) {
                if (OnRTActivate != null)
                    OnRTActivate();
            }
            else {
                if (OnRTDeactivate != null)
                    OnRTDeactivate();
            }
        }

        if (RaytracingMaterial.materialsBank.Count < 1) {
            RaytracingMaterial.recordPresetMaterial();
        }

        if (!UnityMolMain.inVR() && UnityMolMain.raytracingMode &&
                allOK && _isRunning && UnityPathTracerIsInit(URT)) {

            if (currentScreenResoH != Screen.height || currentScreenResoW != Screen.width) {
                //Do resize !
                currentScreenResoW = Screen.width;
                currentScreenResoH = Screen.height;
                Destroy(_texture);

                _texture = new Texture2D(currentScreenResoW, currentScreenResoH, TextureFormat.RGBA32, false);
                _texture.wrapMode = TextureWrapMode.Clamp;
                _texture.filterMode = FilterMode.Bilinear;

                texWidth = _texture.width;
                texHeight = _texture.height;

                texData = new byte[_texture.width * _texture.height * sizeof(int)];

                UnityPathTracerResize(URT, texWidth, texHeight);

            }

            if (cam.cullingMask != onlyUIMask)
                cam.cullingMask = onlyUIMask;

            //When a frame finished to render -> copy it to the texture that will be displayed
            if (UnityPathTracerIsReady(URT)) {

                UnityPathTracerGetFrame(URT, texData);

                //Send the frame to the GPU
                _texture.LoadRawTextureData(texData);
                _texture.Apply();

                //Restart a frame
                UnityPathTracerRender(URT);
            }
        }
        else {
            if (!UnityMolMain.raytracingMode && cam.cullingMask != savedCullingMask) {
                cam.cullingMask = savedCullingMask;
            }
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if (UnityMolMain.raytracingMode && allOK) {
            _blitMat.SetTexture("_MainTex", src);
            _blitMat.SetTexture("_RenderTex", _texture);
            //Display the current content of the texture
            // Graphics.Blit(src, dest, _blitMat);
            Graphics.Blit(_texture, dest);
        }
        else {
            Graphics.Blit(src, dest);
        }
    }

    ///Current frame id computed by the raytracer
    /// as RT is done in a different thread, the frame id is desynchronized with Unity
    public int getRTFrameId() {
        if (!hasStarted)
            return 0;
        return RT_GetFrameId(URT);
    }

    ///Did the denoiser ran this frame
    public bool denoiserRan() {
        if (!hasStarted)
            return false;
        return RT_DenoiserHasRan(URT);
    }

    public void forceDenoiserOff(bool v) {
        if (!hasStarted)
            return;
        UnityPathTracerForceDenoiserOff(URT, v);
        rtDenoiser = !v;
    }

    public void setDenoiserFrameStart(int f = 8) {
        if (f >= 0) {
            UnityPathTracerSetDenoiserStart(URT, f);
        }
        else {
            Debug.LogError("Only positive values");
        }
    }
    public void restartRTFrame() {
        if (hasStarted)
            UnityPathTracerRestartFrame(URT);
    }

    public void setAmbientLight(float i) {
        if (hasStarted)
            RT_SetAmbientLightIntensity(URT, i);
    }

    void OnApplicationQuit() {
        if (hasStarted) {
            UnityPathTracerRelease(URT);
            URT = IntPtr.Zero;
            allOK = false;
        }
        Destroy(_texture);
    }

    IEnumerator GetMaterialLib(string uri)
    {
        Debug.Log("Start downloading material library");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            webRequest.certificateHandler = new BypassCertificate();
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError || webRequest.isHttpError)
                Debug.LogError(webRequest.error);
            else {
                Debug.Log("Finished downloading material library");

                using(MemoryStream byteStream = new MemoryStream(webRequest.downloadHandler.data)) {
                    using (ZipInputStream zipStream = new ZipInputStream(byteStream)) {
                        while (zipStream.GetNextEntry() is ZipEntry zipEntry) {
                            var entryFileName = zipEntry.Name;
                            var directoryName = Path.GetDirectoryName(entryFileName);
                            if (directoryName.EndsWith("textures")) {
                                Debug.Log("Textures/" + Path.GetFileName(entryFileName));
                            }
                            else {
                                Debug.Log(Path.GetFileName(entryFileName));
                            }
                        }
                    }
                }
            }
        }
    }
}

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        //Simply return true no matter what
        return true;
    }
}
}