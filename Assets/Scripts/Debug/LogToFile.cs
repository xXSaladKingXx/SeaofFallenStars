using System;
using System.IO;
using UnityEngine;

public sealed class LogToFile : MonoBehaviour
{
    [SerializeField] private string fileName = "runtime_log.txt";
    [SerializeField] private bool includeStackTraces = true;

    private string _path;
    private readonly object _lock = new object();

    private void OnEnable()
    {
        _path = Path.Combine(Application.persistentDataPath, fileName);
        Application.logMessageReceivedThreaded += HandleLog;

        WriteLine($"=== Log started {DateTime.Now:O} ===");
        WriteLine($"persistentDataPath: {Application.persistentDataPath}");
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
        WriteLine($"=== Log ended {DateTime.Now:O} ===");
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        var line = $"{DateTime.Now:O} [{type}] {condition}";
        if (includeStackTraces && !string.IsNullOrWhiteSpace(stackTrace))
            line += "\n" + stackTrace;

        WriteLine(line);
    }

    private void WriteLine(string text)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_path, text + "\n");
            }
        }
        catch { /* never throw from logger */ }
    }
}
