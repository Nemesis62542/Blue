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
		target_cs._updateDivisor = (int)EditorGUILayout.Slider("フレームスキップ", (float)target_cs._updateDivisor, 1.0f, 10.0f);
		GUI.enabled = true;
		if(target_cs._updateDivisor > 4)
		{
			EditorGUILayout.LabelField("カクカクした動きになります", warningStyle);
		}
		else if(target_cs._updateDivisor > 2)
		{
			EditorGUILayout.LabelField("カクカクした動きになる可能性があります", warningStyle2);
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		serializedObject.Update();
		EditorGUILayout.PropertyField(myProperty, new GUIContent("魚のPrefab"), true);
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.LabelField("PrefabにはSchoolChildコンポーネントが必要です", EditorStyles.miniLabel);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("グループ化", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("魚を親トランスフォームに移動", EditorStyles.miniLabel);
		target_cs._groupChildToSchool = EditorGUILayout.Toggle("群れにグループ化", target_cs._groupChildToSchool);
		if(target_cs._groupChildToSchool)
		{
			GUI.enabled = false;
		}
		target_cs._groupChildToNewTransform = EditorGUILayout.Toggle("新規GameObjectにグループ化", target_cs._groupChildToNewTransform);
		target_cs._groupName = EditorGUILayout.TextField("グループ名", target_cs._groupName);
		GUI.enabled = true;
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("エリアサイズ", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("群れが回遊するエリアのサイズ", EditorStyles.miniLabel);
		target_cs._positionSphere = EditorGUILayout.FloatField("回遊エリアの幅", target_cs._positionSphere);
		target_cs._positionSphereDepth = EditorGUILayout.FloatField("回遊エリアの奥行き", target_cs._positionSphereDepth);
		target_cs._positionSphereHeight = EditorGUILayout.FloatField("回遊エリアの高さ", target_cs._positionSphereHeight);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("群れのサイズ", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("魚が泳ぐエリアのサイズ", EditorStyles.miniLabel);
		target_cs._childAmount = (int)EditorGUILayout.Slider("魚の数", (float)target_cs._childAmount, 1.0f, 500.0f);
		target_cs._spawnSphere = EditorGUILayout.FloatField("群れの幅", target_cs._spawnSphere);
		target_cs._spawnSphereDepth = EditorGUILayout.FloatField("群れの奥行き", target_cs._spawnSphereDepth);
		target_cs._spawnSphereHeight = EditorGUILayout.FloatField("群れの高さ", target_cs._spawnSphereHeight);
		target_cs._posOffset = EditorGUILayout.Vector3Field("開始位置のオフセット", target_cs._posOffset);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("速度と移動", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("魚の速度、回転、移動動作を変更", EditorStyles.miniLabel);
		target_cs._childSpeedMultipler = EditorGUILayout.FloatField("ランダム速度倍率", target_cs._childSpeedMultipler);
		target_cs._speedCurveMultiplier = EditorGUILayout.CurveField("速度カーブ倍率", target_cs._speedCurveMultiplier);
		if(target_cs._childSpeedMultipler < 0.01f) target_cs._childSpeedMultipler = 0.01f;
		target_cs._minSpeed = EditorGUILayout.FloatField("最小速度", target_cs._minSpeed);
		target_cs._maxSpeed = EditorGUILayout.FloatField("最大速度", target_cs._maxSpeed);
		target_cs._acceleration = EditorGUILayout.Slider("魚の加速度", target_cs._acceleration, .001f, 0.07f);
		target_cs._brake = EditorGUILayout.Slider("魚のブレーキ力", target_cs._brake, .001f, 0.025f);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("旋回速度", EditorStyles.boldLabel);
		target_cs._minDamping = EditorGUILayout.FloatField("最小旋回速度", target_cs._minDamping);
		target_cs._maxDamping = EditorGUILayout.FloatField("最大旋回速度", target_cs._maxDamping);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("魚のサイズをランダム化", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("魚が追加されたときのスケールを変更", EditorStyles.miniLabel);
		target_cs._minScale = EditorGUILayout.FloatField("最小スケール", target_cs._minScale);
		target_cs._maxScale = EditorGUILayout.FloatField("最大スケール", target_cs._maxScale);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("魚のランダムアニメーション速度", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("アニメーション速度は移動速度によっても増加します", EditorStyles.miniLabel);
		target_cs._minAnimationSpeed = EditorGUILayout.FloatField("最小アニメーション速度", target_cs._minAnimationSpeed);
		target_cs._maxAnimationSpeed = EditorGUILayout.FloatField("最大アニメーション速度", target_cs._maxAnimationSpeed);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("魚のウェイポイント距離", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("小さな球内のウェイポイント", EditorStyles.miniLabel);
		target_cs._waypointDistance = EditorGUILayout.FloatField("ウェイポイントまでの距離", target_cs._waypointDistance);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("魚が群れのウェイポイントをトリガー", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("魚のウェイポイントが新しい群れのウェイポイントをトリガー", EditorStyles.miniLabel);
		target_cs._childTriggerPos = EditorGUILayout.Toggle("魚がウェイポイントをトリガー", target_cs._childTriggerPos);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("自動で新しいウェイポイント", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("自動的に新しい群れのウェイポイントをトリガー", EditorStyles.miniLabel);
		target_cs._autoRandomPosition = EditorGUILayout.Toggle("自動群れウェイポイント", target_cs._autoRandomPosition);
		if(target_cs._autoRandomPosition)
		{
			target_cs._randomPositionTimerMin = EditorGUILayout.FloatField("最小遅延", target_cs._randomPositionTimerMin);
			target_cs._randomPositionTimerMax = EditorGUILayout.FloatField("最大遅延", target_cs._randomPositionTimerMax);
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
		EditorGUILayout.LabelField("魚に群れのウェイポイントを強制", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("群れがウェイポイントを変更したとき、すべての魚にウェイポイントを変更させる", EditorStyles.miniLabel);
		target_cs._forceChildWaypoints = EditorGUILayout.Toggle("魚のウェイポイントを強制", target_cs._forceChildWaypoints);
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("新しいウェイポイントの遅延を強制", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("群れの魚がウェイポイントを変更するまでの秒数", EditorStyles.miniLabel);
		target_cs._forcedRandomDelay = EditorGUILayout.FloatField("ウェイポイント遅延", target_cs._forcedRandomDelay);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("障害物回避", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("障害物から操舵して離れる（CPU使用量が増加）", EditorStyles.miniLabel);
		EditorGUILayout.PropertyField(avoidanceMask, new GUIContent("コライダーマスク"));
		target_cs._avoidance = EditorGUILayout.Toggle("回避（有効/無効）", target_cs._avoidance);
		if(target_cs._avoidance)
		{
			target_cs._avoidAngle = EditorGUILayout.Slider("回避角度", target_cs._avoidAngle, .05f, .95f);
			target_cs._avoidDistance = EditorGUILayout.FloatField("回避距離", target_cs._avoidDistance);
			if(target_cs._avoidDistance <= 0.1f) target_cs._avoidDistance = 0.1f;
			target_cs._avoidSpeed = EditorGUILayout.FloatField("回避速度", target_cs._avoidSpeed);
			target_cs._stopDistance = EditorGUILayout.FloatField("停止距離", target_cs._stopDistance);
			target_cs._stopSpeedMultiplier = EditorGUILayout.FloatField("停止速度倍率", target_cs._stopSpeedMultiplier);
			if(target_cs._stopDistance <= 0.1f) target_cs._stopDistance = 0.1f;
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		target_cs._push = EditorGUILayout.Toggle("押し出し（有効/無効）", target_cs._push);
		if(target_cs._push)
		{
			target_cs._pushDistance = EditorGUILayout.FloatField("押し出し距離", target_cs._pushDistance);
			if(target_cs._pushDistance <= 0.1f) target_cs._pushDistance = 0.1f;
			target_cs._pushForce = EditorGUILayout.FloatField("押し出し力", target_cs._pushForce);
			if(target_cs._pushForce <= 0.01f) target_cs._pushForce = 0.01f;
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Boids アルゴリズム（群れ行動）", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("近隣の魚と相互作用してリアルな群れを形成", EditorStyles.miniLabel);
		target_cs._boids = EditorGUILayout.Toggle("Boids（有効/無効）", target_cs._boids);
		if(target_cs._boids)
		{
			target_cs._neighborDistance = EditorGUILayout.FloatField("近隣検索距離", target_cs._neighborDistance);
			if(target_cs._neighborDistance <= 0.1f) target_cs._neighborDistance = 0.1f;

			target_cs._separationDistance = EditorGUILayout.FloatField("分離距離", target_cs._separationDistance);
			if(target_cs._separationDistance <= 0.1f) target_cs._separationDistance = 0.1f;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("重み設定", EditorStyles.miniBoldLabel);
			target_cs._separationWeight = EditorGUILayout.Slider("分離の重み", target_cs._separationWeight, 0.0f, 5.0f);
			target_cs._alignmentWeight = EditorGUILayout.Slider("整列の重み", target_cs._alignmentWeight, 0.0f, 5.0f);
			target_cs._cohesionWeight = EditorGUILayout.Slider("結合の重み", target_cs._cohesionWeight, 0.0f, 5.0f);

			EditorGUILayout.Space();
			serializedObject.Update();
			EditorGUILayout.PropertyField(fishLayer, new GUIContent("魚のレイヤー"));
			serializedObject.ApplyModifiedProperties();
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("リーダー追従システム", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("一部の魚がリーダーとなり、他の魚が追従", EditorStyles.miniLabel);
		target_cs._leaderFollow = EditorGUILayout.Toggle("リーダー追従（有効/無効）", target_cs._leaderFollow);
		if(target_cs._leaderFollow)
		{
			target_cs._leaderRatio = EditorGUILayout.Slider("リーダー比率", target_cs._leaderRatio, 0.05f, 0.5f);
			EditorGUILayout.LabelField($"リーダー数: {Mathf.RoundToInt(target_cs._childAmount * target_cs._leaderRatio)}/{target_cs._childAmount}", EditorStyles.miniLabel);

			target_cs._followDistance = EditorGUILayout.FloatField("追従検索距離", target_cs._followDistance);
			if(target_cs._followDistance <= 0.1f) target_cs._followDistance = 0.1f;

			target_cs._followWeight = EditorGUILayout.Slider("追従の重み", target_cs._followWeight, 0.0f, 5.0f);
			target_cs._leaderChangeInterval = EditorGUILayout.FloatField("リーダー交代間隔（秒）", target_cs._leaderChangeInterval);
			if(target_cs._leaderChangeInterval < 1.0f) target_cs._leaderChangeInterval = 1.0f;

			// Manual leader reassignment button (only in play mode)
			if (Application.isPlaying) {
				if (GUILayout.Button("今すぐリーダーを再選出")) {
					target_cs.AssignLeaders();
				}
			}
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("ダイナミック密度システム", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("脅威レベルに応じて魚の行動が変化（サイズベース）", EditorStyles.miniLabel);
		target_cs._dynamicDensity = EditorGUILayout.Toggle("ダイナミック密度（有効/無効）", target_cs._dynamicDensity);
		if(target_cs._dynamicDensity)
		{
			// Display current threat level in play mode
			if (Application.isPlaying) {
				EditorGUILayout.Space();
				EditorGUILayout.LabelField($"現在の脅威レベル: {target_cs._currentThreatLevel}", EditorStyles.boldLabel);
				EditorGUILayout.Space();
			}

			EditorGUILayout.LabelField("サイズ設定", EditorStyles.miniBoldLabel);
			target_cs._schoolThreatSize = EditorGUILayout.FloatField("群れの脅威サイズ", target_cs._schoolThreatSize);
			target_cs._schoolThreatThreshold = EditorGUILayout.FloatField("脅威とみなすサイズ", target_cs._schoolThreatThreshold);
			EditorGUILayout.LabelField("（-1 = 何も恐れない、0以上 = これより大きいものを脅威とみなす）", EditorStyles.miniLabel);

			EditorGUILayout.Space();
			target_cs._threatDetectionRadius = EditorGUILayout.FloatField("脅威検知半径", target_cs._threatDetectionRadius);
			if(target_cs._threatDetectionRadius <= 0.1f) target_cs._threatDetectionRadius = 0.1f;

			target_cs._transitionSpeed = EditorGUILayout.Slider("遷移速度", target_cs._transitionSpeed, 0.1f, 10.0f);
			target_cs._fleeWeight = EditorGUILayout.Slider("逃走の重み", target_cs._fleeWeight, 0.0f, 10.0f);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("パフォーマンス設定", EditorStyles.miniBoldLabel);
			target_cs._threatDetectionInterval = EditorGUILayout.IntSlider("検知間隔（フレーム数）", target_cs._threatDetectionInterval, 1, 30);
			EditorGUILayout.LabelField("値が大きいほど高パフォーマンス、反応は鈍くなる", EditorStyles.miniLabel);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("脅威レベル: None（分散）", EditorStyles.miniBoldLabel);
			target_cs._noneSpeedMultiplier = EditorGUILayout.Slider("速度倍率", target_cs._noneSpeedMultiplier, 0.1f, 2.0f);
			target_cs._noneSeparationWeight = EditorGUILayout.Slider("分離の重み", target_cs._noneSeparationWeight, 0.0f, 5.0f);
			target_cs._noneCohesionWeight = EditorGUILayout.Slider("結合の重み", target_cs._noneCohesionWeight, 0.0f, 5.0f);
			target_cs._noneSpawnSphereMultiplier = EditorGUILayout.Slider("範囲倍率", target_cs._noneSpawnSphereMultiplier, 0.5f, 3.0f);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("脅威レベル: Low（通常）", EditorStyles.miniBoldLabel);
			target_cs._lowSpeedMultiplier = EditorGUILayout.Slider("速度倍率", target_cs._lowSpeedMultiplier, 0.1f, 2.0f);
			target_cs._lowSeparationWeight = EditorGUILayout.Slider("分離の重み", target_cs._lowSeparationWeight, 0.0f, 5.0f);
			target_cs._lowCohesionWeight = EditorGUILayout.Slider("結合の重み", target_cs._lowCohesionWeight, 0.0f, 5.0f);
			target_cs._lowSpawnSphereMultiplier = EditorGUILayout.Slider("範囲倍率", target_cs._lowSpawnSphereMultiplier, 0.5f, 3.0f);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("脅威レベル: High（密集・高速）", EditorStyles.miniBoldLabel);
			target_cs._highSpeedMultiplier = EditorGUILayout.Slider("速度倍率", target_cs._highSpeedMultiplier, 0.1f, 3.0f);
			target_cs._highSeparationWeight = EditorGUILayout.Slider("分離の重み", target_cs._highSeparationWeight, 0.0f, 5.0f);
			target_cs._highCohesionWeight = EditorGUILayout.Slider("結合の重み", target_cs._highCohesionWeight, 0.0f, 5.0f);
			target_cs._highSpawnSphereMultiplier = EditorGUILayout.Slider("範囲倍率", target_cs._highSpawnSphereMultiplier, 0.1f, 2.0f);

			// Manual control buttons for testing (only in play mode)
			if (Application.isPlaying) {
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("手動制御（テスト用）", EditorStyles.miniBoldLabel);
				if (GUILayout.Button("脅威レベル設定: None")) {
					target_cs.SetThreatLevel(SchoolController.ThreatLevel.None);
				}
				if (GUILayout.Button("脅威レベル設定: Low")) {
					target_cs.SetThreatLevel(SchoolController.ThreatLevel.Low);
				}
				if (GUILayout.Button("脅威レベル設定: High")) {
					target_cs.SetThreatLevel(SchoolController.ThreatLevel.High);
				}
			}
		}
		EditorGUILayout.EndVertical();
		if(GUI.changed) EditorUtility.SetDirty(target_cs);
	}
}