using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
public class MouseLook : MonoBehaviour
{
	[Header("Camera")]
    public CinemachineCamera playerCamera;
	[Header("ActionSet")]
    public InputAction look;
	[Header("Mouse Settings")]
	public float mouseSensitivity = 1;
	public float mouseLookUpLimit = 0;
	public float mouseLookDownLimit = 180;

	private float xRotation = 0;
	private float yRotation = 0;
	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
	}
	private void OnEnable()
	{
		look.Enable();
	}
	private void OnDisable()
	{
		look.Disable();
	}
	private void LateUpdate()
	{
		Vector2 _lookInput = look.ReadValue<Vector2>();
		float _mouseX = _lookInput.x * mouseSensitivity;
		float _mouseY = _lookInput.y * mouseSensitivity;
		xRotation -= _mouseY;
		yRotation += _mouseX;
		xRotation = Mathf.Clamp(xRotation, mouseLookUpLimit,mouseLookDownLimit);
		playerCamera.gameObject.transform.rotation = Quaternion.Euler(xRotation,yRotation,0);
	}
}
