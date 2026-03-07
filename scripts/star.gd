extends Area2D

@export var star_amount: int = 1

func _ready() -> void:
	$Sprite2D.play("default")

func _on_body_entered(body: Node2D) -> void:
	if body.is_in_group("player"):
		var stage := get_tree().get_first_node_in_group("stage")
		if stage and stage.has_method("add_run_star"):
			stage.add_run_star(star_amount)
		queue_free()
