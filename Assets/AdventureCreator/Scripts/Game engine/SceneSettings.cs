/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2014
 *	
 *	"SceneSettings.cs"
 * 
 *	This script defines which cutscenes play when the scene is loaded,
 *	and where the player should begin from.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component is where settings specific to a scene are stored, such as the navigation method, and the Cutscene to play when the scene begins.
	 * The SceneManager provides a UI to assign these fields.
	 * This component should be placed on the GameEngine prefab.
	 */
	public class SceneSettings : MonoBehaviour
	{

		/** The Cutscene to run whenever the game beings from this scene, or when this scene is visited during gameplay */
		public Cutscene cutsceneOnStart;
		/** The Cutscene to run whenever this scene is loaded after restoring a saved game file */
		public Cutscene cutsceneOnLoad;
		/** The Cutscene to run whenever a variable's value is changed */
		public Cutscene cutsceneOnVarChange;
		/** The scene's default PlayerStart prefab */
		public PlayerStart defaultPlayerStart;
		/** The scene's navigation method (meshCollider, UnityNavigation, PolygonCollider) */
		public AC_NavigationMethod navigationMethod = AC_NavigationMethod.meshCollider;
		/** The scene's active NavigationMesh, if navigationMethod != AC_NavigationMethod.UnityNavigation */
		public NavigationMesh navMesh;
		/** The scene's default SortingMap prefab */
		public SortingMap sortingMap;
		/** The scene's default Sound prefab */
		public Sound defaultSound;
		/** The scene's default LightMap prefab */
		public TintMap tintMap;

		/** If True, then the global verticalReductionFactor in SettingsManager will be overridden with a scene-specific value */
		public bool overrideVerticalReductionFactor = false;
		/** How much slower vertical movement is compared to horizontal movement, if the game is in 2D and overriderVerticalReductionFactor = True */
		public float verticalReductionFactor = 0.7f;
		
		
		private void Awake ()
		{
			// Turn off all NavMesh objects
			NavigationMesh[] navMeshes = FindObjectsOfType (typeof (NavigationMesh)) as NavigationMesh[];
			foreach (NavigationMesh _navMesh in navMeshes)
			{
				if (navMesh != _navMesh)
				{
					_navMesh.TurnOff ();
				}
			}
			
			// Turn on default NavMesh if using MeshCollider method
			if (navMesh && (navMesh.GetComponent <Collider>() || navMesh.GetComponent <Collider2D>()))
			{
				navMesh.TurnOn ();
			}
		}
		
		
		private void Start ()
		{
			if (KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			if (KickStarter.saveSystem)
			{
				if (KickStarter.saveSystem.loadingGame == LoadingGame.No)
				{
					KickStarter.levelStorage.ReturnCurrentLevelData (false);
					FindPlayerStart ();
				}
				else if (KickStarter.saveSystem.loadingGame == LoadingGame.JustSwitchingPlayer)
				{
					KickStarter.levelStorage.ReturnCurrentLevelData (false);
				}
				else
				{
					KickStarter.saveSystem.loadingGame = LoadingGame.No;
				}
			}
		}
		

		/**
		 * Resets any references made to the Player prefab by the SortingMap.
		 */
		public void ResetPlayerReference ()
		{
			if (sortingMap)
			{
				sortingMap.GetAllFollowers ();
			}
		}
		
		
		private void FindPlayerStart ()
		{
			PlayerStart playerStart = GetPlayerStart ();
			if (playerStart != null)
			{
				playerStart.SetPlayerStart ();
			}

			bool playedGlobal = KickStarter.stateHandler.PlayGlobalOnStart ();

			if (cutsceneOnStart != null)
			{
				if (!playedGlobal)
				{
					// Place in a temporary cutscene to set everything up
					KickStarter.stateHandler.gameState = GameState.Cutscene;
				}
				Invoke ("RunCutsceneOnStart", 0.01f);
			}
		}


		private void RunCutsceneOnStart ()
		{
			KickStarter.stateHandler.gameState = GameState.Normal;
			cutsceneOnStart.Interact ();
		}
		

		/**
		 * <summary>Gets the appropriate PlayerStart to use when the scene begins.</summary>
		 * <returns>The appropriate PlayerStart to use when the scene begins</returns>
		 */
		public PlayerStart GetPlayerStart ()
		{
			PlayerStart[] starters = FindObjectsOfType (typeof (PlayerStart)) as PlayerStart[];
			foreach (PlayerStart starter in starters)
			{
				if (starter.chooseSceneBy == ChooseSceneBy.Name && starter.previousSceneName != "" && starter.previousSceneName == KickStarter.sceneChanger.previousSceneName)
				{
					return starter;
				}
				if (starter.chooseSceneBy == ChooseSceneBy.Number && starter.previousScene > -1 && starter.previousScene == KickStarter.sceneChanger.previousScene)
				{
					return starter;
				}
			}
			
			if (defaultPlayerStart)
			{
				return defaultPlayerStart;
			}
			
			return null;
		}
		

		/**
		 * Runs the "cutsceneOnLoad" Cutscene.
		 */
		public void OnLoad ()
		{
			if (cutsceneOnLoad != null)
			{
				cutsceneOnLoad.Interact ();
			}
		}
		

		/**
		 * <summary>Plays an AudioClip on the default Sound prefab.</summary>
		 * <param name = "audioClip">The AudioClip to play</param>
		 * <param name = "doLoop">If True, the sound will loop</param>
		 */
		public void PlayDefaultSound (AudioClip audioClip, bool doLoop)
		{
			if (defaultSound == null)
			{
				Debug.Log ("Cannot play sound since no Default Sound Prefab is defined - please set one in the Scene Manager.");
				return;
			}
			
			if (audioClip && defaultSound.GetComponent <AudioSource>())
			{
				defaultSound.GetComponent <AudioSource>().clip = audioClip;
				defaultSound.Play (doLoop);
			}
		}


		/**
		 * Pauses the game by freezing time and sounds.
		 */
		public void PauseGame ()
		{
			// Work out which Sounds will have to be re-played after pausing
			Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			List<Sound> soundsToResume = new List<Sound>();
			foreach (Sound sound in sounds)
			{
				if (sound.playWhilePaused && sound.IsPlaying ())
				{
					soundsToResume.Add (sound);
				}
			}

			#if !UNITY_5
			// Disable Interactive Cloth components
			InteractiveCloth[] interactiveCloths = FindObjectsOfType (typeof (InteractiveCloth)) as InteractiveCloth[];
			foreach (InteractiveCloth interactiveCloth in interactiveCloths)
			{
				interactiveCloth.enabled = false;
			}
			#endif

			Time.timeScale = 0f;
			AudioListener.pause = true;

			#if !UNITY_5
			foreach (Sound sound in soundsToResume)
			{
				sound.Play ();
			}
			#endif
		}


		/**
		 * Unpauses the game by unfreezing time.
		 */
		public void UnpauseGame (float newScale)
		{
			Time.timeScale = newScale;

			#if !UNITY_5
			// Enable Interactive Cloth components
			InteractiveCloth[] interactiveCloths = FindObjectsOfType (typeof (InteractiveCloth)) as InteractiveCloth[];
			foreach (InteractiveCloth interactiveCloth in interactiveCloths)
			{
				interactiveCloth.enabled = true;
			}
			#endif
		}

		/**
		 * <summary>Gets how much slower vertical movement is compared to horizontal movement, if the game is in 2D.</summary>
		 * <returns>Gets how much slower vertical movement is compared to horizontal movement</returns>
		 */
		public float GetVerticalReductionFactor ()
		{
			if (overrideVerticalReductionFactor)
			{
				return verticalReductionFactor;
			}
			return KickStarter.settingsManager.verticalReductionFactor;
		}


		/**
		 * <summary>Deletes a GameObject once the current frame has finished renderering.</summary>
		 * <param name = "_gameObject">The GameObject to delete</param>
		 */
		public void ScheduleForDeletion (GameObject _gameObject)
		{
			StartCoroutine (ScheduleForDeletionCoroutine (_gameObject));
		}


		private IEnumerator ScheduleForDeletionCoroutine (GameObject _gameObject)
		{
			yield return new WaitForEndOfFrame ();
			DestroyImmediate (_gameObject);
			KickStarter.stateHandler.GatherObjects (true);
		}
		
	}
	
}
