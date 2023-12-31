public class ButtonContainerAudio : Sfxable
{
	private void OnDisable()
	{
		PlaySFX("fe-menudisappear");
	}
	private void OnEnable()
	{
		PlaySFX("fe-menuappear");
	}
}
