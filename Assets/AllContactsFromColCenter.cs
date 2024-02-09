using System;
using Unity.Collections;
using UnityEngine;

public class AllContactsFromColCenter : MonoBehaviour
{
	Collider col;
	void Start()
	{
		col = GetComponent<Collider>();
		col.hasModifiableContacts = true;
		Physics.ContactModifyEvent += ModificationEvent;
	}

	private void ModificationEvent(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
	{
		foreach (ModifiableContactPair pair in pairs)
		{
			

		}
	}
	private void OnDestroy()
	{
		Physics.ContactModifyEvent -= ModificationEvent;
	}
}
