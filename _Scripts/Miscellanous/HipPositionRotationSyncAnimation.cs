using System.Collections;
using UnityEngine;

public class HipPositionRotationSyncAnimation : MonoBehaviour
{
    public Transform hipsBone; // Assign the hip bone in the inspector
    private Vector3 storedHipPosition;
    private Quaternion storedHipRotation;

    private void Start()
    {
        hipsBone = GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
        if (hipsBone == null)
        {
            Debug.LogError("[HipPositionRotationSync] Hip bone is not assigned!");
        }
    }

    // Call this from Animation Event at the end of the first animation
    public void StoreHipPositionAndRotation()
    {
        storedHipPosition = hipsBone.position;
        storedHipRotation = hipsBone.rotation;

        Debug.Log(
            $"[HipPositionRotationSync] Stored Hip Position: {storedHipPosition}, Rotation: {storedHipRotation.eulerAngles}"
        );
    }

    // Call this before playing the next animation
    public void ApplyStoredHipPositionAndRotation()
    {
        Vector3 originalPos = hipsBone.position;
        transform.position = hipsBone.position;
        hipsBone.position = originalPos;
        Debug.Log(
            $"[HipPositionRotationSync] Applied Stored Hip Position: {storedHipPosition}, Rotation: {storedHipRotation.eulerAngles}"
        );
    }
}
