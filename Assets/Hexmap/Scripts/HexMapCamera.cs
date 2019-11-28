using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class HexMapCamera : MonoBehaviour {

	public float stickMinZoom, stickMaxZoom;

	public float swivelMinZoom, swivelMaxZoom;

	public float moveSpeedMinZoom, moveSpeedMaxZoom;

	public float rotationSpeed;

	Transform swivel, stick;

	public HexGrid grid;

	float zoom = 0.8f;

	float rotationAngle;

	static HexMapCamera instance;

	public static bool Locked {
		set {
			instance.enabled = !value;
		}
	}

	public static void ValidatePosition () {
		instance.AdjustPosition(0f, 0f);
	}

	void Awake () {
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
		AdjustZoom(0);
	}

	void OnEnable () {
		instance = this;
		ValidatePosition();
	}
	
	// 触屏双指缩放屏幕:
	public Vector2 st0, st1, od0, od1;

	void Update ()
	{
		#if UNITY_STANDALONE_WIN || UNITY_EDITOR
			float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		#else
			float zoomDelta = GetZoomDelta();
		#endif
		//if (zoomDelta != 0f)
		{
			AdjustZoom(zoomDelta);
		}

		float rotationDelta = Input.GetAxis("Rotation");
		if (rotationDelta != 0f) {
			AdjustRotation(rotationDelta);
		}

		float xDelta = Input.GetAxis("Horizontal");
		float zDelta = Input.GetAxis("Vertical");
		if (xDelta != 0f || zDelta != 0f) {
			AdjustPosition(xDelta, zDelta);
		}
		
		CameraMove();
	}

	void AdjustZoom (float delta)
	{
		zoom = Mathf.Clamp01(zoom + delta);

		float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
		
//		// 因为摄影机距离地面的距离如果太近的话, 会导致穿帮, 所以要根据当前视口中心所在地块的高度来重置摄影机的位置
//		if (Math.Abs(zoom - 1f) < 0.01f)
//		{
//			Ray ray = HexGameUI.CurrentCamera.ScreenPointToRay(Input.mousePosition);
//			RaycastHit hit;
//			if (Physics.Raycast(ray, out hit)) {
//				HexCell currentCell = grid.GetCell(hit.point);
//				float dist = hit.distance;
//				if (currentCell != null)
//				{
//					distance = distance + (15f-currentCell.Position.y);
//				}
//			}
//		}
//
		stick.localPosition = new Vector3(0f, 0f, distance);

		float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
		swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

	void AdjustRotation (float delta) {
		rotationAngle += delta * rotationSpeed * Time.deltaTime;
		if (rotationAngle < 0f) {
			rotationAngle += 360f;
		}
		else if (rotationAngle >= 360f) {
			rotationAngle -= 360f;
		}
		transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
	}

	void AdjustPosition (float xDelta, float zDelta) {
		Vector3 direction =
			transform.localRotation *
			new Vector3(xDelta, 0f, zDelta).normalized;
		float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
		float distance =
			Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) *
			damping * Time.deltaTime;

		Vector3 position = transform.localPosition;
		position += direction * distance;
		transform.localPosition =
			grid.wrapping ? WrapPosition(position) : ClampPosition(position);
	}

	Vector3 ClampPosition (Vector3 position) {
		float xMax = (grid.cellCountX - 0.5f) * HexMetrics.innerDiameter;
		position.x = Mathf.Clamp(position.x, 0f, xMax);

		float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		return position;
	}

	Vector3 WrapPosition (Vector3 position) {
		float width = grid.cellCountX * HexMetrics.innerDiameter;
		while (position.x < 0f) {
			position.x += width;
		}
		while (position.x > width) {
			position.x -= width;
		}

		float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		grid.CenterMap(position.x);
		return position;
	}
	
	// 鼠标右键控制拖动。Oct.22.2019. Liu Gang.
	private bool _leftMouseDown;
	private bool _rightMouseDown;
	private Vector3 _lastMousePos;
	private readonly float mouseMoveSpeed = 0.2f;

	private Texture2D _oldCursorTex;
	
	/// <summary>
	/// 鼠标右键控制拖动。Oct.22.2019. Liu Gang.
	/// </summary>
	public void CameraMove()
	{
		bool over = IsPointerOverUIObject(Input.mousePosition);
		if (over)
		{
			return;
		}
		if (Input.GetMouseButton(0))
		{
			if (Input.touchCount > 1)
			{ // 如果多于一个手指触摸到屏幕上,就等于是缩放屏幕了, 不算按下状态
				_leftMouseDown = false;
				return;
			}
			Vector3 mousePos = Input.mousePosition;
			if (!_leftMouseDown)
			{
				//鼠标图标换成自定义小手
				if (CursorManager.Instance)
				{
					CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.CAMERA_MOVE);
				}
				_lastMousePos = mousePos;
				_leftMouseDown = true;
			}

			Vector3 deltaMousePos = mousePos - _lastMousePos;
			transform.position += new Vector3(-deltaMousePos.x, 0, -deltaMousePos.y) * mouseMoveSpeed;
			_lastMousePos = mousePos;
			//Debug.Log("DeltaMousePos <"+deltaMousePos.y+">");
		}
		else
		{
			if (_leftMouseDown)
			{
				//鼠标恢复默认图标，置null即可
				if (CursorManager.Instance)
				{
					CursorManager.Instance.RestoreCursor();
				}
				_leftMouseDown = false;
			}
		}
	}

	public void SetPosition(HexCell cell)
	{
		SetPosition(cell.Position);
	}

	public void SetPosition(Vector3 pos)
	{
		transform.position = new Vector3(pos.x, 0, pos.z);
	}

	public Vector3 GetPosition()
	{
		return transform.position;
	}

	private float GetZoomDelta()
	{
		//判断是否双手触控
		if (Input.touchCount >= 2)
		{
			if (Input.GetTouch(1).phase == TouchPhase.Began)
			{
				//如果刚开始双手触控，记录位置不做处理
				st0 = od0 = Input.GetTouch(0).position;
				st1 = od1 = Input.GetTouch(1).position;
				return 0;
			}

			//获得新的手指位置
			st0 = Input.GetTouch(0).position;
			st1 = Input.GetTouch(1).position;

			//如果双指之间的距离变化了，视野缩放
			float delta = (Vector2.Distance(st0, st1) - Vector2.Distance(od0, od1)) / 1000;
			//给老坐标赋值
			od0 = st0;
			od1 = st1;
			return delta;
		}

		return 0;
	}
	
	//    ————————————————
	//    版权声明：本文为CSDN博主「SunnyIncsdn」的原创文章，遵循 CC 4.0 BY-SA 版权协议，转载请附上原文出处链接及本声明。
	//    原文链接：https://blog.csdn.net/SunnyInCSDN/article/details/72470247
	public static bool IsPointerOverUIObject(Vector3 mousePosition) {//判断是否点击的是UI，有效应对安卓没有反应的情况，true为UI
		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
		eventDataCurrentPosition.position = new Vector2(mousePosition.x, mousePosition.y);
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		return results.Count > 0;
	}


}