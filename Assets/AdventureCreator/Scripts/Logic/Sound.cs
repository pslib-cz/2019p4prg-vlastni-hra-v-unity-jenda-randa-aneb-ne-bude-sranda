/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"Sound.cs"
 * 
 *	This script allows for easy playback of audio sources from within the ActionList system.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This component controls the volume of the AudioSource component it is attached beside, according to the volume levels set within OptionsData by the player.
	 * It also allows for AudioSources to be controlled using Actions.
	 */
	[RequireComponent (typeof (AudioSource))]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_sound.html")]
	#endif
	public class Sound : MonoBehaviour
	{

		/** The type of sound, so far as volume levels go (SFX, Music, Other) */
		[HideInInspector] public SoundType soundType;
		/** If True, then the sound can play when the game is paused */
		[HideInInspector] public bool playWhilePaused = false;
		/** The volume of the sound, relative to its categoriy's "global" volume set within OptionsData */
		[HideInInspector] public float relativeVolume = 1f;
		/** If True, then the GameObject this is attached to will not be destroyed when changing scene */
		[HideInInspector] public bool surviveSceneChange = false;

		private float maxVolume = 1f;
		private float smoothVolume = 1f;
		private float smoothUpdateSpeed = 20f;

		private float fadeTime;
		private float originalFadeTime;
		private FadeType fadeType;

		private Options options;
		protected AudioSource audioSource;
		private float otherVolume = 1f;

		private float originalRelativeVolume;
		private float targetRelativeVolume;
		private float relativeChangeTime;
		private float originalRelativeChangeTime;

		
		private void Awake ()
		{
			Initialise ();
		}


		private void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		private void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		private void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}


		protected void Initialise ()
		{
			if (surviveSceneChange)
			{
				if (transform.root != null && transform.root != gameObject.transform)
				{
					transform.SetParent (null);
				}
				DontDestroyOnLoad (this);
			}
			
			if (GetComponent <AudioSource>())
			{
				audioSource = GetComponent <AudioSource>();

				if (audioSource.playOnAwake)
				{
					audioSource.playOnAwake = false;
				}
			}

			audioSource.ignoreListenerPause = playWhilePaused;
			AdvGame.AssignMixerGroup (audioSource, soundType);
		}


		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			// Search for duplicates carried over from scene change
			if (GetComponent <ConstantID>())
			{
				int ownID = GetComponent <ConstantID>().constantID;
				Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
				foreach (Sound sound in sounds)
				{
					if (sound != this && sound.GetComponent <ConstantID>() && sound.GetComponent <ConstantID>().constantID == ownID)
					{
						DestroyImmediate (sound.gameObject);
						return;
					}
				}
			}
		}


		/**
		 * Initialises the AudioSource's volume, when the scene begins.
		 */
		public void AfterLoading ()
		{
			if (audioSource == null && GetComponent <AudioSource>())
			{
				audioSource = GetComponent <AudioSource>();
			}

			if (audioSource)
			{
				audioSource.ignoreListenerPause = playWhilePaused;
				
				if (audioSource.playOnAwake && audioSource.clip)
				{
					FadeIn (0.5f, audioSource.loop);
				}
				else
				{
					SetMaxVolume ();
				}

				SnapSmoothVolume ();
			}
			else
			{
				ACDebug.LogWarning ("Sound object " + this.name + " has no AudioSource component.", this);
			}
		}
		

		/**
		 * Updates the AudioSource's volume.
		 * This is called every frame by StateHandler.
		 */
		public virtual void _Update ()
		{
			float deltaTime = Time.deltaTime;
			if (KickStarter.stateHandler.gameState == GameState.Paused)
			{
				if (playWhilePaused)
				{
					deltaTime = Time.fixedDeltaTime;
				}
				else
				{
					return;
				}
			}

			if (relativeChangeTime > 0f)
			{
				relativeChangeTime -= deltaTime;
				float i = (originalRelativeChangeTime - relativeChangeTime) / originalRelativeChangeTime; // 0 -> 1
				
				if (relativeChangeTime <= 0f)
				{
					relativeVolume = targetRelativeVolume;
				}
				else
				{
					relativeVolume = (i * targetRelativeVolume) + ((1f - i) * originalRelativeVolume);
				}
				SetMaxVolume ();
			}

			if (fadeTime > 0f && audioSource.isPlaying)
			{
				smoothVolume = maxVolume;

				fadeTime -= deltaTime;
				float progress = (originalFadeTime - fadeTime) / originalFadeTime;

				if (fadeType == FadeType.fadeIn)
				{
					if (progress > 1f)
					{
						audioSource.volume = smoothVolume;
						fadeTime = 0f;
					}
					else
					{
						audioSource.volume = progress * smoothVolume;
					}
				}
				else if (fadeType == FadeType.fadeOut)
				{
					if (progress > 1f)
					{
						audioSource.volume = 0f;
						Stop ();
					}
					else
					{
						audioSource.volume = (1 - progress) * smoothVolume;
					}
				}
				SetSmoothVolume ();
			}
			else
			{
				SetSmoothVolume ();
				if (audioSource)
				{
					audioSource.volume = smoothVolume;
				}
			}
		}


		private void SetSmoothVolume ()
		{
			if (!Mathf.Approximately (smoothVolume, maxVolume))
			{
				if (smoothUpdateSpeed > 0)
				{
					smoothVolume = Mathf.Lerp (smoothVolume, maxVolume, (KickStarter.stateHandler.gameState == GameState.Paused) ? Time.fixedDeltaTime : Time.deltaTime * smoothUpdateSpeed);
				}
				else
				{
					SnapSmoothVolume ();
				}
			}
		}


		private void SnapSmoothVolume ()
		{
			smoothVolume = maxVolume;
		}
		

		/**
		 * Plays the AudioSource's current AudioClip.
		 */
		public void Interact ()
		{
			fadeTime = 0f;
			SetMaxVolume ();
			Play (audioSource.loop);
		}
		

		/**
		 * <summary>Fades in the AudioSource's current AudioClip, after which it continues to play.</summary>
		 * <param name = "_fadeTime">The fade duration, in seconds</param>
		 * <param name = "loop">If True, then the AudioClip will loop</param>
		 * <param name = "_timeSamples">The timeSamples to play from</param>
		 */
		public void FadeIn (float _fadeTime, bool loop, int _timeSamples = 0)
		{
			if (audioSource.clip == null)
			{
				return;
			}

			audioSource.loop = loop;

			fadeTime = originalFadeTime = _fadeTime;
			fadeType = FadeType.fadeIn;
			
			SetMaxVolume ();

			audioSource.volume = 0f;
			audioSource.timeSamples = _timeSamples;
			audioSource.Play ();
		}
		

		/**
		 * <summary>Fades out the AudioSource's current AudioClip, after which it stops.</summary>
		 * <param name = "_fadeTime">The fade duration, in seconds</param>
		 */
		public void FadeOut (float _fadeTime)
		{
			if (_fadeTime > 0f && audioSource.isPlaying)
			{
				fadeTime = originalFadeTime = _fadeTime;
				fadeType = FadeType.fadeOut;
				
				SetMaxVolume ();
			}
			else
			{
				Stop ();
			}
		}


		/**
		 * <summary>Checks if the AudioSource's AudioClip is being faded out.</summary>
		 * <returns>True if the AudioSource's AudioClip is being faded out</returns>
		 */
		public bool IsFadingOut ()
		{
			if (fadeTime > 0f && fadeType == FadeType.fadeOut)
			{
				return true;
			}
			return false;
		}


		#if !(UNITY_5 || UNITY_2017_1_OR_NEWER)
		/**
		 * <summary>Fixes a Unity 4 issue whereby an AudioSource does not play while paused unless it is re-played from the current point.</summary>
		 */
		public void ContinueFix ()
		{
			float startPoint = audioSource.time;
			Play ();
			audioSource.time = startPoint;
		}
		#endif


		/**
		 * <summary>Plays the AudioSource's current AudioClip, without starting over if it was paused or changing its "loop" variable.</summary>
		 */
		public void Play ()
		{
			if (audioSource == null)
			{
				return;
			}
			fadeTime = 0f;
			SetMaxVolume ();
			audioSource.Play ();
		}
		

		/**
		 * <summary>Plays the AudioSource's current AudioClip.</summary>
		 * <param name = "loop">If true, the AudioClip will be looped</param>
		 */
		public void Play (bool loop)
		{
			if (audioSource == null)
			{
				return;
			}
			audioSource.loop = loop;
			audioSource.timeSamples = 0;
			Play ();
		}


		/**
		 * <summary>Plays an AudioClip.</summary>
		 * <param name = "clip">The AudioClip to play</param>
		 * <param name = "loop">If true, the AudioClip will be looped</param>
		 * <param name = "_timeSamples">The timeSamples to play from</param>
		 */
		public void Play (AudioClip clip, bool loop, int _timeSamples = 0)
		{
			if (audioSource == null)
			{
				return;
			}
			audioSource.clip = clip;
			audioSource.loop = loop;
			audioSource.timeSamples = _timeSamples;
			Play ();
		}


		/**
		 * <summary>Plays the AudioSource's current AudioClip from a set point.</summary>
		 * <param name = "loop">If true, the AudioClip will be looped</param>
		 * <param name = "samplePoint">The playback position in PCM samples</param>
		 */
		public void PlayAtPoint (bool loop, int samplePoint)
		{
			if (audioSource == null)
			{
				return;
			}
			audioSource.loop = loop;
			audioSource.timeSamples = samplePoint;
			Play ();
		}
		

		/**
		 * Calculates the maximum volume that the AudioSource can have.
		 * This should be called whenever the volume in OptionsData is changed.
		 */
		public void SetMaxVolume ()
		{
			maxVolume = relativeVolume;

			#if UNITY_5 || UNITY_2017_1_OR_NEWER
			if (KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				SetFinalVolume ();
				return;
			}
			#endif

			if (Options.optionsData != null)
			{
				if (soundType == SoundType.Music)
				{
					maxVolume *= Options.optionsData.musicVolume;
				}
				else if (soundType == SoundType.SFX)
				{
					maxVolume *= Options.optionsData.sfxVolume;
				}
				else if (soundType == SoundType.Speech)
				{
					maxVolume *= Options.optionsData.speechVolume;
				}
			}
			if (soundType == SoundType.Other)
			{
				maxVolume *= otherVolume;
			}
			SetFinalVolume ();
		}


		/**
		 * <summary>Sets the volume, but takes relativeVolume into account as well.</summary>
		 * <param name = "volume">The volume to set</param>
		 */
		public void SetVolume (float volume)
		{
			#if UNITY_5 || UNITY_2017_1_OR_NEWER
			if (KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				volume = 1f;
			}
			#endif

			maxVolume = relativeVolume * volume;
			otherVolume = volume;
			SetFinalVolume ();
		}


		/**
		 * <summary>Changes the relativeVolume value.</summary>
		 * <param name = "newRelativeVolume">The new value for relativeVolume</param>
		 * <param name = "changeTime">The time, in seconds, to make the change in</param>
		 */
		public void ChangeRelativeVolume (float newRelativeVolume, float changeTime = 0f)
		{
			if (changeTime <= 0)
			{
				relativeVolume = newRelativeVolume;
				relativeChangeTime = 0f;
				SetMaxVolume ();
			}
			else
			{
				originalRelativeVolume = relativeVolume;
				targetRelativeVolume = newRelativeVolume;
				relativeChangeTime = originalRelativeChangeTime = changeTime;
			}
		}


		private void SetFinalVolume ()
		{
			if (KickStarter.dialog.AudioIsPlaying ())
			{
				if (soundType == SoundType.SFX)
				{
					maxVolume *= 1f - KickStarter.speechManager.sfxDucking;
				}
				else if (soundType == SoundType.Music)
				{
					maxVolume *= 1f - KickStarter.speechManager.musicDucking;
				}
			}
		}


		/**
		 * Abrubtply stops the currently-playing sound.
		 */
		public void Stop ()
		{
			fadeTime = 0f;
			audioSource.Stop ();
		}


		/**
		 * <summary>Checks if the sound is fading in or out.</summary>
		 * <returns>True if the sound is fading in or out</summary>
		 */
		public bool IsFading ()
		{
			return (fadeTime > 0f) ? true : false;
		}


		/**
		 * <summary>Checks if sound is playing.</summary>
		 * <returns>True if sound is playing</summary>
		 */
		public bool IsPlaying ()
		{
			if (audioSource == null)
			{
				Initialise ();
			}

			if (audioSource != null)
			{
				return audioSource.isPlaying;
			}
			return false;
		}


		/**
		 * <summary>Checks if a particular AudioClip is playing.</summary>
		 * <param name = "clip">The AudioClip to check for</param>
		 * <returns>True if the AudioClip is playing</returns>
		 */
		public bool IsPlaying (AudioClip clip)
		{
			if (audioSource != null && clip != null && audioSource.clip != null && audioSource.clip == clip && audioSource.isPlaying)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * Destroys itself, if it should do.
		 */
		public void TryDestroy ()
		{
			if (this is Music || this is Ambience)
			{}
			else if (surviveSceneChange && !audioSource.isPlaying)
			{
				if (gameObject.GetComponentInParent <Player>() == null &&
					GetComponent <Player>() == null &&
					GetComponentInChildren <Player>() == null)
				{
					ACDebug.Log ("Deleting Sound object '" + gameObject + "' as it is not currently playing any sound.", gameObject);
					DestroyImmediate (gameObject);
				}
			}
		}


		/**
		 * <summary>Fades out all sounds of a particular type being played.</summary>
		 * <param name = "soundType">If the soundType matches this, the sound will end</param>
		 * <param name = "ignoreSound">The Sound object to not affect</param>
		 */
		public void EndOld (SoundType _soundType, Sound ignoreSound)
		{
			if (soundType == _soundType && audioSource.isPlaying && this != ignoreSound)
			{
				if (fadeTime <= 0f || fadeType == FadeType.fadeIn)
				{
					FadeOut (0.1f);
				}
			}
		}


		private void TurnOn ()
		{
			audioSource.timeSamples = 0;
			Play ();
		}


		private void TurnOff ()
		{
			FadeOut (0.2f);
		}


		private void Kill ()
		{
			Stop ();
		}


		/**
		 * <summary>Updates a SoundData class with its own variables that need saving.</summary>
		 * <param name = "soundData">The original SoundData class</param>
		 * <returns>The updated SoundData class</returns>
		 */
		public SoundData GetSaveData (SoundData soundData)
		{
			soundData.isPlaying = IsPlaying ();

			soundData.isLooping = audioSource.loop;
			soundData.samplePoint = audioSource.timeSamples;
			soundData.relativeVolume = relativeVolume;

			soundData.maxVolume = maxVolume;
			soundData.smoothVolume = smoothVolume;

			soundData.fadeTime = fadeTime;
			soundData.originalFadeTime = originalFadeTime;
			soundData.fadeType = (int) fadeType;
			soundData.otherVolume = otherVolume;
			
			soundData.originalRelativeVolume = originalRelativeVolume;
			soundData.targetRelativeVolume = targetRelativeVolume;
			soundData.relativeChangeTime = relativeChangeTime;
			soundData.originalRelativeChangeTime = originalRelativeChangeTime;

			if (audioSource.clip != null)
			{
				soundData.clipID = AssetLoader.GetAssetInstanceID (audioSource.clip);
			}
			return soundData;
		}


		/**
		 * <summary>Updates its own variables from a SoundData class.</summary>
		 * <param name = "soundData">The SoundData class to load from</param>
		 */
		public void LoadData (SoundData soundData)
		{
			if (soundData.isPlaying)
			{
				audioSource.clip = AssetLoader.RetrieveAsset (audioSource.clip, soundData.clipID);
				PlayAtPoint (soundData.isLooping, soundData.samplePoint);
			}
			else
			{
				Stop ();
			}

			relativeVolume = soundData.relativeVolume;
			
			maxVolume = soundData.maxVolume;
			smoothVolume = soundData.smoothVolume;

			fadeTime = soundData.fadeTime;
			originalFadeTime = soundData.originalFadeTime;
			fadeType = (FadeType) soundData.fadeType;
			otherVolume = soundData.otherVolume;
			
			originalRelativeVolume = soundData.originalRelativeVolume;
			targetRelativeVolume = soundData.targetRelativeVolume;
			relativeChangeTime = soundData.relativeChangeTime;
			originalRelativeChangeTime = soundData.originalRelativeChangeTime;
		}

	}
	
}