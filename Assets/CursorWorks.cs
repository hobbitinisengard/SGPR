using System.Collections;
using UnityEngine;

public class CursorWorks : MonoBehaviour
{
	Coroutine hideCursorCo;
	Vector2 prevPos;
	void Update()
	{
		var pos = F.I.pointRef.action.ReadValue<Vector2>();
		if (pos == prevPos)
		{
			hideCursorCo ??= StartCoroutine(HideCursor());
		}
		else
		{
			if (hideCursorCo != null)
			{
				StopCoroutine(hideCursorCo);
				hideCursorCo = null;
				Cursor.visible = true;
			}
		}
		prevPos = pos;
	}

	private IEnumerator HideCursor()
	{
		yield return new WaitForSeconds(3);
		Cursor.visible = false;
	}
}
