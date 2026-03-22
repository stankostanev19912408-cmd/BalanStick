using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StickTiltForce))]
public class StickRetryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform stickTransform;
    [SerializeField] private Rigidbody stickRigidbody;
    [SerializeField] private StickTiltForce stickTiltForce;

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
    }

    public void ResetStickRotation()
    {
        if (stickTransform == null)
        {
            return;
        }

        Quaternion zeroRotation = Quaternion.identity;
        if (stickRigidbody != null)
        {
            stickRigidbody.position = Vector3.up;
            stickRigidbody.rotation = zeroRotation;
            stickRigidbody.velocity = Vector3.zero;
            stickRigidbody.angularVelocity = Vector3.zero;
            stickRigidbody.WakeUp();
        }
        else
        {
            stickTransform.position = Vector3.up;
            stickTransform.rotation = zeroRotation;
        }

        if (stickTiltForce != null)
        {
            stickTiltForce.ClearRetryRequirement();
        }
    }
}
