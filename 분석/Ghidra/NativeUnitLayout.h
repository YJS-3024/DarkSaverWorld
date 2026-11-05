/* Evidence-backed partial layouts, not recovered original source.
 * Source: late UserUnit.dll SHA256
 * 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b
 * ApplyUnitSnapshot at 0x1000b0a0 and its received/scripted callers.
 * Unknown bytes are deliberately retained. Native pointers/CStrings are not
 * converted into host pointers, and these types are not a runnable client.
 */
#ifndef DARKSAVER_NATIVE_UNIT_LAYOUT_H
#define DARKSAVER_NATIVE_UNIT_LAYOUT_H
#include <stddef.h>
#include <stdint.h>

typedef struct DsUnitInfo32 {
    int32_t unit_id;                         /* 0x00 */
    int32_t job_id;                          /* 0x04 */
    int32_t appearance_key;                  /* 0x08 */
    int32_t display_level;                   /* 0x0c; no inferred class/base split */
    uint32_t experience;                     /* 0x10 */
    uint32_t next_experience;                /* 0x14 */
    int32_t movement_type;                   /* 0x18 */
    int32_t max_hp, hp, max_mp, mp;          /* 0x1c..0x28 */
    int32_t attack, defense;                 /* 0x2c/0x30 */
    int32_t movement_budget;                 /* 0x34 */
    uint8_t unresolved_38_43[12];
    int32_t command_range;                   /* 0x44 */
    int32_t tile_x;                          /* 0x48 */
    int32_t tile_y;                          /* 0x4c */
    uint32_t magic_flags, command_flags;     /* 0x50..0x54; separate native consumers */
    int32_t max_ap, ap, team;                /* 0x58..0x60 */
    int32_t magic_slots[8];                  /* 0x64; first -1 ends native list */
    int32_t skill_slots[8];                  /* 0x84; first -1 ends native list */
    uint32_t name_pointer_1, name_pointer_2; /* 0xa4; native CString values, never host pointers */
    int32_t attack_range;                    /* 0xac */
} DsUnitInfo32;

typedef struct DsCharacter32 {
    int32_t active;                          /* 0x00 */
    int32_t enabled;                         /* 0x04 */
    uint32_t experience, next_experience;    /* 0x08..0x0c */
    int32_t unit_id;                         /* 0x10 */
    int32_t display_level;                   /* 0x14 */
    int32_t job_id;                          /* 0x18 */
    int32_t movement_type;                   /* 0x1c */
    int32_t hp, max_hp, mp, max_mp;          /* 0x20..0x2c; different order from unitinfo */
    int32_t attack_range;                    /* 0x30 */
    int32_t attack, defense;                 /* 0x34/0x38 */
    int32_t movement_budget;                 /* 0x3c */
    uint8_t unresolved_40_4b[12];
    int32_t command_range;                   /* 0x4c */
    uint8_t unresolved_50_5b[12];
    int32_t max_ap, ap, team;                /* 0x5c..0x64 */
    int32_t appearance_key;                  /* 0x68 */
    uint8_t unresolved_6c_6f[4];
    int32_t tile_x;                          /* 0x70 */
    int32_t tile_y;                          /* 0x74 */
    uint8_t unresolved_78_ff[0x88];
    uint32_t state_flags, status_flags;      /* 0x100/0x104; status is MagicFlag output */
    int32_t ai_state;                        /* 0x108 */
    uint32_t command_flags;                  /* 0x10c */
    uint8_t unresolved_110_113[4];
    uint32_t name_pointer, class_pointer;    /* 0x114/0x118; native CString values */
    uint8_t unresolved_11c_2df[0x1c4];
} DsCharacter32;

/* GameCtl5C60 fills a fixed table from a variable-length message.
 * This is the destination table layout, not a serialized wire structure.
 * D550 copies0xDD DWORDs; status100054C0 searches only its first9 rows.
 */
typedef struct DsBattleTeamName {
    int16_t team;
    char name[40];
} DsBattleTeamName;
typedef struct DsBattleTeamNames {
    int16_t count;
    DsBattleTeamName rows[21];
} DsBattleTeamNames;
_Static_assert(sizeof(DsBattleTeamName)==42,"team-name native destination stride");
_Static_assert(sizeof(DsBattleTeamNames)==884,"team-name table copied extent");
_Static_assert(offsetof(DsBattleTeamNames,rows)==2,"team-name table first record");

/* GameCtl5C54 destination table at1006A740, not its packed wire header.
 * The wire header is12 bytes; the native destination has padding before
 * spoils/count/rows. Fourteen signed WORDs plus a DWORD form each row.
 */
