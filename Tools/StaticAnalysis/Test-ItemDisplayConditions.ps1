# Read-only evidence checks. Never load or execute an original EXE/DLL.
$ErrorActionPreference = 'Stop'
$itemPython = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$itemProbe = @'
import sys, json, hashlib, struct
from pathlib import Path
sys.path.insert(0, 'Tools/StaticAnalysisDeps')
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

root = Path('\uBD84\uC11D/Saver_1.2/extracted/Saver_Files/Saver')
saver_path = root / 'Saver.exe'
saver = pefile.PE(str(saver_path))
gamectl = pefile.PE(str(root / 'GameCtl.dll'))
decoder = Cs(CS_ARCH_X86, CS_MODE_32)
checks = []
def instruction(pe, address, mnemonic, operands, label):
    raw = pe.get_data(address - pe.OPTIONAL_HEADER.ImageBase, 15)
    actual = next(decoder.disasm(raw, address))
    assert (actual.mnemonic, actual.op_str) == (mnemonic, operands), (label, actual.mnemonic, actual.op_str)
    checks.append(label)

# These are sale-display checks, NOT proof of the final equipment or server rule.
unit = pefile.PE(str(root / 'UserUnit.dll'))
compress = pefile.PE(str(root / 'Compress.dll'))
dmanage = pefile.PE(str(root / 'DManage.dll'))
instruction(saver,0x41ad02,'mov','eax, 0x20','class_menu_crop_left_top_32')
instruction(saver,0x41ad15,'mov','eax, 0x40','class_menu_crop_right_bottom_64')
assert dmanage.get_string_at_rva(0x13064)==b'ReadArcBmp2'
checks.append('class_surface_decoder_ReadArcBmp2')
instruction(unit,0x1000ae33,'call','dword ptr [eax + 0x1c]','class_surface_decode_dispatch')
instruction(compress,0x42cfbe,'mov','edx, dword ptr [ebp - 0x44]','class_decoder_raw_bmp_width')
instruction(compress,0x42cfc1,'mov','dword ptr [eax], edx','class_decoder_outputs_raw_width')
instruction(compress,0x42cfc6,'mov','edx, dword ptr [ebp - 0x40]','class_decoder_raw_bmp_height')
instruction(compress,0x42cfc9,'mov','dword ptr [eax], edx','class_decoder_outputs_raw_height')
instruction(unit,0x1000ae92,'call','0x10011792','class_surface_create_with_decoded_dimensions')
instruction(unit,0x1000aec6,'call','0x1001176e','class_surface_unscaled_BltFast')
instruction(saver, 0x40a478, 'mov', 'ecx, dword ptr [ebp + 0x10]', 'display_a_item_value')
instruction(saver, 0x40a47b, 'movsx', 'edx, word ptr [eax + 0xc]', 'display_a_signed_unit_value')
instruction(saver, 0x40a47f, 'cmp', 'ecx, edx', 'display_a_comparison')
instruction(saver, 0x40a481, 'jg', '0x40a4a5', 'display_a_signed_rejection')
instruction(saver, 0x40a495, 'mov', 'eax, dword ptr [eax + 0x58]', 'ability_troop_type')
instruction(saver, 0x40a498, 'mov', 'cl, byte ptr [eax + ebp + 0x18]', 'item_type_flag')
instruction(saver, 0x40a4a0, 'cmp', 'cl, 1', 'type_flag_must_equal_one')
instruction(saver, 0x40ce70, 'mov', 'edx, dword ptr [ebp + 0x10]', 'display_b_item_value')
instruction(saver, 0x40ce73, 'movsx', 'ecx, word ptr [eax + 0xc]', 'display_b_signed_unit_value')
instruction(saver, 0x40ce77, 'cmp', 'edx, ecx', 'display_b_comparison')
instruction(saver, 0x40ce79, 'jg', '0x40cebc', 'display_b_signed_rejection')
instruction(gamectl, 0x1000724e, 'mov', 'esi, dword ptr [eax + 4]', 'icon_primary_picture')
instruction(gamectl, 0x10007264, 'mov', 'esi, 0x64', 'icon_hundred_columns')
instruction(gamectl, 0x10007277, 'shl', 'ecx, 5', 'icon_32_pixel_row')

base = saver.OPTIONAL_HEADER.ImageBase
text = saver.get_data(0x450334 - base, 160).split(b'\0', 1)[0].decode('cp949')
assert text == '\uc774\ub984: %s   \uac00\uaca9: %I64u  \ud310\ub9e4\uc5ec\ubd80: %s', repr(text)
checks.append('sale_status_format_not_equipment')

