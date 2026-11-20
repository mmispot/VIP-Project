using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
public class MouseLook : MonoBehaviour
{
    public CinemachineCamera playerCamera;
    public InputAction look;
	public float mouseSensitivity;
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
		Vector3 _rotate = new Vector3(-_mouseY, _mouseX,0);
		playerCamera.gameObject.transform.eulerAngles += _rotate;
	}
}
