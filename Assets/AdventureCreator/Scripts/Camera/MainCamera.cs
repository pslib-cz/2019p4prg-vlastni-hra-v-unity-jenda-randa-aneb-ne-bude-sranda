/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"MainCamera.cs"
 * 
 *	This is attached to the Main Camera, and must be tagged as "MainCamera" to work.
 *	Only one Main Camera should ever exist in the scene.
 *
 *	Shake code adapted from Mike Jasper's code: http://www.mikedoesweb.com/2012/camera-shake-in-unity/
 *
 *  Aspect-ratio code adapated from Eric Haines' code: http://wiki.unity3d.com/index.php?title=AspectRatioEnforcer
 * 
 */

#if !UNITY_5_0 && (UNITY_5 || UNITY_2017) && (UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS)
#define ALLOW_VR
#endif

#if UNITY_2018_2_OR_NEWER
#define ALLOW_PHYSICAL_CAMERA
#endif

using UnityEngine;
using System.Collections;
#if ALLOW_VR
using UnityEngine.VR;
#endif

namespace AC
{

	/**
	 * This is attached to the scene's Main Camera, and must be tagged as "MainCamera".
	 * The camera system works by having the MainCamera attach itself to the "active" _Camera component.
	 * Each _Camera component is merely used for reference - only the MainCamera actually performs any rendering.
	 * Shake code adapted from Mike Jasper's code: http://www.mikedoesweb.com/2012/camera-shake-in-unity/
	 * Aspect-ratio code adapated from Eric Haines' code: http://wiki.unity3d.com/index.php?title=AspectRatioEnforcer
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_main_camera.html")]
	#endif
	public class MainCamera : MonoBehaviour
	{

		/** The texture to display fullscreen when fading */
		public Texture2D fadeTexture;
		private Texture2D tempFadeTexture = null;

		/** The current active camera, i.e. the one that the MainCamera is attaching itself to */
		public _Camera attachedCamera;
		/** The last active camera during gameplay */
		public _Camera lastNavCamera;
		/** The last-but-one active camera during gameplay */
		public _Camera lastNavCamera2;

		private _Camera transitionFromCamera;

		private enum MainCameraMode { NormalSnap, NormalTransition };
		private MainCameraMode mainCameraMode = MainCameraMode.NormalSnap;

		private bool isCrossfading;
		private Texture2D crossfadeTexture;
		
		private Vector2 perspectiveOffset = new Vector2 (0f, 0f);

		private float fadeDuration, fadeTimer;
		private int drawDepth = -1000;
		private float alpha = 0f; 
		private FadeType fadeType;
		private bool hideSceneWhileLoading;

		private GameCameraData currentFrameCameraData;
		
		private MoveMethod moveMethod;
		private float transitionDuration, transitionTimer;
		private AnimationCurve timeCurve;

		private _Camera previousAttachedCamera = null;
		private GameCameraData oldCameraData;
		private bool retainPreviousSpeed = false;

		private Texture2D actualFadeTexture = null;
		private float shakeStartTime;
		private float shakeDuration;
		private float shakeStartIntensity;
		private CameraShakeEffect shakeEffect;
		private float shakeIntensity;
		private Vector3 shakePosition;
		private Vector3 shakeRotation;
		
		// Aspect ratio
		private Camera borderCam;
		private float borderWidth;
		private MenuOrientation borderOrientation;
		private Rect borderRect1 = new Rect (0f, 0f, 0f, 0f);
		private Rect borderRect2 = new Rect (0f, 0f, 0f, 0f);
		private Rect midBorderRect = new Rect (0f, 0f, 0f, 0f);

		private Vector2 aspectRatioScaleCorrection = Vector2.zero;
		private Vector2 aspectRatioOffsetCorrection = Vector2.zero;

		// Split-screen
		/** If True, the game window is shared with another _Camera */
		public bool isSplitScreen;
		/** If True, then this Camera takes up the left or top half of a split-screen effect, if isSplitScreen = True */
		public bool isTopLeftSplit;
		/** The orientation of the split-screen divider, if isSplitScreen = True (Horizontal, Vertical) */
		public MenuOrientation splitOrientation;
		/** The _Camera to share the game window with, if isSplitScreen = True */
		public _Camera splitCamera;
		/** The portion of the screen that this Camera takes up, if isSplitScreen = True */
		public float splitAmountMain = 0.49f;
		/** The portion of the screen that splitCamera takes up, if isSplitScreen = True */
		public float splitAmountOther = 0.49f;
		
		// Custom FX
		private float focalDistance = 10f;
		
		private Camera ownCamera;
		private AudioListener _audioListener;

		#if ALLOW_VR
		/** If True, the camera's position and rotation will be restored when loading (VR only) */
		public bool restoreTransformOnLoadVR = false;
		#endif


