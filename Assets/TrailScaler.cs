using RVP;
using UnityEngine;

public class TrailScaler : MonoBehaviour
{
    VehicleParent vp;
    // Start is called before the first frame update
    void Start()
    {
        vp = transform.GetTopmostParentComponent<VehicleParent>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = Vector3.one * (0.5f * Mathf.Clamp(vp.velMag, 0, 110)/110);
    }
}
