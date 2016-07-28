class QuickUI extends EditorWindow {
	
	var textures : Object;
	var style : GUIStyle;
	var currentWidth : float;
	
	var selImages : Texture2D[] = [];
	var afbeelding01 : Texture;
	
	var gridContents : GUIContent[];
	var selGridInt : int = 0;
	var scrollPosition : Vector2 = Vector2.zero;
	
	var titleString : String;
	var subtitleString : String;
	var optionsHeight : int;
	var optionsScrollHeight : int;
	
	var radius : int;
	var squareRadius : int;
	var squareRadiusActive : boolean;
	
	var arrowRad : int;
	var arrowRadius : int;
	var arrowRadiusActive : boolean;
	
	var labelRad : int;
	var labelRadius : int;
	var labelRadiusActive : boolean;
	
	var selectedStyle;
	private var currentMenu : int = 0;
	
	static var MainUIColor : Color = Color.white;
	static var MainUIPosition : Vector3;
	static var MainUIScale : float = 0.5;
	
	static var font : String;
	
	@MenuItem("Window/QuickUI")
	static function Init() {
		if(EditorPrefs.GetInt("FirstRun") != 1) {
			EditorPrefs.SetInt("CurrentStep", 0);
			var tutorial = EditorWindow.GetWindow(tutorial, true, "Tutorial");
			tutorial.position = Rect(300, 140, 510, 230);
			tutorial.maxSize = new Vector2(510, 230);
			tutorial.minSize = new Vector2(510, 230);
		} else {
			var window = GetWindow(QuickUI);
		
			window.position = Rect(30, 30, 290, 600);
			window.maxSize = new Vector2(290, 600);
			window.minSize = new Vector2(290, 300);
			window.Show();
		}
	}
	
	function Start() {
		selectedStyle = GUI.skin.button.onNormal.background;
	}
	
	function OnGUI() {
		currentWidth = position.width/2;
		GUI.backgroundColor = ConvertColor(34, 44, 54);
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 30;
		GUI.skin.label.alignment = TextAnchor.UpperCenter;
		GUI.skin.button.onNormal.background = selectedStyle;
		if(currentMenu == 0) {
			gridContents = new GUIContent[6];
			for(i = 0; i < 6; i++) {
				gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Main/" + i));
			}
			titleString = "Categories";
			subtitleString = "";
			optionsHeight = 320;
			optionsScrollHeight = 348;
		}
		
		GUI.Box(new Rect(0, 0, 9999, 9999), "");
		textures = Resources.Load("QuickUI/Textures/circle");
		
		GUI.backgroundColor = Color.white;
		GUI.Label (Rect (currentWidth-110, 10, 220, 50), titleString);
		GUI.skin.label.fontSize = 13;
		GUI.Label (Rect (currentWidth-80, 42, 160, 50), subtitleString);
		GUI.skin.label.fontSize = 30;
		
		scrollPosition = GUI.BeginScrollView (Rect (20, 60, position.width, position.height), scrollPosition, Rect (0, 0, 0, optionsScrollHeight), GUIStyle.none, GUIStyle.none);
		
		selGridInt = GUI.SelectionGrid(Rect(0, 0, position.width - 40, optionsHeight), selGridInt, gridContents, 2);
		
		GUI.EndScrollView ();
		GUI.backgroundColor = Color.clear;
		if(currentMenu > 0) {
			if (GUI.Button(Rect(0, 0, 40, 40), Resources.Load("QuickUI/Icons/back"))) {
				if(squareRadiusActive == true) {
					currentMenu = 0;
					selGridInt = 1;
				} else if(arrowRadiusActive == true) {
					currentMenu = 0;
					selGridInt = 3;
				} else if(labelRadiusActive == true) {
					currentMenu = 0;
					selGridInt = 4;
				} else {
					currentMenu = 0;
					selGridInt = 16;
				}
				
			}
		} else {
			if (GUI.Button(Rect(position.width - 37, 0, 35, 35), Resources.Load("QuickUI/Icons/mail"))) {
				Application.OpenURL("mailto:quickui@thomashopstaken.com?subject=QuickUI%20Feedback");	
			}
		}
		
		GUI.backgroundColor = Color.white;
		if (GUI.changed) {
			var e : Event = Event.current;
			if (e.alt && currentMenu != 0) {
				if(squareRadiusActive == true || arrowRadiusActive == true || labelRadiusActive == true || currentMenu == 3 || currentMenu == 1) {
					var optionsMenu = EditorWindow.GetWindow(optionsMenu, true, "Options");
					optionsMenu.position = Rect(500, 140, 290, 600);
					optionsMenu.maxSize = new Vector2(290, 300);
					optionsMenu.minSize = new Vector2(290, 300);
				}
				
			} else {
				switch (currentMenu) {
					case 0:
						CheckMenuButtons(selGridInt);
						break;
					case 1:
						AddCircles(selGridInt);
						break;
					case 2:
						ChooseSquareRadius(selGridInt);
						break;
					case 3:
						AddRoundButton(selGridInt);
						break;
					case 4:
						ChooseArrowRadius(selGridInt);
						break;
					case 5:
						ChooseLabelRadius(selGridInt);
						break;
					case 6:
						AddText(selGridInt);
						break;
				}
			}
		}
	}
	
	function CheckMenuButtons(menuItem : int) {
		switch (menuItem) {
		    case 0:
		    	currentMenu = 1;
		    	titleString = "Cricles";
		    	subtitleString = "";
		    	optionsHeight = 1000;
				optionsScrollHeight = 1070;
				squareRadiusActive = false;
				arrowRadiusActive = false;
		        ChangeMenu();
		        GUI.skin.button.onNormal.background = GUI.skin.button.normal.background;
		        break;
		    case 1:
		    	currentMenu = 2;
		    	titleString = "Squares";
		    	subtitleString = "select corner radius";
		    	optionsHeight = 800;
				optionsScrollHeight = 870;
				squareRadiusActive = false;
				arrowRadiusActive = false;
		        ChangeMenu();
		        GUI.skin.button.onNormal.background = GUI.skin.button.normal.background;
		       	break;
		    case 2:
				currentMenu = 3;
		    	titleString = "Round Buttons";
		    	subtitleString = "";
		    	optionsHeight = 1000;
				optionsScrollHeight = 1070;
				squareRadiusActive = false;
				arrowRadiusActive = false;
				labelRadiusActive = false;
		        ChangeMenu();
		        GUI.skin.button.onNormal.background = GUI.skin.button.normal.background;
		       	break;
		    case 3:
				currentMenu = 4;
		    	titleString = "Arrows";
		    	subtitleString = "select corner radius";
		    	optionsHeight = 800;
				optionsScrollHeight = 870;
				squareRadiusActive = false;
				arrowRadiusActive = false;
				labelRadiusActive = false;
		        ChangeMenu();
		        GUI.skin.button.onNormal.background = GUI.skin.button.normal.background;
		       	break;
			case 4:
				currentMenu = 5;
		    	titleString = "Labels";
		    	subtitleString = "select corner radius";
		    	optionsHeight = 800;
				optionsScrollHeight = 870;
				squareRadiusActive = false;
				arrowRadiusActive = false;
				labelRadiusActive = false;
		        ChangeMenu();
		        GUI.skin.button.onNormal.background = GUI.skin.button.normal.background;
		       	break;
		    case 5:
		    	currentMenu = 6;
		    	titleString = "Text";
		    	subtitleString = "select a font";
		    	optionsHeight = 308;
				optionsScrollHeight = 335;
				squareRadiusActive = false;
				arrowRadiusActive = false;
		        ChangeMenu();
		        GUI.skin.button.onNormal.background = GUI.skin.button.normal.background;
		        break;
		}
	}
	
	function ChangeMenu() {
		switch (currentMenu) {
		    case 1:
		    	gridContents = new GUIContent[19];
		        for(i = 0; i < 19; i++) {
					gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Circle/" + i));
				}
		        break;
		    case 2:
		    	gridContents = new GUIContent[13];
		        for(i = 0; i < 13; i++) {
					gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Square/" + i));
				}
		        break;
		    case 3:
		    	gridContents = new GUIContent[17];
		        for(i = 0; i < 17; i++) {
					gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/RoundButton/" + i));
				}
		        break;
		    case 4:
		    	gridContents = new GUIContent[13];
		        for(i = 0; i < 13; i++) {
					gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Arrow/" + i));
				}
		        break;
		    case 5:
		    	gridContents = new GUIContent[12];
		        for(i = 0; i < 12; i++) {
					gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Label/" + i));
				}
		        break;
		    case 6:
		    	gridContents = new GUIContent[5];
		        for(i = 0; i < 5; i++) {
					gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Text/" + i));
				}
		        break;
		}
	}	
	
	function ConvertColor (r : int, g : int, b : int) : Color { 
		return Color(r/255.0, g/255.0, b/255.0); 
	}
	
	function AddSquare() {
		CreateObject(squareRadius, "Square", false, radius, 300, 300, true);
	}
	
	function AddRoundButton(menuItem : int) {
		CreateObject(menuItem, "RoundButton", true, 0, 300, 150, true);
	}
	
	function AddArrow() {
		CreateObject(arrowRadius, "Arrow", false, arrowRad, 183, 100, false);
	}
	
	function AddLabel() {
		CreateObject(labelRadius, "Label", false, labelRad, 500, 200, true);
	}
	
	function AddText(menuItem : int) {
		switch (menuItem) {
			case 0:
				font = "Roboto";
				break;
			case 1:
				font = "OpenSans";
				break;
			case 2:
				font = "Helvetica";
				break;
			case 3:
				font = "BebasNeue";
				break;
			case 4:
				font = "Exo";
				break;
		}
		var fontMenu = EditorWindow.GetWindow(fontMenu, true, "Text Options");
		fontMenu.position = Rect(500, 140, 290, 500);
		fontMenu.maxSize = new Vector2(290, 500);
		fontMenu.minSize = new Vector2(290, 500);
	}
	
	function ChooseSquareRadius(menuItem : int) {
		squareRadius = menuItem;
		if(squareRadiusActive == false) {
			radius = squareRadius;
			subtitleString = "";
			squareRadiusActive = true;
			gridContents = new GUIContent[16];
	        for(i = 0; i < 16; i++) {
				gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Square/" + squareRadius + "/" + i));
			}
		} else if(squareRadiusActive == true) {
			AddSquare();
		}
	}
	
	function ChooseArrowRadius(menuItem : int) {
		arrowRadius = menuItem;
		if(arrowRadiusActive == false) {
			arrowRad = arrowRadius;
			subtitleString = "";
			arrowRadiusActive = true;
			gridContents = new GUIContent[11];
	        for(i = 0; i < 11; i++) {
				gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Arrow/" + arrowRadius + "/" + i));
			}
		} else if(arrowRadiusActive == true) {
			AddArrow();
		}
	}
	
	function ChooseLabelRadius(menuItem : int) {
		labelRadius = menuItem;
		if(labelRadiusActive == false) {
			labelRad = labelRadius;
			subtitleString = "";
			labelRadiusActive = true;
			gridContents = new GUIContent[11];
	        for(i = 0; i < 11; i++) {
				gridContents[i] = new GUIContent(Resources.Load("QuickUI/Textures/Label/" + labelRadius + "/" + i));
			}
		} else if(labelRadiusActive == true) {
			AddLabel();
		}
	}
	
	function CreateObject(objectNumber : int, objectName : String, onefolder : boolean, subfolder : int, width : int, height : int, fillcenter : boolean) {
		var UIImage : Object;
		var objectGameObject : GameObject;
		
		if(onefolder == true) {
			UIImage = Resources.Load("QuickUI/UI/" + objectName + "/" + objectNumber, Sprite);
		} else {
			UIImage = Resources.Load("QuickUI/UI/" + objectName + "/" + subfolder + "/" + objectNumber, Sprite);
		}
		
		objectGameObject = new GameObject("QuickUI " + objectName);
		objectGameObject.AddComponent(UI.Image);
		objectGameObject.GetComponent(UI.Image).sprite = UIImage;
		objectGameObject.GetComponent(UI.Image).color = MainUIColor;
		objectGameObject.GetComponent(UI.Image).type = UI.Image.Type.Sliced;
		objectGameObject.GetComponent(UI.Image).fillCenter = fillcenter;
		
		objectGameObject.GetComponent(RectTransform).sizeDelta = new Vector2(width, height);
		
		if (GameObject.Find("Canvas") != null) {
		    objectGameObject.transform.SetParent(GameObject.Find("Canvas").transform, true);
		} else {
			var canvas = new GameObject ("Canvas", Canvas);
			var evensystem = new GameObject ("EventSystem");
			
			canvas.AddComponent(UI.CanvasScaler);
			canvas.AddComponent(UI.GraphicRaycaster);
			evensystem.AddComponent(EventSystems.EventSystem);
			evensystem.AddComponent(EventSystems.StandaloneInputModule);
			evensystem.AddComponent(EventSystems.TouchInputModule);
			
			canvas.GetComponent(Canvas).renderMode = RenderMode.ScreenSpaceCamera;
			canvas.GetComponent(Canvas).worldCamera = Camera.main;
			
			objectGameObject.transform.SetParent(GameObject.Find("Canvas").transform, true);
		}
		
		objectGameObject.transform.localPosition = MainUIPosition;
		objectGameObject.transform.localScale = Vector3(MainUIScale, MainUIScale, MainUIScale);
		objectGameObject.layer = 5;
	}

		
	function AddCircles(menuItem : int) {
		CreateCircle(menuItem);
	}
	
	function CreateCircle(objectNumber : int) {
		var UIImage : Object;
		var circleGameObject : GameObject;
		
		UIImage = Resources.Load("QuickUI/UI/Circle/" + objectNumber, Sprite);
		
		circleGameObject = new GameObject("QuickUI Circle");
		circleGameObject.AddComponent(UI.Image);
		circleGameObject.GetComponent(UI.Image).sprite = UIImage;
		circleGameObject.GetComponent(UI.Image).color = MainUIColor;
		
		circleGameObject.GetComponent(RectTransform).sizeDelta = new Vector2(300, 300);
		
		if (GameObject.Find("Canvas") != null) {
		    circleGameObject.transform.SetParent(GameObject.Find("Canvas").transform, true);
		} else {
			var canvas = new GameObject ("Canvas", Canvas);
			var evensystem = new GameObject ("EventSystem");
			
			canvas.AddComponent(UI.CanvasScaler);
			canvas.AddComponent(UI.GraphicRaycaster);
			evensystem.AddComponent(EventSystems.EventSystem);
			evensystem.AddComponent(EventSystems.StandaloneInputModule);
			evensystem.AddComponent(EventSystems.TouchInputModule);
			
			canvas.GetComponent(Canvas).renderMode = RenderMode.ScreenSpaceCamera;
			canvas.GetComponent(Canvas).worldCamera = Camera.main;
			
			circleGameObject.transform.SetParent(GameObject.Find("Canvas").transform, true);
		}
		
		circleGameObject.transform.localPosition = MainUIPosition;
		circleGameObject.transform.localScale = Vector3(MainUIScale, MainUIScale, MainUIScale);
		circleGameObject.layer = 5;
	}
}

