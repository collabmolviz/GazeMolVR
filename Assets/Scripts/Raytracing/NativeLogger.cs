//From https://raw.githubusercontent.com/andydbc/unity-native-logger/master/Assets/NativeLogger/NativeLogger.cs
using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class NativeLogger
{

    public enum Level
    {
        LogInfo = 0,
        LogWarning = 1,
        LogError = 2
    }

    // [InitializeOnLoadMethod]
    public static void Initialize()
    {
        LogDelegate callback_delegate = new LogDelegate(LogCallback);
        IntPtr delegatePtr = Marshal.GetFunctionPointerForDelegate(callback_delegate);
        SetLogger(delegatePtr);
    }


    [DllImport("UnityPathTracer")]
    private static extern void SetLogger(IntPtr fp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogDelegate(int level, string str);

    static void LogCallback(int level, string msg)
    {
        if (level == (int)Level.LogInfo)
            Debug.Log(msg);
        else if (level == (int)Level.LogWarning)
            Debug.LogWarning(msg);
        else if (level == (int)Level.LogError)
            Debug.LogError(msg);
    }
}