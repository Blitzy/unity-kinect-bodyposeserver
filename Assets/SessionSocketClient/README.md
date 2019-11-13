# Unity Session Socket Client

A Unity library for connecting, sending, and receiving with the [Session Socket Server](https://github.com/Blitzy/session-socket-server)

## Quick Start
1. Launch and instance of the Session Socket Server and open up the Demo scene.  
2. Fill in the socket server's ip adress and port on the `DemoController` component.
3. Indicator in top right corner should turn green and say "Connected" if connection was successful. If it does not connect, check the Unity console logs for debugging.
4. Play with the on-screen controls in the Demo scene and notice how they persist even when you restart the game.

## The Components

### Session Socket Client

The Session Socket Client game object has three main components attached to it.

#### SocketManager
The `SocketManager` contains all the functionality of connecting, sending, and receiving messages with/from the Session Socket Server.

#### SocketHeartbeat
The `SocketHeartbeat` component watches the SocketManager's connection state and will send regular `KEEP` (Heartbeat) messages to the Session Socket Server to let it know that the client is still connected. If the server shuts down or connection is otherwise lost to the Sesssion Socket Server, `SocketHeartbeat` will automatically stop sending heartbeats.

#### SessionDataManager
The `SessionDataManager` manages all of the session data on the client side from the Session Socket Server. It is the responsibility of SessionData objects to subscribe to it for updates and unsubscribe when no longer needed (like when being destroyed).

### Session Data

#### ISessionData
The `ISessionData` interface is a generic interface that can be implemented by any class that wants to subscribe to updates from the `SessionDataManager`.

#### UnitySessionData
`UnitySessionData` is an abstract class that inherits from `ISessionData` and handles SessionData in a very Unity-like way through MonoBehaviour components. There are a number of examples already written to show how to use `UnitySessionData` that you can check out. 

The basic concept is that a `UnitySessionData` component is simply a MonoBehaviour component that acts as a data store for another object. It should listen for changes on that object and keep a copy of that object's data for itself. When data changes on the object the `UnitySessionData` component is designed to watch, it should save it locally and then run the `UnitySessionData.SaveData()` method in order to send it to the Session Socket Server.

> **NOTE:** `UnitySessionData` components save and update themselves using Unity's [JsonUtility](https://docs.unity3d.com/ScriptReference/JsonUtility.html) class. You should be familiar with how it works so that you make sure to only have data you actually want transfered over the server to be sent.

`UnitySessionData` has a number of methods that derived classes need to implement: 

1. `Init()` -  Use to initialize the session data component with other components it is designed to watch.
2. `HookupEventListeners()` - Subscribe to events the session data cares about to modify its own data.
3. `UnhookEventListeners()` - Unsubscribe from events the session data is listening to.
4. `UpdateDataFromLocal()` - Use to update the session data stored in this component from the other components it watches.
5. `UpdateLocalFromData()` - Use to update the watched components with the data stored in this component.

## Third Party Credits
 - [MiniJSON](https://gist.github.com/darktable/1411710) - Calvin Rien ([darktable](https://gist.github.com/darktable/1411710))