class optionsMenu extends EditorWindow {

	var optionsMenuStyle : GUIStyle;
	var currentWidth : float;
	
	var UIColor : Color = QuickUI.MainUIColor;
	var showPosition : boolean = true;
	
	var UIPosition : Vector3 = QuickUI.MainUIPosition;
	var UISize : float = QuickUI.MainUIScale;
	
	function OnGUI() {
		GUI.skin.label.alignment = TextAnchor.UpperLeft;
		currentWidth = position.width/2;
		
		GUI.backgroundColor = ConvertColor(34, 44, 54);
		GUI.Box(new Rect(0, 0, 9999, 9999), "");
		GUI.backgroundColor = Color.white;
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 30;
		GUI.Label (Rect (80, 10, 140, 50), "Options");
		
		GUI.skin.label.fontSize = 15;	
		GUI.Label (Rect (10, 60, 140, 50), "UI Color");	
		for(i = 0; i < 14; i++) {
			EditorGUILayout.Space();
		}
		EditorGUI.indentLevel++;
		EditorGUI.indentLevel++;
		GUI.skin.label.normal.textColor = Color.black;
		GUI.skin.label.fontSize = 12;
		UIColor = EditorGUILayout.ColorField(UIColor);
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 15;
		GUI.Label (Rect (10, 130, 240, 50), "Position");
		
		for(i = 0; i < 9; i++) {
			EditorGUILayout.Space();
		}
		UIPosition = EditorGUILayout.Vector3Field("", UIPosition);
		
		GUI.skin.label.fontSize = 13;
		GUI.Label (Rect (34, 158, 240, 50), "X");
		GUI.Label (Rect (120, 158, 240, 50), "Y");
		GUI.Label (Rect (203, 158, 240, 50), "Z");
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 15;
		GUI.Label (Rect (10, 200, 240, 50), "Scale");
		
		for(i = 0; i < 5; i++) {
			EditorGUILayout.Space();
		}
		EditorGUI.indentLevel++;
		UISize = EditorGUILayout.FloatField("", UISize, GUILayout.Width(113));
		
		
		if(GUI.Button(Rect(position.width - 70, position.height - 34, position.width - 230, 25), "Apply")) {
			QuickUI.MainUIColor = UIColor;
			QuickUI.MainUIPosition = UIPosition;
			QuickUI.MainUIScale = UISize;
			
			Close();
		}
		
		GUI.backgroundColor = Color.clear;
		if (GUI.Button(Rect(position.width - 106, position.height - 39, 35, 35), Resources.Load("QuickUI/Icons/reset"))) {
			UIColor = Color.white;
			showPosition = true;
		
			UIPosition = Vector3(0, 0, 0);
			UISize = 0.5;
		}
	}
	
