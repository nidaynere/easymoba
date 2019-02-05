using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalDestroyer : MonoBehaviour {

	public float lifeTime = 5.0f;

	private void Start()
	{
		Destroy(gameObject,lifeTime);
	}
}
