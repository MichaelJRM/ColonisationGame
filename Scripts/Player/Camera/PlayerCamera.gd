extends Node3D

@onready var _viewport : Window = get_viewport()
@onready var _rotation: Marker3D = $Rotation
@onready var _camera_rig : Node3D = $Rotation/CameraRig
@onready var _camera : Camera3D = $Rotation/CameraRig/Camera3D
@onready var _camera_forward_collision_raycast : RayCast3D = $Rotation/CameraRig/Camera3D/ForwardCollisionRayCast3D
@onready var _camera_zoom_raycast : RayCast3D = $Rotation/CameraRig/Camera3D/ZoomRayCast3D
@onready var _camera_bottom_collision_raycast : RayCast3D = $Rotation/CameraRig/BottomCollisionRayCast3D
var _camera_zoom_tween : Tween

const k_screen_edge_camera_movement_trigger_threshold := 10
const k_camera_move_speed := 50
const k_camera_y_rotation_speed_multiplier := 0.008
const k_camera_x_rotation_speed_multiplier := 0.008
const k_max_camera_y_rotation_speed := 5.0
const k_max_camera_x_rotation_speed := 13.0
const k_camera_move_speed_pow := 1.2
var k_camera_zoom_step := 8
var k_camera_zoom_duration := 0.8
var k_camera_zoom_divider := 50

var is_rotation_active := false
var is_edge_camera_movement_trigger_active := false
var current_zoom := 0.0
var transition_type := Tween.TRANS_EXPO
var ease_type := Tween.EASE_OUT
var scheduled_physics_process_pdate_camera_zoom_callback : Array[Callable]
var scheduled_physics_process_pdate_camera_rotation_callback : Array[Callable]


func _physics_process(delta: float) -> void:
	_update_camera_position(delta)
	if !scheduled_physics_process_pdate_camera_rotation_callback.is_empty():
		for callback in scheduled_physics_process_pdate_camera_rotation_callback:
			callback.call()
		scheduled_physics_process_pdate_camera_rotation_callback.clear()
	if !scheduled_physics_process_pdate_camera_zoom_callback.is_empty():
		for callback in scheduled_physics_process_pdate_camera_zoom_callback:
			callback.call()
		scheduled_physics_process_pdate_camera_zoom_callback.clear()


func _update_camera_position(delta: float) -> void:
	var move_speed := pow(k_camera_move_speed * delta * current_zoom, k_camera_move_speed_pow)
	var viewport_size : Vector2i = _viewport.size
	var mouse_pos := _viewport.get_mouse_position()
	var direction := Vector2();
	
	if (
		Input.is_action_pressed("world_camera_move_left") 
		or (
			is_edge_camera_movement_trigger_active 
			and mouse_pos.x < k_screen_edge_camera_movement_trigger_threshold
		)
	):
		direction += Vector2.LEFT
	elif (
		Input.is_action_pressed("world_camera_move_right") 
		or (
			is_edge_camera_movement_trigger_active 
			and mouse_pos.x > viewport_size.x - k_screen_edge_camera_movement_trigger_threshold
		)
	):
		direction += Vector2.RIGHT
	
	if (
		Input.is_action_pressed("world_camera_move_forward") 
		or (
			is_edge_camera_movement_trigger_active 
			and mouse_pos.y < k_screen_edge_camera_movement_trigger_threshold
		)
	):
		direction += Vector2.UP
	elif (
		Input.is_action_pressed("world_camera_move_backward") 
		or (
			is_edge_camera_movement_trigger_active 
			and mouse_pos.y > viewport_size.y - k_screen_edge_camera_movement_trigger_threshold
		)
	):
		direction += Vector2.DOWN
	direction *= move_speed
	global_transform = global_transform.translated_local(Vector3(direction.x, 0.0, direction.y))
	

func _update_camera_zoom(event: InputEvent) -> void:
	_update_current_zoom()
	var new_camera_pos := Vector3.ZERO
	
	if event.is_action("world_camera_zoom_in"):
		_camera_forward_collision_raycast.force_raycast_update()
		if _camera_forward_collision_raycast.is_colliding():
			return
		new_camera_pos = _camera_rig.position + -_camera.transform.basis.z * k_camera_zoom_step * current_zoom
		
	elif event.is_action("world_camera_zoom_out"):
		new_camera_pos = _camera_rig.position + -_camera.transform.basis.z * -k_camera_zoom_step * current_zoom
	
	if new_camera_pos != Vector3.ZERO:
		if _camera_zoom_tween != null:
			_camera_zoom_tween.kill()
		_camera_zoom_tween = create_tween()
		_camera_zoom_tween.tween_property(
			_camera_rig, 
			"position", 
			new_camera_pos, 
			k_camera_zoom_duration
		).set_trans(transition_type).set_ease(ease_type)


func _update_current_zoom() -> void:
	_camera_zoom_raycast.force_raycast_update()
	var collision_point = _camera_zoom_raycast.get_collision_point()
	if collision_point != null:
		current_zoom = _camera_rig.global_position.distance_to(collision_point) / k_camera_zoom_divider


func _update_camera_rotation(event: InputEvent) -> void:
	if event.is_action_pressed("world_camera_rotation"):
		is_rotation_active = true
	elif event.is_action_released("world_camera_rotation"):
		is_rotation_active = false
	if is_rotation_active && event is InputEventMouseMotion:
		var relative_motion = (event as InputEventMouseMotion).relative
		var y_rotation = -((minf(abs(relative_motion.x), k_max_camera_x_rotation_speed)) * signf(relative_motion.x)) * k_camera_y_rotation_speed_multiplier
		var x_rotation = -((minf(abs(relative_motion.y), k_max_camera_y_rotation_speed)) * signf(relative_motion.y)) * k_camera_x_rotation_speed_multiplier
		var lastTransform = transform
		var lastRotationTransform = _rotation.transform
		global_rotate(Vector3.UP, y_rotation)
		_rotation.rotate_object_local(_rotation.basis.x.normalized(), x_rotation if _rotation.rotation.x + x_rotation >= 0 else 0.0)
		_camera_bottom_collision_raycast.force_raycast_update()
		if  _camera_bottom_collision_raycast.is_colliding():
			transform = lastTransform
			_rotation.transform = lastRotationTransform


func _unhandled_input(event: InputEvent) -> void:
	scheduled_physics_process_pdate_camera_rotation_callback.append(func(): _update_camera_rotation(event))
	scheduled_physics_process_pdate_camera_zoom_callback.append(func(): _update_camera_zoom(event))
