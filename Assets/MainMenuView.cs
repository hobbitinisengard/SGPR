using UnityEngine;
using UnityEngine.UI;
using RVP;
using System;

public class MainMenuView : MonoBehaviour
{
	[NonSerialized]
	public GameObject prevView;
	public Image dyndak;
	public Button firstButtonToBeSelected;
	public Text bottomText;
	private void OnEnable()
	{
		firstButtonToBeSelected.Select();
	}
	void Update()
	{
		if (prevView && Input.GetKeyDown(KeyCode.Escape))
		{
			GoToView(prevView);
		}
	}
	public void GoToView(GameObject view)
	{
		for(int i=0; i< transform.childCount; ++i)
		{
			Check(transform.GetChild(i));
		}
		var dimmer = transform.FindParentComponent<Dimmer>();
		dimmer.PlayDimmer(gameObject, view);
	}
	void Check(Transform node)
	{
		var comp = node.GetComponent<SlideInOut>();
		if (comp)
		{
			comp.PlaySlideOut();
		}
		else
		{
			for (int i = node.transform.childCount - 1; i >= 0; --i)
			{
				Check(node.transform.GetChild(i));
			}
		}
	}
}
