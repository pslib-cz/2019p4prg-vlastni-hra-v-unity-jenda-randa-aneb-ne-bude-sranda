/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"GameCamera.cs"
 * 
 *	This is attached to cameras that act as "guides" for the Main Camera.
 *	They are never active: only the Main Camera is ever active.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * The standard camera used in 3D games.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_game_camera.html")]
	#endif
	public class GameCamera : CursorInfluenceCamera
	{

		/** If True, then the camera's position will be relative to the scene's default PlayerStart, rather then the Player's initial position. This ensures that camera movement is the same regardless of where the Player begins in the scene */
		public bool actFromDefaultPlayerStart = true;

		/** If True, movement in the X-axis is prevented */
		public bool lockXLocAxis = true;
		/** If True, movement in the Y-axis is prevented */
		public bool lockYLocAxis = true;
		/** If True, movement in the Z-axis is prevented */
		public bool lockZLocAxis = true;
		/** If True, pitch rotation is prevented */
		public bool lockXRotAxis = true;
		/** If True, spin rotation is prevented */
		public bool lockYRotAxis = true;
		/** If True, changing of the FOV is prevented */
		public bool lockFOV = true;

		/** The constrain type on X-axis movement, if lockXLocAxis = False (TargetX, TargetZ, TargetAcrossScreen, TargetIntoScreen, SideScrolling, TargetHeight) */
		public CameraLocConstrainType xLocConstrainType;
		/** The constrain type on Y-axis movement, if lockYLocAxis = False (TargetX, TargetZ, TargetAcrossScreen, TargetIntoScreen, SideScrolling, TargetHeight) */
		public CameraLocConstrainType yLocConstrainType = CameraLocConstrainType.TargetHeight;
		/** The constrain type on Z-axis movement, if lockZLocAxis = False (TargetX, TargetZ, TargetAcrossScreen, TargetIntoScreen, SideScrolling, TargetHeight) */
		public CameraLocConstrainType zLocConstrainType;
		/** The constrain type on pitch rotation, if lockXRotAxis = False (TargetX, TargetZ, TargetAcrossScreen, TargetIntoScreen, SideScrolling, TargetHeight) */
		public CameraLocConstrainType xRotConstrainType = CameraLocConstrainType.TargetHeight;
		/** The constrain type on spin rotation, if lockYRotAxis = False (TargetX, TargetZ, TargetAcrossScreen, TargetIntoScreen, LookAtTarget) */
		public CameraRotConstrainType yRotConstrainType;

		/** The influence of the target's position on X-axis movement, if lockXLocAxis = False */
		public float xGradient = 1f;
		/** The influence of the target's position on Y-axis movement, if lockYLocAxis = False */
		public float yGradientLoc = 1f;
		/** The influence of the target's position on Z-axis movement, if lockZLocAxis = False */
		public float zGradient = 1f;
		/** The influence of the target's position on pitch rotation, if lockXRotAxis = False */
		public float xGradientRot = 2f;
		/** The influence of the target's position on spin rotation, if lockYRotAxis = False */
		public float yGradient = 2f;
		/** The influence of the target's position on FOV, if lockFOV = True */
		public float FOVGradient = 2f;

		/** The X-axis position offset, if lockXLocAxis = False */
		public float xOffset = 0f;
		/** The Y-axis position offset, if lockYLocAxis = False */
		public float yOffsetLoc = 0f;
		/** The Z-axis position offset, if lockZLocAxis = False */
		public float zOffset = 0f;
		/** The pitch rotation offset, if lockXRotAxis = False */
		public float xOffsetRot = 0f;
		/** The spin rotation offset, if lockYRotAxis = False */
		public float yOffset = 0f;
		/** The FOV offset, if lockXLocAxis = False */
		public float FOVOffset = 0f;

		/** The track freedom along the X-axis, if xLocConstrainType = CameraLocConstrainType.SideScrolling */
		public float xFreedom = 2f;
		/** The track freedom along the Y-axis, if yLocConstrainType = CameraLocConstrainType.SideScrolling */
		public float yFreedom = 2f;
		/** The track freedom along the Z-axis, if zLocConstrainType = CameraLocConstrainType.SideScrolling */
		public float zFreedom = 2f;

		/** If True, then X-axis movement will be limited to minimum and maximum values */
		public bool limitX;
		/** If True, then Y-axis movement will be limited to minimum and maximum values */
		public bool limitYLoc;
		/** If True, then Z-axis movement will be limited to minimum and maximum values */
		public bool limitZ;
		/** If True, then pitch rotation will be limited to minimum and maximum values */
		public bool limitXRot;
		/** If True, then spin rotation will be limited to minimum and maximum values */
		public bool limitY;
		/** If True, then FOV will be limited to minimum and maximum values */
		public bool limitFOV;

		/** The target height offset */
		public float targetHeight;
		/** The target positional offset in the X-axis */
		public float targetXOffset;
		/** The target positional offset in the Z-axis */
		public float targetZOffset;

		/** The lower and upper X-axis movement limits, if limitX = True */
		public Vector2 constrainX;
		/** The lower and upper Y-axis movement limits, if limitYLoc = True */
		public Vector2 constrainYLoc;
		/** The lower and upper Z-axis movement limits, if limitZ = True */
		public Vector2 constrainZ;
		/** The lower and upper pitch rotation limits, if limitXRot = True */
		public Vector2 constrainXRot;
		/** The lower and upper spin rotation limits, if limitY = True */
		public Vector2 constrainY;
		/** The lower and upper FOV limits, if limitFOV = True */
		public Vector2 constrainFOV;

		/** The influence that the target's facing direction has on the tracking position */
		public float directionInfluence = 0f;
		/** The follow speed when tracking a target */
		public float dampSpeed = 0.9f;

		/** If True, then focalDistance will match the distance to target */
		public bool focalPointIsTarget = false;

		private Vector3 desiredPosition;
		private float desiredSpin;
		private float desiredPitch;
		private float desiredFOV;
		
		private Vector3 originalTargetPosition;
		private Vector3 originalPosition;
		private float originalSpin;
		private float originalPitch;
		private float originalFOV;
		private bool haveSetOriginalPosition = false;


		protected override void Awake ()
		{
			base.Awake ();

			SetOriginalPosition ();

			desiredPosition = originalPosition;
			desiredPitch = originalPitch;
			desiredSpin = originalSpin;
			desiredFOV = originalFOV;
			
			if (!lockXLocAxis && limitX)
			{
				desiredPosition.x = ConstrainAxis (desiredPosition.x, constrainX);
			}
			
			if (!lockYLocAxis && limitY)
			{
				desiredPosition.y = ConstrainAxis (desiredPosition.y, constrainYLoc);
			}
			
			if (!lockZLocAxis && limitZ)
			{
				desiredPosition.z = ConstrainAxis (desiredPosition.z, constrainZ);
			}

			if (!lockXRotAxis && limitXRot)
			{
				desiredPitch = ConstrainAxis (desiredPitch, constrainXRot);
			}
			
			if (!lockYRotAxis && limitY && yRotConstrainType != CameraRotConstrainType.LookAtTarget)
			{
				desiredSpin = ConstrainAxis (desiredSpin, constrainY);
			}
			
			if (!lockFOV && limitFOV)
			{
				desiredFOV = ConstrainAxis (desiredFOV, constrainFOV);
			}
		}
		
		
		protected override void Start ()
		{
			base.Start ();

			ResetTarget ();
			
			if (target)
			{
				SetTargetOriginalPosition ();
				MoveCameraInstant ();
			}
		}


		public override void _Update ()
		{
			if (target == null)
			{
				return;
			}

			SetDesired ();
			
			if (!lockXLocAxis || !lockYLocAxis || !lockZLocAxis)
			{
				transform.position = (dampSpeed > 0f)
										? Vector3.Lerp (transform.position, desiredPosition, Time.deltaTime * dampSpeed)
										: desiredPosition;
			}
			
			if (!lockFOV)
			{
				if (Camera.orthographic)
				{
					Camera.orthographicSize = (dampSpeed > 0f)
						? Mathf.Lerp (Camera.orthographicSize, desiredFOV, Time.deltaTime * dampSpeed)
												: desiredFOV;
				}
				else
				{
					Camera.fieldOfView = (dampSpeed > 0f)
						? Mathf.Lerp (Camera.fieldOfView, desiredFOV, Time.deltaTime * dampSpeed)
											: desiredFOV;
				}
			}

			float newPitch = transform.eulerAngles.x;
			if (!lockXRotAxis)
			{
				float t = transform.eulerAngles.x;
				if (t > 180f)
				{
					t -= 360f;
				}

				newPitch = (dampSpeed > 0f)
							? Mathf.Lerp (t, desiredPitch, Time.deltaTime * dampSpeed)
							: desiredPitch;
			}
			
			if (!lockYRotAxis)
			{
				if (yRotConstrainType == CameraRotConstrainType.LookAtTarget)
				{
					if (!lockXRotAxis)
					{
						ACDebug.LogWarning (gameObject.name + " cannot obey Pitch rotation, since Spin rotation's 'Look At Target' is overriding.", gameObject);
					}

					if (target)
					{
						Vector3 lookAtPos = target.position;
						lookAtPos.y += targetHeight;
						lookAtPos.x += targetXOffset;
						lookAtPos.z += targetZOffset;
						
						// Look at and dampen the rotation
						Vector3 lookDir = lookAtPos - transform.position;
						if (!Mathf.Approximately (directionInfluence, 0f))
						{
							lookDir += TargetForward * directionInfluence;
						}

						Quaternion rotation = Quaternion.LookRotation (lookDir);

						transform.rotation = (dampSpeed > 0f)
												? Quaternion.Slerp (transform.rotation, rotation, Time.deltaTime * dampSpeed)
												: rotation;
					}
					else if (!targetIsPlayer)
					{
						ACDebug.LogWarning (this.name + " has no target", gameObject);
					}
				}
				else
				{
					float thisSpin = transform.eulerAngles.y;
					if (desiredSpin > (thisSpin + 180f))
					{
						desiredSpin -= 360f;
					}
					else if (thisSpin > (desiredSpin + 180f))
					{
						thisSpin -= 360f;
					}

					float newSpin = (dampSpeed > 0f)
									  ? Mathf.Lerp (thisSpin, desiredSpin, Time.deltaTime * dampSpeed)
									  : desiredSpin;

					transform.eulerAngles = new Vector3 (newPitch, newSpin, transform.eulerAngles.z);
				}
			}
			else
			{
				transform.eulerAngles = new Vector3 (newPitch, transform.eulerAngles.y, transform.eulerAngles.z);
			}

			SetFocalPoint ();
		}


		public override void SwitchTarget (Transform _target)
		{
			base.SwitchTarget (_target);
			originalTargetPosition = Vector3.zero;
			SetTargetOriginalPosition ();
		}
		
		
		private void SetTargetOriginalPosition ()
		{
			if (originalTargetPosition == Vector3.zero)
			{
				if (actFromDefaultPlayerStart)
				{
					if (KickStarter.sceneSettings != null && KickStarter.sceneSettings.defaultPlayerStart != null)
					{
						originalTargetPosition = KickStarter.sceneSettings.defaultPlayerStart.transform.position;
					}
					else
					{
						originalTargetPosition = target.position;
					}
				}
				else
				{
					originalTargetPosition = target.position;
				}
			}
		}
		
		
		private void TrackTarget2D_X ()
		{
			if (target.position.x < (transform.position.x - xFreedom))
			{
				desiredPosition.x = target.position.x + xFreedom;
			}
			else if (target.position.x > (transform.position.x + xFreedom))
			{
				desiredPosition.x = target.position.x - xFreedom;
			}

			desiredPosition.x += xOffset;
		}


		private void TrackTarget2D_Y ()
		{
			if (target.position.y < (transform.position.y - yFreedom))
			{
				desiredPosition.y = target.position.y + yFreedom;
			}
			else if (target.position.y > (transform.position.y + yFreedom))
			{
				desiredPosition.y = target.position.y - yFreedom;
			}

			desiredPosition.y += yOffsetLoc;
		}
		
		
		private void TrackTarget2D_Z ()
		{
			if (target.position.z < (transform.position.z - zFreedom))
			{
				desiredPosition.z = target.position.z + zFreedom;
			}
			else if (target.position.z > (transform.position.z + zFreedom))
			{
				desiredPosition.z = target.position.z -zFreedom;
			}

			desiredPosition.z += zOffset;
		}
		
		
		private float GetDesiredPosition (float originalValue, float gradient, float offset, CameraLocConstrainType constrainType )
		{
			float desiredPosition = originalValue + offset;
			
			if (constrainType == CameraLocConstrainType.TargetX)
			{
				desiredPosition += (target.position.x - originalTargetPosition.x) * gradient;
			}
			else if (constrainType == CameraLocConstrainType.TargetZ)
			{
				desiredPosition += (target.position.z - originalTargetPosition.z) * gradient;
			}
			else if (constrainType == CameraLocConstrainType.TargetIntoScreen)
			{
				desiredPosition += (PositionRelativeToCamera (originalTargetPosition).x - PositionRelativeToCamera (target.position).x) * gradient;
			}
			else if (constrainType == CameraLocConstrainType.TargetAcrossScreen)
			{
				desiredPosition += (PositionRelativeToCamera (originalTargetPosition).z - PositionRelativeToCamera (target.position).z) * gradient;
			}
			else if (constrainType == CameraLocConstrainType.TargetHeight)
			{
				desiredPosition += (target.position.y - originalTargetPosition.y) * gradient;
			}
			
			return desiredPosition;
		}


		private bool AllLocked ()
		{
			if (lockXLocAxis && lockYLocAxis && lockZLocAxis && lockXRotAxis && lockYRotAxis && lockFOV)
			{
				return true;
			}
			return false;
		}


		private void SetFocalPoint ()
		{
			if (focalPointIsTarget && target != null)
			{
				focalDistance = Vector3.Dot (transform.forward, target.position - transform.position);
				if (focalDistance < 0f)
				{
					focalDistance = 0f;
				}
			}
		}


		private void SetOriginalPosition ()
		{	
			if (!haveSetOriginalPosition)
			{
				originalPosition = transform.position;
				originalSpin = transform.eulerAngles.y;
				originalPitch = transform.eulerAngles.x;

				if (Camera != null)
				{
					originalFOV = Camera.fieldOfView;
				}
				haveSetOriginalPosition = true;
			}
		}

		
		public override void MoveCameraInstant ()
		{
			if (targetIsPlayer && KickStarter.player)
			{
				target = KickStarter.player.transform;
			}

			SetOriginalPosition ();
			SetDesired ();
			
			if (!lockXLocAxis || !lockYLocAxis || !lockZLocAxis)
			{
				transform.position = desiredPosition;
			}

			float pitch = transform.eulerAngles.x;
			if (!lockXRotAxis)
			{
				pitch = desiredPitch;
			}
			
			if (!lockYRotAxis)
			{
				if (yRotConstrainType == CameraRotConstrainType.LookAtTarget)
				{
					if (target)
					{
						Vector3 lookAtPos = target.position;
						lookAtPos.y += targetHeight;
						lookAtPos.x += targetXOffset;
						lookAtPos.z += targetZOffset;
						
						Quaternion rotation = Quaternion.LookRotation (lookAtPos - transform.position);
						transform.rotation = rotation;
					}
				}
				else
				{
					transform.eulerAngles = new Vector3 (pitch, desiredSpin, transform.eulerAngles.z);
				}
			}
			else
			{
				transform.eulerAngles = new Vector3 (pitch, transform.eulerAngles.y, transform.eulerAngles.z);
			}

			SetDesiredFOV ();
			if (!lockFOV)
			{
				Camera.fieldOfView = desiredFOV;
			}

			SetFocalPoint ();
		}
		
		
		private void SetDesired ()
		{
			if (lockXLocAxis)
			{
				desiredPosition.x = transform.position.x;
			}
			else
			{
				if (target)
				{
					if (xLocConstrainType == CameraLocConstrainType.SideScrolling)
					{
						TrackTarget2D_X ();
					}
					else
					{
						desiredPosition.x = GetDesiredPosition (originalPosition.x, xGradient, xOffset, xLocConstrainType);
					}
				}
				
				if (limitX)
				{
					desiredPosition.x = ConstrainAxis (desiredPosition.x, constrainX);
				}
			}
			
			if (lockYLocAxis)
			{
				desiredPosition.y = transform.position.y;
			}
			else
			{
				if (target)
				{
					if (yLocConstrainType == CameraLocConstrainType.SideScrolling)
					{
						TrackTarget2D_Y ();
					}
					else
					{
						desiredPosition.y = GetDesiredPosition (originalPosition.y, yGradientLoc, yOffsetLoc, yLocConstrainType);
					}
				}
				
				if (limitYLoc)
				{
					desiredPosition.y = ConstrainAxis (desiredPosition.y, constrainYLoc);
				}
			}
			
			if (lockXRotAxis)
			{
				desiredPitch = transform.eulerAngles.x;
			}
			else
			{
				if (target)
				{
					if (xRotConstrainType != CameraLocConstrainType.SideScrolling)
					{
						desiredPitch = GetDesiredPosition (originalPitch, xGradientRot, xOffsetRot, xRotConstrainType);
					}
				}
				
				if (limitXRot)
				{
					desiredPitch = ConstrainAxis (desiredPitch, constrainXRot);
				}

				desiredPitch = Mathf.Clamp (desiredPitch, -85f, 85f);
			}

			if (lockYRotAxis)
			{
				desiredSpin = 0f;
			}
			else
			{
				if (target)
				{
					desiredSpin = GetDesiredPosition (originalSpin, yGradient, yOffset, (CameraLocConstrainType) yRotConstrainType);

					if (!Mathf.Approximately (directionInfluence, 0f))
					{
						desiredSpin += Vector3.Dot (TargetForward, transform.right) * directionInfluence;
					}
				}
				
				if (limitY)
				{
					desiredSpin = ConstrainAxis (desiredSpin, constrainY);
				}
				
			}
			
			if (lockZLocAxis)
			{
				desiredPosition.z = transform.position.z;
			}
			else
			{
				if (target)
				{
					if (zLocConstrainType == CameraLocConstrainType.SideScrolling)
					{
						TrackTarget2D_Z ();
					}
					else
					{
						desiredPosition.z = GetDesiredPosition (originalPosition.z, zGradient, zOffset, zLocConstrainType);
					}
				}
				
				if (limitZ)
				{
					desiredPosition.z = ConstrainAxis (desiredPosition.z, constrainZ);
				}
			}
			
			SetDesiredFOV ();
		}


		private void SetDesiredFOV ()
		{
			if (lockFOV)
			{
				if (Camera.orthographic)
				{
					desiredFOV = Camera.orthographicSize;
				}
				else
				{
					desiredFOV = Camera.fieldOfView;
				}
			}
			else
			{
				if (target)
				{
					desiredFOV = GetDesiredPosition (originalFOV, FOVGradient, FOVOffset, CameraLocConstrainType.TargetIntoScreen);
				}
				
				if (limitFOV)
				{
					desiredFOV = ConstrainAxis (desiredFOV, constrainFOV);
				}
			}
		}
		
	}

}