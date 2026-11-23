using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public static SoundManager instance;
	[Header("Audio Sources")]
	[SerializeField] private AudioSource sfxSource;
	[SerializeField] private AudioSource musSourceIntro;
	[SerializeField] private AudioSource musSourceLoop;
	[Header("Music")]
	[SerializeField] private AudioClip gameMusIntro;
	[SerializeField] private AudioClip gameMusLoop;
	[Header("Sound Effects")]
	public AudioClip jingleSuccess;
	public AudioClip jingleSuccessSecret;
	public AudioClip jingleFail;
	/// <summary>
	/// Singleton check on Awake
	/// </summary>
	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
			return;
		}
		DontDestroyOnLoad(gameObject);
	}
	/// <summary>
	/// Initializes the music on start
	/// </summary>
	private void Start()
	{
		PlayMusicWithIntro(gameMusIntro, gameMusLoop);
	}
	/// <summary>
	/// Plays an AudioClip as a sound effect
	/// </summary>
	/// <param name="audio">AudioClip to play</param>
	public void PlaySound(AudioClip audio)
	{
		sfxSource.PlayOneShot(audio);
	}
	/// <summary>
	/// Sets up two AudioSources to play the music, one to play the intro and the other to play a version that loops seamlessly.
	/// </summary>
	/// <param name="intro"></param>
	/// <param name="loop"></param>
	public void PlayMusicWithIntro(AudioClip intro, AudioClip loop)
	{
		musSourceIntro.Stop(); //Ensures no music is playing before loading new music.
		musSourceLoop.Stop();

		double startTime = AudioSettings.dspTime + 1; //Grabs the current time in the audio system and adds 1 second to give time to load.
		double introDuration = (double)intro.samples / intro.frequency; //Calculates the total duration of the audioclip in seconds.
		musSourceIntro.clip = intro;
		musSourceIntro.loop = false; //Should already be set in the AudioSource object. Sets the value anyways to be extra sure.
		musSourceIntro.PlayScheduled(startTime);
		Debug.Log($"Playing music intro at {startTime} with a duration of {introDuration}");

		musSourceLoop.clip = loop;
		musSourceLoop.loop = true;
		musSourceLoop.PlayScheduled(startTime + introDuration);
		Debug.Log($"Playing music loop at {startTime + introDuration}");
	}
}
