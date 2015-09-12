﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2014
 *	
 *	"Dialog.cs"
 * 
 *	This class processes any dialogue line.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * A container class for an active line of dialogue.
	 */
	public class Speech
	{

		/** The line's SpeechLog entry */
		public SpeechLog log;
		/** The display text */
		public string displayText;
		/** True if the line should play in the backround, and not interrupt Actions or gameplay. */
		public bool isBackground;
		/** How long the line should be active for */
		public float displayDuration;
		/** True if the line is active */
		public bool isAlive;
		/** If True, the speech line has an AudioClip supplied */
		public bool hasAudio;
		/** If True, then the Action that ran this speech has ended, but the speech line is still active */
		public bool continueFromSpeech = false;
		
		private int gapIndex = -1;
		private int continueIndex = -1;
		private List<SpeechGap> speechGaps = new List<SpeechGap>();
		private float endTime;
		private float continueTime;

		private AC.Char speaker;
		private bool isSkippable;
		private bool pauseGap;

		private float scrollAmount = 0f;
		private float pauseEndTime = 0f;



		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_speaker">The speaking character. If null, the line is considered a narration</param>
		 * <param name = "_message">The subtitle text to display</param>
		 * <param name = "lineID">The unique ID number of the line, as generated by the Speech Manager</param>
		 * <param name = "_language">The currently-selected language</param>
		 * <param name = "_isBackground">True if the line should play in the background, and not interrupt Actions or gameplay</param>
		 * <param name = "_noAnimation">True if the speaking character should not play a talking animation</param>
		 */
		public Speech (Char _speaker, string _message, int lineID, string _language, bool _isBackground, bool _noAnimation)
		{
			log.Clear ();
			isBackground = _isBackground;
			
			if (_speaker)
			{
				speaker = _speaker;
				speaker.isTalking = !_noAnimation;
				log.speakerName = _speaker.name;
				
				if (_speaker.GetComponent <Player>())
				{
					if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow || !KickStarter.speechManager.usePlayerRealName)
					{
						log.speakerName = "Player";
					}
				}
				
				if (_speaker.GetComponent <Hotspot>())
				{
					if (_speaker.GetComponent <Hotspot>().hotspotName != "")
					{
						log.speakerName = _speaker.GetComponent <Hotspot>().hotspotName;
					}
				}

				_speaker.ClearExpression ();

				if (!_noAnimation)
				{
					if (KickStarter.speechManager.lipSyncMode == LipSyncMode.Off)
					{
						speaker.isLipSyncing = false;
					}
					else if (KickStarter.speechManager.lipSyncMode == LipSyncMode.Salsa2D || KickStarter.speechManager.lipSyncMode == LipSyncMode.FromSpeechText || KickStarter.speechManager.lipSyncMode == LipSyncMode.ReadPamelaFile || KickStarter.speechManager.lipSyncMode == LipSyncMode.ReadSapiFile || KickStarter.speechManager.lipSyncMode == LipSyncMode.ReadPapagayoFile)
					{
						speaker.StartLipSync (KickStarter.dialog.GenerateLipSyncShapes (KickStarter.speechManager.lipSyncMode, lineID, log.speakerName, _language, _message));
					}
				}
			}
			else
			{
				if (speaker)
				{
					speaker.isTalking = false;
				}
				speaker = null;			
				log.speakerName = "Narrator";
			}
			
			_message = AdvGame.ConvertTokens (_message);
			_message = DetermineGaps (_message);
			if (speechGaps.Count > 0)
			{
				gapIndex = 0;
				foreach (SpeechGap gap in speechGaps)
				{
					if (gap.expressionID < 0)
					{
						displayDuration += (float) gap.waitTime;
					}
				}
			}
			else
			{
				gapIndex = -1;
			}

			if (lineID > -1)
			{
				log.lineID = lineID;
			}

			// Play sound and time displayDuration to it
			if (lineID > -1 && log.speakerName != "" && KickStarter.speechManager.searchAudioFiles)
			{
				string fullFilename = "Speech/";
				string filename = KickStarter.speechManager.GetLineFilename (lineID);
				if (_language != "" && KickStarter.speechManager.translateAudio)
				{
					// Not in original language
					fullFilename += _language + "/";
				}
				if (KickStarter.speechManager.placeAudioInSubfolders)
				{
					fullFilename += filename + "/";
				}
				fullFilename += filename + lineID;
				
				AudioClip clipObj = Resources.Load (fullFilename) as AudioClip;
				if (clipObj)
				{
					AudioSource audioSource = null;

					if (_speaker != null)
					{
						if (!_noAnimation)
						{
							if (KickStarter.speechManager.lipSyncMode == LipSyncMode.FaceFX)
							{
								FaceFXIntegration.Play (speaker, log.speakerName + lineID, clipObj);
							}
						}
						
						if (_speaker.GetComponent <AudioSource>())
						{
							_speaker.GetComponent <AudioSource>().volume = Options.optionsData.speechVolume;
							audioSource = _speaker.GetComponent <AudioSource>();
						}
						else
						{
							Debug.LogWarning (_speaker.name + " has no audio source component!");
						}
					}
					else if (KickStarter.player && KickStarter.player.GetComponent <AudioSource>())
					{
						KickStarter.player.GetComponent <AudioSource>().volume = Options.optionsData.speechVolume;
						audioSource = KickStarter.player.GetComponent <AudioSource>();
					}
					else
					{
						audioSource = KickStarter.dialog.GetDefaultAudioSource ();
					}
					
					if (audioSource != null)
					{
						audioSource.clip = clipObj;
						audioSource.loop = false;
						audioSource.Play();
						hasAudio = true;
					}
					
					displayDuration = clipObj.length;
				}
				else
				{
					displayDuration = KickStarter.speechManager.screenTimeFactor * (float) _message.Length;
					if (displayDuration < 0.5f)
					{
						displayDuration = 0.5f;
					}
					
					Debug.Log ("Cannot find audio file: " + fullFilename);
				}
			}
			else
			{
				displayDuration = KickStarter.speechManager.screenTimeFactor * (float) _message.Length;
				if (displayDuration < 0.5f)
				{
					displayDuration = 0.5f;
				}
			}
			
			log.fullText = _message;
			
			if (!CanScroll ())
			{
				if (speaker != null)
				{
					foreach (SpeechGap gap in speechGaps)
					{
						if (gap.expressionID >= 0)
						{
							speaker.SetExpression (gap.expressionID);
						}
					}
				}

				if (continueIndex > 0)
				{
					continueTime = Time.time + (continueIndex / KickStarter.speechManager.textScrollSpeed);
				}
				
				if (speechGaps.Count > 0)
				{
					displayText = log.fullText.Substring (0, speechGaps[0].characterIndex);
				}
				else
				{
					displayText = log.fullText;
				}
			}
			else
			{
				displayText = "";
			}
			
			isAlive = true;
			isSkippable = true;
			pauseGap = false;
			endTime = Time.time + displayDuration;
		}


		/**
		 * Updates the state of the line.
		 * This is called every FixedUpdate call by StateHandler.
		 */
		public void _FixedUpdate ()
		{
			if (pauseGap)
			{
				if (pauseEndTime > 0f && Time.time > pauseEndTime)
				{
					EndPause ();
				}
				else
				{
					return;
				}
			}

			if (CanScroll ())
			{
				if (scrollAmount < 1f)
				{
					if (!pauseGap)
					{
						scrollAmount += KickStarter.speechManager.textScrollSpeed / 100f / log.fullText.Length;
						if (scrollAmount > 1f)
						{
							scrollAmount = 1f;
						}
						
						int currentCharIndex = (int) (scrollAmount * log.fullText.Length);
						
						if (gapIndex > 0)
						{
							currentCharIndex += speechGaps[gapIndex-1].characterIndex;
							if (currentCharIndex > log.fullText.Length)
							{
								currentCharIndex = log.fullText.Length;
							}
						}

						string newText = log.fullText.Substring (0, currentCharIndex);
						
						if (displayText != newText && !hasAudio)
						{
							KickStarter.dialog.PlayScrollAudio (speaker);
						}
						
						displayText = newText;
						if (gapIndex >= 0 && speechGaps.Count > gapIndex)
						{
							if (currentCharIndex == speechGaps [gapIndex].characterIndex)
							{
								float waitTime = speechGaps [gapIndex].waitTime;
								pauseGap = true;
								if (waitTime >= 0f)
								{
									pauseEndTime = Time.time + waitTime;
								}
								else if (speechGaps [gapIndex].expressionID >= 0)
								{
									pauseEndTime = Time.time;
									speaker.SetExpression (speechGaps [gapIndex].expressionID);
								}
								else
								{
									pauseEndTime = 0f;
								}
								return;
							}
						}

						if (continueIndex >= 0)
						{
							if (currentCharIndex >= continueIndex)
							{
								continueIndex = -1;
								continueFromSpeech = true;
							}
						}
					}
					return;
				}
				displayText = log.fullText;
			}
			else
			{
				if (gapIndex >= 0 && speechGaps.Count >= gapIndex)
				{
					if (gapIndex == speechGaps.Count)
					{
						displayText = log.fullText;
						foreach (SpeechGap gap in speechGaps)
						{
							if (gap.waitTime > 0f)
							{
								endTime -= gap.waitTime;
							}
						}
					}
					else
					{
						float waitTime = (float) speechGaps[gapIndex].waitTime;
						displayText = log.fullText.Substring (0, speechGaps[gapIndex].characterIndex);
						
						if (waitTime >= 0)
						{
							endTime = Time.time + waitTime;
						}
						else if (speechGaps[gapIndex].expressionID >= 0)
						{
							gapIndex ++;
						}
						else
						{
							pauseGap = true;
						}
					}
				}
				else
				{
					displayText = log.fullText;
				}
				
				if (continueIndex >= 0)
				{
					if (continueTime < Time.time)
					{
						continueFromSpeech = true;
					}
				}
			}

			if (Time.time > endTime)
			{
				if ((KickStarter.speechManager.displayForever && isBackground)
				 || !KickStarter.speechManager.displayForever)
				{
					EndMessage ();
				}
			}
		}


		/**
		 * Ends the current pause.
		 */
		public void EndPause ()
		{
			pauseGap = false;
			gapIndex ++;
			scrollAmount = 0f;
		}


		private void EndMessage (bool forceOff = false)
		{
			isSkippable = false;
			
			if (speaker)
			{
				speaker.StopSpeaking ();
			}
			
			if (!forceOff && gapIndex >= 0 && gapIndex < speechGaps.Count)
			{
				gapIndex ++;
			}
			else
			{
				isAlive = false;
				KickStarter.stateHandler.UpdateAllMaxVolumes ();
			}
		}


		/**
		 * <summary>Updates the state of the Speech based on the user's input.
		 * This is called every Update call by StateHandler.</summary>
		 */
		public void UpdateInput ()
		{
			if (isSkippable)
			{
				if (pauseGap && !IsBackgroundSpeech ())
				{
					if ((KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.RightClick))
					{
						if (speechGaps[gapIndex].waitTime < 0f)
						{
							KickStarter.playerInput.ResetMouseClick ();
							EndPause ();
						}
						else if (KickStarter.speechManager.allowSpeechSkipping)
						{
							KickStarter.playerInput.ResetMouseClick ();
							EndPause ();
						}
					}
				}
				
				else if (KickStarter.speechManager.displayForever)
				{
					if ((KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.RightClick))
					{
						KickStarter.playerInput.ResetMouseClick ();
						
						if (KickStarter.stateHandler.gameState == GameState.Cutscene)
						{
							if (KickStarter.speechManager.endScrollBeforeSkip && CanScroll () && displayText != log.fullText)
							{
								// Stop scrolling
								scrollAmount = 1f;
								displayText = log.fullText;

								// Find last non-encountered expression
								if (speechGaps != null && speechGaps.Count > gapIndex)
								{
									for (int i=speechGaps.Count-1; i>=gapIndex; i--)
									{
										if (speechGaps[i].expressionID >= 0)
										{
											speaker.SetExpression (speechGaps[i].expressionID);
											return;
										}
									}
								}
							}
							else
							{
								// Stop message
								EndMessage (true);
							}
						}
					}
				}
				
				else if ((KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.RightClick) && KickStarter.speechManager && KickStarter.speechManager.allowSpeechSkipping &&
				         (!IsBackgroundSpeech () || KickStarter.speechManager.allowGameplaySpeechSkipping))
				{
					KickStarter.playerInput.ResetMouseClick ();
					
					if (KickStarter.stateHandler.gameState == GameState.Cutscene || (KickStarter.speechManager.allowGameplaySpeechSkipping && KickStarter.stateHandler.gameState == GameState.Normal))
					{
						if (KickStarter.speechManager.endScrollBeforeSkip && CanScroll () && displayText != log.fullText)
						{
							// Stop scrolling
							if (speechGaps.Count > 0 && speechGaps.Count > gapIndex)
							{
								while (gapIndex < speechGaps.Count && speechGaps[gapIndex].waitTime >= 0)
								{
									// Find next wait
									gapIndex ++;
								}
								
								if (gapIndex == speechGaps.Count)
								{
									scrollAmount = 1f;
									displayText = log.fullText;
								}
								else
								{
									pauseGap = true;
									displayText = log.fullText.Substring (0, speechGaps[gapIndex].characterIndex);
								}
							}
							else
							{
								scrollAmount = 1f;
								displayText = log.fullText;
							}
						}
						else
						{
							EndMessage (true);
						}
					}
				}
			}
		}


		private string DetermineGaps (string _text)
		{
			speechGaps.Clear ();
			continueIndex = -1;
			
			if (_text != null)
			{
				if (_text.Contains ("[wait") || _text.Contains ("[expression:"))
				{
					while (_text.Contains ("[wait") || _text.Contains ("[expression:"))
					{
						int startIndex = _text.IndexOf ("[wait");
						int expressionIndex = _text.IndexOf ("[expression:");

						if (speaker != null && expressionIndex >= 0 && (startIndex == -1 || expressionIndex < startIndex))
						{
							// Expression change
							startIndex = expressionIndex;
							int endIndex = _text.IndexOf ("]", startIndex);
							string expressionText = _text.Substring (startIndex + 12, endIndex - startIndex - 12);
							int expressionID = speaker.GetExpressionID (expressionText);
							speechGaps.Add (new SpeechGap (startIndex, expressionID));
							_text = _text.Substring (0, startIndex) + _text.Substring (endIndex + 1); 
						}
						else if (_text.Substring (startIndex).StartsWith ("[wait]"))
						{
							// Indefinite wait
							speechGaps.Add (new SpeechGap (startIndex, -1));
							_text = _text.Substring (0, startIndex) + _text.Substring (startIndex + 6);
						}
						else
						{
							// Timed wait
							int endIndex = _text.IndexOf ("]", startIndex);
							string waitTimeText = _text.Substring (startIndex + 6, endIndex - startIndex - 6);
							speechGaps.Add (new SpeechGap (startIndex, FloatParse (waitTimeText)));
							_text = _text.Substring (0, startIndex) + _text.Substring (endIndex + 1); 
						}
					}
				}
				
				if (_text.Contains ("[continue]"))
				{
					continueIndex = _text.IndexOf ("[continue]");
					_text = _text.Replace ("[continue]", "");
				}
			}

			// Sort speechGaps
			if (speechGaps.Count > 1)
			{
				speechGaps.Sort (delegate (SpeechGap a, SpeechGap b) {return a.characterIndex.CompareTo (b.characterIndex);});
			}
			
			return _text;
		}


		private float FloatParse (string text)
		{
			float _value = 0f;
			if (text != "")
			{
				float.TryParse (text, out _value);
			}
			return _value;
		}


		private bool IsBackgroundSpeech ()
		{
			if (KickStarter.stateHandler.gameState == GameState.Normal)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Ends speech audio, if it is playing in the background.</summary>
		 * <param name = "newSpeaker">If the line's speaker matches this, the audio will not end</param>
		 */
		public void EndBackgroundSpeechAudio (AC.Char newSpeaker)
		{
			if (isBackground && hasAudio && speaker != null && speaker != newSpeaker)
			{
				if (speaker.GetComponent <AudioSource>())
				{
					speaker.GetComponent <AudioSource>().Stop ();
				}
			}
		}


		/**
		 * <summary>Gets the display name of the speaking character.</summary>
		 * <returns>The display name of the speaking character</returns>
		 */
		public string GetSpeaker ()
		{
			if (speaker)
			{
				if (speaker.speechLabel != "")
				{
					return speaker.speechLabel;
				}
				return speaker.name;
			}
			
			return "";
		}


		/**
		 * <summary>Gets the colour of the subtitle text.</summary>
		 * <returns>The colour of the subtitle text</returns>
		 */
		public Color GetColour ()
		{
			if (speaker)
			{
				return speaker.speechColor;
			}
			return Color.white;
		}
		

		/**
		 * <summary>Gets the speaking character.</summary>
		 * <returns>The speaking character</returns>
		 */
		public AC.Char GetSpeakingCharacter ()
		{
			return speaker;
		}


		/**
		 * <summary>Checks if the speech line is temporarily paused, due to a [wait] or [wait:X] token.</summary>
		 * <returns>True if the speech line is temporarily paused.</returns>
		 */
		public bool IsPaused ()
		{
			return pauseGap;
		}


		/**
		 * <summary>Checks if the line has any pause gaps.</summary>
		 * <returns>True if there are any line gaps</returns>
		 */
		public bool HasPausing ()
		{
			if (speechGaps.Count > 0)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets a Sprite based on the portrait graphic of the speaking character.
		 * If lipsincing is enabled, the sprite will be based on the current phoneme.</summary>
		 * <returns>The speaking character's portrait sprite</returns>
		 */
		public Sprite GetPortraitSprite ()
		{
			if (speaker != null)
			{
				CursorIconBase portraitIcon = speaker.GetPortrait ();
				if (portraitIcon != null && portraitIcon.texture != null)
				{
					if (IsAnimating ())
					{
						if (speaker.isLipSyncing)
						{
							return portraitIcon.GetAnimatedSprite (speaker.GetLipSyncFrame ());
						}
						else
						{
							return portraitIcon.GetAnimatedSprite (true);
						}
					}
					else
					{
						return portraitIcon.GetSprite ();
					}
				}
			}
			return null;
		}
		

		/**
		 * <summary>Gets the portrait graphic of the speaking character.</summary>
		 * <returns>The speaking character's portrait graphic</returns>
		 */
		public Texture2D GetPortrait ()
		{
			if (speaker && speaker.GetPortrait ().texture)
			{
				return speaker.GetPortrait ().texture;
			}
			return null;
		}
		

		/**
		 * <summary>Checks if the speaking character's portrait graphic can be animated.</summary>
		 * <returns>True if the character's portrait graphic can be animated</returns>
		 */
		public bool IsAnimating ()
		{
			if (speaker && speaker.GetPortrait ().isAnimated)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets a Rect of the character's portrait graphic.
		 * If the graphic is animating, only the relevant portion will be returned.</summary>
		 * <returns>A Rect of the character's portrait graphic</returns>
		 */
		public Rect GetAnimatedRect ()
		{
			if (speaker != null && speaker.GetPortrait () != null)
			{
				if (speaker.isLipSyncing)
				{
					return speaker.GetPortrait ().GetAnimatedRect (speaker.GetLipSyncFrame ());
				}
				else if (speaker.isTalking)
				{
					return speaker.GetPortrait ().GetAnimatedRect ();
				}
				else
				{
					return speaker.GetPortrait ().GetAnimatedRect (0);
				}
			}
			return new Rect (0,0,0,0);
		}


		private bool CanScroll ()
		{
			if (speaker == null)
			{
				return KickStarter.speechManager.scrollNarration;
			}
			return KickStarter.speechManager.scrollSubtitles;
		}

	}


	/**
	 * A data struct for an entry in the game's speech log.
	 */
	public struct SpeechLog
	{

		/** The full display text of the line */
		public string fullText;
		/** The display name of the speaking character */
		public string speakerName;
		/** The ID number of the line, as set by the Speech Manager */
		public int lineID;


		/**
		 * Clears the struct.
		 */
		public void Clear ()
		{
			fullText = "";
			speakerName = "";
			lineID = -1;
		}

	}

}