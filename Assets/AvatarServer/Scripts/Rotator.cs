using UnityEngine;
using System.Collections;

public class Rotator : MonoBehaviour
{
    public Vector3 speed;
    public Space space;

    private void Update ()
    {
        transform.Rotate(speed * Time.deltaTime, space);
    }
}
