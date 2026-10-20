## Controle exclusivo da preview; nunca e anexado ao asset.
extends Node

var camera: Camera3D
var target := Vector3.ZERO
var yaw := 0.0
var pitch := 0.5
var initial_yaw := 0.0
var initial_pitch := 0.5
var dragging := false

func configure(p_camera: Camera3D, p_target: Vector3) -> void:
	camera = p_camera
	target = p_target
	var offset := camera.position-target
	yaw = atan2(offset.x,offset.z)
	pitch = asin(offset.normalized().y)
	initial_yaw = yaw
	initial_pitch = pitch

func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT or event.button_index == MOUSE_BUTTON_RIGHT:
			dragging = event.pressed
		if event.pressed and event.button_index == MOUSE_BUTTON_WHEEL_UP:
			camera.size = maxf(4,camera.size/1.12)
		elif event.pressed and event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			camera.size = minf(65,camera.size*1.12)
	elif event is InputEventMouseMotion and dragging:
		yaw -= event.relative.x*0.008
		pitch = clampf(pitch+event.relative.y*0.006,0.10,1.35)
		_update_camera()
	elif event is InputEventKey and event.pressed:
		if event.keycode == KEY_ESCAPE:
			get_tree().quit()
		elif event.keycode == KEY_R:
			yaw = initial_yaw
			pitch = initial_pitch
			camera.size = 18.5
			_update_camera()

func _update_camera() -> void:
	camera.position = target+Vector3(sin(yaw)*cos(pitch),sin(pitch),cos(yaw)*cos(pitch))*65.0
	camera.look_at(target)