typedef struct DsBattleResultRow {
    int16_t words[14];
    uint32_t experience;
} DsBattleResultRow;
typedef struct DsBattleResult {
    int16_t outcome,event_total,event_completed;
    uint16_t padding_06;
    uint32_t spoils;
    int16_t count;
    uint16_t padding_0e;
    DsBattleResultRow rows[10];
} DsBattleResult;
_Static_assert(sizeof(DsBattleResultRow)==32,"result native row stride");
_Static_assert(offsetof(DsBattleResultRow,experience)==28,"result row XP DWORD");
_Static_assert(offsetof(DsBattleResult,spoils)==8,"result spoils DWORD");
_Static_assert(offsetof(DsBattleResult,rows)==16,"result destination rows");
_Static_assert(sizeof(DsBattleResult)==336,"result table before1006A890 ready flag");
/* 5C31 -> GameCtl1000DEC0: signed16 count/jobs become signed32 display keys.
 * This is a bounded analysis view of1006AD4C/1006AD50, not a claim that the
 * unguarded original receiver has a1000-record server or storage limit. */
typedef struct DsResultKillWireHeader {
    uint8_t marker, subtype;
    int16_t count;
    int16_t jobs[];
} DsResultKillWireHeader;
typedef struct DsResultKillBoundedView {
    int32_t count;
    int32_t jobs[1000];
} DsResultKillBoundedView;
_Static_assert(sizeof(DsResultKillWireHeader)==4,"kill list wire header");
_Static_assert(offsetof(DsResultKillWireHeader,jobs)==4,"kill list WORD body");
_Static_assert(offsetof(DsResultKillBoundedView,jobs)==4,"kill list destination DWORD body");

_Static_assert(sizeof(DsUnitInfo32) == 0xb0, "native unitinfo extent");
_Static_assert(offsetof(DsUnitInfo32, unit_id) == 0x00, "unitinfo unit");
_Static_assert(offsetof(DsUnitInfo32, job_id) == 0x04, "unitinfo job");
_Static_assert(offsetof(DsUnitInfo32, tile_x) == 0x48, "unitinfo X");
_Static_assert(offsetof(DsUnitInfo32, tile_y) == 0x4c, "unitinfo Y");
_Static_assert(sizeof(DsCharacter32) == 0x2e0, "native runtime stride");
_Static_assert(offsetof(DsCharacter32, unit_id) == 0x10, "runtime unit");
_Static_assert(offsetof(DsCharacter32, job_id) == 0x18, "runtime job");
_Static_assert(offsetof(DsCharacter32, tile_x) == 0x70, "runtime X");
_Static_assert(offsetof(DsCharacter32, tile_y) == 0x74, "runtime Y");
_Static_assert(offsetof(DsCharacter32, ai_state) == 0x108, "runtime AI state");
#define DS_OFFSET(type, field, offset) _Static_assert(offsetof(type, field) == offset, #type "." #field)
DS_OFFSET(DsUnitInfo32, experience, 0x10);
DS_OFFSET(DsUnitInfo32, next_experience, 0x14);
DS_OFFSET(DsUnitInfo32, movement_type, 0x18);
DS_OFFSET(DsUnitInfo32, max_hp, 0x1c);
DS_OFFSET(DsUnitInfo32, hp, 0x20);
DS_OFFSET(DsUnitInfo32, max_mp, 0x24);
DS_OFFSET(DsUnitInfo32, mp, 0x28);
DS_OFFSET(DsUnitInfo32, attack, 0x2c);
DS_OFFSET(DsUnitInfo32, defense, 0x30);
DS_OFFSET(DsUnitInfo32, movement_budget, 0x34);
DS_OFFSET(DsUnitInfo32, command_range, 0x44);
DS_OFFSET(DsUnitInfo32, max_ap, 0x58);
DS_OFFSET(DsUnitInfo32, ap, 0x5c);
DS_OFFSET(DsUnitInfo32, magic_slots, 0x64);
DS_OFFSET(DsUnitInfo32, skill_slots, 0x84);
DS_OFFSET(DsUnitInfo32, name_pointer_1, 0xa4);
DS_OFFSET(DsUnitInfo32, attack_range, 0xac);
DS_OFFSET(DsCharacter32, experience, 0x08);
DS_OFFSET(DsCharacter32, next_experience, 0x0c);
DS_OFFSET(DsCharacter32, display_level, 0x14);
DS_OFFSET(DsCharacter32, hp, 0x20);
DS_OFFSET(DsCharacter32, max_hp, 0x24);
DS_OFFSET(DsCharacter32, mp, 0x28);
DS_OFFSET(DsCharacter32, max_mp, 0x2c);
DS_OFFSET(DsCharacter32, attack_range, 0x30);
DS_OFFSET(DsCharacter32, attack, 0x34);
DS_OFFSET(DsCharacter32, defense, 0x38);
DS_OFFSET(DsCharacter32, movement_budget, 0x3c);
DS_OFFSET(DsCharacter32, command_range, 0x4c);
DS_OFFSET(DsCharacter32, max_ap, 0x5c);
DS_OFFSET(DsCharacter32, ap, 0x60);
DS_OFFSET(DsCharacter32, appearance_key, 0x68);
DS_OFFSET(DsCharacter32, state_flags, 0x100);
DS_OFFSET(DsCharacter32, status_flags, 0x104);
DS_OFFSET(DsCharacter32, command_flags, 0x10c);
DS_OFFSET(DsCharacter32, name_pointer, 0x114);
DS_OFFSET(DsCharacter32, class_pointer, 0x118);
#undef DS_OFFSET
#endif
