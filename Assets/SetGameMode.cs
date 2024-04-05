using UnityEngine;

public class SetGameMode : MonoBehaviour
{
   public MultiMode setGameModeToThis;
   public void Set()
   {
      F.I.gameMode = setGameModeToThis;
   }
}