# Information-window records are not the map/movement records that happen to
# use offsets 0x0c/0x0e as coordinates. Establish provenance before naming fields.
instruction(saver, 0x41384f, 'call', '0x42c740', 'info_window_record_lookup')
instruction(saver, 0x41385a, 'mov', 'dword ptr [esi + 0xb4], eax', 'info_window_record_pointer')
instruction(saver, 0x412b4a, 'mov', 'ebp, dword ptr [esi + 0xb4]', 'info_window_record_read')
instruction(saver, 0x412b64, 'add', 'ebp, 4', 'info_window_payload_offset')
instruction(saver, 0x412e81, 'movsx', 'ecx, word ptr [ebp + 8]', 'parenthesized_field_read')
instruction(saver, 0x412e85, 'movsx', 'edx, word ptr [ebp + 0xa]', 'outer_field_read')
instruction(saver, 0x412e89, 'push', 'ecx', 'parenthesized_argument')
instruction(saver, 0x412e8a, 'push', 'edx', 'outer_argument')
instruction(saver, 0x42c740, 'mov', 'edx, dword ptr [ecx + 0x28]', 'info_record_array')
instruction(saver, 0x42c75f, 'add', 'ecx, 0x2dc', 'info_record_stride_732')
instruction(saver, 0x42c765, 'cmp', 'eax, 0x14', 'info_record_slots_20')
instruction(saver, 0x42c50b, 'mov', 'ecx, 0xb5', 'info_copy_dwords_181')
instruction(saver, 0x42c510, 'lea', 'edi, [eax + 4]', 'info_copy_payload_destination')
instruction(saver, 0x42c513, 'rep movsd', 'dword ptr es:[edi], dword ptr [esi]', 'info_payload_copy_724_bytes')
instruction(saver, 0x42c516, 'mov', 'dword ptr [eax + 0x2d8], 1', 'info_presence_flag')
instruction(saver, 0x42fb5e, 'mov', 'esi, dword ptr [ecx + 0x68]', 'separate_map_record_array')
instruction(saver, 0x42fb68, 'mov', 'word ptr [eax + 0xc], si', 'map_coordinate_c_write')
instruction(saver, 0x42fb70, 'mov', 'word ptr [eax + 0xe], si', 'map_coordinate_e_write')
instruction(saver, 0x42fba3, 'call', 'dword ptr [0x446684]', 'map_move_import_call')
move_import = [entry.name for library in saver.DIRECTORY_ENTRY_IMPORT for entry in library.imports if entry.address == 0x446684]
assert len(move_import) == 1 and move_import[0].startswith(b'?MoveChar@@'), move_import
checks.append('map_move_symbol_not_level_update')
instruction(saver, 0x4092a1, 'mov', 'ecx, 7', 'inventory_grid_seven_columns')
instruction(saver, 0x4092cb, 'cmp', 'esi, 0x1c', 'inventory_grid_twenty_eight_cells')
instruction(saver, 0x4092ba, 'push', '0x20', 'inventory_grid_cell_32')
instruction(saver, 0x40929a, 'lea', 'ecx, [eax + eax*4 + 0x28]', 'inventory_grid_row_pitch_35_origin40')
instruction(saver, 0x4092b2, 'lea', 'edx, [eax + eax*4 + 0x14]', 'inventory_grid_column_pitch_35_origin20')
instruction(saver, 0x4092d6, 'mov', 'eax, 0x116', 'shop_grid_origin_x278')
instruction(saver, 0x4092dd, 'mov', 'edx, 0x28', 'shop_grid_origin_y40')
instruction(saver, 0x40932b, 'mov', 'ecx, 3', 'shop_grid_three_columns')
instruction(saver, 0x40935a, 'cmp', 'esi, 0xc', 'shop_grid_twelve_rectangles')
instruction(saver, 0x40991a, 'call', '0x409a90', 'shop_grid_item_id_lookup')
instruction(saver, 0x4099c4, 'cmp', 'ebx, 0xc', 'shop_grid_draw_twelve_icons')
instruction(saver, 0x409add, 'lea', 'edi, [eax + 3]', 'shop_scroll_down_three_items')
instruction(saver, 0x409b17, 'add', 'eax, -3', 'shop_scroll_up_three_items')
instruction(saver, 0x40a1d0, 'mov', 'eax, dword ptr [edi + 0xbc]', 'shop_purchase_same_item_array')
instruction(saver, 0x40a1e5, 'mov', 'edi, dword ptr [eax + edx*4]', 'shop_purchase_selected_item')
instruction(saver, 0x425f61, 'push', '0x450bd4', 'shop_constructor_layout_string')
instruction(saver, 0x425fa1, 'call', '0x40a9d0', 'shop_constructor_call')
instruction(saver, 0x434f87, 'push', '0x450fe0', 'window_wlib_prefix')
assert saver.get_data(0x450bd4 - base, 32).split(b'\0')[0] == b'scene10.lsc'
assert saver.get_data(0x450fe0 - base, 16).split(b'\0')[0] == b'WLIB\\'
checks.append('shop_layout_resolves_wlib_scene10')
instruction(saver, 0x426d3a, 'push', '0x450b20', 'house_purchase_confirmation_layout_string')
instruction(saver, 0x426d76, 'call', '0x408eb0', 'house_purchase_confirmation_constructor')
instruction(saver, 0x409053, 'push', '0x450190', 'house_purchase_confirmation_question_format')
instruction(saver, 0x408fe5, 'call', '0x409140', 'house_purchase_confirmation_yes_action')
assert saver.get_data(0x450b20 - base, 32).split(b'\0')[0] == b'scene29.lsc'
checks.append('house_purchase_confirmation_scene29')
instruction(saver, 0x409d1e, 'call', '0x40a340', 'ordinary_shop_click_details')
instruction(saver, 0x4096a4, 'mov', 'dword ptr [esi + 0x35c], 1', 'ordinary_shop_sale_side_drag_start')
instruction(saver, 0x40968a, 'mov', 'dword ptr [esi + 0x364], 1', 'ordinary_shop_owned_side_drag_start')
instruction(saver, 0x409745, 'call', 'dword ptr [edx + 0x54]', 'ordinary_shop_drop_buy')
instruction(saver, 0x40976b, 'call', 'dword ptr [eax + 0x58]', 'ordinary_shop_drop_sell')
assert int.from_bytes(saver.get_data(0x446dbc + 0x54 - base, 4), 'little') == 0x40a150
assert int.from_bytes(saver.get_data(0x446dbc + 0x58 - base, 4), 'little') == 0x40a230
checks.append('ordinary_shop_vtable_buy_sell_targets')
instruction(saver, 0x40a1da, 'push', '0x4b', 'ordinary_shop_buy_opcode')
instruction(saver, 0x40a2dd, 'push', '0x4c', 'ordinary_shop_sell_opcode')
instruction(saver, 0x43d0ef, 'mov', 'byte ptr [ecx + esi], dl', 'shop_opcode_byte_writer')
instruction(saver, 0x43d0d0, 'mov', 'word ptr [ecx + esi], dx', 'shop_item_word_writer')
instruction(saver, 0x409611, 'call', '0x426f40', 'ordinary_shop_confirm_closes_window')
instruction(saver, 0x40914f, 'push', '0xd8', 'house_confirmation_opcode_not_item_buy')
instruction(saver, 0x40a7a0, 'mov', 'eax, dword ptr [ebp + 0x74]', 'item_move_tick_reduction_read')
instruction(saver, 0x40a7ac, 'push', '0x450238', 'item_move_speed_text')
instruction(saver, 0x40a7c8, 'mov', 'eax, dword ptr [ebp + 0x78]', 'item_attack_tick_reduction_read')
instruction(saver, 0x40a7d4, 'push', '0x450224', 'item_attack_speed_text')
instruction(saver, 0x40a7f0, 'mov', 'eax, dword ptr [ebp + 0x7c]', 'item_rest_tick_reduction_read')
instruction(saver, 0x40a7fc, 'push', '0x450210', 'item_rest_speed_text')
instruction(saver, 0x40a818, 'mov', 'ebp, dword ptr [ebp + 0x80]', 'item_magic_tick_reduction_read')
instruction(saver, 0x40a827, 'push', '0x4501fc', 'item_magic_speed_text')
assert saver.get_data(0x450c34-base, 32).split(b'\0')[0] == b'scene4.lsc'
instruction(saver, 0x425d11, 'push', '0x450c34', 'equipment_window_scene4_reference')
instruction(saver, 0x425d51, 'call', '0x41f570', 'equipment_window_constructor')
instruction(saver, 0x41f620, 'mov', 'ecx, 7', 'equipment_inventory_seven_columns')
instruction(saver, 0x41f62e, 'lea', 'edx, [eax + eax*4 + 0x2a]', 'equipment_inventory_origin_y42_pitch35')
instruction(saver, 0x41f641, 'lea', 'edx, [eax + eax*4 + 0x92]', 'equipment_inventory_origin_x146_pitch35')
instruction(saver, 0x41f64c, 'push', '0x20', 'equipment_inventory_cell32_height')
instruction(saver, 0x41f64e, 'push', '0x20', 'equipment_inventory_cell32_width')
instruction(saver, 0x41f65d, 'cmp', 'ebp, 0x1c', 'equipment_inventory_twenty_eight_slots')
instruction(saver, 0x41f8ee, 'call', '0x426f40', 'equipment_window_close_for_controls_one_and_two')
layout = (root/'Wlib/scene4.lsc').read_bytes()
assert struct.unpack_from('<ii',layout) == (409,227)
cursor=28
for _ in range(struct.unpack_from('<i',layout,24)[0]):
    count=struct.unpack_from('<i',layout,cursor+4)[0];cursor+=8+count*16
