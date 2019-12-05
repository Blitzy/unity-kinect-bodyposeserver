# Unity Kinect Body Pose Server

This Unity projects uses the Azure Kinect's body pose tracking capabilities to track a single person and translate them to a virtual avatar. These avatar movements are then piped through the [Session Socket Server](https://github.com/Blitzy/session-socket-server) (see dependency below) to allow other clients connected to the session socket server to represent the tracked body pose in locally without needing to be connected with a connect. This allows any supported Unity platform to receive the Kinect body tracking results as applied to a Unity humanoid over the network.

## Getting Started

### Option 1:
You may clone and use this Unity project directly in the editor by opening up `AvatarServer` scene file.

### Option 2:
You may download and install the pre-built Windows binary files attached to the most recent release and run the application directly.

### Connect to Server:
You may connect to a Session Socket Server using the Server button in the upper left corner of the screen.

> NOTE: Body tracking works even if not connected to a server. If you want the data to be piped to a Session Socket Server then you need to connect to one.

### Avatar's Session Data Id:
You may change the session data id of the kinect avatar using the input field displayed on screen. This allows you to change the target of the avatar data on the fly, depending on the id your remote application is expecting.

## External Dependencies

### Azure Kinect SDKs
This project has an external dependency on the [Azure Kinect SDK 1.3.0](https://docs.microsoft.com/en-us/azure/kinect-dk/sensor-sdk-download) and [Azure Kinect Body Tracking SDK 0.9.4](https://docs.microsoft.com/en-us/azure/kinect-dk/body-sdk-download)
As well as the [Azure Kinect Examples for Unity v1.7](https://assetstore.unity.com/packages/tools/integration/azure-kinect-examples-for-unity-149700). They are specifically not included in this repository as a file size constraint.

Make sure that both of those SDKs are installed and that the Azure Kinect Exmaples for Unity package is imported to the project.

### Socket Session Server
This project is designed to communicate through the open source (MIT) [Socket Session Server](https://github.com/Blitzy/session-socket-server) project. You can of course fork and modify how body tracking data is piped to other clients, but out of the box this is the backend solution.

Because the Socket Session Server is built on connection-less UDP sockets, this project will still run, it just wont be sending the data to anywhere remotely.


