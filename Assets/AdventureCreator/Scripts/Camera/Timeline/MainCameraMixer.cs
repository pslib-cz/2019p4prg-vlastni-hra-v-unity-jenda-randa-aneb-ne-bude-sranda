/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"MainCameraMixer.cs"
 * 
 *	A PlayableBehaviour that allows for the MainCamera to cut to different _Camera instances on a Timeline.  This is adapted from CinemachineMixer.cs, published by Unity Technologies, and all credit goes to its respective authors.
 * 
 */

#if UNITY_2017_1_OR_NEWER

using UnityEngine.Playables;

namespace AC
{

	/**
	 * A PlayableBehaviour that allows for the MainCamera to cut to different _Camera instances on a Timeline.  This is adapted from CinemachineMixer.cs, published by Unity Technologies, and all credit goes to its respective authors.
	 */
	internal sealed class MainCameraMixer : PlayableBehaviour
	{

		#region PublicFunctions

		public override void OnGraphStop (Playable playable)
		{
			if (MainCamera != null)
			{
				MainCamera.ReleaseTimelineOverride ();
			}
		}


		public override void ProcessFrame (Playable playable, FrameData info, object playerData)
		{
			base.ProcessFrame (playable, info, playerData);

			if (MainCamera == null)
			{
				return;
			}

			int activeInputs = 0;
			ClipInfo clipA = new ClipInfo ();
			ClipInfo clipB = new ClipInfo ();

			for (int i=0; i<playable.GetInputCount (); ++i)
			{
				float weight = playable.GetInputWeight (i);
				ScriptPlayable <MainCameraPlayableBehaviour> clip = (ScriptPlayable <MainCameraPlayableBehaviour>) playable.GetInput (i);

				MainCameraPlayableBehaviour shot = clip.GetBehaviour ();
				if (shot != null && 
					shot.IsValid &&
					playable.GetPlayState() == PlayState.Playing &&
					weight > 0.0001f)
				{
					clipA = clipB;
					clipB.camera = shot.gameCamera;
					clipB.weight = weight;
					clipB.localTime = clip.GetTime ();
					clipB.duration = clip.GetDuration ();

					if (++activeInputs == 2)
					{
						break;
					}
				}
			}

			// Figure out which clip is incoming
			bool incomingIsB = clipB.weight >= 1 || clipB.localTime < clipB.duration / 2;
			if (activeInputs == 2)
			{
				if (clipB.localTime > clipA.localTime)
				{
					incomingIsB = true;
				}
				else if (clipB.localTime < clipA.localTime)
				{
					incomingIsB = false;
				}
				else 
				{
					incomingIsB = clipB.duration >= clipA.duration;
				}
			}

			_Camera cameraA = incomingIsB ? clipA.camera : clipB.camera;
			_Camera cameraB = incomingIsB ? clipB.camera : clipA.camera;
			float camWeightB = incomingIsB ? clipB.weight : 1 - clipB.weight;

			if (cameraB == null)
			{
				cameraB = cameraA;
				cameraA = null;
				camWeightB = 1f - camWeightB;
			}

			MainCamera.SetTimelineOverride (cameraA, cameraB, camWeightB);
		}

		#endregion


		#region GetSet

		private MainCamera MainCamera
		{
			get
			{
				return KickStarter.mainCamera;
			}
		}

		#endregion


		#region PrivateStructs

		private struct ClipInfo
		{

			public _Camera camera;
			public float weight;
			public double localTime;
			public double duration;

		}

		#endregion

	}

}

#endif