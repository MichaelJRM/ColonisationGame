extends Node

@onready var _viewport : Viewport = get_viewport()
@onready var _camera : Camera3D = _viewport.get_camera_3d()
var mouse_position_in_world : Vector3 = Vector3.ZERO
var is_debug : bool = true


func _process(_delta: float) -> void:
	_update_mouse_position_in_world()


func _update_mouse_position_in_world() -> void:
	var new_pos = WorldUtil.get_mouse_position_in_world(
		_camera, 
		_viewport.get_mouse_position()
	)
	if new_pos:
		mouse_position_in_world = new_pos
