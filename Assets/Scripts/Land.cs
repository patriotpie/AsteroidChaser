﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Land : MonoBehaviour {

	public enum LandingStage {
		CantLand,
		CanLand,
		Landing,
		Landed
	}
	
	public float LandingSpeed;
	public float Tolerance;

	private GameObject _Earth;
	private LandingStage _landingStage;
	private GameObject _landingObj;

	private SpriteRenderer _sr;
	private float _landedWhenThisClose = 2.5f;
	private Vector3 _vel;

	public Sprite RegularSprite;
	public Sprite LandingSprite;
	public Sprite LandedSprite;

	
	public Sprite RegularSpriteUpg;
	public Sprite LandingSpriteUpg;
	public Sprite LandedSpriteUpg;

	private float _landingStartTime;
	public float MaxLandingTime;

	//private GameObject _earthLandingTextObj;
	private GameObject _astLandingTextObj;
	private GameObject _landingTextBg;

	public bool Landing {
		get {
			return _landingStage == LandingStage.Landing;
		}
	}

	public bool Landed {
		get {
			return _landingStage == LandingStage.Landed;
		}
	}

	public bool CanLand {
		get {
			return _landingStage == LandingStage.CanLand;
		}
	}


	// Use this for initialization
	void Start () {
		_Earth = GameObject.Find ("Earth");
		_vel = new Vector3 (0, 0, 0);
		_sr = this.gameObject.GetComponent<SpriteRenderer> ();

		//_earthLandingTextObj = GameObject.Find ("earthText");
		_astLandingTextObj = GameObject.Find ("asteroidText");
		_landingTextBg = GameObject.Find ("landingTextBG");
		//_earthLandingTextObj.SetActive (false);
		_astLandingTextObj.SetActive (false);
		_landingTextBg.SetActive (false);

	}
	
	// Update is called once per frame
	void Update () {
		
		if (Landed) {
			transform.position = _landingObj.transform.position;
			if (_landingObj == _Earth) {
				if (Input.GetButtonDown("Fire1") || Input.GetKey (KeyCode.B)) {
					UpgradeMech();
				}
			}
		}
		
		if (Landed && Input.GetButtonDown ("Jump")) {
			BlastOff ();
		}

		if (Landing) {
			transform.position = Vector3.SmoothDamp (transform.position, _landingObj.transform.position, ref _vel, LandingSpeed);
			if (Vector3.Distance(transform.position, _landingObj.transform.position) <= _landedWhenThisClose
			    || Time.time - _landingStartTime >= MaxLandingTime)
			{
				FinalizeLanding();
			}
		}

		if (CanLand && Input.GetButtonDown ("Jump")) {
			InitiateLandingSequence ();
		}
	}

//	void OnGUI()
//	{
//		if (CanLand) {
//			string msg;
//			if (IsAsteroid(_landingObj)) {
//				msg = "PRESS Y TO LAND ON " + _landingObj.name + "\n(" + _landingObj.GetComponent<AsteroidValue>().Value + " RESOURCES)";
//			}
//			else {
//				msg = "PRESS Y TO LAND ON " + _landingObj.name;
//			}
//			GUI.Box (new Rect (new Vector2 (Screen.width / 2 - 100, Screen.height / 2 - 100), new Vector2 (200, 200)), msg);
//		} else if (Landed) {
//			string msg;
//			if (_landingObj == _Earth) {
//				msg = "PRESS Y TO BLAST OFF\nOR A TO BUY UPGRADE";
//			} else {
//				msg = "PRESS Y TO BLAST OFF";
//			}
//			GUI.Box (new Rect (new Vector2 (Screen.width / 2 - 100, Screen.height / 2 - 100), new Vector2 (200, 200)), msg);
//		}
//	}
		
	void OnTriggerEnter2D(Collider2D other) {

		if (other.gameObject == _Earth) {

			if (_landingStage != LandingStage.CantLand) {

				switch (_landingStage) {

				case LandingStage.CanLand:
					CancelLandingPreparations(_landingObj);
					break;
				case LandingStage.Landing:
					FinalizeLanding();
					BlastOff();
					break;
				case LandingStage.Landed:
					BlastOff();
					break;
				}
			}

			PrepareToLandOn(_Earth);

		} else if (other.gameObject.name.Contains ("AsteroidLandingZone")) {

			Debug.Log ("Entered asteroid landing zone");
			// Test velocity to see if we can land
			if (_landingObj == null && CanLandOnAsteroid(other.gameObject.transform.parent.gameObject)) {

				PrepareToLandOn(other.gameObject.transform.parent.gameObject);

			} else {

				Debug.Log ("MATCH ITS SPEED");

			}
		}
	}

	void OnTriggerStay2D(Collider2D other) {

		if (IsAsteroidLandingZone (other.gameObject)) {

			if (_landingObj == null && !CanLandOnAsteroid (other.gameObject.transform.parent.gameObject)) {

				Debug.Log ("MATCH ITS SPEED");

			} else if (_landingObj == null && CanLandOnAsteroid (other.gameObject.transform.parent.gameObject)) {

				PrepareToLandOn (other.gameObject.transform.parent.gameObject);

			}
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (Landing)
			return;

		if (other.gameObject == _Earth && other.gameObject == _landingObj) {

			CancelLandingPreparations (_Earth);

		} else if (IsAsteroidLandingZone(other.gameObject)) {

			if (other.gameObject.transform.parent.gameObject == _landingObj) {

				CancelLandingPreparations (other.gameObject.transform.parent.gameObject);

			}
		}
	}

	void PrepareToLandOn(GameObject target) {
		_landingStage = LandingStage.CanLand;
		_landingObj = target;
		ShowText ("Y to land on " + _landingObj.name);
	}

	void CancelLandingPreparations (GameObject _Earth)
	{
		_landingStage = LandingStage.CantLand;
		_landingObj = null;
		HideText ();
	}

	void InitiateLandingSequence ()
	{
		_landingStage = LandingStage.Landing;
		_sr.sprite = LandingSprite;
		_landingStartTime = Time.time;
	}

	/// <summary>
	/// Finalizes the landing. Extracts resources, adds to inventory.
	/// </summary>
	void FinalizeLanding ()
	{
		_landingStage = LandingStage.Landed;
		_sr.sprite = LandedSprite;

		if (IsAsteroid (_landingObj)) {
			// Extract resources from this asteroid and add them to inventory
			this.GetComponent<Inventory> ().MineralXAmt += _landingObj.GetComponent<AsteroidValue> ().Extract ();
			ShowText ("Y to blast off");
		} else {
			Inventory i = this.GetComponent<Inventory>();
			i.returnedAmt += i.MineralXAmt;
			i.MineralXAmt = 0;
			ShowText ("A to upgrade; Y to blast off");
		}
	}

	void HideText() {
		_astLandingTextObj.SetActive (false);
		_landingTextBg.SetActive (false);
	}
	
	void ShowText(string str) {
		_astLandingTextObj.SetActive (true);
		_astLandingTextObj.GetComponent<Text> ().text = str;
		_landingTextBg.SetActive (true);
	}


	bool CanLandOnAsteroid(GameObject other) {
		// Compare velocities
		Move myMovement = gameObject.GetComponent<Move> ();
		Vector2 myVel = myMovement.Velocity;
		AsteroidMovement otherMovement = other.GetComponent<AsteroidMovement>();
		Vector2 otherVel = otherMovement.Velocity;
		
		float dist = Vector2.Distance(_vel, otherVel);
		return dist < Tolerance;
	}

	void BlastOff() {
		_landingStage = LandingStage.CantLand;
		_sr.sprite = RegularSprite;

		float radius = 0.0f;
		if (_landingObj == _Earth)
			radius = 10.0f;
		else if (IsAsteroid(_landingObj)) {
			radius = 13.0f;
		}

		Vector2 random = (new Vector2 (Random.Range (-1.0f, 1.0f), Random.Range (-1.0f, 1.0f))).normalized;
		Vector3 delta = radius * random;
		transform.position = transform.position + delta;

		
		_landingObj = null;
		HideText ();
	}

	void UpgradeMech ()
	{
		RegularSprite = RegularSpriteUpg;
		LandingSprite = LandingSpriteUpg;
		LandedSprite = LandedSpriteUpg;
		_sr.sprite = LandedSprite;

		GameObject.Find ("Ship").GetComponentInChildren<ParticleSystem> ().startSize = 4.00f;
		GameObject.Find ("Ship").GetComponentInChildren<ParticleSystem> ().emissionRate = 500.0f;
		GameObject.Find ("Ship").GetComponent<Move> ().SpeedScale = 3.0f;
		GameObject.Find ("Ship").GetComponent<Move> ().MaxSpeed = 0.4f;
	}

	bool IsAsteroid (GameObject _landingObj)
	{
		return _landingObj.name.Contains ("Asteroid");
	}

	bool IsAsteroidLandingZone(GameObject other) {
		return other.name.Contains("AsteroidLandingZone");
	}

}
