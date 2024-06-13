extends Node3D

@export var text_batch_size := 35
@export_multiline var textContent: String

var editing := false
var index := 0
var text_batches: Array[String] = []
var page_size = 300
var save_filename = "user://data.txt"
var initialized := false
var is_ready := false

@onready var lines_container: Node3D = $LinesContainer
@onready var text_prev: Label3D = $TextPrev
@onready var text_current: Label3D = $TextCurrent
@onready var text_next: Label3D = $TextNext
@onready var input_text_container: MarginContainer = $InputTextContainer
@onready var margin_container: MarginContainer = $MarginContainer
@onready var text_edit: TextEdit = %TextEdit
@onready var text_whole: Label3D = $TextWhole
@onready var auto_paste_checkbox: CheckBox = %AutoPasteCheckbox


func _ready() -> void:
	var file = FileAccess.open(save_filename, FileAccess.READ)
	var contents = file.get_as_text()
	#printt("save file", contents)
	if contents:
		set_text_content(contents)
	else:
		set_text_content("")
		
	is_ready = true
	
	#for entry in DisplayServer.tts_get_voices_for_language("en"):
		#print(entry)
		#DisplayServer.tts_speak("shika noko noko no kosh tan tan", entry)
		#while DisplayServer.tts_is_speaking():
			#await get_tree().process_frame


func _input(event: InputEvent) -> void:
	if editing:
		if event.is_action_pressed("ui_cancel"):
			_on_cancel_button_pressed()
		return
	
	var last_index := index
	if event.is_action_pressed("ui_home"):
		index = 0
	if event.is_action_pressed("ui_end"):
		index = text_batches.size()-1
		
	if event.is_action_pressed("ui_cancel"):
		_on_show_input_button_pressed()
	
	if event.is_action_pressed("ui_accept") or event.is_action_pressed("ui_right"):
		if index < text_batches.size()-1:
			index += 1
			
	if event.is_action_pressed("ui_text_backspace") or  event.is_action_pressed("ui_left"):
		if index > 0:
			index -= 1
	
	if last_index != index:		
		update_batch()
	
	var keyev := event as InputEventKey
	if keyev and keyev.ctrl_pressed and keyev.keycode == KEY_V and keyev.is_pressed():
		print_debug("paste")
		var contents = DisplayServer.clipboard_get()
		set_text_content(contents)
			
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass

func _notification(what: int) -> void:
	if !is_ready: return
	if what == NOTIFICATION_WM_WINDOW_FOCUS_IN and auto_paste_checkbox.button_pressed:
		if !initialized:
			initialized = true
		else:
			var contents = DisplayServer.clipboard_get()
			if contents and contents != textContent:
				set_text_content(contents)


func set_text_content(text: String):
	textContent = text
	index = 0
	text_edit.text = text
	
	text_batches.clear()
	
	for line in textContent.split("\n", false):
		if line.length() == 1 and text_batches.size() > 0:
			text_batches[text_batches.size()-1] += "⏎"
			continue
		line = line.strip_edges() + "⏎"
		var i := 0
		while i < line.length():
			var j: int = line.find(" ", i+text_batch_size)
			if j < 0: j = line.length()
			#printt(i, j, textContent.substr(i, j-i))
			text_batches.append(line.substr(i, j-i+1))
			i = j+1
			
	save()
	#var i := 0
	#while i < textContent.length():
		#var j: int = textContent.find(" ", i+text_batch_size)
		#if j < 0: j = textContent.length()
		##printt(i, j, textContent.substr(i, j-i))
		#text_batches.append(textContent.substr(i, j-i+1).replacen("\n", "⏎"))
		#i = j+1
		
	update_batch()

func get_batch_at(index: int) -> String:
	if index < 0 or index >= text_batches.size(): return ""
	return text_batches[index]

func update_batch():
	text_prev.text = get_batch_at(index-1)
	var i = text_prev.text.length()-text_batch_size/2

	if text_prev.text.length() > 0:
		while text_prev.text[i] != ' ' and i > 0:
			i -= 1
		if text_prev.text.length()-i > (text_batch_size*3)/4:
			i = text_prev.text.length() - text_batch_size/2
		text_prev.text = "..." + text_prev.text.substr(i)
	
	text_current.text = get_batch_at(index-0)
	text_next.text = get_batch_at(index+1)

	await get_tree().process_frame
	await get_tree().physics_frame
	var aabb_prev = text_prev.get_aabb()
	var aabb_current = text_current.get_aabb()
	text_prev.position.y = aabb_prev.size.y/2 + aabb_current.size.y/2
	
	#DisplayServer.tts_stop()
	#DisplayServer.tts_speak(text_current.text, "English (Caribbean)+Antonio", 20, 0.7, 1)
	
	var text:String = ""
	for t in text_batches.slice(0, index+1):
		text += t + " "
	text_whole.text = text.replacen("⏎", "\n")
	

func _on_show_input_button_pressed() -> void:
	editing = true
	text_edit.grab_focus()
	input_text_container.visible = true
	margin_container.visible = false
	await get_tree().process_frame
	text_edit.select_all()

func _on_enter_button_pressed() -> void:
	editing = false
	set_text_content(text_edit.text)
	input_text_container.visible = false
	margin_container.visible = true
	save()
	
func _on_cancel_button_pressed() -> void:
	editing = false
	input_text_container.visible = false
	margin_container.visible = true

func save():
	var file = FileAccess.open(save_filename, FileAccess.WRITE)
	file.store_string(textContent)
	file.close()
