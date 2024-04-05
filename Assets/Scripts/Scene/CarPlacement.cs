
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class CarPlacement
{
	/// <summary>
	/// 0 = pole position, 9 = last position
	/// </summary>
	public int position;
	public ulong PlayerId;
	/// <summary>
	/// from 0 to 19
	/// </summary>
	public string carName;
	public string name;
	public Livery sponsor;
	public static CarPlacement CPU(int pos, in List<int> preferredCars)
	{
		return new CarPlacement() {
			carName = "car" + preferredCars.GetRandom().ToString("D2"),
			PlayerId = 0,
			position = pos,
			name = "CP" + (pos + 1).ToString(),
			sponsor = (Livery)(UnityEngine.Random.Range(0, F.I.Liveries) + 1),
		};
	}
	public static CarPlacement LocalPlayer()
	{
		return new CarPlacement()
		{
			carName = F.I.s_playerCarName,
			PlayerId = 0,
			position = F.I.s_cpuRivals,
			name = F.I.playerData.playerName,
			sponsor = (Livery)(UnityEngine.Random.Range(0, F.I.Liveries) + 1),
		};
	}
	public static CarPlacement OnlinePlayer(int pos, Player p)
	{
		var pIndex = pos - F.I.s_cpuRivals;
		return new CarPlacement()
		{
			carName = p.carNameGet(),
			PlayerId = F.I.ActivePlayers.Find(p => p.playerLobbyId == MultiPlayerSelector.I.server.lobby.Players[pIndex].Id).playerRelayId,
			position = (F.I.s_cpuRivals + pIndex),
			name = p.NameGet(),
			sponsor = p.SponsorGet(),
		};
	}
}
