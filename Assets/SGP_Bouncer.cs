using RVP;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
    static float shockScale = 0.01f;
    float maxShock = 100;

    ContactPoint[] contacts = new ContactPoint[20];
    VehicleParent vp;
    public float lastBounceTime;
    public float lastCarCarBounceTime;
    public float lastSideBounceTime;
    float rotationalFrictionScale = 0.05f;
    public float shockThreshold = 0.25f;
    //public float timeDelay = .4f;
    int rbId;
    static AnimationCurve multCurve;
    public Collider[] bouncyCols;

    static Dictionary<int, VehicleParent> carRbs = new(10);
    static bool OnContactModifyRegistered = false;
    readonly static float[] colRestitutionTable = new float[] {
         0.000000f, 0.000000f, 0.000000f, 0.000000f, 0.000000f, 0.000000f, 0.000000f, 0.000000f,
         0.000000f, 0.000567f, 0.001135f, 0.001702f, 0.002270f, 0.002837f, 0.003405f, 0.003972f,
         0.004539f, 0.007853f, 0.011166f, 0.014479f, 0.017793f, 0.021106f, 0.024419f, 0.027733f,
         0.031046f, 0.044842f, 0.058639f, 0.072435f, 0.086232f, 0.100029f, 0.113825f, 0.127622f,
         0.141418f, 0.168396f, 0.195375f, 0.222353f, 0.249331f, 0.276310f, 0.303288f, 0.330266f,
         0.357244f, 0.386774f, 0.416303f, 0.445833f, 0.475362f, 0.498690f, 0.522018f, 0.545346f,
         0.568673f, 0.587365f, 0.606056f, 0.624748f, 0.643439f, 0.662131f, 0.680822f, 0.699514f,
         0.718205f, 0.733274f, 0.748342f, 0.763410f, 0.778478f, 0.793546f, 0.808615f, 0.823683f,
         0.838751f, 0.850874f, 0.862996f, 0.875119f, 0.887242f, 0.894770f, 0.902297f, 0.909825f,
         0.917353f, 0.924881f, 0.932408f, 0.939936f, 0.947464f, 0.951480f, 0.955497f, 0.959514f,
         0.963531f, 0.967547f, 0.971564f, 0.975581f, 0.979597f, 0.984698f, 0.989799f, 0.994899f,
         1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f,
         1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f,
         1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f,
         1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f,
         1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f,
    }; // from GAME\CONFIG\LEVELS\DEFAULT.CFG

    void Awake()
    {
        vp = GetComponent<VehicleParent>();
        rbId = vp.rb.GetInstanceID();
        carRbs.Add(rbId, vp);

        if (multCurve == null)
        {
            Keyframe[] kf = new Keyframe[]
            {
                new (Mathf.Cos((-90)*Mathf.Deg2Rad),1),
                new (Mathf.Cos((0)*Mathf.Deg2Rad),0),
                new (Mathf.Cos((90)*Mathf.Deg2Rad),1),
            };
            multCurve = new AnimationCurve(kf);
        }
        for (int i = 0; i < bouncyCols.Length; i++)
        {
            bouncyCols[i].hasModifiableContacts = true;
        }

        if (!OnContactModifyRegistered)
        {
            OnContactModifyRegistered = true;
            Physics.ContactModifyEvent += OnContactModify;
        }
    }
    static void OnContactModify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
    {
        foreach (var pair in pairs)
        {
            if (/*pair.bodyInstanceID != 0 && pair.otherBodyInstanceID != 0 &&*/
                carRbs.ContainsKey(pair.bodyInstanceID) && carRbs.ContainsKey(pair.otherBodyInstanceID)) // car-car collisions
            {
                if (pair.contactCount > 0)
                {
                    pair.SetPoint(0, carRbs[pair.otherBodyInstanceID].worldCOM);
                    pair.SetNormal(0, (pair.GetNormal(0) + Vector3.up)/2f);
                    for (int i = 1; i < pair.contactCount; ++i)
                    {
                        pair.IgnoreContact(i);
                    }
                }
            }
        }
    }
    private void OnDestroy()
    {
        carRbs.Remove(rbId);
    }

    /*collision_energy_min,0.25,"Range(0,100) Min energy for a single impact"
collision_energy_max,0.5,"Range(0,100) Max energy for a single impact"
collision_energy_scale,0.01,Single impact energy scale
collision_energy_impact_limit,0.75,"Range(0,100) Max impact energy after multiple collisions per time"
collision_energy_impact_timedelay,0.4,"Range(0, 1) Time in Seconds"
	 */
    void ApplyShock(Vector3 direction, float impactStrength)
    {
        float maxShock = 0.75f;
        float shockScale = 0.01f;
        float shockMagnitude = Mathf.Min(impactStrength * shockScale * impactStrength * shockScale, maxShock);
        Vector3 shockForce = direction * shockMagnitude;
        vp.rb.AddForce(shockForce * 4, ForceMode.VelocityChange);
    }
    float GetRestitution(float impactStrength01)
    {
        int tableSize = colRestitutionTable.Length;
        int index = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(impactStrength01) * (tableSize - 1)), 0, tableSize - 1);
        return colRestitutionTable[index];
    }
    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionStay(collision);
    }
    private void OnCollisionStay(Collision col)
    {
        int contactsNr = col.GetContacts(contacts);
        if (contacts[0].otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
            return;
        if (CountDownSeq.Countdown > 0)
            return;
        Vector3 collisionNormal = Vector3.zero;
        for (int i = 0; i < contactsNr; i++)
        {
            collisionNormal += contacts[i].normal;
        }
        collisionNormal /= contactsNr;
        //collisionNormal = (vp.rb.worldCenterOfMass - collisionNormal).normalized;

        //if (contacts[0].otherCollider.gameObject.layer != F.I.carCarCollisionLayer)
        {
            // HandleParticleToSceneCollision__FUi

            // bounce
            float impactStrength = Vector3.Dot(vp.rb.linearVelocity, collisionNormal);
            //float restitution = GetRestitution(impactStrength); // e.g., from a curve or table
            //vp.rb.AddForce((collisionNormal + Vector3.up)/2f * impactStrength * restitution, ForceMode.VelocityChange);
            //vp.rb.linearVelocity += Vector3.Reflect(vp.rb.linearVelocity, -collisionNormal) * restitution;

            // rotational impulse
            float rotationalImpulse = impactStrength * rotationalFrictionScale;
            vp.rb.AddTorque(-collisionNormal * rotationalImpulse, ForceMode.VelocityChange);

            //Debug.Log(impactStrength.ToString("F2"));
            // shock
            if (Mathf.Abs(impactStrength) > shockThreshold)
            {
                float shockMagnitude = Mathf.Min(impactStrength * shockScale * impactStrength * shockScale, maxShock);
                Vector3 shockForce = collisionNormal * shockMagnitude;
                vp.rb.AddForce(shockForce, ForceMode.VelocityChange);
            }
        }
        //else
        {
            //HandleCarToCarCollisionImpact__Fv

            //Vector3 collisionDir = (col.body.transform.position - transform.position).normalized;
            //float impactStrength = Vector3.Dot(col.relativeVelocity, collisionDir);
            //ApplyShock(collisionDir, impactStrength);

        }
        vp.colliding = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        vp.colliding = false;
    }
}

