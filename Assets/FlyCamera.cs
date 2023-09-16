using System.Runtime.InteropServices;
using UnityEngine;
//Logic of camera movements
public class FlyCamera : MonoBehaviour
{
	[DllImport("user32.dll")]
	static extern bool SetCursorPos(int X, int Y);
	public float mainSpeed;// = 20; //regular speed
	public float shiftAdd;// = 50; //multiplied by how long shift is held. Basically running
	public float maxShift;// = 100; //Maximum speed when holding shift
	public float ctrlAdd;// = 8; //multiplied by how long ctrl is held. Basically running
	public float maxCtrl;// = 5; //Maximum speed when holding ctrl
	public float camSens;// = 0.25f; //How sensitive it with mouse
	private Vector3 lastMouse = new Vector3(0, 0, 0); //kind of in the middle of the screen, rather than at the top (play)
	private float totalRun = 1.0f;
	private int flag = 0;

	void Update()
	{
		if (Input.GetKeyUp(KeyCode.BackQuote))
		{
			float posY = this.transform.position.y;
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(r.origin, r.direction, out RaycastHit hit, Mathf.Infinity))
			{ // if casting elevated element from underground, raise camera up to it

				Vector3 hPos = hit.transform.position;
				hPos.y += 20;
				this.transform.position = hPos;
			}
		}
		if (Input.GetKey(KeyCode.PageUp))
		{
			Vector3 pos = transform.position;
			pos.y += 10f;
			transform.position = pos;
		}
		if (Input.GetKey(KeyCode.PageDown))
		{
			Vector3 pos = transform.position;
			pos.y -= 10f;
			transform.position = pos;
		}
		Ordinarycamera();
	}
	void Ordinarycamera()
	{
		GetComponent<Camera>().orthographic = false;

		if (Input.GetKey(KeyCode.Space))
		{
			//Debug.Log ("input:"+Input.mousePosition);
			//Debug.Log ("last:"+lastMouse);
			if (flag == 0)
			{
				lastMouse = Input.mousePosition - lastMouse;
			}
			else
			{
				lastMouse.x = 0;
				lastMouse.y = 0;
				lastMouse.z = 0;
				flag = 0;
			}
			lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0); //Obrót bie¿¹cy
			lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x, transform.eulerAngles.y + lastMouse.y, 0);
			transform.eulerAngles = lastMouse;
			if (Input.mousePosition.x < 0.232 * Screen.width || Input.mousePosition.x > Screen.width - 10 || Input.mousePosition.y < 0.094 * Screen.height || Input.mousePosition.y > Screen.height - 10)
			{
				SetCursorPos(Mathf.RoundToInt(Screen.width / 2f), Mathf.RoundToInt((Screen.height / 2f)));
				flag = 1;
			}

		}
		lastMouse = Input.mousePosition;
		//Ruch
		Vector3 p = GetBaseInput();
		if (Input.GetKey(KeyCode.LeftShift))
		{
			//totalRun += Time.deltaTime;
			p *= shiftAdd;
			p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
			p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
			p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
		}
		else if (Input.GetKey(KeyCode.LeftControl))
		{
			//totalRun += Time.deltaTime;
			p *= ctrlAdd;
			p.x = Mathf.Clamp(p.x, -maxCtrl, maxCtrl);
			p.y = Mathf.Clamp(p.y, -maxCtrl, maxCtrl);
			p.z = Mathf.Clamp(p.z, -maxCtrl, maxCtrl);
		}
		else
		{
			totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
			p *= mainSpeed;
		}

		//Lock to XZ plane
		p = p * Time.deltaTime;
		Vector3 newPosition = transform.position;
		if (Input.GetKey(KeyCode.LeftAlt))
		{
			transform.Translate(p);
			newPosition.x = transform.position.x;
			newPosition.z = transform.position.z;
			transform.position = newPosition;
		}
		else
		{
			transform.Translate(p);
		}
	}
	private Vector3 GetBaseInput()
	{
		Vector3 p_Velocity = new Vector3();
		
		if (Input.GetKey(KeyCode.W))
		{
			p_Velocity += new Vector3(0, 0, 1);
		}
		if (Input.GetKey(KeyCode.S))
		{
			p_Velocity += new Vector3(0, 0, -1);
		}
		if (Input.GetKey(KeyCode.A))
		{
			p_Velocity += new Vector3(-1, 0, 0);
		}
		if (Input.GetKey(KeyCode.D))
		{
			p_Velocity += new Vector3(1, 0, 0);
		}

		return p_Velocity;
	}
}