assert struct.unpack_from('<i',layout,cursor)[0] == 3
assert [struct.unpack_from('<iii',layout,cursor+4+i*12) for i in range(3)] == [(37,360,16),(38,377,16),(6,343,191)]
checks.append('equipment_scene4_dimensions_and_control_positions')
instruction(saver, 0x420677, 'add', 'ecx, 0xc1', 'equipment_gold_y193')
instruction(saver, 0x420685, 'lea', 'ebp, [eax + 0x68]', 'equipment_gold_right_anchor_x104')
instruction(saver, 0x4206a2, 'mov', 'eax, dword ptr [ecx + 0x38]', 'equipment_gold_value_player_state')
instruction(saver, 0x41f8fb, 'call', 'dword ptr [edx + 0x44]', 'equipment_minimize_virtual_dispatch')
assert struct.unpack('<I',saver.get_data(0x447c84+0x44-base,4))[0] == 0x435b30
instruction(saver, 0x435b73, 'call', '0x4359d0', 'common_minimize_layout_name_builder')
assert saver.get_data(0x45057c-base,16).split(b'\0')[0] == b'm.lsc'
instruction(saver, 0x435a38, 'push', '0x45057c', 'common_minimize_layout_suffix')
instruction(saver, 0x435cb6, 'mov', 'dword ptr [esi + 0x80], edi', 'common_minimize_state_flag')
instruction(saver, 0x435d23, 'call', '0x435a80', 'common_restore_original_layout_name')
mini = (root/'Wlib/scene4m.lsc').read_bytes()
assert struct.unpack_from('<ii',mini) == (29,30)
cursor=28
for _ in range(struct.unpack_from('<i',mini,24)[0]):
    cursor+=8+struct.unpack_from('<i',mini,cursor+4)[0]*16
assert struct.unpack_from('<i',mini,cursor)[0] == 0
checks.extend(['equipment_minimize_vtable_and_suffix','equipment_minimized_dimensions_no_controls'])
instruction(saver, 0x41fd9d, 'call', '0x420460', 'actual_equipped_item_drop_validation')
instruction(saver, 0x420478, 'mov', 'esi, dword ptr [edx + 0x10]', 'actual_equip_item_required_value')
instruction(saver, 0x42047f, 'movsx', 'ecx, word ptr [eax + 8]', 'actual_equip_signed_unit_value_offset8')
instruction(saver, 0x420483, 'cmp', 'esi, ecx', 'actual_equip_required_value_comparison')
instruction(saver, 0x420485, 'jg', '0x4204fa', 'actual_equip_signed_requirement_rejection')
instruction(saver, 0x4204a2, 'cmp', 'byte ptr [eax + ecx + 0x18], 1', 'actual_equip_troop_permission_flag')
assert list(struct.unpack('<10I',saver.get_data(0x420604-base,40))) == [0x42051c,0x42052d,0x4205c5,0x4205ca,0x4205ec,0x4205cf,0x4204fa,0x42052d,0x4204fa,0x4205ec]
checks.append('actual_equip_position_switch_all_ten_types')
instruction(saver, 0x420565, 'cmp', 'edx, dword ptr [ecx + 0xc]', 'actual_equip_opposite_slot_type_comparison')
instruction(saver, 0x420568, 'je', '0x4204fa', 'actual_equip_opposite_same_type_rejected')
instruction(saver, 0x420823, 'push', '0x40', 'equipment_request_opcode40')
instruction(saver, 0x420837, 'call', '0x43d0e0', 'equipment_request_opcode_writer_u8')
for address,label in [(0x420846,'unit_record'),(0x420852,'slot'),(0x42085e,'item'),(0x42086e,'additional_unknown'),(0x420881,'previous_item')]:
    instruction(saver,address,'call','0x43d0c0','equipment_request_word_'+label)
