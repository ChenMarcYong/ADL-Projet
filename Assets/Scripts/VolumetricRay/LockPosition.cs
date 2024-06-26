using UnityEngine;

public class LockPosition : MonoBehaviour
{
    private Vector3 initialPosition;
    private bool isStart;
    public Transform playerTransform;

    void Start()
    {
        if (playerTransform != null)
        {
            transform.position = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        }
        isStart = false;
    }
    

    void LateUpdate()
    {
        //transform.position = new Vector3(initialPosition.x, transform.position.y, initialPosition.z);
        
        if ( !isStart && playerTransform != null)
        {
            transform.position = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            isStart = true;
        }
    }
}
