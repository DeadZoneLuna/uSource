using UnityEngine;

public class point_viewcontrol : MonoBehaviour 
{
    public Transform info_target;

    public void Start()
    {
        gameObject.AddComponent<Camera>();

        Vector3 relativePos = info_target.position - transform.position;
        transform.rotation = Quaternion.LookRotation(relativePos);
    }
}
