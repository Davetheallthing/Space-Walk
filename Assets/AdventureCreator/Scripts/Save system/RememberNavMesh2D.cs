﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2014
 *	
 *	"RememberNavMesh2D.cs"
 * 
 *	This script is attached to NavMesh2D prefabs
 *	who have their "holes" changed during gameplay.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script is attached to NavMesh2D objects who have their "holes" changed during gameplay.
	 */
	public class RememberNavMesh2D : Remember
	{

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			NavMesh2DData navMesh2DData = new NavMesh2DData ();
			
			navMesh2DData.objectID = constantID;
			
			if (GetComponent <NavigationMesh>())
			{
				NavigationMesh navMesh = GetComponent <NavigationMesh>();
				List<int> linkedIDs = new List<int>();

				for (int i=0; i<navMesh.polygonColliderHoles.Count; i++)
				{
					if (navMesh.polygonColliderHoles[i].GetComponent <ConstantID>())
					{
						linkedIDs.Add (navMesh.polygonColliderHoles[i].GetComponent <ConstantID>().constantID);
					}
					else
					{
						Debug.LogWarning ("Cannot save " + this.gameObject.name + "'s holes because " + navMesh.polygonColliderHoles[i].gameObject.name + " has no Constant ID!");
					}
				}

				navMesh2DData._linkedIDs = ArrayToString <int> (linkedIDs.ToArray ());
			}
			
			return Serializer.SaveScriptData <NavMesh2DData> (navMesh2DData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to it's previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			NavMesh2DData data = Serializer.LoadScriptData <NavMesh2DData> (stringData);
			if (data == null) return;

			if (GetComponent <NavigationMesh>())
			{
				NavigationMesh navMesh = GetComponent <NavigationMesh>();
				navMesh.polygonColliderHoles.Clear ();

				int[] linkedIDs = StringToIntArray (data._linkedIDs);

				for (int i=0; i<linkedIDs.Length; i++)
				{
					PolygonCollider2D polyHole = Serializer.returnComponent <PolygonCollider2D> (linkedIDs[i]);
					if (polyHole != null)
					{
						navMesh.AddHole (polyHole);
					}
				}
			}
		}
		
	}
	

	/**
	 * A data container used by the RememberNavMesh2D script.
	 */
	[System.Serializable]
	public class NavMesh2DData : RememberData
	{

		/** The Constant ID numbers of each "hole" currently assigned */
		public string _linkedIDs;

		/**
		 * The default Constructor.
		 */
		public NavMesh2DData () { }

	}
	
}