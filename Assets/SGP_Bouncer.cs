using RVP;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
    Rigidbody rb;
    VehicleParent vp;
    public float minimalVelocityAtWall = 40;

    public float d_collAngle;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        vp = GetComponent<VehicleParent>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        vp.colliding = true;
        Transform collider = collision.GetContact(0).thisCollider.transform;
        if(collider.childCount > 0)
        {
            // set sparks particle system to position of contact
            collider.GetChild(0).position = collision.GetContact(0).point;
            // show sparks
            if(collider.GetChild(0).name != "Tire Mark")
                collider.GetChild(0).GetComponent<ParticleSystem>().Play();
            // no bounce if wheels not grounded
            if (vp.groundedWheels == 0)
                return;
            Vector3 colNormal = collision.GetContact(0).normal;
            //Debug.DrawRay(collision.GetContact(0).point, colNormal, Color.red, 5);
            float velocityMagn = collision.relativeVelocity.magnitude;

            // bounce only horizontal forces
            if (Mathf.Abs(colNormal.y) < Mathf.Abs(colNormal.x) || Mathf.Abs(colNormal.y) < Mathf.Abs(colNormal.z))
            {
                
                float collisionAngle = Mathf.Rad2Deg * Mathf.Abs(Mathf.Acos(Vector3.Dot(-vp.rb.velocity.normalized, colNormal)));
                collisionAngle %= 90f;
                float mult;
                d_collAngle = collisionAngle;
                if (collisionAngle >= 45)
                    mult = 1 + (45-collisionAngle) / 45;
                else
                    mult = collisionAngle / 45;
                
                float energy = velocityMagn * mult;
                if (energy < minimalVelocityAtWall) // velocity directed at the wall
                    return;
                //float currentAngleSteer = Mathf.Sign(vp.steerInput) * vp.wheels[1].suspensionParent.steerAngle * vp.wheels[1].suspensionParent.steerDegrees;
                //Vector3 intendedDirection = Quaternion.AngleAxis(currentAngleSteer, vp.upDir) * vp.forwardDir;
                Vector3 intendedDirection = (vp.forwardDir + 0.4f * vp.upDir).normalized;
                rb.AddForce(velocityMagn * intendedDirection, ForceMode.VelocityChange);
            }
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        vp.colliding = false;
    }
}
