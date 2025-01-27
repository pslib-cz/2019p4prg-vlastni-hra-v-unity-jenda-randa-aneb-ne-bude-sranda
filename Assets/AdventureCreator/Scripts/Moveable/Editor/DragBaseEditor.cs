﻿using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(DragBase))]
	public class DragBaseEditor : Editor
	{

		protected CursorManager cursorManager;


		protected void GetReferences ()
		{
			if (AdvGame.GetReferences ().cursorManager)
			{
				cursorManager = AdvGame.GetReferences ().cursorManager;
			}
		}


		protected void SharedGUI (DragBase _target, bool isOnHinge)
		{
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Collision settings:", EditorStyles.boldLabel);
			_target.ignorePlayerCollider = CustomGUILayout.ToggleLeft ("Ignore Player's collider?", _target.ignorePlayerCollider, "", "If True, then the Physics system will ignore collisions between this object and the player");
			_target.ignoreMoveableRigidbodies = CustomGUILayout.ToggleLeft ("Ignore Moveable Rigidbodies?", _target.ignoreMoveableRigidbodies, "", " If True, then the Physics system will ignore collisions between this object and the bounday colliders of any DragTrack that this is not locked to");
			_target.childrenShareLayer = CustomGUILayout.ToggleLeft ("Place children on same layer?", _target.childrenShareLayer, "", "If True, then this object's children will be placed on the same layer");
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Icon settings:", EditorStyles.boldLabel);
			_target.showIcon = CustomGUILayout.Toggle ("Icon at contact point?", _target.showIcon, "", "If True, then an icon will be displayed at the 'grab point' when the object is held");
			if (_target.showIcon)
			{
				if (cursorManager && cursorManager.cursorIcons.Count > 0)
				{
					int cursorInt = cursorManager.GetIntFromID (_target.iconID);
					cursorInt = CustomGUILayout.Popup ("Cursor icon:", cursorInt, cursorManager.GetLabelsArray (), "", "The cursor that gets shown when held");
					_target.iconID = cursorManager.cursorIcons [cursorInt].id;
				}
				else
				{
					_target.iconID = -1;
				}
			}		
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Sound settings:", EditorStyles.boldLabel);
			_target.moveSoundClip = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Move sound:", _target.moveSoundClip, false, "", "The sound to play when the object is moved");
			_target.slideSoundThreshold = CustomGUILayout.FloatField ("Min. move speed:", _target.slideSoundThreshold, "", "The minimum speed that the object must be moving by for sound to play");
			_target.slidePitchFactor = CustomGUILayout.FloatField ("Pitch factor:", _target.slidePitchFactor, "", "The factor by which the movement sound's pitch is adjusted in relation to speed");

			_target.collideSoundClip = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Collide sound:", _target.collideSoundClip, false, "", "The sound to play when the object has a collision");
			if (isOnHinge)
			{
				_target.onlyPlayLowerCollisionSound = CustomGUILayout.Toggle ("Only on lower boundary?", _target.onlyPlayLowerCollisionSound, "", "If True, then the collision sound will only play when the object collides with its lower boundary collider");
			}
			EditorGUILayout.EndVertical ();
		}

	}

}