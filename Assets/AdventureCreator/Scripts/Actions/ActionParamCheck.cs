﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"ActionParamCheck.cs"
 * 
 *	This action checks to see if a Parameter has been assigned a certain value,
 *	and performs something accordingly.
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
	public class ActionParamCheck : ActionCheck
	{
		
		public int parameterID = -1;
		public int compareParameterID = -1;

		public int intValue;
		public float floatValue;
		public IntCondition intCondition;
		public string stringValue;
		public int compareVariableID;
		public Vector3 vector3Value;

		public GameObject compareObject;
		public int compareObjectConstantID;

		public Object compareUnityObject;

		public BoolValue boolValue = BoolValue.True;
		public BoolCondition boolCondition;

		public VectorCondition vectorCondition = VectorCondition.EqualTo;

		private ActionParameter _parameter, _compareParameter;
		#if UNITY_EDITOR
		[SerializeField] private string parameterLabel = "";
		#endif


		public ActionParamCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ActionList;
			title = "Check parameter";
			description = "Queries the value of parameters defined in the parent ActionList.";
		}
		
		
		override public void AssignValues (List<ActionParameter> parameters)
		{
			_parameter = GetParameterWithID (parameters, parameterID);
			_compareParameter = (compareParameterID != parameterID) ? GetParameterWithID (parameters, compareParameterID) : null;
			compareObject = AssignFile (compareObjectConstantID, compareObject);
		}
		
		
		override public ActionEnd End (List<AC.Action> actions)
		{
			if (_parameter == null)
			{
				return GenerateStopActionEnd ();
			}

			GVar compareVar = null;
			InvItem compareItem = null;

			if (_parameter.parameterType == ParameterType.GlobalVariable || _parameter.parameterType == ParameterType.LocalVariable || _parameter.parameterType == ParameterType.InventoryItem)
			{
				if (compareVariableID == -1)
				{
					return GenerateStopActionEnd ();
				}
				
				if (_parameter.parameterType == ParameterType.GlobalVariable)
				{
					compareVar = GlobalVariables.GetVariable (compareVariableID);
					compareVar.Download ();
				}
				else if (_parameter.parameterType == ParameterType.LocalVariable && !isAssetFile)
				{
					compareVar = LocalVariables.GetVariable (compareVariableID);
				}
				else if (_parameter.parameterType == ParameterType.InventoryItem)
				{
					compareItem = KickStarter.inventoryManager.GetItem (compareVariableID);
				}
			}

			return ProcessResult (CheckCondition (compareItem, compareVar), actions);
		}
		
		
		private bool CheckCondition (InvItem _compareItem, GVar _compareVar)
		{
			if (_parameter == null)
			{
				ACDebug.LogWarning ("Cannot check state of variable since it cannot be found!");
				return false;
			}
			
			if (_parameter.parameterType == ParameterType.Boolean)
			{
				int fieldValue = _parameter.intValue;
				int compareValue = (int) boolValue;

				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareValue = _compareParameter.intValue;
				}

				if (boolCondition == BoolCondition.EqualTo)
				{
					if (fieldValue == compareValue)
					{
						return true;
					}
				}
				else
				{
					if (fieldValue != compareValue)
					{
						return true;
					}
				}
			}
			
			else if (_parameter.parameterType == ParameterType.Integer)
			{
				int fieldValue = _parameter.intValue;
				int compareValue = intValue;

				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareValue = _compareParameter.intValue;
				}

				if (intCondition == IntCondition.EqualTo)
				{
					if (fieldValue == compareValue)
					{
						return true;
					}
				}
				else if (intCondition == IntCondition.NotEqualTo)
				{
					if (fieldValue != compareValue)
					{
						return true;
					}
				}
				else if (intCondition == IntCondition.LessThan)
				{
					if (fieldValue < compareValue)
					{
						return true;
					}
				}
				else if (intCondition == IntCondition.MoreThan)
				{
					if (fieldValue > compareValue)
					{
						return true;
					}
				}
			}
			
			else if (_parameter.parameterType == ParameterType.Float)
			{
				float fieldValue = _parameter.floatValue;
				float compareValue = floatValue;

				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareValue = _compareParameter.floatValue;
				}

				if (intCondition == IntCondition.EqualTo)
				{
					if (Mathf.Approximately (fieldValue, compareValue))
					{
						return true;
					}
				}
				else if (intCondition == IntCondition.NotEqualTo)
				{
					if (!Mathf.Approximately (fieldValue, compareValue))
					{
						return true;
					}
				}
				else if (intCondition == IntCondition.LessThan)
				{
					if (fieldValue < compareValue)
					{
						return true;
					}
				}
				else if (intCondition == IntCondition.MoreThan)
				{
					if (fieldValue > compareValue)
					{
						return true;
					}
				}
			}

			else if (_parameter.parameterType == ParameterType.Vector3)
			{
				if (vectorCondition == VectorCondition.EqualTo)
				{
					if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
					{
						return (_parameter.vector3Value == _compareParameter.vector3Value);
					}

					return (_parameter.vector3Value == vector3Value);
				}
				else if (vectorCondition == VectorCondition.MagnitudeGreaterThan)
				{
					if (_compareParameter != null && _compareParameter.parameterType == ParameterType.Float)
					{
						return (_parameter.vector3Value.magnitude > _compareParameter.floatValue);
					}
					return (_parameter.vector3Value.magnitude > floatValue);
				}
			}
			
			else if (_parameter.parameterType == ParameterType.String)
			{
				string fieldValue = _parameter.stringValue;
				string compareValue = AdvGame.ConvertTokens (stringValue);

				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareValue = _compareParameter.stringValue;
				}

				if (boolCondition == BoolCondition.EqualTo)
				{
					if (fieldValue == compareValue)
					{
						return true;
					}
				}
				else
				{
					if (fieldValue != compareValue)
					{
						return true;
					}
				}
			}

			else if (_parameter.parameterType == ParameterType.GameObject)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareObjectConstantID = _compareParameter.intValue;
					compareObject = _compareParameter.gameObject;
				}

				if ((compareObject != null && _parameter.gameObject == compareObject) ||
					(compareObjectConstantID != 0 && _parameter.intValue == compareObjectConstantID))
				{
					return true;
				}
				if (compareObject == null && _parameter.gameObject == null)
				{
					return true;
				}
			}

			else if (_parameter.parameterType == ParameterType.UnityObject)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareUnityObject = _compareParameter.objectValue;
				}

				if (compareUnityObject != null && _parameter.objectValue == (Object) compareUnityObject)
				{
					return true;
				}
				if (compareUnityObject == null && _parameter.objectValue == null)
				{
					return true;
				}
			}

			else if (_parameter.parameterType == ParameterType.GlobalVariable || _parameter.parameterType == ParameterType.LocalVariable)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					return (_compareParameter.intValue == _parameter.intValue);
				}

				if (_compareVar != null && _parameter.intValue == _compareVar.id)
				{
					return true;
				}
			}

			else if (_parameter.parameterType == ParameterType.InventoryItem)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					return (_compareParameter.intValue == _parameter.intValue);
				}

				if (_compareItem != null && _parameter.intValue == _compareItem.id)
				{
					return true;
				}
			}

			return false;
		}
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI (parameters, parameterID);
			ShowVarGUI (parameters, GetParameterWithID (parameters, parameterID));
		}
		
		
		private void ShowVarGUI (List<ActionParameter> parameters, ActionParameter parameter)
		{
			if (parameters == null || parameters.Count == 0 || parameter == null)
			{
				EditorGUILayout.HelpBox ("No parameters exist! Please define one in the Inspector.", MessageType.Warning);
				parameterLabel = "";
				return;
			}

			parameterLabel = parameter.label;
			EditorGUILayout.BeginHorizontal ();

			if (parameter.parameterType == ParameterType.Boolean)
			{
				boolCondition = (BoolCondition) EditorGUILayout.EnumPopup (boolCondition);

				compareParameterID = Action.ChooseParameterGUI ("", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				if (compareParameterID < 0)
				{
					boolValue = (BoolValue) EditorGUILayout.EnumPopup (boolValue);
				}
			}
			else if (parameter.parameterType == ParameterType.Integer)
			{
				intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

				compareParameterID = Action.ChooseParameterGUI ("", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				if (compareParameterID < 0)
				{
					intValue = EditorGUILayout.IntField (intValue);
				}
			}
			else if (parameter.parameterType == ParameterType.Float)
			{
				intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

				compareParameterID = Action.ChooseParameterGUI ("", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				if (compareParameterID < 0)
				{
					floatValue = EditorGUILayout.FloatField (floatValue);
				}
			}
			else if (parameter.parameterType == ParameterType.String)
			{
				boolCondition = (BoolCondition) EditorGUILayout.EnumPopup (boolCondition);

				compareParameterID = Action.ChooseParameterGUI ("", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				if (compareParameterID < 0)
				{
					stringValue = EditorGUILayout.TextField (stringValue);
				}
			}
			else if (parameter.parameterType == ParameterType.Vector3)
			{
				vectorCondition = (VectorCondition) EditorGUILayout.EnumPopup ("Condition:", vectorCondition);

				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();

				if (vectorCondition == VectorCondition.MagnitudeGreaterThan)
				{
					compareParameterID = Action.ChooseParameterGUI ("Float:", parameters, compareParameterID, ParameterType.Float, parameter.ID);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					if (compareParameterID < 0)
					{
						floatValue = EditorGUILayout.FloatField ("Float:", floatValue);
					}
				}
				else if (vectorCondition == VectorCondition.EqualTo)
				{
					compareParameterID = Action.ChooseParameterGUI ("Vector3", parameters, compareParameterID, parameter.parameterType, parameter.ID);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					if (compareParameterID < 0)
					{
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.LabelField ("Vector3:", GUILayout.MaxWidth (60f));
						vector3Value = EditorGUILayout.Vector3Field ("", vector3Value);
					}
				}
			}
			else if (parameter.parameterType == ParameterType.GameObject)
			{
				compareParameterID = Action.ChooseParameterGUI ("Is equal to:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					compareObject = (GameObject) EditorGUILayout.ObjectField ("Is equal to:", compareObject, typeof (GameObject), true);

					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					compareObjectConstantID = FieldToID (compareObject, compareObjectConstantID);
					compareObject = IDToField (compareObject, compareObjectConstantID, false);
				}
			}
			else if (parameter.parameterType == ParameterType.UnityObject)
			{
				compareParameterID = Action.ChooseParameterGUI ("Is equal to:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					compareUnityObject = (Object) EditorGUILayout.ObjectField ("Is equal to:", compareUnityObject, typeof (Object), true);
				}
			}
			else if (parameter.parameterType == ParameterType.GlobalVariable)
			{
				compareParameterID = Action.ChooseParameterGUI ("Variable:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					if (AdvGame.GetReferences ().variablesManager == null || AdvGame.GetReferences ().variablesManager.vars == null || AdvGame.GetReferences ().variablesManager.vars.Count == 0)
					{
						EditorGUILayout.HelpBox ("No Global variables exist!", MessageType.Info);
					}
					else
					{
						compareVariableID = ShowVarSelectorGUI (AdvGame.GetReferences ().variablesManager.vars, compareVariableID);
					}
				}
			}
			else if (parameter.parameterType == ParameterType.InventoryItem)
			{
				compareParameterID = Action.ChooseParameterGUI ("Inventory item:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					compareVariableID = ShowInvSelectorGUI (compareVariableID);
				}
			}
			else if (parameter.parameterType == ParameterType.LocalVariable)
			{
				compareParameterID = Action.ChooseParameterGUI ("Inventory item:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					if (isAssetFile)
					{
						EditorGUILayout.HelpBox ("Cannot compare local variables in an asset file.", MessageType.Warning);
					}
					else if (KickStarter.localVariables == null || KickStarter.localVariables.localVars == null || KickStarter.localVariables.localVars.Count == 0)
					{
						EditorGUILayout.HelpBox ("No Local variables exist!", MessageType.Info);
					}
					else
					{
						compareVariableID = ShowVarSelectorGUI (KickStarter.localVariables.localVars, compareVariableID);
					}
				}
			}

			EditorGUILayout.EndHorizontal ();
		}


		override public void AssignConstantIDs (bool saveScriptsToo)
		{
			AssignConstantID (compareObject, compareObjectConstantID, 0);
		}
		
		
		override public string SetLabel ()
		{
			return parameterLabel;
		}


		private int ShowVarSelectorGUI (List<GVar> vars, int ID)
		{
			int variableNumber = -1;
			
			List<string> labelList = new List<string>();
			foreach (GVar _var in vars)
			{
				labelList.Add (_var.label);
			}
			
			variableNumber = GetVarNumber (vars, ID);
			
			if (variableNumber == -1)
			{
				// Wasn't found (variable was deleted?), so revert to zero
				ACDebug.LogWarning ("Previously chosen variable no longer exists!");
				variableNumber = 0;
				ID = 0;
			}
			
			variableNumber = EditorGUILayout.Popup ("Variable:", variableNumber, labelList.ToArray());
			ID = vars[variableNumber].id;
			
			return ID;
		}
		
		
		private int ShowInvSelectorGUI (int ID)
		{
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager == null)
			{
				return ID;
			}
			
			int invNumber = -1;
			List<string> labelList = new List<string>();
			int i=0;
			foreach (InvItem _item in inventoryManager.items)
			{
				labelList.Add (_item.label);
				
				// If an item has been removed, make sure selected variable is still valid
				if (_item.id == ID)
				{
					invNumber = i;
				}
				
				i++;
			}
			
			if (invNumber == -1)
			{
				// Wasn't found (item was possibly deleted), so revert to zero
				ACDebug.LogWarning ("Previously chosen item no longer exists!");
				
				invNumber = 0;
				ID = 0;
			}
			
			invNumber = EditorGUILayout.Popup ("Inventory item:", invNumber, labelList.ToArray());
			ID = inventoryManager.items[invNumber].id;
			
			return ID;
		}
		
		
		private int GetVarNumber (List<GVar> vars, int ID)
		{
			int i = 0;
			foreach (GVar _var in vars)
			{
				if (_var.id == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		public override int GetVariableReferences (List<ActionParameter> parameters, VariableLocation location, int varID)
		{
			int thisCount = 0;

			ActionParameter _param = GetParameterWithID (parameters, parameterID);

			if (_param != null && _param.parameterType == ParameterType.LocalVariable && location == VariableLocation.Local && varID == intValue)
			{
				thisCount ++;
			}
			else if (_param != null && _param.parameterType == ParameterType.GlobalVariable && location == VariableLocation.Global && varID == intValue)
			{
				thisCount ++;
			}

			thisCount += base.GetVariableReferences (parameters, location, varID);
			return thisCount;
		}


		public override int GetInventoryReferences (List<ActionParameter> parameters, int _invID)
		{
			ActionParameter _param = GetParameterWithID (parameters, parameterID);
			if (_param != null && _param.parameterType == ParameterType.InventoryItem && _invID == intValue)
			{
				return 1;
			}
			return 0;
		}

		#endif
		
	}
	
}