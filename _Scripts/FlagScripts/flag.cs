using Mirror;
using UnityEngine;

public class Flag : NetworkBehaviour
{
    [HideInInspector]
    public Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.position;
    }
}