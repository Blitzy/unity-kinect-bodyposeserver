using com.rfilkov.components;
using com.rfilkov.kinect;
using UnityEngine;

[RequireComponent(typeof(StageAvatarController))]
public class StageOriginGestureListener : MonoBehaviour, GestureListenerInterface {

    public int playerIndex = 0;
    public GestureType stageOriginCalibratePose = GestureType.Tpose;
    
    private StageAvatarController stageAvatarController;

    private void Awake() {
        stageAvatarController = GetComponent<StageAvatarController>();
    }

    public void UserDetected(ulong userId, int userIndex) {
        KinectGestureManager gestureManager = KinectManager.Instance.gestureManager;
        if (!gestureManager || (userIndex != playerIndex)) {
            return;
        }
        
        gestureManager.DetectGesture(userId, stageOriginCalibratePose);
    }

    public void UserLost(ulong userId, int userIndex) {
        if (userIndex != playerIndex) {
            return;
        }
    }

    public bool GestureCompleted(ulong userId, int userIndex, GestureType gesture, KinectInterop.JointType joint, Vector3 screenPos) {
        if (userIndex != playerIndex) {
            return false;
        }
        
        stageAvatarController.MoveStageOriginToCurrentPosition();

        return true;
    }

    public bool GestureCancelled(ulong userId, int userIndex, GestureType gesture, KinectInterop.JointType joint) {
        if (userIndex != playerIndex) {
            return false;
        }
        
        return true;
    }


    public void GestureInProgress(ulong userId, int userIndex, GestureType gesture, float progress, KinectInterop.JointType joint, Vector3 screenPos) {
    }
}