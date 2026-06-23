/**************************************
	Copyright 2015 Unluck Software
 	www.chemicalbliss.com
***************************************/

using Blue.Attack;
using Blue.Entity;
using Blue.Interface;
using UnityEngine;


public class SchoolChild : MonoBehaviour, ILivingEntity
{
	[SerializeField] private Transform _scanner;

    private SchoolController _spawner;
    private Vector3 _wayPoint;
    private float _speed = 10.0f;
    private float _stuckCounter;
    private float _damping;
    private float _targetSpeed;
    private float tParam = 0.0f;
    private float _rotateCounterR;
    private float _rotateCounterL;
    private bool _scan = true;
    private int _updateNextSeed = 0;
    private int _updateSeed = -1;
    private Transform _cacheTransform;
    private Rigidbody _rigidbody;
    private Collider[] _neighborBuffer = new Collider[20]; // Buffer for neighbor search (reduces GC)
    private bool _isLeader = false;              // Whether this fish is a leader
    private SchoolChild _targetLeader = null;    // The leader this fish is following

#if UNITY_EDITOR
    private bool _sWarning;
#endif

	public SchoolController Spawner => _spawner;

	// ILivingEntity implementation
	public Status Status => null; // School fish don't have individual Status
	public float Size => _spawner._schoolThreatSize;
	public float ThreatSizeThreshold => _spawner._schoolThreatThreshold;

	public void Damage(AttackData attackData) {
		// School fish don't take individual damage
	}

	public void OnDead() {
		// School fish death handling (if needed)
	}

	public void Start()
	{
		if (_cacheTransform == null) _cacheTransform = transform;
		_rigidbody = GetComponent<Rigidbody>();
		if (_rigidbody == null)
		{
			_rigidbody = gameObject.AddComponent<Rigidbody>();
			_rigidbody.useGravity = false;
			_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
		}
		SetRandomScale();
		LocateRequiredChildren();
		SkewModelForLessUniformedMovement();
		_speed = Random.Range(_spawner._minSpeed, _spawner._maxSpeed);
		Wander(0.0f);
		SetRandomWaypoint();
		GetStartPos();
		FrameSkipSeedInit();
		_spawner._activeChildren++;
	}

    public void Update() {
        if (_spawner._updateDivisor <= 1 || _spawner._updateCounter == _updateSeed){
            CheckForDistanceToWaypoint();
        }
    }

    public void FixedUpdate() {
        if (_spawner._updateDivisor <= 1 || _spawner._updateCounter == _updateSeed){
            RotationBasedOnWaypointOrAvoidance();
            RayCastToPushAwayFromObstacles();
            ForwardMovement();
        }
    }

    public void OnDisable() {
        if (_spawner != null && _spawner._activeChildren > 0) {
            _spawner._activeChildren--;
        }
    }

    public void OnEnable() {
        if(_spawner != null){
            _spawner._activeChildren++;
        }
    }

    public void SetSpawner(SchoolController spawner) {
        _spawner = spawner;
    }

    public void Wander(float delay) {
        _damping = Random.Range(_spawner._minDamping, _spawner._maxDamping);
        _targetSpeed = Random.Range(_spawner._minSpeed, _spawner._maxSpeed) * _spawner._speedCurveMultiplier.Evaluate(Random.value) * _spawner._schoolSpeed;
        Invoke(nameof(SetRandomWaypoint), delay);
    }

    public void SetRandomWaypoint() {
        tParam = 0.0f;
        _wayPoint = findWaypoint();
    }

    private void FrameSkipSeedInit(){
		if(_spawner._updateDivisor > 1){
			int _updateSeedCap = _spawner._updateDivisor -1;
			_updateNextSeed++;
		    _updateSeed = _updateNextSeed;
		    _updateNextSeed = _updateNextSeed % _updateSeedCap;
		}
	}

    private void LocateRequiredChildren(){
		if(_scanner == null){
			_scanner = transform;
			_scanner.localRotation = Quaternion.identity;
			_scanner.localPosition = Vector3.zero;
		}
	}
	
