using UnityEngine.EventSystems;

public class ArcadeButton : Sfxable, ISelectHandler
{
	public void OnSelect(BaseEventData eventData)
	{
		PlaySFX("fe-dialogmove");
	}
}
