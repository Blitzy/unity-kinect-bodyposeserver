namespace SessionSocketClient {

    /// <summary>
    /// Interface to implement for any class that wants to send and receive session data to/from the socket server.
    /// </summary>
    public interface ISessionData {
        
        string Id { get; }

        void UpdateData(string json);
    }
}