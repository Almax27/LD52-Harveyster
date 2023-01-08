using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour {

    public Transform target = null;
    public float lookDamp = 0.3f;
    public float followDamp = 0.5f;
    public Vector3 offset = new Vector3(0,2,-10);
    public Vector2 screenContaintSize = new Vector2(10, 10);

    Vector2 followVelocity = Vector2.zero;
    Vector2 desiredPosition = Vector2.zero;

    Camera cam = null;

    bool snap = true;
    float smoothTime = 0;
    float smoothTimeVel = 0;

    public void SnapToTarget() { snap = true; }

    // Use this for initialization
    void Awake () 
    {
        if (target)
        {
            desiredPosition = target.position;
        }
        cam = GetComponent<Camera>();
        smoothTime = followDamp;
    }
	
	// Update is called once per frame
	void LateUpdate () 
    {
        //if(target == null && GameManager.Instance.CurrentPlayer)
            //target = GameManager.Instance.CurrentPlayer.transform;

        var player = GameManager.Instance.CurrentPlayer;

        Vector2 viewSize = new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
        Rect worldCameraBounds = GameManager.Instance.GetMapBounds(viewSize);

        float targetSmoothTime = followDamp;

        if (target != null)
        {
            desiredPosition = target.position + offset;

            //Constrain distance from player
            Rect screenContraintArea = new Rect((Vector2)desiredPosition - screenContaintSize * 0.5f, screenContaintSize);
            transform.position = screenContraintArea.Clamp(transform.position);
        }
        else
        {
            //desiredPosition = transform.position;
            //snap = true;
        }

        desiredPosition = worldCameraBounds.Clamp(desiredPosition);

        Vector2 newPos = desiredPosition;
        if (snap)
        {
            followVelocity = Vector3.zero;
            snap = false;
        }
        else
        {
            smoothTime = Mathf.SmoothDamp(smoothTime, targetSmoothTime, ref smoothTimeVel, 0.3f);
            newPos = Vector2.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime, float.MaxValue, Time.deltaTime);
        }

        newPos = Vector2.Min(Vector2.Max(newPos, worldCameraBounds.min), worldCameraBounds.max);

        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
	}

    void OnDrawGizmosSelected()
    {
        if(!cam) cam = GetComponent<Camera>();

        Vector2 viewSize = new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
        Rect worldCameraBounds = GameManager.Instance.GetMapBounds(viewSize);
        //Vector2 mapSize = GameManager.Instance.GetMapSize();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(worldCameraBounds.center, worldCameraBounds.size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector3)desiredPosition - Vector3.forward, 0.4f);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position + Vector3.forward, 0.4f);

        if (target)
        {
            Rect screenContraintArea = new Rect((Vector2)(target.position + offset) - screenContaintSize * 0.5f, screenContaintSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(screenContraintArea.center, screenContraintArea.size);
        }

    }
}