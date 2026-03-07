extends Area2D

@export var checkpoint_id: int = 0
var activated: bool = false

func _on_body_entered(body: Node2D) -> void:
	if activated:
		return

	if body.is_in_group("player"):
		var stage := get_tree().get_first_node_in_group("stage")
		if stage and stage.has_method("set_checkpoint"):
			stage.set_checkpoint(global_position)
			activated = true
			print("Checkpoint activated: ", checkpoint_id)