		public void OnAwake (bool hideWhileLoading = true)
		{
			Initialise ();

			gameObject.tag = Tags.mainCamera;
			
			if (hideWhileLoading)
			{
				hideSceneWhileLoading = true;
			}

			if (this.transform.parent && this.transform.parent.name != "_Cameras")
			{
				ACDebug.LogWarning ("Note: The MainCamera is parented to an unknown object. Be careful when moving the parent, as it may cause mis-alignment when the MainCamera is attached to a GameCamera.", this);
			}

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.forceAspectRatio)
			{
				#if !UNITY_IPHONE
				KickStarter.settingsManager.landscapeModeOnly = false;
				#endif
				if (SetAspectRatio ())
				{
					CreateBorderCamera ();
				}
				SetCameraRect ();
			}
		}
		

		/**
		 * <summary>Initialises lookAtTransform if none exists and assigns fadeTexture.</summary>
		 * <param name = "_fadeTexture">The new fadeTexture to use, if not null</param>
		 */
		public void Initialise (Texture2D _fadeTexture = null)
		{
			if (_fadeTexture != null)
			{
				fadeTexture = _fadeTexture;
			}
		}
		
		
		public void OnStart ()
		{
			AssignFadeTexture ();
			if (KickStarter.sceneChanger != null)
			{
				SetFadeTexture (KickStarter.sceneChanger.GetAndResetTransitionTexture ());
			}

			if (KickStarter.playerMenus.ArePauseMenusOn ())
			{
				hideSceneWhileLoading = false;
			}
			else
			{
				StartCoroutine ("ShowScene");
			}
		}
		
		
		private IEnumerator ShowScene ()
		{
			yield return new WaitForSeconds (0.1f);
			hideSceneWhileLoading = false;
		}
		

		/**
		 * <summary>Pauses the game.</summary>
		 * <param name = "canWait">If True and the game cannot currently be paused, the game will paused at the next possible time</para>
		 */
		public void PauseGame (bool canWait = false)
		{
			if (hideSceneWhileLoading)
			{
				if (canWait)
				{
					StartCoroutine ("PauseWhenLoaded"); //
				}
			}
			else
			{
				KickStarter.stateHandler.gameState = GameState.Paused;
				KickStarter.sceneSettings.PauseGame ();
			}
		}


		public void CancelPauseGame ()
		{
			StopCoroutine ("PauseWhenLoaded");
		}

		
		private IEnumerator PauseWhenLoaded ()
		{
			while (hideSceneWhileLoading)
			{
				yield return new WaitForEndOfFrame ();
			}
			KickStarter.stateHandler.gameState = GameState.Paused;
		}
		

		/**
		 * Displays the fadeTexture full-screen for a brief moment while the scene loads.
		 */
		public void HideScene ()
		{
			hideSceneWhileLoading = true;
			StartCoroutine ("ShowScene");
		}
		

		/**
		 * <summary>Shakes the Camera, creating an "earthquake" effect.</summary>
		 * <param name = "_shakeIntensity">The shake intensity</param>
		 * <param name = "_duration">The duration of the effect, in sectonds</param>
		 * <param name = "_shakeEffect">The type of shaking to make (Translate, Rotate, TranslateAndRotate)</param>
		 */
		public void Shake (float _shakeIntensity, float _duration, CameraShakeEffect _shakeEffect)
		{
			shakePosition = Vector3.zero;
			shakeRotation = Vector3.zero;
			
			shakeEffect = _shakeEffect;
			shakeDuration = _duration;
			shakeStartTime = Time.time;
			shakeIntensity = _shakeIntensity;
			
			shakeStartIntensity = shakeIntensity;

			KickStarter.eventManager.Call_OnShakeCamera (shakeIntensity, shakeDuration);
		}
		

		/**
		 * <summary>Checks if the Camera is shaking.</summary>
		 * <returns>True if the Camera is shaking</returns>
		 */
		public bool IsShaking ()
		{
			if (shakeIntensity > 0f)
			{
				return true;
			}
			
			return false;
		}
		

		/**
		 * Ends the "earthquake" shake effect.
		 */
		public void StopShaking ()
		{
			shakeIntensity = 0f;
			shakePosition = Vector3.zero;
			shakeRotation = Vector3.zero;

			KickStarter.eventManager.Call_OnShakeCamera (0f, 0f);
		}
		

		/**
		 * Prepares the Camera for being able to render a BackgroundImage underneath scene objects.
		 */
		public void PrepareForBackground ()
		{
			Camera.clearFlags = CameraClearFlags.Depth;
			
			if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) != -1)
			{
				Camera.cullingMask = Camera.cullingMask & ~(1 << LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer));
			}
		}
		
		
		private void RemoveBackground ()
		{
			Camera.clearFlags = CameraClearFlags.Skybox;
			
			if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) != -1)
			{
				Camera.cullingMask = Camera.cullingMask & ~(1 << LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer));
			}
		}
		

		/**
		 * Activates the FirstPersonCamera found in the Player prefab.
		 */
		public void SetFirstPerson ()
		{
			if (KickStarter.player == null)
			{
				ACDebug.LogWarning ("Cannot set first-person camera because no Player can be found!");
				return;
			}

			FirstPersonCamera firstPersonCamera = KickStarter.player.GetComponentInChildren<FirstPersonCamera>();
			if (firstPersonCamera)
			{
				SetGameCamera (firstPersonCamera);
			}
			
			if (attachedCamera)
			{
				if (lastNavCamera != attachedCamera)
				{
					lastNavCamera2 = lastNavCamera;
				}
				
				lastNavCamera = attachedCamera;
			}
		}


		private void UpdateCameraFade ()
		{
			if (fadeTimer > 0f)
			{
				fadeTimer -= Time.deltaTime;
				alpha = 1f - (fadeTimer / fadeDuration);
				
				if (fadeType == FadeType.fadeIn)
				{
					alpha = 1f - alpha;
				}
				
				alpha = Mathf.Clamp01 (alpha);
				
				if (fadeTimer <= 0f)
				{
					if (fadeType == FadeType.fadeIn)
					{
						alpha = 0f;
					}
					else
					{
						alpha = 1f;
					}
					
					fadeDuration = fadeTimer = 0f;
					StopCrossfade ();
				}
			}
		}
		

		/**
		 * <summary>Draws the Camera's fade texture. This is called every OnGUI call by StateHandler.</summary>
		 */
		public void DrawCameraFade ()
		{
			if (hideSceneWhileLoading && actualFadeTexture != null)
			{
				GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), actualFadeTexture);
			}
			else if (alpha > 0f)
			{
				Color tempColor = GUI.color;
				tempColor.a = alpha;
				GUI.color = tempColor;
				GUI.depth = drawDepth;
				
				if (isCrossfading)
				{
					if (crossfadeTexture)
					{
						GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), crossfadeTexture);
					}
					else
					{
						ACDebug.LogWarning ("Cannot crossfade as the crossfade texture was not succesfully generated.");
					}
				}
				else
				{
					if (actualFadeTexture)
					{
						GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), actualFadeTexture);
					}
					else
					{
						ACDebug.LogWarning ("Cannot fade camera as no fade texture has been assigned.");
					}
				}
			}
			else if (actualFadeTexture != fadeTexture && !isFading())
			{
				ReleaseFadeTexture ();
			}
		}
		

		/**
		 * Resets the Camera's projection matrix.
		 */
		public void ResetProjection ()
		{
			if (Camera != null)
			{
				perspectiveOffset = Vector2.zero;
				Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, perspectiveOffset);
				Camera.ResetProjectionMatrix ();
			}
		}
		

		/**
		 * Resets the transition effect when moving from one _Camera to another.
		 */
		public void ResetMoving ()
		{
			mainCameraMode = MainCameraMode.NormalSnap;
			transitionTimer = 0f;
			transitionDuration = 0f;
		}
		

		/**
		 * Updates the Camera's position.
		 * This is called every frame by StateHandler.
		 */
		public void _LateUpdate ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			UpdateCameraFade ();

			if (KickStarter.stateHandler.IsInGameplay ())
			{
				if (attachedCamera)
				{
					if (lastNavCamera != attachedCamera)
					{
						lastNavCamera2 = lastNavCamera;
					}
					
					lastNavCamera = attachedCamera;
				}
			}
			
			if (attachedCamera && (!(attachedCamera is GameCamera25D)))
			{
				if (mainCameraMode == MainCameraMode.NormalSnap)
				{
					currentFrameCameraData = new GameCameraData (attachedCamera);
				}
				else if (mainCameraMode == MainCameraMode.NormalTransition)
				{
					UpdateCameraTransition ();
				}

				if (!timelineOverride)
				{
					ApplyCameraData (currentFrameCameraData);
				}
			}
			
			else if (attachedCamera && (attachedCamera is GameCamera25D))
			{
				transform.position = attachedCamera.transform.position;
				transform.rotation = attachedCamera.transform.rotation;

				perspectiveOffset = attachedCamera.GetPerspectiveOffset ();
				if (AllowProjectionShifting (Camera))
				{
					Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, perspectiveOffset);
				}

				currentFrameCameraData = new GameCameraData (this);
			}

			// Shake
			if (KickStarter.stateHandler.gameState != GameState.Paused)
			{
				if (shakeIntensity > 0f)
				{
					if (shakeEffect != CameraShakeEffect.Rotate)
					{
						shakePosition = Random.insideUnitSphere * shakeIntensity * 0.5f;
					}

					if (shakeEffect != CameraShakeEffect.Translate)
					{
						shakeRotation = new Vector3
						(
							Random.Range (-shakeIntensity, shakeIntensity) * 0.2f,
							Random.Range (-shakeIntensity, shakeIntensity) * 0.2f,
							Random.Range (-shakeIntensity, shakeIntensity) * 0.2f
						);
					}
					
					shakeIntensity = Mathf.Lerp (shakeStartIntensity, 0f, AdvGame.Interpolate (shakeStartTime, shakeDuration, MoveMethod.Linear, null));
					
					transform.position += shakePosition;
					transform.localEulerAngles += shakeRotation;
				}
				else if (shakeIntensity < 0f)
				{
					StopShaking ();
				}
			}
		}


		private void UpdateCameraTransition ()
		{
			if (transitionTimer > 0f)
			{
				transitionTimer -= Time.deltaTime;

				if (transitionTimer <= 0f)
				{
					ResetMoving ();
					return;
				}

				float transitionProgress = 1f - (transitionTimer / transitionDuration);
				
				if (retainPreviousSpeed && previousAttachedCamera != null)
				{
					oldCameraData = new GameCameraData (previousAttachedCamera);
				}
				
				GameCameraData attachedCameraData = new GameCameraData (attachedCamera);
				float timeValue = AdvGame.Interpolate (transitionProgress, moveMethod, timeCurve);
				currentFrameCameraData = oldCameraData.CreateMix (attachedCameraData, timeValue, (moveMethod == MoveMethod.Curved));
			}
		}


		/**
		 * Snaps the Camera to the attachedCamera instantly.
		 */
		public void SnapToAttached ()
		{
			if (attachedCamera && attachedCamera.Camera)
			{
				ResetMoving ();
				transitionFromCamera = null;

				bool changedOrientation = (transform.rotation != attachedCamera.transform.rotation);

				currentFrameCameraData = new GameCameraData (attachedCamera);
				ApplyCameraData (currentFrameCameraData);

				if (changedOrientation && !SceneSettings.IsUnity2D () && KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.settingsManager.directMovementType == DirectMovementType.RelativeToCamera && /*KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen &&*/ KickStarter.playerInput != null)
				{
					if (KickStarter.player != null && 
						(KickStarter.player.GetPath () == null || !KickStarter.player.IsLockedToPath ()))
					{
						KickStarter.playerInput.cameraLockSnap = true;
					}
				}
			}
		}
		

		/**
		 * <summary>Crossfades to a new _Camera over time.</summary>
		 * <param name = "_transitionDuration">The duration, in seconds, of the crossfade</param>
		 * <param name = "_linkedCamera">The _Camera to crossfade to</param>
		 */
		public void Crossfade (float _transitionDuration, _Camera _linkedCamera)
		{
			object[] parms = new object[2] { _transitionDuration, _linkedCamera};
			StartCoroutine ("StartCrossfade", parms);
		}
		

		/**
		 * Instantly ends the crossfade effect.
		 */
		public void StopCrossfade ()
		{
			StopCoroutine ("StartCrossfade");
			if (isCrossfading)
			{
				isCrossfading = false;
				alpha = 0f;
			}

			#if UNITY_2018_1_OR_NEWER
			Destroy (crossfadeTexture);
			#else
			DestroyObject (crossfadeTexture);
			#endif
			crossfadeTexture = null;
		}
		
		
		private IEnumerator StartCrossfade (object[] parms)
		{
			float _transitionDuration = (float) parms[0];
			_Camera _linkedCamera = (_Camera) parms[1];
			
			yield return new WaitForEndOfFrame ();

			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				crossfadeTexture = new Texture2D (Screen.width, Screen.height, TextureFormat.RGB24, false, true);
			}
			else
			{
				crossfadeTexture = new Texture2D (Screen.width, Screen.height, TextureFormat.RGB24, false);
			}
			crossfadeTexture.ReadPixels (new Rect (0f, 0f, Screen.width, Screen.height), 0, 0, false);
			crossfadeTexture.Apply ();
			
			ResetMoving ();
			isCrossfading = true;
			SetGameCamera (_linkedCamera);
			FadeOut (0f);
			FadeIn (_transitionDuration);
		}
		

		/**
		 * Places a full-screen texture of the current game window over the screen, allowing for a scene change to have no visible transition.
		 */
		public void _ExitSceneWithOverlay ()
		{
			StartCoroutine ("ExitSceneWithOverlay");
		}
		
		
		private IEnumerator ExitSceneWithOverlay ()
		{
			yield return new WaitForEndOfFrame ();
			Texture2D screenTex = new Texture2D (Screen.width, Screen.height, TextureFormat.RGB24, false);
			screenTex.ReadPixels (new Rect (0f, 0f, Screen.width, Screen.height), 0, 0, false);
			screenTex.Apply ();
			SetFadeTexture (screenTex);
			KickStarter.sceneChanger.SetTransitionTexture (screenTex);
			FadeOut (0f);
		}
		
		
		private void SmoothChange (float _transitionDuration, MoveMethod method, AnimationCurve _timeCurve = null)
		{
			moveMethod = method;
			mainCameraMode = MainCameraMode.NormalTransition;
			StopCrossfade ();
			
			transitionTimer = transitionDuration = _transitionDuration;

			if (method == MoveMethod.CustomCurve)
			{
				timeCurve = _timeCurve;
			}
			else
			{
				timeCurve = null;
			}
		}


		/**
		 * <summary>Gets the _Camera being transitioned from, if the MainCamera is transitioning between two _Cameras.</summary>
		 * <returns>The _Camera being transitioned from, if the MainCamera is transitioning between two _Cameras.</returns>
		 */
		public _Camera GetTransitionFromCamera ()
		{
			if (mainCameraMode == MainCameraMode.NormalTransition)
			{
				return transitionFromCamera;
			}
			return null;
		}
		

		/**
		 * <summary>Sets a _Camera as the new attachedCamera to follow.</summary>
		 * <param name = "newCamera">The new _Camera to follow</param>
		 * <param name = "transitionTime">The time, in seconds, that it will take to move towards the new _Camera</param>
		 * <param name = "_moveMethod">How the Camera should move towards the new _Camera, if transitionTime > 0f (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_animationCurve">The AnimationCurve that dictates movement over time, if _moveMethod = MoveMethod.CustomCurve</param>
		 * <param name = "_retainPreviousSpeed">If True, and transitionTime > 0, then the previous _Camera's speed will influence the transition, allowing for a smoother effect</param>
		 */
		public void SetGameCamera (_Camera newCamera, float transitionTime = 0f, MoveMethod _moveMethod = MoveMethod.Linear, AnimationCurve _animationCurve = null, bool _retainPreviousSpeed = false)
		{
			if (newCamera == null)
			{
				return;
			}

			if (KickStarter.eventManager != null) KickStarter.eventManager.Call_OnSwitchCamera (attachedCamera, newCamera, transitionTime);
			
			if (attachedCamera != null && attachedCamera is GameCamera25D)
			{
				transitionTime = 0f;

				if (newCamera is GameCamera25D)
				{ }
				else
				{
					RemoveBackground ();
				}
			}

			previousAttachedCamera = attachedCamera;

			oldCameraData = currentFrameCameraData;
			if (oldCameraData == null)
			{
				oldCameraData = new GameCameraData (this);
			}

			retainPreviousSpeed = (mainCameraMode == MainCameraMode.NormalSnap) ? _retainPreviousSpeed : false;

			Camera.ResetProjectionMatrix ();

			if (newCamera != attachedCamera && transitionTime > 0f)
			{
				transitionFromCamera = attachedCamera;
			}
			else
			{
				transitionFromCamera = null;
			}

			attachedCamera = newCamera;

			if (attachedCamera && attachedCamera.Camera)
			{
				Camera.farClipPlane = attachedCamera.Camera.farClipPlane;
				Camera.nearClipPlane = attachedCamera.Camera.nearClipPlane;
			}
			
			// Set background
			if (attachedCamera is GameCamera25D)
			{
				GameCamera25D cam25D = (GameCamera25D) attachedCamera;
				cam25D.SetActiveBackground ();
			}
			
			// TransparencySortMode
			if (attachedCamera is GameCamera2D)
			{
				Camera.transparencySortMode = TransparencySortMode.Orthographic;
			}
			else if (attachedCamera)
			{
				if (attachedCamera.Camera.orthographic)
				{
					Camera.transparencySortMode = TransparencySortMode.Orthographic;
				}
				else
				{
					Camera.transparencySortMode = TransparencySortMode.Perspective;
				}
			}

			KickStarter.stateHandler.LimitHotspotsToCamera (attachedCamera);
			
			if (transitionTime > 0f)
			{
				SmoothChange (transitionTime, _moveMethod, _animationCurve);
			}
			else if (attachedCamera != null)
			{
				attachedCamera.MoveCameraInstant ();
				SnapToAttached ();
			}
		}
		

		private void SetFadeTexture (Texture2D tex)
		{
			if (tex != null)
			{
				tempFadeTexture = tex;
			}
			AssignFadeTexture ();
		}
		

		private void ReleaseFadeTexture ()
		{
			tempFadeTexture = null;
			AssignFadeTexture ();
		}
		
		
		private void AssignFadeTexture ()
		{
			if (tempFadeTexture != null)
			{
				actualFadeTexture = tempFadeTexture;
			}
			else
			{
				actualFadeTexture = fadeTexture;
			}
		}
		

		/**
		 * <summary>Fades the camera out with a custom texture.</summary>
		 * <param name = "_fadeDuration">The duration, in seconds, of the fade effect</param>
		 * <param name = "tempTex">The texture to display full-screen</param>
		 * <param name = "forceCompleteTransition">If True, the camera will be faded in instantly before beginning</param>
		 */
		public void FadeOut (float _fadeDuration, Texture2D tempTex, bool forceCompleteTransition = true)
		{
			if (tempTex != null)
			{
				SetFadeTexture (tempTex);
			}
			FadeOut (_fadeDuration, forceCompleteTransition);
		}
		

		/**
		 * <summary>Fades the camera in.</summary>
		 * <param name = "_fadeDuration">The duration, in seconds, of the fade effect</param>
		 * <param name = "forceCompleteTransition">If True, the camera will be faded out instantly before beginning</param>
		 */
		public void FadeIn (float _fadeDuration, bool forceCompleteTransition = true)
		{
			AssignFadeTexture ();

			if ((forceCompleteTransition || alpha > 0f) && _fadeDuration > 0f)
			{
				fadeDuration = _fadeDuration;

				if (forceCompleteTransition)
				{
					alpha = 1f;
					fadeTimer = _fadeDuration;
				}
				else 
				{
					fadeTimer = _fadeDuration * alpha;
				}

				fadeType = FadeType.fadeIn;
			}
			else
			{
				alpha = 0f;
				fadeTimer = fadeDuration = 0f;
				ReleaseFadeTexture ();
			}
		}
		

		/**
		 * <summary>Fades the camera out.</summary>
		 * <param name = "_fadeDuration">The duration, in seconds, of the fade effect</param>
		 * <param name = "forceCompleteTransition">If True, the camera will be faded in instantly before beginning</param>
		 */
		public void FadeOut (float _fadeDuration, bool forceCompleteTransition = true)
		{
			AssignFadeTexture ();
			
			if (alpha <= 0f)
			{
				alpha = 0.01f;
			}
			if ((forceCompleteTransition || alpha < 1f) && _fadeDuration > 0f)
			{
				if (forceCompleteTransition)
				{
					alpha = 0.01f;
					fadeTimer = _fadeDuration;
				}
				else
				{
					alpha = Mathf.Clamp01 (alpha);
					fadeTimer = _fadeDuration * (1f - alpha);
				}
				fadeDuration = _fadeDuration;
				fadeType = FadeType.fadeOut;
			}
			else
			{
				alpha = 1f;
				fadeTimer = fadeDuration = 0f;
			}
		}
		

		/**
		 * <summary>Checks if the Camera is fading in our out.</summary>
		 * <returns>True if the Camera is fading in or out</returns>
		 */
		public bool isFading ()
		{
			if (fadeType == FadeType.fadeOut && alpha < 1f)
			{
				return true;
			}
			else if (fadeType == FadeType.fadeIn && alpha > 0f)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Converts a point in world space to one relative to the Camera's forward vector.</summary>
		 * <returns>Converts a point in world space to one relative to the Camera's forward vector.</returns>
		 */
		public Vector3 PositionRelativeToCamera (Vector3 _position)
		{
			return (_position.x * ForwardVector ()) + (_position.z * RightVector ());
		}
		

		/**
		 * <summary>Gets the Camera's right vector.</summary>
		 * <returns>The Camera's right vector</returns>
		 */
		public Vector3 RightVector ()
		{
			return (transform.right);
		}
		

		/**
		 * <summary>Gets the Camera's forward vector, not accounting for pitch.</summary>
		 * <returns>The Camera's forward vector, not accounting for pitch</returns>
		 */
		public Vector3 ForwardVector ()
		{
			Vector3 camForward = transform.forward;
			camForward.y = 0;
			
			return (camForward);
		}


		private bool SetAspectRatio ()
		{
			float currentAspectRatio = 0f;
			Vector2 screenSize = AdvGame.GetMainGameViewSize (false);

			if (Screen.orientation == ScreenOrientation.LandscapeRight || Screen.orientation == ScreenOrientation.LandscapeLeft)
			{
				currentAspectRatio = screenSize.x / screenSize.y;
			}
			else
			{
				if (Screen.height > Screen.width && KickStarter.settingsManager.landscapeModeOnly)
				{
					currentAspectRatio = screenSize.y / screenSize.x;
				}
				else
				{
					currentAspectRatio = screenSize.x / screenSize.y;
				}
			}

			// If the current aspect ratio is already approximately equal to the desired aspect ratio, use a full-screen Rect (in case it was set to something else previously)
			if (!KickStarter.settingsManager.forceAspectRatio || Mathf.Approximately (currentAspectRatio, KickStarter.settingsManager.wantedAspectRatio))
			{
				borderWidth = 0f;
				borderOrientation = MenuOrientation.Horizontal;
				
				if (borderCam) 
				{
					Destroy (borderCam.gameObject);
				}
				return false;
			}
			
			if (currentAspectRatio > KickStarter.settingsManager.wantedAspectRatio)
			{
				// Pillarbox
				borderWidth = 1f - KickStarter.settingsManager.wantedAspectRatio / currentAspectRatio;
				borderWidth /= 2f;
				borderOrientation = MenuOrientation.Vertical;
				
				borderRect1 = new Rect (0, 0, borderWidth * Screen.width, Screen.height);
				borderRect2 = new Rect (Screen.width * (1f - borderWidth), 0f, Screen.width * borderWidth, Screen.height);
			}
			else
			{
				// Letterbox
				borderWidth = 1f - currentAspectRatio / KickStarter.settingsManager.wantedAspectRatio;
				borderWidth /= 2f;
				borderOrientation = MenuOrientation.Horizontal;
				
				borderRect1 = new Rect (0, 0, Screen.width, borderWidth * Screen.height);
				borderRect2 = new Rect (0, Screen.height * (1f - borderWidth), Screen.width, Screen.height * borderWidth);
			}
			
			
			return true;
		}
		

		/**
		 * Updates the camera's rect values according to the aspect ratio and split-screen settings.
		 */
		public void SetCameraRect ()
		{
			if (SetAspectRatio () && Application.isPlaying)
			{
				CreateBorderCamera ();
			}
			
			if (isSplitScreen)
			{
				Camera.rect = GetSplitScreenRect (true);
			}
			else
			{
				if (borderOrientation == MenuOrientation.Vertical)
				{
					Camera.rect = new Rect (borderWidth, 0f, 1f - (2*borderWidth), 1f);
				}
				else if (borderOrientation == MenuOrientation.Horizontal)
				{
					Camera.rect = new Rect (0f, borderWidth, 1f, 1f - (2*borderWidth));
				}
			}
			
			if (KickStarter.stateHandler)
			{
				foreach (BackgroundCamera backgroundCamera in KickStarter.stateHandler.BackgroundCameras)
				{
					backgroundCamera.UpdateRect ();
				}
			}

			CalculateUnityUIAspectRatioCorrection ();
		}


		private void CalculateUnityUIAspectRatioCorrection ()
		{
			Vector2 screenSize = AdvGame.GetMainGameViewSize (true);
			Vector2 windowSize = AdvGame.GetMainGameViewSize (false);

			aspectRatioScaleCorrection = new Vector2 (screenSize.x / windowSize.x, screenSize.y / windowSize.y);
			aspectRatioOffsetCorrection = new Vector2 ((windowSize.x - screenSize.x) / 2f, (windowSize.y - screenSize.y) / 2f);
		}


		/**
		 * <summary>Corrects a screen position Vector to account for the MainCamera's viewport Rect. This is necessary when positioning Unity UI RectTransforms while an aspect ratio is enforced, because the original screen position assumes a default Rect.</summary>
		 * <param name = "screenPosition">The screen position to correct.</param>
		 * <returns>The corrected screen position</returns>
		 */
		public Vector2 CorrectScreenPositionForUnityUI (Vector2 screenPosition)
		{
			if (aspectRatioScaleCorrection == Vector2.zero)
			{
				CalculateUnityUIAspectRatioCorrection ();
			}
			return new Vector2 ((screenPosition.x * aspectRatioScaleCorrection.x) + aspectRatioOffsetCorrection.x, (screenPosition.y * aspectRatioScaleCorrection.y) + aspectRatioOffsetCorrection.y);
		}


		/**
		 * <summary>Gets the difference between the window size and the game's viewport.</summary>
		 * <returns>The difference between the window size and the game's viewport.</returns>
		 */
		public Vector2 GetWindowViewportDifference ()
		{
			if (aspectRatioScaleCorrection == Vector2.zero)
			{
				CalculateUnityUIAspectRatioCorrection ();
			}

			return aspectRatioOffsetCorrection;
		}


		/**
		 * Draws any borders generated by a fixed aspect ratio, as set with forceAspectRatio in SettingsManager.
		 * This will be called every OnGUI call by StateHandler.
		 */
		public void DrawBorders ()
		{
			if (!Application.isPlaying)
			{
				if (AdvGame.GetReferences () == null || AdvGame.GetReferences ().settingsManager == null || !AdvGame.GetReferences ().settingsManager.forceAspectRatio)
				{
					return;
				}
				SetAspectRatio ();
			}

			if (borderWidth > 0f)
			{
				if (fadeTexture == null)
				{
					ACDebug.LogWarning ("Cannot draw camera borders because no Fade texture is assigned in the MainCamera!");
					return;
				}

				GUI.depth = 10;
				GUI.DrawTexture (borderRect1, fadeTexture);
				GUI.DrawTexture (borderRect2, fadeTexture);
			}
			else if (isSplitScreen)
			{
				if (fadeTexture == null)
				{
					ACDebug.LogWarning ("Cannot draw camera borders because no Fade texture is assigned in the MainCamera!", gameObject);
					return;
				}

				GUI.depth = 10;
				GUI.DrawTexture (midBorderRect, fadeTexture);
			}
		}
		

		/**
		 * <summary>Checks if the Camera uses orthographic perspective.</summary>
		 * <returns>True if the Camera uses orthographic perspective</returns>
		 */
		public bool IsOrthographic ()
		{
			if (Camera == null)
			{
				return false;
			}
			return Camera.orthographic;
		}
		
		
		private void CreateBorderCamera ()
		{
			if (!borderCam)
			{
				// Make a new camera behind the normal camera which displays black; otherwise the unused space is undefined
				borderCam = new GameObject ("BorderCamera", typeof (Camera)).GetComponent <Camera>();
				borderCam.transform.parent = this.transform;
				borderCam.depth = int.MinValue;
				borderCam.clearFlags = CameraClearFlags.SolidColor;
				borderCam.backgroundColor = Color.black;
				borderCam.cullingMask = 0;
			}
		}
		

		/**
		 * <summary>Limits a point in screen-space to stay within the Camera's rect boundary, if forceAspectRatio in SettingsManager = True.</summary>
		 * <param name = "position">The original position in screen-space</param>
		 * <returns>The point, repositioned to stay within the Camera's rect boundary</returns>
		 */
		public Vector2 LimitToAspect (Vector2 position)
		{
			if (!KickStarter.cursorManager.keepCursorWithinScreen)
			{
				return position;
			}

			if (!KickStarter.settingsManager.forceAspectRatio)
			{
				return LimitVector (position, 0f, 0f);
			}

			if (borderOrientation == MenuOrientation.Horizontal)
			{
				return LimitVector (position, 0f, borderWidth);
			}

			if (borderOrientation == MenuOrientation.Vertical)
			{
				return LimitVector (position, borderWidth, 0f);
			}
			
			return position;
		}


		private Vector2 LimitVector (Vector2 point, float xBorder, float yBorder)
		{
			// Pillarbox
			int xOffset = (int) (Screen.width * xBorder);
			
			if (point.x <= xOffset)
			{
				point.x = xOffset + 1;
			}
			else if (point.x >= (Screen.width - xOffset))
			{
				point.x = Screen.width - xOffset - 1;
			}

			// Letterbox
			int yOffset = (int) (Screen.height * yBorder);
			
			if (point.y <= yOffset)
			{
				point.y = yOffset + 1;
			}
			else if (point.y >= (Screen.height - yOffset))
			{
				point.y = Screen.height - yOffset - 1;
			}

			return point;
		}
		

		/**
		 * <summary>Checks if a point in screen-space is within the Camera's viewport</summary>
		 * <param name = "point">The point to check the position of</param>
		 * <returns>True if the point is within the Camera's viewport</returns>
		 */
		public bool IsPointInCamera (Vector2 point)
		{
			if (!isSplitScreen)
			{
				return true;
			}
			point = new Vector2 (point.x / Screen.width, point.y / Screen.height);
			return Camera.rect.Contains (point);
		}
		

		/**
		 * <summary>Resizes an OnGUI Rect so that it fits within the Camera's rect, if forceAspectRatio = True in SettingsManager.</summary>
		 * <param name = "rect">The OnGUI Rect to resize</param>
		 * <returns>The resized OnGUI Rect</returns>
		 */
		public Rect LimitMenuToAspect (Rect rect)
		{
			if (KickStarter.settingsManager == null || !KickStarter.settingsManager.forceAspectRatio)
			{
				return rect;
			}
			
			if (borderOrientation == MenuOrientation.Horizontal)
			{
				// Letterbox
				int yOffset = (int) (Screen.height * borderWidth);
				
				if (rect.y < yOffset)
				{
					rect.y = yOffset;
					
					if (rect.height > (Screen.height - yOffset - yOffset))
					{
						rect.height = Screen.height - yOffset - yOffset;
					}
				}
				else if (rect.y + rect.height > (Screen.height - yOffset))
				{
					rect.y = Screen.height - yOffset - rect.height;
				}
			}
			else
			{
				// Pillarbox
				int xOffset = (int) (Screen.width * borderWidth);
				
				if (rect.x < xOffset)
				{
					rect.x = xOffset;
					
					if (rect.width > (Screen.width - xOffset - xOffset))
					{
						rect.width = Screen.width - xOffset - xOffset;
					}
				}
				else if (rect.x + rect.width > (Screen.width - xOffset))
				{
					rect.x = Screen.width - xOffset - rect.width;
				}
			}
			
			return rect;
		}
		

		/**
		 * <summary>Creates a new split-screen effect.</summary>
		 * <param name = "_camera1">The first _Camera to use in the effect</param>
		 * <param name = "_camera2">The second _Camera to use in the effect</param>
		 * <param name = "_splitOrientation">How the two _Cameras are arranged (Horizontal, Vertical)</param>
		 * <param name = "_isTopLeft">If True, the MainCamera will take the position of _camera1</param>
		 * <param name = "_splitAmountMain">The proportion of the screen taken up by this Camera</param>
		 * <param name = "_splitAmountOther">The proportion of the screen take up by the other _Camera</param>
		 */
		public void SetSplitScreen (_Camera _camera1, _Camera _camera2, MenuOrientation _splitOrientation, bool _isTopLeft, float _splitAmountMain, float _splitAmountOther)
		{
			splitCamera = _camera2;
			isSplitScreen = true;
			splitOrientation = _splitOrientation;
			isTopLeftSplit = _isTopLeft;
			
			SetGameCamera (_camera1);
			StartSplitScreen (_splitAmountMain, _splitAmountOther);
		}
		

		/**
		 * <summary>Adjusts the screen ratio of any active split-screen effect.</summary>
		 * <param name = "_splitAmountMain">The proportion of the screen taken up by this Camera</param>
		 * <param name = "_splitAmountOther">The proportion of the screen take up by the other _Camera</param>
		 */
		public void StartSplitScreen (float _splitAmountMain, float _splitAmountOther)
		{
			splitAmountMain = _splitAmountMain;
			splitAmountOther = _splitAmountOther;
			
			splitCamera.SetSplitScreen ();
			SetCameraRect ();
			SetMidBorder ();
		}


		/**
		 * Ends any active split-screen effect.
		 */
		public void RemoveSplitScreen ()
		{
			isSplitScreen = false;
			SetCameraRect ();
			
			if (splitCamera)
			{
				splitCamera.RemoveSplitScreen ();
				splitCamera = null;
			}
		}


		/**
		 * <summary>Gets a screen Rect of the split-screen camera.</summary>
		 * <param name = "isMainCamera">If True, then the Rect of the MainCamera's view will be returned. Otherwise, the Rect of the other split-screen _Camera's view will be returned</param>
		 * <returns>A screen Rect of the split-screen camera</returns>
		 */
		public Rect GetSplitScreenRect (bool isMainCamera)
		{
			bool _isTopLeftSplit = (isMainCamera) ? isTopLeftSplit : !isTopLeftSplit;
			float split = (isMainCamera) ? splitAmountMain : splitAmountOther;
			Rect splitScreenRect;

			if (borderOrientation == MenuOrientation.Vertical)
			{
				// Pillarbox
				if (splitOrientation == MenuOrientation.Horizontal)
				{
					if (!_isTopLeftSplit)
					{
						splitScreenRect = new Rect (borderWidth, 0f, 1f - (2*borderWidth), split);
					}
					else
					{
						splitScreenRect = new Rect (borderWidth, 1f - split, 1f - (2*borderWidth), split);
					}
				}
				else
				{
					if (_isTopLeftSplit)
					{
						splitScreenRect = new Rect (borderWidth, 0f, split - borderWidth, 1f);
					}
					else
					{
						splitScreenRect = new Rect (1f - split, 0f, split - borderWidth, 1f);
					}
				}
			}
			else
			{
				// Letterbox
				if (splitOrientation == MenuOrientation.Horizontal)
				{
					if (_isTopLeftSplit)
					{
						splitScreenRect = new Rect (0f, 1f - split, 1f, split - borderWidth);
					}
					else
					{
						splitScreenRect = new Rect (0f, borderWidth, 1f, split - borderWidth);
					}
				}
				else
				{
					if (_isTopLeftSplit)
					{
						splitScreenRect = new Rect (0f, borderWidth, split, 1f - (2*borderWidth));
					}
					else
					{
						splitScreenRect = new Rect (1f - split, borderWidth, split, 1f - (2*borderWidth));
					}
				}
			}

			return splitScreenRect;
		}


		private void SetMidBorder ()
		{
			if (borderWidth <= 0f && (splitAmountMain + splitAmountOther) < 1f)
			{
				Vector2 screenSize = AdvGame.GetMainGameViewSize (false);

				if (splitOrientation == MenuOrientation.Horizontal)
				{
					if (isTopLeftSplit)
					{
						midBorderRect = new Rect (0f, screenSize.y * splitAmountMain, screenSize.x, screenSize.y * (1f - splitAmountOther - splitAmountMain));
					}
					else
					{
						midBorderRect = new Rect (0f, screenSize.y * splitAmountOther, screenSize.x, screenSize.y * (1f - splitAmountOther - splitAmountMain));
					}
				}
				else
				{
					if (isTopLeftSplit)
					{
						midBorderRect = new Rect (screenSize.x * splitAmountMain, 0f, screenSize.x * (1f - splitAmountOther - splitAmountMain), screenSize.y);
					}
					else
					{
						midBorderRect = new Rect (screenSize.x * splitAmountOther, 0f, screenSize.x * (1f - splitAmountOther - splitAmountMain), screenSize.y);
					}
				}
			}
			else
			{
				midBorderRect = new Rect (0f, 0f, 0f, 0f);
			}
		}


		/**
		 * <summary>Gets the current focal distance.</summary>
		 * <returns>The current focal distance</returns>
		 */
		public float GetFocalDistance ()
		{
			return focalDistance;
		}


		private void OnDestroy ()
		{
			crossfadeTexture = null;
		}
		

		/**
		 * Disables the Camera and AudioListener.
		 */
		public void Disable ()
		{
			if (Camera)
			{
				Camera.enabled = false;
			}
			if (AudioListener)
			{
				AudioListener.enabled = false;
			}
		}
		

		/**
		 * Enables the Camera and AudioListener.
		 */
		public void Enable ()
		{
			if (Camera)
			{
				Camera.enabled = true;
			}
			if (AudioListener)
			{
				AudioListener.enabled = true;
			}
		}


		/**
		 * <summary>Checks if the Camera is enabled.</summary>
		 * returns>True if the Camera is enabled</returns>
		 */
		public bool IsEnabled ()
		{
			if (Camera)
			{
				return Camera.enabled;
			}
			return false;
		}
		

		/**
		 * <summary>Sets the GameObject's tag.</summary>
		 * <param name = "_tag">The tag to give the GameObject</param>
		 */
		public void SetCameraTag (string _tag)
		{
			if (Camera != null)
			{
				Camera.gameObject.tag = _tag;
			}
		}
		

		/**
		 * <summary>Sets the state of the AudioListener component.</summary>
		 * <param name = "state">If True, the AudioListener will be enabled. If False, it will be disabled.</param>
		 */
		public void SetAudioState (bool state)
		{
			if (AudioListener)
			{
				AudioListener.enabled = state;
			}
		}


		/**
		 * <summary>Gets the previously-used gameplay _Camera.</summary>
		 * <returns>The previously-used gameplay _Camera</returns>
		 */
		public _Camera GetLastGameplayCamera ()
		{
			if (lastNavCamera != null)
			{
				if (lastNavCamera2 != null && attachedCamera == lastNavCamera)
				{
					return (_Camera) lastNavCamera2;
				}
				else
				{
					return (_Camera) lastNavCamera;
				}
			}
			ACDebug.LogWarning ("Could not get the last gameplay camera - was it previously set?");
			return null;
		}


		/**
		 * <summary>Gets the current perspective offset, as set by a GameCamera2D.</summary>
		 * <returns>The current perspective offset, as set by a GameCamera2D.</returns>
		 */
		public Vector2 GetPerspectiveOffset ()
		{
			return perspectiveOffset;
		}


		/**
		 * <summary>Saves data related to the camera.</summary>
		 * <param name = "playerData">The PlayerData class to update with current data</param>
		 * <returns>A PlayerData class, updated with current camera data</returns>
		 */
		public PlayerData SaveData (PlayerData playerData)
		{
			SnapToAttached ();
			if (attachedCamera)
			{
				playerData.gameCamera = Serializer.GetConstantID (attachedCamera.gameObject);
				if (KickStarter.sceneChanger.GetSubSceneIndexOfGameObject (attachedCamera.gameObject) > 0)
				{
					ACDebug.LogWarning ("Cannot save the active camera '" + attachedCamera.gameObject.name + "' as it is not in the active scene.", attachedCamera.gameObject);
				}
			}
			if (lastNavCamera)
			{
				playerData.lastNavCamera = Serializer.GetConstantID (lastNavCamera.gameObject);
			}
			if (lastNavCamera2)
			{
				playerData.lastNavCamera2 = Serializer.GetConstantID (lastNavCamera2.gameObject);
			}

			if (shakeIntensity > 0f)
			{
				playerData.shakeIntensity = shakeIntensity;
				playerData.shakeDuration = shakeDuration;
				playerData.shakeEffect = (int) shakeEffect;
			}
			else
			{
				playerData.shakeIntensity = 0f;
				playerData.shakeDuration = 0f;
				playerData.shakeEffect = 0;
				StopShaking ();
			}

			playerData.mainCameraLocX = transform.position.x;
			playerData.mainCameraLocY = transform.position.y;
			playerData.mainCameraLocZ = transform.position.z;
			
			playerData.mainCameraRotX = transform.eulerAngles.x;
			playerData.mainCameraRotY = transform.eulerAngles.y;
			playerData.mainCameraRotZ = transform.eulerAngles.z;

			playerData.isSplitScreen = isSplitScreen;
			if (isSplitScreen)
			{
				playerData.isTopLeftSplit = isTopLeftSplit;
				playerData.splitAmountMain = splitAmountMain;
				playerData.splitAmountOther = splitAmountOther;
				
				if (splitOrientation == MenuOrientation.Vertical)
				{
					playerData.splitIsVertical = true;
				}
				else
				{
					playerData.splitIsVertical = false;
				}
				if (splitCamera && splitCamera.GetComponent <ConstantID>())
				{
					playerData.splitCameraID = splitCamera.GetComponent <ConstantID>().constantID;
				}
				else
				{
					playerData.splitCameraID = 0;
				}
			}

			return playerData;
		}


		/**
		 * <summary>Restores the camera state from saved data</summary>
		 * <param name = "playerData">The data class to load from</param>
		 */
		public void LoadData (PlayerData playerData)
		{
			if (isSplitScreen)
			{
				RemoveSplitScreen ();
			}

			StopShaking ();
			if (playerData.shakeIntensity > 0f && playerData.shakeDuration > 0f)
			{
				Shake (playerData.shakeIntensity, playerData.shakeDuration, (CameraShakeEffect) playerData.shakeEffect);
			}

			_Camera _attachedCamera = Serializer.returnComponent <_Camera> (playerData.gameCamera);
			if (_attachedCamera != null)
			{
				_attachedCamera.MoveCameraInstant ();
				SetGameCamera (_attachedCamera);
			}
			else if (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson && KickStarter.settingsManager.IsInFirstPerson ())
			{
				SetFirstPerson ();
			}

			lastNavCamera = Serializer.returnComponent <_Camera> (playerData.lastNavCamera);
			lastNavCamera2 = Serializer.returnComponent <_Camera> (playerData.lastNavCamera2);
			ResetMoving ();

			#if ALLOW_VR
				#if UNITY_2017_2_OR_NEWER
				if (!UnityEngine.XR.XRSettings.enabled || restoreTransformOnLoadVR) {
				#else
				if (!VRSettings.enabled || restoreTransformOnLoadVR) {
				#endif
			#endif
				transform.position = new Vector3 (playerData.mainCameraLocX, playerData.mainCameraLocY, playerData.mainCameraLocZ);
				transform.eulerAngles = new Vector3 (playerData.mainCameraRotX, playerData.mainCameraRotY, playerData.mainCameraRotZ);
				ResetProjection ();
			#if ALLOW_VR
			}
			#endif

			SnapToAttached ();

			isSplitScreen = playerData.isSplitScreen;
			if (isSplitScreen)
			{
				isTopLeftSplit = playerData.isTopLeftSplit;
				if (playerData.splitIsVertical)
				{
					splitOrientation = MenuOrientation.Vertical;
				}
				else
				{
					splitOrientation = MenuOrientation.Horizontal;
				}
				if (playerData.splitCameraID != 0)
				{
					_Camera _splitCamera = Serializer.returnComponent <_Camera> (playerData.splitCameraID);
					if (_splitCamera)
					{
						splitCamera = _splitCamera;
					}
				}
				StartSplitScreen (playerData.splitAmountMain, playerData.splitAmountOther);
			}
		}


		/**
		 * The MainCamera's Camera component.
		 */
		public Camera Camera
		{
			get
			{
				if (ownCamera == null)
				{
					ownCamera = GetComponent <Camera>();

					if (ownCamera == null)
					{
						ownCamera = GetComponentInChildren <Camera>();

						if (ownCamera == null)
						{
							ACDebug.LogError ("The MainCamera script requires a Camera component.", gameObject);
						}
					}
				}
				return ownCamera;
			}
		}


		private AudioListener AudioListener
		{
			get
			{
				if (_audioListener == null)
				{
					_audioListener = GetComponent <AudioListener>();

					if (_audioListener == null && Camera != null)
					{
						_audioListener = Camera.GetComponent <AudioListener>();
					}

					if (_audioListener == null)
					{
						ACDebug.LogWarning ("No AudioListener found on the MainCamera!", gameObject);
					}
				}
				return _audioListener;
			}
		}


		public static bool AllowProjectionShifting (Camera _camera)
		{
			#if ALLOW_PHYSICAL_CAMERA
			return false;
			#else
			return (!_camera.orthographic);
			#endif
		}


		/**
		 * Displays information about the MainCamera section of the 'AC Status' box.
		 */
		public void DrawStatus ()
		{
			if (IsEnabled ())
			{
				if (timelineOverride)
				{
					GUILayout.Label ("Current camera: Set by Timeline");
				}
				else if (attachedCamera != null)
				{
					if (GUILayout.Button ("Current camera: " + attachedCamera.gameObject.name))
					{
						#if UNITY_EDITOR
						UnityEditor.EditorGUIUtility.PingObject (attachedCamera.gameObject);
						#endif
					}
				}
			}
			else
			{
				GUILayout.Label ("MainCamera: Disabled");
			}
		}


		/* Timeline */

		private bool timelineOverride;

		public void SetTimelineOverride (_Camera cam1, _Camera cam2, float cam2Weight)
		{
			#if UNITY_EDITOR
			if ((!timelineOverride && !Application.isPlaying) || currentFrameCameraData == null)
			#else
			if (currentFrameCameraData == null)
			#endif
			{
				currentFrameCameraData = new GameCameraData (this);
			}

			timelineOverride = true;

			if (cam1 == null)
			{
				if (cam2 == null)
				{
					ReleaseTimelineOverride ();
					return;
				}

				// Blending in/out of a clip with no mix
				GameCameraData cameraData2 = new GameCameraData (cam2);
				ApplyCameraData (currentFrameCameraData, cameraData2, cam2Weight);
			}
			else
			{
				ApplyCameraData (cam1, cam2, cam2Weight);
			}
		}


		public void ReleaseTimelineOverride ()
		{
			timelineOverride = false;

			#if UNITY_EDITOR
			if (!Application.isPlaying && currentFrameCameraData != null)
			{
				ApplyCameraData (currentFrameCameraData);
			}
			#endif
		}


		private void ApplyCameraData (_Camera _camera1, _Camera _camera2, float camera2Weight, MoveMethod _moveMethod = MoveMethod.Linear, AnimationCurve _timeCurve = null)
		{
			if (_camera1 == null)
			{
				ApplyCameraData (new GameCameraData (_camera2));
				return;
			}

			if (_camera2 == null)
			{
				ApplyCameraData (new GameCameraData (_camera1));
				return;
			}

			GameCameraData cameraData1 = new GameCameraData (_camera1);
			GameCameraData cameraData2 = new GameCameraData (_camera2);

			ApplyCameraData (cameraData1, cameraData2, camera2Weight, _moveMethod, _timeCurve);
		}


		private void ApplyCameraData (GameCameraData cameraData1, GameCameraData cameraData2, float camera2Weight, MoveMethod _moveMethod = MoveMethod.Linear, AnimationCurve _timeCurve = null)
		{
			float timeValue = AdvGame.Interpolate (camera2Weight, _moveMethod, _timeCurve);

			GameCameraData mixedData = cameraData1.CreateMix (cameraData2, timeValue, _moveMethod == MoveMethod.Curved);
			ApplyCameraData (mixedData);
		}


		private void ApplyCameraData (GameCameraData cameraData)
		{
			if (cameraData.is2D)
			{
				perspectiveOffset = cameraData.perspectiveOffset;
				Camera.ResetProjectionMatrix ();
			}

			transform.position = cameraData.position;
			transform.rotation = cameraData.rotation;
			Camera.orthographic = cameraData.isOrthographic;
			Camera.fieldOfView = cameraData.fieldOfView;
			Camera.orthographicSize = cameraData.orthographicSize;
			focalDistance = cameraData.focalDistance;

			if (cameraData.is2D && !cameraData.isOrthographic)
			{
				Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, perspectiveOffset);
			}

			#if ALLOW_PHYSICAL_CAMERA
			Camera.usePhysicalProperties = cameraData.usePhysicalProperties;
			Camera.sensorSize = cameraData.sensorSize;
			Camera.lensShift = cameraData.lensShift;
			#endif
		}


		private class GameCameraData
		{

			public Vector3 position { get; private set; }
			public Quaternion rotation { get; private set; }
			public bool isOrthographic { get; private set; }
			public float fieldOfView { get; private set; }
			public float orthographicSize { get; private set; }
			public float focalDistance { get; private set; }

			public bool is2D { get; private set; }
			public Vector2 perspectiveOffset { get; private set; }

			#if ALLOW_PHYSICAL_CAMERA
			public bool usePhysicalProperties { get; private set; }
			public Vector2 sensorSize { get; private set; }
			public Vector2 lensShift { get; private set; }
			#endif

			public GameCameraData () {}


			public GameCameraData (MainCamera mainCamera)
			{
				position = mainCamera.transform.position;
				rotation = mainCamera.transform.rotation;
				fieldOfView = mainCamera.Camera.fieldOfView;
				isOrthographic = mainCamera.Camera.orthographic;
				orthographicSize = mainCamera.Camera.orthographicSize;
				focalDistance = mainCamera.GetFocalDistance ();

				is2D = false;
				perspectiveOffset = Vector2.zero;

				#if ALLOW_PHYSICAL_CAMERA
				usePhysicalProperties = mainCamera.Camera.usePhysicalProperties;
				sensorSize = mainCamera.Camera.sensorSize;
				lensShift = mainCamera.Camera.lensShift;
				#endif
			}


			public GameCameraData (_Camera _camera)
			{
				position = _camera.transform.position;
				rotation = _camera.transform.rotation;

				is2D = _camera.Is2D ();
				Vector2 cursorOffset = _camera.CreateRotationOffset ();

				if (is2D)
				{
					if (_camera.Camera.orthographic)
					{
						position += (Vector3) cursorOffset;
					}
				}
				else
				{
					rotation *= Quaternion.Euler (5f * new Vector3 (-cursorOffset.y, cursorOffset.x, 0f));
				}

				fieldOfView = _camera.Camera.fieldOfView;
				isOrthographic = _camera.Camera.orthographic;
				orthographicSize = _camera.Camera.orthographicSize;
				focalDistance = _camera.focalDistance;

				perspectiveOffset = (is2D)
									? _camera.GetPerspectiveOffset ()
									: Vector2.zero;

				if (is2D && !_camera.Camera.orthographic)
				{
					perspectiveOffset += cursorOffset;
				}

				#if ALLOW_PHYSICAL_CAMERA
				usePhysicalProperties = _camera.Camera.usePhysicalProperties;
				sensorSize = _camera.Camera.sensorSize;
				lensShift = _camera.Camera.lensShift;
				#endif
			}


			public GameCameraData CreateMix (GameCameraData otherData, float otherDataWeight, bool slerpRotation = false)
			{
				if (otherDataWeight <= 0f)
				{
					return this;
				}

				if (otherDataWeight >= 1f)
				{
					return otherData;
				}

				GameCameraData mixData = new GameCameraData ();

				mixData.is2D = otherData.is2D;

				if (mixData.is2D)
				{
					float offsetX = AdvGame.Lerp (perspectiveOffset.x, otherData.perspectiveOffset.x, otherDataWeight);
					float offsetY = AdvGame.Lerp (perspectiveOffset.y, otherData.perspectiveOffset.y, otherDataWeight);

					mixData.perspectiveOffset = new Vector2 (offsetX, offsetY);
				}

				mixData.position = Vector3.Lerp (position, otherData.position, otherDataWeight);
				mixData.rotation = (slerpRotation)
									? Quaternion.Lerp (rotation, otherData.rotation, otherDataWeight)
									: Quaternion.Slerp (rotation, otherData.rotation, otherDataWeight);

				mixData.isOrthographic = otherData.isOrthographic;
				mixData.fieldOfView = Mathf.Lerp (fieldOfView, otherData.fieldOfView, otherDataWeight);
				mixData.orthographicSize = Mathf.Lerp (orthographicSize, otherData.orthographicSize, otherDataWeight);
				mixData.focalDistance = Mathf.Lerp (focalDistance, otherData.focalDistance, otherDataWeight);

				#if ALLOW_PHYSICAL_CAMERA
				mixData.usePhysicalProperties = otherData.usePhysicalProperties;
				mixData.sensorSize = Vector2.Lerp (sensorSize, otherData.sensorSize, otherDataWeight);
				mixData.lensShift = Vector2.Lerp (lensShift, otherData.lensShift, otherDataWeight);
				#endif

				return mixData;
			}

		}
		
	}
	
}