	private void SkewModelForLessUniformedMovement() {
		// Adds a slight rotation to the model so that the fish get a little less uniformed movement	
		Quaternion rx = Quaternion.identity;
		rx.eulerAngles = new Vector3(0.0f, 0.0f , (float)Random.Range(-25, 25));
		transform.rotation = rx;
	}
	
	private void SetRandomScale(){
		float sc = Random.Range(_spawner._minScale, _spawner._maxScale);
		_cacheTransform.localScale=Vector3.one*sc;
	}
	
	private void GetStartPos(){
		//-Vector is to avoid zero rotation warning
		_cacheTransform.position = _wayPoint - new Vector3(.1f,.1f,.1f);
	}
	
	private Vector3 findWaypoint(){
		Vector3 t = Vector3.zero;
		t.x = Random.Range(-_spawner._spawnSphere, _spawner._spawnSphere) + _spawner._posBuffer.x;
		t.z = Random.Range(-_spawner._spawnSphereDepth, _spawner._spawnSphereDepth) + _spawner._posBuffer.z;
		t.y = Random.Range(-_spawner._spawnSphereHeight, _spawner._spawnSphereHeight) + _spawner._posBuffer.y;
		return t;
	}
	
	//Uses scanner to push away from obstacles
	private void RayCastToPushAwayFromObstacles() {
		if(_spawner._push){
			RotateScanner();
			RayCastToPushAwayFromObstaclesCheckForCollision();
		}
	}
	
	private void RayCastToPushAwayFromObstaclesCheckForCollision() {
		RaycastHit hit = new RaycastHit();
		float d = 0.0f;
		Vector3 cacheForward = _scanner.forward;
		if (Physics.Raycast(_cacheTransform.position, cacheForward, out hit, _spawner._pushDistance, _spawner._avoidanceMask)){		
			SchoolChild s = null;
			s = hit.transform.GetComponent<SchoolChild>();	
			d = (_spawner._pushDistance - hit.distance)/_spawner._pushDistance;	// Equals zero to one. One is close, zero is far	
			if(s != null){
				_cacheTransform.position -= cacheForward*_spawner._newDelta*d*_spawner._pushForce;	
			}
			else{
				_speed -= .01f*_spawner._newDelta;
				if(_speed < .1f)
				_speed = .1f;
				_cacheTransform.position -= cacheForward*_spawner._newDelta*d*_spawner._pushForce*2;
				//Tell scanner to rotate slowly
				_scan = false;
			}					
		}else{
			//Tell scanner to rotate randomly
			_scan = true;
		}
	}
	
	private void RotateScanner() {
		//Scan random if not pushing
		if(_scan){
			_scanner.rotation = Random.rotation;
			return;
		}
		//Scan slow if pushing
		_scanner.Rotate(new Vector3(150*_spawner._newDelta,0.0f,0.0f));
	}
	
	private void CheckForDistanceToWaypoint(){
		if((_cacheTransform.position - _wayPoint).magnitude < _spawner._waypointDistance+_stuckCounter){
	      	Wander(0.0f);	//create a new waypoint
	        _stuckCounter=0.0f;
	        CheckIfThisShouldTriggerNewFlockWaypoint();
	        return;
	    }
	    _stuckCounter+=_spawner._newDelta*(_spawner._waypointDistance*.25f);
	}
	
	private void CheckIfThisShouldTriggerNewFlockWaypoint(){
		if(_spawner._childTriggerPos){
			_spawner.SetRandomWaypointPosition();
		}
	}
	
	private float ClampAngle(float angle,float min,float max) {
		if (angle < -360)angle += 360.0f;
		if (angle > 360)angle -= 360.0f;
		return Mathf.Clamp (angle, min, max);
	}

