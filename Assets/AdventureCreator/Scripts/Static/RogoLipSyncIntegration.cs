﻿using UnityEngine;
#if RogoLipSyncIsPresent
using RogoDigital.Lipsync;
#endif

namespace AC
{

	/**
	 * A class the contains a number of static functions to assist with Rogo Digital LipSync integration.
	 * To use Rogo Digital LipSync with Adventure Creator, the 'RogoLipSyncIsPresent' preprocessor must be defined.
	 */
	public class RogoLipSyncIntegration
	{
		
		/**
		 * <summary>Checks if the 'RogoLipSyncIsPresent' preprocessor has been defined.</summary>
		 * <returns>True if the 'RogoLipSyncIsPresent' preprocessor has been defined</returns>
		 */
		public static bool IsDefinePresent ()
		{
			#if RogoLipSyncIsPresent
			return true;
			#else
			return false;
			#endif
		}


		public static void Play (Char speaker, int lineID, string language)
		{
			if (speaker == null)
			{
				return;
			}

			#if RogoLipSyncIsPresent
			if (lineID > -1 && speaker != null && KickStarter.speechManager.searchAudioFiles)
			{
				LipSyncData lipSyncData = (LipSyncData) KickStarter.runtimeLanguages.GetSpeechLipsyncFile <LipSyncData> (lineID, speaker);

				if (lipSyncData != null)
				{
					LipSync[] lipSyncs = speaker.GetComponentsInChildren <LipSync>();
					if (lipSyncs != null && lipSyncs.Length > 0)
					{
						foreach (LipSync lipSync in lipSyncs)
						{
							if (lipSync != null && lipSync.enabled)
							{
								lipSync.Play (lipSyncData);
							}
						}
					}
					else
					{
						ACDebug.LogWarning ("No LipSync component found on " + speaker.gameObject.name + " gameobject.");
					}
				}
			}
			#else
			ACDebug.LogError ("The 'RogoLipSyncIsPresent' preprocessor define must be declared in the Player Settings.");
			#endif
		}


		public static void Stop (Char speaker)
		{
			if (speaker == null)
			{
				return;
			}
			
			#if RogoLipSyncIsPresent
			LipSync[] lipSyncs = speaker.GetComponentsInChildren <LipSync>();
			if (lipSyncs != null && lipSyncs.Length > 0)
			{
				foreach (LipSync lipSync in lipSyncs)
				{
					if (lipSync != null && lipSync.enabled)
					{
						lipSync.Stop (true);
					}
				}
			}
			#endif
		}
		
	}

}