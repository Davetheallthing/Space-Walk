/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2014
 *	
 *	"MainCamera.cs"
 * 
 *	This is attached to the Main Camera, and must be tagged as "MainCamera" to work.
 *	Only one Main Camera should ever exist in the scene.
 *
 *	Shake code adapted from Mike Jasper's code: http://www.mikedoesweb.com/2012/camera-shake-in-unity/
 *
 *  Aspect-ratio code adapated from Eric Haines' code: http://wiki.unity3d.com/index.php?title=AspectRatioEnforcer
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This is attached to the scene's Main Camera, and must be tagged as "MainCamera".
	 * The camera system works by having the MainCamera attach itself to the "active" _Camera component.
	 * Each _Camera component is merely used for reference - only the MainCamera actually performs any rendering.
	 * Shake code adapted from Mike Jasper's code: http://www.mikedoesweb.com/2012/camera-shake-in-unity/
	 * Aspect-ratio code adapated from Eric Haines' code: http://wiki.unity3d.com/index.php?title=AspectRatioEnforcer
	 */
	public class MainCamera : MonoBehaviour
	{

		/** The texture to display fullscreen when fading */
		public Texture2D fadeTexture;
		private Texture2D tempFadeTexture = null;

		/** The current active camera, i.e. the one that the MainCamera is attaching itself to */
		public _Camera attachedCamera;
		/** The last active camera during gameplay */
		public _Camera lastNavCamera;
		/** The last-but-one active camera during gameplay */
		public _Camera lastNavCamera2;

		private bool isSmoothChanging;
		
		private bool isCrossfading;
		private Texture2D crossfadeTexture;
		
		private bool cursorAffectsRotation;
		
		private Vector2 perspectiveOffset = new Vector2 (0f, 0f);
		private Vector2 startPerspectiveOffset = new Vector2 (0f, 0f);
		
		private float timeToFade = 0f;
		private int drawDepth = -1000;
		private float alpha = 0f; 
		private FadeType fadeType;
		private float fadeStartTime;
		private bool hideSceneWhileLoading;
		
		private MoveMethod moveMethod;
		private float changeTime;
		private AnimationCurve timeCurve;
		
		private	Vector3 startPosition;
		private	Quaternion startRotation;
		private float startFOV;
		private float startOrtho;
		private	float startTime;
		private float startFocalDistance;

		/** The object to point towards. Since this object is assumed to be a child, the "LookAt" becomes an offset to regular rotation rather than a replacement */
		public Transform lookAtTransform;

		private Vector2 lookAtAmount;
		private float LookAtZ;
		private Vector3 lookAtTarget;
		
		private Texture2D actualFadeTexture = null;
		private float shakeStartTime;
		private float shakeDuration;
		private float shakeStartIntensity;
		private bool shakeMove;
		private float shakeIntensity;
		private Vector3 shakePosition;
		private Vector3 shakeRotation;
		
		// Aspect ratio
		private Camera borderCam;
		private float borderWidth;
		private MenuOrientation borderOrientation;
		private Rect borderRect1 = new Rect (0f, 0f, 0f, 0f);
		private Rect borderRect2 = new Rect (0f, 0f, 0f, 0f);
		
		// Split-screen
		/** If True, the game window is shared with another _Camera */
		public bool isSplitScreen;
		/** If True, then this Camera takes up the left or top half of a split-screen effect, if isSplitScreen = True */
		public bool isTopLeftSplit;
		/** The orientation of the split-screen divider, if isSplitScreen = True (Horizontal, Vertical) */
		public MenuOrientation splitOrientation;
		/** The _Camera to share the game window with, if isSplitScreen = True */
		public _Camera splitCamera;
		/** The portion of the screen that this Camera takes up, if isSplitScreen = True */
		public float splitAmountMain = 0.49f;
		/** The portion of the screen that splitCamera takes up, if isSplitScreen = True */
		public float splitAmountOther = 0.49f;
		
		// Custom FX
		private float focalDistance = 10f;
		
		private Camera _camera;
		private AudioListener _audioListener;
		
		
		private void Awake ()
		{
			gameObject.tag = Tags.mainCamera;
			
			hideSceneWhileLoading = true;
			
			AssignOwnCamera ();
			
			if (GetComponent <AudioListener>())
			{
				_audioListener = GetComponent <AudioListener>();
			}
			else if (_camera != null && _camera.GetComponent <AudioListener>())
			{
				_audioListener = _camera.GetComponent <AudioListener>();
			}
			
			if (this.transform.parent && this.transform.parent.name != "_Cameras")
			{
				if (GameObject.Find ("_Cameras"))
				{
					this.transform.parent = GameObject.Find ("_Cameras").transform;
				}
				else
				{
					this.transform.parent = null;
				}
			}

			if (KickStarter.settingsManager.forceAspectRatio)
			{
				#if !UNITY_IPHONE
				KickStarter.settingsManager.landscapeModeOnly = false;
				#endif
				if (SetAspectRatio ())
				{
					CreateBorderCamera ();
				}
				SetCameraRect ();
			}
		}
		

		/**
		 * <summary>Initialises lookAtTransform if none exists and assigns fadeTexture.</summary>
		 * <param name = "_fadeTexture">The new fadeTexture to use, if not null</param>
		 */
		public void Initialise (Texture2D _fadeTexture)
		{
			if (lookAtTransform == null)
			{
				GameObject lookAtOb = new GameObject ();
				lookAtOb.name = "LookAt";
				lookAtOb.transform.SetParent (gameObject.transform);
				lookAtOb.transform.localPosition = new Vector3 (0f, 0f, 10f);
				lookAtTransform = lookAtOb.transform;
			}
			
			if (_fadeTexture != null)
			{
				fadeTexture = _fadeTexture;
			}
		}
		
		
		private void Start ()
		{
			if (lookAtTransform)
			{
				lookAtTransform.localPosition = new Vector3 (0f, 0f, 10f);
				LookAtZ = lookAtTransform.localPosition.z;
				LookAtCentre ();
			}
			
			if (KickStarter.settingsManager.movementMethod == MovementMethod.UltimateFPS)
			{
				UltimateFPSIntegration.SetCameraEnabled (true);
			}

			AssignFadeTexture ();
			SetFadeTexture (KickStarter.sceneChanger.GetAndResetTransitionTexture ()); //			
			StartCoroutine ("ShowScene");
		}
		
		
		private IEnumerator ShowScene ()
		{
			yield return new WaitForSeconds (0.1f);
			hideSceneWhileLoading = false;
		}
		

		/**
		 * Pauses the game.
		 */
		public void PauseGame ()
		{
			if (hideSceneWhileLoading)
			{
				//	Debug.Log ("Pause the game");
				//	StartCoroutine ("PauseWhenLoaded");
			}
			else
			{
				KickStarter.stateHandler.gameState = GameState.Paused;
				KickStarter.sceneSettings.PauseGame ();
			}
		}
		
		
		private IEnumerator PauseWhenLoaded ()
		{
			while (hideSceneWhileLoading)
			{
				yield return new WaitForEndOfFrame ();
			}
			KickStarter.stateHandler.gameState = GameState.Paused;
		}
		

		/**
		 * Displays the fadeTexture full-screen for a brief moment while the scene loads.
		 */
		public void HideScene ()
		{
			hideSceneWhileLoading = true;
			StartCoroutine ("ShowScene");
		}
		

		/**
		 * <summary>Shakes the Camera, creating an "earthquake" effect.</summary>
		 * <param name = "_shakeIntensity">The shake intensity</param>
		 * <param name = "_duration">The duration of the effect, in sectonds</param>
		 * <param name = "_shakeMove">If True, the shake will cause the Camera to move as well as rotate</param>
		 */
		public void Shake (float _shakeIntensity, float _duration, bool _shakeMove)
		{
			shakePosition = Vector3.zero;
			shakeRotation = Vector3.zero;
			
			shakeMove = _shakeMove;
			shakeDuration = _duration;
			shakeStartTime = Time.time;
			shakeIntensity = _shakeIntensity;
			
			shakeStartIntensity = shakeIntensity;
		}
		

		/**
		 * <summary>Checks if the Camera is shaking.</summary>
		 * <returns>True if the Camera is shaking</returns>
		 */
		public bool IsShaking ()
		{
			if (shakeIntensity > 0f)
			{
				return true;
			}
			
			return false;
		}
		

		/**
		 * Ends the "earthquake" shake effect.
		 */
		public void StopShaking ()
		{
			shakeIntensity = 0f;
			shakePosition = Vector3.zero;
			shakeRotation = Vector3.zero;
		}
		

		/**
		 * Prepares the Camera for being able to render a BackgroundImage underneath scene objects.
		 */
		public void PrepareForBackground ()
		{
			AssignOwnCamera ();
			_camera.clearFlags = CameraClearFlags.Depth;
			
			if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) != -1)
			{
				_camera.cullingMask = ~(1 << LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer));
			}
		}
		
		
		private void RemoveBackground ()
		{
			AssignOwnCamera ();			
			_camera.clearFlags = CameraClearFlags.Skybox;
			
			if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) != -1)
			{
				_camera.cullingMask = ~(1 << LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer));
			}
		}
		

		/**
		 * Activates the FirstPersonCamera found in the Player prefab.
		 */
		public void SetFirstPerson ()
		{
			FirstPersonCamera firstPersonCamera = KickStarter.player.GetComponentInChildren<FirstPersonCamera>();
			if (firstPersonCamera)
			{
				SetGameCamera (firstPersonCamera);
			}
			
			if (attachedCamera)
			{
				if (lastNavCamera != attachedCamera)
				{
					lastNavCamera2 = lastNavCamera;
				}
				
				lastNavCamera = attachedCamera;
			}
		}
		

		/**
		 * Draws the Camera's fade texture.
		 * This is called every OnGUI call by StateHandler.
		 */
		public void DrawCameraFade ()
		{
			if (timeToFade > 0f)
			{
				alpha = (Time.time - fadeStartTime) / timeToFade;
				
				if (fadeType == FadeType.fadeIn)
				{
					alpha = 1f - alpha;
				}
				
				alpha = Mathf.Clamp01 (alpha);
				
				if (Time.time > (fadeStartTime + timeToFade))
				{
					if (fadeType == FadeType.fadeIn)
					{
						alpha = 0f;
					}
					else
					{
						alpha = 1f;
					}
					
					timeToFade = 0f;
					StopCrossfade ();
				}
			}
			
			if (hideSceneWhileLoading && actualFadeTexture != null)
			{
				GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), actualFadeTexture);
			}
			else if (alpha > 0f)
			{
				Color tempColor = GUI.color;
				tempColor.a = alpha;
				GUI.color = tempColor;
				GUI.depth = drawDepth;
				
				if (isCrossfading)
				{
					GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), crossfadeTexture);
				}
				else
				{
					GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), actualFadeTexture);
				}
			}
			else if (actualFadeTexture != fadeTexture)
			{
				ReleaseFadeTexture ();
			}
		}
		

		/**
		 * Resets the Camera's projection matrix.
		 */
		public void ResetProjection ()
		{
			if (_camera)
			{
				perspectiveOffset = Vector2.zero;
				_camera.projectionMatrix = AdvGame.SetVanishingPoint (_camera, perspectiveOffset);
				_camera.ResetProjectionMatrix ();
			}
		}
		

		/**
		 * Resets the transition effect when moving from one _Camera to another.
		 */
		public void ResetMoving ()
		{
			isSmoothChanging = false;
			startTime = 0f;
			changeTime = 0f;
		}
		

		/**
		 * Updates the Camera's position.
		 * This is called every frame by StateHandler.
		 */
		public void _LateUpdate ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}
			
			if (KickStarter.stateHandler.gameState == GameState.Normal)
			{
				if (attachedCamera)
				{
					if (lastNavCamera != attachedCamera)
					{
						lastNavCamera2 = lastNavCamera;
					}
					
					lastNavCamera = attachedCamera;
				}
			}
			
			if (attachedCamera && (!(attachedCamera is GameCamera25D)))
			{
				if (!isSmoothChanging)
				{
					transform.rotation = attachedCamera.transform.rotation;
					transform.position = attachedCamera.transform.position;
					focalDistance = attachedCamera.focalDistance;
					
					if (attachedCamera is GameCamera2D)
					{
						GameCamera2D cam2D = (GameCamera2D) attachedCamera;
						perspectiveOffset = cam2D.GetPerspectiveOffset ();
						if (!_camera.orthographic)
						{
							_camera.projectionMatrix = AdvGame.SetVanishingPoint (_camera, perspectiveOffset);
						}
						else
						{
							_camera.orthographicSize = attachedCamera._camera.orthographicSize;
						}
					}
					
					else
					{
						_camera.fieldOfView = attachedCamera._camera.fieldOfView;
						if (cursorAffectsRotation)
						{
							SetlookAtTransformation ();
							transform.LookAt (lookAtTransform, attachedCamera.transform.up);
						}
					}
				}
				else
				{
					// Move from one GameCamera to another
					if (Time.time < startTime + changeTime)
					{
						if (attachedCamera is GameCamera2D)
						{
							GameCamera2D cam2D = (GameCamera2D) attachedCamera;
							
							perspectiveOffset.x = AdvGame.Lerp (startPerspectiveOffset.x, cam2D.GetPerspectiveOffset ().x, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
							perspectiveOffset.y = AdvGame.Lerp (startPerspectiveOffset.y, cam2D.GetPerspectiveOffset ().y, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
							
							_camera.ResetProjectionMatrix ();
						}
						
						if (moveMethod == MoveMethod.Curved)
						{
							// Don't slerp y position as this will create a "bump" effect
							Vector3 newPosition = Vector3.Slerp (startPosition, attachedCamera.transform.position, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
							newPosition.y = Mathf.Lerp (startPosition.y, attachedCamera.transform.position.y, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
							transform.position = newPosition;
							
							transform.rotation = Quaternion.Slerp (startRotation, attachedCamera.transform.rotation, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
						}
						else
						{
							transform.position = AdvGame.Lerp (startPosition, attachedCamera.transform.position, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve)); 
							transform.rotation = AdvGame.Lerp (startRotation, attachedCamera.transform.rotation, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
						}
						
						focalDistance = AdvGame.Lerp (startFocalDistance, attachedCamera.focalDistance, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
						_camera.fieldOfView = AdvGame.Lerp (startFOV, attachedCamera._camera.fieldOfView, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
						_camera.orthographicSize = AdvGame.Lerp (startOrtho, attachedCamera._camera.orthographicSize, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
						
						if (attachedCamera is GameCamera2D && !_camera.orthographic)
						{
							_camera.projectionMatrix = AdvGame.SetVanishingPoint (_camera, perspectiveOffset);
						}
					}
					else
					{
						LookAtCentre ();
						isSmoothChanging = false;
					}
				}
				
				if (cursorAffectsRotation && lookAtTransform != null)
				{
					lookAtTransform.localPosition = Vector3.Lerp (lookAtTransform.localPosition, lookAtTarget, Time.deltaTime * 3f);	
				}
			}
			
			else if (attachedCamera && (attachedCamera is GameCamera25D))
			{
				transform.position = attachedCamera.transform.position;
				transform.rotation = attachedCamera.transform.rotation;
			}
			
			// Shake
			if (shakeIntensity > 0f)
			{
				if (shakeMove)
				{
					shakePosition = Random.insideUnitSphere * shakeIntensity * 0.5f;
				}
				
				shakeRotation = new Vector3
					(
						Random.Range (-shakeIntensity, shakeIntensity) * 0.2f,
						Random.Range (-shakeIntensity, shakeIntensity) * 0.2f,
						Random.Range (-shakeIntensity, shakeIntensity) * 0.2f
						);
				
				shakeIntensity = Mathf.Lerp (shakeStartIntensity, 0f, AdvGame.Interpolate (shakeStartTime, shakeDuration, MoveMethod.Linear, null));
				
				transform.position += shakePosition;
				transform.localEulerAngles += shakeRotation;
			}
			else if (shakeIntensity < 0f)
			{
				StopShaking ();
			}
		}
		
		
		private void LookAtCentre ()
		{
			if (lookAtTransform)
			{
				lookAtTarget = new Vector3 (0, 0, LookAtZ);
			}
		}
		
		
		private void SetlookAtTransformation ()
		{
			if (KickStarter.stateHandler.gameState == GameState.Normal)
			{
				Vector2 mousePosition = KickStarter.playerInput.GetMousePosition ();
				Vector2 mouseOffset = new Vector2 (mousePosition.x / (Screen.width / 2) - 1, mousePosition.y / (Screen.height / 2) - 1);
				float distFromCentre = mouseOffset.magnitude;
				
				if (distFromCentre < 1.4f)
				{
					lookAtTarget = new Vector3 (mouseOffset.x * lookAtAmount.x, mouseOffset.y * lookAtAmount.y, LookAtZ);
				}
			}
		}
		

		/**
		 * Snaps the Camera to the attachedCamera instantly.
		 */
		public void SnapToAttached ()
		{
			if (attachedCamera && attachedCamera._camera)
			{
				LookAtCentre ();
				isSmoothChanging = false;
				
				_camera.orthographic = attachedCamera._camera.orthographic;
				_camera.fieldOfView = attachedCamera._camera.fieldOfView;
				_camera.orthographicSize = attachedCamera._camera.orthographicSize;
				transform.position = attachedCamera.transform.position;
				transform.rotation = attachedCamera.transform.rotation;
				focalDistance = attachedCamera.focalDistance;
				
				if (attachedCamera is GameCamera2D)
				{
					GameCamera2D cam2D = (GameCamera2D) attachedCamera;
					perspectiveOffset = cam2D.GetPerspectiveOffset ();
				}
				else
				{
					perspectiveOffset = new Vector2 (0f, 0f);
				}
				
				if (KickStarter.stateHandler.gameState == GameState.Normal && KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.settingsManager.directMovementType == DirectMovementType.RelativeToCamera && KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen)
				{
					if (KickStarter.player.GetPath () == null || !KickStarter.player.IsLockedToPath ())
					{
						KickStarter.playerInput.cameraLockSnap = true;
					}
				}
			}
		}
		

		/**
		 * <summary>Crossfades to a new _Camera over time.</summary>
		 * <param name = "_changeTime">The duration, in seconds, of the crossfade</param>
		 * <param name = "_linkedCamera">The _Camera to crossfade to</param>
		 */
		public void Crossfade (float _changeTime, _Camera _linkedCamera)
		{
			object[] parms = new object[2] { _changeTime, _linkedCamera};
			StartCoroutine ("StartCrossfade", parms);
		}
		

		/**
		 * Instantly ends the crossfade effect.
		 */
		public void StopCrossfade ()
		{
			StopCoroutine ("StartCrossfade");
			if (isCrossfading)
			{
				isCrossfading = false;
				alpha = 0f;
			}
			DestroyObject (crossfadeTexture);
			crossfadeTexture = null;
		}
		
		
		private IEnumerator StartCrossfade (object[] parms)
		{
			float _changeTime = (float) parms[0];
			_Camera _linkedCamera = (_Camera) parms[1];
			
			yield return new WaitForEndOfFrame ();
			
			crossfadeTexture = new Texture2D (Screen.width, Screen.height, TextureFormat.RGB24, false);
			crossfadeTexture.ReadPixels (new Rect (0f, 0f, Screen.width, Screen.height), 0, 0, false);
			crossfadeTexture.Apply ();
			
			isSmoothChanging = false;
			isCrossfading = true;
			SetGameCamera (_linkedCamera);
			FadeOut (0f);
			FadeIn (_changeTime);
		}
		

		/**
		 * Places a full-screen texture of the current game window over the screen, allowing for a scene change to have no visible transition.
		 */
		public void _ExitSceneWithOverlay ()
		{
			StartCoroutine ("ExitSceneWithOverlay");
		}
		
		
		private IEnumerator ExitSceneWithOverlay ()
		{
			yield return new WaitForEndOfFrame ();
			Texture2D screenTex = new Texture2D (Screen.width, Screen.height, TextureFormat.RGB24, false);
			screenTex.ReadPixels (new Rect (0f, 0f, Screen.width, Screen.height), 0, 0, false);
			screenTex.Apply ();
			SetFadeTexture (screenTex);
			KickStarter.sceneChanger.SetTransitionTexture (screenTex);
			FadeOut (0f);
		}
		
		
		private void SmoothChange (float _changeTime, MoveMethod method, AnimationCurve _timeCurve = null)
		{
			LookAtCentre ();
			moveMethod = method;
			isSmoothChanging = true;
			StopCrossfade ();
			
			startTime = Time.time;
			changeTime = _changeTime;
			
			startPosition = transform.position;
			startRotation = transform.rotation;
			startFOV = _camera.fieldOfView;
			startOrtho = _camera.orthographicSize;
			startFocalDistance = focalDistance;
			
			startPerspectiveOffset = perspectiveOffset;
			
			if (method == MoveMethod.CustomCurve)
			{
				timeCurve = _timeCurve;
			}
			else
			{
				timeCurve = null;
			}
		}
		

		/**
		 * <summary>Sets a _Camera as the new attachedCamera to follow.</summary>
		 * <param name = "newCamera">The new _Camera to follow</param>
		 * <param name = "transitionTime">The time, in seconds, that it will take to move towards the new _Camera</param>
		 * <param name = "_moveMethod">How the Camera should move towards the new _Camera, if transitionTime > 0f (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_animationCurve">The AnimationCurve that dictates movement over time, if _moveMethod = MoveMethod.CustomCurve</param>
		 */
		public void SetGameCamera (_Camera newCamera, float transitionTime = 0f, MoveMethod _moveMethod = MoveMethod.Linear, AnimationCurve _animationCurve = null)
		{
			if (newCamera == null)
			{
				return;
			}
			
			if (attachedCamera != null && attachedCamera is GameCamera25D)
			{
				if (newCamera is GameCamera25D)
				{ }
				else
				{
					RemoveBackground ();
				}
			}
			
			AssignOwnCamera ();
			_camera.ResetProjectionMatrix ();
			attachedCamera = newCamera;
			attachedCamera.SetCameraComponent ();
			
			if (attachedCamera && attachedCamera._camera)
			{
				_camera.farClipPlane = attachedCamera._camera.farClipPlane;
				_camera.nearClipPlane = attachedCamera._camera.nearClipPlane;
				_camera.orthographic = attachedCamera._camera.orthographic;
			}
			
			// Set LookAt
			if (attachedCamera is GameCamera)
			{
				GameCamera gameCam = (GameCamera) attachedCamera;
				cursorAffectsRotation = gameCam.followCursor;
				lookAtAmount = gameCam.cursorInfluence;
			}
			else if (attachedCamera is GameCameraAnimated)
			{
				GameCameraAnimated gameCam = (GameCameraAnimated) attachedCamera;
				if (gameCam.animatedCameraType == AnimatedCameraType.SyncWithTargetMovement)
				{
					cursorAffectsRotation = gameCam.followCursor;
					lookAtAmount = gameCam.cursorInfluence;
				}
				else
				{
					cursorAffectsRotation = false;
				}
			}
			else
			{
				cursorAffectsRotation = false;
			}
			
			// Set background
			if (attachedCamera is GameCamera25D)
			{
				GameCamera25D cam25D = (GameCamera25D) attachedCamera;
				cam25D.SetActiveBackground ();
			}
			
			// TransparencySortMode
			if (attachedCamera is GameCamera2D)
			{
				_camera.transparencySortMode = TransparencySortMode.Orthographic;
			}
			else if (attachedCamera)
			{
				if (attachedCamera._camera.orthographic)
				{
					_camera.transparencySortMode = TransparencySortMode.Orthographic;
				}
				else
				{
					_camera.transparencySortMode = TransparencySortMode.Perspective;
				}
			}
			
			// UFPS
			if (KickStarter.settingsManager.movementMethod == MovementMethod.UltimateFPS)
			{
				UltimateFPSIntegration._Update (KickStarter.stateHandler.gameState);
			}
			
			KickStarter.stateHandler.LimitHotspotsToCamera (attachedCamera);
			
			if (transitionTime > 0f)
			{
				SmoothChange (transitionTime, _moveMethod, _animationCurve);
			}
			else if (attachedCamera != null)
			{
				attachedCamera.MoveCameraInstant ();
				SnapToAttached ();
			}
		}
		

		private void SetFadeTexture (Texture2D tex)
		{
			if (tex != null)
			{
				tempFadeTexture = tex;
			}
			AssignFadeTexture ();
		}
		

		private void ReleaseFadeTexture ()
		{
			tempFadeTexture = null;
			AssignFadeTexture ();
		}
		
		
		private void AssignFadeTexture ()
		{
			if (tempFadeTexture != null)
			{
				actualFadeTexture = tempFadeTexture;
			}
			else
			{
				actualFadeTexture = fadeTexture;
			}
		}
		

		/**
		 * <summary>Fades the camera out with a custom texture.</summary>
		 * <param name = "_timeToFade">The duration, in seconds, of the fade effect</param>
		 * <param name = "tempTex">The texture to display full-screen</param>
		 */
		public void FadeOut (float _timeToFade, Texture2D tempTex)
		{
			if (tempTex != null)
			{
				SetFadeTexture (tempTex);
			}
			FadeOut (_timeToFade);
		}
		

		/**
		 * <summary>Fades the camera in.</summary>
		 * <param name = "_timeToFade">The duration, in seconds, of the fade effect</param>
		 */
		public void FadeIn (float _timeToFade)
		{
			AssignFadeTexture ();
			
			if (_timeToFade > 0f)
			{
				timeToFade = _timeToFade;
				alpha = 1f;
				fadeType = FadeType.fadeIn;
				fadeStartTime = Time.time;
			}
			else
			{
				alpha = 0f;
				timeToFade = 0f;
				ReleaseFadeTexture ();
			}
		}
		

		/**
		 * <summary>Fades the camera out.</summary>
		 * <param name = "_timeToFade">The duration, in seconds, of the fade effect</param>
		 */
		public void FadeOut (float _timeToFade)
		{
			AssignFadeTexture ();
			
			if (alpha == 0f)
			{
				alpha = 0.01f;
			}
			if (_timeToFade > 0f)
			{
				alpha = Mathf.Clamp01 (alpha);
				timeToFade = _timeToFade;
				fadeType = FadeType.fadeOut;
				fadeStartTime = Time.time - (alpha * timeToFade);
			}
			else
			{
				alpha = 1f;
				timeToFade = 0f;
			}
		}
		

		/**
		 * <summary>Checks if the Camera is fading in our out.</summary>
		 * <returns>True if the Camera is fading in or out</returns>
		 */
		public bool isFading ()
		{
			if (fadeType == FadeType.fadeOut && alpha < 1f)
			{
				return true;
			}
			else if (fadeType == FadeType.fadeIn && alpha > 0f)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Converts a point in world space to one relative to the Camera's forward vector.</summary>
		 * <returns>Converts a point in world space to one relative to the Camera's forward vector.</returns>
		 */
		public Vector3 PositionRelativeToCamera (Vector3 _position)
		{
			return (_position.x * ForwardVector ()) + (_position.z * RightVector ());
		}
		

		/*
		 * <summary>Gets the Camera's right vector.</summary>
		 * <returns>The Camera's right vector</returns>
		 */
		public Vector3 RightVector ()
		{
			return (transform.right);
		}
		

		/*
		 * <summary>Gets the Camera's forward vector, not accounting for pitch.</summary>
		 * <returns>The Camera's forward vector, not accounting for pitch</returns>
		 */
		public Vector3 ForwardVector ()
		{
			Vector3 camForward;
			
			camForward = transform.forward;
			camForward.y = 0;
			
			return (camForward);
		}
		
		
		private bool SetAspectRatio ()
		{
			float currentAspectRatio = 0f;
			
			if (Screen.orientation == ScreenOrientation.LandscapeRight || Screen.orientation == ScreenOrientation.LandscapeLeft)
			{
				currentAspectRatio = (float) Screen.width / Screen.height;
			}
			else
			{
				if (Screen.height > Screen.width && KickStarter.settingsManager.landscapeModeOnly)
				{
					currentAspectRatio = (float) Screen.height / Screen.width;
				}
				else
				{
					currentAspectRatio = (float) Screen.width / Screen.height;
				}
			}
			
			// If the current aspect ratio is already approximately equal to the desired aspect ratio, use a full-screen Rect (in case it was set to something else previously)
			if (!KickStarter.settingsManager.forceAspectRatio || (int) (currentAspectRatio * 100) / 100f == (int) (KickStarter.settingsManager.wantedAspectRatio * 100) / 100f)
			{
				borderWidth = 0f;
				borderOrientation = MenuOrientation.Horizontal;
				
				if (borderCam) 
				{
					Destroy (borderCam.gameObject);
				}
				return false;
			}
			
			// Pillarbox
			if (currentAspectRatio > KickStarter.settingsManager.wantedAspectRatio)
			{
				borderWidth = 1f - KickStarter.settingsManager.wantedAspectRatio / currentAspectRatio;
				borderWidth /= 2f;
				borderOrientation = MenuOrientation.Vertical;
				
				borderRect1 = new Rect (0, 0, borderWidth * Screen.width, Screen.height);
				borderRect2 = new Rect (Screen.width * (1f - borderWidth), 0f, Screen.width * borderWidth, Screen.height);
			}
			// Letterbox
			else
			{
				borderWidth = 1f - currentAspectRatio / KickStarter.settingsManager.wantedAspectRatio;
				borderWidth /= 2f;
				borderOrientation = MenuOrientation.Horizontal;
				
				borderRect1 = new Rect (0, 0, Screen.width, borderWidth * Screen.height);
				borderRect2 = new Rect (0, Screen.height * (1f - borderWidth), Screen.width, Screen.width * borderWidth);
			}
			
			
			return true;
		}
		

		/**
		 * Updates the camera's rect values according to the aspect ratio and split-screen settings.
		 */
		public void SetCameraRect ()
		{
			if (SetAspectRatio () && Application.isPlaying)
			{
				CreateBorderCamera ();
			}
			
			if (isSplitScreen)
			{
				_camera.rect = GetSplitScreenRect (true);
			}
			else
			{
				if (borderOrientation == MenuOrientation.Vertical)
				{
					_camera.rect = new Rect (borderWidth, 0f, 1f - (2*borderWidth), 1f);
				}
				else if (borderOrientation == MenuOrientation.Horizontal)
				{
					_camera.rect = new Rect (0f, borderWidth, 1f, 1f - (2*borderWidth));
				}
			}
			
			BackgroundCamera backgroundCamera = FindObjectOfType (typeof (BackgroundCamera)) as BackgroundCamera;
			if (backgroundCamera)
			{
				backgroundCamera.UpdateRect ();
			}
		}
		

		/**
		 * Draws any borders generated by a fixed aspect ratio, as set with forceAspectRatio in SettingsManager.
		 * This will be called every OnGUI call by StateHandler.
		 */
		public void DrawBorders ()
		{
			if (borderWidth != 0f)
			{
				GUI.depth = 10;
				
				GUI.DrawTexture (borderRect1, fadeTexture);
				GUI.DrawTexture (borderRect2, fadeTexture);
			}
		}
		

		/**
		 * <summary>Checks if the Camera uses orthographic perspective.</summary>
		 * <returns>True if the Camera uses orthographic perspective</returns>
		 */
		public bool IsOrthographic ()
		{
			if (_camera == null)
			{
				return false;
			}
			return _camera.orthographic;
		}
		
		
		private void CreateBorderCamera ()
		{
			if (!borderCam)
			{
				// Make a new camera behind the normal camera which displays black; otherwise the unused space is undefined
				borderCam = new GameObject ("BorderCamera", typeof (Camera)).GetComponent <Camera>();
				borderCam.transform.parent = this.transform;
				borderCam.depth = int.MinValue;
				borderCam.clearFlags = CameraClearFlags.SolidColor;
				borderCam.backgroundColor = Color.black;
				borderCam.cullingMask = 0;
			}
		}
		

		/**
		 * <summary>Limits a point in screen-space to stay within the Camera's rect boundary, if forceAspectRatio in SettingsManager = True.</summary>
		 * <param name = "position">The original position in screen-space</param>
		 * <returns>The point, repositioned to stay within the Camera's rect boundary</returns>
		 */
		public Vector2 LimitToAspect (Vector2 position)
		{
			if (KickStarter.settingsManager == null || !KickStarter.settingsManager.forceAspectRatio)
			{
				return position;
			}
			
			if (borderOrientation == MenuOrientation.Horizontal)
			{
				// Letterbox
				int yOffset = (int) (Screen.height * borderWidth);
				
				if (position.y < yOffset)
				{
					position.y = yOffset;
				}
				else if (position.y > (Screen.height - yOffset))
				{
					position.y = Screen.height - yOffset;
				}
			}
			else
			{
				// Pillarbox
				int xOffset = (int) (Screen.width * borderWidth);
				
				if (position.x < xOffset)
				{
					position.x = xOffset;
				}
				else if (position.x > (Screen.width - xOffset))
				{
					position.x = Screen.width - xOffset;
				}
			}
			
			return position;
		}
		

		/**
		 * <summary>Checks if a point in screen-space is within the Camera's viewport</summary>
		 * <param name = "point">The point to check the position of</param>
		 * <returns>True if the point is within the Camera's viewport</returns>
		 */
		public bool IsPointInCamera (Vector2 point)
		{
			if (!isSplitScreen)
			{
				return true;
			}
			point = new Vector2 (point.x / Screen.width, point.y / Screen.height);
			return _camera.rect.Contains (point);
		}
		

		/**
		 * <summary>Resizes an OnGUI Rect so that it fits within the Camera's rect, if forceAspectRatio = True in SettingsManager.</summary>
		 * <param name = "rect">The OnGUI Rect to resize</param>
		 * <returns>The resized OnGUI Rect</returns>
		 */
		public Rect LimitMenuToAspect (Rect rect)
		{
			if (KickStarter.settingsManager == null || !KickStarter.settingsManager.forceAspectRatio)
			{
				return rect;
			}
			
			if (borderOrientation == MenuOrientation.Horizontal)
			{
				// Letterbox
				int yOffset = (int) (Screen.height * borderWidth);
				
				if (rect.y < yOffset)
				{
					rect.y = yOffset;
					
					if (rect.height > (Screen.height - yOffset - yOffset))
					{
						rect.height = Screen.height - yOffset - yOffset;
					}
				}
				else if (rect.y + rect.height > (Screen.height - yOffset))
				{
					rect.y = Screen.height - yOffset - rect.height;
				}
			}
			else
			{
				// Pillarbox
				int xOffset = (int) (Screen.width * borderWidth);
				
				if (rect.x < xOffset)
				{
					rect.x = xOffset;
					
					if (rect.width > (Screen.width - xOffset - xOffset))
					{
						rect.width = Screen.width - xOffset - xOffset;
					}
				}
				else if (rect.x + rect.width > (Screen.width - xOffset))
				{
					rect.x = Screen.width - xOffset - rect.width;
				}
			}
			
			return rect;
		}
		

		/**
		 * <summary>Creates a new split-screen effect.</summary>
		 * <param name = "_camera1">The first _Camera to use in the effect</param>
		 * <param name = "_camera2">The second _Camera to use in the effect</param>
		 * <param name = "_splitOrientation">How the two _Cameras are arranged (Horizontal, Vertical)</param>
		 * <param name = "_isTopLeft">If True, the MainCamera will take the position of _camera1</param>
		 * <param name = "_splitAmountMain">The proportion of the screen taken up by this Camera</param>
		 * <param name = "_splitAmountOther">The proportion of the screen take up by the other _Camera</param>
		 */
		public void SetSplitScreen (_Camera _camera1, _Camera _camera2, MenuOrientation _splitOrientation, bool _isTopLeft, float _splitAmountMain, float _splitAmountOther)
		{
			splitCamera = _camera2;
			isSplitScreen = true;
			splitOrientation = _splitOrientation;
			isTopLeftSplit = _isTopLeft;
			
			SetGameCamera (_camera1);
			StartSplitScreen (_splitAmountMain, _splitAmountOther);
		}
		

		/**
		 * <summary>Adjusts the screen ratio of any active split-screen effect.</summary>
		 * <param name = "_splitAmountMain">The proportion of the screen taken up by this Camera</param>
		 * <param name = "_splitAmountOther">The proportion of the screen take up by the other _Camera</param>
		 */
		public void StartSplitScreen (float _splitAmountMain, float _splitAmountOther)
		{
			splitAmountMain = _splitAmountMain;
			splitAmountOther = _splitAmountOther;
			
			splitCamera.SetSplitScreen ();
			SetCameraRect ();
		}


		/**
		 * Ends any active split-screen effect.
		 */
		public void RemoveSplitScreen ()
		{
			isSplitScreen = false;
			SetCameraRect ();
			
			if (splitCamera)
			{
				splitCamera.RemoveSplitScreen ();
				splitCamera = null;
			}
		}


		/**
		 * <summary>Gets a screen Rect of the split-screen camera.</summary>
		 * <param name = "isMainCamera">If True, then the Rect of the MainCamera's view will be returned. Otherwise, the Rect of the other split-screen _Camera's view will be returned</param>
		 * <returns>A screen Rect of the split-screen camera</returns>
		 */
		public Rect GetSplitScreenRect (bool isMainCamera)
		{
			bool _isTopLeftSplit = isTopLeftSplit;
			float split = splitAmountMain;
			
			if (!isMainCamera)
			{
				_isTopLeftSplit = !isTopLeftSplit;
				split = splitAmountOther;
			}
			
			// Pillarbox
			if (borderOrientation == MenuOrientation.Vertical)
			{
				if (splitOrientation == MenuOrientation.Horizontal)
				{
					if (!_isTopLeftSplit)
					{
						return new Rect (borderWidth, 0f, 1f - (2*borderWidth), split);
					}
					else
					{
						return new Rect (borderWidth, 1f - split, 1f - (2*borderWidth), split);
					}
				}
				else
				{
					if (_isTopLeftSplit)
					{
						return new Rect (borderWidth, 0f, split - borderWidth, 1f);
					}
					else
					{
						return new Rect (1f - split, 0f, split - borderWidth, 1f);
					}
				}
			}
			// Letterbox
			else
			{
				if (splitOrientation == MenuOrientation.Horizontal)
				{
					if (_isTopLeftSplit)
					{
						return new Rect (0f, 1f - split, 1f, split - borderWidth);
					}
					else
					{
						return new Rect (0f, borderWidth, 1f, split - borderWidth);
					}
				}
				else
				{
					if (_isTopLeftSplit)
					{
						return new Rect (0f, borderWidth, split, 1f - (2*borderWidth));
					}
					else
					{
						return new Rect (1f - split, borderWidth, split, 1f - (2*borderWidth));
					}
				}
			}
		}


		/**
		 * <summary>Gets the current focal distance.</summary>
		 * <returns>The current focal distance</returns>
		 */
		public float GetFocalDistance ()
		{
			return focalDistance;
		}


		private void AssignOwnCamera ()
		{
			if (_camera == null)
			{
				if (GetComponent <Camera>())
				{
					_camera = GetComponent <Camera>();
				}
				else if (GetComponentInChildren <Camera>())
				{
					_camera = GetComponentInChildren <Camera>();
				}
				else
				{
					Debug.LogError ("The MainCamera script requires a Camera component.");
					return;
				}
			}
		}
		
		
		private void OnDestroy ()
		{
			crossfadeTexture = null;
		}
		

		/**
		 * Disables the Camera and AudioListener.
		 */
		public void Disable ()
		{
			if (_camera)
			{
				_camera.enabled = false;
			}
			if (_audioListener)
			{
				_audioListener.enabled = false;
			}
		}
		

		/**
		 * Enables the Camera and AudioListener.
		 */
		public void Enable ()
		{
			if (_camera)
			{
				_camera.enabled = true;
			}
			if (_audioListener)
			{
				_audioListener.enabled = true;
			}
		}
		

		/**
		 * <summary>Sets the GameObject's tag.</summary>
		 * <param name = "_tag">The tag to give the GameObject</param>
		 */
		public void SetCameraTag (string _tag)
		{
			if (_camera != null)
			{
				_camera.gameObject.tag = _tag;
			}
		}
		

		/**
		 * <summary>Sets the state of the AudioListener component.</summary>
		 * <param name = "state">If True, the AudioListener will be enabled. If False, it will be disabled.</param>
		 */
		public void SetAudioState (bool state)
		{
			if (_audioListener)
			{
				_audioListener.enabled = state;
			}
		}


		/**
		 * <summary>Gets the previously-used gameplay _Camera.</summary>
		 * <returns>The previously-used gameplay _Camera</returns>
		 */
		public _Camera GetLastGameplayCamera ()
		{
			if (lastNavCamera != null)
			{
				if (lastNavCamera2 != null && attachedCamera == lastNavCamera)
				{
					return (_Camera) lastNavCamera2;
				}
				else
				{
					return (_Camera) lastNavCamera;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the current perspective offset, as set by a GameCamera2D.</summary>
		 * <returns>The current perspective offset, as set by a GameCamera2D.</returns>
		 */
		public Vector2 GetPerspectiveOffset ()
		{
			return perspectiveOffset;
		}
		
		
	}
	
}