	function ConvertColor (r : int, g : int, b : int) : Color { 
		return Color(r/255.0, g/255.0, b/255.0); 
	}
}

class fontMenu extends EditorWindow {

	var optionsMenuStyle : GUIStyle;
	var currentWidth : float;
	
	var text : String = "New Text";
	var textColor : Color = Color.white;
	var fontSize : int = 30;
	
	var mainStyleInt : int = 1;
	var mainStyles : String[] = ["Light", "Regular", "Bold"];
	
	var styleInt : int = 0;
	var styles : String[] = ["Normal", "Italic", "Bold & Italic"];
	
	var width : float = 250;
	var height : float = 75;
	
	function OnGUI() {
		GUI.skin.label.alignment = TextAnchor.UpperLeft;
		currentWidth = position.width/2;
		
		GUI.backgroundColor = ConvertColor(34, 44, 54);
		GUI.Box(new Rect(0, 0, 9999, 9999), "");
		GUI.backgroundColor = Color.white;
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 30;
		GUI.Label (Rect (50, 10, 200, 50), "Text Options");
		
		GUI.skin.label.fontSize = 15;	
		GUI.Label (Rect (10, 60, 140, 50), "Text");	
		for(i = 0; i < 14; i++) {
			EditorGUILayout.Space();
		}
		EditorGUI.indentLevel++;
		EditorGUI.indentLevel++;
		text = GUI.TextArea (Rect (35, 85, 233, 70), text);
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 15;
		GUI.Label (Rect (10, 170, 240, 50), "Text Color");
		
		for(i = 0; i < 18; i++) {
			EditorGUILayout.Space();
		}
		GUI.skin.label.normal.textColor = Color.black;
		GUI.skin.label.fontSize = 12;
		textColor = EditorGUILayout.ColorField(textColor);
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 15;
		GUI.Label (Rect (10, 230, 240, 50), "Font Size");
		
		for(i = 0; i < 7; i++) {
			EditorGUILayout.Space();
		}
		fontSize = EditorGUILayout.IntField("", fontSize, GUILayout.Width(113));
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 15;
		GUI.Label (Rect (10, 290, 240, 50), "Text Style");
		
		mainStyleInt = GUI.Toolbar (Rect (35, 315, 230, 20), mainStyleInt, mainStyles);
		styleInt = GUI.Toolbar (Rect (35, 340, 230, 20), styleInt, styles);
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 15;
		GUI.Label (Rect (10, 370, 240, 50), "Size");
		
		GUI.skin.label.fontSize = 10;
		GUI.Label (Rect (35, 390, 240, 50), "width");
		GUI.Label (Rect (145, 390, 240, 50), "height");
		width = EditorGUI.FloatField(Rect(8, 410, 120, 20), "", width);
		height = EditorGUI.FloatField(Rect(118, 410, 120, 20), "", height);
		
		GUI.skin.label.fontSize = 15;
		if(GUI.Button(Rect(position.width - 70, position.height - 34, position.width - 230, 25), "Create")) {
			var UITextObject : GameObject;
			
			UITextObject = new GameObject("QuickUI Text");
			UITextObject.AddComponent(UI.Text);
			UITextObject.GetComponent(UI.Text).text = text;
			UITextObject.GetComponent(UI.Text).color = textColor;
			UITextObject.GetComponent(UI.Text).fontSize = fontSize;
			
			if(mainStyleInt == 0) {
				UITextObject.GetComponent(UI.Text).font = Resources.Load("QuickUI/Fonts/" + QuickUI.font + "/" + QuickUI.font + " " + "Light", Font);
			} else if(mainStyleInt == 1) {
				UITextObject.GetComponent(UI.Text).font = Resources.Load("QuickUI/Fonts/" + QuickUI.font + "/" + QuickUI.font + " " + "Regular", Font);
			} else {
				UITextObject.GetComponent(UI.Text).font = Resources.Load("QuickUI/Fonts/" + QuickUI.font + "/" + QuickUI.font + " " + "Bold", Font);
			}
			
			if(styleInt == 0) {
				UITextObject.GetComponent(UI.Text).fontStyle = FontStyle.Normal;
			} else if(styleInt == 1) {
				UITextObject.GetComponent(UI.Text).fontStyle = FontStyle.Italic;
			} else {
				UITextObject.GetComponent(UI.Text).fontStyle = FontStyle.BoldAndItalic;
			}
			
			UITextObject.GetComponent(RectTransform).sizeDelta = new Vector2(width, height);
			
			if (GameObject.Find("Canvas") != null) {
			    UITextObject.transform.SetParent(GameObject.Find("Canvas").transform, true);
			} else {
				var canvas = new GameObject ("Canvas", Canvas);
				var evensystem = new GameObject ("EventSystem");
				
				canvas.AddComponent(UI.CanvasScaler);
				canvas.AddComponent(UI.GraphicRaycaster);
				evensystem.AddComponent(EventSystems.EventSystem);
				evensystem.AddComponent(EventSystems.StandaloneInputModule);
				evensystem.AddComponent(EventSystems.TouchInputModule);
				
				canvas.GetComponent(Canvas).renderMode = RenderMode.ScreenSpaceCamera;
				canvas.GetComponent(Canvas).worldCamera = Camera.main;
				
				UITextObject.transform.SetParent(GameObject.Find("Canvas").transform, true);
			}
			
			UITextObject.transform.localPosition = Vector3(0, 0, 0);
			UITextObject.transform.localScale = Vector3(1, 1, 1);
			UITextObject.layer = 5;
			
			Close();
		}
		
		GUI.backgroundColor = Color.clear;
		if (GUI.Button(Rect(position.width - 106, position.height - 39, 35, 35), Resources.Load("QuickUI/Icons/reset"))) {
			text = "New Text";
			textColor = Color.white;
			fontSize = 30;
			
			mainStyleInt = 1;
			styleInt = 0;
			
			width = 250;
			height = 75;
		}
	}
	
