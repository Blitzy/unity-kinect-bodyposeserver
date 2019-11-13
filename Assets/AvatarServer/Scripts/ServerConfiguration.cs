using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class ServerConfiguration : IDisposable {

    private const string PrefsKey_Address = "server_address";
    private const string PrefsKey_Port = "server_port";

    public IPAddress Address { get; private set; }
    public int Port { get; private set; }

    public event System.Action<ServerConfiguration> onServerConfigrationSet;

    public bool IsConfigured {
        get {
            if (Address == null) {
                return false;
            }
            if (!IsValidPort(Port)) {
                return false;
            }

            return true;
        }
    }

    public ServerConfiguration() {
        Load();
    }

    /// <summary>
    /// Load server configuration from unity's player preferences.
    /// </summary>
    /// <returns>Returns true if successful, otherwise false.</returns>
    public bool Load() {
        string prefsAddress = PlayerPrefs.GetString(PrefsKey_Address, string.Empty);
        IPAddress ip;
        if (IPAddress.TryParse(prefsAddress, out ip)) {
            Address = ip;
        } else {
            Address = null;
            return false;
        }

        int prefsPort = PlayerPrefs.GetInt(PrefsKey_Port, -1);
        if (prefsPort == -1) {
            return false;
        } else {
            Port = prefsPort;
        }

        return true;
    }

    /// <summary>
    /// Set the host and port of the server configuration.
    /// Will save to unity's player preference for persistence.
    /// </summary>
    /// <param name="address">Server address to connect to.</param>
    /// <param name="port">Port on the host to connect to.</param>
    public void SetAndSave(IPAddress address, int port) {
        Address = address;
        Port = port;

        PlayerPrefs.SetString(PrefsKey_Address, address.ToString());
        PlayerPrefs.SetInt(PrefsKey_Port, port);
        PlayerPrefs.Save();

        if (onServerConfigrationSet != null) {
            onServerConfigrationSet(this);
        }
    }

    private static bool IsValidPort(int port) {
        return port >= 1 && port <= 65535;
    }

    public void Dispose() {
    }
}