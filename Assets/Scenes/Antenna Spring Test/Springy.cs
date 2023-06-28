using RVP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]//Diverts the selection to this object
public class Springy : MonoBehaviour
{
    Transform springTarget;
    Transform springObj;
    VehicleParent vp;
    public float drag = 2.5f;//drag
    public float springForce = 80.0f;//Spring
    float internalForce = 1;
    public float targetHeight = 0.26f; // how high above this should target be located?

    public Springy prevSpringy;
    public float d_distance = 0;
    public float d_velocity = 0;
    private Vector3 prevDistance;
    private Vector3 prevLocalVelocity;
    Rigidbody SpringRB;
    private Vector3 distance;//Distance between the two points
    private Vector3 LocalVelocity;//Velocity converted to local space
    private Vector3 smooth_vel;

    Vector3 ElevateY(Vector3 pos, float height)
    {
        //return pos + vp.upDir * height;
        return new Vector3(pos.x, pos.y + height, pos.z);
    }
    void Start()
    {
        vp = transform.GetTopmostParentComponent<VehicleParent>();
        if (prevSpringy)
            transform.position = prevSpringy.Ending();
        springTarget = new GameObject(this.name + "_target").transform;
        springTarget.parent = transform.parent;
        springTarget.position = ElevateY(transform.position, targetHeight);

        springObj = new GameObject(this.name + "_spring").transform;
        springObj.gameObject.layer = 2;
        springObj.position = springTarget.position;
        //springObj.parent = transform.parent;
        SpringRB = springObj.gameObject.AddComponent<Rigidbody>();//Find the RigidBody component
        SpringRB.useGravity = false;
    }
    void FixedUpdate()
    {
        //Sync the rotation 
        //SpringRB.transform.rotation = transform.rotation;
        if(prevSpringy)
        {
            transform.position = prevSpringy.Ending();
        }
        springTarget.position = ElevateY(transform.position, targetHeight);
        //Calculate the distance between the two points
        distance = springTarget.position - springObj.position;
        //Vector3 a = distance - prevDistance;
        internalForce = springForce;// * Mathf.Clamp01(distance.magnitude / targetHeight);
        SpringRB.AddForce(distance * internalForce); // Apply spring
        LocalVelocity = SpringRB.velocity;
        //a = LocalVelocity - prevLocalVelocity;
        SpringRB.AddForce(-LocalVelocity * drag);//Apply drag
        //springObj.localPosition = Vector3.SmoothDamp(springObj.localPosition, springTarget.localPosition,
        //    ref smooth_vel, 0.0f,0.3f,Time.fixedDeltaTime);

        transform.LookAt(springObj.position);

        d_distance = distance.magnitude;
        d_velocity = LocalVelocity.magnitude;
        prevDistance = distance;
        prevLocalVelocity = LocalVelocity;
    }
    public Vector3 Ending()
    {
        if (springObj == null)
            return new Vector3(transform.position.x, transform.position.y + targetHeight, transform.position.z);
        //return transform.position + transform.rotation * Vector3.forward * targetHeight;
        return transform.position + (springObj.position - transform.position).normalized * targetHeight;//transform.rotation * Vector3.forward * targetHeight;
    }
}
