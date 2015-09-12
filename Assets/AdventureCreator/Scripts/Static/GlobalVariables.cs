/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2014
 *	
 *	"GlobalVariables.cs"
 * 
 *	This script contains static functions to access Global Variables at runtime.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * A class that can manipulate and retrieve the game's Global Variables at runtime.
	 */
	public class GlobalVariables : MonoBehaviour
	{

		/**
		 * <summary>Returns a list of all global variables.</summary>
		 * <returns>A List of GVar variables</returns>
		 */
		public static List<GVar> GetAllVars ()
		{
			if (KickStarter.runtimeVariables)
			{
				return KickStarter.runtimeVariables.globalVars;
			}
			return null;
		}


		/**
		 * Backs up the values of all global variables.
		 * Necessary when skipping ActionLists that involve checking variable values.
		 */
		public static void BackupAll ()
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar _var in KickStarter.runtimeVariables.globalVars)
				{
					_var.BackupValue ();
				}
			}
		}
		
		
		/**
		 * Uploads the values all linked variables to their linked counterparts.
		 */
		public static void UploadAll ()
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar var in KickStarter.runtimeVariables.globalVars)
				{
					var.Upload ();
				}
			}
		}
		
		
		/**
		 * Downloads the values of all linked variables from their linked counterparts.
		 */
		public static void DownloadAll ()
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar var in KickStarter.runtimeVariables.globalVars)
				{
					var.Download ();
				}
			}
		}
		

		/**
		 * <summary>Returns a global variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 */
		public static GVar GetVariable (int _id)
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar _var in KickStarter.runtimeVariables.globalVars)
				{
					if (_var.id == _id)
					{
						return _var;
					}
				}
			}
			return null;
		}
		
		
		/**
		 * <summary>Returns the value of a global Integer variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <returns>The integer value of the variable</returns>
		 */
		public static int GetIntegerValue (int _id)
		{
			return GetVariable (_id).val;
		}
		
		
		/**
		 * <summary>Returns the value of a global Boolean variable.<summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <returns>The bool value of the variable</returns>
		 */
		public static bool GetBooleanValue (int _id)
		{
			if (GetVariable (_id).val == 1)
			{
				return true;
			}
			return false;
		}
		
		
		/**
		 * <summary>Returns the value of a global String variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <returns>The string value of the variable</returns>
		 */
		public static string GetStringValue (int _id)
		{
			return GetVariable (_id).textVal;
		}
		

		/**
		 * <summary>Returns the value of a global Float variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <returns>The float value of the variable</returns>
		 */
		public static float GetFloatValue (int _id)
		{
			return GetVariable (_id).floatVal;
		}
		

		/**
		 * <summary>Returns the value of a global Popup variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <returns>The string value of the variable</returns>
		 */
		public static string GetPopupValue (int _id)
		{
			return GetVariable (_id).GetValue ();
		}


		/**
		 * <summary>Sets the value of a global Integer variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new integer value of the variable</param>
		 */
		public static void SetIntegerValue (int _id, int _value)
		{
			GetVariable (_id).val = _value;
		}

		
		/**
		 * <summary>Sets the value of a global Boolean variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new bool value of the variable</param>
		 */
		public static void SetBooleanValue (int _id, bool _value)
		{
			if (_value)
			{
				GetVariable (_id).val = 1;
			}
			else
			{
				GetVariable (_id).val = 0;
			}
		}
		
		
		/**
		 * <summary>Sets the value of a global String variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new string value of the variable</param>
		 */
		public static void SetStringValue (int _id, string _value)
		{
			GetVariable (_id).textVal = _value;
		}
		
		
		/**
		 * <summary>Sets the value of a global Float variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new float value of the variable</param>
		 */
		public static void SetFloatValue (int _id, float _value)
		{
			GetVariable (_id).floatVal = _value;
		}


		/**
		 * <summary>Sets the value of a global PopUp variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new index value of the variable</param>
		 */
		public static void SetPopupValue (int _id, int _value)
		{
			GetVariable (_id).val = _value;
		}

	}

}