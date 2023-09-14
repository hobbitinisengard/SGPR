using UnityEngine;

public class SC_EditorFlyCamera : MonoBehaviour
{
	public float moveSpeed = 15;
	public float turnSpeed = 3;

	bool freeLook = false;
	bool moveFast = false;
	float rotationY;

	// Use this for initialization
	void Start()
	{
		rotationY = -transform.localEulerAngles.x;
	}

	// Update is called once per frame
	void Update()
	{
		Movement();
	}

	void Movement()
	{
		moveFast = Input.GetKey(KeyCode.LeftShift);

		float speed = moveSpeed * Time.deltaTime * (moveFast ? 3 : 1);

		if (Input.GetKey(KeyCode.W))
		{
			transform.root.Translate(transform.forward * speed, Space.World);
		}
		if (Input.GetKey(KeyCode.S))
		{
			transform.root.Translate(-transform.forward * speed, Space.World);
		}
		if (Input.GetKey(KeyCode.A))
		{
			transform.root.Translate(-transform.right * speed, Space.World);
		}
		if (!Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.D))
		{
			transform.root.Translate(transform.right * speed, Space.World);
		}
		if (Input.GetKey(KeyCode.Q))
		{
			transform.root.Translate(transform.up * speed, Space.World);
		}
		if (Input.GetKey(KeyCode.E))
		{
			transform.root.Translate(-transform.up * speed, Space.World);
		}

		if (Input.GetMouseButtonDown(1))
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		freeLook = Input.GetMouseButton(1);
		if (Input.GetMouseButtonUp(1))
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		if (freeLook)
		{
			float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * turnSpeed;

			rotationY += Input.GetAxis("Mouse Y") * turnSpeed;
			rotationY = Mathf.Clamp(rotationY, -90, 90);

			transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
		}
	}
}