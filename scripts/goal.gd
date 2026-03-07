extends Area2D

var finished: bool = false

func _on_body_entered(body: Node2D) -> void:
	if finished:
		return
	
	if body.is_in_group("player"):
		finished = true
		print("STAGE COMPLETE")
		
		body.set_physics_process(false)
		body.set_process(false)
