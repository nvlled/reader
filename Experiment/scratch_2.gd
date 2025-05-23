extends Node3D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	var label := Label3D.new()
	var label2 := Label3D.new()
	add_child(label)
	add_child(label2)
	label.text = "M"
	label2.text = "M line two"
	label.autowrap_mode = TextServer.AUTOWRAP_WORD
	#label.position.z = 0
	#label2.position.z = 0
	
	
	label.font_size = 15
	label.outline_size = 5
	label2.font_size = label.font_size
	label2.outline_size = 5
	
	
	#label.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	#label.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	#label2.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	#label2.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	
	await get_tree().process_frame
	var w = label.get_aabb().size.x
	var h = label.get_aabb().size.y
	
	label.position.y = 0.6
	label2.position.y = (label.position.y - h/2)
	
	
	#label.width = get_window().size.x
	#label.scale *= 0.5
	

	printt(w, h, h/100)

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
