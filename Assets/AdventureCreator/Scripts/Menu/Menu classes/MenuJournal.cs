/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2014
 *	
 *	"MenuJournal.cs"
 * 
 *	This MenuElement provides an array of labels, used to make a book.
 * 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that provides an array of labels, each one representing a page, that collectively form a bork.
	 * "Pages" can be added to the journal mid-game, and changes made to it will be saved in save games.
	 */
	public class MenuJournal : MenuElement
	{

		/** The Unity UI Text this is linked to (Unity UI Menus only) */
		public Text uiText;
		/** A List of JournalPage instances that make up the pages within */
		public List<JournalPage> pages = new List<JournalPage>();
		/** The initial number of pages when the game begins */
		public int numPages = 1;
		/** The index number of the current page being shown */
		public int showPage = 1;
		/** If True, then the "Preview page" set in the Editor will be the first page open when the game begins */
		public bool startFromPage = false;
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;

		private string fullText;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiText = null;

			pages = new List<JournalPage>();
			pages.Add (new JournalPage ());
			numPages = 1;
			showPage = 1;
			isVisible = true;
			isClickable = false;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			textEffects = TextEffects.None;
			fullText = "";

			base.Declare ();
		}


		/**
		 * <summary>Creates and returns a new MenuJournal that has the same values as itself.</summary>
		 * <returns>A new MenuJournal with the same values as itself</returns>
		 */
		public override MenuElement DuplicateSelf ()
		{
			MenuJournal newElement = CreateInstance <MenuJournal>();
			newElement.Declare ();
			newElement.CopyJournal (this);
			return newElement;
		}
		
		
		private void CopyJournal (MenuJournal _element)
		{
			uiText = _element.uiText;
			pages = new List<JournalPage>();
			foreach (JournalPage page in _element.pages)
			{
				pages.Add (page);
			}

			numPages = _element.numPages;
			startFromPage = _element.startFromPage;
			if (startFromPage)
			{
				showPage = _element.showPage;
			}
			else
			{
				showPage = 1;
			}
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			fullText = "";

			base.Copy (_element);
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public override void LoadUnityUI (AC.Menu _menu)
		{
			uiText = LinkUIElement <Text>();
		}
		

		/**
		 * <summary>Gets the boundary of the element</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <returns>The boundary Rect of the element</returns>
		 */
		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiText)
			{
				return uiText.rectTransform;
			}
			return null;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (MenuSource source)
		{
			EditorGUILayout.BeginVertical ("Button");
			numPages = EditorGUILayout.IntField ("Number of starting pages:", numPages);
			if (numPages > 0)
			{
				showPage = EditorGUILayout.IntSlider ("Preview page:", showPage, 1, numPages);
				startFromPage = EditorGUILayout.Toggle ("Start from this page?", startFromPage);

				if (numPages != pages.Count)
				{
					if (numPages > pages.Count)
					{
						while (numPages > pages.Count)
						{
							pages.Add (new JournalPage ());
						}
					}
					else
					{
						pages.RemoveRange (numPages, pages.Count - numPages);
					}
				}

				if (showPage > 0 && pages.Count >= showPage-1)
				{
					EditorGUILayout.LabelField ("Page " + showPage + " text:");
					pages[showPage-1].text = EditorGUILayout.TextArea (pages[showPage-1].text);

					if (pages[showPage-1].text.Contains ("*"))
					{
						EditorGUILayout.HelpBox ("Errors will occur if pages contain '*' characters.", MessageType.Error);
					}
					else if (pages[showPage-1].text.Contains ("|"))
					{
						EditorGUILayout.HelpBox ("Errors will occur if pages contain '|' characters.", MessageType.Error);
					}
				}
			}
			else
			{
				numPages = 1;
			}

			if (source == MenuSource.AdventureCreator)
			{
				anchor = (TextAnchor) EditorGUILayout.EnumPopup ("Text alignment:", anchor);
				textEffects = (TextEffects) EditorGUILayout.EnumPopup ("Text effect:", textEffects);
			}
			else
			{
				EditorGUILayout.EndVertical ();
				EditorGUILayout.BeginVertical ("Button");
				uiText = LinkedUiGUI <Text> (uiText, "Linked Text:", source);
			}

			EditorGUILayout.EndVertical ();
			
			base.ShowGUI (source);
		}
		
		#endif


		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (pages.Count >= showPage)
			{
				fullText = TranslatePage (pages[showPage - 1], languageNumber);
				fullText = AdvGame.ConvertTokens (fullText);
			}

			if (uiText != null)
			{
				UpdateUIElement (uiText);
				uiText.text = fullText;
			}
		}
		

		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">Ignored by this subclass</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);
			
			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (pages.Count >= showPage)
			{
				if (textEffects != TextEffects.None)
				{
					AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, Color.black, _style.normal.textColor, 2, textEffects);
				}
				else
				{
					GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
				}
			}
		}


		/**
		 * <summary>Gets the display text of the current page</summary>
		 * <param name = "slot">Ignored by this subclass</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the current page</returns>
		 */
		public override string GetLabel (int slot, int languageNumber)
		{
			return TranslatePage (pages[showPage - 1], languageNumber);
		}


		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <param name = "doLoop">If True, then shifting right beyond the last page will display the first page, and vice-versa</param>
		 */
		public void Shift (AC_ShiftInventory shiftType, bool doLoop)
		{
			if (shiftType == AC_ShiftInventory.ShiftRight)
			{
				if (pages.Count > showPage)
				{
					showPage ++;
				}
				else if (doLoop)
				{
					showPage = 1;
				}
			}
			else if (shiftType == AC_ShiftInventory.ShiftLeft)
			{
				if (showPage > 1)
				{
					showPage --;
				}
				else if (doLoop)
				{
					showPage = pages.Count;
				}
			}
		}


		private string TranslatePage (JournalPage page, int languageNumber)
		{
			return (SpeechManager.GetTranslation (page.text, page.lineID, languageNumber));
		}

		
		protected override void AutoSize ()
		{
			if (showPage > 0 && pages.Count >= showPage-1)
			{
				if (pages[showPage-1].text == "" && backgroundTexture != null)
				{
					GUIContent content = new GUIContent (backgroundTexture);
					AutoSize (content);
				}
				else
				{
					GUIContent content = new GUIContent (pages[showPage-1].text);
					AutoSize (content);
				}
			
			}
		}
		
	}


	/**
	 * A data container for the contents of each page in a MenuJournal.
	 */
	[System.Serializable]
	public class JournalPage
	{

		/** The translation ID, as set by SpeechManager */
		public int lineID = -1;
		/** The page text, in it's original language */
		public string text = "";


		/**
		 * The default Constructor.
		 */
		public JournalPage ()
		{ }


		public JournalPage (int _lineID, string _text)
		{
			lineID = _lineID;
			text = _text;
		}

	}

}