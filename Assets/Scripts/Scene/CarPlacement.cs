
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class CarPlacement
{
	/// <summary>
	/// 0 = pole position, 9 = last position
	/// </summary>
	public int position;
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
			position = pos,
			name = "CP" + (pos + 1).ToString(),
			sponsor = F.RandomLivery(),
		};
	}
	public static CarPlacement LocalPlayer()
	{
		return new CarPlacement()
		{
			carName = F.I.s_playerCarName,
			position = F.I.s_cpuRivals,
			name = F.I.playerData.playerName,
			sponsor = (F.I.s_PlayerCarSponsor == Livery.Random) ? F.RandomLivery() : F.I.s_PlayerCarSponsor,
		};
	}
	public static CarPlacement OnlinePlayer(int pos, Player p)
	{
		var pIndex = pos - F.I.s_cpuRivals;
		return new CarPlacement()
		{
			carName = p.carNameGet(),
			position = (F.I.s_cpuRivals + pIndex),
			name = p.NameGet(),
			sponsor = p.SponsorGet(),
		};
	}
}
