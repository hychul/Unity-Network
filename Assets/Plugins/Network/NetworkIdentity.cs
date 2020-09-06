using UnityEngine;

public abstract class NetworkIdentity : MonoBehaviour {
	
	protected int objectId;

	public bool isLoaded;

	/*
	 * +-------------------------+
	 * |       Initiailize       |
	 * +-------------------------+
	 */

	public void Initialize(int objectId) {
		this.objectId = objectId;
		this.isLoaded = false;
	}

	/*
	 * +-------------------------+
	 * |          Sync           |
	 * +-------------------------+
	 */

	public virtual void SyncAwake() {
		gameObject.SetActive(false);
	}

	public virtual void SyncResume() {
		gameObject.SetActive(true);
	}

	public abstract void SyncStart(NetworkEntity networkEntity);

	public abstract void SyncUpdate(NetworkEntity networkEntity);

	public virtual void SyncPause() {
		gameObject.SetActive(false);
	}
}
