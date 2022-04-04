using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VCraft
{
	public class CameraController : MonoBehaviour
	{
		Camera cam;
		Vector3 center;
		Vector2 pressedPos;
		Vector2 mousePosPrev;
		bool isPressed;
		bool isDragged;
		bool isAuto = true;
		bool isMoving = false;
		bool isDisableMotion = false;
		Coroutine coroutine;
		float pinchDistPrev;
		bool isIgnoreClick;
		float ignoreClickTime;

		const float CENTER_Y = 1.0f;
		const float ROTATE_RATE = 0.4f;
		const float DRAG_MARGIN = 5f;

		static readonly float[,] cameraTransforms = { // position.x, y, z rotation x,y,z
			{ 0f, 1f, 2f, 0f, 180f, 0f},
			{ -2f, 1.5f, 2f, 5f, 160f, 0f },
			{ 2f, 0.5f, 2f, -15f, 200f, 0f },
			{ 0f, 2f, 2f, 20f, 180f, 0f },
			{ 0f, 3f, 2f, 30f, 180f, 0f },
			{ -1f, 0.1f, 1.5f, -30f, 160f, 0f },
			{ 0f, 0f, 2f, -30f, 180f, 0f },
			{ 3f, 3f, 2.5f, 15f, 200f, 0f },

			{ -10f, 8f, 6f, 20f, 160f, 0f },
			{ -2f, 2.5f, 1f, 20f, 120f, 0f },
			{ 4f, 4f, 4f, 10f, 240f, 0f },
			{ 0f, 1f, 2f, 0f, 180f, 0f},
			{ 1f, 0.5f, 2f, -15f, 200f, 0f },
//			{ -10f, 8f, 6f, 20f, 160f, 0f },
			{ 0f, 5f, -9f, 15f, 0f, 0f },
		};

		Taiso taiso;

		void Start() {
			Debug.Log("Start CameraController");
			cam = Camera.main;
			mousePosPrev = Input.mousePosition;
			taiso = FindObjectOfType<Taiso>();
		}

		void Update() {
			Vector2 mousePos = Input.mousePosition;
			bool inScreen = mousePos.x >= 0 && mousePos.x < Screen.width && mousePos.y >= 0 && mousePos.y < Screen.height;

			if (isIgnoreClick && Time.time - ignoreClickTime > 0.2f) {
				isIgnoreClick = false;
			}

			if (Input.touchCount < 2) {
				if (Input.GetMouseButtonDown(0)) {
					if (!isIgnoreClick) {
						Debug.Log("mouse down: " + mousePos);
						int fingerId = (Input.touchCount > 0) ? Input.GetTouch(0).fingerId : -1;
						if (!EventSystem.current.IsPointerOverGameObject(fingerId) && inScreen) {
							isPressed = true;
							pressedPos = mousePos;
						}
						isDragged = false;
					}
				}
				if (Input.GetMouseButtonUp(0)) {
					if (isIgnoreClick) {
						isIgnoreClick = false;
					} else {
						Debug.Log("mouse up: " + mousePos);
						if (isPressed && !isDragged) {
							Ray ray = cam.ScreenPointToRay(mousePos);
							RaycastHit hit;
							if (Physics.Raycast(ray, out hit)) {
								string name = hit.collider.gameObject.name;
								Debug.Log("raycast hit: " + name);
								if (name.StartsWith("chibi")) {
									changeCenterTo(hit.collider.gameObject);
								}
							}
						}
					}
					isPressed = false;
				}

				if (Input.GetMouseButton(0) && isPressed) {
					if (!isDragged) {
						if (Vector2.Distance(mousePos, pressedPos) >= DRAG_MARGIN) {
							isDragged = true;
							Debug.Log("mouse dragging started: " + mousePos);
						}
					}
					if (isDragged) {
						float dx = mousePos.x - mousePosPrev.x;
						float dy = mousePos.y - mousePosPrev.y;
						if (dx != 0 || dy != 0) { // TODO:
							isAuto = false;
							cam.transform.RotateAround(center, Vector3.up, dx * ROTATE_RATE);
							Vector3 axis = center - cam.transform.position;
							cam.transform.RotateAround(center, new Vector3(-axis.z, 0f, axis.x).normalized, dy * ROTATE_RATE);
							Vector3 angles = cam.transform.eulerAngles;
							cam.transform.eulerAngles = new Vector3(angles.x, angles.y, 0f);
						}
					}
				}
				mousePosPrev = mousePos;

			}

			if (Input.mouseScrollDelta.y != 0 && inScreen) {
				cam.transform.position += cam.transform.forward * Input.mouseScrollDelta.y * 0.5f;
			}

			if (Input.touchCount >= 2) {
				Touch t1 = Input.GetTouch(0);
				Touch t2 = Input.GetTouch(1);
				if (t2.phase == TouchPhase.Began) {
					pinchDistPrev = Vector2.Distance(t1.position, t2.position);
					Debug.Log("pinch started: " + pinchDistPrev);
				}  else if (t1.phase == TouchPhase.Moved && t2.phase == TouchPhase.Moved) {
					float dist = Vector2.Distance(t1.position, t2.position);
					Debug.Log("pinching: " + dist);
					cam.transform.position += cam.transform.forward * (dist - pinchDistPrev) * 0.01f;
					pinchDistPrev = dist;
				}
			}
		}

		public void setIgnoreClick() {
			isIgnoreClick = true;
			ignoreClickTime = Time.time;
		}

		public void setCenter(Vector3 pos) {
			center = new Vector3(pos.x, CENTER_Y, pos.z);
		}


		void changeCenterTo(GameObject go) {
			isAuto = false;
			Vector3 newCenter = go.transform.position;
			newCenter.y = CENTER_Y;
			StartCoroutine(moveCenter(newCenter));
		}

		IEnumerator moveCenter(Vector3 newCenter) {
			bool beReset = newCenter.Equals(center);
			Vector3 camPos = cam.transform.position;
			Vector3 newCamPos = beReset ? new Vector3(center.x, 1f, center.z + 2f) : camPos + (newCenter - center);
			Quaternion camRotation = cam.transform.rotation;
			Quaternion newCamRotation = Quaternion.Euler(0f, 180f, 0f);
			const float ELAPSE = 0.5f;
			float startTime = Time.time;
			for (;;) {
				if (Time.time - startTime >= ELAPSE) break;
				float t = (Time.time - startTime) / ELAPSE;
				cam.transform.position = Vector3.Lerp(camPos, newCamPos, t);
				if (beReset) {
					cam.transform.rotation = Quaternion.Lerp(camRotation, newCamRotation, t);
				}
				yield return null;
			}
			center = newCenter;
			yield break;
		}

		public void setMotion(int index, float elapse) {
			if (isDisableMotion && index > 2) return;
			Debug.Log("camera: setMotion #" + index);
			isAuto = true;
			if (isMoving && (index == 0 || index == 6 || index == 13)) {
				StopCoroutine(coroutine);
			}
			isMoving = false;
			switch (index) {
				case 0:
					coroutine = StartCoroutine(moveCamera(8, elapse / 2, elapse / 2));
					break;
				case 1:
				case 2:
				case 12:
					setTransform(0);
					break;

				case 3:
					setTransform(1);
					break;
				case 4:
					setTransform(2);
					break;
				case 5:
					setTransform(3);
					break;
				case 6:
					var span = taiso.getPlayersSpan();
					startPanCamera(span.x, -span.x, 0.5f, 2f, -15f, elapse);
					break;
				case 7:
					setTransform(3);
					break;
				case 8:
					setTransform(4);
					break;
				case 9:
					setTransform(5);
					break;
				case 10:
					setTransform(6);
					break;
				case 11:
					setTransform(7);
					break;
				case 13:
					setTransform(12);
					coroutine = StartCoroutine(moveCamera(12, elapse/2, 0f, elapse / 2));
					break;
			}
		}

		public void setDisableMotion(bool isDisable) {
			this.isDisableMotion = isDisable;
		}

		(Vector3 pos, Vector3 rot) getTransform(int index) {
			float[,] ct = cameraTransforms;
			Vector3 position = new Vector3(ct[index, 0], ct[index, 1], ct[index, 2]);
			Vector3 rotation = new Vector3(ct[index, 3], ct[index, 4], ct[index, 5]);
			return (position, rotation);
		}

		void setTransform(int index) {
			var tf = getTransform(index);
			cam.transform.position = tf.pos;
			cam.transform.eulerAngles = tf.rot;
		}

		void startPanCamera(float fromX, float toX, float posY, float posZ, float rotX, float elapse) {
			Vector3 pos0 = new Vector3(fromX, posY, posZ);
			Vector3 pos1 = new Vector3(toX, posY, posZ);
			Vector3 rot = new Vector3(rotX, 180f, 0);
			coroutine = StartCoroutine(moveCamera((pos0, rot), (pos1, rot), elapse));
		}

		IEnumerator moveCamera((Vector3 pos, Vector3 rot)tf0, (Vector3 pos, Vector3 rot)tf1, float elapse) {
			float startTime = Time.time;
			isMoving = true;
			while (isAuto && isMoving) {
				if (Time.time - startTime >= elapse) break;
				float t = (Time.time - startTime) / elapse;
				cam.transform.position = Vector3.Lerp(tf0.pos, tf1.pos, t);
				cam.transform.eulerAngles = Vector3.Lerp(tf0.rot, tf1.rot, t);
				yield return null;
			}
			isMoving = false;
			yield break;
		}

		IEnumerator moveCamera(int index, float elapse, float elapseNext = 0f, float wait = 0f) {
			if (wait != 0f) {
				 yield return new WaitForSeconds(wait);
			}
			var tf0 = getTransform(index);
			var tf1 = getTransform(index + 1);
			float startTime = Time.time;
			isMoving = true;
			while (isAuto && isMoving) {
				if (Time.time - startTime >= elapse) break;
				float t = (Time.time - startTime) / elapse;
				cam.transform.position = Vector3.Lerp(tf0.pos, tf1.pos, t);
				cam.transform.eulerAngles = Vector3.Lerp(tf0.rot, tf1.rot, t);
				yield return null;
			}
			if (elapseNext != 0 && isMoving) {
				yield return moveCamera(index + 2, elapseNext);
			}
			isMoving = false;
			yield break;
		}
	}
}