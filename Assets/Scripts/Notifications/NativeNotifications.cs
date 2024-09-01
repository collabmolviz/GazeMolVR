using System.Runtime.InteropServices;
using UnityEngine;

namespace UMol {
public class NativeNotifications {
    [DllImport("NativeNotifBrowser")]
    public static extern void ShowNotification(string t, string m, int itype);

    [DllImport("NativeNotifBrowser")]
    public static extern bool ShowDualChoice(string t, string m, int itype);

    [DllImport("NativeNotifBrowser")]
    public static extern bool ShowOKCancel(string t, string m, int itype);

    public static void Notify(string message, string title = "UnityMol", notifType itype = notifType.info){
        ShowNotification(title, message, (int)itype);
    }
    public static bool AskYesNo(string question, string title = "UnityMol", notifType itype = notifType.question) {
        return ShowDualChoice(title, question, (int)itype);
    }
    public static bool AskContinue(string question, string title = "UnityMol", notifType itype = notifType.question) {
        return ShowOKCancel(title, question, (int)itype);
    }
}

public enum notifType{
    info = 0,
    warning = 1,
    error = 2,
    question = 3
}
}