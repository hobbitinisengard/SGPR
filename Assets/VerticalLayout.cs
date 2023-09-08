using UnityEngine;
using UnityEngine.UI;

public class VerticalLayout : LayoutGroup
{
	public float offset;
	protected override void OnEnable() { base.OnEnable(); Calculate(); }
	public override void SetLayoutHorizontal()
	{
	}
	public override void SetLayoutVertical()
	{
	}
	public override void CalculateLayoutInputVertical()
	{
		Calculate();
	}
	//public override void CalculateLayoutInputHorizontal()
	//{
	//	Calculate();
	//}
#if UNITY_EDITOR
	protected override void OnValidate()
	{
		base.OnValidate();
		Calculate();
	}
#endif

	protected override void OnDisable()
	{
		m_Tracker.Clear(); // key change - do not restore - false
		LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
	}

	void Calculate()
	{
		m_Tracker.Clear();
		if (transform.childCount == 0)
			return;

		int activeCount = 0;
		for (int i = 0; i < transform.childCount; i++)
		{
			RectTransform child = (RectTransform)transform.GetChild(i);
			if ((child != null) && child.gameObject.activeSelf)
			{
				//Adding the elements to the tracker stops the user from modifying their positions via the editor.
				m_Tracker.Add(this, child,
				DrivenTransformProperties.Anchors |
				DrivenTransformProperties.AnchoredPositionY |
				DrivenTransformProperties.Pivot);
				child.anchoredPosition = new Vector3(child.anchoredPosition.x, 
					-(offset+activeCount * child.rect.height));
				child.anchorMin = child.anchorMax = child.pivot = new Vector2(0, 1);
				activeCount++;
			}
		}
	}
}
