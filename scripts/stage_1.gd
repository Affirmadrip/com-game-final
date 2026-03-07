extends Node2D

var run_gold: int = 0
var run_star: int = 0
var current_checkpoint: Vector2

@onready var start_spawn: Marker2D = $StartSpawn

func _ready() -> void:
	current_checkpoint = start_spawn.global_position

func add_run_gold(amount: int) -> void:
	run_gold += amount
	print("Run Gold: ", run_gold)

func add_run_star(amount: int) -> void:
	run_star += amount
	print("Run Star: ", run_star)

func set_checkpoint(pos: Vector2) -> void:
	current_checkpoint = pos
	print("Current checkpoint set to: ", current_checkpoint)

func get_respawn_position() -> Vector2:
	return current_checkpoint
