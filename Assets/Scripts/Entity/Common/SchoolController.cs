/****************************************
	Original: Copyright 2015 Unluck Software / www.chemicalbliss.com
	移動制御を BaseSwimmer へ移したため、生成・共有パラメータ・群れ全体の
	集計だけを残す形に作り直している。
*****************************************/

using System.Collections.Generic;
using Blue.Entity.Common;
using Blue.Interface;
using UnityEngine;

public class SchoolController : MonoBehaviour
{
	[Header("Spawn")]
	public SchoolMember[] _childPrefab;
	public int _childAmount = 250;
	public bool _groupChildToSchool;
	public bool _groupChildToNewTransform;
	public Transform _groupTransform;
	public string _groupName = "";

	[Header("Individual")]
	public float _minScale = .7f;
	public float _maxScale = 1.0f;

	// 各個体が中心のまわりで泳ぐ範囲。BaseSwimmer の roamArea として渡す
	[Header("School Shape")]
	public float _spawnSphere = 3.0f;
	public float _spawnSphereHeight = 1.5f;
	public float _spawnSphereDepth = 3.0f;

	// 群れ全体の目標がこの範囲を移動する
	[Header("School Movement")]
	public float _positionSphere = 25.0f;
	public float _positionSphereHeight = 5.0f;
	public float _positionSphereDepth = 5.0f;
	public bool _autoRandomPosition = true;
	public float _randomPositionTimerMin = 4.0f;
	public float _randomPositionTimerMax = 10.0f;
	public float _centreMoveSpeed = 1.5f;
	public Vector3 _posOffset;

	[Header("Boids")]
	public float _alignmentWeight = 0.6f;
	public float _cohesionWeight = 0.8f;

	[Header("Threat")]
	public bool _detectThreat = true;
	public float _schoolThreatSize = 1.0f;
	public float _schoolThreatThreshold = 2.0f;
	public float _threatDetectionRadius = 10.0f;
	public float _fleeWeight = 3.0f;
	public int _threatDetectionInterval = 5;
	public LayerMask _threatMask = ~0;

	private readonly List<SchoolMember> members = new List<SchoolMember>();
	private const int MaxThreatBuffer = 512;

	private Collider[] threatBuffer = new Collider[64];

	private Vector3 schoolCentre;
	private Vector3 centreTarget;
	private Vector3 centroid;
	private Vector3 averageForward;
	private Vector3 threatPosition;
	private bool hasThreat;
	private float repositionTimer;
	private int threatCounter;

	/// <summary>群れの中心。各個体の縄張りの中心になる</summary>
	public Vector3 SchoolCenter => schoolCentre;

	/// <summary>実際に個体が集まっている位置の平均</summary>
	public Vector3 Centroid => centroid;

	/// <summary>個体の向きの平均</summary>
	public Vector3 AverageForward => averageForward;

	public bool HasThreat => hasThreat;
	public Vector3 ThreatPosition => threatPosition;
	public int MemberCount => members.Count;

	private void Start()
	{
		schoolCentre = transform.position + _posOffset;
		centreTarget = schoolCentre;
		centroid = schoolCentre;
		repositionTimer = RandomRepositionTime();

		Spawn(_childAmount);
	}

	private void Update()
	{
		if (members.Count == 0) return;

		UpdateCentre();
		UpdateAggregate();
		UpdateThreat();
	}

	#region Spawn
	public void Spawn(int amount)
	{
		if (_childPrefab == null || _childPrefab.Length == 0)
		{
			Debug.LogError($"[SchoolController] {name}: 生成するプレハブが設定されていません。", this);
			return;
		}

		if (_groupChildToNewTransform) CreateGroupTransform();

		for (int i = 0; i < amount; i++)
		{
			SchoolMember prefab = _childPrefab[Random.Range(0, _childPrefab.Length)];
			if (prefab == null) continue;

			SchoolMember member = Instantiate(prefab, RandomPointInSchool(), Random.rotation);
			AttachToParent(member.transform);

			// Register は OnEnable で走るが、Initialize より前なので所属が未設定になる。
			// ここで確実に持たせてから、重複しないよう登録する
			member.Initialize(this);
			Register(member);
		}
	}

	private void CreateGroupTransform()
	{
		if (_groupTransform != null) return;

		GameObject group = new GameObject(string.IsNullOrEmpty(_groupName) ? $"{name} Fish Container" : _groupName);
		group.transform.position = transform.position;
		_groupTransform = group.transform;
	}

	private void AttachToParent(Transform child)
	{
		if (_groupChildToSchool) child.SetParent(transform);
		else if (_groupChildToNewTransform && _groupTransform != null) child.SetParent(_groupTransform);
	}

	private Vector3 RandomPointInSchool()
	{
		return schoolCentre + new Vector3(
			Random.Range(-_spawnSphere, _spawnSphere),
			Random.Range(-_spawnSphereHeight, _spawnSphereHeight),
			Random.Range(-_spawnSphereDepth, _spawnSphereDepth));
	}

	public void Register(SchoolMember member)
	{
		if (member == null || members.Contains(member)) return;

		members.Add(member);
	}

	public void Unregister(SchoolMember member)
	{
		members.Remove(member);
	}
	#endregion

