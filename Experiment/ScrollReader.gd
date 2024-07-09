extends Node3D

var find_index := 0

@export_multiline var textContent: String

@onready var label_3d: Label3D = $"Label3D"
@onready var camera_3d: Camera3D = $"Camera3D"
@onready var lines_container: Node3D = $LinesContainer

# bleh, after a while, I get the guitar hero syndrome
# and I feel slight dizziness while reading
# basically, what I want something that strains
# my eyes less while reading, which is caused
# by my too much back-and-forth eye movement
# maybe I could try something else,
# not scrolling but show a slice of text one by one

func _ready() -> void:
	var i := 0
	var font_size := 32
	for line in textContent.split("\n", false):
		var label = Label3D.new()
		label.text = line
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
		label.vertical_alignment = VERTICAL_ALIGNMENT_TOP
		label.font_size = font_size
		label.modulate = Color.from_string("090f5f", Color.WEB_GRAY)
		label.global_position.y -= font_size * i * 0.01
		lines_container.add_child(label)
		i += 1

	#await get_tree().create_timer(0.20).timeout
	
	for j in range(lines_container.get_child_count()):
		var line = lines_container.get_child(j) as Label3D
		if not line: continue

		await get_tree().process_frame
		#line.force_update_transform()
		var aabb := line.get_aabb()
		printt(camera_3d.position, line.position)
		var pos = line.global_position
		var tween = create_tween()
		
		tween.set_trans(Tween.TRANS_ELASTIC)
		tween.tween_property(camera_3d, "global_position", Vector3(pos.x, pos.y, camera_3d.global_position.z), 1)
		await tween.finished
		
		for other in lines_container.get_children():
			other.modulate = Color.from_string("050f2f", Color.WEB_GRAY)
				
		line.modulate = Color.WHITE
		await get_tree().create_timer(0.5).timeout	
		#print(">", aabb.get_longest_axis(), aabb.size.abs())
		
		while true:
			await get_tree().process_frame
			camera_3d.position.x += 1 * get_process_delta_time()
			#printt(camera_3d.position.x, line.position.x, aabb.size.x)
			if camera_3d.position.x > line.position.x + aabb.size.x:
				break
				
		await get_tree().create_timer(1).timeout	
		
	


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	#camera_3d.position.x += 0.9 * delta
	pass

func next_line():
	var i = textContent.find("\n", find_index)
	var line = textContent.substr(find_index, i)
	label_3d.text = line
	
