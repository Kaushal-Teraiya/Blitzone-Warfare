using UnityEngine;

public class BoneDebugger : MonoBehaviour
{
    public Transform rootObject;

    void Start()
    {
        if (rootObject == null)
        {
            Debug.LogWarning("Assign rootObject in Inspector!");
            return;
        }
    }

    string GetFullPath(Transform current)
    {
        string path = current.name;
        while (current.parent != null && current.parent != rootObject)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        return path;
    }
}
