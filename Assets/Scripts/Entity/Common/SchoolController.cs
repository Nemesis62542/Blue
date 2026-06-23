/****************************************		
	Copyright 2015 Unluck Software	
 	www.chemicalbliss.com
 	
 	Changelog
 	v1.2
 	IMPORTANT!	_posBuffer is now used for flock position, not controller position.
 	Changed from _roamers Array To List
 	Code cleanup
	v1.21
	Added grouping
	Fixed gizmo bug
	v1.3																																																																																																																		
*****************************************/

using UnityEngine;
using System.Collections.Generic;


public class SchoolController : MonoBehaviour
{
	
	public SchoolChild[] _childPrefab;			// Assign prefab with SchoolChild script attached
	public bool _groupChildToNewTransform;	// Parents fish transform to school transform
	public Transform _groupTransform;			// New game object created for group
	public string _groupName = "";				// Name of group (if no name, the school name will be used)
	public bool _groupChildToSchool;			// Parents fish transform to school transform
	public int _childAmount = 250;				// Number of objects
	public float _spawnSphere = 3.0f;				// Range around the spawner waypoints will created //changed to box
	public float _spawnSphereDepth = 3.0f;			
	public float _spawnSphereHeight = 1.5f;		
	public float _childSpeedMultipler = 2.0f;		// Adjust speed of entire school
	public float _minSpeed = 6.0f;					// minimum random speed
	public float _maxSpeed = 10.0f;				// maximum random speed
	public AnimationCurve _speedCurveMultiplier = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));
	public float _minScale = .7f;				// minimum random size
	public float _maxScale = 1.0f;					// maximum random size
	public float _minDamping = 1.0f;				// Rotation tween damping, lower number = smooth/slow rotation (if this get stuck in a loop, increase this value)
	public float _maxDamping = 2.0f;
	public float _waypointDistance = 1.0f;			// How close this can get to waypoint before creating a new waypoint (also fixes stuck in a loop)
	public float _minAnimationSpeed = 2.0f;
	public float _maxAnimationSpeed = 4.0f;		
	public float _randomPositionTimerMax = 10.0f;	// When _autoRandomPosition is enabled
	public float _randomPositionTimerMin = 4.0f;	
	public float _acceleration = .025f;			// How fast child speeds up
	public float _brake = .01f;					// How fast child slows down 
	public float _positionSphere = 25.0f;			// If _randomPositionTimer is bigger than zero the controller will be moved to a random position within this sphere
	public float _positionSphereDepth = 5.0f;		// Overides height of sphere for more controll
	public float _positionSphereHeight = 5.0f;		// Overides height of sphere for more controll
	public bool _childTriggerPos;			// Runs the random position function when a child reaches the controller
	public bool _forceChildWaypoints;		// Forces all children to change waypoints when this changes position
	public bool _autoRandomPosition;			// Automaticly positions waypoint based on random values (_randomPositionTimerMin, _randomPositionTimerMax)
	public float _forcedRandomDelay = 1.5f;		// Random delay added before forcing new waypoint
	public float _schoolSpeed;					// Value multiplied to child speed
	public List<SchoolChild> _roamers;
	public Vector3 _posBuffer;
	public Vector3 _posOffset;
	
	///AVOIDANCE
	public bool _avoidance;				//Enable/disable avoidance
	public float _avoidAngle = 0.35f; 		//Angle of the rays used to avoid obstacles left and right
	public float _avoidDistance = 1.0f;		//How far avoid rays travel
	public float _avoidSpeed = 75.0f;			//How fast this turns around when avoiding	
	public float _stopDistance	= .5f;		//How close this can be to objects directly in front of it before stopping and backing up. This will also rotate slightly, to avoid "robotic" behaviour
	public float _stopSpeedMultiplier = 2.0f;	//How fast to stop when within stopping distance
	public LayerMask _avoidanceMask = (LayerMask)(-1);
	
	///PUSH
	public bool _push;					//Enable/disable push
	public float _pushDistance;				//How far away obstacles can be before starting to push away
	public float _pushForce = 5.0f;			//How fast/hard to push away

	///BOIDS ALGORITHM
	public bool _boids = false;				//Enable/disable boids algorithm
	public float _neighborDistance = 5.0f;		//Distance to search for neighboring fish
	public float _separationDistance = 2.0f;	//Distance at which fish separate from each other
	public float _separationWeight = 1.5f;		//Strength of separation force
	public float _alignmentWeight = 1.0f;		//Strength of alignment force (matching velocity)
	public float _cohesionWeight = 1.0f;		//Strength of cohesion force (moving to group center)
	public LayerMask _fishLayer = -1;			//Layer mask for fish detection

	///LEADER FOLLOW SYSTEM
	public bool _leaderFollow = false;			//Enable/disable leader follow system
	[Range(0.05f, 0.5f)]
	public float _leaderRatio = 0.2f;			//Ratio of leaders in the school (5%-50%)
	public float _followDistance = 8.0f;		//Distance to search for leaders
	public float _followWeight = 2.0f;			//Strength of leader following force
	public float _leaderChangeInterval = 10.0f;	//Interval for changing leaders (seconds)
	private float _leaderChangeTimer = 0.0f;	//Timer for leader rotation
	public List<SchoolChild> _leaders;			//List of current leaders
	
	//FRAME SKIP
	public int _updateDivisor = 1;				//Skip update every N frames (Higher numbers might give choppy results, 3 - 4 on 60fps , 2 - 3 on 30 fps)
	public float _newDelta;
	public int _updateCounter;
	public int _activeChildren;
	
	public void Start() {
		_posBuffer = transform.position + _posOffset;
		_schoolSpeed = Random.Range(1.0f , _childSpeedMultipler);
		_leaders = new List<SchoolChild>();
		AddFish(_childAmount);

		// Initialize leaders if leader follow system is enabled
		if (_leaderFollow) {
			AssignLeaders();
		}

		Invoke(nameof(AutoRandomWaypointPosition), RandomWaypointTime());
	}
	
	public void Update() {
		if(_activeChildren > 0){
			if(_updateDivisor > 1){
				_updateCounter++;
			    _updateCounter = _updateCounter % _updateDivisor;
				_newDelta = Time.deltaTime*_updateDivisor;
			}else{
				_newDelta = Time.deltaTime;
			}

			// Update leader rotation
			UpdateLeaderRotation();
		}
	}
	
	public void InstantiateGroup(){
		if(_groupTransform != null) return;
		GameObject g = new GameObject();
		_groupTransform = g.transform;
		_groupTransform.position = transform.position;
		if(_groupName != ""){
			g.name = _groupName;
			return;
		}	
		g.name = transform.name + " Fish Container";
	}
	
	public void AddFish(int amount){
		if(_groupChildToNewTransform)InstantiateGroup();	
		for(int i=0;i<amount;i++){
			int child = Random.Range(0,_childPrefab.Length);
			SchoolChild obj = Instantiate(_childPrefab[child]);
			obj.SetSpawner(this);
		    _roamers.Add(obj);
			AddChildToParent(obj.transform);
		}	
	}
	
	public void AddChildToParent(Transform obj){	
	    if(_groupChildToSchool){
			obj.parent = transform;
			return;
		}
		if(_groupChildToNewTransform){
			obj.parent = _groupTransform;
			return;
		}
	}
	
	public void RemoveFish(int amount){
		SchoolChild dObj = _roamers[_roamers.Count - amount];
		_roamers.RemoveAt(_roamers.Count - amount);
		Destroy(dObj.gameObject);
	}
	
	//Set waypoint randomly inside box
	public void SetRandomWaypointPosition() {
		_schoolSpeed = Random.Range(1.0f , _childSpeedMultipler);
		Vector3 t = Vector3.zero;
		t.x = Random.Range(-_positionSphere, _positionSphere) + transform.position.x;
		t.z = Random.Range(-_positionSphereDepth, _positionSphereDepth) + transform.position.z;
		t.y = Random.Range(-_positionSphereHeight, _positionSphereHeight) + transform.position.y;
		_posBuffer = t;	
		if(_forceChildWaypoints){
			for (int i = 0; i < _roamers.Count; i++)
			{
				if (_roamers[i] != null)
				{
					_roamers[i].Wander(Random.value * _forcedRandomDelay);
				}
				else
				{
					_roamers.RemoveAt(i);
				}
			}	
		}
	}
	
	public void AutoRandomWaypointPosition() {
		if(_autoRandomPosition && _activeChildren > 0){
			SetRandomWaypointPosition();
		}
		CancelInvoke(nameof(AutoRandomWaypointPosition));
		Invoke(nameof(AutoRandomWaypointPosition), RandomWaypointTime());
	}
	
	public float RandomWaypointTime(){
		return Random.Range(_randomPositionTimerMin, _randomPositionTimerMax);
	}

	// Assign leaders randomly from all fish
	public void AssignLeaders() {
		if (_leaders == null) _leaders = new List<SchoolChild>();
		_leaders.Clear();

		int leaderCount = Mathf.Max(1, Mathf.RoundToInt(_roamers.Count * _leaderRatio));
		List<SchoolChild> shuffled = new List<SchoolChild>(_roamers);

		// Fisher-Yates shuffle
		for (int i = shuffled.Count - 1; i > 0; i--) {
			int j = Random.Range(0, i + 1);
			SchoolChild temp = shuffled[i];
			shuffled[i] = shuffled[j];
			shuffled[j] = temp;
		}

		// Assign first N fish as leaders
		for (int i = 0; i < leaderCount && i < shuffled.Count; i++) {
			if (shuffled[i] != null) {
				shuffled[i].SetAsLeader(true);
				_leaders.Add(shuffled[i]);
			}
		}

		// Rest are followers
		for (int i = leaderCount; i < shuffled.Count; i++) {
			if (shuffled[i] != null) {
				shuffled[i].SetAsLeader(false);
			}
		}
	}

	// Update leader rotation timer
	private void UpdateLeaderRotation() {
		if (!_leaderFollow || _activeChildren == 0) return;

		_leaderChangeTimer += Time.deltaTime;

		if (_leaderChangeTimer >= _leaderChangeInterval) {
			_leaderChangeTimer = 0.0f;
			AssignLeaders();
		}
	}
	
	public void OnDrawGizmos() {
		if(!Application.isPlaying && _posBuffer != transform.position+ _posOffset) _posBuffer = transform.position + _posOffset;
	   	Gizmos.color = Color.blue;
		Gizmos.DrawWireCube (_posBuffer, new Vector3(_spawnSphere*2, _spawnSphereHeight*2 ,_spawnSphereDepth*2));
	    Gizmos.color = Color.cyan;
	    Gizmos.DrawWireCube (transform.position, new Vector3((_positionSphere*2)+_spawnSphere*2, (_positionSphereHeight*2)+_spawnSphereHeight*2 ,(_positionSphereDepth*2)+_spawnSphereDepth*2));
	}
}