	private Vector3 CalculateBoidsForce() {
		if (!_spawner._boids) return Vector3.zero;

		Vector3 separation = Vector3.zero;
		Vector3 alignment = Vector3.zero;
		Vector3 cohesion = Vector3.zero;
		int neighborCount = 0;

		// Use OverlapSphereNonAlloc to reduce GC allocation
		int count = Physics.OverlapSphereNonAlloc(
			_cacheTransform.position,
			_spawner._neighborDistance,
			_neighborBuffer,
			_spawner._fishLayer
		);

		float separationDistanceSqr = _spawner._separationDistance * _spawner._separationDistance;

		for (int i = 0; i < count; i++) {
			if (_neighborBuffer[i].transform == _cacheTransform) continue;

			SchoolChild fish = _neighborBuffer[i].GetComponent<SchoolChild>();
			if (fish == null || fish.Spawner != _spawner) continue;

			Vector3 neighborPos = _neighborBuffer[i].transform.position;
			Vector3 offset = _cacheTransform.position - neighborPos;
			float distanceSqr = offset.sqrMagnitude;

			// Separation: Move away from fish that are too close (optimized with sqrMagnitude)
			if (distanceSqr < separationDistanceSqr && distanceSqr > 0.0001f) {
				float distance = Mathf.Sqrt(distanceSqr); // Only calculate sqrt when needed
				separation += offset / distance;
			}

			// Alignment: Match velocity with nearby fish
			alignment += fish._rigidbody.linearVelocity;

			// Cohesion: Move towards the center of nearby fish
			cohesion += neighborPos;

			neighborCount++;
		}

		if (neighborCount > 0) {
			alignment /= neighborCount;
			cohesion = (cohesion / neighborCount) - _cacheTransform.position;
		}

		return separation * _spawner._separationWeight +
		       alignment.normalized * _spawner._alignmentWeight +
		       cohesion.normalized * _spawner._cohesionWeight;
	}

	public void SetAsLeader(bool isLeader) {
		_isLeader = isLeader;
		if (_isLeader) {
			_targetLeader = null; // Leaders don't follow anyone
		}
	}

	public bool IsLeader() {
		return _isLeader;
	}

	// Find the nearest leader within follow distance
	private SchoolChild FindNearestLeader() {
		if (_spawner._leaders == null || _spawner._leaders.Count == 0) return null;

		SchoolChild nearest = null;
		float nearestDistanceSqr = _spawner._followDistance * _spawner._followDistance;

		foreach (SchoolChild leader in _spawner._leaders) {
			if (leader == null || leader == this) continue;

			// Use sqrMagnitude for performance
			float distanceSqr = (_cacheTransform.position - leader.transform.position).sqrMagnitude;

			if (distanceSqr < nearestDistanceSqr) {
				nearestDistanceSqr = distanceSqr;
				nearest = leader;
			}
		}

		return nearest;
	}

	// Calculate force to follow the leader
	private Vector3 CalculateLeaderFollowForce() {
		if (!_spawner._leaderFollow || _isLeader) return Vector3.zero;

		// Update target leader periodically (not every frame for performance)
		if (Random.value < 0.05f) { // ~5% chance per frame (~3 frames at 60fps)
			_targetLeader = FindNearestLeader();
		}

		if (_targetLeader == null) return Vector3.zero;

		// Follow behind the leader
		Vector3 leaderBack = _targetLeader.transform.position - _targetLeader.transform.forward * 2.0f;
		Vector3 targetPosition = leaderBack;

		return (targetPosition - _cacheTransform.position).normalized * _spawner._followWeight;
	}

	// Calculate force to flee from threats
	private Vector3 CalculateFleeForce() {
		if (!_spawner._dynamicDensity || !_spawner._hasThreat) return Vector3.zero;

		// Calculate direction away from threat
		Vector3 fleeDirection = _cacheTransform.position - _spawner._threatPosition;

		if (fleeDirection.magnitude < 0.01f) return Vector3.zero;

		return fleeDirection.normalized * _spawner._fleeWeight;
	}