instruction(saver, 0x43d0d0, 'mov', 'word ptr [ecx + esi], dx', 'equipment_word_writer_native_x86_u16')
instruction(saver, 0x43d0d6, 'add', 'ecx, 2', 'equipment_word_writer_advances_two_bytes')
instruction(saver, 0x4205ad, 'cmp', 'edx, dword ptr [ecx + 0xc]', 'actual_equip_other_hand_type_comparison')
instruction(saver, 0x4205b0, 'je', '0x4204fa', 'actual_equip_other_hand_same_type_rejected')
instruction(saver, 0x40f619, 'call', '0x40f530', 'equipment_slot_write_has_drop_path_caller_not_ack_proof')
for name,size,expected in [('scene6.lsc',(335,274),[(61,286,16),(62,303,16),(82,29,230)]),('scene6m.lsc',(34,22),[]),('scene40.lsc',(419,224),[(90,351,158),(19,351,186)])]:
    data=(root/'Wlib'/name).read_bytes()
    assert struct.unpack_from('<ii',data) == size
    cursor=28
    for _ in range(struct.unpack_from('<i',data,24)[0]):
        cursor+=8+struct.unpack_from('<i',data,cursor+4)[0]*16
    assert struct.unpack_from('<i',data,cursor)[0] == len(expected)
    assert [struct.unpack_from('<iii',data,cursor+4+i*12) for i in range(len(expected))] == expected
    cursor+=4+len(expected)*12
    expected_tail = (1,1,279,112) if name=='scene40.lsc' else (0,)
    assert struct.unpack_from('<'+'i'*len(expected_tail),data,cursor) == expected_tail and cursor+len(expected_tail)*4 == len(data)
    checks.append('character_info_layout_'+name)
instruction(saver, 0x425d9b, 'call', '0x4126a0', 'character_info_constructor')
instruction(saver, 0x412737, 'mov', 'dword ptr [esi], 0x4473f4', 'character_info_vtable')
assert struct.unpack('<I',saver.get_data(0x4473f4+0x10-base,4))[0] == 0x412980
checks.append('character_info_button_handler_registration')
instruction(saver, 0x4129d6, 'call', '0x426f40', 'character_info_close_button')
instruction(saver, 0x4129e3, 'call', 'dword ptr [edx + 0x44]', 'character_info_minimize_button')
instruction(saver, 0x41299a, 'push', '0x28', 'character_info_third_button_other_window_unconnected')
instruction(saver,0x4129a7,'push','0x28','character_adjustment_lookup_scene40')
instruction(saver,0x4129a9,'call','0x427270','character_adjustment_lookup_window')
instruction(saver,0x4129b8,'movsx','edx, word ptr [ecx + 4]','character_adjustment_pass_info_record_id')
instruction(saver,0x4129bf,'call','0x41a830','character_adjustment_set_record')
instruction(saver, 0x412c70, 'mov', 'dword ptr [esp + 0x14], 0x92', 'character_info_name_left146')
instruction(saver, 0x412c78, 'mov', 'dword ptr [esp + 0x1c], 0x140', 'character_info_name_right320')
instruction(saver, 0x412c80, 'mov', 'dword ptr [esp + 0x18], 0x29', 'character_info_name_top41')
instruction(saver, 0x412c94, 'mov', 'dword ptr [esp + 0x28], 0x39', 'character_info_name_bottom57')
instruction(saver, 0x412cbe, 'lea', 'ecx, [ebp + 0xb7]', 'character_info_name_record_string')
instruction(saver, 0x412d87, 'push', '0x11', 'character_info_job_next_row_offset17')
instruction(saver, 0x412da5, 'call', 'dword ptr [0x4466b0]', 'character_info_job_ability_lookup')
instruction(saver, 0x412dae, 'add', 'eax, 0xa0', 'character_info_job_name_ability_string')
instruction(saver, 0x41320f, 'push', '0x8c', 'character_info_gold_bottom140')
instruction(saver, 0x413214, 'push', '0x13e', 'character_info_gold_right318')
instruction(saver, 0x413219, 'push', '0x7c', 'character_info_gold_top124')
instruction(saver, 0x41321f, 'push', '0x92', 'character_info_gold_left146')
instruction(saver, 0x413248, 'mov', 'eax, dword ptr [ecx + 0x38]', 'character_info_gold_player_state_value')
assert saver.get_data(0x4500fc-base,16).split(b'\0')[0] == b'%lu'
checks.append('character_info_gold_original_unsigned_format')
userunit=pefile.PE(str(root/'UserUnit.dll'))
instruction(saver,0x41ad56,'mov','eax, dword ptr [ebx + 0xec]','class_options_left_uses_second_lookup')
instruction(saver,0x41ae43,'mov','eax, dword ptr [ebx + 0xe8]','class_options_right_uses_first_lookup')
instruction(saver,0x41ad22,'lea','eax, [ebx + 0x38c]','class_options_left_rect_reference')
instruction(saver,0x41ae0f,'lea','ecx, [ebx + 0x3ac]','class_options_right_rect_reference')
instruction(saver,0x41a474,'lea','ecx, [esi + 0xa0]','class_options_name_from_ability_record')
instruction(saver,0x41a482,'add','esi, 0x1a0','class_options_description_from_ability_record')
instruction(saver,0x419981,'mov','dword ptr [esi + 0xfc], eax','class_options_previous_index_write')
instruction(saver,0x4199b4,'mov','dword ptr [esi + 0xfc], edx','class_options_next_index_write')
for address,color in [(0x4192b5,0xff0000),(0x4192c8,0xff),(0x4192db,0x4080ff),(0x4192ee,0xffff00),(0x419301,0xff00),(0x419314,0xffff)]:
    instruction(saver,address,'push',hex(color),'equipment_stat_brush_COLORREF_'+hex(address))
