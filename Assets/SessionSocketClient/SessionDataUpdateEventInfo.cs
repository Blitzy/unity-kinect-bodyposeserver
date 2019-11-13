using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SessionDataUpdateEventInfo
{
    public string senderIp;
    public string sessionDataId;
    public string sessionDataJson;
    
    public SessionDataUpdateEventInfo(string senderIp, string sessionDataId, string sessionDataJson){
        this.senderIp = senderIp;
        this.sessionDataId = sessionDataId;
        this.sessionDataJson = sessionDataJson;
    }
}