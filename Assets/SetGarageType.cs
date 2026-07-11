using UnityEngine;

public class SetGarageType : MonoBehaviour
{
   public GarageType garageType;
   public void Set()
   {
      F.I.carSelector.SetType(garageType);
   }
}