	function ConvertColor (r : int, g : int, b : int) : Color { 
		return Color(r/255.0, g/255.0, b/255.0); 
	}
}

class tutorial extends EditorWindow {

	var currentWidth : float;
	
	var titleString : String;
	var labelString_01 : String;
	var labelString_02 : String;
	var labelString_03 : String;
	var labelString_04 : String;
	
	function OnGUI() {
		currentWidth = position.width/2;
		
		GUI.backgroundColor = ConvertColor(34, 44, 54);
		GUI.Box(new Rect(0, 0, 9999, 9999), "");
		GUI.backgroundColor = Color.white;
		
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 30;
		GUI.Label (Rect (200, 10, 140, 50), titleString);
		
		GUI.skin.label.fontSize = 15;	
		GUI.Label (Rect (10, 60, 490, 50), labelString_01);	
		GUI.Label (Rect (10, 80, 490, 50), labelString_02);	
		GUI.Label (Rect (10, 100, 490, 50), labelString_03);
		GUI.Label (Rect (10, 120, 490, 50), labelString_04);
		
		if(EditorPrefs.GetInt("CurrentStep") == 4) {
			if(GUI.Button(Rect(position.width - 90, position.height - 34, 80, 25), "DONE")) {
				EditorPrefs.SetInt("FirstRun", 1);
				Close();
				
				var window = GetWindow(QuickUI);
				window.position = Rect(30, 30, 290, 600);
				window.maxSize = new Vector2(290, 600);
				window.minSize = new Vector2(290, 300);
				window.Show();
			}
		} else {
			if(GUI.Button(Rect(position.width - 90, position.height - 34, 80, 25), "Next")) {
				var currentStep: int = EditorPrefs.GetInt("CurrentStep");
				currentStep++;	
				EditorPrefs.SetInt("CurrentStep", currentStep);
				Close();
				var tutorial = EditorWindow.GetWindow(tutorial, true, "Tutorial");	
				tutorial.maxSize = new Vector2(510, 230);
				tutorial.minSize = new Vector2(510, 230);
			}
		}
		
		
		
		switch (EditorPrefs.GetInt("CurrentStep")) {
			case 0: 
				titleString = "Tutorial";
				labelString_01 = "Welcome! Thank you for downloading this plugin.";
				labelString_02 = "Before you start using this plugin, we want to help you get started.";
				labelString_03 = "Please follow this short introduction to get the most out of the";
				labelString_04 = "plugin.";
				
				break;
		    case 1:
		        titleString = "Position";
				labelString_01 = "The best location for the QuickUI editor window";
				labelString_02 = "is next to the Hierarchy tab. You can of cource put it somewhere";
				labelString_03 = "else, but it is recommended not to.";
				labelString_04 = "";
				
		        break;
		    case 2:
		    	titleString = "Usage";
				labelString_01 = "This editor plugin is really simple to use.";
				labelString_02 = "The main screen shows all the catagories, like circle and square,";
				labelString_03 = "once you select one of them you will see, depending on your";
				labelString_04 = "choice, all the different options on border and corner radius.";
				
		        break;
		    case 3:
		    	titleString = "Alt";
				labelString_01 = "If you hold down the alt button while you select a shape,";
				labelString_02 = "you will see a window where you can change even more options.";
				labelString_03 = "If you click the 'Apply' button, your setting are saved. The";
				labelString_04 = "settings will apply for all the shapes untill you edit them again.";
				
		        break;
		    case 4:
		    	titleString = "The End";
				labelString_01 = "This is everthing there is to know about this plugin.";
				labelString_02 = "We hope you will find this plugin helpfull.";
				labelString_03 = "If you do, considder rating it in the Asset Store.";
				labelString_04 = "If you run into any problems, feel free to contact us.";
				
		        break;
		}
	}
	
	function ConvertColor (r : int, g : int, b : int) : Color { 
		return Color(r/255.0, g/255.0, b/255.0); 
	}
}
