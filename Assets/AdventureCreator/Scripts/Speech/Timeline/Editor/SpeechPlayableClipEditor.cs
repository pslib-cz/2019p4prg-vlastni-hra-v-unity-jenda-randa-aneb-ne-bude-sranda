﻿#if UNITY_2017_1_OR_NEWER

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof(SpeechPlayableClip))]
	public class SpeechPlayableClipEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			SpeechPlayableClip _target = (SpeechPlayableClip) target;

			_target.ShowGUI ();

			if (GUI.changed)
			{
				EditorUtility.SetDirty (_target);
			}
		}
	}

}

#endif