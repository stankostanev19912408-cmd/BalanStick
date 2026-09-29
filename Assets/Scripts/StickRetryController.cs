using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StickTiltForce))]
public class StickRetryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform stickTransform;
    [SerializeField] private Rigidbody stickRigidbody;
    [SerializeField] private StickTiltForce stickTiltForce;
    [SerializeField] private BoundRunController boundRunController;

    public StickTiltForce TiltForce => stickTiltForce;

    private void Reset()
    {
        stickTransform = transform;
        stickRigidbody = GetComponent<Rigidbody>();
        stickTiltForce = GetComponent<StickTiltForce>();
    }

    private void Awake()
    {
        if (stickTransform == null)
        {
            stickTransform = transform;
        }

        if (stickRigidbody == null)
        {
            stickRigidbody = GetComponent<Rigidbody>();
        }

        if (stickTiltForce == null)
        {
            stickTiltForce = GetComponent<StickTiltForce>();
        }

        if (boundRunController == null)
        {
            boundRunController = FindObjectOfType<BoundRunController>();
        }
    }

    public void ResetStickRotation()
    {
        if (stickTransform == null)
        {
            return;
        }

        bool managedRetry = boundRunController != null && boundRunController.BeginRetry();
        Quaternion zeroRotation = Quaternion.identity;
        if (stickRigidbody != null)
        {
            if (!stickRigidbody.isKinematic)
            {
                stickRigidbody.velocity = Vector3.zero;
                stickRigidbody.angularVelocity = Vector3.zero;
            }

            stickRigidbody.position = Vector3.up;
            stickRigidbody.rotation = zeroRotation;
            stickTransform.SetPositionAndRotation(Vector3.up, zeroRotation);
            Physics.SyncTransforms();
            if (!stickRigidbody.isKinematic)
            {
                stickRigidbody.WakeUp();
            }
        }
        else
        {
            stickTransform.SetPositionAndRotation(Vector3.up, zeroRotation);
        }

        if (stickTiltForce != null && !managedRetry)
        {
            stickTiltForce.ClearRetryRequirement();
        }
    }
}
