using UnityEngine;
using UnityEngine.UIElements;

public class CameraFollow : MonoBehaviour
{
    public GameObject FollowTarget;
    void Start()
    {

    }

    void Update()
    {
        transform.LookAt(FollowTarget.transform);
    }
}
