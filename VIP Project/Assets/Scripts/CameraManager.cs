using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera puzzleCamera;
    [SerializeField] private CinemachineCamera cutsceneCamera;
	private void Start()
	{
		SwapCamera(playerCamera);
	}
	public void SwapCamera(CinemachineCamera cam)
    {
        playerCamera.gameObject.SetActive(false);
        puzzleCamera.gameObject.SetActive(false);
        cutsceneCamera.gameObject.SetActive(false);

        cam.gameObject.SetActive(true);
    }
    [ContextMenu("Swap to cam 1")]
    public void DebugSwap1()
    {
        SwapCamera(playerCamera);
    }
    [ContextMenu("Swap to cam 2")]
    public void DebugSwap2()
    {
        SwapCamera(puzzleCamera);
    }
    [ContextMenu("Swap to cam 3")]
    public void DebugSwap3()
    {
        SwapCamera(cutsceneCamera);
    }
}
