﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"ActionPlayerSwitch.cs"
 * 
 *	This action causes a different Player prefab
 *	to be controlled.  Note that only one Player prefab
 *  can exist in a scene at any one time - for two player
 *  "characters" to be present, one must be a swapped-out
 * 	NPC instead.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionPlayerSwitch : Action
	{
		
		public int playerID;
		public int playerNumber;
		
		public NewPlayerPosition newPlayerPosition = NewPlayerPosition.ReplaceNPC;
		public OldPlayer oldPlayer = OldPlayer.RemoveFromScene;
		
		public bool restorePreviousData = false;
		public bool keepInventory = false;
		
		public ChooseSceneBy chooseNewSceneBy = ChooseSceneBy.Number;
		public int newPlayerScene;
		public string newPlayerSceneName;
		
		public int oldPlayerNPC_ID;
		public NPC oldPlayerNPC;
		
		public int newPlayerNPC_ID;
		public NPC newPlayerNPC;
		
		public int newPlayerMarker_ID;
		public Marker newPlayerMarker;

		#if UNITY_EDITOR
		private SettingsManager settingsManager;
		#endif
		
		
		public ActionPlayerSwitch ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Player;
			title = "Switch";
			description = "Swaps out the Player prefab mid-game. If the new prefab has been used before, you can restore that prefab's position data – otherwise you can set the position or scene of the new player. This Action only applies to games for which 'Player switching' has been allowed in the Settings Manager.";
		}
		
		
		override public void AssignValues ()
		{
			newPlayerNPC = AssignFile <NPC> (newPlayerNPC_ID, newPlayerNPC);
			newPlayerMarker = AssignFile <Marker> (newPlayerMarker_ID, newPlayerMarker);

			if (oldPlayer == OldPlayer.ReplaceWithAssociatedNPC)
			{
				if (KickStarter.player != null)
				{
					if (KickStarter.player.associatedNPCPrefab != null)
					{
						ConstantID associatedNPCPrefabID = KickStarter.player.associatedNPCPrefab.GetComponent <ConstantID>();
						if (associatedNPCPrefabID != null)
						{
							oldPlayerNPC_ID = associatedNPCPrefabID.constantID;
						}
					}
				}
			}

			oldPlayerNPC = AssignFile <NPC> (oldPlayerNPC_ID, oldPlayerNPC);
		}
		
		
		override public float Run ()
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				if (KickStarter.sceneChanger.GetSubScenes ().Length > 0)
				{
				//	ACDebug.LogWarning ("Cannot switch players while multiple scenes are open!");
				//	return 0f;	
				}

				if (KickStarter.settingsManager.players.Count > 0 && KickStarter.settingsManager.players.Count > playerNumber && playerNumber > -1)
				{
					if (KickStarter.player != null && KickStarter.player.ID == playerID)
					{
						ACDebug.Log ("Cannot switch player - already controlling the desired prefab.");
						return 0f;
					}
					
					if (KickStarter.settingsManager.players[playerNumber].playerOb != null)
					{
						KickStarter.saveSystem.SaveCurrentPlayerData ();
						
						Vector3 oldPlayerPosition = Vector3.zero;
						Quaternion oldPlayerRotation = new Quaternion ();
						Vector3 oldPlayerScale = Vector3.one;
						
						if (KickStarter.player != null)
						{
							oldPlayerPosition = KickStarter.player.transform.position;
							oldPlayerRotation = KickStarter.player.TransformRotation;
							oldPlayerScale = KickStarter.player.transform.localScale;
						}

						if (newPlayerPosition != NewPlayerPosition.ReplaceCurrentPlayer)
						{
							if (oldPlayer == OldPlayer.ReplaceWithAssociatedNPC && (oldPlayerNPC == null || !oldPlayerNPC.gameObject.activeInHierarchy) && KickStarter.player.associatedNPCPrefab != null)
							{
								GameObject newObject = (GameObject) Instantiate (KickStarter.player.associatedNPCPrefab.gameObject);
								newObject.name = KickStarter.player.associatedNPCPrefab.gameObject.name;
								oldPlayerNPC = newObject.GetComponent <NPC>();
							}

							if ((oldPlayer == OldPlayer.ReplaceWithNPC || oldPlayer == OldPlayer.ReplaceWithAssociatedNPC) &&
								oldPlayerNPC != null && oldPlayerNPC.gameObject.activeInHierarchy)
							{
								oldPlayerNPC.transform.position = oldPlayerPosition;
								oldPlayerNPC.TransformRotation = oldPlayerRotation;
								oldPlayerNPC.transform.localScale = oldPlayerScale;

								// Force the rotation / sprite child to update
								oldPlayerNPC._Update ();
							}
						}

						if (newPlayerNPC == null || newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
						{
							// Try to find from associated NPC prefab

							if (KickStarter.settingsManager.players[playerNumber].playerOb.associatedNPCPrefab != null)
							{
								ConstantID prefabID = KickStarter.settingsManager.players[playerNumber].playerOb.associatedNPCPrefab.GetComponent <ConstantID>();
								if (prefabID != null && prefabID.constantID != 0)
								{
									newPlayerNPC_ID = prefabID.constantID;
									newPlayerNPC = AssignFile <NPC> (prefabID.constantID, null);
								}
							}
						}

						Quaternion newRotation = Quaternion.identity;
						if (newPlayerPosition == NewPlayerPosition.ReplaceCurrentPlayer)
						{
							newRotation = oldPlayerRotation;
						}
						else if (newPlayerPosition == NewPlayerPosition.ReplaceNPC && newPlayerNPC)
						{
							newRotation = newPlayerNPC.TransformRotation;
						}
						else if (newPlayerPosition == NewPlayerPosition.AppearAtMarker && newPlayerMarker)
						{
							newRotation = newPlayerMarker.transform.rotation;
						}

						bool replacesOldPlayer = newPlayerPosition == NewPlayerPosition.ReplaceCurrentPlayer &&
												 (!restorePreviousData || !KickStarter.saveSystem.DoesPlayerDataExist (playerID, true));
						
						KickStarter.ResetPlayer (KickStarter.settingsManager.players[playerNumber].playerOb, playerID, true, newRotation, keepInventory, false, replacesOldPlayer);
						Player newPlayer = KickStarter.player;
						PlayerMenus.ResetInventoryBoxes ();

						if (restorePreviousData && KickStarter.saveSystem.DoesPlayerDataExist (playerID, true))
						{
							if (newPlayerNPC)
							{
								newPlayerNPC.transform.position += new Vector3 (100f, -100f, 100f);
							}

							int sceneToLoad = KickStarter.saveSystem.GetPlayerScene (playerID);
							if (sceneToLoad >= 0 && sceneToLoad != UnityVersionHandler.GetCurrentSceneNumber ())
							{
								KickStarter.saveSystem.loadingGame = LoadingGame.JustSwitchingPlayer;
								KickStarter.sceneChanger.ChangeScene (new SceneInfo ("", sceneToLoad), true, false, newPlayerNPC_ID);
							}
							else
							{
								string sceneToLoadName = KickStarter.saveSystem.GetPlayerSceneName (playerID);
								if (sceneToLoadName != "" && sceneToLoadName != UnityVersionHandler.GetCurrentSceneName ())
								{
									KickStarter.saveSystem.loadingGame = LoadingGame.JustSwitchingPlayer;
									KickStarter.sceneChanger.ChangeScene (new SceneInfo (sceneToLoadName, -1), true, false, newPlayerNPC_ID);
								}
							}
						}
						else
						{
							// No data to restore

							if (newPlayerPosition == NewPlayerPosition.ReplaceCurrentPlayer)
							{
								newPlayer.Teleport (oldPlayerPosition);
								newPlayer.SetRotation (oldPlayerRotation);
								newPlayer.transform.localScale = oldPlayerScale;
							}
							else if (newPlayerPosition == NewPlayerPosition.ReplaceNPC || newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
							{
								if (newPlayerNPC)
								{
									newPlayer.Teleport (newPlayerNPC.transform.position);
									newPlayer.SetRotation (newPlayerNPC.TransformRotation);
									newPlayer.transform.localScale = newPlayerNPC.transform.localScale;
									
									newPlayerNPC.transform.position += new Vector3 (100f, -100f, 100f);
								}
							}
							else if (newPlayerPosition == NewPlayerPosition.AppearAtMarker)
							{
								if (newPlayerMarker)
								{
									newPlayer.Teleport (newPlayerMarker.transform.position);
									newPlayer.SetRotation (newPlayerMarker.transform.rotation);
									newPlayer.transform.localScale = newPlayerMarker.transform.localScale;
								}
							}
							else if (newPlayerPosition == NewPlayerPosition.AppearInOtherScene)
							{
								if (chooseNewSceneBy == ChooseSceneBy.Name && newPlayerSceneName == UnityVersionHandler.GetCurrentSceneName () ||
									(chooseNewSceneBy == ChooseSceneBy.Number && newPlayerScene == UnityVersionHandler.GetCurrentSceneNumber ()))
								{
									// Already in correct scene
									if (newPlayerNPC && newPlayerNPC.gameObject.activeInHierarchy)
									{
										newPlayer.Teleport (newPlayerNPC.transform.position);
										newPlayer.SetRotation (newPlayerNPC.TransformRotation);
										newPlayer.transform.localScale = newPlayerNPC.transform.localScale;
										
										newPlayerNPC.transform.position += new Vector3 (100f, -100f, 100f);
									}
								}
								else
								{
									if (newPlayerNPC && newPlayerNPC.gameObject.activeInHierarchy)
									{
										newPlayerNPC.transform.position += new Vector3 (100f, -100f, 100f);
									}

									//KickStarter.saveSystem.loadingGame = LoadingGame.JustSwitchingPlayer;
									KickStarter.sceneChanger.ChangeScene (new SceneInfo (chooseNewSceneBy, newPlayerSceneName, newPlayerScene), true, false, newPlayerNPC_ID, true);
								}
							}
						}
						
						if (KickStarter.mainCamera.attachedCamera)
						{
							KickStarter.mainCamera.attachedCamera.MoveCameraInstant ();
						}
						
						AssetLoader.UnloadAssets ();
					}
					else
					{
						ACDebug.LogWarning ("Cannot switch player - no player prefabs is defined.");
					}
				}
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI ()
		{
			if (!settingsManager)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (!settingsManager)
			{
				return;
			}
			
			if (settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				EditorGUILayout.HelpBox ("This Action requires Player Switching to be allowed, as set in the Settings Manager.", MessageType.Info);
				return;
			}
			
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			playerNumber = -1;
			
			if (settingsManager.players.Count > 0)
			{
				foreach (PlayerPrefab playerPrefab in settingsManager.players)
				{
					if (playerPrefab.playerOb != null)
					{
						labelList.Add (playerPrefab.playerOb.name);
					}
					else
					{
						labelList.Add ("(Undefined prefab)");
					}
					
					// If a player has been removed, make sure selected player is still valid
					if (playerPrefab.ID == playerID)
					{
						playerNumber = i;
					}
					
					i++;
				}
				
				if (playerNumber == -1)
				{
					// Wasn't found (item was possibly deleted), so revert to zero
					ACDebug.LogWarning ("Previously chosen Player no longer exists!");
					
					playerNumber = 0;
					playerID = 0;
				}
				
				playerNumber = EditorGUILayout.Popup ("New Player:", playerNumber, labelList.ToArray());
				playerID = settingsManager.players[playerNumber].ID;
				
				if (AdvGame.GetReferences ().settingsManager == null || !AdvGame.GetReferences ().settingsManager.shareInventory)
				{
					keepInventory = EditorGUILayout.Toggle ("Transfer inventory?", keepInventory);
				}
				restorePreviousData = EditorGUILayout.Toggle ("Restore position?", restorePreviousData);
				if (restorePreviousData)
				{
					EditorGUILayout.BeginVertical (CustomStyles.thinBox);
					EditorGUILayout.LabelField ("If first time in game:", EditorStyles.boldLabel);
				}
				
				newPlayerPosition = (NewPlayerPosition) EditorGUILayout.EnumPopup ("New Player position:", newPlayerPosition);
				
				if (newPlayerPosition == NewPlayerPosition.ReplaceNPC)
				{
					newPlayerNPC = (NPC) EditorGUILayout.ObjectField ("NPC to be replaced:", newPlayerNPC, typeof (NPC), true);
					
					newPlayerNPC_ID = FieldToID <NPC> (newPlayerNPC, newPlayerNPC_ID);
					newPlayerNPC = IDToField <NPC> (newPlayerNPC, newPlayerNPC_ID, false);
				}
				else if (newPlayerPosition == NewPlayerPosition.AppearAtMarker)
				{
					newPlayerMarker = (Marker) EditorGUILayout.ObjectField ("Marker to appear at:", newPlayerMarker, typeof (Marker), true);
					
					newPlayerMarker_ID = FieldToID <Marker> (newPlayerMarker, newPlayerMarker_ID);
					newPlayerMarker = IDToField <Marker> (newPlayerMarker, newPlayerMarker_ID, false);
				}
				else if (newPlayerPosition == NewPlayerPosition.AppearInOtherScene)
				{
					chooseNewSceneBy = (ChooseSceneBy) EditorGUILayout.EnumPopup ("Choose scene by:", chooseNewSceneBy);
					if (chooseNewSceneBy == ChooseSceneBy.Name)
					{
						newPlayerSceneName = EditorGUILayout.TextField ("Scene to appear in:", newPlayerSceneName);
					}
					else
					{
						newPlayerScene = EditorGUILayout.IntField ("Scene to appear in:", newPlayerScene);
					}

					newPlayerNPC = (NPC) EditorGUILayout.ObjectField ("NPC to be replaced:", newPlayerNPC, typeof (NPC), true);
					
					newPlayerNPC_ID = FieldToID <NPC> (newPlayerNPC, newPlayerNPC_ID);
					newPlayerNPC = IDToField <NPC> (newPlayerNPC, newPlayerNPC_ID, false);

					EditorGUILayout.HelpBox ("If the Player has an Associated NPC defined, it will be used if none is defined here.", MessageType.Info);
				}
				else if (newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
				{
					EditorGUILayout.HelpBox ("A Player's 'Associated NPC' is defined in the Player Inspector.", MessageType.Info);
				}

				if (restorePreviousData)
				{
					EditorGUILayout.EndVertical ();
				}

				if (newPlayerPosition == NewPlayerPosition.ReplaceNPC ||
					newPlayerPosition == NewPlayerPosition.AppearAtMarker ||
					newPlayerPosition == NewPlayerPosition.AppearInOtherScene ||
					newPlayerPosition == NewPlayerPosition.ReplaceAssociatedNPC)
				{
					EditorGUILayout.Space ();
					oldPlayer = (OldPlayer) EditorGUILayout.EnumPopup ("Old Player:", oldPlayer);
					
					if (oldPlayer == OldPlayer.ReplaceWithNPC)
					{
						oldPlayerNPC = (NPC) EditorGUILayout.ObjectField ("NPC to replace old Player:", oldPlayerNPC, typeof (NPC), true);
						
						oldPlayerNPC_ID = FieldToID <NPC> (oldPlayerNPC, oldPlayerNPC_ID);
						oldPlayerNPC = IDToField <NPC> (oldPlayerNPC, oldPlayerNPC_ID, false);

						EditorGUILayout.HelpBox ("This NPC must be already be present in the scene - either within the scene file itself, or spawned at runtime with the 'Object: Add or remove' Action.", MessageType.Info);
					}
					else if (oldPlayer == OldPlayer.ReplaceWithAssociatedNPC)
					{
						EditorGUILayout.HelpBox ("A Player's 'Associated NPC' is defined in the Player Inspector.", MessageType.Info);
					}
				}
			}
			
			else
			{
				EditorGUILayout.LabelField ("No players exist!");
				playerID = -1;
				playerNumber = -1;
			}
			
			EditorGUILayout.Space ();
			
			AfterRunningOption ();
		}


		override public void AssignConstantIDs (bool saveScriptsToo)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberNPC> (oldPlayerNPC);
				AddSaveScript <RememberNPC> (newPlayerNPC);
			}

			AssignConstantID <NPC> (oldPlayerNPC, oldPlayerNPC_ID, 0);
			AssignConstantID <NPC> (newPlayerNPC, newPlayerNPC_ID, 0);
			AssignConstantID <Marker> (newPlayerMarker, newPlayerMarker_ID, 0);
		}

		
		public override string SetLabel ()
		{
			if (settingsManager == null)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (settingsManager != null && settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				if (settingsManager.players.Count > 0 && settingsManager.players.Count > playerNumber && playerNumber > -1)
				{
					if (settingsManager.players[playerNumber].playerOb != null)
					{
						return settingsManager.players[playerNumber].playerOb.name;
					}
					else
					{
						return "Undefined prefab";
					}
				}
			}
			
			return string.Empty;
		}
		
		#endif
		
	}
	
}