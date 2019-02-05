using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogOnScreen : MonoBehaviour
{

    Vector2 scrollPos;
    string myLog;
    Queue myLogQueue = new Queue();
    void Start()
    {
        Debug.Log("Server Starting");
    }
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }
    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        myLog = logString;
        string newString = "\n [" + type + "] : " + myLog;
        myLogQueue.Enqueue(newString);
        if (type == LogType.Exception)
        {
            newString = "\n" + stackTrace;
            myLogQueue.Enqueue(newString);
        }
        myLog = string.Empty;
        foreach (string mylog in myLogQueue)
        {
            myLog += mylog;
        }
    }
    void OnGUI()
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.Label(myLog);
        GUILayout.EndScrollView();
    }
}