﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2015
 *	
 *	"FollowSortingMap.cs"
 * 
 *	This script causes any attached Sprite Renderer
 *	to change according to the scene's Sorting Map.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Attach this script to a GameObject to affect the sorting values of it's SpriteRenderer component, according to the scene's SortingMap.
	 * It is also used by the Char script to scale a 2D character's sprite child, if the SortingMap controls scale.
	 * This is intended for 2D character sprites, to handle their scale and display when moving around a scene.
	 */
	[ExecuteInEditMode]
	public class FollowSortingMap : MonoBehaviour
	{

		/** If True, the SpriteRenderer's sorting values will be locked to their current values */
		public bool lockSorting = false;
		/** If True, then the sorting values of child SpriteRenderers will be affected as well */
		public bool affectChildren = true;
		/** If True, then the SpriteRenderer's sorting values will be amended based on the GameObject's position relative to the scene's SortingMap */
		public bool followSortingMap = false;
		/** If True, then the SpriteRenderer's sorting values will be increased by their original values when the game began */
		public bool offsetOriginal = false;
		/** If True, then the script will update the SpriteRender's sorting values when the game is not running */ 
		public bool livePreview = false;
		
		private float originalDepth = 0f;
		private enum DepthAxis { Y, Z };
		private DepthAxis depthAxis = DepthAxis.Y;
		
		private Renderer _renderer;
		
		private List<int> offsets = new List<int>();
		private int sortingOrder = 0;
		private string sortingLayer = "";
		private SortingMap sortingMap;
		
		
		private void Awake ()
		{
			if (KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}
			if (GetComponent <Renderer>())
			{
				_renderer = GetComponent <Renderer>();
			}
			else
			{
				Debug.LogWarning ("FollowSortingMap on " + gameObject.name + " must be attached alongside a Renderer component.");
			}
			SetOriginalDepth ();
		}
		
		
		private void Start ()
		{
			if (KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}
			
			UpdateSortingMap ();
			SetOriginalOffsets ();
		}
		
		
		private void LateUpdate ()
		{
			UpdateRenderers ();
		}
		
		
		private void SetOriginalOffsets ()
		{
			if (offsets.Count > 0)
			{
				return;
			}
			
			offsets = new List<int>();
			
			if (offsetOriginal && _renderer)
			{
				if (affectChildren)
				{
					Renderer[] renderers = GetComponentsInChildren <Renderer>();
					foreach (Renderer childRenderer in renderers)
					{
						offsets.Add (childRenderer.sortingOrder);
					}
				}
				else
				{
					offsets.Add (_renderer.sortingOrder);
				}
			}
		}
		

		/**
		 * Gets the order of the sprite, according to the GameObject's position in the SortingMap.
		 * If the SortingMap affects the sprite's order in layer, then the order number will be returned.
		 * If the SortingMap affects the sprite's sorting layer, then the layer name will be returned.
		 */
		public string GetOrder ()
		{
			if (sortingMap == null)
			{
				return "";
			}
			
			if (sortingMap.mapType == SortingMapType.OrderInLayer)
			{
				return sortingOrder.ToString ();
			}
			else
			{
				return sortingLayer;
			}
		}
		
		
		private void OnLevelWasLoaded ()
		{
			if (KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}
			
			UpdateSortingMap ();
			SetOriginalOffsets ();
		}
		
		
		private void SetOriginalDepth ()
		{
			if (KickStarter.settingsManager == null)
			{
				return;
			}
			
			if (KickStarter.settingsManager.IsTopDown ())
			{
				depthAxis = DepthAxis.Y;
				originalDepth = transform.localPosition.y;
			}
			else
			{
				depthAxis = DepthAxis.Z;
				originalDepth = transform.localPosition.z;
			}
		}
		

		/**
		 * <summary>Offsets the GameObject's position by a small ammount.
		 * This is done to ensure that multiple GameObjects with FollowSortingMaps in the same SortingMap region are displayed in the correct order.</summary>
		 * <param name = "depth">The amount to offset the GameObject by</param>
		 */
		public void SetDepth (float depth)
		{
			if (depthAxis == DepthAxis.Y)
			{
				transform.localPosition = new Vector3 (transform.localPosition.x, originalDepth + depth, transform.localPosition.z);
			}
			else
			{
				transform.localPosition = new Vector3 (transform.localPosition.x, transform.localPosition.y, originalDepth + depth);
			}
		}
		

		/**
		 * Tells the scene's SortingMap to account for this particular instance of FollowSortingMap.
		 */
		public void UpdateSortingMap ()
		{
			if (KickStarter.sceneSettings != null && KickStarter.sceneSettings.sortingMap != null)
			{
				sortingMap = KickStarter.sceneSettings.sortingMap;
				SetOriginalDepth ();
				sortingMap.UpdateSimilarFollowers (this);
			}
			else
			{
				Debug.Log (this.gameObject.name + " cannot find Sorting Map to follow!");
			}
		}
		
		
		private void UpdateRenderers ()
		{
			if (lockSorting || !followSortingMap || sortingMap == null || _renderer == null)
			{
				return;
			}
			
			#if UNITY_EDITOR
			if (!Application.isPlaying && !livePreview)
			{
				if (GetComponentInParent <Char>())
				{
					GetComponentInParent <Char>().transform.localScale = Vector3.one;
					return;
				}
			}
			#endif
			
			sortingMap.UpdateSimilarFollowers (this);
			
			if (sortingMap.sortingAreas.Count > 0)
			{
				// Set initial value as below the last line
				if (sortingMap.mapType == SortingMapType.OrderInLayer)
				{
					sortingOrder = sortingMap.sortingAreas [sortingMap.sortingAreas.Count-1].order;
				}
				else if (sortingMap.mapType == SortingMapType.SortingLayer)
				{
					sortingLayer = sortingMap.sortingAreas [sortingMap.sortingAreas.Count-1].layer;
				}
				
				for (int i=0; i<sortingMap.sortingAreas.Count; i++)
				{
					// Determine angle between SortingMap's normal and relative position - if <90, must be "behind" the plane
					if (Vector3.Angle (sortingMap.transform.forward, sortingMap.GetAreaPosition (i) - transform.position) < 90f)
					{
						if (sortingMap.mapType == SortingMapType.OrderInLayer)
						{
							sortingOrder = sortingMap.sortingAreas [i].order;
						}
						else if (sortingMap.mapType == SortingMapType.SortingLayer)
						{
							sortingLayer = sortingMap.sortingAreas [i].layer;
						}
						break;
					}
				}
			}
			
			if (!affectChildren)
			{
				if (sortingMap.mapType == SortingMapType.OrderInLayer)
				{
					_renderer.sortingOrder = sortingOrder;
					
					if (offsetOriginal && offsets.Count > 0)
					{
						_renderer.sortingOrder += offsets[0];
					}
				}
				else if (sortingMap.mapType == SortingMapType.SortingLayer)
				{
					_renderer.sortingLayerName = sortingLayer;
					
					if (offsetOriginal && offsets.Count > 0)
					{
						_renderer.sortingOrder = offsets[0];
					}
					else
					{
						_renderer.sortingOrder = 0;
					}
				}
				
				return;
			}
			
			Renderer[] renderers = GetComponentsInChildren <Renderer>();
			for (int i=0; i<renderers.Length; i++)
			{
				if (sortingMap.mapType == SortingMapType.OrderInLayer)
				{
					renderers[i].sortingOrder = sortingOrder;
					
					if (offsetOriginal && offsets.Count > i)
					{
						renderers[i].sortingOrder += offsets[i];
					}
				}
				else if (sortingMap.mapType == SortingMapType.SortingLayer)
				{
					renderers[i].sortingLayerName = sortingLayer;
					
					if (offsetOriginal && offsets.Count > i)
					{
						renderers[i].sortingOrder = offsets[i];
					}
					else
					{
						renderers[i].sortingOrder = 0;
					}
				}
			}
			
			#if UNITY_EDITOR
			if (!Application.isPlaying && livePreview && GetComponentInParent <Char>())
			{
				GetComponentInParent <Char>().transform.localScale = Vector3.one * GetLocalScale ();
			}
			#endif
		}
		

		/**
		 * <summary>Locks the SpriteRenderer to a specific order within it's layer.</summary>
		 * <param name = "order">The order within it's current layer to lock the SpriteRenderer to</param>
		 */
		public void LockSortingOrder (int order)
		{
			if (_renderer == null) return;
			
			lockSorting = true;
			
			if (!affectChildren)
			{
				_renderer.sortingOrder = order;
				return;
			}
			
			Renderer[] renderers = GetComponentsInChildren <Renderer>();
			foreach (Renderer childRenderer in renderers)
			{
				childRenderer.sortingOrder = order;
			}
		}
		

		/**
		 * <summary>Locks the SpriteRenderer to a specific layer.</summary>
		 * <param name = "layer">The layer to lock the SpriteRenderer to</param>
		 */
		public void LockSortingLayer (string layer)
		{
			if (_renderer == null) return;
			
			lockSorting = true;
			
			if (!affectChildren)
			{
				_renderer.sortingLayerName = layer;
				return;
			}
			
			Renderer[] renderers = GetComponentsInChildren <Renderer>();
			foreach (Renderer childRenderer in renderers)
			{
				childRenderer.sortingLayerName = layer;
			}
		}
		

		/**
		 * <summary>Gets the intended scale factor of the GameObject, based on it's position on the scene's SortingMap.</summary>
		 * <returns>The intended scale factor.  If 0, then the Char script will not make use of it.</returns>
		 */
		public float GetLocalScale ()
		{
			if (followSortingMap && sortingMap != null && sortingMap.affectScale)
			{
				return (sortingMap.GetScale (transform.position) / 100f);
			}
			return 0f;
		}
		

		/**
		 * <summary>Gets the indended speed factor of the GameObject, based on it's position on the scene's SortingMap.</summary>
		 * <returns>The intended speed factor.  This is used by the Char script to amend the character's speed.</returns>
		 */
		public float GetLocalSpeed ()
		{
			if (followSortingMap && sortingMap != null && sortingMap.affectScale && sortingMap.affectSpeed)
			{
				return (sortingMap.GetScale (transform.position) / 100f);
			}
			
			return 1f;
		}
		
	}
	
}