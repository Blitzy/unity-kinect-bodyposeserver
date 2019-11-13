using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

#pragma warning disable CS0649

public class DeviceConsole : MonoBehaviour
{
	#region Inspector Variables

	[SerializeField] private GameObject		uiContainer;
	[SerializeField] private GameObject		logContainer;
	[SerializeField] private InputField		commandInputField;
	[SerializeField] private Button			closeButton;
	public bool	autoFocusInputField = true;

	[Space]

	[SerializeField] private Color			headerColour;

	[Header("Header Text:")]

	[TextArea]
	[SerializeField] private string			headerText;

	[Space]

	[SerializeField] private DeviceLogUI	logPrefab;
	[SerializeField] private DeviceLogUI	warningLogPrefab;
	[SerializeField] private DeviceLogUI	errorLogPrefab;
	[SerializeField] private DeviceLogUI	assertLogPrefab;
	[SerializeField] private DeviceLogUI	exceptionLogPrefab;
	[SerializeField] private DeviceLogUI	exceptionStackTracePrefab;

	[Space]

	#endregion

	#region Member Variables

    /// <summary>
    /// Should the console automatically open when an error is received.
    /// </summary>
    public bool OpenOnError = false;

    /// <summary>
    /// How many errors is the console allowed to display. 0 == unlimited.
    /// </summary>
    public int ErrorLimit = 5;

	public event System.Action OnConsoleOpened;

    private static int MsgMaxCharCount = 15000;
    private static int TotalMaxCharCount = 20000;
    private static int ErrorCount = 0;


	private List<DeviceLogUI>	logs;
    private int					curTotalCharCount;
    private int					commandHistoryIndex;
	private bool _printLogs = true;

	#endregion

	#region Unity Methods

	private void Awake()
	{
		logs = new List<DeviceLogUI>();

		closeButton.onClick.AddListener(HandleCloseClicked);

		commandHistoryIndex = 0;

		PrintHeader();
		PrintLogs();

		// Add callback so we can get when new logs are logged.
		DebugLogs.Instance.OnLogAdded		+= OnLogAdded;
		DebugLogs.Instance.OnLogsCleared	+= OnLogsCleared;

        // Add the default console commands
        DebugCommands.Instance.AddCommand("help", PrintHelp, "Prints list of commands");
		DebugCommands.Instance.AddCommand("clear", Clear, "Clears all text from the debug console");
	}

	private void LateUpdate()
	{
		if (commandInputField != null)
		{
			// Handles the up/down arrow key presses which displays commands from the history
			if (Input.GetKeyDown(KeyCode.UpArrow) && commandHistoryIndex > 0)
			{
				commandHistoryIndex--;

				commandInputField.text = DebugCommands.Instance.CommandHistory[commandHistoryIndex];
				commandInputField.MoveTextEnd(false);
			}
			else if (Input.GetKeyDown(KeyCode.DownArrow) && commandHistoryIndex < DebugCommands.Instance.CommandHistory.Count)
			{
				commandHistoryIndex++;

				if (commandHistoryIndex == DebugCommands.Instance.CommandHistory.Count)
				{
					commandInputField.text = "";
				}
				else
				{
					commandInputField.text = DebugCommands.Instance.CommandHistory[commandHistoryIndex];
					commandInputField.MoveTextEnd(false);
				}
			}
		}

		// Toggle visibility of the DeviceConsole when the '~' key is pressed in editor.
		if (Input.GetKeyDown(KeyCode.BackQuote))
		{
			SetVisible(!uiContainer.activeInHierarchy);
		}
	}