instruction(saver,0x4190ee,'push','0xffffff','equipment_stat_gain_brush_white')
for address in [0x41ba18,0x41ba2c]: instruction(saver,address,'call','dword ptr [0x4461a0]','equipment_stat_MulDiv_'+hex(address))
for address in [0x41b88c,0x41b8b3]: instruction(saver,address,'call','ebx','equipment_stat_fillrect_'+hex(address))
for address,name in [(0x4461a0,b'MulDiv'),(0x4460e8,b'CreateSolidBrush'),(0x44660c,b'FillRect')]:
    assert any(entry.address==address and entry.name==name for library in saver.DIRECTORY_ENTRY_IMPORT for entry in library.imports)
    checks.append('equipment_stat_import_'+name.decode())
instruction(saver,0x41ba3b,'jle','0x41ba44','equipment_stat_current_upper_width_clamp')
instruction(saver,0x41ba5a,'cmp','dword ptr [ecx], eax','equipment_stat_baseline_width_limited_to_current')
assert saver.get_data(0x4508d4-base,128).split(b'\0')[0].decode('cp949')=='\uc7a5\ucc29\uc804:%d/\uc7a5\ucc29\ud6c4:%d/\ud55c\uacc4\uce58:%d'
checks.append('equipment_stat_original_before_after_limit_format')
for i,(before,after,limit) in enumerate([(0x41b986,0x41b98a,0x41b992),(0x41b998,0x41b99c,0x41b9a4),(0x41b9aa,0x41b9ae,0x41b9b6),(0x41b9bc,0x41b9c0,0x41b9c8),(0x41b9ce,0x41b9d2,0x41b9da),(0x41b9e0,0x41b9e4,0x41b9ec)]):
    for address,register,offset in [(before,'ebx',0x14+i*6),(after,'ebp',0x16+i*6),(limit,'edi',0x18+i*6)]:
        instruction(saver,address,'movsx',f'{register}, word ptr [eax + {hex(offset)}]','equipment_stat_triplet_'+hex(offset))
instruction(saver,0x41b523,'movsx','eax, word ptr [edx + 0x38]','equipment_stat_remaining_points_display')
instruction(saver,0x419d08,'push','0xe2','equipment_stat_first_row_y226')
instruction(saver,0x419d0d,'push','0x11d','equipment_stat_row_x285')
instruction(saver,0x419d29,'push','0x16','equipment_stat_row_step22')
instruction(saver,0x419d62,'mov','eax, 0x1e4','equipment_stat_points_x484')
instruction(saver,0x419d67,'mov','ecx, 0x16d','equipment_stat_points_y365')
for address,opcode in [(0x419fdf,0x84),(0x41a0a3,0x83),(0x41a133,0x85)]:
    instruction(saver,address,'push',hex(opcode),'adjustment_magic_request_opcode_'+hex(opcode))
for address in [0x419ffe,0x41a00d,0x41a038,0x41a0c1,0x41a0d8,0x41a151,0x41a168]:
    instruction(saver,address,'call','0x43d0c0','adjustment_magic_request_word_'+hex(address))
instruction(saver,0x419ff3,'mov','edx, dword ptr [esp + 0x30]','adjustment_magic84_first_word_caller_argument')
instruction(saver,0x41a0d0,'mov','cx, word ptr [esi + eax*2 + 0x42]','adjustment_magic83_uses_list0')
instruction(saver,0x41a160,'mov','cx, word ptr [edi + eax*2 + 0x6c]','adjustment_magic85_uses_list1')
instruction(saver,0x41a1c6,'mov','di, word ptr [edx + 2]','adjustment_list1_compact_read_next')
instruction(saver,0x41a1cb,'mov','word ptr [edx], di','adjustment_list1_compact_write_current')
instruction(saver,0x41a1db,'dec','word ptr [eax + 0x6a]','adjustment_list1_decrement_count')
instruction(saver,0x41a1e9,'mov','dword ptr [ecx + 0xb8], 0xffffffff','adjustment_list1_empty_clears_selection')
instruction(saver,0x41a204,'mov','dword ptr [ecx + 0xb8], eax','adjustment_list1_clamps_selection')
instruction(saver,0x419d9b,'cmp','esi, 2','adjustment_stat_decrease_mode2')
assert list(struct.unpack('<12I',saver.get_data(0x4199e4-base,48)))==[0x4198bc+15*i for i in range(6)]+[0x419916+15*i for i in range(6)]
checks.append('adjustment_controls4_to9_decrease_10_to15_increase')
for i in range(6):
    instruction(saver,0x4198bc+15*i,'push','2','adjustment_decrease_direction_'+str(i))
    instruction(saver,0x419916+15*i,'push','1','adjustment_increase_direction_'+str(i))
instruction(saver,0x419dee,'cmp','ecx, eax','adjustment_decrease_compares_opening_snapshot')
instruction(saver,0x419df0,'jle','0x419f03','adjustment_decrease_rejects_at_baseline')
instruction(saver,0x419dfc,'movsx','eax, word ptr [edi + 0x38]','adjustment_signed_remaining_points')
instruction(saver,0x419e00,'cmp','eax, dword ptr [ebp + ecx*4 + 0x19c]','adjustment_8b_values_are_per_stat_costs')
instruction(saver,0x419e07,'jl','0x419f03','adjustment_increase_requires_points')
instruction(saver,0x419e1b,'push','0x82','adjustment_stat_request_opcode82_not_battle_attack')
for address in [0x419e39,0x419e49,0x419e55]: instruction(saver,address,'call','0x43d0c0','adjustment_stat_request_word_'+hex(address))
instruction(saver,0x419e61,'cmp','esi, 1','adjustment_increase_mode1')
instruction(saver,0x419e6e,'sub','word ptr [edi + 0x38], dx','adjustment_increase_spends_cost')
instruction(saver,0x419e80,'add','word ptr [edi + 0x38], cx','adjustment_decrease_refunds_cost')
assert list(struct.unpack('<6I',saver.get_data(0x419f0c-base,24)))==[0x419db4,0x419dbe,0x419dc8,0x419dd2,0x419ddc,0x419de6]
checks.append('adjustment_six_stat_baseline_branches')
for address,offset in [(0x419e99,0x16),(0x419ea8,0x1c),(0x419eb7,0x22),(0x419ec6,0x28),(0x419ed5,0x2e),(0x419ee4,0x34)]:
    instruction(saver,address,'add',f'word ptr [edi + {hex(offset)}], si','adjustment_stat_signed_unit_increment_'+hex(offset))
