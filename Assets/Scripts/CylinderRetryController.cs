using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CylinderTiltForce))]
public class CylinderRetryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cylinderTransform;
    [SerializeField] private Rigidbody cylinderRigidbody;
    [SerializeField] private CylinderTiltForce cylinderTiltForce;

    public CylinderTiltForce TiltForce => cylinderTiltForce;

    private void Reset()
    {
        cylinderTransform = transform;
        cylinderRigidbody = GetComponent<Rigidbody>();
        cylinderTiltForce = GetComponent<CylinderTiltForce>();
    }

    private void Awake()
    {
        if (cylinderTransform == null)
        {
            cylinderTransform = transform;
        }

        if (cylinderRigidbody == null)
        {
            cylinderRigidbody = GetComponent<Rigidbody>();
        }

        if (cylinderTiltForce == null)
        {
            cylinderTiltForce = GetComponent<CylinderTiltForce>();
        }
    }

    public void ResetCylinderRotation()
    {
        if (cylinderTransform == null)
        {
            return;
        }

        Quaternion zeroRotation = Quaternion.identity;
        if (cylinderRigidbody != null)
        {
            cylinderRigidbody.position = Vector3.up;
            cylinderRigidbody.rotation = zeroRotation;
            cylinderRigidbody.velocity = Vector3.zero;
            cylinderRigidbody.angularVelocity = Vector3.zero;
            cylinderRigidbody.WakeUp();
        }
        else
        {
            cylinderTransform.position = Vector3.up;
            cylinderTransform.rotation = zeroRotation;
        }

        if (cylinderTiltForce != null)
        {
            cylinderTiltForce.ClearRetryRequirement();
        }
    }
}