	private void OnDestroy()
	{
		DebugLogs.Instance.OnLogAdded		-= OnLogAdded;
		DebugLogs.Instance.OnLogsCleared	-= OnLogsCleared;
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Sets this instances visibility
	/// </summary>
	public void SetVisible(bool visible)
	{
		// Set its visibility
		uiContainer.SetActive(visible);

		if (commandInputField != null)
		{
			// Clear any user typed text
			commandInputField.text = "";

			// If it became visible, focus the input field
			if (visible)
			{
				InputFieldObtainFocus();
			}
		}
	}

	/// <summary>
	/// Returns wether or not this device console is visible.
	/// </summary>
	public bool IsVisible() {
		return uiContainer.activeSelf;
	}

	/// <summary>
	/// Called when the input field is done editing (eg. user pressed enter)
	/// </summary>
	public void OnEndEdit()
	{
		// If the UI Container is not active then don't process the command.
		if (!uiContainer.activeSelf)
		{
			return;
		}

		string	ciText			= commandInputField.text;
		int		newlineIndex	= ciText.IndexOf('\n');

		if (newlineIndex >= 0)
		{
			ciText = ciText.Remove(newlineIndex, 1);
		}

		// If the text is empty just return
		if (string.IsNullOrEmpty(ciText))
		{
			return;
		}

		string[]	strs	= ciText.Split(' ');
		string		command	= strs[0];

		Debug.Log(string.Format("$ {0}", ciText));

		// Execute the command
		if (!DebugCommands.Instance.ExecuteCommand(strs))
		{
			Debug.LogWarningFormat("{0}: Command Not Found", command);
		}

		commandHistoryIndex = DebugCommands.Instance.CommandHistory.Count;

		commandInputField.text = "";

		InputFieldObtainFocus();
	}

	#endregion

	#region Private Methods

	private void PrintToConsole(string text, DeviceLogUI prefab = null)
	{
		if (logContainer != null)
		{
			AddLogToContainer((prefab != null) ? prefab : logPrefab, text);
		}
	}

	private void PrintToConsole(DebugLogs.Log log, string prefix = "")
	{
        if (logContainer == null)
            return;
        
        if (log.type == LogType.Assert || log.type == LogType.Exception || log.type == LogType.Error)
        {
            ErrorCount++;
            if (ErrorLimit > 0)
            {
                if (ErrorCount > ErrorLimit)
                {
                    // Not allowed to display any more error style messages.
                    return;
                }
            }

            if (OpenOnError)
            {
                // Automatically open the console if log is an error.
                SetVisible(true);
            }
        }

        DeviceLogUI prefab = GetPrefab(log);
		string logMessage = log.message;

		logMessage = prefix + logMessage;

		AddLogToContainer(prefab, logMessage);

		if (log.type == LogType.Exception)
		{
			PrintToConsole(log.stackTrace, exceptionStackTracePrefab);
		}
	}

    private DeviceLogUI GetPrefab (DebugLogs.Log log)
    {
        switch (log.type)
        {
            case LogType.Log:
                return logPrefab;
            case LogType.Warning:
                return warningLogPrefab;
            case LogType.Error:
                return errorLogPrefab;
            case LogType.Assert:
                return assertLogPrefab;
            case LogType.Exception:
                return exceptionLogPrefab;
            default:
                throw new System.Exception("No prefab for log type: " + log.type);
        }
    }

	private void PrintHeader()
	{
		PrintToConsole(string.Format("<color=#{0}>{1}</color>", ColourToHex(headerColour), headerText));
	}

	private void PrintLogs()
	{
		for (int i = 0; i < DebugLogs.Instance.Logs.Count; i++)
		{
			PrintToConsole(DebugLogs.Instance.Logs[i]);
		}
	}

	private void DestroyLogs()
	{
		if (logs != null)
		{
			for (int i = 0; i < logs.Count; i++)
			{
				Destroy(logs[i].gameObject);
			}
		}
		
		logs.Clear();

        ErrorCount = 0;
		curTotalCharCount = 0;
	}
	
	private void AddLogToContainer(DeviceLogUI prefab, string text)
	{
		DeviceLogUI deviceLogUI = Instantiate<DeviceLogUI>(prefab);

        var clampedText = text;
        if (clampedText.Length > MsgMaxCharCount)
            clampedText = text.Substring(0, MsgMaxCharCount);
        
        deviceLogUI.textUI.text = clampedText;
		deviceLogUI.transform.SetParent(logContainer.transform);
		deviceLogUI.transform.localScale = Vector3.one;

		curTotalCharCount += deviceLogUI.textUI.text.Length;
		
		while (curTotalCharCount > TotalMaxCharCount)
		{
			curTotalCharCount -= logs[0].textUI.text.Length;
			Destroy(logs[0].gameObject);
			logs.RemoveAt(0);
		}

		logs.Add(deviceLogUI);
	}

	private void OnLogAdded(DebugLogs.Log log)
	{
		if (!_printLogs)
			return;

		PrintToConsole(log);
	}

	private void OnLogsCleared()
	{
		DestroyLogs();
		PrintHeader();
	}

	private void InputFieldObtainFocus()
	{
		if (autoFocusInputField && commandInputField != null)
		{
			commandInputField.Select();
			commandInputField.ActivateInputField();
		}
	}

	private string ColourToHex(Color32 color)
	{
		string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
		return hex;
	}

	private void HandleCloseClicked()
	{
		SetVisible(false);
	}

	#region Command Methods

	private void SetAF(string[] args)
	{
		if (args.Length != 2)
        {
            PrintToConsole("Please specify either t or f.", errorLogPrefab);
			return;
		}

		if (args[1] == "t" || args[1] == "true")
		{
			autoFocusInputField = true;
		}
		else if (args[1] == "f" || args[1] == "false")
		{
			autoFocusInputField = false;
		}
		else
        {
            PrintToConsole("Please specify either t or f.", errorLogPrefab);
		}
	}

	private static void PrintHelp(string[] args)
	{
		for (int i = 0; i < DebugCommands.Instance.Commands.Count; i++)
		{
			DebugCommands.Command command = DebugCommands.Instance.Commands[i];

			string helpStr = string.Format("{0}", command.name);

			if (!string.IsNullOrEmpty(command.argsDescription))
			{
				helpStr += string.Format(" {0}", command.argsDescription);
			}

			if (!string.IsNullOrEmpty(command.description))
			{
				helpStr += string.Format(" - {0}", command.description);
			}

			Debug.Log(helpStr);
		}
	}

	private static void Clear(string[] args)
	{
		DebugLogs.Instance.ClearLogs();
	}

	#endregion

	#endregion
}

#pragma warning restore CS0649 