instruction(saver,0x419e95,'and','esi, 0xfffffffe','adjustment_stat_step_sign_mask')
instruction(saver,0x419e98,'inc','esi','adjustment_stat_step_plus_or_minus_one')
for address,opcode in [(0x41a659,0x88),(0x41a6b9,0x89),(0x41a7e2,0x8a),(0x41a735,0x81)]:
    instruction(saver,address,'push',hex(opcode),'adjustment_command_opcode_'+hex(opcode))
for address in [0x41a668,0x41a6c8,0x41a7f1,0x41a744]:
    instruction(saver,address,'call','0x43d0e0','adjustment_command_u8_'+hex(address))
for address in [0x41a677,0x41a6d7,0x41a807,0x41a753,0x41a796]:
    instruction(saver,address,'call','0x43d0c0','adjustment_command_word_'+hex(address))
instruction(saver,0x419854,'call','0x41a640','adjustment_confirm_handler')
instruction(saver,0x419866,'call','0x41a6a0','adjustment_cancel_handler')
targets=struct.unpack('<21I',saver.get_data(0x4199d4-base,84))
assert targets[1]==targets[19]==0x419852 and targets[20]==0x419864
checks.append('adjustment_native_x_and_confirm_share_opcode88_cancel_uses89')
instruction(saver,0x41928b,'call','0x41a7d0','adjustment_constructor_queries8a')
instruction(saver,0x41b077,'movsx','ecx, word ptr [eax + 0x40]','adjustment_magic_list0_count')
instruction(saver,0x41b06a,'add','edi, 0x42','adjustment_magic_list0_first_id')
instruction(saver,0x41b133,'cmp','ebp, 0x10','adjustment_magic_list0_capacity16')
instruction(saver,0x41b187,'movsx','ecx, word ptr [eax + 0x6a]','adjustment_magic_list1_count')
instruction(saver,0x41b17a,'add','edi, 0x6c','adjustment_magic_list1_first_id')
instruction(saver,0x41b243,'cmp','ebp, 8','adjustment_magic_list1_capacity8')
instruction(saver,0x41a4c3,'movsx','edx, word ptr [edx + eax*2 + 0x6c]','adjustment_selected_list1_signed_id')
instruction(saver,0x41a4cf,'movsx','edx, word ptr [edx + eax*2 + 0x42]','adjustment_selected_list0_signed_id')
instruction(saver,0x41a531,'call','dword ptr [0x4466ac]','adjustment_magic_detail_table_lookup')
instruction(saver,0x419c21,'push','0xa0','adjustment_magic_detail_top160')
instruction(saver,0x419c2b,'push','0xb1','adjustment_magic_detail_left177')
instruction(saver,0x419c3f,'push','0x25','adjustment_magic_detail_height37')
instruction(saver,0x419c41,'push','0x159','adjustment_magic_detail_width345')
instruction(saver,0x41a569,'cmp','dword ptr [esi + 0x118], 0x22','adjustment_magic_kind_uses_raw_team34')
instruction(saver,0x41a595,'mov','eax, dword ptr [esi + 0x110]','adjustment_magic_detail_mp')
instruction(saver,0x41a5c6,'mov','eax, dword ptr [esi + 0x108]','adjustment_magic_detail_range')
instruction(saver,0x41a5ef,'mov','esi, dword ptr [esi + 0x10c]','adjustment_magic_detail_area')
for address,expected in [(0x450884,'\ubc94\uc704: %d '),(0x450890,'\uac70\ub9ac: %d '),(0x45089c,'\ub9c8\ubc95\ub825 \uc18c\ubaa8: %d '),(0x4508b0,'\ubcf4\uc870 \uacc4\uc5f4'),(0x4508bc,'\uacf5\uaca9 \uacc4\uc5f4'),(0x4508c8,'\ub9c8\ubc95 \uc885\ub958: ')]:
    assert saver.get_string_at_rva(address-base).decode('cp949')==expected
    checks.append('adjustment_magic_detail_string_'+hex(address))
