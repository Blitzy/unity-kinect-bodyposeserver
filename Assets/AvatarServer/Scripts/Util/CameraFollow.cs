using UnityEngine;

[ExecuteAlways]
public class CameraFollow : MonoBehaviour {
    public Transform target;
    public Vector3 offset = new Vector3(0.0f, 1.0f, -3.0f);
    public bool lookAt = true;
    public Vector3 lookOffset = new Vector3(0.0f, 0.0f, 0.0f);

    private void LateUpdate() {
        if (target == null) {
            return;
        }

        // Move.
        Vector3 pos = target.position;
        pos += offset;

        transform.position = pos;

        if (lookAt) {
            // Look at.
            Vector3 lookDir = target.position - transform.position; 
            Vector3 eulerRot = Quaternion.LookRotation(lookDir).eulerAngles;
            eulerRot += lookOffset;

            transform.rotation = Quaternion.Euler(eulerRot);
        }
    }
}