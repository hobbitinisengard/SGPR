using RVP;
using UnityEngine;
public enum InfoMessage
{
    TAKES_THE_LEAD, NO_ENERGY, SPLIT_TIME,
}

public class RaceManager : MonoBehaviour
{
    public VehicleParent[] cars;
    bool initialized = false;
    int position = 1;
    public float trackDistance = 1000;
    public SGP_HUD hud;
    public bool Initialized()
    {
        return initialized;
    }
    public void Initialize(VehicleParent[] cars)
    {
        this.cars = cars;
        initialized = true;
    }
    void Update()
    {
        // DEBUG
        if (Input.GetKeyDown(KeyCode.K))
            hud.AddMessage(new Message("CP1 IS RECHARGING!" + 5*Random.value, BottomInfoType.PIT_IN));
        if (Input.GetKeyDown(KeyCode.M))
            hud.AddMessage(new Message("CP2 TAKES THE LEAD!" + 5 * Random.value, BottomInfoType.NEW_LEADER));
    }
    public int Position(VehicleParent vp)
    {
        //// DEBUG
        //if (Input.GetKeyDown(KeyCode.K))
        //    position++;
        //if (Input.GetKeyDown(KeyCode.M))
        //    position--;
        position = Mathf.Clamp(position, 1, 10);
        return position;
    }
}
