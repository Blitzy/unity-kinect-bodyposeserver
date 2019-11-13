using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using SessionSocketClient;

/// <summary>
/// Seperate file to contain all the app's console commands.
/// </summary>
public class AppConsoleCommands
{
    private static bool _IsSetup;

    public static void Setup () {
        if (_IsSetup)
            return;

        _IsSetup = true;

        DebugCommands.Instance.AddCommand("version", Command_Version, "Print the version and build date of the app.",  "");
        DebugCommands.Instance.AddCommand("throwerror", Command_ThrowError, "Throw an application error. Useful for testing only.", "");
        DebugCommands.Instance.AddCommand("sessiondatadebug", Command_SessionDataDebug, "Wether or not to enable debug logs for session data components.", "< true | false >");
    }

    private static void Command_Version(string[] args) {
        Debug.Log("Version: " + Application.version);
    }

    private static void Command_ThrowError(string[] args) {
        Debug.LogError("This is a test error.");
    }

    private static void Command_SessionDataDebug(string[] args) {
        if (args.Length > 1) {
            string val = args[1];
            bool enable;
            if (bool.TryParse(val, out enable)) {
                UnitySessionData.DebugEnabled = enable;
                SessionDataManager.DebugEnabled = enable;

            } else {
                Debug.Log("Not a valid argument for this command.");
            }
        }
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Unity Session Data debug enabled?: " + UnitySessionData.DebugEnabled);
        sb.AppendLine("Session Data Manager debug enabled?: " + SessionDataManager.DebugEnabled);
        Debug.Log(sb.ToString());
    }
}