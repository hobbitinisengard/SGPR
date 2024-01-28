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
		// For each contact pair, ignore the contact points that are close to origin
		//foreach (ModifiableContactPair pair in pairs)
		//{
		//	for (int i = 0; i < pair.contactCount; ++i)
		//		pair.
		//		if (Vector3.Distance(pair.GetPoint(i), Vector3.zero) < IgnoredRadius)
		//			pair.IgnoreContact(i);
		//}
	}
	private void OnDestroy()
	{
		Physics.ContactModifyEvent -= ModificationEvent;
	}
}
