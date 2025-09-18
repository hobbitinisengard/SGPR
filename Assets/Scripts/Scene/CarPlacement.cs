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
	public Livery livery = Livery.Random;
	public static CarPlacement CPU(int pos, Car c)
	{
		return new CarPlacement() {
			carName = c.internalName,
			position = pos,
			name = "CP" + (pos + 1).ToString(),
			livery = (Livery)(((int)F.I.s_PlayerCarSponsor + pos) % F.I.Liveries),
		};
	}
	public static CarPlacement LocalPlayer()
	{
		return new CarPlacement()
		{
			carName = F.I.s_playerCarName,
			position = F.I.s_cpuRivals,
			name = F.I.playerData.playerName,
			livery = F.I.s_PlayerCarSponsor,
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
			livery = p.SponsorGet(),
		};
	}
}