instruction(saver,0x419b8f,'sar','eax, 3','adjustment_magic_grid_eight_columns')
instruction(saver,0x419b97,'lea','edx, [ecx + eax + 0x2f]','adjustment_magic_list0_y47_stride33')
instruction(saver,0x419baf,'lea','edx, [ecx + eax + 0xd0]','adjustment_magic_list0_x208_stride33')
instruction(saver,0x419be3,'push','0x79','adjustment_magic_list1_y121')
instruction(saver,0x41a308,'cmp','byte ptr [edi], 0x87','adjustment_options_opcode87')
instruction(saver,0x41a31a,'inc','edi','adjustment_options_payload_after_opcode')
instruction(saver,0x41a323,'call','0x43e2f0','adjustment_options_skipped_header_word')
instruction(saver,0x41a333,'movsx','edx, word ptr [eax]','adjustment_options_first_signed_lookup')
instruction(saver,0x41a33a,'mov','dword ptr [esi + 0xe8], edx','adjustment_options_first_lookup_storage')
instruction(saver,0x41a347,'movsx','ecx, word ptr [eax]','adjustment_options_second_signed_lookup')
instruction(saver,0x41a34e,'mov','dword ptr [esi + 0xec], ecx','adjustment_options_second_lookup_storage')
instruction(saver,0x41a35b,'movsx','eax, word ptr [eax]','adjustment_options_signed_count')
instruction(saver,0x41a361,'mov','dword ptr [esi + 0xf0], eax','adjustment_options_count_storage')
instruction(saver,0x41a367,'lea','ebp, [esi + 0x100]','adjustment_options_array')
instruction(saver,0x41a380,'movsx','edx, word ptr [eax]','adjustment_options_signed_entry')
instruction(saver,0x41a38b,'mov','dword ptr [ebp], 0xffffffff','adjustment_options_unused_entry_sentinel')
instruction(saver,0x41a396,'cmp','ebx, 0xa','adjustment_options_ten_entry_storage')
instruction(saver,0x41a450,'mov','esi, dword ptr [esi + 0xe8]','adjustment_lookup_uses_first_value')
instruction(saver,0x41a458,'mov','esi, dword ptr [esi + 0xec]','adjustment_lookup_uses_second_value')
instruction(saver,0x41a465,'call','dword ptr [0x4466b0]','adjustment_lookup_uses_character_ability_table')
instruction(saver,0x41a22f,'cmp','word ptr [edi + 0x3a], 0','adjustment_query_original_guard_not_auto_sent')
instruction(saver,0x41a244,'push','0x86','adjustment_query_opcode86')
instruction(saver,0x41a253,'call','0x43d0e0','adjustment_query_u8_writer')
instruction(saver,0x41a258,'mov','cx, word ptr [edi]','adjustment_query_record_word')
instruction(saver,0x41a262,'call','0x43d0c0','adjustment_query_u16_writer')
instruction(saver,0x41a299,'sub','eax, 0x87','adjustment_reply_base_opcode87')
instruction(saver,0x41a2a0,'sub','eax, 4','adjustment_reply_opcode8b')
instruction(saver,0x41a2ad,'mov','dword ptr [esp + 0x10], 1','adjustment_reply_first_word_offset1')
instruction(saver,0x41a2bb,'mov','edi, 6','adjustment_reply_six_values')
instruction(saver,0x41a2c6,'call','0x43e2f0','adjustment_reply_word_reader')
instruction(saver,0x41a2cb,'movsx','ecx, word ptr [eax]','adjustment_reply_signed_values')
instruction(saver,0x43e2f6,'add','ecx, 2','original_word_reader_advances_two')
instruction(saver,0x419b05,'mov','eax, 0x32','adjustment_identity_origin_x50')
instruction(saver,0x419b0a,'mov','ecx, 0x80','adjustment_name_origin_y128')
instruction(saver,0x419b1b,'mov','eax, 0x11','adjustment_job_origin_delta17')
instruction(saver,0x419b30,'mov','dword ptr [esi + 0x1e0], ebx','adjustment_job_point_y145')
instruction(saver,0x41aa02,'lea','edx, [ebp + 0xb7]','adjustment_name_record_string')
instruction(saver,0x41aaaf,'movsx','eax, word ptr [ebp + 6]','adjustment_job_id_from_info_payload')
instruction(saver,0x41aab4,'call','dword ptr [0x4466b0]','adjustment_job_ability_lookup')
instruction(saver,0x41aabd,'add','eax, 0xa0','adjustment_job_ability_name')
instruction(saver,0x42628f,'push','0x450b74','adjustment_constructor_resource_string')
assert saver.get_data(0x450b74-base,32).split(b'\0')[0]==b'scene23.lsc'
checks.append('adjustment_internal40_uses_scene23_not_scene40')
instruction(saver,0x4262cf,'call','0x419030','adjustment_original_constructor_call')
instruction(saver,0x419141,'mov','dword ptr [esi + 0x198], 0x28','adjustment_internal_window_id40')
instruction(saver,0x41a8ab,'call','dword ptr [0x44666c]','adjustment_original_getface_call')
instruction(saver,0x4191bd,'push','0x40','adjustment_portrait_surface64_height')
instruction(saver,0x4191bf,'push','0x40','adjustment_portrait_surface64_width')
instruction(saver,0x419ad6,'push','0x2f','adjustment_portrait_top47')
instruction(saver,0x419ad8,'push','0x14','adjustment_portrait_left20')
data=(root/'Wlib'/'scene23.lsc').read_bytes()
assert struct.unpack_from('<ii',data)==(540,435)
cursor=28
for _ in range(struct.unpack_from('<i',data,24)[0]): cursor+=8+struct.unpack_from('<i',data,cursor+4)[0]*16
expected_ids=[37,38,77,79,78,78,78,78,78,78,80,80,80,80,80,80,56,57,81,19,54]
assert struct.unpack_from('<i',data,cursor)[0]==21
assert [struct.unpack_from('<i',data,cursor+4+i*12)[0] for i in range(21)]==expected_ids
assert struct.unpack_from('<iii',data,cursor+4+12)==(38,507,16)
assert cursor+4+21*12+4==len(data) and struct.unpack_from('<i',data,len(data)-4)[0]==0
checks.append('adjustment_scene23_geometry_and_21_controls')
instruction(saver,0x413860,'movsx','eax, word ptr [eax + 0xa]','character_info_face_class_id_with_record_prefix')
instruction(saver,0x41386a,'call','dword ptr [0x44666c]','character_info_getface_call')
instruction(saver,0x412a49,'add','ecx, 0x2e','character_info_face_y46')
instruction(saver,0x412a4e,'add','eax, 0x1d','character_info_face_x29')
instruction(saver,0x412778,'push','0x40','character_info_face_surface_height64')
instruction(saver,0x41277a,'push','0x40','character_info_face_surface_width64')
instruction(userunit,0x1000f211,'cmp','esi, 4','getface_four_archive_search_limit')
for address,value in [(0x10015258,b'%d.bmp'),(0x10015244,b'gameres\\face.arc'),(0x10015230,b'gameres\\face%d.arc'),(0x10015208,b'gameres\\etc.arc'),(0x10015224,b'noface.bmp')]:
    assert userunit.get_data(address-userunit.OPTIONAL_HEADER.ImageBase,64).split(b'\0')[0]==value
    checks.append('getface_string_'+hex(address))