//private void OnCollisionEnter(Collision col)
//{
//    ContactPoint contact = col.GetContact(0);
//    if (contact.otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
//        return;
//    if (CountDownSeq.Countdown > 0)
//        return;
//    //if (contact.otherCollider.gameObject.layer != F.I.roadLayer)
//    //	return;
//    //if (contact.otherCollider.gameObject.name.Contains("slope"))
//    //	return;


//    vp.colliding = true;
//    Vector3 norm = contact.normal;
//    float upNormDot = Vector3.Dot(vp.tr.up, norm);

//    {
//        //collision_energy_min,0.25,"Range(0,100) Min energy for a single impact"
//        //collision_energy_max,0.5,"Range(0,100) Max energy for a single impact"
//        //collision_energy_scale,0.01,Single impact energy scale
//        //collision_energy_impact_limit,0.75,"Range(0,100) Max impact energy after multiple collisions per time"
//        //collision_energy_impact_timedelay,0.4,"Range(0, 1) Time in Seconds"

//        //if (col.relativeVelocity.magnitude < 0.25f || col.relativeVelocity.magnitude > 0.5)
//        //	return;

//        //if (Time.time - lastSideBounceTime < timeDelay)
//        //	return;

//        //lastSideBounceTime = Time.time;
//        Vector3 collisionDir = (col.body.transform.position - transform.position).normalized;
//        float impactStrength = Vector3.Dot(col.relativeVelocity, collisionDir);

//        // speed-up on collision
//        //vp.rb.AddForceAtPosition(Vector3.ProjectOnPlane(-col.impulse, Vector3.up),
//        //	col.GetContact(0).point,
//        //	ForceMode.VelocityChange);

//        // up force on collision
//        //vp.rb.AddForceAtPosition(-col.relativeVelocity.magnitude * vecGain * vp.upDir,
//        //	col.GetContact(0).point,
//        //	ForceMode.Acceleration);

//        if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
//        {
//            // collision car to car - project on plane 
//            // + force up
//            //vec = Vector3.ProjectOnPlane(-col.relativeVelocity, Vector3.up);
//            //vec += Vector3.up * col.relativeVelocity.magnitude;
//            //            vp.rb.AddForceAtPosition(vec,
//            //	vp.rb.worldCenterOfMass,//col.GetContact(0).point,
//            //	ForceMode.VelocityChange);

//            //mult = 1f;
//            //direction = Vector3.ProjectOnPlane(-collision.impulse.normalized, Vector3.up);
//            ////direction = norm;
//            //rb.AddForceAtPosition(collision.impulse.magnitude * mult * direction,
//            //collision.GetContact(0).point,//vp.transform.position
//            //ForceMode.VelocityChange);
//        }
//        else
//        {
//            //vp.rb.mass = Mathf.Lerp(vp.originalMass,minimumMass, vp.velMag / 60);

//            //	if (col.impulse.magnitude < 40)
//            //		return;

//            //	// workaround for unity slingshot
//            //if (upNormDot < .1f && upNormDot > -.5f) // angle between 84d and 135d
//            {
//                //mult = multCurve.Evaluate(Vector3.Dot(norm, vp.tr.forward));

//                //mult = vp.rb.mass / vp.originalMass;
//                //vec = Vector3.ProjectOnPlane(-col.impulse / mult, Vector3.up);
//                //vp.rb.AddForceAtPosition(vec,
//                //	col.GetContact(0).point,
//                //	ForceMode.Impulse);

//                ////Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
//                //Vector3 addForce = mult * col.relativeVelocity;
//                //Vector3 direction = (vp.tr.forward + norm + vp.tr.up).normalized;
//                //lastSideBounceTime = Time.time;
//                //vp.rb.AddForceAtPosition(direction * addForce.magnitude,
//                //col.GetContact(0).point,//vp.transform.position
//                //ForceMode.VelocityChange);
//            }
//        }

//        //Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
//    }
//}
