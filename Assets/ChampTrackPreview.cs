using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ChampTrackPreview : MainMenuView
{
	public Image trackImage;
	public Transform recordsContainer;
	public Text trackDescText;
	public Text trackAuthorText;
	public Transform tilesContainer;
	bool launching;

	protected override void OnEnable()
	{
		base.OnEnable(); launching = false; prevView = ChampionshipUI.View("ChampionshipsView");
		if (Championships.NextTrack == null) return;
		F.I.s_trackName = Championships.NextTrack;
		var track = F.I.tracks[F.I.s_trackName];
		ChampionshipUI.SetText(trackDescText.gameObject, track.LocalizedName + "\n\n" + track.LocalizedDesc);
		ChampionshipUI.SetText(trackAuthorText.gameObject, track.author);
		trackImage.sprite = IMG2Sprite.LoadNewSprite(F.I.tracksPath + F.I.s_trackName + ".jpg");
		SetTiles();
		for (int i = 0; recordsContainer && i < recordsContainer.childCount && i < 4; i++)
		{
			var row = recordsContainer.GetChild(i); int recordIndex = row.name == "RACE" ? 1 : row.name == "LAP" ? 0 : row.name == "DRIFT" ? 3 : 2; var record = track.records[recordIndex];
			if (row.childCount > 1) ChampionshipUI.SetText(row.GetChild(1).gameObject, record?.playerName ?? "");
			if (row.childCount > 2) ChampionshipUI.SetText(row.GetChild(2).gameObject, record == null || record.secondsOrPts <= 0 ? "" : recordIndex < 2 ? TimeSpan.FromSeconds(record.secondsOrPts).ToLaptimeStr() : record.secondsOrPts.ToString("N0"));
		}
	}
	public void Race() { if (!launching && Championships.PrepareRace()) { launching = true; ToRaceScene(); } }

	protected void SetTiles()
	{
		void AddTile(string spriteName)
		{
			var tile = Instantiate(tilesContainer.GetChild(0).gameObject, tilesContainer);
			tile.SetActive(true);
			tile.name = spriteName;
			try
			{
				tile.GetComponent<Image>().sprite = F.I.icons.First(i => i.name == spriteName);
			}
			catch
			{
				Debug.LogError(spriteName);
			}
		}

		for (int i = 1; i < tilesContainer.childCount; ++i)
			Destroy(tilesContainer.GetChild(i).gameObject);


		AddTile(Enum.GetName(typeof(CarGroup), F.I.tracks[F.I.s_trackName].preferredCarClass));
		AddTile(Enum.GetName(typeof(Envir), F.I.tracks[F.I.s_trackName].envir));
		AddTile((F.I.tracks[F.I.s_trackName].difficulty).ToString());
		foreach (var flag in F.I.tracks[F.I.s_trackName].icons)
			AddTile(F.I.IconNames[flag]);
		tilesContainer.gameObject.SetActive(true);
	}
}
