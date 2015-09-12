/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2014
 *	
 *	"LoadingScreen.cs"
 * 
 *	This script temporarily loads an "in-between" scene that acts
 *	as a loading screen.  Code adapted from work by Robert Utter
 *	at https://chicounity3d.wordpress.com/2014/01/25/loading-screen-tutorial
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Loads an "in-between" scene that acts as a loading screen.  It is added to the scene at runtime by SceneChanger.
	 * It uses code adapated from work by Robert Utter (https://chicounity3d.wordpress.com/2014/01/25/loading-screen-tutorial)
	 */
	public class LoadingScreen : MonoBehaviour
	{

		/**
		 * <summary>Loads a scene (the "loading" scene), and then immediately loads the next scene (the "proper" scene).</summary>
		 * <param name = "sceneName">The name of the next scene to load</param>
		 * <param name = "sceneNumber">The number of the next scene to load, if sceneName = ""</param>
		 * <param name = "loadingSceneName">The name of the loading scene to load</param>
		 * <param name = "loadingSceneNumber">The number of the loading scene to load, if loadingSceneName = ""</param>
		 */
		public void InnerLoad (string sceneName, int sceneNumber, string loadingSceneName, int loadingSceneNumber)
		{
			StartCoroutine (InnerLoadCoroutine (sceneName, sceneNumber, loadingSceneName, loadingSceneNumber));
		}


		private IEnumerator InnerLoadCoroutine (string sceneName, int sceneNumber, string loadingSceneName, int loadingSceneNumber)
		{
			Object.DontDestroyOnLoad (this.gameObject);
			if (loadingSceneName != "")
			{
				Application.LoadLevel (loadingSceneName);
			}
			else
			{
				Application.LoadLevel (loadingSceneNumber);
			}

			yield return null;

			if (sceneName != "")
			{
				Application.LoadLevel (sceneName);
			}
			else
			{
				Application.LoadLevel (sceneNumber);
			}
			Destroy (this.gameObject);
		}

	}

}