/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"Dialog.cs"
 * 
 *	This script handles the running of dialogue lines, speech or otherwise.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Manages the creation, updating, and removal of all Speech lines.
	 * It should be placed on the GameEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_dialog.html")]
	#endif
	public class Dialog : MonoBehaviour
	{

		/** A List of all active Speech lines */
		public List<Speech> speechList = new List<Speech>();
		/** The Sound prefab to use to play narration speech audio from */
		public Sound narratorSound;
		/** The delay in seconds between choosing a Conversation's dialogue option and it triggering */
		[Range (0f, 1f)] public float conversationDelay = 0.3f;
		/** An array of rich-text tag names that can be detected when scrolling speech, to prevent them from displaying incorrectly.  If a name ends with an '=' symbol, the tag has a parameter */
		public string[] richTextTags = new string[]
		{
			#if TextMeshProIsPresent
			"s",
			"u",
			"sub",
			"sup",
			"link=",
			#endif
			"b",
			"i",
			"size=",
			"color="
		};

		private AudioSource defaultAudioSource;
		private AudioSource narratorAudioSource;
		private string[] speechEventTokenKeys = new string[0];


		public void OnAwake ()
		{
			if (KickStarter.sceneSettings.defaultSound && KickStarter.sceneSettings.defaultSound.GetComponent <AudioSource>())
			{
				defaultAudioSource = this.GetComponent <SceneSettings>().defaultSound.GetComponent <AudioSource>();
			}
		}


		/**
		 * Updates all active Speech lines.
		 * This is called every frame by StateHandler.
		 */
		public void _Update ()
		{
			if (KickStarter.stateHandler.gameState != GameState.Paused)
			{
				for (int i=0; i<speechList.Count; i++)
				{
					if (speechList[i].isAlive)
					{
						speechList[i].UpdateInput ();
					}
				}
			}

			if (KickStarter.playerInput.InputGetButtonDown ("EndConversation") && KickStarter.stateHandler.gameState == GameState.DialogOptions)
			{
				KickStarter.playerInput.EndConversation ();
				KickStarter.actionListManager.OnEndConversation ();
				KickStarter.actionListManager.SetCorrectGameState ();
			}
		}


		public void _LateUpdate ()
		{
			for (int i=0; i<speechList.Count; i++)
			{
				speechList[i].UpdateDisplay ();
				if (!speechList[i].isAlive)
				{
					EndSpeech (i);
					return;
				}
			}
		}


		/** Updates all speech volumes to the level set in Options.  This should be called whenever the Speech volume level is changed. */
		public void UpdateSpeechVolumes ()
		{
			for (int i=0; i<speechList.Count; i++)
			{
				speechList[i].UpdateVolume ();
			}
		}


		/**
		 * <summary>Initialises a new Speech line.</summary>
		 * <param name = "_speaker">The speaking character. If null, the line will be treated as narration</param>
		 * <param name = "_text">The subtitle text to display</param>
		 * <param name = "isBackground">True if the line should play in the background, and not interrupt any Actions or gameplay</param>
		 * <param name = "lineID">The ID number of the line, if it is listed in the Speech Manager</param>
		 * <param name = "noAnimation">True if the character should not play a talking animation</param>
		 * <param name = "preventSkipping">True if the speech cannot be skipped regardless of subtitle settings in the Speech Manager</param>
		 * <returns>The generated Speech line</returns>
		 */
		public Speech StartDialog (Char _speaker, string _text, bool isBackground = false, int lineID = -1, bool noAnimation = false, bool preventSkipping = false)
		{
			if (!KickStarter.actionListManager.IsGameplayBlocked () && !KickStarter.stateHandler.IsInScriptedCutscene ())
			{
				// Force background if during gameplay
				isBackground = true;
			}

			// Get the language
			string _language = "";
			int lanuageNumber = Options.GetLanguage ();
			if (lanuageNumber > 0)
			{
				// Not in original language, so pull translation in from Speech Manager
				_language = KickStarter.runtimeLanguages.Languages [lanuageNumber];
			}

			// Remove speaker's previous line
			for (int i=0; i<speechList.Count; i++)
			{
				if (speechList[i].GetSpeakingCharacter () == _speaker)
				{
					EndSpeech (i);
					i=0;
				}
			}
			
			Speech speech = new Speech (_speaker, _text, lineID, _language, isBackground, noAnimation, preventSkipping);
			speechList.Add (speech);

			KickStarter.runtimeVariables.AddToSpeechLog (speech.log);
			KickStarter.playerMenus.AssignSpeechToMenu (speech);

			if (speech.hasAudio)
			{
				if (KickStarter.speechManager.relegateBackgroundSpeechAudio)
				{
					EndBackgroundSpeechAudio (speech);
				}

				KickStarter.stateHandler.UpdateAllMaxVolumes ();
			}

			return speech;
		}


		/**
		 * <summary>Initialises a new Speech line. This differs from the other StartDialog function in that it requires a lineID, which must be present in the Speech Manager and is used to source the line's actual text.</summary>
		 * <param name = "_speaker">The speaking character. If null, the line will be treated as narration</param>
		 * <param name = "lineID">The ID number of the line, if it is listed in the Speech Manager</param>
		 * <param name = "isBackground">True if the line should play in the background, and not interrupt any Actions or gameplay</param>
		 * <param name = "noAnimation">True if the character should not play a talking animation</param>
		 * <returns>The generated Speech line</returns>
		 */
		public Speech StartDialog (Char _speaker, int lineID, bool isBackground = false, bool noAnimation = false)
		{
			string _text = "";

			SpeechLine speechLine = KickStarter.speechManager.GetLine (lineID);
			if (speechLine != null)
			{
				_text = speechLine.text;
			}
			else
			{
				ACDebug.LogWarning ("Cannot start dialog because the line ID " + lineID + " was not found in the Speech Manager.");
				return null;
			}

			// Get the language
			string _language = "";
			int lanuageNumber = Options.GetLanguage ();
			if (lanuageNumber > 0)
			{
				// Not in original language, so pull translation in from Speech Manager
				_language = KickStarter.runtimeLanguages.Languages [lanuageNumber];
			}

			// Remove speaker's previous line
			for (int i=0; i<speechList.Count; i++)
			{
				if (speechList[i].GetSpeakingCharacter () == _speaker)
				{
					EndSpeech (i);
					i=0;
				}
			}
			
			Speech speech = new Speech (_speaker, _text, lineID, _language, isBackground, noAnimation);
			speechList.Add (speech);

			KickStarter.runtimeVariables.AddToSpeechLog (speech.log);
			KickStarter.playerMenus.AssignSpeechToMenu (speech);

			if (speech.hasAudio)
			{
				if (KickStarter.speechManager.relegateBackgroundSpeechAudio)
				{
					EndBackgroundSpeechAudio (speech);
				}

				KickStarter.stateHandler.UpdateAllMaxVolumes ();
			}

			return speech;
		}


		/**
		 * <summary>Gets, and creates if necessary, an AudioSource used for Narration speech. If the narratorSound variable is empty, one will be generated and its AudioSource referenced.</summary>
		 * <returns>The AudioSource for Narration speech</returns>
		 */
		public AudioSource GetNarratorAudioSource ()
		{
			if (narratorAudioSource == null)
			{
				if (narratorSound != null)
				{
					narratorAudioSource = narratorSound.GetComponent <AudioSource>();
				}
				else
				{
					GameObject narratorSoundOb = new GameObject ("Narrator");
					UnityVersionHandler.PutInFolder (narratorSoundOb, "_Sounds");

					narratorAudioSource = narratorSoundOb.AddComponent <AudioSource>();
					AdvGame.AssignMixerGroup (narratorAudioSource, SoundType.Speech);
					#if UNITY_5 || UNITY_2017_1_OR_NEWER
					narratorAudioSource.spatialBlend = 0f;
					#endif

					narratorSound = narratorSoundOb.AddComponent <Sound>();
					narratorSound.soundType = SoundType.Speech;
				}
			}

			narratorSound.SetMaxVolume ();
			return narratorAudioSource;
		}


		/**
		 * <summary>Plays text-scoll audio for a given character. If no character is speaking, narration text-scroll audio will be played instead.</summary>
		 * <param name = "_speaker">The speaking character</param>
		 */
		public void PlayScrollAudio (AC.Char _speaker)
		{
			AudioClip textScrollClip = KickStarter.speechManager.textScrollCLip;

			if (_speaker == null)
			{
				textScrollClip = KickStarter.speechManager.narrationTextScrollCLip;
			}
			else if (_speaker.textScrollClip != null)
			{
				textScrollClip = _speaker.textScrollClip;
			}

			if (textScrollClip != null)
			{
				if (defaultAudioSource)
				{
					if (KickStarter.speechManager.playScrollAudioEveryCharacter || !defaultAudioSource.isPlaying)
					{
						defaultAudioSource.PlayOneShot (textScrollClip);
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot play text scroll audio clip as no 'Default' sound prefab has been defined in the Scene Manager");
				}
			}
		}


		/**
		 * <summary>Gets the last Speech line to be played.</summary>
		 * <returns>The last item in the speechList List</returns>
		 */
		public Speech GetLatestSpeech ()
		{
			if (speechList.Count > 0)
			{
				return speechList [speechList.Count - 1];
			}
			return null;
		}


		/**
		 * <summary>Gets a speech line with a specific ID, that's currently being spoken</summary>
		 * <param name = "lineID">The ID number of the speech line to get</param>
		 * <returns>The active speech line</returns>
		 */
		public Speech GetLiveSpeechWithID (int lineID)
		{
			if (speechList.Count > 0)
			{
				foreach (Speech speech in speechList)
				{
					if (speech.LineID == lineID)
					{
						return speech;
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Checks if any of the active Speech lines have audio.</summary>
		 * <returns>True if any active Speech lines have audio</returns>
		 */
		public bool FoundAudio ()
		{
			foreach (Speech speech in speechList)
			{
				if (speech.hasAudio)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if any speech is currently playing.</summary>
		 * <param name = "ignoreBackgroundSpeech">If True, then only gameplay-blocking speech will be accounted for</param>
		 * <returns>True if any speech is currently playing</returns>
		 */
		public bool IsAnySpeechPlaying (bool ignoreBackgroundSpeech = false)
		{
			if (speechList.Count > 0)
			{
				if (ignoreBackgroundSpeech)
				{
					if (KickStarter.stateHandler.IsInGameplay ())
					{
						return false;
					}

					for (int i=0; i<speechList.Count; i++)
					{
						if (!speechList[i].isBackground)
						{
							return true;
						}
					}
				}
				else
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Gets the display name of the most recently-speaking character.</summary>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The character's display name</returns>
		 */
		public string GetSpeaker (int languageNumber = 0)
		{
			if (speechList.Count > 0)
			{
				return GetLatestSpeech ().GetSpeaker (languageNumber);
			}			
			return "";
		}


		/**
		 * <summary>Checks if a given character is speaking.</summary>
		 * <param name = "_char".The character to check</param>
		 * <returns>True if the character is speaking</returns>
		 */
		public bool CharacterIsSpeaking (Char _char)
		{
			for (int i=0; i<speechList.Count; i++)
			{
				if (speechList[i].GetSpeakingCharacter () == _char)
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Gets the most recently-speaking character.</summary>
		 * <returns>The character</returns>
		 */
		public AC.Char GetSpeakingCharacter ()
		{
			if (speechList.Count > 0)
			{
				return GetLatestSpeech ().GetSpeakingCharacter ();
			}
			return null;
		}


		/**
		 * <summary>Checks if any of the active Speech lines have audio and are playing it.</summary>
		 * <returns>True if any active Speech lines have audio and are playing it</returns>
		 */
		public bool AudioIsPlaying ()
		{
			if (Options.optionsData != null && Options.optionsData.speechVolume > 0f)
			{
				for (int i=0; i<speechList.Count; i++)
				{
					if (speechList[i].hasAudio && speechList[i].isAlive)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Kills all active Speech lines.</summary>
		 * <param name = "stopCharacter">If True, then all characters speaking will cease their talking animation</param>
		 * <param name = "forceMenusOff">True if subtitles should be turned off immediately</param>
		 * <param name = "speechMenuLimit">The type of speech to kill (All, BlockingOnly, BackgroundOnly)</param>
		 */
		public void KillDialog (bool stopCharacter, bool forceMenusOff, SpeechMenuLimit speechMenuLimit = SpeechMenuLimit.All, SpeechMenuType speechMenuType = SpeechMenuType.All, string limitToCharacters = "")
		{
			bool hadEffect = false;

			for (int i=0; i<speechList.Count; i++)
			{
				if (speechList[i].HasConditions (speechMenuLimit, speechMenuType, limitToCharacters))
				{
					EndSpeech (i, stopCharacter);
					hadEffect = true;
					i=0;
				}
			}

			if (hadEffect)
			{
				KickStarter.stateHandler.UpdateAllMaxVolumes ();
				if (forceMenusOff)
				{
					KickStarter.playerMenus.ForceOffSubtitles ();
				}
			}
		}


		/**
		 * <summary>Kills a given Speech line.</summary>
		 * <param name = "speech">The Speech class instance to end.</param>
		 * <param name = "stopCharacter">If True, then the speaking character will cease their talking animation</param>
		 */
		public void KillDialog (Speech speech)
		{
			if (speech != null)
			{
				if (speechList.Contains (speech))
				{
					EndSpeech (speechList.IndexOf (speech), true);
				}
				else
				{
					ACDebug.Log ("Cannot kill dialog '" + speech.log.fullText + "' because it is not in the speech list.");
				}
			}

			KickStarter.stateHandler.UpdateAllMaxVolumes ();
		}


		/**
		 * <summary>Generates the animation data for lipsyncing a given Speech line.</summary>
		 * <param name = "_lipSyncMode">The chosen method of lipsyncing (Off, FromSpeechText, ReadPamelaFile, ReadSapiFile, ReadPapagayoFile, FaceFX, Salsa2D)</param>
		 * <param name = "lineNumber">The speech line's ID number</param>
		 * <param name = "_speaker">The speaking character</param>
		 * <param name = "language">The name of the current language</param>
		 * <param name = "_message">The speech text</param<
		 * <returns>A List of LipSyncShape structs that contain the lipsync animation data</returns>
		 */
		public List<LipSyncShape> GenerateLipSyncShapes (LipSyncMode _lipSyncMode, int lineID, Char _speaker, string language = "", string _message = "")
		{
			List<LipSyncShape> lipSyncShapes = new List<LipSyncShape>();
			lipSyncShapes.Add (new LipSyncShape (0, 0f, KickStarter.speechManager.lipSyncSpeed));
			TextAsset textFile = null;

			if (_lipSyncMode == LipSyncMode.Salsa2D)
			{
				return lipSyncShapes;
			}
			
			if (lineID > -1 && _speaker != null && KickStarter.speechManager.searchAudioFiles && KickStarter.speechManager.UseFileBasedLipSyncing ())
			{
				textFile = (TextAsset) KickStarter.runtimeLanguages.GetSpeechLipsyncFile <TextAsset> (lineID, _speaker);
			}
			
			if (_lipSyncMode == LipSyncMode.ReadPamelaFile && textFile != null)
			{
				var splitFile = new string[] { "\r\n", "\r", "\n" };
				var pamLines = textFile.text.Split (splitFile, System.StringSplitOptions.None);

				bool foundSpeech = false;
				float fps = 24f;
				foreach (string pamLine in pamLines)
				{
					if (!foundSpeech)
					{
						if (pamLine.Contains ("framespersecond:"))
						{
							string[] pamLineArray = pamLine.Split(':');
							float.TryParse (pamLineArray[1], out fps);
						}
						else if (pamLine.Contains ("[Speech]"))
						{
							foundSpeech = true;
						}
					}
					else if (pamLine.Contains (":"))
					{
						string[] pamLineArray = pamLine.Split(':');
						
						float timeIndex = 0f;
						float.TryParse (pamLineArray[0], out timeIndex);
						string searchText = pamLineArray[1].ToLower ().Substring (0, pamLineArray[1].Length-1);
						
						bool found = false;
						foreach (string phoneme in KickStarter.speechManager.phonemes)
						{
							string[] shapesArray = phoneme.ToLower ().Split ("/"[0]);
							if (!found)
							{
								foreach (string shape in shapesArray)
								{
									//if (shape == searchText)
									if (searchText.Contains (shape) && searchText.Length == shape.Length)
									{
										int frame = KickStarter.speechManager.phonemes.IndexOf (phoneme);
										lipSyncShapes.Add (new LipSyncShape (frame, timeIndex, KickStarter.speechManager.lipSyncSpeed, fps));
										found = true;
									}
								}
							}
						}
						if (!found)
						{
							lipSyncShapes.Add (new LipSyncShape (0, timeIndex, KickStarter.speechManager.lipSyncSpeed, fps));
						}
					}
				}
			}
			else if (_lipSyncMode == LipSyncMode.ReadSapiFile && textFile != null)
			{
				var splitFile = new string[] { "\r\n", "\r", "\n" };
				var sapiLines = textFile.text.Split (splitFile, System.StringSplitOptions.None);

				foreach (string sapiLine in sapiLines)
				{
					if (sapiLine.StartsWith ("phn "))
					{
						string[] sapiLineArray = sapiLine.Split(' ');
						float timeIndex = 0f;
						float.TryParse (sapiLineArray[1], out timeIndex);
						string searchText = sapiLineArray[4].EndsWith (" ") ? sapiLineArray[4].ToLower ().Substring (0, sapiLineArray[4].Length-1) : sapiLineArray[4].ToLower ();
						bool found = false;
						foreach (string _phoneme in KickStarter.speechManager.phonemes)
						{
							string phoneme = _phoneme.ToLower ();
							if (phoneme.Contains (searchText))
							{
								string[] shapesArray = phoneme.Split ("/"[0]);
								if (!found)
								{
									foreach (string shape in shapesArray)
									{
										if (shape == searchText)
										{
											int frame = KickStarter.speechManager.phonemes.IndexOf (_phoneme);
											lipSyncShapes.Add (new LipSyncShape (frame, timeIndex, KickStarter.speechManager.lipSyncSpeed, 60f));
											found = true;
										}
									}
								}
							}
						}
						if (!found)
						{
							lipSyncShapes.Add (new LipSyncShape (0, timeIndex, KickStarter.speechManager.lipSyncSpeed, 60f));
						}
					}
				}
			}
			else if (_lipSyncMode == LipSyncMode.ReadPapagayoFile && textFile != null)
			{
				var splitFile = new string[] { "\r\n", "\r", "\n" };
				var papagoyoLines = textFile.text.Split (splitFile, System.StringSplitOptions.None);

				foreach (string papagoyoLine in papagoyoLines)
				{
					if (!string.IsNullOrEmpty (papagoyoLine) && !papagoyoLine.Contains ("MohoSwitch"))
					{
						string[] papagoyoLineArray = papagoyoLine.Split(' ');
						if (papagoyoLineArray.Length == 2)
						{
							float timeIndex = 0f;
							if (float.TryParse (papagoyoLineArray[0], out timeIndex))
							{
								string searchText = papagoyoLineArray[1].ToLower ().Substring (0, papagoyoLineArray[1].Length);
								
								bool found = false;
								if (!searchText.Contains ("rest") && !searchText.Contains ("etc"))
								{
									foreach (string phoneme in KickStarter.speechManager.phonemes)
									{
										string[] shapesArray = phoneme.ToLower ().Split ("/"[0]);
										if (!found)
										{
											foreach (string shape in shapesArray)
											{
												if (shape == searchText)
												{
													int frame = KickStarter.speechManager.phonemes.IndexOf (phoneme);
													lipSyncShapes.Add (new LipSyncShape (frame, timeIndex, KickStarter.speechManager.lipSyncSpeed, 24f));
													found = true;
													break;
												}
											}
										}
									}
									if (!found)
									{
										lipSyncShapes.Add (new LipSyncShape (0, timeIndex, KickStarter.speechManager.lipSyncSpeed, 24f)); // was 240
									}
								}
							}
						}
					}
				}
			}
			else if (_lipSyncMode == LipSyncMode.FromSpeechText)
			{
				for (int i=0; i<_message.Length; i++)
				{
					int maxSearch = Mathf.Min (5, _message.Length - i);
					for (int n=maxSearch; n>0; n--)
					{
						string searchText = _message.Substring (i, n);
						searchText = searchText.ToLower ();
						
						foreach (string phoneme in KickStarter.speechManager.phonemes)
						{
							string[] shapesArray = phoneme.ToLower ().Split ("/"[0]);
							foreach (string shape in shapesArray)
							{
								if (shape == searchText)
								{
									int frame = KickStarter.speechManager.phonemes.IndexOf (phoneme);
									lipSyncShapes.Add (new LipSyncShape (frame, (float) i, KickStarter.speechManager.lipSyncSpeed));
									i += n;
									n = Mathf.Min (5, _message.Length - i);
									break;
								}
							}
						}
						
					}
					lipSyncShapes.Add (new LipSyncShape (0, (float) i, KickStarter.speechManager.lipSyncSpeed));
				}
			}
			
			if (lipSyncShapes.Count > 1)
			{
				lipSyncShapes.Sort (delegate (LipSyncShape a, LipSyncShape b) {return a.timeIndex.CompareTo (b.timeIndex);});
			}
			
			return lipSyncShapes;
		}


		private void EndBackgroundSpeechAudio (Speech speech)
		{
			foreach (Speech _speech in speechList)
			{
				if (_speech != speech)
				{
					_speech.EndBackgroundSpeechAudio (speech.GetSpeakingCharacter ());
				}
			}
		}


		/**
		 * <summary>Ends speech spoken by a given character</summary>
		 * <param name = "character">The character to stop speaking</param>
		 */
		public void EndSpeechByCharacter (Char character)
		{
			for (int i=0; i<speechList.Count; i++)
			{
				if (speechList[i].GetSpeakingCharacter () == character)
				{
					EndSpeech (i, true);
					return;
				}
			}
		}
		
		
		private void EndSpeech (int i, bool stopCharacter = false)
		{
			Speech oldSpeech = speechList[i];
			KickStarter.playerMenus.RemoveSpeechFromMenu (oldSpeech);
			if (stopCharacter)
			{
				if (oldSpeech.GetSpeakingCharacter ())
				{
					oldSpeech.GetSpeakingCharacter ().StopSpeaking ();
				}
				else
				{
					oldSpeech.EndSpeechAudio ();
				}
			}
			oldSpeech.isAlive = false;
			speechList.RemoveAt (i);
			
			if (oldSpeech.hasAudio)
			{
				KickStarter.stateHandler.UpdateAllMaxVolumes ();
			}

			// Call event
			KickStarter.eventManager.Call_OnStopSpeech (oldSpeech.GetSpeakingCharacter ());
		}


		/**
		 * An array of string keys that can be inserted into speech text in the form [key:value].
		 * When processed by the speech display, they will be removed from the speech, but will trigger the OnSpeechToken event.
		 */
		public string[] SpeechEventTokenKeys
		{
			get
			{
				return speechEventTokenKeys;
			}
			set
			{
				speechEventTokenKeys = value;
			}
		}


		private void OnDestroy ()
		{
			defaultAudioSource = null;
			narratorAudioSource = null;
		}

	}
	

	/**
	 * A data struct for any pauses, delays or Expression changes within a speech line.
	 */
	public struct SpeechGap
	{

		/** The character index of the gap */
		public int characterIndex;
		/** The time delay of the gap */
		public float waitTime;
		/** If True, there is no time delay - the gap will pause indefinitely until the player clicks */
		public bool pauseIsIndefinite;
		/** The ID number of the Expression */
		public int expressionID;
		/** The key, if a custom event token */
		public string tokenKey;
		/** The value, if a custom event token */
		public string tokenValue;


		/**
		 * The default Constructor.</summary>
		 * <param name = "_characterIndex</param>The character index of the gap</param>
		 * <param name = "_waitTime</param>The time delay of the gap</param>
		 */
		public SpeechGap (int _characterIndex, float _waitTime)
		{
			characterIndex = _characterIndex;
			waitTime = _waitTime;
			expressionID = -1;
			pauseIsIndefinite = false;
			tokenKey = tokenValue = string.Empty;
		}


		/**
		 * A Constructor for an indefinite pauses.</summary>
		 * <param name = "_characterIndex</param>The character index of the gap</param>
		 * <param name = "_expressionID</param>The ID number of the Expression</param>
		 */
		public SpeechGap (int _characterIndex, bool _pauseIsIndefinite)
		{
			characterIndex = _characterIndex;
			waitTime = -1f;
			expressionID = -1;
			pauseIsIndefinite = _pauseIsIndefinite;
			tokenKey = tokenValue = string.Empty;
		}


		/**
		 * A Constructor for an expression change.</summary>
		 * <param name = "_characterIndex</param>The character index of the gap</param>
		 * <param name = "_expressionID</param>The ID number of the Expression</param>
		 */
		public SpeechGap (int _characterIndex, int _expressionID)
		{
			characterIndex = _characterIndex;
			waitTime = -1f;
			expressionID = _expressionID;
			pauseIsIndefinite = false;
			tokenKey = tokenValue = string.Empty;
		}


		/**
		 * A Constructor for custom event tokens.</summary>
		 * <param name = "_characterIndex</param>The character index of the gap</param>
		 * <param name = "_expressionID</param>The ID number of the Expression</param>
		 */
		public SpeechGap (int _characterIndex, string _tokenKey, string _tokenValue)
		{
			characterIndex = _characterIndex;
			waitTime = 0f;
			expressionID = -1;
			tokenKey = _tokenKey;
			tokenValue = _tokenValue;
			pauseIsIndefinite = false;
		}


		public void CallEvent (AC.Char speaker, int lineID)
		{
			if (!string.IsNullOrEmpty (tokenValue))
			{
				KickStarter.eventManager.Call_OnSpeechToken (speaker, lineID, tokenKey, tokenValue);
			}
		}
		
	}


	/**
	 * A data struct of lipsync animation
	 */
	public struct LipSyncShape
	{

		/** The animation frame to play */
		public int frame;
		/** The time index that the animation correlates to */
		public float timeIndex;
		

		/**
		 * The default Constructor.
		 * <param name = "_frame">The animation frame to play</param>
		 * <param name = "_timeIndex">The time index that the animation correlates to</param>
		 * <param name = "speed">The playback speed set by the player</param>
		 * <param name = "fps">The FPS rate set by the third-party LipSync tool</param>
		 */
		public LipSyncShape (int _frame, float _timeIndex, float speed, float fps = 1f)
		{
			// Pamela / Sapi
			frame = _frame;
			timeIndex = (_timeIndex / 15f / speed / fps) + Time.time;
		}

	}
	
}