/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"SceneChanger.cs"
 * 
 *	This script handles the changing of the scene, and stores
 *	which scene was previously loaded, for use by PlayerStart.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Handles the changing of the scene, and keeps track of which scene was previously loaded.
	 * It should be placed on the PersistentEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_scene_changer.html")]
	#endif
	public class SceneChanger : MonoBehaviour
	{

		private SceneInfo previousSceneInfo;
		private SceneInfo previousGlobalSceneInfo;

		private List<SubScene> subScenes = new List<SubScene>();

		private Vector3 relativePosition;
		private AsyncOperation preloadAsync;
		private SceneInfo preloadSceneInfo;
		private SceneInfo thisSceneInfo;
		private Player playerOnTransition = null;
		private Texture2D textureOnTransition = null;
		private bool isLoading = false;
		private float loadingProgress = 0f;
		
		private int removeNPCID = 0;
		private bool takeNPCPosition;


		public void OnAwake ()
		{
			previousSceneInfo = new SceneInfo ("", -1);
			previousGlobalSceneInfo = new SceneInfo ("", -1);

			relativePosition = Vector3.zero;
			isLoading = false;
			AssignThisSceneInfo ();
		}


		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			AssignThisSceneInfo ();
		}


		private void AssignThisSceneInfo ()
		{
			thisSceneInfo = new SceneInfo (UnityVersionHandler.GetCurrentSceneName (), UnityVersionHandler.GetCurrentSceneNumber ());
		}


		/**
		 * <summary>Calculates the player's position relative to the next scene's PlayerStart.</summary>
		 * <param name = "markerTransform">The Transform of the GameObject that marks the position that the player should be placed relative to.</param>
		 */
		public void SetRelativePosition (Transform markerTransform)
		{
			if (KickStarter.player == null || markerTransform == null)
			{
				relativePosition = Vector2.zero;
			}
			else
			{
				relativePosition = KickStarter.player.transform.position - markerTransform.position;
				if (SceneSettings.IsUnity2D ())
				{
					relativePosition.z = 0f;
				}
				else if (SceneSettings.IsTopDown ())
				{
					relativePosition.y = 0f;
				}
			}
		}


		/**
		 * <summary>Gets the player's starting position by adding the relative position (set in ActionScene) to the PlayerStart's position.</summary>
		 * <param name = "playerStartPosition">The position of the PlayerStart object</param>
		 * <returns>The player's starting position</returns>
		 */
		public Vector3 GetStartPosition (Vector3 playerStartPosition)
		{
			Vector3 startPosition = playerStartPosition + relativePosition;
			relativePosition = Vector2.zero;
			return startPosition;
		}


		/**
		 * <summary>Gets the progress of an asynchronous scene load as a decimal.</summary>
		 * <returns>The progress of an asynchronous scene load as a decimal.</returns>
		 */
		public float GetLoadingProgress ()
		{
			if (KickStarter.settingsManager.useAsyncLoading)
			{
				if (isLoading)
				{
					return loadingProgress;
				}
			}
			else
			{
				ACDebug.LogWarning ("Cannot get the loading progress because asynchronous loading is not enabled in the Settings Manager.");
			}
			return 0f;
		}


		public bool IsLoading ()
		{
			return isLoading;
		}


		public void PreloadScene (SceneInfo nextSceneInfo)
		{
			if (preloadSceneInfo != null && preloadSceneInfo.Matches (nextSceneInfo))
			{
				ACDebug.Log ("Skipping preload of scene '" + nextSceneInfo.GetLabel () + "' - already preloaded.");
				return;
			}
			StartCoroutine (PreloadLevelAsync (nextSceneInfo));
		}


		/**
		 * <summary>Loads a new scene.  This method should be used instead of Unity's own scene-switching method, because this allows for AC objects to be saved beforehand</summary>
		 * <param name = "nextSceneInfo">Info about the scene to load</param>
		 * <param name = "sceneNumber">The number of the scene to load, if sceneName = ""</param>
		 * <param name = "saveRoomData">If True, then the states of the current scene's Remember scripts will be recorded in LevelStorage</param>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 * <param name = "_removeNPCID">If non-zero, then an NPC with a Constant ID of this number will be removed after the scene-change</param>
		 * <param name = "_takeNPCPosition">If True, and _revemoNPCID is non-zero, then the Player will teleport to the NPC's position before the NPC is removed</param>
		 */
		public void ChangeScene (SceneInfo nextSceneInfo, bool saveRoomData, bool forceReload = false, int _removeNPCID = 0, bool _takeNPCPosition = false)
		{
			removeNPCID = 0;
			takeNPCPosition = false;

			if (!isLoading)
			{
				if (!nextSceneInfo.Matches (thisSceneInfo) || forceReload)
				{
					if (preloadSceneInfo != null && preloadSceneInfo.IsValid () && !preloadSceneInfo.Matches (nextSceneInfo))
					{
						ACDebug.LogWarning ("Opening scene '" + nextSceneInfo.GetLabel () + "', but have preloaded scene '" + preloadSceneInfo.GetLabel () + "'.  Preloaded data must be scrapped.");
						if (preloadAsync != null) preloadAsync.allowSceneActivation = true;
						preloadSceneInfo = null;
					}

					removeNPCID = _removeNPCID;
					takeNPCPosition = _takeNPCPosition;

					PrepareSceneForExit (!KickStarter.settingsManager.useAsyncLoading, saveRoomData);
					LoadLevel (nextSceneInfo, KickStarter.settingsManager.useLoadingScreen, KickStarter.settingsManager.useAsyncLoading, forceReload);
				}
			}
		}


		/**
		 * <summary>Loads the previously-entered scene.</summary>
		 */
		public void LoadPreviousScene ()
		{
			if (previousSceneInfo != null && !previousSceneInfo.IsNull)
			{
				ChangeScene (previousSceneInfo, true);
			}
			else
			{
				ACDebug.LogWarning ("Cannot load previous scene - no scene data present!");
			}
		}


		/**
		 * <summary>Gets the Player prefab that was active during the last scene transition.</summary>
		 * <returns>The Player prefab that was active during the last scene transition</returns>
		 */
		public Player GetPlayerOnTransition ()
		{
			return playerOnTransition;
		}


		/**
		 * Destroys the Player prefab that was active during the last scene transition.
		 */
		public void DestroyOldPlayer ()
		{
			if (playerOnTransition)
			{
				ACDebug.Log ("New player prefab found - " + playerOnTransition.name + " deleted");
				DestroyImmediate (playerOnTransition.gameObject);
			}
		}


		/*
		 * <summary>Stores a texture used as an overlay during a scene transition. This texture can be retrieved with GetAndResetTransitionTexture().</summary>
		 * <param name = "_texture">The Texture2D to store</param>
		 */
		public void SetTransitionTexture (Texture2D _texture)
		{
			textureOnTransition = _texture;
		}


		/**
		 * <summary>Gets, and removes from memory, the texture used as an overlay during a scene transition.</summary>
		 * <returns>The texture used as an overlay during a scene transition</returns>
		 */
		public Texture2D GetAndResetTransitionTexture ()
		{
			Texture2D _texture = textureOnTransition;
			textureOnTransition = null;
			return _texture;
		}


		/**
		 * <summary>Deletes a GameObject once the current frame has finished renderering.</summary>
		 * <param name = "_gameObject">The GameObject to delete</param>
		 */
		public void ScheduleForDeletion (GameObject _gameObject)
		{
			if (_gameObject.GetComponentInChildren <ActionList>())
			{
				ActionList actionList = _gameObject.GetComponent <ActionList>();
				if (actionList != null && KickStarter.actionListManager.IsListRunning (actionList))
				{
					actionList.Kill ();
					ACDebug.LogWarning ("The ActionList '" + actionList.name + "' is being removed from the scene while running!  Killing it now to prevent hanging.");
				}
			}

			StartCoroutine (ScheduleForDeletionCoroutine (_gameObject));
		}


		private IEnumerator ScheduleForDeletionCoroutine (GameObject _gameObject)
		{
			yield return new WaitForEndOfFrame ();
			DestroyImmediate (_gameObject);
		}


		private void LoadLevel (SceneInfo nextSceneInfo, bool useLoadingScreen, bool useAsyncLoading, bool forceReload = false)
		{
			if (useLoadingScreen)
			{
				StartCoroutine (LoadLoadingScreen (nextSceneInfo, new SceneInfo (KickStarter.settingsManager.loadingSceneIs, KickStarter.settingsManager.loadingSceneName, KickStarter.settingsManager.loadingScene), useAsyncLoading));
			}
			else
			{
				if (useAsyncLoading && !forceReload)
				{
					StartCoroutine (LoadLevelAsync (nextSceneInfo));
				}
				else
				{
					StartCoroutine (LoadLevelCo (nextSceneInfo, forceReload));
				}
			}
		}


		private IEnumerator LoadLoadingScreen (SceneInfo nextSceneInfo, SceneInfo loadingSceneInfo, bool loadAsynchronously = false)
		{
			if (preloadSceneInfo != null && !preloadSceneInfo.IsNull)
			{
				ACDebug.LogWarning ("Cannot use preloaded scene '" + preloadSceneInfo.GetLabel () + "' because the loading scene overrides it - discarding preloaded data.");
			}
			preloadAsync = null;
			preloadSceneInfo = new SceneInfo ("", -1);

			isLoading = true;
			loadingProgress = 0f;

			loadingSceneInfo.LoadLevel ();
			yield return null;

			if (KickStarter.player != null)
			{
				KickStarter.player.transform.position += new Vector3 (0f, -10000f, 0f);
			}

			PrepareSceneForExit (true, false);
			if (loadAsynchronously)
			{
				if (KickStarter.settingsManager.loadingDelay > 0f)
				{
					float waitForTime = Time.realtimeSinceStartup + KickStarter.settingsManager.loadingDelay;
					while (Time.realtimeSinceStartup < waitForTime && KickStarter.settingsManager.loadingDelay > 0f)
					{
						yield return null;
					}
				}

				AsyncOperation aSync = nextSceneInfo.LoadLevelASync ();

				if (KickStarter.settingsManager.loadingDelay > 0f)
				{
					aSync.allowSceneActivation = false;

					while (aSync.progress < 0.9f)
					{
						loadingProgress = aSync.progress;
						yield return null;
					}
				
					isLoading = false;

					float waitForTime = Time.realtimeSinceStartup + KickStarter.settingsManager.loadingDelay;
					while (Time.realtimeSinceStartup < waitForTime && KickStarter.settingsManager.loadingDelay > 0f)
					{
						yield return null;
					}

					aSync.allowSceneActivation = true;
				}
				else
				{
					while (!aSync.isDone)
					{
						loadingProgress = aSync.progress;
						yield return null;
					}
				}

				KickStarter.stateHandler.IgnoreNavMeshCollisions ();
			}
			else
			{
				nextSceneInfo.LoadLevel ();
			}

			isLoading = false;

			StartCoroutine (OnCompleteSceneChange ());
		}


		private IEnumerator LoadLevelAsync (SceneInfo nextSceneInfo)
		{
			isLoading = true;
			loadingProgress = 0f;
			PrepareSceneForExit (true, false);

			AsyncOperation aSync = null;
			if (nextSceneInfo.Matches (preloadSceneInfo))
			{
				aSync = preloadAsync;
				aSync.allowSceneActivation = true;
			}
			else
			{
				aSync = nextSceneInfo.LoadLevelASync ();
			}

			while (!aSync.isDone)
			{
				loadingProgress = aSync.progress;
				yield return null;
			}

			KickStarter.stateHandler.IgnoreNavMeshCollisions ();
			isLoading = false;
			preloadAsync = null;
			preloadSceneInfo = new SceneInfo ("", -1);

			StartCoroutine (OnCompleteSceneChange ());
		}


		private IEnumerator PreloadLevelAsync (SceneInfo nextSceneInfo)
		{
			// Wait for any other loading operations to complete
			while (isLoading)
			{
				yield return null;
			}

			loadingProgress = 0f;

			preloadSceneInfo = nextSceneInfo;
			preloadAsync = nextSceneInfo.LoadLevelASync ();

			preloadAsync.allowSceneActivation = false;

			// Wait until done and collect progress as we go.
			while (!preloadAsync.isDone)
			{
				loadingProgress = preloadAsync.progress;
				if (loadingProgress >= 0.9f)
				{
					// Almost done.
					break;
				}
				yield return null;
			}

			if (KickStarter.eventManager != null)
			{
				KickStarter.eventManager.Call_OnCompleteScenePreload (nextSceneInfo);
			}
		}


		private IEnumerator LoadLevelCo (SceneInfo nextSceneInfo, bool forceReload = false)
		{
			isLoading = true;
			yield return new WaitForEndOfFrame ();

			nextSceneInfo.LoadLevel (forceReload);
			isLoading = false;

			StartCoroutine (OnCompleteSceneChange ());
		}


		private IEnumerator OnCompleteSceneChange ()
		{
			int _removeNPCID = removeNPCID;
			removeNPCID = 0;

			if (KickStarter.dialog)
			{
				KickStarter.dialog.KillDialog (true, true);
			}

			yield return new WaitForEndOfFrame ();
			
			if (_removeNPCID != 0)
			{
				NPC npcToRemove = Serializer.returnComponent <NPC> (_removeNPCID);
				if (npcToRemove != null)
				{
					if (takeNPCPosition)
					{
						KickStarter.player.Teleport (npcToRemove.transform.position);
						KickStarter.player.SetRotation (npcToRemove.TransformRotation);
						KickStarter.player.transform.localScale = npcToRemove.transform.localScale;
					}

					npcToRemove.transform.position += new Vector3 (100f, -100f, 100f);
				}
				_removeNPCID = 0;
			}
		}


		/**
		 * <summary>Saves the current scene objects, kills speech dialog etc.  This should if the scene is changed using a custom script, i.e. without using the provided 'Scene: Switch' Action.</summary>
		 */
		public void PrepareSceneForExit ()
		{
			PrepareSceneForExit (false, true);
		}


		private void PrepareSceneForExit (bool isInstant, bool saveRoomData)
		{
			if (isInstant)
			{
				KickStarter.mainCamera.FadeOut (0f);
				
				if (KickStarter.player)
				{
					KickStarter.player.Halt (false);
				}
				
				KickStarter.stateHandler.gameState = GameState.Normal;
			}
			
			Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			foreach (Sound sound in sounds)
			{
				sound.TryDestroy ();
			}

			KickStarter.playerMenus.ClearParents ();

			if (saveRoomData)
			{
				KickStarter.levelStorage.StoreAllOpenLevelData ();
				previousSceneInfo = new SceneInfo ();
				previousGlobalSceneInfo = new SceneInfo ();

				KickStarter.saveSystem.SaveCurrentPlayerData ();
			}
			subScenes.Clear ();

			playerOnTransition = KickStarter.player;

			if (KickStarter.eventManager != null)
			{
				KickStarter.eventManager.Call_OnBeforeChangeScene ();
			}
		}


		/** SUB-SCENES */

		/**
		 * <summary>Adds a new scene as a sub-scene, without affecting any other open scenes.</summary>
		 * <param name = "sceneInfo">The SceneInfo of the new scene to open</param>
		 */
		public bool AddSubScene (SceneInfo sceneInfo)
		{
			// Check if scene is already open
			if (sceneInfo.Matches (thisSceneInfo))
			{
				return false;
			}
		
			foreach (SubScene subScene in subScenes)
			{
				if (subScene.SceneInfo.Matches (sceneInfo))
				{
					return false;
				}
			}
		
			sceneInfo.AddLevel ();

			KickStarter.playerMenus.AfterSceneAdd ();
			return true;
		}


		public IEnumerator AddSubSceneCoroutine (SceneInfo sceneInfo)
		{
			// This is necessary in Unity 5.4+, otherwise the game locks
			yield return new WaitForEndOfFrame ();
			AddSubScene (sceneInfo);
		}


		/**
		 * <summary>Registers a SubScene component with the SceneChanger.</summary>
		 * <param name = "subScene">The SubScene component to register</param>
		 */
		public void RegisterSubScene (SubScene subScene)
		{
			if (!subScenes.Contains (subScene))
			{
				subScenes.Add (subScene);

				KickStarter.levelStorage.ReturnSubSceneData (subScene, isLoading);
				KickStarter.stateHandler.IgnoreNavMeshCollisions ();
			}
		}


		/**
		 * <summary>Gets an array of all open SubScenes.</summary>
		 * <returns>An array of all open SubScenes</returns>
		 */
		public SubScene[] GetSubScenes ()
		{
			return subScenes.ToArray ();
		}


		/**
		 * <summary>Removes a scene, without affecting any other open scenes, provided multiple scenes are open. If the active scene is removed, the last-added sub-scene will become the new active scene.</summary>
		 * <param name = "sceneInfo">The SceneInfo of the new scene to remove</param>
		 */
		public bool RemoveScene (SceneInfo sceneInfo)
		{
			// Kill actionlists
			KickStarter.actionListManager.KillAllFromScene (sceneInfo);

			if (thisSceneInfo.Matches (sceneInfo))
			{
				// Want to close active scene

				if (subScenes == null || subScenes.Count == 0)
				{
					ACDebug.LogWarning ("Cannot remove scene " + sceneInfo.number + ", as it is the only one open!");
					return false;
				}

				// Save active scene
				KickStarter.levelStorage.StoreCurrentLevelData ();

				// Make the last-opened subscene the new active one
				SubScene lastSubScene = subScenes [subScenes.Count-1];
				KickStarter.mainCamera.gameObject.SetActive (false);
				lastSubScene.MakeMain ();
				subScenes.Remove (lastSubScene);

				StartCoroutine (CloseScene (thisSceneInfo));
				thisSceneInfo = lastSubScene.SceneInfo;
				return true;
			}

			// Want to remove a sub-scene
			for (int i=0; i<subScenes.Count; i++)
			{
				if (subScenes[i].SceneInfo.Matches (sceneInfo))
				{
					// Save sub scene
					KickStarter.levelStorage.StoreSubSceneData (subScenes[i]);

					StartCoroutine (CloseScene (subScenes[i].SceneInfo));
					subScenes.RemoveAt (i);
					return true;
				}
			}

			return false;
		}


		private IEnumerator CloseScene (SceneInfo _sceneInfo)
		{
			yield return new WaitForEndOfFrame ();
			_sceneInfo.CloseLevel ();
			yield return new WaitForEndOfFrame (); // Necessary in 2018.2
			KickStarter.stateHandler.RegisterWithGameEngine ();
		}


		/**
		 * <summary>Saves data used by this script in a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData to save in.</param>
		 * <returns>The updated PlayerData</returns>
		 */
		public PlayerData SavePlayerData (PlayerData playerData)
		{
			playerData.previousScene = previousSceneInfo.number;
			playerData.previousSceneName = previousSceneInfo.name;

			System.Text.StringBuilder subSceneData = new System.Text.StringBuilder ();
			foreach (SubScene subScene in subScenes)
			{
				subSceneData.Append (subScene.SceneInfo.name + SaveSystem.colon + subScene.SceneInfo.number + SaveSystem.pipe);
			}
			if (subSceneData.Length > 0)
			{
				subSceneData.Remove (subSceneData.Length-1, 1);
			}
			playerData.openSubScenes = subSceneData.ToString ();

			return playerData;
		}


		/**
		 * <summary>Loads data used by this script from a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData to load from.</param>
		 * <param name = "loadSubScenes">If True, then sub-scenes will be loaded</param>
		 */
		public void LoadPlayerData (PlayerData playerData, bool loadSubScenes = true)
		{
			previousSceneInfo = new SceneInfo (playerData.previousSceneName, playerData.previousScene);
			foreach (SubScene subScene in subScenes)
			{
				subScene.SceneInfo.CloseLevel ();
			}
			subScenes.Clear ();

			if (loadSubScenes && playerData.openSubScenes != null && playerData.openSubScenes.Length > 0)
			{
				string[] subSceneArray = playerData.openSubScenes.Split (SaveSystem.pipe[0]);
				foreach (string chunk in subSceneArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					int _number = 0;
					int.TryParse (chunkData[0], out _number);
					SceneInfo sceneInfo = new SceneInfo (chunkData[0], _number);

					//AddSubScene (sceneInfo);
					StartCoroutine (AddSubSceneCoroutine (sceneInfo));
				}
			}

			KickStarter.stateHandler.RegisterWithGameEngine ();
		}


		/**
		 * <summary>Gets info about the previous scene.<</summary>
		 * <param name = "forPlayer">If True, then the previous scene will be assumed to be the player character's last-visited scene, as opposed to the last scene that was open</param>
		 * <returns>Info about the previous scene</returns>
		 */
		public SceneInfo GetPreviousSceneInfo (bool forPlayer = true)
		{
			if (forPlayer)
			{
				return previousSceneInfo;
			}
			return previousGlobalSceneInfo;
		}


		/**
		* <summary>Gets the index within the internal list of active sub-scenes that a GameObject is in.  This is not the build index of the scene, but the order in which the scene is loaded internally</summary>
		* <param name = "gameObject">The GameObject to get the sub-scene index for</param>
		* <returns>The index within the internal list of active sub-scenes that a GameObject is in, starting from 1.  If the gameObject is not found, or the sub-scene is not found, it will return 0</returns>
		*/
		public int GetSubSceneIndexOfGameObject (GameObject gameObject)
		{
			if (gameObject != null && subScenes != null && subScenes.Count > 0)
			{
				for (int i=0; i<subScenes.Count; i++)
				{
					if (gameObject.scene == subScenes[i].SceneSettings.gameObject.scene)
					{
						return i+1;
					}
				}
			}

			return 0;
		}

	}


	/**
	 * A container for information about a scene that can be loaded.
	 */
	public class SceneInfo
	{

		/** The scene's name */
		public string name;
		/** The scene's number. If name is left empty, this number will be used to reference the scene instead */
		public int number;


		/**
		 * A Constructor for the current active scene.
		 */
		public SceneInfo ()
		{
			number = UnityVersionHandler.GetCurrentSceneNumber ();
			name = UnityVersionHandler.GetCurrentSceneName ();
		}


		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_name">The scene's name</param>
		 * <param name = "_number">The scene's number. If name is left empty, this number will be used to reference the scene instead</param>
		 */
		public SceneInfo (string _name, int _number)
		{
			number = _number;
			name = _name;
		}


		/**
		 * <summary>A Constructor where only the scene's name is defined.</summary>
		 * <param name = "_name">The scene's name</param>
		 */
		public SceneInfo (string _name)
		{
			name = _name;
			number = -1;
		}


		/**
		 * <summary>A Constructor where only the scene's number is defined.</summary>
		 * <param name = "_name">The scene's build index number</param>
		 */
		public SceneInfo (int _number)
		{
			number = _number;
			name = string.Empty;
		}


		/**
		 * <summary>A Constructor.</summary>
		 * <param name = "chooseSeneBy">The method by which the scene is referenced (Name, Number)</param>
		 * <param name = "_name">The scene's name</param>
		 * <param name = "_number">The scene's number. If name is left empty, this number will be used to reference the scene instead</param>
		 */
		public SceneInfo (ChooseSceneBy chooseSceneBy, string _name, int _number)
		{
			if (chooseSceneBy == ChooseSceneBy.Number || string.IsNullOrEmpty (_name))
			{
				number = _number;
				name = string.Empty;
			}
			else
			{
				name = _name;
				number = -1;
			}
		}


		public bool IsValid ()
		{
			if (string.IsNullOrEmpty (name) && number < 0)
			{
				return false;
			}
			return true;
		}


		/**
		 * <summary>Checks if the variables in this instance of the class match another instance.</summary>
		 * <param name = "_sceneInfo">The other SceneInfo instance to compare</param>
		 * <returns>True if the variables in this instance of the class matches the other instance</returns>
		 */
		public bool Matches (SceneInfo _sceneInfo)
		{
			if (_sceneInfo != null)
			{
				if (number == _sceneInfo.number && number >= 0)
				{
					if (!string.IsNullOrEmpty (name) && name == _sceneInfo.name)
					{
						return true;
					}
					if (string.IsNullOrEmpty (name) || string.IsNullOrEmpty (_sceneInfo.name))
					{
						return true;
					}
				}
				else if (number == -1 || _sceneInfo.number == -1)
				{
					if (!string.IsNullOrEmpty (name))
					{
						return (name == _sceneInfo.name);
					}
				}
			}

			return false;
		}


		/*
		 * <summary>Gets a string with info about the scene the class represents.</summary>
		 * <returns>A string with info about the scene the class represents.</returns>
		 */
		public string GetLabel ()
		{
			if (!string.IsNullOrEmpty (name))
			{
				return name;
			}
			return number.ToString ();
		}


		/**
		 * <summary>Loads the scene normally.</summary>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 */
		public void LoadLevel (bool forceReload = false)
		{
			if (!string.IsNullOrEmpty (name))
			{
				UnityVersionHandler.OpenScene (name, forceReload);
			}
			else
			{
				UnityVersionHandler.OpenScene (number, forceReload);
			}
		}


		/**
		 * <summary>Adds the scene additively.</summary>
		 */
		public void AddLevel ()
		{
			if (!string.IsNullOrEmpty (name))
			{
				UnityVersionHandler.OpenScene (name, false, true);
			}
			else
			{
				UnityVersionHandler.OpenScene (number, false, true);
			}
		}


		/**
		 * <summary>Closes the scene additively.</summary>
		 * <returns>True if the operation was successful</returns>
		 */
		public bool CloseLevel ()
		{
			if (!string.IsNullOrEmpty (name))
			{
				return UnityVersionHandler.CloseScene (name);
			}
			return UnityVersionHandler.CloseScene (number);
		}


		/**
		 * <summary>Loads the scene asynchronously.</summary>
		 * <returns>The generated AsyncOperation class</returns>
		 */
		public AsyncOperation LoadLevelASync ()
		{
			return UnityVersionHandler.LoadLevelAsync (number, name);
		}


		/**
		 * Returns True if the scene data is empty
		 */
		public bool IsNull
		{
			get
			{
				if (string.IsNullOrEmpty (name) && number == -1)
				{
					return true;
				}
				return false;
			}
		}

	}

}