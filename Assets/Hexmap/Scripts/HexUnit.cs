﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class HexUnit : MonoBehaviour {

	const float rotationSpeed = 180f;
	private float travelSpeed = 4f;

	public static HexUnit unitPrefab;

	public HexGrid Grid { get; set; }

	public HexCell Location {
		get {
			return location;
		}
		set {
			if (location) {
				Grid.DecreaseVisibility(location, VisionRange);
				location.Unit = null;
			}
			location = value;
			value.Unit = this;
			Grid.IncreaseVisibility(value, VisionRange);
			transform.localPosition = value.Position;
			Grid.MakeChildOfColumn(transform, value.ColumnIndex);
		}
	}

	HexCell location, currentTravelLocation;

	public float Orientation {
		get {
			return orientation;
		}
		set {
			orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}

	public int Speed {
		get {
			return 24;
		}
	}

	public int VisionRange {
		get {
			return 3;
		}
	}

	float orientation;

	List<HexCell> pathToTravel;

	public void ValidateLocation () {
		transform.localPosition = location.Position;
	}

	public bool IsValidDestination (HexCell cell) {
		// 去掉视野范围, 所有地方都可以走到...Nov.21.2019. Liu Gang.
//		if(cell.Unit)
//			Debug.LogWarning($"HexUnit - IsValidDestination - cell is ocuppied! - {cell.coordinates}");
		//return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
		return !cell.IsUnderwater && !cell.Unit;
	}

	public void Stop()
	{
		pathToTravel = null;
		StopAllCoroutines();
	}

	// 最简单的走路过程
//	IEnumerator TravelPath () {
//	
//		for (int i = 1; i < pathToTravel.Count; i++) 
//		{
//			Vector3 a = pathToTravel[i - 1].Position;
//			Vector3 b = pathToTravel[i].Position;
//			for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed) {
//				transform.localPosition = Vector3.Lerp(a, b, t);
//				yield return null;
//			}
//		}		
//	}

	private HexCell lastLocation;
	public void Travel (List<HexCell> path, float speed) {
//		location.Unit = null;
//		location = path[path.Count - 1];
//		location.Unit = this;
		lastLocation = location;
		pathToTravel = path;
		travelSpeed = speed;
		StopAllCoroutines();
		StartCoroutine(TravelPath());
	}

	IEnumerator TravelPath () {
		Vector3 a, b, c = transform.localPosition;
		yield return LookAt(pathToTravel[1].Position);

		if (!currentTravelLocation) {
			currentTravelLocation = pathToTravel[0];
		}
		Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
		int currentColumn = currentTravelLocation.ColumnIndex;
		float originalTravelSpeed = travelSpeed;

		float t = Time.deltaTime * travelSpeed;
		for (int i = 1; i < pathToTravel.Count; i++) {
			currentTravelLocation = pathToTravel[i];
			a = c;
			if (i == 1)
				b = transform.localPosition;
			else
				b = pathToTravel[i - 1].Position;

			int nextColumn = currentTravelLocation.ColumnIndex;
			if (currentColumn != nextColumn) {
				if (nextColumn < currentColumn - 1) {
					a.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
					b.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
				}
				else if (nextColumn > currentColumn + 1) {
					a.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
					b.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
				}
				Grid.MakeChildOfColumn(transform, nextColumn);
				currentColumn = nextColumn;
			}

			c = (b + currentTravelLocation.Position) * 0.5f;
			Grid.IncreaseVisibility(pathToTravel[i], VisionRange);

			for (; t < 1f; t += Time.deltaTime * travelSpeed) {
				Vector3 pos = Bezier.GetPoint(a, b, c, t);
				
				
				HexCell locationNew = Grid.GetCell(pos);
				if (locationNew.Unit != null && locationNew.Unit != this) // 别人占了这个坑
				{
					// 空转。。。等待在这里
					t -= Time.deltaTime * travelSpeed;
				}
				else
				{
					lastLocation.SetLabel("");
					lastLocation = location;
					location.Unit = null;
					location = locationNew;
					location.Unit = this;
					location.SetLabel(location.GetLabelStr(1));
					
					pos.y = location.Position.y;
					transform.localPosition = pos;
					
					// 获取本地块的速度
					travelSpeed = originalTravelSpeed * Grid.GetCellSpeed(location);
				
					Vector3 d = Bezier.GetDerivative(a, b, c, t);
					d.y = 0f;
					transform.localRotation = Quaternion.LookRotation(d);
				}
				yield return null;
			}
			Grid.DecreaseVisibility(pathToTravel[i], VisionRange);
			t -= 1f;
		}
		currentTravelLocation = null;

		// 最后一个节点，要单独设置一下
		lastLocation = location;
		location.Unit = null;
		location = pathToTravel[pathToTravel.Count - 1];
		location.Unit = this;
		lastLocation.SetLabel(lastLocation.GetLabelStr(1));
		location.SetLabel(location.GetLabelStr(1));
		
		a = c;
		b = location.Position;
		c = b;
		Grid.IncreaseVisibility(location, VisionRange);
		for (; t < 1f; t += Time.deltaTime * travelSpeed) {
			Vector3 pos = Bezier.GetPoint(a, b, c, t);
				
			HexCell locationNew = Grid.GetCell(pos);
			if (locationNew.Unit != null && locationNew.Unit != this) // 别人占了这个坑
			{
				// 空转。。。等待在这里
				t -= Time.deltaTime * travelSpeed;
			}
			else
			{
				lastLocation.SetLabel("");
				lastLocation = location;
				location.Unit = null;
				location = locationNew;
				location.Unit = this;
				location.SetLabel(location.GetLabelStr(1));
				pos.y = location.Position.y;
				transform.localPosition = pos;
				
				// 获取本地块的速度
				travelSpeed = originalTravelSpeed * Grid.GetCellSpeed(location);
				
				Vector3 d = Bezier.GetDerivative(a, b, c, t);
				d.y = 0f;
				transform.localRotation = Quaternion.LookRotation(d);
			}
			yield return null;
		}

		transform.localPosition = location.Position;
		orientation = transform.localRotation.eulerAngles.y;
		ListPool<HexCell>.Add(pathToTravel); // 还回缓冲池
		pathToTravel = null;
	}

	public IEnumerator LookAt (Vector3 point) {
		if (HexMetrics.Wrapping) {
			float xDistance = point.x - transform.localPosition.x;
			if (xDistance < -HexMetrics.innerRadius * HexMetrics.wrapSize) {
				point.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
			}
			else if (xDistance > HexMetrics.innerRadius * HexMetrics.wrapSize) {
				point.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
			}
		}

		point.y = transform.localPosition.y;
		Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation =
			Quaternion.LookRotation(point - transform.localPosition);
		float angle = Quaternion.Angle(fromRotation, toRotation);

		if (angle > 0f) {
			float speed = rotationSpeed / angle;
			for (
				float t = Time.deltaTime * speed;
				t < 1f;
				t += Time.deltaTime * speed
			) {
				transform.localRotation =
					Quaternion.Slerp(fromRotation, toRotation, t);
				yield return null;
			}
		}

		transform.LookAt(point);
		orientation = transform.localRotation.eulerAngles.y;
	}

	public int GetMoveCost (
		HexCell fromCell, HexCell toCell, HexDirection direction)
	{
		if (!IsValidDestination(toCell)) {
			return -1;
		}
		HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
		if (edgeType == HexEdgeType.Cliff) {
			return -1;
		}
		int moveCost;
		if (fromCell.HasRoadThroughEdge(direction)) {
			moveCost = 1;
		}
		else if (fromCell.Walled != toCell.Walled) {
			//return -1;
			// 现在改为城墙没有阻挡作用
			return 2;
		}
		else {
			moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
			moveCost +=
				toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel + toCell.MineLevel;
		}
		return moveCost;
	}

	public void Die () {
		if (location) {
			Grid.DecreaseVisibility(location, VisionRange);
		}
		location.Unit = null;
		Destroy(gameObject);
	}

	public void Save (BinaryWriter writer) {
		location.coordinates.Save(writer);
		writer.Write(orientation);
	}

	public static void Load (BinaryReader reader, HexGrid grid) {
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();
		grid.AddUnit(
			Instantiate(unitPrefab), grid.GetCell(coordinates), orientation
		);
	}

	void OnEnable () {
		if (location) {
			transform.localPosition = location.Position;
			if (currentTravelLocation) {
				Grid.IncreaseVisibility(location, VisionRange);
				Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
				currentTravelLocation = null;
			}
		}
	}

//	void OnDrawGizmos () {
//		if (pathToTravel == null || pathToTravel.Count == 0) {
//			return;
//		}
//
//		Vector3 a, b, c = pathToTravel[0].Position;
//
//		for (int i = 1; i < pathToTravel.Count; i++) {
//			a = c;
//			b = pathToTravel[i - 1].Position;
//			c = (b + pathToTravel[i].Position) * 0.5f;
//			for (float t = 0f; t < 1f; t += 0.1f) {
//				Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
//			}
//		}
//
//		a = c;
//		b = pathToTravel[pathToTravel.Count - 1].Position;
//		c = b;
//		for (float t = 0f; t < 1f; t += 0.1f) {
//			Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
//		}
//	}
}