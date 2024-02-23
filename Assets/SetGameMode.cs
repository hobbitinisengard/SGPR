using UnityEngine;

public class SetGameMode : MonoBehaviour
{
   public MultiMode setGameModeToThis;
   public void Set()
   {
      Info.gameMode = setGameModeToThis;
   }
}
