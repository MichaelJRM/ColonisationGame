class_name WorldUtil extends Resource


static func get_mouse_position_in_world(camera: Camera3D, mouse_position: Vector2, ray_length: int = 1000000):
	var origin := camera.project_ray_origin(mouse_position)
	var target := origin + camera.project_ray_normal(mouse_position) * ray_length
	return Plane(Vector3.UP, 0.0).intersects_ray(origin, target)


static func calculate_angle_between_points(a: Vector3, b: Vector3, c: Vector3) -> float:
	var ab := b - a
	var cb := b - c
	var dot_product := ab.dot(cb)
	var magnitude_product := ab.length() * cb.length()
	var angle_in_radians := acos(dot_product / magnitude_product)

	# Calculate the cross product of the two vectors
	var cross_product := ab.cross(cb)

	# Check the sign of the angle by looking at the sign of the cross product
	if cross_product.y < 0:
		angle_in_radians *= -1

	return rad_to_deg(angle_in_radians)


static func calculate_angle_between_vec2_points(a: Vector2, b: Vector2, c: Vector2) -> float:
	var ba = (a - b).normalized()
	var bc = (c - b).normalized()

	var dot_product = ba.dot(bc)
	var cross_product = ba.cross(bc)

	var angle = atan2(cross_product, dot_product)
	return rad_to_deg(angle)


## Returns 1 if the b position is to the right of a position and -1 if b position is to the left of a position.
static func position_in_relation_to_other(a_position: Vector3, a_forward_vector: Vector3, b_position: Vector3) -> int:
	var direction := b_position - a_position
	var cross_product = a_forward_vector.cross(direction)
	return 1 if cross_product.y > 0 else -1


static func get_angle_to_target(origin_transform: Transform3D, target_pos: Vector3) -> float:
	var direction := (target_pos - origin_transform.origin).normalized()
	var dot_product := (-origin_transform.basis.z).dot(direction)
	return rad_to_deg(acos(dot_product)) * signf(dot_product)


#static func get_closest_node(nodes: Array, position: Vector3) -> Node3D:
#	if nodes.is_empty():
#		return null
#
#	var closest_node : Node3D = nodes.front()
#	var closest_node_distance : float = closest_node.global_position.distance_squared_to(position)
#	for node in nodes:
#		var distance : float = node.global_position.distance_squared_to(position)
#		if distance < closest_node_distance:
#			closest_node = node
#			closest_node_distance = distance
#	return closest_node


static func get_parallel_position(target_transform: Transform3D, source_position: Vector3) -> Vector3:
	var direction := source_position - target_transform.origin
	var parallel_vector := direction.project(-target_transform.basis.z)
	var parallel_position := target_transform.origin + parallel_vector
	return parallel_position


static func is_looking_at(transform: Transform3D, target: Vector3, threshold: float) -> bool:
	var forward := -transform.basis.z.normalized()
	var to_target := target - transform.origin
	var angle := to_target.angle_to(forward)
	return rad_to_deg(angle) <= threshold
