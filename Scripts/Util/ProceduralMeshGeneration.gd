class_name ProceduralMeshGeneration extends Resource

var is_closed := false
var normal_angle := 30.0
var polygon := PackedVector2Array():
	get:
		return polygon
	set(value):
		polygon = smooth_polygon(value)

var _mesh := ArrayMesh.new()
var _verts := PackedVector3Array()
var _indices := PackedInt32Array()
var _normals := PackedVector3Array()
var _uvs := PackedVector2Array()


func update(curve: Curve3D) -> ArrayMesh:
	reset()
	if curve.point_count == 0:
		return _mesh
	var rings := curve.get_baked_points()
	var up_vectors := curve.get_baked_up_vectors()
	calculate_vertices(rings, up_vectors)
	calculate_normals()
	calculate_uvs(rings.size())
	return generate_mesh()


func reset() -> void:
	_mesh = ArrayMesh.new()
	_verts.clear()
	_indices.clear()
	_normals.clear()
	_uvs.clear()


func smooth_polygon(_polygon: PackedVector2Array) -> PackedVector2Array:
	var points := PackedVector2Array()
	for i in range(_polygon.size()):
		var angle := absf(WorldUtil.calculate_angle_between_vec2_points(
			_polygon[i - 1 if i > 0 else _polygon.size() - 1],
			_polygon[i],
			_polygon[i + 1 if i < _polygon.size() - 1 else 0],
		))
		points.append(_polygon[i])
		if angle > normal_angle:
			points.append(_polygon[i])
	return points


func calculate_vertices(rings: PackedVector3Array, up_vectors: PackedVector3Array) -> void:
	var vert_index := 0
	for i in range(rings.size()):
		var forward := rings[i].direction_to(rings[i + 1]) if i < rings.size() - 1 else Vector3.ZERO
		var up := up_vectors[i]
		var right := forward.cross(up).normalized()

		for point_i in range(polygon.size()):
			var vert := rings[i] + right * polygon[point_i].x + up * polygon[point_i].y
			_verts.append(vert)
			
		var vert_count := polygon.size()
		if i < rings.size() - 1:
			for j in vert_count:
				_indices.append(vert_index + j)
				_indices.append(vert_index + j + vert_count)
				_indices.append(vert_index + (j + 1 if j + 1 < vert_count else 0))
				
				_indices.append(vert_index + (j + 1 if j + 1 < vert_count else 0))
				_indices.append(vert_index + j + vert_count)
				_indices.append(vert_index + (j + 1 + vert_count if j + 1 < vert_count else vert_count))
		vert_index += vert_count


func calculate_uvs(ring_count: int) -> void:
	var unfolded_polygon_length : float = 0.0
	var distances := PackedFloat32Array()
	distances.resize(polygon.size() + 1)
	distances.append(0.0)
	for point_i in range(polygon.size()):
		if point_i < polygon.size() - 1:
			distances.append(polygon[point_i].distance_to(polygon[point_i + 1]))
			unfolded_polygon_length += distances[distances.size() - 1]
		else:
			distances.append(polygon[point_i].distance_to(polygon[0]))
			unfolded_polygon_length += distances[distances.size() - 1]
	var uv_v_scale := 1 / unfolded_polygon_length
	for i in range(ring_count):
		var current_dis := 0.0
		for j in range(polygon.size()):
			current_dis += distances[j]
			if i % 2 == 0:
				_uvs.append(Vector2(0.0, current_dis * uv_v_scale))
			else:
				_uvs.append(Vector2(1.0, current_dis * uv_v_scale))


func calculate_normals() -> void:
	_normals.resize(_verts.size())
	var per_vertice_normals : Array[PackedVector3Array] = []
	per_vertice_normals.resize(_verts.size())
	#warning-ignore:integer_division
	var triangle_count : int = _indices.size() / 3
	
	for i in range(triangle_count):
		var normal_triangle_index := i * 3
		var vertex_index_a := _indices[normal_triangle_index]
		var vertex_index_b := _indices[normal_triangle_index + 1]
		var vertex_index_c := _indices[normal_triangle_index + 2]

		var triangle_normal := face_normal_from_indices(vertex_index_a, vertex_index_b, vertex_index_c)
		per_vertice_normals[vertex_index_a].append(triangle_normal)
		per_vertice_normals[vertex_index_b].append(triangle_normal)
		per_vertice_normals[vertex_index_c].append(triangle_normal)

	for i in range(_verts.size()):
		for j in range(per_vertice_normals[i].size()):
			_normals[i] += per_vertice_normals[i][j]
			
		_normals[i] = _normals[i].normalized()


func face_normal_from_indices(indexA: int, indexB: int, indexC: int) -> Vector3:
	var pointA := _verts[indexA];
	var pointB := _verts[indexB];
	var pointC := _verts[indexC];
	var sideAB := pointB - pointA;
	var sideAC := pointC - pointA;
	return sideAC.cross(sideAB).normalized()


func generate_mesh() -> ArrayMesh:
	var mesh_arr := []
	mesh_arr.resize(Mesh.ARRAY_MAX)
	mesh_arr[Mesh.ARRAY_VERTEX] = _verts
	mesh_arr[Mesh.ARRAY_INDEX] = _indices
	mesh_arr[Mesh.ARRAY_NORMAL] = _normals
	mesh_arr[Mesh.ARRAY_TEX_UV] = _uvs
	_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, mesh_arr)
	return _mesh


#func _optimize_curve_baking_points(baked_points: PackedVector3Array) -> PackedVector3Array:
#	if baked_points.size() < 3:
#		return baked_points
#
#	var optimized := PackedVector3Array()
#	optimized.append(baked_points[0])
#	for i in range(1, baked_points.size() - 1):
#		var angle := WorldUtil.calculate_angle_between_points(
#			baked_points[i - 1],
#			baked_points[i],
#			baked_points[i + 1]
#		)
#		if !is_equal_approx(angle, 180.0):
#			optimized.append_array([baked_points[i - 1], baked_points[i], baked_points[i + 1]])
#
#
#
		
