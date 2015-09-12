/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2015
 *	
 *	"ActionType.cs"
 * 
 *	This defines the variables needed by the ActionsManager Editor Window.
 * 
 */

namespace AC
{

	/**
	 * A data container for an Action's properties as displayed within ActionsManager.
	 */
	[System.Serializable]
	public class ActionType
	{

		/** The Action's filename */
		public string fileName;
		/** The Action's category (ActionList, Camera, Character, Container, Dialogue, Engine, Hotspot, Input, Inventory, Menu, Moveable, Object, Player, Save, Sound, ThirdParty, Variable, Custom) */
		public ActionCategory category;
		/** The Action's title */
		public string title;
		/** A brief description about what the Action does */
		public string description;
		/** If True, the Action is enabled and can be used in ActionList objects */
		public bool isEnabled;
		

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_fileName">The Action's filename</param>
		 * <param name = "_action">The Action itself</param>
		 */
		public ActionType (string _fileName, Action _action)
		{
			fileName = _fileName;
			category = _action.category;
			title = _action.title;
			description = _action.description;
			isEnabled = true;
		}


		/**
		 * <summary>Gets the full name (category + title) of the Action.  Actions in the Custom category are prefixed with "ZZ" so that they appear at the bottom in an alphabetical order.</summary>
		 * <returns>The full name (category + title) of the Action</param>
		 */
		public string GetFullTitle (bool forSorting = false)
		{
			if (forSorting)
			{
				if (category == ActionCategory.Custom)
				{
					return ("ZZ" + title);
				}
			}
			return (category.ToString () + ": " + title);
		}
		
	}

}