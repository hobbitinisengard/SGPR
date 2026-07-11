using UnityEngine;
[DisallowMultipleComponent]
public class SetGameMode : MonoBehaviour
{
   public GameMode setGameModeToThis;
   public void Set()
   {
      F.I.gameMode = setGameModeToThis;
   }
}
