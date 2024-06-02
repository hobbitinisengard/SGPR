using RVP;
using System.Collections;
using TMPro;
using UnityEngine;

public class EndraceCountdownTimer : MonoBehaviour
{
   public TextMeshProUGUI text;
   Coroutine cntdown;
   AudioSource snd;
   public Color beginColor;
   public Color endColor;
	private void Awake()
	{
      snd = GetComponent<AudioSource>();
	}
	public void OnEnable()
   {
		if (cntdown != null)
			StopCoroutine(cntdown);
		cntdown = StartCoroutine(EndraceCountdown());
   }
	public void OnDisable()
	{
		gameObject.SetActive(false);
	}
	IEnumerator EndraceCountdown()
   {
      float timer = Info.AfterMultiPlayerRaceWaitForPlayersSeconds;
      int round = Info.AfterMultiPlayerRaceWaitForPlayersSeconds;
      while(timer > 0)
      {
			timer -= Time.deltaTime;
         if(round != Mathf.FloorToInt(timer))
         {
            if(ResultsView.FinishedPlayers >= ServerC.I.lobby.Players.Count)
            {
					gameObject.SetActive(false);
               yield break;
				}
            round = Mathf.FloorToInt(timer);
				text.text = round.ToString();
            if(round < 10)
               snd.Play();
			}
         text.color = Color.Lerp(endColor, beginColor, timer / Info.AfterMultiPlayerRaceWaitForPlayersSeconds);
         yield return null;
      }
		RaceManager.I.TimeForRaceEnded();
		gameObject.SetActive(false);
   }
}
