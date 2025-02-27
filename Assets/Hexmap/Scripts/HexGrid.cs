﻿using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HexGrid : MonoBehaviour {

	public int cellCountX = 20, cellCountZ = 15;

	public bool wrapping;

	public int showLabel; // 增加是否显示label的开关，0-不显示，1-显示坐标；2-显示资源

	public HexCell cellPrefab;
	public Text cellLabelPrefab;
	public HexGridChunk chunkPrefab;
	public HexUnit unitPrefab;

	public Texture2D noiseSource;

	public int seed;

	public bool HasPath {
		get {
			return currentPathExists;
		}
	}

	Transform[] columns;
	HexGridChunk[] chunks;
	HexCell[] cells;

	int chunkCountX, chunkCountZ;

	HexCellPriorityQueue searchFrontier;

	int searchFrontierPhase;

	HexCell currentPathFrom, currentPathTo;
	bool currentPathExists;

	int currentCenterColumnIndex = -1;

	List<HexUnit> units = new List<HexUnit>();

	HexCellShaderData cellShaderData;
	
	// Oct.30.2019. Liu Gang
	HexResource[] resLayer; // 资源数据

	void Awake () {
		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitializeHashGrid(seed);
		HexUnit.unitPrefab = unitPrefab;
		cellShaderData = gameObject.AddComponent<HexCellShaderData>();
		cellShaderData.Grid = this;
		CreateMap(cellCountX, cellCountZ, wrapping);
	}

	public void AddUnit (HexUnit unit, HexCell location, float orientation) {
		units.Add(unit);
		unit.Grid = this;
		unit.Location = location;
		unit.Orientation = orientation;
	}

	public void RemoveUnit (HexUnit unit) {
		units.Remove(unit);
		unit.Die();
	}

	public void MakeChildOfColumn (Transform child, int columnIndex) {
		child.SetParent(columns[columnIndex], false);
	}

	public bool CreateMap (int x, int z, bool wrapping) {
		if (
			x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
			z <= 0 || z % HexMetrics.chunkSizeZ != 0
		) {
			Debug.LogError("Unsupported map size.");
			return false;
		}

		ClearPath();
		ClearUnits();
		if (columns != null) {
			for (int i = 0; i < columns.Length; i++) {
				Destroy(columns[i].gameObject);
			}
		}

		cellCountX = x;
		cellCountZ = z;
		this.wrapping = wrapping;
		currentCenterColumnIndex = -1;
		HexMetrics.wrapSize = wrapping ? cellCountX : 0;
		chunkCountX = cellCountX / HexMetrics.chunkSizeX;
		chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
		cellShaderData.Initialize(cellCountX, cellCountZ);
		CreateChunks();
		CreateCells();
		CreateResources();
		return true;
	}

	void CreateChunks () {
		columns = new Transform[chunkCountX];
		for (int x = 0; x < chunkCountX; x++) {
			columns[x] = new GameObject("Column").transform;
			columns[x].SetParent(transform, false);
		}

		chunks = new HexGridChunk[chunkCountX * chunkCountZ];
		for (int z = 0, i = 0; z < chunkCountZ; z++) {
			for (int x = 0; x < chunkCountX; x++) {
				HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
				chunk.transform.SetParent(columns[x], false);
			}
		}
	}

	void CreateCells () {
		cells = new HexCell[cellCountZ * cellCountX];

		for (int z = 0, i = 0; z < cellCountZ; z++) {
			for (int x = 0; x < cellCountX; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	void CreateResources()
	{
		resLayer = new HexResource[cellCountZ * cellCountX];

		for (int z = 0, i = 0; z < cellCountZ; z++) {
			for (int x = 0; x < cellCountX; x++) {
				CreateResource(x, z, i++);
			}
		}
	}

	void ClearUnits () {
		for (int i = 0; i < units.Count; i++) {
			units[i].Die();
		}
		units.Clear();
	}

	void OnEnable () {
		if (!HexMetrics.noiseSource) {
			HexMetrics.noiseSource = noiseSource;
			HexMetrics.InitializeHashGrid(seed);
			HexUnit.unitPrefab = unitPrefab;
			HexMetrics.wrapSize = wrapping ? cellCountX : 0;
			ResetVisibility();
		}
	}

	public HexCell GetCell (Ray ray) {
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) {
			return GetCell(hit.point);
		}
		return null;
	}

	public HexCell GetCell (Vector3 position) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		return GetCell(coordinates);
	}

	public HexCell GetCell (HexCoordinates coordinates) {
		int z = coordinates.Z;
		if (z < 0 || z >= cellCountZ) {
			return null;
		}
		int x = coordinates.X + z / 2;
		if (x < 0 || x >= cellCountX) {
			return null;
		}
		return cells[x + z * cellCountX];
	}

	public HexCell GetCell (int xOffset, int zOffset) {
		return cells[xOffset + zOffset * cellCountX];
	}

	public HexCell GetCell (int cellIndex) {
		return cells[cellIndex];
	}

	public void ShowUI (bool visible) {
		for (int i = 0; i < chunks.Length; i++) {
			chunks[i].ShowUI(visible);
		}
	}

	void CreateCell (int x, int z, int i) {
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * HexMetrics.innerDiameter;
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.Index = i;
		cell.ColumnIndex = x / HexMetrics.chunkSizeX;
		cell.ShaderData = cellShaderData;

		if (wrapping) {
			cell.Explorable = z > 0 && z < cellCountZ - 1;
		}
		else {
			cell.Explorable =
				x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;
		}

		if (x > 0) {
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
			if (wrapping && x == cellCountX - 1) {
				cell.SetNeighbor(HexDirection.E, cells[i - x]);
			}
		}
		if (z > 0) {
			if ((z & 1) == 0) {
				cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
				if (x > 0) {
					cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
				}
				else if (wrapping) {
					cell.SetNeighbor(HexDirection.SW, cells[i - 1]);
				}
			}
			else {
				cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
				if (x < cellCountX - 1) {
					cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
				}
				else if (wrapping) {
					cell.SetNeighbor(
						HexDirection.SE, cells[i - cellCountX * 2 + 1]
					);
				}
			}
		}

		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		cell.uiRect = label.rectTransform;

		cell.Elevation = 0;

		AddCellToChunk(x, z, cell);

		cell.SetLabel(cell.GetLabelStr(showLabel));
	}

	void CreateResource(int x, int y, int i)
	{
		HexResource res = resLayer[i] = new HexResource();
		AddResToCell(i, res);
	}

	void AddCellToChunk (int x, int z, HexCell cell) {
		int chunkX = x / HexMetrics.chunkSizeX;
		int chunkZ = z / HexMetrics.chunkSizeZ;
		HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

		int localX = x - chunkX * HexMetrics.chunkSizeX;
		int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
		chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
	}

	void AddResToCell(int i, HexResource res)
	{
		cells[i].Res = res;
	}

	public void Save (BinaryWriter writer) {
		writer.Write(cellCountX);
		writer.Write(cellCountZ);
		writer.Write(wrapping);

		for (int i = 0; i < cells.Length; i++) {
			cells[i].Save(writer);
		}

		// header version 7, Unit数据现在该为其他地方保存了(参考RoomServer里的ActorManager.SaveBuffer())
//		writer.Write(units.Count);
//		for (int i = 0; i < units.Count; i++) {
//			units[i].Save(writer);
//		}

		// header version 6，保存资源数据
		int resCount = 0;
		for (int i = 0; i < cells.Length; i++)
		{
			if (resLayer[i].GetAmount(resLayer[i].ResType) > 0)
				resCount++;
		}
		writer.Write(resCount);
		for (int i = 0; i < cells.Length; i++) {
			if (resLayer[i].GetAmount(resLayer[i].ResType) > 0)
			{
				writer.Write(i);
				resLayer[i].Save(writer);
			}
		}
	}

	public void Load (BinaryReader reader, int header) {
		ClearPath();
		ClearUnits();
		int x = 20, z = 15;
		if (header >= 1) {
			x = reader.ReadInt32();
			z = reader.ReadInt32();
		}
		bool wrapping = header >= 5 ? reader.ReadBoolean() : false;
		wrapping = false;
		if (x != cellCountX || z != cellCountZ || this.wrapping != wrapping) {
			if (!CreateMap(x, z, wrapping))
			{
				Debug.LogError("HexGrid Load() Error - ReCreateMap Failed!");
				return;
			}
		}

		bool originalImmediateMode = cellShaderData.ImmediateMode;
		cellShaderData.ImmediateMode = true;

		HexCell.showLabel = showLabel;
		for (int i = 0; i < cells.Length; i++) {
			cells[i].Load(reader, header);
		}

		// Unit数据现在该为其他地方读取了(参考RoomServer里的ActorManager.LoadBuffer())
		if (header >= 2 && header <=6 ) {
			int unitCount = reader.ReadInt32();
			for (int i = 0; i < unitCount; i++) {
				HexUnit.Load(reader, this);
			}
		}
		
		// header version 6
		if (header >= 6)
		{// 读取资源层resLayer的数据
			// Sand-0; Grass-1; Mud-2; Stone-3; Snow-4
			int[] terrainTypeCount = { 0,0,0,0,0};
			for (int i = 0; i < cells.Length; i++)
			{
				terrainTypeCount[cells[i].TerrainTypeIndex]++;
			}

			Debug.Log($"HexGrid Load - Terrain Type Count - Sand:{terrainTypeCount[0]} - Grass:{terrainTypeCount[1]} - Mud:{terrainTypeCount[2]} - Stone:{terrainTypeCount[3]} - Snow:{terrainTypeCount[4]}");

			List<int> resList = new List<int>();
			int resCount = reader.ReadInt32();
			Debug.Log($"HexGrid Load - Res Count Total:{resCount}");
			int[] resTypeCount = {0, 0, 0};
			for(int i = 0; i < resCount; i++)
			{
				int index = reader.ReadInt32();
				var res = resLayer[index];
				res.Load(reader, header);
				resList.Add(index);
				cells[index].UpdateFeatureLevelFromRes();

				resTypeCount[(int) resLayer[index].ResType]++;
			}

			string format = string.Format("HexGrid Load - Res Count Wood:{0}({1:P})", resTypeCount[0],
				resTypeCount[0] / (float)terrainTypeCount[1]);
			format += string.Format("Res Count Food:{0}({1:P})", resTypeCount[1],
				resTypeCount[1] / (float)terrainTypeCount[2]);
			format += string.Format("Res Count Iron:{0}({1:P})", resTypeCount[2],
				resTypeCount[2] / (float)terrainTypeCount[3]);
			Debug.Log(format);
		}

		// 把所有数据都变成模型
		for (int i = 0; i < chunks.Length; i++) {
			chunks[i].Refresh();
		}
		
		cellShaderData.ImmediateMode = originalImmediateMode;
	}

	public List<HexCell> GetPath () {
		if (!currentPathExists) {
			return null;
		}
		List<HexCell> path = ListPool<HexCell>.Get();
		for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom) {
			path.Add(c);
		}
		path.Add(currentPathFrom);
		path.Reverse();
		return path;
	}

	public void ClearPath () {
		if (currentPathExists) {
			HexCell current = currentPathTo;
			while (current != currentPathFrom) {
				current.SetLabel(current.GetLabelStr(showLabel));
				current.DisableHighlight();
				current = current.PathFrom;
			}
			current.DisableHighlight();
			currentPathExists = false;
		}
		else if (currentPathFrom) {
			currentPathFrom.DisableHighlight();
			currentPathTo.DisableHighlight();
		}
		currentPathFrom = currentPathTo = null;
	}

	// 老算法建议留着, 可以了解原来的思路. Nov.21.2019. Liu Gang.
//	void ShowPath (int speed) {
//		if (currentPathExists) {
//			HexCell current = currentPathTo;
//			while (current != currentPathFrom) {
//				int turn = (current.Distance - 1) / speed;
//				
//				string label = $"{turn}";
//				if(current.Unit)
//					label = $"{turn}-<color=#FF0000FF>T</color>";
//				current.SetLabel(label);
//				current.EnableHighlight(Color.white);
//				current = current.PathFrom;
//			}
//		}
//		currentPathFrom.EnableHighlight(Color.blue);
//		currentPathTo.EnableHighlight(Color.red);
//	}
	/// <summary>
	/// 新算法比较费, 以后有空再优化吧. Nov.21.2019. Liu Gang.
	/// </summary>
	/// <param name="speed"></param>
	void ShowPath (int speed)
	{
		List<HexCell> path = GetPath();
		if (path == null) return;
		for (int i = 0; i < path.Count; ++i)
		{
			HexCell current = path[i];
			string label = $"{i}";
			if(current.Unit)
				label = $"{i}-<color=#FF0000FF>T</color>";
			current.SetLabel(label);
			current.EnableHighlight(Color.yellow);
			current = current.PathFrom;
		}
		currentPathFrom.EnableHighlight(Color.blue);
		currentPathTo.EnableHighlight(Color.red);
	}

	List<HexCell> _listPathSaved = new List<HexCell>();
	public void ShowPath (List<HexCell> path)
	{
		// 先擦掉之前画的
		if (_listPathSaved != null)
		{
			foreach (var current in _listPathSaved)
			{
				current.SetLabel(current.GetLabelStr(showLabel));
				current.DisableHighlight();
			}
		}

		_listPathSaved.Clear();
		if (path == null)
			return;
		
		// 再画新的
		for (int i = 0; i < path.Count; ++i)
		{
			HexCell current = path[i];
			string label = $"{i}";
			if(current.Unit)
				label = $"{i}-<color=#FF0000FF>T</color>";
			current.SetLabel(label);
			current.EnableHighlight(Color.yellow);
		}

		if (path.Count >= 2)
		{
			path[0].EnableHighlight(Color.blue);
			path[path.Count-1].EnableHighlight(Color.red);
		}
		
		foreach (var current in path)
		{
			_listPathSaved.Add(current);
		}
	}
	
	public void FindPath (HexCell fromCell, HexCell toCell, HexUnit unit) {
		ClearPath();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = Search(fromCell, toCell, unit);
		ShowPath(unit.Speed);
	}

	bool Search (HexCell fromCell, HexCell toCell, HexUnit unit) {
		int speed = unit.Speed;
		searchFrontierPhase += 2;
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);
		while (searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;

			if (current == toCell) {
				return true;
			}

			//int currentTurn = (current.Distance - 1) / speed;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (
					neighbor == null 
					|| neighbor.SearchPhase > searchFrontierPhase
				) {
					continue;
				}
				if (!unit.IsValidDestination(neighbor)) {
					continue;
				}
				int moveCost = unit.GetMoveCost(current, neighbor, d);
				if (moveCost < 0) {
					continue;
				}

				int distance = current.Distance + moveCost;
//				int turn = (distance - 1) / speed;
//				if (turn > currentTurn) {
//					distance = turn * speed + moveCost;
//				}

				if (neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					neighbor.SearchHeuristic =
						neighbor.coordinates.DistanceTo(toCell.coordinates);
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance) {
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return false;
	}

	public void IncreaseVisibility (HexCell fromCell, int range) {
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++) {
			cells[i].IncreaseVisibility();
		}
		ListPool<HexCell>.Add(cells);
	}

	public void DecreaseVisibility (HexCell fromCell, int range) {
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++) {
			cells[i].DecreaseVisibility();
		}
		ListPool<HexCell>.Add(cells);
	}

	public void ResetVisibility () {
		for (int i = 0; i < cells.Length; i++) {
			cells[i].ResetVisibility();
		}
		for (int i = 0; i < units.Count; i++) {
			HexUnit unit = units[i];
			IncreaseVisibility(unit.Location, unit.VisionRange);
		}
	}

	List<HexCell> GetVisibleCells (HexCell fromCell, int range) {
		List<HexCell> visibleCells = ListPool<HexCell>.Get();

		searchFrontierPhase += 2;
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		range += fromCell.ViewElevation;
		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);
		HexCoordinates fromCoordinates = fromCell.coordinates;
		while (searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;
			visibleCells.Add(current);

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (
					neighbor == null ||
					neighbor.SearchPhase > searchFrontierPhase ||
					!neighbor.Explorable
				) {
					continue;
				}

				int distance = current.Distance + 1;
				if (distance + neighbor.ViewElevation > range ||
					distance > fromCoordinates.DistanceTo(neighbor.coordinates)
				) {
					continue;
				}

				if (neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.SearchHeuristic = 0;
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance) {
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return visibleCells;
	}

	public void CenterMap (float xPosition) {
		int centerColumnIndex = (int)
			(xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));
		
		if (centerColumnIndex == currentCenterColumnIndex) {
			return;
		}
		currentCenterColumnIndex = centerColumnIndex;

		int minColumnIndex = centerColumnIndex - chunkCountX / 2;
		int maxColumnIndex = centerColumnIndex + chunkCountX / 2;

		Vector3 position;
		position.y = position.z = 0f;
		for (int i = 0; i < columns.Length; i++) {
			if (i < minColumnIndex) {
				position.x = chunkCountX *
					(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
			}
			else if (i > maxColumnIndex) {
				position.x = chunkCountX *
					-(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
			}
			else {
				position.x = 0f;
			}
			columns[i].localPosition = position;
		}
	}

	public void OnShowLabels(int showLabel)
	{
		string msg = null;
		for (int i = 0; i < cells.Length; ++i)
		{
			cells[i].SetLabel(cells[i].GetLabelStr(showLabel));
		}
	}

	public void ShowLabel(int cellIndex, int showLabel)
	{
		cells[cellIndex].SetLabel(cells[cellIndex].GetLabelStr(showLabel));
	}

	/// <summary>
	/// 得到指定地块的地表速度，地表类型的数据被保存在cellShaderData的alpha通道
	/// Sand-0; Grass-1; Mud-2; Stone-3; Snow-4
	/// </summary>
	/// <param name="cell">指定的地块</param>
	/// <returns>在该地块上行走的速度（率），以1为标准单位</returns>
	public float  GetCellSpeed(HexCell cell)
	{
		float[] speedRate = new float[] { 0.75f, 1f, 1.25f, 1.5f, 0.5f};
		int cellType = cellShaderData.GetCellTypeIndex(cell);
		if (cellType < 0 || cellType >= 5)
		{
			Debug.Log($"HexGrid Error : Cell Type is out of range：{cellType} - valid type should between: {0}~{4}");			
			return 1f;
		}

		float rateRiver = 1f; // 有河流流过的话，速度再降低一半
		if (cell.HasRiver)
			rateRiver = 0.5f;

		float rateRoad = 1f; // 有道路的话，速度提升一倍
		if (cell.HasRoads)
			rateRoad = 2.0f;

		return speedRate[cellType] * rateRiver * rateRoad;
	}
	
}