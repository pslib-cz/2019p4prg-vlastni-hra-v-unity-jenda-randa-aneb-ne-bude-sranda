/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2019
 *	
 *	"Menu.cs"
 * 
 *	This script is a container of MenuElement subclasses, which together make up a menu.
 *	When menu elements are added, this script updates the size, positioning etc automatically.
 *	The handling of menu visibility, element clicking, etc is all handled in MenuSystem,
 *	rather than the Menu class itself.
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
	 * A Menu is an in-game GUI.
	 * It is made by grouping together MenuElement subclasses, and displaying them in a particular way.
	 * Menus can either be created using OnGUI (aka "Adventure Creator") calls, or by referencing Canvas objects and Unity UI components.
	 */
	[System.Serializable]
	public class Menu : ScriptableObject
	{

		/** The source of the Menu's display information (AdventureCreator, UnityUiPrefab, UnityUiInScene) */ 
		public MenuSource menuSource = MenuSource.AdventureCreator;
		/** If a Menu links to Unity UI, the linked Canvas gameobject */
		public Canvas canvas;
		/** The ConstantID number of the canvas */
		public int canvasID = 0;
		/** A RectTransform that describes the Menu's screen space */
		public RectTransform rectTransform;
		/** The ConstantID number of the rectTransform */
		public int rectTransformID = 0;
		/** The transition method for Unity UI-based menus (None, CanvasGroupFade, CustomAnimation) */
		public UITransition uiTransitionType = UITransition.None;
		/** The position method for Unity UI-based menus (AbovePlayer, AboveSpeakingCharacter, AppearAtCursorAndFreeze, FollowCursor, Manual, OnHotspot) */
		public UIPositionType uiPositionType = UIPositionType.Manual;

		/** If True, the Menu's propertied can be edited in MenuManager */
		public bool isEditing = false;
		/** If True, the Menu is locked off, and won't ever be displayed */
		public bool isLocked = false;
		/** A unique identifier */
		public int id;
		/** A name for the Menu, used in PlayerMenus to identify it */
		public string title;
		/** An OnGUI Menu's total size, if sizeType = AC_SizeType.Manual */
		public Vector2 manualSize = Vector2.zero;
		/** How an OnGUI Menu is positioned (Centred, Aligned, Manual, FollowCursor, AppearAtCursorAndFreeze, OnHotspot, AboveSpeakingCharacter, AbovePlayer) */
		public AC_PositionType positionType = AC_PositionType.Centred;
		/** An OnGUI Menu's centre point, if positionType = AC_PositionType.Manual */
		public Vector2 manualPosition = Vector2.zero;
		/** If True, and the position is changed during the game, a smoothing effect will be applied */
		public bool positionSmoothing = false;
		/** An OnGUI Menu's alignment type, if positionType = AC_PositionType.Aligned */
		public TextAnchor alignment = TextAnchor.MiddleCenter;
		/** The Input axis that toggle the Menu on and off, it appearType = AppearType.OnInputKey */
		public string toggleKey = "";
		/** If True, then mouse clicks will be ineffective */
		public bool ignoreMouseClicks = false;
		/** If True, then the game will be paused whenever the Menu is enabled */
		public bool pauseWhenEnabled = false;
		/** If True, and appearType = AppearType.WhenSpeechPlays, the Menu will not be removed when the game is paused */
		public bool showWhenPaused = false;
		/** If True, then the Menu will be clickable during gameplay-blocking cutscenes */
		public bool canClickInCutscene = false;
		/** If True, and appearType = AppearType.Manual, then the Menu will be enabled when the game begins */
		public bool enabledOnStart = false;
		/** The ActionListAsset to run whenever the Menu is enabled */
		public ActionListAsset actionListOnTurnOn = null;
		/** The ActionListAsset to run whenever the Menu is disabled */
		public ActionListAsset actionListOnTurnOff = null;
		/** If True, the Menu will update while fading out */
		public bool updateWhenFadeOut = true;
		/** If True, and the SettingsManager's takeSaveScreenshots = True, then the Menu will be hidden from view if enabled while a save-game screenshot is being taken */
		public bool hideDuringSaveScreenshots = true;

		/** If True, the Menu will be positioned such that it is always completely within the screen boundary */
		public bool fitWithinScreen = true;
		/** The texture to display in the background */
		public Texture2D backgroundTexture;

		/** A List of MenuElement subclasses that are currently visible */
		public List<MenuElement> visibleElements = new List<MenuElement>();
		/** The progress made along an in/out transition (0 = off, 1 = on) */
		public float transitionProgress = 0f;
		/** The 'rule' that dictates when a Menu is displayed (Manual, MouseOver, DuringConversation, OnInputKey, OnInteraction, OnHotspot, WhenSpeechPlays, DuringGameplay, OnContainer) */
		public AppearType appearType;
		/** What kind of speaker has to be speaking for this Menu to enable, if appearType = AppearType.WhenSpeechPlays (All, CharactersOnly, NarrationOnly, SpecificCharactersOnly) */
		public SpeechMenuType speechMenuType = SpeechMenuType.All;
		/** What kind of speech has to play for this Menu to enable, if appearType = AppearType.WhenSpeechPlays (All, BlockingOnly, BackgroundOnly) */
		public SpeechMenuLimit speechMenuLimit = SpeechMenuLimit.All;
		/** A list of character names that this Menu will show for, if appearType = AppearType.WhenSpeechPlays and speechMenuType = SpeechMenuType.SpecificCharactersOnly */
		public string limitToCharacters = "";
		/** If appearType = AppearType.WhenSpeechPlays, the Menu will show regardless of the 'Subtitles' setting in Options */
		public bool forceSubtitles = false;
		/** If True, and positionType = PositionType.AboveSpeakingCharacter, and oneMenuPerSpeech = True, then the Menu will update its position every frame */
		public bool moveWithCharacter = true;

		/** Which OnGUI MenuElement is currently active, when it is keyboard-controlled */
		public MenuElement selected_element;
		/** Which slot within an OnGUI MenuElement is currently active, when it is keyboard-controlled */
		public int selected_slot = 0;
		/** If True, the first visible Unity UI MenuElement will be automatically selected when the Menu is turned on */
		public bool autoSelectFirstVisibleElement = false;
		/** The name of the Unity UI MenuElement to automatically select when the Menu is turned on */
		public string firstSelectedElement;

		/** A List of MenuElement subclasses that are drawn within the Menu */
		public List<MenuElement> elements = new List<MenuElement>();

		/** The spacing between OnGUI MenuElement subclasses, when sizeType = AC_SizeType.Automatic */
		public float spacing;
		/** How the size of the OnGUI Menu is determined (AbsolutePixels, Automatic, Manual) */
		public AC_SizeType sizeType;
		/** If True, and sizeType = AC_SizeType.Automatic, then the dimensions of the Menu will be recalculated every frame */
		public bool autoSizeEveryFrame = false;

		/** How OnGUI MenuElements are arranged together (Horizontal, Vertical) */
		public MenuOrientation orientation;
		/** How an OnGUI Menu transitions in and out (Fade, FadeAndPan, None, Pan, Zoom) */
		public MenuTransition transitionType = MenuTransition.None;
		/** The pan direction of an OnGUI Menu, if the Menu pans when transitioning */
		public PanDirection panDirection = PanDirection.Up;
		/** The pan animation style of an OnGUI Menu, if the Menu pans when transitioning */
		public PanMovement panMovement = PanMovement.Linear;
		/** An AnimationCurve that describes the transition progress over time */
		public AnimationCurve timeCurve = new AnimationCurve (new Keyframe(0, 0), new Keyframe(1, 1));
		/** The pan distance of an OnGUI Menu, if the Menu pans when transitioning */
		public float panDistance = 0.5f;
		/** The transition duration, in seconds */
		public float fadeSpeed = 0f;
		/** The zoom alignment, if transitionType = MenuTransitio.Zoom */
		public TextAnchor zoomAnchor = TextAnchor.MiddleCenter;
		/** If True, then MenuElement subclasses will also re-size during zoom transitions */
		public bool zoomElements = false;
		/** If True, then a new instance of the Menu will be created for each speech line, if appearType = AppearType.WhenSpeechPlays */
		public bool oneMenuPerSpeech = false;
		/** If oneMenuPerSpeech = True, and appearType = AppearType.WhenSpeechPlays, then this determines how the menu is affected by proximity to the speaking character */
		public SpeechProximityLimit speechProximityLimit = SpeechProximityLimit.NoLimit;
		/** The maximum distance to display speech for, if speechProximityLimit != SpeechProximityLimit.NoLimit */
		public float speechProximityDistance = 10f;
		private bool isDuplicate = false;
		private bool hasMoved = false;

		public bool deleteUIWhenTurnOff = false;

		/** The Speech instance tied to the Menu, if a duplicate was made specifically for it */
		public Speech speech;

		// Interaction menus
		private InvItem forItem;
		private Hotspot forHotspot;

		private CanvasScaler canvasScaler;
		private CanvasGroup canvasGroup;
		private Animator canvasAnimator;
		private float fadeStartTime = 0f;
		private bool isFading = false;
		private FadeType fadeType = FadeType.fadeIn;
		private Vector2 panOffset = Vector2.zero;
		private Vector2 dragOffset = Vector2.zero;
		private float zoomAmount = 1f;
		
		private GameState gameStateWhenTurnedOn;
		private bool isEnabled;
		private bool isDisabledForScreenshot = false;
		private string idString;

		private bool canDoSmoothing = false;
		private int elementCount = -1;

		[SerializeField] private Vector2 biggestElementSize;
		[SerializeField] private Rect rect = new Rect ();

		#if UNITY_EDITOR
		private bool doProportionalScaling = false;
		public bool showInFilter;
		#endif


		/**
		 * <summary>Initialises a Menu when it is created within MenuManager.</summary>
		 * <param name = "idArray">An array of previously-used ID numbers</param>
		 */
		public void Declare (int[] idArray)
		{
			menuSource = MenuSource.AdventureCreator;
			canvas = null;
			canvasID = 0;
			uiPositionType = UIPositionType.Manual;
			uiTransitionType = UITransition.None;

			spacing = 0.5f;
			orientation = MenuOrientation.Vertical;
			appearType = AppearType.Manual;
			oneMenuPerSpeech = false;
			speechProximityLimit = SpeechProximityLimit.NoLimit;
			speechProximityDistance = 10f;
			moveWithCharacter = true;

			fitWithinScreen = true;
			elements = new List<MenuElement>();
			visibleElements = new List<MenuElement>();
			enabledOnStart = false;
			isEnabled = false;
			sizeType = AC_SizeType.Automatic;
			autoSizeEveryFrame = false;
			speechMenuType = SpeechMenuType.All;
			speechMenuLimit = SpeechMenuLimit.All;
			limitToCharacters = "";
			forceSubtitles = false;
			actionListOnTurnOn = null;
			actionListOnTurnOff = null;
			firstSelectedElement = "";
			autoSelectFirstVisibleElement = false;
			
			fadeSpeed = 0f;
			transitionType = MenuTransition.None;
			panDirection = PanDirection.Up;
			panMovement = PanMovement.Linear;
			timeCurve = new AnimationCurve (new Keyframe(0, 0), new Keyframe(1, 1));
			panDistance = 0.5f;
			zoomAnchor = TextAnchor.MiddleCenter;
			zoomElements = false;
			ignoreMouseClicks = false;
			
			pauseWhenEnabled = false;
			showWhenPaused = false;
			canClickInCutscene = false;
			id = 0;
			isLocked = false;
			updateWhenFadeOut = true;
			hideDuringSaveScreenshots = true;
			positionSmoothing = false;
			hasMoved = false;
			elementCount = -1;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (id == _id)
				{
					id ++;
				}
			}
			
			title = "Menu " + (id + 1).ToString ();
		}


		/**
		 * <summary>Copies the values of another Menu, and initialises it for display.</summary>
		 * <param name = "menuToCopy">The other Menu to copy from</param>
		 */
		public void CreateDuplicate (AC.Menu menuToCopy)
		{
			Copy (menuToCopy, false);
			LoadUnityUI ();
			Recalculate ();
			Initalise ();
		}


		/**
		 * <summary>Copies the variables of another Menu onto itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <param name = "_menu">The Menu to copy from</param>
		 * <param name = "forceUIFields">If True, the variables related to Unity UI-sourced Menus will also be copied, regardless of the Menu's menuSource value</param>
		 */
		public void Copy (AC.Menu _menu, bool fromEditor, bool forceUIFields = false)
		{
			menuSource = _menu.menuSource;
			if (forceUIFields || menuSource == MenuSource.UnityUiPrefab || menuSource == MenuSource.UnityUiInScene)
			{
				canvas = _menu.canvas;
				canvasID = _menu.canvasID;
				rectTransform = _menu.rectTransform;
				rectTransformID = _menu.rectTransformID;
			}
			uiTransitionType = _menu.uiTransitionType;
			uiPositionType = _menu.uiPositionType;

			isEditing = false;
			id = _menu.id;
			isLocked = _menu.isLocked;
			title = _menu.title;
			manualSize = _menu.manualSize;
			autoSizeEveryFrame = _menu.autoSizeEveryFrame;
			positionType = _menu.positionType;
			manualPosition = _menu.manualPosition;
			fitWithinScreen = _menu.fitWithinScreen;
			alignment = _menu.alignment;
			toggleKey = _menu.toggleKey;

			backgroundTexture = _menu.backgroundTexture;
			visibleElements = new List<MenuElement>();
			transitionProgress = 0f;
			appearType = _menu.appearType;
			oneMenuPerSpeech = _menu.oneMenuPerSpeech;
			speechProximityLimit = _menu.speechProximityLimit;
			speechProximityDistance = _menu.speechProximityDistance;
			moveWithCharacter = _menu.moveWithCharacter;
			selected_element = null;
			selected_slot = 0;
			firstSelectedElement = _menu.firstSelectedElement;
			autoSelectFirstVisibleElement = _menu.autoSelectFirstVisibleElement;

			spacing = _menu.spacing;
			sizeType = _menu.sizeType;
			orientation = _menu.orientation;
			fadeSpeed = _menu.fadeSpeed;
			transitionType = _menu.transitionType;
			panDirection = _menu.panDirection;
			panMovement = _menu.panMovement;
			timeCurve = _menu.timeCurve;
			panDistance = _menu.panDistance;
			zoomAnchor = _menu.zoomAnchor;
			zoomElements = _menu.zoomElements;
			pauseWhenEnabled = _menu.pauseWhenEnabled;
			showWhenPaused = _menu.showWhenPaused;
			canClickInCutscene = _menu.canClickInCutscene;

			speechMenuType = _menu.speechMenuType;
			speechMenuLimit = _menu.speechMenuLimit;
			enabledOnStart = _menu.enabledOnStart;
			actionListOnTurnOn = _menu.actionListOnTurnOn;
			actionListOnTurnOff = _menu.actionListOnTurnOff;
			ignoreMouseClicks = _menu.ignoreMouseClicks;
			limitToCharacters = _menu.limitToCharacters;
			forceSubtitles = _menu.forceSubtitles;
			updateWhenFadeOut = _menu.updateWhenFadeOut;
			hideDuringSaveScreenshots = _menu.hideDuringSaveScreenshots;
			positionSmoothing = _menu.positionSmoothing;

			idString = id.ToString ();
			elementCount = -1;

			elements = new List<MenuElement>();
			bool ignoreUnityUI = (Application.isPlaying && !fromEditor && _menu.menuSource == MenuSource.AdventureCreator);
			foreach (MenuElement _element in _menu.elements)
			{
				MenuElement newElement = _element.DuplicateSelf (fromEditor, ignoreUnityUI);
				elements.Add (newElement);
			}

			canDoSmoothing = CanDoSmoothing ();
		}


		/**
		 * Instantiates and initialises a linked Unity UI Canvas, if Unity UI is used for display.
		 */
		public void LoadUnityUI ()
		{
			if (!IsUnityUI ())
			{
				return;
			}

			Canvas localCanvas = null;

			if (menuSource == MenuSource.UnityUiPrefab)
			{
				if (canvas != null)
				{
					localCanvas = (Canvas) Instantiate (canvas);
					localCanvas.gameObject.name = canvas.name;
					DontDestroyOnLoad (localCanvas.gameObject);
				}
			}
			else if (menuSource == MenuSource.UnityUiInScene)
			{
				localCanvas = Serializer.returnComponent <Canvas> (canvasID, KickStarter.sceneSettings.gameObject);
			}

			canvas = localCanvas;
			EnableUI ();

			if (localCanvas != null)
			{
				rectTransform = Serializer.GetGameObjectComponent <RectTransform> (rectTransformID, localCanvas.gameObject);
				if (/*localCanvas.renderMode != RenderMode.ScreenSpaceOverlay &&*/ localCanvas.worldCamera == null)
				{
					localCanvas.worldCamera = Camera.main;
				}

				if (rectTransform != null && rectTransform.gameObject == localCanvas.gameObject)
				{
					ACDebug.LogWarning ("The menu '" + title + "' uses its Canvas for its RectTransform boundary. The RectTransform boundary should instead be a child object of the Canvas.", canvas.gameObject);
				}

				SetParent ();

				canvasGroup = canvas.GetComponent <CanvasGroup>();
				canvasScaler = canvas.GetComponent <CanvasScaler>();
				canvasAnimator = canvas.GetComponent <Animator>();
			}
			else
			{
				ACDebug.LogWarning ("The Menu '" + title + "' has its Source set to " + menuSource.ToString () + ", but no Linked Canvas can be found!");
			}

			if (IsUnityUI ())
			{
				foreach (MenuElement _element in elements)
				{
					_element.LoadUnityUI (this, localCanvas);
				}
			}

			if (!isDuplicate)
			{
				// Duplicates don't need to be disabled, since they're initialised when turned on
				DisableUI ();
			}
		}


		private void SetAnimState ()
		{
			if (IsUnityUI () && uiTransitionType == UITransition.CustomAnimation && fadeSpeed > 0f && canvas != null && canvasAnimator != null)
			{
				if (!canvas.gameObject.activeSelf)
				{
					return;
				}

				if (isFading)
				{
					if (fadeType == FadeType.fadeIn)
					{
						canvasAnimator.Play ("On", -1, transitionProgress);
					}
					else
					{
						canvasAnimator.Play ("Off", -1, 1f - transitionProgress);
					}
				}
				else
				{
					if (isEnabled)
					{
						canvasAnimator.Play ("OnInstant", -1, 0f);
					}
					else
					{
						canvasAnimator.Play ("OffInstant", -1, 0f);
					}
				}
			}
		}


		/**
		 * Places the linked Canvas in the "_UI" hierarchy folder, if Unity UI is used for display.
		 */
		public void SetParent ()
		{
			#if UNITY_5_4_OR_NEWER
			return;
			#else

			if (GetsDuplicated ()) return;

			GameObject uiOb = GameObject.Find ("_UI");
			if (uiOb != null && canvas != null && canvas.renderMode != RenderMode.WorldSpace)
			{
				uiOb.transform.position = Vector3.zero;
				canvas.transform.SetParent (uiOb.transform);
			}

			#endif
		}


		/**
		 * <summary>Checks if the Menu gets duplicated for either each subtitle line or Hotspot.</summary>
		 * <returns>True if the Menu gets duplicated for either each subtitle line or Hotspot.</returns>
		 */
		public bool GetsDuplicated ()
		{
			if (oneMenuPerSpeech)
			{
				return (appearType == AppearType.WhenSpeechPlays);
			}
			return false;
		}


		public void DuplicateInGame (Menu otherMenu)
		{
			isDuplicate = true;
			Copy (otherMenu, false);
		}


		/**
		 * Removes the linked Canvas from the "_UI" hierarchy folder, if Unity UI is used for display.
		 * This is necessary for prefabs that must survive scene changes.
		 */
		public void ClearParent ()
		{
			if (GetsDuplicated ()) return;

			GameObject uiOb = GameObject.Find ("_UI");
			if (uiOb != null && canvas != null)
			{
				if (canvas.transform.parent == uiOb.transform)
				{
					canvas.transform.SetParent (null);
				}
			}
		}


		/**
		 * Initialises the Menu when the game begins.
		 */
		public void Initalise ()
		{
			if (appearType == AppearType.Manual && enabledOnStart && !isLocked)
			{
				transitionProgress = 1f;
				EnableUI ();
				TurnOn (false);
			}
			else
			{
				transitionProgress = 0f;
				DisableUI ();
				TurnOff (false);
			}
			if (transitionType == MenuTransition.Zoom)
			{
				zoomAmount = 0f;
			}

			foreach (MenuElement _element in elements)
			{
				_element.Initialise (this);
			}

			SetAnimState ();
			UpdateTransition ();
		}


		/**
		 * Enables the associated Unity UI canvas, if source != MenuSource.AdventureCreator
		 */
		public void EnableUI ()
		{
			if (menuSource == MenuSource.AdventureCreator || (GetsDuplicated () && !isDuplicate)) return;

			if (canvas != null)
			{
				canvas.gameObject.SetActive (true);
				canvas.enabled = true;

				if (isDuplicate && uiTransitionType == UITransition.CanvasGroupFade && canvasGroup != null && fadeSpeed > 0f)
				{
					canvasGroup.alpha = 0f;
				}

				if (CanCurrentlyKeyboardControl () && IsClickable ())
				{
					if (selected_element == null)
					{
						// If manually set with 'Menu: Select element' Action, don't select any element
						KickStarter.playerMenus.FindFirstSelectedElement ();
					}
				}
			}
		}


		/**
		 * Disables the associated Unity UI canvas, if source != MenuSource.AdventureCreator
		 */
		public void DisableUI ()
		{
			if (canvas != null && menuSource != MenuSource.AdventureCreator)
			{
				isEnabled = false;
				isFading = false;

				if (canvas.gameObject.activeSelf)
				{
					SetAnimState ();
					canvas.gameObject.SetActive (false);
				}

				bool shouldDisable = KickStarter.playerMenus.DeselectEventSystemMenu (this);
				if (shouldDisable)
				{
					KickStarter.playerMenus.FindFirstSelectedElement (this);
				}
			}

			if (deleteUIWhenTurnOff)
			{
				if (canvas != null)
				{
					KickStarter.sceneChanger.ScheduleForDeletion (canvas.gameObject);
				}
				KickStarter.playerMenus.UnregisterCustomMenu (this, false);
			}
		}


		/**
		 * Makes all linked UI elements interactive, if the Menu is drawn with Unity UI.
		 */
		public void MakeUIInteractive ()
		{
			SetUIInteractableState (true);
		}


		/**
		 * Makes all linked UI elements non-interactive, if the Menu is drawn with Unity UI.
		 */
		public void MakeUINonInteractive ()
		{
			if (!IsClickable ())
			{
				SetUIInteractableState (false);
			}
		}


		private void SetUIInteractableState (bool state)
		{
			if (menuSource != MenuSource.AdventureCreator)
			{
				foreach (MenuElement element in elements)
				{
					element.SetUIInteractableState (state);
				}
			}
		}


		#if UNITY_EDITOR
		
		public void ShowGUI ()
		{
			string apiPrefix = "AC.PlayerMenus.GetMenuWithName (\"" + title + "\")";

			title = CustomGUILayout.TextField ("Menu name:", title, apiPrefix + ".title", "A name for the menu");
			menuSource = (MenuSource) CustomGUILayout.EnumPopup ("Source:", menuSource, apiPrefix + ".menuSource", "The source of the menu's display information");

			isLocked = CustomGUILayout.Toggle ("Start game locked off?", isLocked, apiPrefix + ".isLocked", "If True, the menu is locked off, and won't ever be displayed - even if the conditions for its 'Appear type' are met");
			ignoreMouseClicks = CustomGUILayout.Toggle ("Ignore Cursor clicks?", ignoreMouseClicks, apiPrefix + ".ignoreMouseClicks", "If True, then mouse clicks will be ineffective");
			actionListOnTurnOn = ActionListAssetMenu.AssetGUI ("ActionList when turn on:", actionListOnTurnOn, title + "_TurnOn", apiPrefix + ".actionListOnTurnOn", "The ActionList asset to run whenever the menu is enabled");
			actionListOnTurnOff = ActionListAssetMenu.AssetGUI ("ActionList when turn off:", actionListOnTurnOff, title + "_TurnOff", apiPrefix + ".actionListOnTurnOff", "The ActionList asset to run whenever the menu is disabled");

			if (actionListOnTurnOff != null && ShouldTurnOffWhenLoading ())
			{
				EditorGUILayout.HelpBox ("The 'ActionList when turn off' will not be run if the Menu is turned off as a result of loading a save game.  The SaveList element's 'ActionList after load' should be used instead.", MessageType.Warning);
			}

			appearType = (AppearType) CustomGUILayout.EnumPopup ("Appear type:", appearType, apiPrefix + ".appearType", "The 'rule' that dictates when a menu is displayed");

			if (appearType == AppearType.OnInputKey)
			{
				toggleKey = CustomGUILayout.TextField ("Toggle key:", toggleKey, apiPrefix + ".toggleKey", "The Input axis that toggle the menu on and off");
			}
			else if (appearType == AppearType.Manual)
			{
				enabledOnStart = CustomGUILayout.Toggle ("Enabled on start?", enabledOnStart, apiPrefix + ".enabledOnStart", "If True, then the menu will be enabled when the game begins");
			}
			else if (appearType == AppearType.WhenSpeechPlays)
			{
				speechMenuType = (SpeechMenuType) CustomGUILayout.EnumPopup ("For speakers of type:", speechMenuType, apiPrefix + ".speechMenuType", "What kind of speaker has to be speaking for this menu to enable");
				speechMenuLimit = (SpeechMenuLimit) CustomGUILayout.EnumPopup ("For speech of type:", speechMenuLimit, apiPrefix + ".speechMenuLimit", "What kind of speech has to play for this menu to enable");

				if (speechMenuType == SpeechMenuType.SpecificCharactersOnly)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent ("Character(s) to limit to:", "A list of character names that this menu will show for"), GUILayout.MaxWidth (148f));
					limitToCharacters = CustomGUILayout.TextArea (limitToCharacters, GUILayout.MaxWidth (1000f), apiPrefix + ".limitToCharacters");
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.HelpBox ("Multiple character names should be separated by a colon ';'", MessageType.Info);
				}
				else if (speechMenuType == SpeechMenuType.AllExceptSpecificCharacters)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent ("Character(s) to exclude:", "A list of character names that this menu will not show for"), GUILayout.MaxWidth (148f));
					limitToCharacters = CustomGUILayout.TextArea (limitToCharacters, GUILayout.MaxWidth (1000f), apiPrefix + ".limitToCharacters");
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.HelpBox ("Multiple character names should be separated by a colon ';'", MessageType.Info);
				}

				oneMenuPerSpeech = CustomGUILayout.Toggle ("Duplicate for each line?", oneMenuPerSpeech, apiPrefix + ".oneMenuPerSpeech", "If True, then a new instance of the menu will be created for each speech line");
				if (oneMenuPerSpeech)
				{
					speechProximityLimit = (SpeechProximityLimit) CustomGUILayout.EnumPopup ("Limit by speaker proximity:", speechProximityLimit, apiPrefix + ".speechProximityLimit", "Determines how the menu is affected by proximity to the speaking character");
					if (speechProximityLimit != SpeechProximityLimit.NoLimit)
					{
						speechProximityDistance = CustomGUILayout.FloatField ("Max distance from speaker:", speechProximityDistance, apiPrefix + ".speechProximityDistance", "The maximum distance from the speaker to display speech for");
					}
				}

				forceSubtitles = CustomGUILayout.Toggle ("Ignore 'Subtitles' option?", forceSubtitles, apiPrefix + ".forceSubtitles", "If True the menu will show regardless of the 'Subtitles' setting in Options");

				if (oneMenuPerSpeech)
				{
					if ((IsUnityUI () && uiPositionType == UIPositionType.AboveSpeakingCharacter) ||
						(!IsUnityUI () && positionType == AC_PositionType.AboveSpeakingCharacter))
					{
						moveWithCharacter = CustomGUILayout.Toggle ("Move with character?", moveWithCharacter, apiPrefix + ".moveWithCharacter", "If True, then the menu will update its position every frame");
					}
				}
			}

			if (CanPause ())
			{
				pauseWhenEnabled = CustomGUILayout.Toggle ("Pause game when enabled?", pauseWhenEnabled, apiPrefix + ".pauseWhenEnabled", "If True, then the game will be paused whenever the menu is enabled");
			}
			else if (appearType == AppearType.WhenSpeechPlays)
			{
				showWhenPaused = CustomGUILayout.Toggle ("Also show when paused?", showWhenPaused, apiPrefix + ".showWhenPaused", "If True, the menu will not be removed when the game is paused");
			}

			if (ShowClickInCutscenesOption () && !ignoreMouseClicks)
			{
				canClickInCutscene = CustomGUILayout.Toggle ("Clickable in cutscenes?", canClickInCutscene, apiPrefix + ".canClickInCutscene", "If True, then the menu will be clickable during gameplay-blocking cutscenes");
				if (canClickInCutscene)
				{
					EditorGUILayout.HelpBox ("Only Button, Toggle, and Cycle will be clickable during cutscenes.", MessageType.Info);
				}
			}

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.takeSaveScreenshots)
			{
				hideDuringSaveScreenshots = CustomGUILayout.Toggle ("Hide in save screenshots?", hideDuringSaveScreenshots, apiPrefix + ".hideDuringSaveScreenshots", "If True, the Menu will be hidden while taking save-game screenshots");
			}

			if (menuSource == MenuSource.AdventureCreator)
			{
				positionType = (AC_PositionType) CustomGUILayout.EnumPopup ("Position:", positionType, apiPrefix + ".positionType", "How the menu is positioned");
				if (positionType == AC_PositionType.Aligned)
				{
					alignment = (TextAnchor) CustomGUILayout.EnumPopup ("Alignment:", alignment, apiPrefix + ".alignment", "The menu's alignment mode");
				}
				else if (positionType == AC_PositionType.Manual || positionType == AC_PositionType.FollowCursor || positionType == AC_PositionType.AppearAtCursorAndFreeze || positionType == AC_PositionType.OnHotspot || positionType == AC_PositionType.AboveSpeakingCharacter || positionType == AC_PositionType.AbovePlayer)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("X:", GUILayout.Width (20f));
					manualPosition.x = EditorGUILayout.Slider (manualPosition.x, 0f, 100f);
					EditorGUILayout.LabelField ("Y:", GUILayout.Width (20f));
					manualPosition.y = EditorGUILayout.Slider (manualPosition.y, 0f, 100f);
					EditorGUILayout.EndHorizontal ();

					fitWithinScreen = CustomGUILayout.Toggle ("Always fit within screen?", fitWithinScreen, apiPrefix + ".fitWithinScreen", "If True, the menu will be positioned such that it is always completely within the screen boundary");
				}
				
				sizeType = (AC_SizeType) CustomGUILayout.EnumPopup ("Size:", sizeType, apiPrefix + ".sizeType", "How the size of the menu is determined");
				if (sizeType == AC_SizeType.Manual)
				{
					Vector2 originalManualSize = manualSize;

					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("W:", GUILayout.Width (17f));
					originalManualSize.x = EditorGUILayout.Slider (manualSize.x, 0f, 100f);
					EditorGUILayout.LabelField ("H:", GUILayout.Width (15f));
					originalManualSize.y = EditorGUILayout.Slider (manualSize.y, 0f, 100f);

					if (GUILayout.Button ("", (doProportionalScaling) ? CustomStyles.IconLock : CustomStyles.IconUnlock))
					{
						doProportionalScaling = !doProportionalScaling;
					}
					EditorGUILayout.EndHorizontal ();

					if (doProportionalScaling)
					{
						if (!Mathf.Approximately (originalManualSize.x, manualSize.x))
						{
							float proportion = manualSize.y / manualSize.x;
							originalManualSize.y = proportion * originalManualSize.x;
						}
						else if (!Mathf.Approximately (originalManualSize.y, manualSize.y))
						{
							float proportion = manualSize.x / manualSize.y;
							originalManualSize.x = proportion * originalManualSize.y;
						}
					}

					manualSize = originalManualSize;
				}
				else if (sizeType == AC_SizeType.AbsolutePixels)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Width:", GUILayout.Width (50f));
					manualSize.x = EditorGUILayout.FloatField (manualSize.x);
					EditorGUILayout.LabelField ("Height:", GUILayout.Width (50f));
					manualSize.y = EditorGUILayout.FloatField (manualSize.y);
					EditorGUILayout.EndHorizontal ();
				}
				else if (sizeType == AC_SizeType.Automatic)
				{
					autoSizeEveryFrame = CustomGUILayout.Toggle ("Resize every frame?", autoSizeEveryFrame, apiPrefix + ".autoSizeEveryFrame", "If True, then the dimensions of the menu will be recalculated every frame");
					if (autoSizeEveryFrame)
					{
						EditorGUILayout.HelpBox ("This process is fairly CPU-intensive, so only use it if your are having display issues without it.", MessageType.Info);
					}
				}

				if (CanDoSmoothing (true))
				{
					positionSmoothing = CustomGUILayout.Toggle ("Smooth movement?", positionSmoothing, apiPrefix + ".positionSmoothing", "If True, a smoothing effect will be applied as the menu is repositioned");
				}
				
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("Background texture:", "The texture to display in the background"), GUILayout.Width (145f));
				backgroundTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (backgroundTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".backgroundTexture");
				EditorGUILayout.EndHorizontal ();

				spacing = CustomGUILayout.Slider ("Element spacing (%):", spacing, 0f, 10f, "The spacing between Elements");
				orientation = (MenuOrientation) CustomGUILayout.EnumPopup ("Element orientation:", orientation, apiPrefix + ".orientation", "How elements are arranged together, when their Position is Automatic");

				transitionType = (MenuTransition) CustomGUILayout.EnumPopup ("Transition type:", transitionType, apiPrefix + ".transitionType", "How the menu transitions in and out ");
				if (transitionType == MenuTransition.Pan || transitionType == MenuTransition.FadeAndPan)
				{
					panDirection = (PanDirection) CustomGUILayout.EnumPopup ("Pan from:", panDirection, apiPrefix + ".panDirection", "The direction to pan when transitioning");
					panDistance = CustomGUILayout.Slider ("Pan distance:", panDistance, 0f, 1f, apiPrefix + ".panDistance", "The distance to pan when transitioning");
				}
				else if (transitionType == MenuTransition.Zoom)
				{
					zoomAnchor = (TextAnchor) CustomGUILayout.EnumPopup ("Zoom from:", zoomAnchor, apiPrefix + ".zoomAnchor", "The zoom alignment when transitioning");
					zoomElements = CustomGUILayout.Toggle ("Adjust elements?", zoomElements, apiPrefix + ".zoomElements", "If True, then elements will also re-size during zoom transitions");
				}

				if (transitionType != MenuTransition.None)
				{
					fadeSpeed = CustomGUILayout.Slider ("Transition time (s):", fadeSpeed, 0f, 2f, apiPrefix + ".fadeSpeed", "The transition duration, in seconds");
					TransitionAnimGUI (apiPrefix);

					if (fadeSpeed > 0f)
					{
						updateWhenFadeOut = CustomGUILayout.Toggle ("Update while fading out?", updateWhenFadeOut, apiPrefix + ".updateWhenFadeOut", "If True, the menu will update while transitioning out");
					}
				}
			}
			else
			{
				uiPositionType = (UIPositionType) CustomGUILayout.EnumPopup ("Position type:", uiPositionType, apiPrefix + ".uiPositionType", "The way the menu is positioned");
				fitWithinScreen = CustomGUILayout.Toggle ("Always fit within screen?", fitWithinScreen, apiPrefix + ".fitWithinScreen", "If True, the menu will be positioned such that it is always completely within the screen boundary");

				if (uiPositionType == UIPositionType.Manual && fitWithinScreen)
				{
					EditorGUILayout.HelpBox ("To keep the Menu within the screen boundary, assign a 'RectTransform boundary', and call Menu.SetCentre to reposition the Menu.", MessageType.Info);
				}

				if (CanDoSmoothing (true))
				{
					positionSmoothing = CustomGUILayout.Toggle ("Smooth movement?", positionSmoothing, apiPrefix + ".positionSmoothing", "If True, a smoothing effect will be applied as the menu is repositioned");
				}

				uiTransitionType = (UITransition) CustomGUILayout.EnumPopup ("Transition type:", uiTransitionType, apiPrefix + ".uiTransitionType", "The way the menu transitions");
				if (uiTransitionType != UITransition.None)
				{
					fadeSpeed = CustomGUILayout.Slider ("Transition time (s):", fadeSpeed, 0f, 2f, apiPrefix + ".fadeSpeed", "The transition duration, in seconds");
					if (uiTransitionType == UITransition.CanvasGroupFade)
					{
						TransitionAnimGUI (apiPrefix);
						if (canvas == null || canvas.GetComponent <CanvasGroup>() == null)
						{
							EditorGUILayout.HelpBox ("A Canvas Group component must be attached to the Canvas object.", MessageType.Info);
						}
					}
					else if (uiTransitionType == UITransition.CustomAnimation)
					{
						EditorGUILayout.HelpBox ("The Canvas must have an Animator with 4 States: On, Off, OnInstant and OffInstant.", MessageType.Info);
					}

					if (uiTransitionType != UITransition.None && fadeSpeed > 0f)
					{
						updateWhenFadeOut = CustomGUILayout.Toggle ("Update while fading out?", updateWhenFadeOut, apiPrefix + ".updateWhenFadeOut", "If True, the menu will update while fading out");
					}
				}

				bool isInScene = false;
				if (menuSource == MenuSource.UnityUiInScene)
				{
					isInScene = true;
				}

				if (isInScene)
				{
					canvas = (Canvas) CustomGUILayout.ObjectField <Canvas> ("Linked Canvas:", canvas, isInScene, apiPrefix + ".canvas", "The in-scene Canvas GameObject that renders the menu");
					canvasID = Menu.FieldToID <Canvas> (canvas, canvasID);
					canvas = Menu.IDToField <Canvas> (canvas, canvasID, menuSource);
				}
				else
				{
					canvas = (Canvas) CustomGUILayout.ObjectField <Canvas> ("Linked Canvas prefab:", canvas, isInScene, apiPrefix + ".canvas", "The Canvas prefab that renders the menu. This will be spawned into the scene automatically at runtime");
				}

				rectTransform = (RectTransform) CustomGUILayout.ObjectField <RectTransform> ("RectTransform boundary:", rectTransform, true, apiPrefix + ".rectTransform", "A RectTransform that describes the Menu's screen space.");
				rectTransformID = Menu.FieldToID <RectTransform> (rectTransform, rectTransformID);
				rectTransform = Menu.IDToField <RectTransform> (rectTransform, rectTransformID, menuSource);

				autoSelectFirstVisibleElement = CustomGUILayout.ToggleLeft ("Auto-select first visible Element?", autoSelectFirstVisibleElement, apiPrefix + ".autoSelectFirstVisibleElement", "If True, the first visible element will be automatically selected when the Menu is turned on");
				if (!autoSelectFirstVisibleElement)
				{
					firstSelectedElement = CustomGUILayout.TextField ("First selected Element:", firstSelectedElement, apiPrefix + ".firstSelectedElement", "The name of the element to automatically select when the Menu is turned on");

					if (string.IsNullOrEmpty (firstSelectedElement))
					{
						EditorGUILayout.HelpBox ("For UIs to be keyboard-controlled, an element to select must be defined above.", MessageType.Warning);
					}
				}
			}
		}


		private void TransitionAnimGUI (string apiPrefix)
		{
			panMovement = (PanMovement) CustomGUILayout.EnumPopup ("Transition animation:", panMovement, apiPrefix + ".panMovement", "The animation style when transitioning");
			if (panMovement == PanMovement.CustomCurve && fadeSpeed > 0f)
			{
				timeCurve = CustomGUILayout.CurveField ("Time curve:", timeCurve, apiPrefix + ".timeCurve", "A curve that describes the transition progress over time");
			}
		}


		public static int FieldToID <T> (T field, int _constantID) where T : Component
		{
			if (field == null)
			{
				return _constantID;
			}
			
			if (field.GetComponent <ConstantID>() != null)
			{
				if (!field.gameObject.activeInHierarchy && field.GetComponent <ConstantID>().constantID == 0)
				{
					UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject, true);
				}
				_constantID = field.GetComponent <ConstantID>().constantID;
			}
			else
			{
				UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject, true);
				AssetDatabase.SaveAssets ();
			}
			
			return _constantID;
		}

		
		public static T IDToField <T> (T field, int _constantID, MenuSource source) where T : Component
		{
			if (Application.isPlaying || source == MenuSource.AdventureCreator)
			{
				return field;
			}
			
			T newField = field;
			if (_constantID != 0)
			{
				newField = Serializer.returnComponent <T> (_constantID);
				if (newField != null && source == MenuSource.UnityUiInScene)
				{
					field = newField;
				}
				
				EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
				if (field == null && source == MenuSource.UnityUiInScene)
				{
					if (GUILayout.Button ("Search scenes", EditorStyles.miniButton))
					{
						AdvGame.FindObjectWithConstantID (_constantID);
					}
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.EndVertical ();
			}
			return field;
		}


		/**
		 * <summary>Gets the number of references the Menu makes to a global variable</summary>
		 * <param name = "varID">The global variable's ID number</param>
		 * <returns>The number of references the Menu makes to the variable</returns>
		 */
		public int GetVariableReferences (int varID)
		{
			int numFound = 0;
			foreach (MenuElement element in elements)
			{
				if (element != null)
				{
					numFound += element.GetVariableReferences (varID);
				}
			}
			return numFound;
		}

		#endif


		/**
		 * <summary>Checks if Unity UI is used for the Menu's display, rather than OnGUI.</summary>
		 * <returns>True if Unity UI is used for the Menu's display</returns>
		 */
		public bool IsUnityUI ()
		{
			if (menuSource == MenuSource.UnityUiPrefab || menuSource == MenuSource.UnityUiInScene)
			{
				return true;
			}
			return false;
		}


		/**
		 * Draws an outline around the Menu and the MenuElement subclasses it houses.
		 */
		public void DrawOutline (MenuElement _selectedElement)
		{
			DrawStraightLine.DrawBox(rect, Color.yellow, 1f, false, 1);
			foreach (MenuElement element in visibleElements)
			{
				if (element == _selectedElement)
				{
					element.DrawOutline (true, this);
				}
				{
					element.DrawOutline (false, this);
				}
			}
		}
		

		/**
		 * Begins the display of an OnGUI-based Menu.
		 */
		public void StartDisplay ()
		{
			if (isFading)
			{
				GUI.BeginGroup (new Rect (dragOffset.x + panOffset.x + GetRect ().x, dragOffset.y + panOffset.y + GetRect ().y, GetRect ().width * zoomAmount, GetRect ().height * zoomAmount));
			}
			else
			{
				GUI.BeginGroup (new Rect (dragOffset.x + GetRect ().x, dragOffset.y + GetRect ().y, GetRect ().width * zoomAmount, GetRect ().height * zoomAmount));
			}

			if (backgroundTexture)
			{
				Rect texRect = new Rect (0f, 0f, rect.width, rect.height);
				GUI.DrawTexture (texRect, backgroundTexture, ScaleMode.StretchToFill, true, 0f);
			}
		}
		

		/**
		 * Ends the display of an OnGUI-based Menu.
		 */
		public void EndDisplay ()
		{
			GUI.EndGroup ();
		}
	

		/**
		 * <summary>Sets the centre-point of a 3D Menu.</summary>
		 * <param name = "_position">The position in 3D space to place the Menu's centre.</param>
		 */
		public void SetCentre3D (Vector3 _position)
		{
			if (IsUnityUI ())
			{
				if (canvas != null && rectTransform != null && canvas.renderMode == RenderMode.WorldSpace)
				{
					rectTransform.transform.position = _position;
					hasMoved = true;
				}
				return;
			}

			SetCentre (new Vector2(_position.x, _position.y), false);
		}
		

		/**
		 * <summary>Sets the centre-point of a 2D Menu.</summary>
		 * <param name = "_position">The position in Screen Space to place the Menu's centre.</param>
		 * <param name="useAspectRatio">If True, the position co-ordinates are assumed to be relative to the aspect-ratio-corrected screen, as opposed to the entire game window</param>
		 */
		public void SetCentre (Vector2 _position, bool useAspectRatio = false)
		{
			if (useAspectRatio && KickStarter.settingsManager != null && !KickStarter.settingsManager.forceAspectRatio)
			{
				useAspectRatio = false;
			}

			hasMoved = true;

			if (IsUnityUI ())
			{
				if (canvas != null && rectTransform != null)
				{
					if (canvas.renderMode != RenderMode.WorldSpace)
					{
						if (useAspectRatio)
						{
							_position = KickStarter.mainCamera.CorrectScreenPositionForUnityUI (_position);
						}

						if (fitWithinScreen)
						{
							float minLeft = rectTransform.sizeDelta.x * (1f - rectTransform.pivot.x) * canvas.scaleFactor * rectTransform.localScale.x;
							float minTop = rectTransform.sizeDelta.y * (1f - rectTransform.pivot.y) * canvas.scaleFactor * rectTransform.localScale.y;
							
							float maxLeft = rectTransform.sizeDelta.x * rectTransform.pivot.x * canvas.scaleFactor * rectTransform.localScale.x;
							float maxTop = rectTransform.sizeDelta.y * rectTransform.pivot.y * canvas.scaleFactor * rectTransform.localScale.y;

							if (KickStarter.settingsManager.forceAspectRatio)
							{
								Vector2 windowViewportDifference = KickStarter.mainCamera.GetWindowViewportDifference ();

								minLeft += windowViewportDifference.x;
								maxLeft += windowViewportDifference.x;

								minTop += windowViewportDifference.y;
								maxTop += windowViewportDifference.y;
							}

							_position.x = Mathf.Clamp (_position.x, maxLeft, Screen.width - minLeft);
							_position.y = Mathf.Clamp (_position.y, maxTop, Screen.height - minTop);
						}

						if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
						{
							float scalerOffset = 1f;
							if (canvasScaler != null && canvasScaler.enabled && canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
							{
								switch (canvasScaler.screenMatchMode)
						        {
						        	case CanvasScaler.ScreenMatchMode.MatchWidthOrHeight:
										float match = canvasScaler.matchWidthOrHeight;
										scalerOffset = (Screen.width / canvasScaler.referenceResolution.x) * (1 - match) + (Screen.height / canvasScaler.referenceResolution.y) * match;
						        		break;

						        	case CanvasScaler.ScreenMatchMode.Expand:
										scalerOffset = Mathf.Min (Screen.width / canvasScaler.referenceResolution.x, Screen.height / canvasScaler.referenceResolution.y);
						        		break;

						        	case CanvasScaler.ScreenMatchMode.Shrink:
										scalerOffset = Mathf.Max (Screen.width / canvasScaler.referenceResolution.x, Screen.height / canvasScaler.referenceResolution.y);
						        		break;
						        }
						    }
							Vector3 localTargetPositionUI = new Vector3 ((_position.x - (Screen.width / 2f)) / scalerOffset, (_position.y - (Screen.height / 2f)) / scalerOffset, rectTransform.transform.localPosition.z);

							if (canDoSmoothing && !IsFading ())
							{
								localTargetPositionUI = Vector3.Lerp (rectTransform.transform.position, localTargetPositionUI, Time.deltaTime * 12f);
							}

							rectTransform.localPosition = localTargetPositionUI;
							return;
						}
					}

					Vector3 targetPositionUI = new Vector3 (_position.x, _position.y, rectTransform.transform.position.z);
					if (canDoSmoothing && !IsFading ())
					{
						targetPositionUI = Vector3.Lerp (rectTransform.transform.position, targetPositionUI, Time.deltaTime * 12f);
					}

					rectTransform.transform.position = targetPositionUI;
				}
				return;
			}

			Vector2 targetPosition = Vector2.zero;
			if (useAspectRatio)
			{
				Vector2 screenSize = AdvGame.GetMainGameViewSize (true);
				Vector2 screenOffset = AdvGame.GetMainGameViewOffset ();
				Vector2 centre = new Vector2 ((_position.x * screenSize.x) + screenOffset.x, (_position.y * screenSize.y) + screenOffset.y);
				targetPosition = new Vector2 (centre.x - (rect.width / 2), centre.y - (rect.height / 2));

				rect.x = centre.x - (rect.width / 2);
				rect.y = centre.y - (rect.height / 2);
			}
			else
			{
				Vector2 screenSize = AdvGame.GetMainGameViewSize (false);
				Vector2 centre = new Vector2 (_position.x * screenSize.x, _position.y * screenSize.y);
				targetPosition = new Vector2 (centre.x - (rect.width / 2), centre.y - (rect.height / 2));

				rect.x = centre.x - (rect.width / 2);
				rect.y = centre.y - (rect.height / 2);
			}

			rect.x = targetPosition.x;
			rect.y = targetPosition.y;

			FitMenuInsideScreen ();
		}


		private bool CanDoSmoothing (bool forGUI = false)
		{
			if ((!CanPause () || !pauseWhenEnabled) &&
					(forGUI ||
			   		(positionSmoothing && Application.isPlaying)))
			{
				/*if (menuSource == MenuSource.AdventureCreator)
				{
					if (positionType == AC_PositionType.AbovePlayer ||
						positionType == AC_PositionType.AboveSpeakingCharacter ||
						positionType == AC_PositionType.FollowCursor ||
						positionType == AC_PositionType.OnHotspot)
					{
						return true;
					}
				}
				else*/
				if (menuSource == MenuSource.UnityUiPrefab)
				{
					if (uiPositionType == UIPositionType.AbovePlayer ||
						uiPositionType == UIPositionType.AboveSpeakingCharacter ||
						//uiPositionType == UIPositionType.OnHotspot ||
						uiPositionType == UIPositionType.FollowCursor)
					{
						return true;
					}
				}
			}
			return false;
		}
		
		
		private Vector2 GetCentre ()
		{
			return new Vector2 (rect.x + (rect.width / 2), rect.y + (rect.height / 2));
		}
		
		
		private void FitMenuInsideScreen ()
		{
			if (positionType == AC_PositionType.Manual || positionType == AC_PositionType.FollowCursor || positionType == AC_PositionType.AppearAtCursorAndFreeze || positionType == AC_PositionType.OnHotspot || positionType == AC_PositionType.AboveSpeakingCharacter || positionType == AC_PositionType.AbovePlayer)
			{
				if (!fitWithinScreen)
				{
					return;
				}

				Vector2 screenSize = AdvGame.GetMainGameViewSize (true);
				Vector2 screenOffset = AdvGame.GetMainGameViewOffset();

				if (rect.x < screenOffset.x)
				{
					rect.x = screenOffset.x;
				}
				else
				{
					float maxRight = screenSize.x + screenOffset.x - rect.width;
					{
						if (rect.x > maxRight) rect.x = maxRight;
					}
				}

				if (rect.y < screenOffset.y)
				{
					rect.y = screenOffset.y;
				}
				else
				{
					float maxUp = screenSize.y + screenOffset.y - rect.height;
					if (rect.y > maxUp)
					{
						rect.y = maxUp;
					}
				}
			}
		}
		

		/**
		 * <summary>Aligns an OnGUI Menu to an area of the screen.</summary>
		 * <param name = "_anchor">The alignement to make</param>
		 */
		public void Align (TextAnchor _anchor)
		{
			Vector2 screenSize = AdvGame.GetMainGameViewSize(true);
			Vector2 screenOffset = AdvGame.GetMainGameViewOffset();

			// X
			if (_anchor == TextAnchor.LowerLeft || _anchor == TextAnchor.MiddleLeft || _anchor == TextAnchor.UpperLeft)
			{
				rect.x = screenOffset.x;
			}
			else if (_anchor == TextAnchor.LowerCenter || _anchor == TextAnchor.MiddleCenter || _anchor == TextAnchor.UpperCenter)
			{
				rect.x = (screenSize.x - rect.width) / 2 + screenOffset.x;
			}
			else
			{
				rect.x = screenSize.x - rect.width + screenOffset.x;
			}
			
			// Y
			if (_anchor == TextAnchor.LowerLeft || _anchor == TextAnchor.LowerCenter || _anchor == TextAnchor.LowerRight)
			{
				rect.y = screenSize.y - rect.height + screenOffset.y;
			}
			else if (_anchor == TextAnchor.MiddleLeft || _anchor == TextAnchor.MiddleCenter || _anchor == TextAnchor.MiddleRight)
			{
				rect.y = (screenSize.y - rect.height) / 2 + screenOffset.y;
			}
			else
			{
				rect.y = screenOffset.y;
			}
		}
		
		
		private void SetManualSize (Vector2 _size)
		{
			Vector2 screenSize = AdvGame.GetMainGameViewSize(true);
			
			rect.width = _size.x * screenSize.x;
			rect.height = _size.y * screenSize.y;
		}


		/**
		 * <summary>Checks if a point in Screen Space lies within the Menu's boundary.</summary>
		 * <param name = "_point">The point to check for</param>
		 * <returns>True if the point is within the Menu's boundary.</returns>
		 */
		public bool IsPointInside (Vector2 _point)
		{
			if (menuSource == MenuSource.AdventureCreator)
			{
				return GetRect ().Contains (_point);
			}
			else if (rectTransform != null && canvas != null)
			{
				if (ignoreMouseClicks && canvasGroup != null && !canvasGroup.interactable)
				{
					return false;
				}

				bool turnOffAgain = false;
				bool answer = false;
				if (!canvas.gameObject.activeSelf)
				{
					canvas.gameObject.SetActive (true);
					turnOffAgain = true;
				}

				if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					answer = RectTransformUtility.RectangleContainsScreenPoint (rectTransform, new Vector2 (_point.x, Screen.height - _point.y), null);
				}
				else
				{
					answer = RectTransformUtility.RectangleContainsScreenPoint (rectTransform, new Vector2 (_point.x, Screen.height - _point.y), canvas.worldCamera);
				}

				if (turnOffAgain)
				{
					canvas.gameObject.SetActive (false);
				}

				return answer;
			}
			return false;
		}
		

		/**
		 * <summary>Gets a Rect that describes an OnGUI Menu's boundary.</summary>
		 * <returns>A Rect that describes an OnGUI Menu's boundary.</returns>
		 */
		public Rect GetRect ()
		{
			if (!Application.isPlaying)
			{
				if (KickStarter.mainCamera)
				{
					return KickStarter.mainCamera.LimitMenuToAspect (rect);
				}
				return rect;
			}

			return rect;
		}
		

		/**
		 * <summary>Checks if a point in Screen Space within a specific slot of a specific MenuElement.</summary>
		 * <param name = "_element">The MenuElement to check for</param>
		 * <param name = "slot">The slot to check for</param>
		 * <param name = "_point">The point to check is within the MenuElement slot.</param>
		 * <returns>True if the point is within the boundary of the MenuElement slot</returns>
		 */
		public bool IsPointerOverSlot (MenuElement _element, int slot, Vector2 _point) 
		{
			if (menuSource == MenuSource.AdventureCreator)
			{
				Rect rectRelative = _element.GetSlotRectRelative (slot);
				Rect rectAbsolute = GetRectAbsolute (rectRelative);
				return (rectAbsolute.Contains (_point));
			}
			else if (canvas != null)
			{
				if (ignoreMouseClicks && canvasGroup != null && !canvasGroup.interactable)
				{
					return false;
				}

				RectTransform slotRectTransform = _element.GetRectTransform (slot);
				if (slotRectTransform != null)
				{
					if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
					{
						return RectTransformUtility.RectangleContainsScreenPoint (slotRectTransform, new Vector2 (_point.x, Screen.height - _point.y), null);
					}
					else
					{
						return RectTransformUtility.RectangleContainsScreenPoint (slotRectTransform, new Vector2 (_point.x, Screen.height - _point.y), canvas.worldCamera);
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Converts a Rect that's relative to an OnGUI Menu's boundary to Screen Space.</summary>
		 * <param name = "_rectRelative">The relative Rect to convert</param>
		 * <returns>The Rect converted to Screen Space co-ordinates</returns>
		 */
		public Rect GetRectAbsolute (Rect _rectRelative)
		{
			return (new Rect (_rectRelative.x + dragOffset.x + GetRect ().x, _rectRelative.y + dragOffset.y + GetRect ().y, _rectRelative.width, _rectRelative.height));
		}
		

		/**
		 * Re-populates the visibleElements List with MenuElement subclasses that are visible
		 */
		public void ResetVisibleElements ()
		{
			visibleElements.Clear ();
			foreach (MenuElement element in elements)
			{
				element.RecalculateSize (menuSource);

				if (element.IsVisible)
				{
					visibleElements.Add (element);
				}
			}
		}


		/**
		 * Refreshes any active MenuDialogList elements, after changing the state of dialogue options.
		 */
		public void RefreshDialogueOptions ()
		{
			bool doRecalc = false;
			if (!IsOff ())
			{
				foreach (MenuElement element in visibleElements)
				{
					if (element is MenuDialogList)
					{
						doRecalc = true;
					}
				}
			}

			if (doRecalc)
			{
				Recalculate ();
			}
		}
		

		/**
		 * Recalculates all position, size and display variables - accounting for hidden and re-sized elements.
		 * This should be called whenever a Menu's shape is changed.
		 */
		public void Recalculate ()
		{
			if (IsUnityUI ())
			{
				AutoResize ();
				return;
			}

			ResetVisibleElements ();
			PositionElements ();

			if (sizeType == AC_SizeType.Automatic)
			{
				AutoResize ();
			}
			else if (sizeType == AC_SizeType.Manual)
			{
				SetManualSize (new Vector2 (manualSize.x / 100f, manualSize.y / 100f));
			}
			else if (sizeType == AC_SizeType.AbsolutePixels)
			{
				rect.width = manualSize.x;
				rect.height = manualSize.y;
			}
			
			if (positionType == AC_PositionType.Centred)
			{
				Centre ();
				manualPosition = GetCentre ();
			}
			else if (positionType == AC_PositionType.Aligned)
			{
				Align (alignment);
				manualPosition = GetCentre ();
			}
			else if (positionType == AC_PositionType.Manual || !Application.isPlaying)
			{
				SetCentre (new Vector2 (manualPosition.x / 100f, manualPosition.y / 100f), true);
			}
		}
		

		/**
		 * Resizes a Menu that's size is dependent on the elements within it.
		 */
		public void AutoResize ()
		{
			visibleElements.Clear ();
			biggestElementSize = new Vector2 ();
			
			foreach (MenuElement element in elements)
			{
				if (element != null)
				{
					element.RecalculateSize (menuSource);

					if (element.IsVisible)
					{
						visibleElements.Add (element);

						if (menuSource == MenuSource.AdventureCreator)
						{
							if (element.GetSizeFromCorner ().x > biggestElementSize.x)
							{
								biggestElementSize.x = element.GetSizeFromCorner ().x;
							}
							
							if (element.GetSizeFromCorner ().y > biggestElementSize.y)
							{
								biggestElementSize.y = element.GetSizeFromCorner ().y;
							}
						}
					}
				}
			}

			if (menuSource == MenuSource.AdventureCreator)
			{
				Vector2 screenSize = AdvGame.GetMainGameViewSize(true);
			   
				rect.width = (spacing / 100f * screenSize.x) + biggestElementSize.x;
				rect.height = (spacing / 100f * screenSize.x) + biggestElementSize.y;
				manualSize = new Vector2(rect.width * 100f / screenSize.x, rect.height * 100f / screenSize.y);
			}
		}
		
		
		private void PositionElements ()
		{
			float totalLength = 0f;

			foreach (MenuElement element in visibleElements)
			{
				if (menuSource != MenuSource.AdventureCreator)
				{
					element.RecalculateSize (menuSource);
					return;
				}

				if (element == null)
				{
					ACDebug.Log ("Null element found");
					break;
				}
				
				if (element.positionType == AC_PositionType2.RelativeToMenuSize && sizeType == AC_SizeType.Automatic)
				{
					ACDebug.LogError ("Menu " + title + " cannot display because its size is Automatic, while its Element " + element.title + "'s Position is set to Relative");
					return;
				}

				if (element.positionType == AC_PositionType2.RelativeToMenuSize)
				{
					element.SetRelativePosition (new Vector2 (rect.width / 100f, rect.height / 100f));
				}
				else if (orientation == MenuOrientation.Horizontal)
				{
					if (element.positionType == AC_PositionType2.Aligned)
					{
						element.SetPosition (new Vector2 ((spacing / 100 * AdvGame.GetMainGameViewSize (true).x) + totalLength, (spacing / 100 * AdvGame.GetMainGameViewSize (true).x)));
					}
					
					totalLength += element.GetSize().x + (spacing / 100 * AdvGame.GetMainGameViewSize (true).x);
				}
				else
				{
					if (element.positionType == AC_PositionType2.Aligned)
					{
						element.SetPosition (new Vector2 ((spacing / 100 * AdvGame.GetMainGameViewSize (true).x), (spacing / 100 * AdvGame.GetMainGameViewSize (true).x) + totalLength));
					}
					
					totalLength += element.GetSize().y + (spacing / 100 * AdvGame.GetMainGameViewSize (true).x);
				}
			}
		}
		

		/**
		 * Positions an OnGUI Menu in the centre of the screen.
		 */
		public void Centre ()
		{
			SetCentre (new Vector2 (0.5f, 0.5f));
		}
		

		/**
		 * <summary>Checks if the Menu is currently enabled.</summary>
		 * <returns>True if the Menu is currently enabled.</return>
		 */
		public bool IsEnabled ()
		{
			if (isLocked)
			{
				if (isFading && fadeType == FadeType.fadeOut)
				{
					return isEnabled;
				}
				
				return false;
			}
			
			return (isEnabled);
		}
		

		/**
		 * <summary>Checks if the Menu is fully visible or not.</summary>
		 * <returns>True if the Menu is fully visible; False will be returned while midway through a transition.</returns>
		 */
		public bool IsVisible ()
		{
			if (transitionProgress >= 1f && isEnabled)
			{
				return true;
			}
			
			return false;
		}
		

		private void EndTransitionOn ()
		{
			transitionProgress = 1f;
			isEnabled = true;
			isFading = false;
		}
		
		
		private void EndTransitionOff ()
		{
			transitionProgress = 0f;
			isFading = false;
			isEnabled = false;
			SetAnimState ();
			ReturnGameState ();
			DisableUI ();
			ClearSpeechText ();

			KickStarter.playerMenus.CheckCrossfade (this);
		}
		

		/**
		 * <summary>Checks if the Menu is fully on or not.</summary>
		 * <returns>True if the Menu is fully on.</returns>
		 */
		public bool IsOn ()
		{
			if (!isLocked && isEnabled && !isFading)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the Menu is fully off or not.</summary>
		 * <returns>True if the Menu is fully off.</returns>
		 */
		public bool IsOff ()
		{
			if (isLocked)
			{
				return true;
			}
			if (!isEnabled)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the Menu transitions over time when being enabled or disabled.</summary>
		 * <returns>True if the Menu transitions over time</returns>
		 */
		public bool HasTransition ()
		{
			if (fadeSpeed <= 0f)
			{
				return false;
			}
			if (IsUnityUI ())
			{
				if (uiTransitionType != UITransition.None)
				{
					return true;
				}
			}
			else if (transitionType != MenuTransition.None)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the value of StateHandler's gameState variable at the point that the Menu was last turned on.</summary>
		 * <returns>The value of StateHandler's gameState variable at the point that the Menu was last turned on</returns>
		 */
		public GameState GetGameStateWhenTurnedOn ()
		{
			return gameStateWhenTurnedOn;
		}


		/**
		 * <summary>Checks if an element inside the Menu is selected by Unity UI's EventSystem.</summary>
		 * <param name = "elementIndex">The element's index in elements</param>
		 * <param name = "slotIndex">The element's slot index, if it has multiple slots</param>
		 * <returns>True if the element is selected by Unity UI's EventSystem.</returns>
		 */
		public bool IsElementSelectedByEventSystem (int elementIndex, int slotIndex)
		{
			if (menuSource != MenuSource.AdventureCreator)
			{	
				return elements[elementIndex].IsSelectedByEventSystem (slotIndex);
			}
			return false;
		}


		private void InitUIElements ()
		{
			if (IsUnityUI ())
			{
				int languageNumber = Options.GetLanguage ();

				for (int j=0; j<NumElements; j++)
				{
					if ((elements[j].GetNumSlots () == 0 || !elements[j].IsVisible))
					{
						elements[j].HideAllUISlots ();
					}

					for (int i=0; i<elements[j].GetNumSlots (); i++)
					{
						elements[j].PreDisplay (i, languageNumber, false);
					}
				}
			}
		}


		/**
		 * <summary>Turns the Menu on.</summary>
		 * <param name = "doFade">If True, the Menu will play its transition animation; otherwise, it will turn on instantly.</param>
		 * <returns>True if the Menu was turned on. False if the Menu was already on.</returns>
		 */
		public bool TurnOn (bool doFade = true)
		{
			if (IsOn ())
			{
				return false;
			}

			gameStateWhenTurnedOn = KickStarter.stateHandler.gameState;
			KickStarter.playerMenus.UpdateMenuPosition (this, KickStarter.playerInput.GetInvertedMouse (), true);

			if (!HasTransition ())
			{
				doFade = false;
			}

			// Setting selected_slot to -2 will cause PlayerInput's selected_option to reset
			if (isLocked)
			{
				#if UNITY_EDITOR
				ACDebug.Log ("Cannot turn on menu " + title + " as it is locked.");
				#endif
			}
			else if (!isEnabled || (isFading && fadeType == FadeType.fadeOut))
			{
				selected_slot = 0;
				selected_element = null;

				if (KickStarter.playerInput)
				{
					if (menuSource == MenuSource.AdventureCreator && positionType == AC_PositionType.AppearAtCursorAndFreeze)
					{
						SetCentre (new Vector2 ((KickStarter.playerInput.GetInvertedMouse ().x / Screen.width) + ((manualPosition.x - 50f) / 100f),
												(KickStarter.playerInput.GetInvertedMouse ().y / Screen.height) + ((manualPosition.y - 50f) / 100f)));
					}
					else if (menuSource != MenuSource.AdventureCreator && uiPositionType == UIPositionType.AppearAtCursorAndFreeze)
					{
						EnableUI (); // Necessary because scaling issues occur otherwise
						SetCentre (new Vector2 (KickStarter.playerInput.GetInvertedMouse ().x, Screen.height + 1f - KickStarter.playerInput.GetInvertedMouse ().y));
					}
				}


				MenuSystem.OnMenuEnable (this);
				foreach (MenuElement element in elements)
				{
					element.OnMenuTurnOn (this);
				}
				ChangeGameState ();
				Recalculate ();
				InitUIElements ();

				dragOffset = Vector2.zero;
				isEnabled = true;
				isFading = doFade;
				
				if (actionListOnTurnOn != null)
				{
					AdvGame.RunActionListAsset (actionListOnTurnOn);
				}
				EnableUI (); // Move here
				KickStarter.eventManager.Call_OnMenuTurnOn (this, !doFade);


				if (doFade && fadeSpeed > 0f)
				{
					fadeType = FadeType.fadeIn;
					fadeStartTime = Time.realtimeSinceStartup - (transitionProgress * fadeSpeed);
				}
				else
				{
					transitionProgress = 1f;
					isEnabled = true;
					isFading = false;

					if (IsUnityUI ())
					{
						UpdateTransition ();
					}
				}
				SetAnimState ();
			}
			return true;
		}


		/**
		 * <summary>Turns the Menu off.</summary>
		 * <param name = "doFade">If True, the Menu will play its transition animation; otherwise, it will turn off instantly.</param>
		 * <returns>True if the Menu was turned off. False if the Menu was already off.</returns>
		 */
		public bool TurnOff (bool doFade = true)
		{
			if (IsOff ())
			{
				return false;
			}

			bool canRunOffAsset = false;
			if (!IsFadingOut ())
			{
				canRunOffAsset = true;
			}
			
			if (appearType == AppearType.OnContainer)
			{
				KickStarter.playerInput.activeContainer = null;
			}
			else if (appearType == AppearType.OnViewDocument)
			{
				KickStarter.runtimeDocuments.CloseDocument ();
			}

			if (!HasTransition ())
			{
				doFade = false;
			}

			if (canRunOffAsset || !doFade)
			{
				KickStarter.eventManager.Call_OnMenuTurnOff (this, !doFade);
			}
			
			if (isEnabled && (!isFading || (isFading && fadeType == FadeType.fadeIn)))// && appearType == AppearType.OnHotspot)))
			{
				isFading = doFade;
				
				if (doFade && fadeSpeed > 0f)
				{
					fadeType = FadeType.fadeOut;
					fadeStartTime = Time.realtimeSinceStartup - ((1f - transitionProgress) * fadeSpeed);
					SetAnimState ();
				}
				else
				{
					transitionProgress = 0f;
					UpdateTransition ();
					isFading = false;
					isEnabled = false;
					ReturnGameState ();
					DisableUI ();
					ClearSpeechText ();
				}
			}

			if (canRunOffAsset)
			{
				if (actionListOnTurnOff != null)
				{
					AdvGame.RunActionListAsset (actionListOnTurnOff);
				}
				KickStarter.playerMenus.OnTurnOffMenu (this);
			}
			
			return true;
		}
		

		/**
		 * <summary>Forces the Menu off instantly.</summary>
		 * <param name = "ignoreActionList">If True, and actionListOnTurnOff is assigned, then it will not be run</param>
		 */
		public void ForceOff (bool ignoreActionList = false)
		{
			if (isEnabled || isFading)
			{
				if (!ignoreActionList && actionListOnTurnOff != null && !IsFadingOut ())
				{
					AdvGame.RunActionListAsset (actionListOnTurnOff);
				}

				transitionProgress = 0f;
				UpdateTransition ();
				isFading = false;
				isEnabled = false;
				DisableUI ();
				ClearSpeechText ();
				ReturnGameState ();
			}
		}


		/**
		 * <summary>Checks if the Menu is transitioning in.</summary>
		 * <returns>True if the Menu is transitioning in</returns>
		 */
		public bool IsFadingIn ()
		{
			if (isFading && fadeType == FadeType.fadeIn)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the Menu is transitioning out.</summary>
		 * <returns>True if the Menu is transitioning out</returns>
		 */
		public bool IsFadingOut ()
		{
			if (isFading && fadeType == FadeType.fadeOut)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the Menu is transitioning in or out.</summary>
		 * <returns>True if the Menu is transitioning in or out</returns>
		 */
		public bool IsFading ()
		{
			return isFading;
		}


		/**
		 * <summary>Gets the progression through the Menu's transition animation (0 = fully on, 1 = fully off)</summary>
		 * <returns>The progression through the Menu's transition animation</returns>
		 */
		public float GetFadeProgress ()
		{
			if (panMovement == PanMovement.Linear)
			{
				return (1f - transitionProgress);
			}
			else if (panMovement == PanMovement.Smooth)
			{
				return ((transitionProgress * transitionProgress) - (2 * transitionProgress) + 1);
			}
			else if (panMovement == PanMovement.CustomCurve)
			{
				float startTime = timeCurve [0].time;
				float endTime = timeCurve [timeCurve.length - 1].time;
				
				return 1f - timeCurve.Evaluate ((endTime - startTime) * transitionProgress);
			}
			return 0f;
		}


		/**
		 * Updates the transition animation.
		 * This is called every frame by PlayerMenus.
		 */
		public void HandleTransition ()
		{
			if (isFading && isEnabled)
			{
				if (fadeType == FadeType.fadeIn)
				{
					transitionProgress = ((Time.realtimeSinceStartup - fadeStartTime) / fadeSpeed);
					
					if (transitionProgress > 1f)
					{
						transitionProgress = 1f;
						UpdateTransition ();
						EndTransitionOn ();
						return;
					}
					else
					{
						UpdateTransition ();
					}
				}
				else
				{
					transitionProgress = 1f - ((Time.realtimeSinceStartup - fadeStartTime) / fadeSpeed);
					if (transitionProgress < 0f)
					{
						transitionProgress = 0f;
						UpdateTransition ();
						EndTransitionOff ();
						return;
					}
					else
					{
						UpdateTransition ();
					}
				}
			}
		}
		

		private void UpdateTransition ()
		{
			if (IsUnityUI ())
			{
				if (uiTransitionType == UITransition.CanvasGroupFade && canvasGroup != null && fadeSpeed > 0f)
				{
					canvasGroup.alpha = 1f - GetFadeProgress ();
				}
				return;
			}

			if (transitionType == MenuTransition.Fade)
			{
				return;
			}
			
			if (transitionType == MenuTransition.FadeAndPan || transitionType == MenuTransition.Pan)
			{
				float amount = GetFadeProgress () * panDistance;

				if (panDirection == PanDirection.Down)
				{
					panOffset = new Vector2 (0f, amount);
				}
				else if (panDirection == PanDirection.Left)
				{
					panOffset = new Vector2 (-amount, 0f);
				}
				else if (panDirection == PanDirection.Up)
				{
					panOffset = new Vector2 (0f, -amount);
				}
				else if (panDirection == PanDirection.Right)
				{
					panOffset = new Vector2 (amount, 0f);
				}
				
				panOffset = new Vector2 (panOffset.x * AdvGame.GetMainGameViewSize (true).x, panOffset.y * AdvGame.GetMainGameViewSize (true).y);
			}
			
			else if (transitionType == MenuTransition.Zoom)
			{
				//zoomAmount = transitionProgress;
				zoomAmount = 1f - GetFadeProgress ();
				
				if (zoomAnchor == TextAnchor.UpperLeft)
				{
					panOffset = Vector2.zero;
				}
				else if (zoomAnchor == TextAnchor.UpperCenter)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width / 2f, 0f);
				}
				else if (zoomAnchor == TextAnchor.UpperRight)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width, 0f);
				}
				else if (zoomAnchor == TextAnchor.MiddleLeft)
				{
					panOffset = new Vector2 (0f, (1f - zoomAmount) * rect.height / 2f);
				}
				else if (zoomAnchor == TextAnchor.MiddleCenter)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width / 2f, (1f - zoomAmount) * rect.height / 2f);
				}
				else if (zoomAnchor == TextAnchor.MiddleRight)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width, (1f - zoomAmount) * rect.height / 2f);
				}
				else if (zoomAnchor == TextAnchor.LowerLeft)
				{
					panOffset = new Vector2 (0, (1f - zoomAmount) * rect.height);
				}
				else if (zoomAnchor == TextAnchor.LowerCenter)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width / 2f, (1f - zoomAmount) * rect.height);
				}
				else if (zoomAnchor == TextAnchor.LowerRight)
				{
					panOffset = new Vector2 ((1f - zoomAmount) * rect.width, (1f - zoomAmount) * rect.height);
				}
			}
		}


		/**
		 * Pauses the game if appropriate after a scene-change.
		 */
		public void AfterSceneChange ()
		{
			if (menuSource == MenuSource.UnityUiInScene)
			{
				LoadUnityUI ();
				Initalise ();
			}
			else if (menuSource == MenuSource.UnityUiPrefab)
			{
				SetParent ();
				if (canvas != null && canvas.worldCamera == null)
				{
					canvas.worldCamera = Camera.main;
				}
			}

			if (IsOn ())
			{
				ChangeGameState ();
			}
		}
		
		
		private void ChangeGameState ()
		{
			if (IsBlocking () && Application.isPlaying)
			{
				if (appearType != AppearType.OnInteraction)
				{
					KickStarter.playerInteraction.DeselectHotspot (true);
				}
				KickStarter.mainCamera.FadeIn (0);
				KickStarter.mainCamera.PauseGame (true);
			}
		}
		
		
		private void ReturnGameState ()
		{
			if (IsBlocking () && !KickStarter.playerMenus.ArePauseMenusOn (this) && Application.isPlaying)
			{
				KickStarter.stateHandler.RestoreLastNonPausedState ();
			}
		}


		/**
		 * <summary>Checks if the Menu's appearType is such that the pauseWhenEnabled option is valid.</summary>
		 * <returns>True if the Menu's appearType is such that the pauseWhenEnabled option is valid.</returns>
		 */
		public bool CanPause ()
		{
			switch (appearType)
			{
				case AppearType.Manual:
				case AppearType.OnInputKey:
				case AppearType.OnInteraction:
				case AppearType.OnContainer:
				case AppearType.MouseOver:
				case AppearType.OnViewDocument:
					return true;

				default:
					return false;
			}
		}


		/**
		 * <summary>If True, the Menu is currently clickable.</summary>
		 * <returns>True if the Menu is currently clickable</returns>
		 */
		public bool IsClickable ()
		{
			if (ignoreMouseClicks)
			{
				return false;
			}

			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				if (canClickInCutscene && ShowClickInCutscenesOption ())
				{
					return true;
				}
				return false;
			}

			return true;
		}


		/**
		 * <summary>If True, the Menu is clickable during Cutscenes.</summary>
		 * <returns>True if the Menu is clickable during Cutscenes.</returns>
		 */
		public bool CanClickInCutscenes ()
		{
			if (ShowClickInCutscenesOption () && !ignoreMouseClicks && canClickInCutscene)
			{
				return true;
			}
			return false;
		}


		private bool ShowClickInCutscenesOption ()
		{
			if (appearType == AppearType.WhenSpeechPlays || appearType == AppearType.DuringConversation || 
				appearType == AppearType.Manual || appearType == AppearType.WhenSpeechPlays ||
				appearType == AppearType.DuringCutscene)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the Menu will pause gameplay when enabled.</summary>
		 * <returns>True if the Menu will pause gameplay when enabled.</returns>
		 */
		public bool IsBlocking ()
		{
			if (pauseWhenEnabled && CanPause ())
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the Menu's enabled state is controlled by either the player or by Actions.</summary>
		 * <returns>True if the Menu's enabled state is controlled by either the player or by Actions.</returns>
		 */
		public bool IsManualControlled ()
		{
			if (appearType == AppearType.Manual || appearType == AppearType.OnInputKey || appearType == AppearType.OnContainer || appearType == AppearType.OnViewDocument)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Recalculates a Menu's display for a particular Hotspot.</summary>
		 * <param name = "hotspot">The Hotspot to recalculate the Menu's display for</param>
		 * <param name = "includeInventory">If True, then InventoryBox elements will also be displayed when appropriate</param>
		 */
		public void MatchInteractions (Hotspot hotspot, bool includeInventory)
		{
			forHotspot = hotspot;
			forItem = null;

			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					if (KickStarter.settingsManager.autoHideInteractionIcons)
					{
						MenuInteraction interaction = (MenuInteraction) element;
						interaction.MatchInteractions (hotspot.useButtons);
					}
				}
				else if (element is MenuInventoryBox)
				{
					if (includeInventory)
					{
						element.RecalculateSize (menuSource);
						Recalculate ();
						element.AutoSetVisibility ();
					}
					else
					{
						element.IsVisible = false;
					}
				}
			}
			
			Recalculate ();
			Recalculate ();
		}
		

		/**
		 * <summary>Recalculates a Menu's display for a particular inventory item.</summary>
		 * <param name = "buttons">The InvItem to recalculate the Menus's display for</param>
		 * <param name = "includeInventory">If True, then InventoryBox elements will also be displayed when appropriate</param>
		 */
		public void MatchInteractions (InvItem item, bool includeInventory)
		{
			forHotspot = null;
			forItem = item;

			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					if (KickStarter.settingsManager.autoHideInteractionIcons)
					{
						MenuInteraction interaction = (MenuInteraction) element;
						interaction.MatchInteractions (item);
					}
				}
				else if (element is MenuInventoryBox)
				{
					if (includeInventory)
					{
						element.RecalculateSize (menuSource);
						Recalculate ();
						element.AutoSetVisibility ();
					}
					else
					{
						element.IsVisible = false;
					}
				}
			}
			
			Recalculate ();
			Recalculate ();
		}
		

		/**
		 * <summary>Recalculates a Menu's display for an "Examine" Hotspot Button.</summary>
		 * <param name = "button">A Button class to recalculate the Menus's display for</param>
		 */
		public void MatchLookInteraction (Button button)
		{
			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					MenuInteraction interaction = (MenuInteraction) element;
					interaction.MatchLookInteraction (KickStarter.cursorManager.lookCursor_ID);
				}
			}
		}
		

		/**
		 * <summary>Recalculates a Menu's display for an "Use" Hotspot Button.</summary>
		 * <param name = "button">A Button class to recalculate the Menus's display for</param>
		 */
		public void MatchUseInteraction (Button button)
		{
			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					MenuInteraction interaction = (MenuInteraction) element;
					interaction.MatchUseInteraction (button);
				}
			}
		}


		/**
		 * Hides all MenuInteraction elements within the Menu.
		 */
		public void HideInteractions ()
		{
			foreach (MenuElement element in elements)
			{
				if (element is MenuInteraction)
				{
					element.IsVisible = false;
					element.isClickable = false; // This function is only called for Context-Sensitive anyway
				}
			}
		}
		

		/**
		 * <summary>Offsets an OnGUI Menu's position when dragged by a MenuDrag element.</summary>
		 * <param name = "pos">The amoung to offset the position by</param>
		 * <param name = "dragRect">The boundary limit to keep the Menu within</param>
		 */
		public void SetDragOffset (Vector2 pos, Rect dragRect)
		{
			if (pos.x < dragRect.x)
			{
				pos.x = dragRect.x;
			}
			else if (pos.x > (dragRect.x + dragRect.width - GetRect ().width))
			{
				pos.x = dragRect.x + dragRect.width - GetRect ().width;
			}
			
			if (pos.y < dragRect.y)
			{
				pos.y = dragRect.y;
			}
			else if (pos.y > (dragRect.y + dragRect.height - GetRect ().height))
			{
				pos.y = dragRect.y + dragRect.height - GetRect ().height;
			}
			
			dragOffset = pos;
		}
		

		/**
		 * <summary>Gets the drag offset.</summary>
		 * <returns>The drag offset</returns>
		 */
		public Vector2 GetDragStart ()
		{
			return dragOffset;
		}
		

		/**
		 * Gets the zoom factor of MenuElements when a Menu is zooming
		 */
		public float GetZoom ()
		{
			if (!IsUnityUI () && transitionType == MenuTransition.Zoom && zoomElements)
			{
				return zoomAmount;
			}
			return 1f;
		}


		/*
		 * <summary>Checks if the Menu can be controlled with the keyboard or controller at this time.</summary>
		 * <returns>True if the Menu can be controlled with the keyboard or controller at this time.</returns>
		 */
		public bool CanCurrentlyKeyboardControl ()
		{
			if (ignoreMouseClicks)
			{
				return false;
			}

			if (menuSource != MenuSource.AdventureCreator || KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
			{
				if ((KickStarter.stateHandler.gameState == GameState.Paused && IsBlocking () && KickStarter.menuManager.keyboardControlWhenPaused) ||
					(KickStarter.stateHandler.gameState == GameState.DialogOptions && appearType == AppearType.DuringConversation && KickStarter.menuManager.keyboardControlWhenDialogOptions) ||
					(KickStarter.stateHandler.gameState == GameState.Cutscene && CanClickInCutscenes ()) ||
					(KickStarter.stateHandler.IsInGameplay () && KickStarter.playerInput.canKeyboardControlMenusDuringGameplay && CanPause () && !pauseWhenEnabled))
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Selects a given element (and optionally, a slot inside it) for direct-controlled menu navigation.</summary>
		 * <param name = "elementName">The name of the MenuElement to select</param>
		 * <param name = "slotIndex">The index number of the slot to select, if the MenuElement has multiple slots</param>
		 */
		public void Select (string elementName, int slotIndex = 0)
		{
			MenuElement elementToSelect = GetElementWithName (elementName);

			if (elementToSelect != null)
			{
				if (elementToSelect.IsVisible)
				{
					selected_element = elementToSelect;
					selected_slot = slotIndex;

					if (IsUnityUI () && IsEnabled ())
					{
						GameObject elementObject = selected_element.GetObjectToSelect (selected_slot);
						if (elementObject != null)
						{
							KickStarter.playerMenus.SelectUIElement (elementObject);
						}
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot select element '" + elementName + "' inside Menu '" + title + "' because it is not visible!");
				}
			}
			else
			{
				ACDebug.LogWarning ("Cannot find element '" + elementName + "' inside Menu '" + title + "'");
			}
		}


		/**
		 * Makes sure an element or slots is selected, ready for direct-controlled menu navigation.
		 * If the Menu has just been turned on, then the first visible element will be selected
		 * If an element was selected, but made invisible, then the slot or element closest to it will be selected.
		 */
		public void AutoSelect ()
		{
			if (visibleElements == null || visibleElements.Count == 0 || menuSource != MenuSource.AdventureCreator) return;

			if (selected_element != null)
			{
				if (!selected_element.IsVisible)
				{
					GetNearestSlot (selected_element, selected_slot);
				}
			}
			else
			{
				// No element selected, so select first-visible one
				for (int i=0; i<visibleElements.Count; i++)
				{
					if (visibleElements[i].isClickable)
					{
						selected_element = visibleElements[i];
						break;
					}
				}
			}

		}


		/**
		 * <summary>Attempts to select a new element or slot in a given direction.  This is used for direct-controlled menu navigation</summary>
		 * <param name = "inputDirection">The direction to move the selection in</param>
		 * <param name = "scrollingLocked">If True, don't change the selection (but still call this in case changing e.g. MenuSlider values)</param>
		 * <returns>True if a new element or slot was changed</returns>
		 */
		public bool GetNextSlot (Vector2 inputDirection, bool scrollingLocked)
		{
			if (menuSource != MenuSource.AdventureCreator) return false;

			if (inputDirection.y > 0.1f)
			{
				// Up
				GetNextSlot (Vector2.down, scrollingLocked, selected_element, selected_slot);
				return true;
			}
			else if (inputDirection.y < -0.1f)
			{
				// Down
				GetNextSlot (Vector2.up, scrollingLocked, selected_element, selected_slot);
				return true;
			}
			if (inputDirection.x < -0.1f)
			{
				// Left
				GetNextSlot (Vector2.left, scrollingLocked, selected_element, selected_slot);
				return true;
			}
			else if (inputDirection.x > 0.1f)
			{
				// Right
				GetNextSlot (Vector2.right, scrollingLocked, selected_element, selected_slot);
				return true;
			}
			return false;
		}


		private void GetNextSlot (Vector2 direction, bool scrollingLocked, MenuElement currentElement, int currentSlotIndex)
		{
			if (currentElement == null) return;

			if (currentElement is MenuSlider)
			{
				MenuSlider menuSlider = currentElement as MenuSlider;
				if (menuSlider.KeyboardControl (direction))
				{
					return;
				}
			}

			if (scrollingLocked)
			{
				return;
			}

			Vector2 thisCentre = GetRectAbsolute (currentElement.GetSlotRectRelative (currentSlotIndex)).center;

			MenuElement nextElement = currentElement;
			int nextSlotIndex = currentSlotIndex;

			float scaledDP = -1f;
			foreach (MenuElement element in visibleElements)
			{
				Vector2[] elementCentres = element.GetSlotCentres (this);

				if (elementCentres != null)
				{
					for (int i=0; i<elementCentres.Length; i++)
					{
						Vector2 relative = elementCentres[i] - thisCentre;
						float dotProduct = Vector2.Dot (relative, direction);
						Vector2 normalVec = Quaternion.Euler (0, 0, 90f) * direction;
						float normalDotProduct = Vector2.Dot (relative, normalVec);
						float thisScaledDP = dotProduct / relative.sqrMagnitude;

						//Debug.Log ("Compare " + currentElement.title + ", " + currentSlotIndex + " with " + element.title + ", " + i +
						//	", Test: " + scaledDP + ", TempTest: " + thisScaledDP);
						
						if (dotProduct > 0f && Mathf.Abs (dotProduct) > Mathf.Abs (normalDotProduct / 2f))
						{
							float dist = relative.sqrMagnitude;
							if (!Mathf.Approximately (dist, 0f) && (thisScaledDP > scaledDP || scaledDP < 0f))
							{
								nextElement = element;
								nextSlotIndex = i;
								scaledDP = thisScaledDP;
								//Debug.LogWarning (nextElement.title + " wins");
							}
						}
					}
				}
			}

			selected_slot = nextSlotIndex;
			selected_element = nextElement;
		}


		private void GetNearestSlot (MenuElement currentElement, int currentSlotIndex)
		{
			if (currentElement == null) return;

			Vector2 thisCentre = GetRectAbsolute (currentElement.GetSlotRectRelative (currentSlotIndex)).center;

			MenuElement nextElement = currentElement;
			int nextSlotIndex = currentSlotIndex;

			float minSqrMag = -1f;
			foreach (MenuElement element in visibleElements)
			{
				Vector2[] elementCentres = element.GetSlotCentres (this);

				if (elementCentres != null)
				{
					for (int i=0; i<elementCentres.Length; i++)
					{
						float thisSqrMag = (elementCentres[i] - thisCentre).sqrMagnitude;
						if (thisSqrMag < minSqrMag || minSqrMag < 0f)
						{
							nextElement = element;
							nextSlotIndex = i;
							minSqrMag = thisSqrMag;
						}
					}
				}
			}
			selected_slot = nextSlotIndex;
			selected_element = nextElement;
		}


		/**
		 * <summary>Gets a MenuElement subclass within the Menu's list of elements.</summary>
		 * <param name = "menuElementName">The title of the MenuElement to get</param>
		 * <returns>The MenuElement subclass</returns>
		 */
		public MenuElement GetElementWithName (string menuElementName)
		{
			foreach (MenuElement menuElement in elements)
			{
				if (menuElement.title == menuElementName)
				{
					return menuElement;
				}
			}
			
			return null;
		}
		

		/**
		 * <summary>Gets the centre-point of a MenuElement slot, in Screen Space.</summary>
		 * <param name = "_element">The MenuElement that the slot is in</param>
		 * <param name = "slot">The slot to reference, by index number</param>
		 * <returns>The centre-point of the MenuElement slot</returns>
		 */
		public Vector2 GetSlotCentre (MenuElement _element, int slot)
		{
			foreach (MenuElement menuElement in elements)
			{
				if (menuElement == _element)
				{
					if (IsUnityUI ())
					{
						Vector3 _position = menuElement.GetRectTransform (slot).position;
						if (canvas.renderMode != RenderMode.WorldSpace)
						{
							return new Vector2 (_position.x, Screen.height - _position.y);
						}
						return Camera.main.WorldToScreenPoint (_position);
					}

					Rect slotRect = _element.GetSlotRectRelative (slot);
					return new Vector2 (GetRect ().x + slotRect.x + (slotRect.width / 2f), GetRect ().y + slotRect.y + (slotRect.height / 2f));
				}
			}
			
			return Vector2.zero;
		}


		private void ClearSpeechText ()
		{
			foreach (MenuElement element in elements)
			{
				element.ClearSpeech ();
			}
		}


		/**
		 * <summary>Assigns the Menu, and all MenuElement classes within it, to a Hotspot.</summary>
		 * <param name = "_speech">The Speech line to assign to</param>
		 */
		public void SetHotspot (Hotspot _hotspot, InvItem _invItem)
		{
			forHotspot = _hotspot;
			forItem = _invItem;
			foreach (MenuElement element in elements)
			{
				element.SetHotspot (_hotspot, _invItem);
			}
		}


		/**
		 * <summary>Assigns the Menu, and all MenuElement classes within it, to a Speech line.</summary>
		 * <param name = "_speech">The Speech line to assign to</param>
		 */
		public void SetSpeech (Speech _speech)
		{
			speech = _speech;
			foreach (MenuElement element in elements)
			{
				element.SetSpeech (_speech);
			}
		}


		/**
		 * <summary>Gets the GameObject of the first-selected MenuElement, for a Unity UI-based Menu.</summary>
		 * <returns>The GameObject of the first-selected MenuElement</returns>
		 */
		public GameObject GetObjectToSelect ()
		{
			if (autoSelectFirstVisibleElement)
			{
				foreach (MenuElement element in visibleElements)
				{
					if (element.IsVisible)
					{
						GameObject objectToSelect = element.GetObjectToSelect ();
						if (objectToSelect != null)
						{
							return objectToSelect;
						}
					}
				}
			}
			else
			{
				if (string.IsNullOrEmpty (firstSelectedElement))
				{
					return null;
				}
				foreach (MenuElement element in visibleElements)
				{
					if (element.title == firstSelectedElement)
					{
						return element.GetObjectToSelect ();
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the inventory item that an interaction Menu was recalculated for.</summary>
		 * <returns>The InvItem that an interaction Menu was recalculated for</returns>
		 */
		public InvItem GetTargetInvItem ()
		{
			return forItem;
		}


		/**
		 * <summary>Gets the Hotspot that an interaction Menu was recalculated for.</summary>
		 * <returns>The Hotspot that an interaction Menu was recalculated for</returns>
		 */
		public Hotspot GetTargetHotspot ()
		{
			return forHotspot;
		}


		/**
		 * <summary>Prepares the Menu for a screenshot by disabling the canvas if it has one.</summary>
		 */
		public void PreScreenshotBackup ()
		{
			if (menuSource != MenuSource.AdventureCreator && canvas != null)
			{
				isDisabledForScreenshot = (hideDuringSaveScreenshots && canvas.gameObject.activeSelf);
				if (isDisabledForScreenshot)
				{
					canvas.gameObject.SetActive (false);
				}
			}
		}


		/**
		 * <summary>Re-enables the Menu's canvas if it was disabled to take a screenshot.</summary>
		 */
		public void PostScreenshotBackup ()
		{
			if (menuSource != MenuSource.AdventureCreator && canvas != null)
			{
				if (isDisabledForScreenshot)
				{
					canvas.gameObject.SetActive (true);
				}
			}
		}


		/**
		 * <summary>Checks if the Menu should be automatically turned off when loading a save game, instead of loaded.  This is only True if the Menu is manually-controlled and contains a SavesList element.</summary>
		 * <returns>True if the Menu should be automatically turned off when loading a save game, instead of loaded.</summary>
		 */
		public bool ShouldTurnOffWhenLoading ()
		{
			if (IsManualControlled ())
			{
				foreach (MenuElement element in elements)
				{
					if (element is MenuSavesList)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * The Menu's id number as a string.
		 */
		public string IDString
		{
			get
			{
				return idString;
			}
		}


		/**
		 * The Menu's id number, which is a unique identifier.
		 */
		public int ID
		{
			set
			{
				id = value;
				idString = id.ToString ();
			}
		}


		/**
		 * True if the Menu has been repositioned
		 */
		public bool HasMoved
		{
			get
			{
				return hasMoved;
			}
		}


		/**
		 * The number of elements in the Menu.
		 */
		public int NumElements
		{
			get
			{
				if (elementCount <= 0)
				{
					elementCount = elements.Count;
				}
				return elementCount;
			}
		}

	}
	
}