	#region School movement
	// 群れ全体の目標をゆっくり移し、各個体の縄張りの中心として配る
	private void UpdateCentre()
	{
		if (_autoRandomPosition)
		{
			repositionTimer -= Time.deltaTime;

			if (repositionTimer <= 0f)
			{
				repositionTimer = RandomRepositionTime();
				centreTarget = transform.position + _posOffset + new Vector3(
					Random.Range(-_positionSphere, _positionSphere),
					Random.Range(-_positionSphereHeight, _positionSphereHeight),
					Random.Range(-_positionSphereDepth, _positionSphereDepth));
			}
		}

		schoolCentre = Vector3.MoveTowards(schoolCentre, centreTarget, _centreMoveSpeed * Time.deltaTime);

		Vector3 area = new Vector3(_spawnSphere, _spawnSphereHeight, _spawnSphereDepth);

		for (int i = members.Count - 1; i >= 0; i--)
		{
			SchoolMember member = members[i];

			if (member == null)
			{
				members.RemoveAt(i);
				continue;
			}

			member.Swimmer.SetRoamCenter(schoolCentre);
			member.Swimmer.SetRoamArea(area);
		}
	}

	private float RandomRepositionTime()
	{
		return Random.Range(_randomPositionTimerMin, _randomPositionTimerMax);
	}
	#endregion

	#region Aggregate
	// 結合と整列に使う値をここで 1 回だけ求める。
	// 個体ごとに近傍探索すると匹数ぶんクエリが増えるため
	private void UpdateAggregate()
	{
		Vector3 positionSum = Vector3.zero;
		Vector3 forwardSum = Vector3.zero;
		int count = 0;

		foreach (SchoolMember member in members)
		{
			if (member == null) continue;

			positionSum += member.transform.position;
			forwardSum += member.transform.forward;
			count++;
		}

		if (count == 0) return;

		centroid = positionSum / count;
		averageForward = forwardSum / count;

		if (averageForward.sqrMagnitude > 0.0001f) averageForward.Normalize();
	}
	#endregion

	#region Threat
	private void UpdateThreat()
	{
		if (!_detectThreat || _schoolThreatThreshold < 0f)
		{
			hasThreat = false;
			return;
		}

		threatCounter++;
		if (threatCounter < _threatDetectionInterval) return;

		threatCounter = 0;

		int count = QueryThreatCandidates();

		float nearestSqr = float.MaxValue;
		hasThreat = false;

		for (int i = 0; i < count; i++)
		{
			// 分割されたコライダーでも所有者へ解決する
			if (!EntityHit.TryResolve(threatBuffer[i], out ILivingEntity entity)) continue;

			// 仲間は脅威にならない。大きさの比較だけに頼ると、
			// _schoolThreatSize を閾値より大きくした瞬間に自分自身から逃げ出す
			if (entity is SchoolMember member && member.School == this) continue;

			if (entity.Size <= _schoolThreatThreshold) continue;

			Vector3 position = threatBuffer[i].transform.position;
			float distanceSqr = (position - centroid).sqrMagnitude;

			if (distanceSqr >= nearestSqr) continue;

			nearestSqr = distanceSqr;
			threatPosition = position;
			hasThreat = true;
		}
	}

	// バッファが埋まると結果は切り捨てられる。群れは検出範囲の中に数百のコライダーを
	// 持つので、仲間だけでバッファが埋まって脅威が一度も入らない。
	// 絞り込みはヒットを受け取った後にしかできないため、埋まったら広げて撃ち直す
	private int QueryThreatCandidates()
	{
		while (true)
		{
			int count = Physics.OverlapSphereNonAlloc(centroid, _threatDetectionRadius, threatBuffer,
				_threatMask, QueryTriggerInteraction.Ignore);

			if (count < threatBuffer.Length) return count;

			if (threatBuffer.Length >= MaxThreatBuffer)
			{
				Debug.LogWarning($"[SchoolController] {name}: 脅威検出のバッファ({MaxThreatBuffer})が埋まりました。" +
				                 "検出範囲を狭めるか、_threatMask で仲間のレイヤーを除いてください。", this);
				return count;
			}

			threatBuffer = new Collider[threatBuffer.Length * 2];
		}
	}
	#endregion

	private void OnDrawGizmosSelected()
	{
		Vector3 centre = Application.isPlaying ? schoolCentre : transform.position + _posOffset;

		// 個体が泳ぐ範囲
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(centre, new Vector3(_spawnSphere, _spawnSphereHeight, _spawnSphereDepth) * 2f);

		// 群れの中心が動ける範囲
		Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.5f);
		Gizmos.DrawWireCube(transform.position + _posOffset,
			new Vector3(_positionSphere, _positionSphereHeight, _positionSphereDepth) * 2f);

		if (!Application.isPlaying) return;

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(centroid, 0.4f);
		Gizmos.DrawRay(centroid, averageForward * 2f);

		if (hasThreat)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawLine(centroid, threatPosition);
		}

#if UNITY_EDITOR
		// 匹数を増やしても箱を広げれば密度は下がる。見た目では判断できないので数字で出す
		float volume = _spawnSphere * _spawnSphereHeight * _spawnSphereDepth * 8f;
		string density = volume > 0.0001f ? $"{members.Count / volume:F2} 匹/m3" : "-";

		UnityEditor.Handles.Label(centre,
			$"{name}\n匹数 {members.Count} / {_childAmount}\n密度 {density}");
#endif
	}
}