# Info-panel display words: payload-relative, signed; no current/max inference.
for address,register,offset in [
    (0x41342c,'eax',0x16),(0x413465,'eax',0x1c),(0x41349e,'eax',0x22),(0x4134d7,'eax',0x28),
    (0x413510,'eax',0x2b8),(0x41354c,'eax',0x2ba),(0x413588,'eax',0x2e),(0x4135c1,'eax',0x34),
    (0x41361d,'ecx',0x2bc),(0x413659,'ecx',0x2be),(0x413695,'ecx',0x2c0),(0x4136d1,'ecx',0x2c2),
    (0x41370d,'ecx',0x2c4),(0x413749,'ecx',0x2c6),(0x413785,'ecx',0x2c8),(0x4137c1,'ecx',0x2ca)]:
    instruction(saver,address,'movsx',f'{register}, word ptr [ebp + {hex(offset)}]','info_signed_display_word_'+hex(offset))
assert saver.get_data(0x450130-base,16).split(b'\0')[0] == b'%d'
checks.append('info_display_words_signed_decimal')
instruction(saver,0x412e81,'movsx','ecx, word ptr [ebp + 8]','info_level_parenthesized_signed_field8')
instruction(saver,0x412e85,'movsx','edx, word ptr [ebp + 0xa]','info_level_first_signed_fieldA')
instruction(saver,0x412e89,'push','ecx','info_level_second_argument')
instruction(saver,0x412e8a,'push','edx','info_level_first_argument')
instruction(saver,0x412f5d,'mov','ecx, dword ptr [ebp + 0x10]','info_experience_second_dword')
instruction(saver,0x412f60,'mov','edx, dword ptr [ebp + 0xc]','info_experience_first_dword')
instruction(saver,0x412e76,'push','0xf','info_level_row_delta15')
instruction(saver,0x412f52,'push','0x11','info_experience_row_delta17')
assert saver.get_string_at_rva(0x450624-base)==b'%d(%d)'
checks.append('info_two_level_original_format')
assert saver.get_string_at_rva(0x45061c-base)==b'%lu/%lu'
checks.append('info_two_unsigned32_experience_original_format')
instruction(saver,0x41334c,'mov','edx, dword ptr [esi + 0xb0]','info_weapon_name_separate_state')
instruction(saver,0x413356,'mov','ecx, dword ptr [edx + 0x10]','info_weapon_name_string_not_item_id')
instruction(saver,0x41330f,'push','0xa1','info_weapon_text_bottom161')
instruction(saver,0x413314,'push','0x5e','info_weapon_text_right94')
instruction(saver,0x413316,'push','0x91','info_weapon_text_top145')
instruction(saver,0x41331f,'push','0xe','info_weapon_text_left14')
instruction(saver,0x412a8e,'mov','ax, word ptr [eax + 0x96]','info_auxiliary_class_raw_word96')
instruction(saver,0x412a95,'cmp','ax, 0xffff','info_auxiliary_no_class_minus1')
instruction(saver,0x412aa1,'call','0x428010','info_auxiliary_class_to_sprite_lookup')
instruction(saver,0x412aa9,'call','dword ptr [0x446668]','info_auxiliary_uses_character_not_item_surface')
instruction(saver,0x412a71,'lea','ebp, [ecx + 0xb2]','info_auxiliary_icon_top178')
instruction(saver,0x412a82,'lea','ebx, [eax + 0x1d]','info_auxiliary_icon_left29')
instruction(saver,0x412b92,'movsx','edx, word ptr [ebp + 0x98]','info_auxiliary_signed_count98')
assert saver.get_string_at_rva(0x45062c-base)==b'X %d'
checks.append('info_auxiliary_original_count_format')
instruction(saver,0x41304a,'mov','ecx, dword ptr [esi + 0xb0]','info_age_separate_state')
instruction(saver,0x413054,'mov','eax, dword ptr [ecx + 8]','info_age_signed_dword_state8')
instruction(saver,0x41312a,'mov','ecx, dword ptr [esi + 0xb0]','info_gender_separate_state')
instruction(saver,0x413130,'mov','eax, dword ptr [ecx + 0xc]','info_gender_dword_stateC')
instruction(saver,0x413030,'push','0x11','info_age_row_delta17')
instruction(saver,0x413043,'add','eax, 0x3c','info_age_width60')
instruction(saver,0x413121,'push','0x66','info_gender_column_delta102')
for address,expected in [(0x45060c,'\uc911\uc131'),(0x450614,'\uc5ec'),(0x450618,'\ub0a8')]:
    assert saver.get_string_at_rva(address-base).decode('cp949')==expected
    checks.append('info_gender_original_string_'+hex(address))
for address,value,label in [(0x4133fe,0xa0,'bottom160'),(0x413403,0xd6,'left_column_right214'),(0x413408,0x92,'top146'),(0x413411,0x9d,'left157'),(0x4135f4,0x13e,'right_column_right318'),(0x413602,0x104,'right260'),(0x41345a,0xe,'row_step14'),(0x41344d,0x25,'singleline_center_flags')]:
    instruction(saver,address,'push',hex(value),'info_values_geometry_'+label)
print(json.dumps({'passed': len(checks), 'saver_sha256': hashlib.sha256(saver_path.read_bytes()).hexdigest(),
                  'scope': 'sale display, record boundaries and inventory grid; base/job field names and final equipment rules unresolved', 'checks': checks}))
'@
$itemProbe | & $itemPython -
if ($LASTEXITCODE -ne 0) { throw 'Original item display evidence did not match.' }