    private bool Avoidance() {
		if(!_spawner._avoidance)
			return false;
		RaycastHit hit;
		float d;
		Quaternion rx = _rigidbody.rotation;
		Vector3 ex = _rigidbody.rotation.eulerAngles;
		Vector3 cacheForward = _cacheTransform.forward;
		Vector3 cacheRight = _cacheTransform.right;
		float randomRight = Random.Range(-.1f, .1f);

		Vector3[] upDownDirs = { -Vector3.up + (cacheForward * .1f), Vector3.up + (cacheForward * .1f) };
		float[] upDownSigns = { -1f, 1f };
		for (int i = 0; i < 2; i++) {
			if (Physics.Raycast(_cacheTransform.position, upDownDirs[i], out hit, _spawner._avoidDistance, _spawner._avoidanceMask)) {
				d = (_spawner._avoidDistance - hit.distance) / _spawner._avoidDistance;
				ex.x += upDownSigns[i] * _spawner._avoidSpeed * d * _spawner._newDelta * (_speed + 1);
				rx.eulerAngles = ex;
				_rigidbody.rotation = rx;
			}
		}

		//Crash avoidance //Checks for obstacles forward
		if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*randomRight), out hit, _spawner._stopDistance, _spawner._avoidanceMask)){
	//					Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._stopDistance - hit.distance)/_spawner._stopDistance;
					ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*(_targetSpeed +3);
					rx.eulerAngles = ex;
					_rigidbody.rotation = rx;
					_speed -= d*_spawner._newDelta*_spawner._stopSpeedMultiplier*_speed;
					if(_speed < 0.01f){
						_speed = 0.01f;
					}
					return true;
		}else if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*(_spawner._avoidAngle+_rotateCounterL)), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){
	//				Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;
					_rotateCounterL+=.1f;
					ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*_rotateCounterL*(_speed +1);
					rx.eulerAngles = ex;
					_rigidbody.rotation = rx;
					if(_rotateCounterL > 1.5f)
						_rotateCounterL = 1.5f;
					_rotateCounterR = 0.0f;
					return true;
		}else if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*-(_spawner._avoidAngle+_rotateCounterR)), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){
	//			Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;
					if(hit.point.y < _cacheTransform.position.y){
						ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);
					}
					else{
						ex.x += _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);
					}
					_rotateCounterR +=.1f;
					ex.y += _spawner._avoidSpeed*d*_spawner._newDelta*_rotateCounterR*(_speed +1);
					rx.eulerAngles = ex;
					_rigidbody.rotation = rx;
					if(_rotateCounterR > 1.5f)
						_rotateCounterR = 1.5f;
					_rotateCounterL = 0.0f;
					return true;
		}else{
			_rotateCounterL = 0.0f;
			_rotateCounterR = 0.0f;
		}
		return false;																	    																																				    																				
	}
	
	private void ForwardMovement(){
		Vector3 targetVelocity = _cacheTransform.TransformDirection(Vector3.forward) * _speed;
		_rigidbody.linearVelocity = targetVelocity;

		if (tParam < 1) {
			if(_speed > _targetSpeed){
				tParam += _spawner._newDelta * _spawner._acceleration;
			}else{
				tParam += _spawner._newDelta * _spawner._brake;
			}
			_speed = Mathf.Lerp(_speed, _targetSpeed,tParam);
		}
	}
	
	private void RotationBasedOnWaypointOrAvoidance(){
		Vector3 targetDirection = _wayPoint - _cacheTransform.position;

		// Add boids force to the target direction
		Vector3 boidsForce = CalculateBoidsForce();
		if (boidsForce.magnitude > 0.01f) {
			targetDirection += boidsForce;
		}

		// Add leader follow force
		Vector3 leaderForce = CalculateLeaderFollowForce();
		if (leaderForce.magnitude > 0.01f) {
			targetDirection += leaderForce;
		}

		// Add flee force from threats (highest priority)
		Vector3 fleeForce = CalculateFleeForce();
		if (fleeForce.magnitude > 0.01f) {
			targetDirection += fleeForce;
		}

		Quaternion rotation = Quaternion.LookRotation(targetDirection);
        if (!Avoidance()){
			_rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, rotation, _spawner._newDelta * _damping);
		}
		//Limit rotation up and down to avoid freaky behavior
		float angle = _rigidbody.rotation.eulerAngles.x;
	    angle = (angle > 180) ? angle - 360 : angle;
		Quaternion rx = _rigidbody.rotation;
	    Vector3 rxea = rx.eulerAngles;
	    rxea.x = ClampAngle(angle, -50.0f , 50.0f);
	    rx.eulerAngles = rxea;
		_rigidbody.rotation = rx;
	}
}
