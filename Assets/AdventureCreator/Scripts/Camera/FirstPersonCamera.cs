/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"FirstPersonCamera.cs"
 * 
 *	An optional script that allows First Person control.
 *	This is attached to a camera which is a child of the player.
 *	Only one First Person Camera should ever exist in the scene at runtime.
 *	Only the yaw is affected here: the pitch is determined by the player parent object.
 *
 *	Headbobbing code adapted from Mr. Animator's code: http://wiki.unity3d.com/index.php/Headbobber
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A camera used for first-person games. To use it, attach it to a child object of your Player prefab, as well as a Camera.
	 * It will then be used during gameplay if SettingsManager's movementMethod = MovementMethod.FirstPerson.
	 * This script only affects the pitch rotation - yaw rotation occurs by rotating the base object.
	 * Headbobbing code adapted from Mr. Animator's code: http://wiki.unity3d.com/index.php/Headbobber
	 */
	[AddComponentMenu("Adventure Creator/Camera/First-person camera")]
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_first_person_camera.html")]
	#endif
	public class FirstPersonCamera : _Camera
	{

		/** The sensitivity of free-aiming */
		public Vector2 sensitivity = new Vector2 (15f, 15f);

		/** The minimum pitch angle */
		public float minY = -60f;
		/** The maximum pitch angle */
		public float maxY = 60f;

		/** If True, the mousewheel can be used to zoom the camera's FOV */
		public bool allowMouseWheelZooming = false;
		/** The minimum FOV, if allowMouseWheelZooming = True */
		public float minimumZoom = 13f;
		/** The maximum FOV, if allowMouseWheelZooming = True */
		public float maximumZoom = 65f;

		/** If True, then the camera will bob up and down as the Player moves */
		public bool headBob = true;
		/** The method of head-bobbing to employ, if headBob = True (BuiltIn, CustomAnimation, CustomScript) */
		public FirstPersonHeadBobMethod headBobMethod = FirstPersonHeadBobMethod.BuiltIn;
		/** The bobbing speed, if headBob = True and headBobMethod = FirstPersonHeadBobMethod.BuiltIn */
		public float builtInSpeedFactor = 1f;
		/** The bobbing magnitude, if headBob = True and headBobMethod = FirstPersonHeadBobMethod.BuiltIn */
		public float bobbingAmount = 0.2f;
		private Animator headBobAnimator;
		/** The name of the float parameter in headBobAnimator to set as the head-bob speed, if headBob = True and headBobMethod = FirstPersonHeadBobMethod.CustomAnimation */
		public string headBobSpeedParameter;
		
		private float rotationY = 0f;
		private float bobTimer = 0f;
		private float height = 0f;
		private float deltaHeight = 0f;
		private Player player;


		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			Awake ();
		}


		protected override void Awake ()
		{
			height = transform.localPosition.y;
			player = GetComponentInParent <Player>();
			headBobAnimator = GetComponent <Animator>();
		}


		/**
		 * Overrides the default in _Camera to do nothing.
		 */
		new public void ResetTarget ()
		{}
		

		/**
		 * Updates the camera's transform.
		 * This is called every frame by StateHandler.
		 */
		public void _UpdateFPCamera ()
		{
			if (headBob)
			{
				if (headBobMethod == FirstPersonHeadBobMethod.BuiltIn)
				{
					deltaHeight = 0f;

					float bobSpeed = GetHeadBobSpeed ();
					float waveSlice = Mathf.Sin (bobTimer);
					
					bobTimer += Mathf.Abs (player.GetMoveSpeed ()) * Time.deltaTime * 5f * builtInSpeedFactor;

					if (bobTimer > Mathf.PI * 2)
					{
						bobTimer = bobTimer - (2f * Mathf.PI);
					}

					float totalAxes = Mathf.Clamp (bobSpeed, 0f, 1f);
					
					deltaHeight = totalAxes * waveSlice * bobbingAmount;

					transform.localPosition = new Vector3 (transform.localPosition.x, height + deltaHeight, transform.localPosition.z);
				}
				else if (headBobMethod == FirstPersonHeadBobMethod.CustomAnimation)
				{
					if (headBobAnimator != null && headBobSpeedParameter != "")
					{
						headBobAnimator.SetFloat (headBobSpeedParameter, GetHeadBobSpeed ());
					}
				}
			}

			if (KickStarter.stateHandler.gameState != GameState.Normal)
			{
				return;
			}

			if (allowMouseWheelZooming && Camera != null && KickStarter.stateHandler.gameState == AC.GameState.Normal)
			{
				float scrollWheelInput = KickStarter.playerInput.InputGetAxis ("Mouse ScrollWheel");
				if (scrollWheelInput > 0f)
				{
					Camera.fieldOfView = Mathf.Max (Camera.fieldOfView - 3, minimumZoom);
				}
				else if (scrollWheelInput < 0f)
				{
					Camera.fieldOfView = Mathf.Min (Camera.fieldOfView + 3, maximumZoom);
				}
			}
		}


		/**
		 * <summary>Gets the desired head-bobbing speed, to be manipulated via a custom script if headBobMethod = FirstPersonHeadBobMethod.CustomScript.</summary>
		 * <returns>The desired head-bobbing speed. Returns zero if the player is idle.</returns>
		 */
		public float GetHeadBobSpeed ()
		{
			if (player != null && player.IsGrounded ())
			{
				return Mathf.Abs (player.GetMoveSpeed ());
			}
			return 0f;
		}
		
		
		private void FixedUpdate ()
		{
			rotationY = Mathf.Clamp (rotationY, minY, maxY);
			transform.localEulerAngles = new Vector3 (rotationY, 0f, 0f);
		}
		

		/**
		 * <summary>Sets the pitch to a specific angle.</summary>
		 * <param name = "angle">The new pitch angle</param>
		 */
		public void SetPitch (float angle)
		{
			rotationY = angle;
		}


		/**
		 * <summary>Increases the pitch, accounting for sensitivity</summary>
		 * <param name = "increase">The amount to increase sensitivity by</param>
		 */
		public void IncreasePitch (float increase)
		{
			rotationY += increase * sensitivity.y;
		}

	}

}
