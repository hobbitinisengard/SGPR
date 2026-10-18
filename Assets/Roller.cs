
using System.Collections;
using UnityEngine;

public class Roller : MonoBehaviour
{
	RectTransform rt;

	float height;
	float pos0;
	float scale;

	// 1 = oryginalna odleg³oœæ przewijania.
	// Ustaw tak, aby koñcowe 0 pokrywa³o siê
	// dok³adnie z pocz¹tkowym 0.
	//[SerializeField, Range(0.8f, 1f)]
	float wrapPoint = 10f / 11f;

	public float target = 0f;
	public float current = 0f;
	public float coeff = 10f;

	bool clockWork = true;
	bool initialized;
	Coroutine playCo;

	const float Tolerance = 0.0001f;

	void Awake()
	{
		Initialize();
	}

	void Initialize()
	{
		if (initialized) return;

		rt = GetComponent<RectTransform>();

		height = rt.sizeDelta.y;
		pos0 = rt.anchoredPosition.y;
		scale = rt.localScale.y;

		initialized = true;
	}

	public void InitializeClone(Roller source)
	{
		source.Initialize();

		rt = GetComponent<RectTransform>();

		height = source.height;
		pos0 = source.pos0;
		scale = source.scale;
		wrapPoint = source.wrapPoint;

		current = 0f;
		target = 0f;

		initialized = true;

		UpdatePosition();
	}

	void OnEnable()
	{
		Initialize();
		PlayAnim();
	}

	void OnDisable()
	{
		playCo = null;
	}

	void UpdatePosition()
	{
		// Logiczny cykl zawsze ma d³ugoœæ 1.
		float visual = Mathf.Repeat(current, 1f);

		// Korekta fizycznej d³ugoœci przewijania.
		float position = visual * wrapPoint;

		Vector2 pos = rt.anchoredPosition;

		pos.y = pos0 + scale * height * position;

		rt.anchoredPosition = pos;
	}

	IEnumerator Move()
	{
		while (Mathf.Abs(current - target) > Tolerance)
		{
			if (!clockWork)
			{
				current = target;
			}
			else
			{
				// P³ynne wyhamowywanie niezale¿ne
				// od liczby klatek na sekundê.
				float step =
						1f - Mathf.Exp(-coeff * Time.deltaTime);

				current = Mathf.Lerp(
						current,
						target,
						step
				);
			}

			UpdatePosition();

			yield return null;
		}

		current = target;
		UpdatePosition();

		playCo = null;
	}

	public void SetActive(bool value)
	{
		gameObject.SetActive(value);
	}

	public void SetValue(float newTarget)
	{
		Initialize();

		newTarget = Mathf.Clamp(newTarget, 0f, 9f);
		clockWork = true;

		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		float normalized = newTarget / 10f;

		// Aktualny cykl animacji.
		float cycle = Mathf.Floor(current);

		float nextTarget = cycle + normalized;

		// Przewijanie wy³¹cznie do przodu.
		if (nextTarget < current - Tolerance)
			nextTarget += 1f;

		// Unikamy niepotrzebnego restartu.
		if (Mathf.Abs(nextTarget - target) <= Tolerance)
			return;

		target = nextTarget;

		PlayAnim();
	}

	public void SetFrac(float frac)
	{
		Initialize();

		clockWork = false;

		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		target = frac;

		PlayAnim();
	}

	void PlayAnim()
	{
		if (!enabled || !gameObject.activeInHierarchy)
			return;

		if (playCo != null)
		{
			StopCoroutine(playCo);
			playCo = null;
		}

		playCo = StartCoroutine(Move());
	}
}
