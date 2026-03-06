extends CharacterBody2D

var SPEED:float = 650.0
var JUMP_VELOCITY = -400.0
var BASE_ACCELERATION = 300.0
var FRICTION:float = 500.0 
static var last_dir:float = 0.0
@onready var animated_sprite_2d: AnimatedSprite2D = $AnimatedSprite2D

func _physics_process(delta: float) -> void:
	if Input.is_action_just_pressed("ui_accept") and is_on_floor():
		velocity.y = JUMP_VELOCITY
		animated_sprite_2d.play("Mo_Jumping_Startup")
	
	if not is_on_floor():
		velocity.y += get_gravity().y * delta
	var direction := Input.get_axis("walk_left", "walk_right")
	
	if direction:
		if last_dir == direction:
			velocity.x = move_toward(velocity.x, SPEED * direction,BASE_ACCELERATION * delta)
		elif last_dir != direction:
			velocity.x = move_toward(velocity.x, 0, FRICTION )
		if direction == 1:
			animated_sprite_2d.flip_h = false
		elif direction == -1:
			animated_sprite_2d.flip_h = true
	else:
			velocity.x = move_toward(velocity.x, 0, FRICTION * delta)
	last_dir = direction
	move_and_slide()
	
	if not is_on_floor():
		if velocity.y < 0:
			animated_sprite_2d.play("Mo_Jumping")
		else:
			animated_sprite_2d.play("Mo_Falling")
	else:
		if abs(velocity.x) > 1:
			animated_sprite_2d.play("Mo_Running")
			
		else:
			animated_sprite_2d.play("Mo_Idle")
