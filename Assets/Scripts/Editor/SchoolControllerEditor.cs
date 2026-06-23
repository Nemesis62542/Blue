using UnityEngine;
using System;
using UnityEditor;
/****************************************	
	Copyright 2015 Unluck Software	
 	www.chemicalbliss.com																															
*****************************************/
[CustomEditor(typeof(SchoolController))]
[Serializable]
public class SchoolControllerEditor: Editor
{
	public SerializedProperty myProperty;
	public SerializedProperty avoidanceMask;
	public SerializedProperty fishLayer;

	public void OnEnable()
	{
        avoidanceMask= serializedObject.FindProperty("_avoidanceMask");
		myProperty = serializedObject.FindProperty("_childPrefab");
		fishLayer = serializedObject.FindProperty("_fishLayer");
	}

	public override void OnInspectorGUI()
	{
		var target_cs = (SchoolController)target;
        Color warningColor = new Color32((byte)255, (byte)174, (byte)0, (byte)255);
		Color warningColor2 = Color.yellow;
		Color dColor = new Color32((byte)175, (byte)175, (byte)175, (byte)255);
		GUIStyle warningStyle = new GUIStyle(GUI.skin.label);
		warningStyle.normal.textColor = warningColor;
		warningStyle.fontStyle = FontStyle.Bold;
		GUIStyle warningStyle2 = new GUIStyle(GUI.skin.label);
		warningStyle2.normal.textColor = warningColor2;
		warningStyle2.fontStyle = FontStyle.Bold;
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		if(UnityEditor.EditorApplication.isPlaying)
		{
			GUI.enabled = false;
		}
		target_cs._updateDivisor = (int)EditorGUILayout.Slider("Frame Skipping", (float)target_cs._updateDivisor, 1.0f, 10.0f);
		GUI.enabled = true;
		if(target_cs._updateDivisor > 4)
		{
			EditorGUILayout.LabelField("Will cause choppy movement", warningStyle);
		}
		else if(target_cs._updateDivisor > 2)
		{
			EditorGUILayout.LabelField("Can cause choppy movement	", warningStyle2);
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		serializedObject.Update();
		EditorGUILayout.PropertyField(myProperty, new GUIContent("Fish Prefabs"), true);
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.LabelField("Prefabs must have SchoolChild component", EditorStyles.miniLabel);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Grouping", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Move fish into a parent transform", EditorStyles.miniLabel);
		target_cs._groupChildToSchool = EditorGUILayout.Toggle("Group to School", target_cs._groupChildToSchool);
		if(target_cs._groupChildToSchool)
		{
			GUI.enabled = false;
		}
		target_cs._groupChildToNewTransform = EditorGUILayout.Toggle("Group to New GameObject", target_cs._groupChildToNewTransform);
		target_cs._groupName = EditorGUILayout.TextField("Group Name", target_cs._groupName);
		GUI.enabled = true;
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Area Size", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Size of area the school roams within", EditorStyles.miniLabel);
		target_cs._positionSphere = EditorGUILayout.FloatField("Roaming Area Width", target_cs._positionSphere);
		target_cs._positionSphereDepth = EditorGUILayout.FloatField("Roaming Area Depth", target_cs._positionSphereDepth);
		target_cs._positionSphereHeight = EditorGUILayout.FloatField("Roaming Area Height", target_cs._positionSphereHeight);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Size of the school", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Size of area the Fish swim towards", EditorStyles.miniLabel);
		target_cs._childAmount = (int)EditorGUILayout.Slider("Fish Amount", (float)target_cs._childAmount, 1.0f, 500.0f);
		target_cs._spawnSphere = EditorGUILayout.FloatField("School Width", target_cs._spawnSphere);
		target_cs._spawnSphereDepth = EditorGUILayout.FloatField("School Depth", target_cs._spawnSphereDepth);
		target_cs._spawnSphereHeight = EditorGUILayout.FloatField("School Height", target_cs._spawnSphereHeight);
		target_cs._posOffset = EditorGUILayout.Vector3Field("Start Position Offset", target_cs._posOffset);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Speed and Movement ", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Change Fish speed, rotation and movement behaviors", EditorStyles.miniLabel);
		target_cs._childSpeedMultipler = EditorGUILayout.FloatField("Random Speed Multiplier", target_cs._childSpeedMultipler);
		target_cs._speedCurveMultiplier = EditorGUILayout.CurveField("Speed Curve Multiplier", target_cs._speedCurveMultiplier);
		if(target_cs._childSpeedMultipler < 0.01f) target_cs._childSpeedMultipler = 0.01f;
		target_cs._minSpeed = EditorGUILayout.FloatField("Min Speed", target_cs._minSpeed);
		target_cs._maxSpeed = EditorGUILayout.FloatField("Max Speed", target_cs._maxSpeed);
		target_cs._acceleration = EditorGUILayout.Slider("Fish Acceleration", target_cs._acceleration, .001f, 0.07f);
		target_cs._brake = EditorGUILayout.Slider("Fish Brake Power", target_cs._brake, .001f, 0.025f);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Turn Speed", EditorStyles.boldLabel);
		target_cs._minDamping = EditorGUILayout.FloatField("Min Turn Speed", target_cs._minDamping);
		target_cs._maxDamping = EditorGUILayout.FloatField("Max Turn Speed", target_cs._maxDamping);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Randomize Fish Size ", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Change scale of Fish when they are added to the stage", EditorStyles.miniLabel);
		target_cs._minScale = EditorGUILayout.FloatField("Min Scale", target_cs._minScale);
		target_cs._maxScale = EditorGUILayout.FloatField("Max Scale", target_cs._maxScale);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Fish Random Animation Speeds", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Animation speeds are also increased by movement speed", EditorStyles.miniLabel);
		target_cs._minAnimationSpeed = EditorGUILayout.FloatField("Min Animation Speed", target_cs._minAnimationSpeed);
		target_cs._maxAnimationSpeed = EditorGUILayout.FloatField("Max Animation Speed", target_cs._maxAnimationSpeed);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Fish Waypoint Distance", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Waypoints inside small sphere", EditorStyles.miniLabel);
		target_cs._waypointDistance = EditorGUILayout.FloatField("Distance To Waypoint", target_cs._waypointDistance);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Fish Triggers School Waypoint", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Fish waypoint triggers a new School waypoint", EditorStyles.miniLabel);
		target_cs._childTriggerPos = EditorGUILayout.Toggle("Fish Trigger Waypoint", target_cs._childTriggerPos);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Automatically New Waypoint", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Automatically trigger new school waypoint", EditorStyles.miniLabel);
		target_cs._autoRandomPosition = EditorGUILayout.Toggle("Auto School Waypoint", target_cs._autoRandomPosition);
		if(target_cs._autoRandomPosition)
		{
			target_cs._randomPositionTimerMin = EditorGUILayout.FloatField("Min Delay", target_cs._randomPositionTimerMin);
			target_cs._randomPositionTimerMax = EditorGUILayout.FloatField("Max Delay", target_cs._randomPositionTimerMax);
			if(target_cs._randomPositionTimerMin < 1)
			{
				target_cs._randomPositionTimerMin = 1.0f;
			}
			if(target_cs._randomPositionTimerMax < 1)
			{
				target_cs._randomPositionTimerMax = 1.0f;
			}
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Fish Force School Waypoint", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Force all Fish to change waypoints when school changes waypoint", EditorStyles.miniLabel);
		target_cs._forceChildWaypoints = EditorGUILayout.Toggle("Force Fish Waypoints", target_cs._forceChildWaypoints);
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Force New Waypoint Delay", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("How many seconds until the Fish in school will change waypoint", EditorStyles.miniLabel);
		target_cs._forcedRandomDelay = EditorGUILayout.FloatField("Waypoint Delay", target_cs._forcedRandomDelay);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Obstacle Avoidance", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Steer and push away from obstacles (uses more CPU)", EditorStyles.miniLabel);
		EditorGUILayout.PropertyField(avoidanceMask, new GUIContent("Collider Mask"));
		target_cs._avoidance = EditorGUILayout.Toggle("Avoidance (enable/disable)", target_cs._avoidance);
		if(target_cs._avoidance)
		{
			target_cs._avoidAngle = EditorGUILayout.Slider("Avoid Angle", target_cs._avoidAngle, .05f, .95f);
			target_cs._avoidDistance = EditorGUILayout.FloatField("Avoid Distance", target_cs._avoidDistance);
			if(target_cs._avoidDistance <= 0.1f) target_cs._avoidDistance = 0.1f;
			target_cs._avoidSpeed = EditorGUILayout.FloatField("Avoid Speed", target_cs._avoidSpeed);
			target_cs._stopDistance = EditorGUILayout.FloatField("Stop Distance", target_cs._stopDistance);
			target_cs._stopSpeedMultiplier = EditorGUILayout.FloatField("Stop Speed Multiplier", target_cs._stopSpeedMultiplier);
			if(target_cs._stopDistance <= 0.1f) target_cs._stopDistance = 0.1f;
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		target_cs._push = EditorGUILayout.Toggle("Push (enable/disable)", target_cs._push);
		if(target_cs._push)
		{
			target_cs._pushDistance = EditorGUILayout.FloatField("Push Distance", target_cs._pushDistance);
			if(target_cs._pushDistance <= 0.1f) target_cs._pushDistance = 0.1f;
			target_cs._pushForce = EditorGUILayout.FloatField("Push Force", target_cs._pushForce);
			if(target_cs._pushForce <= 0.01f) target_cs._pushForce = 0.01f;
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Boids Algorithm (Flocking Behavior)", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Fish interact with nearby fish for realistic schooling", EditorStyles.miniLabel);
		target_cs._boids = EditorGUILayout.Toggle("Boids (enable/disable)", target_cs._boids);
		if(target_cs._boids)
		{
			target_cs._neighborDistance = EditorGUILayout.FloatField("Neighbor Search Distance", target_cs._neighborDistance);
			if(target_cs._neighborDistance <= 0.1f) target_cs._neighborDistance = 0.1f;

			target_cs._separationDistance = EditorGUILayout.FloatField("Separation Distance", target_cs._separationDistance);
			if(target_cs._separationDistance <= 0.1f) target_cs._separationDistance = 0.1f;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Weight Settings", EditorStyles.miniBoldLabel);
			target_cs._separationWeight = EditorGUILayout.Slider("Separation Weight", target_cs._separationWeight, 0.0f, 5.0f);
			target_cs._alignmentWeight = EditorGUILayout.Slider("Alignment Weight", target_cs._alignmentWeight, 0.0f, 5.0f);
			target_cs._cohesionWeight = EditorGUILayout.Slider("Cohesion Weight", target_cs._cohesionWeight, 0.0f, 5.0f);

			EditorGUILayout.Space();
			serializedObject.Update();
			EditorGUILayout.PropertyField(fishLayer, new GUIContent("Fish Layer"));
			serializedObject.ApplyModifiedProperties();
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Leader Follow System", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Some fish become leaders, others follow them", EditorStyles.miniLabel);
		target_cs._leaderFollow = EditorGUILayout.Toggle("Leader Follow (enable/disable)", target_cs._leaderFollow);
		if(target_cs._leaderFollow)
		{
			target_cs._leaderRatio = EditorGUILayout.Slider("Leader Ratio", target_cs._leaderRatio, 0.05f, 0.5f);
			EditorGUILayout.LabelField($"Leaders: {Mathf.RoundToInt(target_cs._childAmount * target_cs._leaderRatio)}/{target_cs._childAmount}", EditorStyles.miniLabel);

			target_cs._followDistance = EditorGUILayout.FloatField("Follow Search Distance", target_cs._followDistance);
			if(target_cs._followDistance <= 0.1f) target_cs._followDistance = 0.1f;

			target_cs._followWeight = EditorGUILayout.Slider("Follow Weight", target_cs._followWeight, 0.0f, 5.0f);
			target_cs._leaderChangeInterval = EditorGUILayout.FloatField("Leader Change Interval (sec)", target_cs._leaderChangeInterval);
			if(target_cs._leaderChangeInterval < 1.0f) target_cs._leaderChangeInterval = 1.0f;

			// Manual leader reassignment button (only in play mode)
			if (Application.isPlaying) {
				if (GUILayout.Button("Reassign Leaders Now")) {
					target_cs.AssignLeaders();
				}
			}
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Dynamic Density System", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Fish behavior changes based on threat level (size-based)", EditorStyles.miniLabel);
		target_cs._dynamicDensity = EditorGUILayout.Toggle("Dynamic Density (enable/disable)", target_cs._dynamicDensity);
		if(target_cs._dynamicDensity)
		{
			// Display current threat level in play mode
			if (Application.isPlaying) {
				EditorGUILayout.Space();
				EditorGUILayout.LabelField($"Current Threat Level: {target_cs._currentThreatLevel}", EditorStyles.boldLabel);
				EditorGUILayout.Space();
			}

			EditorGUILayout.LabelField("Size Settings", EditorStyles.miniBoldLabel);
			target_cs._schoolThreatSize = EditorGUILayout.FloatField("School Threat Size", target_cs._schoolThreatSize);
			target_cs._schoolThreatThreshold = EditorGUILayout.FloatField("Threat Size Threshold", target_cs._schoolThreatThreshold);
			EditorGUILayout.LabelField("(-1 = fear nothing, 0+ = fear anything larger)", EditorStyles.miniLabel);

			EditorGUILayout.Space();
			target_cs._threatDetectionRadius = EditorGUILayout.FloatField("Threat Detection Radius", target_cs._threatDetectionRadius);
			if(target_cs._threatDetectionRadius <= 0.1f) target_cs._threatDetectionRadius = 0.1f;

			target_cs._transitionSpeed = EditorGUILayout.Slider("Transition Speed", target_cs._transitionSpeed, 0.1f, 10.0f);
			target_cs._fleeWeight = EditorGUILayout.Slider("Flee Weight", target_cs._fleeWeight, 0.0f, 10.0f);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Performance Settings", EditorStyles.miniBoldLabel);
			target_cs._threatDetectionInterval = EditorGUILayout.IntSlider("Detection Interval (frames)", target_cs._threatDetectionInterval, 1, 30);
			EditorGUILayout.LabelField("Higher values = better performance, less responsive", EditorStyles.miniLabel);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Threat Level: None (Dispersed)", EditorStyles.miniBoldLabel);
			target_cs._noneSpeedMultiplier = EditorGUILayout.Slider("Speed Multiplier", target_cs._noneSpeedMultiplier, 0.1f, 2.0f);
			target_cs._noneSeparationWeight = EditorGUILayout.Slider("Separation Weight", target_cs._noneSeparationWeight, 0.0f, 5.0f);
			target_cs._noneCohesionWeight = EditorGUILayout.Slider("Cohesion Weight", target_cs._noneCohesionWeight, 0.0f, 5.0f);
			target_cs._noneSpawnSphereMultiplier = EditorGUILayout.Slider("Range Multiplier", target_cs._noneSpawnSphereMultiplier, 0.5f, 3.0f);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Threat Level: Low (Normal)", EditorStyles.miniBoldLabel);
			target_cs._lowSpeedMultiplier = EditorGUILayout.Slider("Speed Multiplier", target_cs._lowSpeedMultiplier, 0.1f, 2.0f);
			target_cs._lowSeparationWeight = EditorGUILayout.Slider("Separation Weight", target_cs._lowSeparationWeight, 0.0f, 5.0f);
			target_cs._lowCohesionWeight = EditorGUILayout.Slider("Cohesion Weight", target_cs._lowCohesionWeight, 0.0f, 5.0f);
			target_cs._lowSpawnSphereMultiplier = EditorGUILayout.Slider("Range Multiplier", target_cs._lowSpawnSphereMultiplier, 0.5f, 3.0f);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Threat Level: High (Dense & Fast)", EditorStyles.miniBoldLabel);
			target_cs._highSpeedMultiplier = EditorGUILayout.Slider("Speed Multiplier", target_cs._highSpeedMultiplier, 0.1f, 3.0f);
			target_cs._highSeparationWeight = EditorGUILayout.Slider("Separation Weight", target_cs._highSeparationWeight, 0.0f, 5.0f);
			target_cs._highCohesionWeight = EditorGUILayout.Slider("Cohesion Weight", target_cs._highCohesionWeight, 0.0f, 5.0f);
			target_cs._highSpawnSphereMultiplier = EditorGUILayout.Slider("Range Multiplier", target_cs._highSpawnSphereMultiplier, 0.1f, 2.0f);

			// Manual control buttons for testing (only in play mode)
			if (Application.isPlaying) {
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Manual Control (for testing)", EditorStyles.miniBoldLabel);
				if (GUILayout.Button("Set Threat: None")) {
					target_cs.SetThreatLevel(SchoolController.ThreatLevel.None);
				}
				if (GUILayout.Button("Set Threat: Low")) {
					target_cs.SetThreatLevel(SchoolController.ThreatLevel.Low);
				}
				if (GUILayout.Button("Set Threat: High")) {
					target_cs.SetThreatLevel(SchoolController.ThreatLevel.High);
				}
			}
		}
		EditorGUILayout.EndVertical();
		if(GUI.changed) EditorUtility.SetDirty(target_cs);
	}
}