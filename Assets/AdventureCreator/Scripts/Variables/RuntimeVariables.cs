/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2015
 *	
 *	"RuntimeVariables.cs"
 * 
 *	This script creates a local copy of the VariableManager's Global vars.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Stores the game's global variables at runtime, as well as the speech log.
	 * This component should be attached to the PersistentEngine prefab.
	 */
	public class RuntimeVariables : MonoBehaviour
	{

		/** The List of the game's global variables. */
		public List<GVar> globalVars = new List<GVar>();

		private List<SpeechLog> speechLines = new List<SpeechLog>();


		/**
		 * Downloads variables from the Global Manager to the scene.
		 * This is public because it is also called when the game is restarted.
		 */
		public void Awake ()
		{
			TransferFromManager ();
		}

		
		/**
		 * Syncs any linked variables with their linked counterparts.
		 * This is public because it is also called when the game is restarted.
		 */
		public void Start ()
		{
			AssignLinkedVariabes ();
			LinkAllValues ();
		}


		/**
		 * <summary>Gets the game's speech log.</summary>
		 * <returns>An array of SpeechLog variables</returns>
		 */
		public SpeechLog[] GetSpeechLog ()
		{
			return speechLines.ToArray ();
		}


		/**
		 * Clears the game's speech log.
		 */
		public void ClearSpeechLog ()
		{
			speechLines.Clear ();
		}


		/**
		 * <summary>Adds a speech line to the game's speech log.</summary>
		 * <param name = "_line">The SpeechLog variable to add</param>
		 */
		public void AddToSpeechLog (SpeechLog _line)
		{
			int ID = _line.lineID;
			if (ID >= 0)
			{
				foreach (SpeechLog speechLine in speechLines)
				{
					if (speechLine.lineID == ID)
					{
						speechLines.Remove (speechLine);
						break;
					}
				}
			}

			speechLines.Add (_line);
		}


		private void TransferFromManager ()
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
			{
				VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
				
				globalVars.Clear ();
				foreach (GVar assetVar in variablesManager.vars)
				{
					globalVars.Add (new GVar (assetVar));
				}
			}
		}

		
		private void AssignLinkedVariabes ()
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
			{
				if (Options.optionsData != null && Options.optionsData.linkedVariables != "")
				{
					SaveSystem.AssignVariables (Options.optionsData.linkedVariables, true);
				}
			}
		}


		private void LinkAllValues ()
		{
			foreach (GVar var in globalVars)
			{
				if (var.link == VarLink.PlaymakerGlobalVariable)
				{
					if (var.updateLinkOnStart)
					{
						var.Download ();
					}
					else
					{
						var.Upload ();
					}
				}
			}
		}


		/**
		 * <summary>Updates a MainData class with it's own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			GlobalVariables.DownloadAll ();
			mainData.runtimeVariablesData = SaveSystem.CreateVariablesData (GlobalVariables.GetAllVars (), false, VariableLocation.Global);

			return mainData;
		}


		/**
		 * <summary>Assigns all Global Variables to preset values.</summary>
		 * <param name = "varPreset">The VarPreset that contains the preset values</param>
		 */
		public void AssignFromPreset (VarPreset varPreset)
		{
			foreach (GVar globalVar in globalVars)
			{
				foreach (PresetValue presetValue in varPreset.presetValues)
				{
					if (globalVar.id == presetValue.id)
					{
						globalVar.val = presetValue.val;
						globalVar.floatVal = presetValue.floatVal;
						globalVar.textVal = presetValue.textVal;
					}
				}
			}
		}


		/**
		 * <summary>Assigns all Glocal Variables to preset values.</summary>
		 * <param name = "varPresetID">The ID number of the VarPreset that contains the preset values</param>
		 */
		public void AssignFromPreset (int varPresetID)
		{
			if (KickStarter.variablesManager.varPresets == null)
			{
				return;
			}

			foreach (VarPreset varPreset in KickStarter.variablesManager.varPresets)
			{
				if (varPreset.ID == varPresetID)
				{
					AssignFromPreset (varPreset);
					return;
				}
			}
		}

	}

}