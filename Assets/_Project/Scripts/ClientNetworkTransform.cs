using Unity.Netcode.Components;
using UnityEngine;
public enum AuthorityMode
{
	Server,
	Client
}

[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
	public AuthorityMode authorityMode = AuthorityMode.Client;
	protected override bool OnIsServerAuthoritative() => authorityMode == AuthorityMode.Server;
	bool zeroize;
	protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
	{
		base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
		base.Update();
	}
	protected override void Update()
	{

		
	}
	private void LateUpdate()
	{
		//transform.position = Vector3.zero;
	}
}
