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
	public global::AuthorityMode authorityMode = global::AuthorityMode.Client;
	protected override bool OnIsServerAuthoritative() => authorityMode == global::AuthorityMode.Server;
	// NetworkTransform now performs interpolation through its OnUpdate callback.
}
