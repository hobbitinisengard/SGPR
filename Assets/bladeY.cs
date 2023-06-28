using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bladeY : MonoBehaviour
{
    public float speed = 2f;
    float pos = 0;
    Vector3 init_rot;
    private void Start()
    {
        init_rot = transform.rotation.eulerAngles;
    }
    float degs(float deg)
    {
        if (deg > 360)
            deg -= 360;
        if (deg < 0)
            deg += 360;
        return deg;
    }
    // Update is called once per frame
    void Update()
    {
        pos += speed;
        pos = degs(pos);
        transform.rotation = Quaternion.Euler(init_rot.x, pos, init_rot.z);
    }
}
