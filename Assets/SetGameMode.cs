using UnityEngine;

public class SetGameMode : MonoBehaviour
{
   public GameMode setGameModeToThis;
   public void Set()
   {
      Info.gameMode = setGameModeToThis;
   }
}
