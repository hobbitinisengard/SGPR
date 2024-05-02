using UnityEngine;

public class SetGarageType : MonoBehaviour
{
   public GarageType garageType;
   public CarSelector carSelector;
   public void Set()
   {
      carSelector.SetType(garageType);
   }
}
