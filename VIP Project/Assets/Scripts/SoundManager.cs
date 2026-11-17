using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
	private AudioSource audioSource;

	[SerializeField] private AudioClip jingleSuccess;
	[SerializeField] private AudioClip jingleSuccessSecret;

	private void OnEnable()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
		audioSource = GetComponent<AudioSource>();
	}

	[ContextMenu("Play Success Sound")]
	private void DebugPlaySound()
	{
		audioSource.PlayOneShot(jingleSuccess);
	}
	[ContextMenu("Play Success Sound Secret")]
	private void DebugPlaySoundSecret()
	{
		audioSource.PlayOneShot(jingleSuccessSecret);
	}
}
