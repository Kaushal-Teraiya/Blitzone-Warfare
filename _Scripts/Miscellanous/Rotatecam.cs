using UnityEngine;

public class Rotatecam : MonoBehaviour
{
    public float turnSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up * turnSpeed * Time.deltaTime);
    }
}
