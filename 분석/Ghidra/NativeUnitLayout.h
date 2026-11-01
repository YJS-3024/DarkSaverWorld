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
    uint8_t unresolved_08_47[0x40];
    int32_t tile_x;                          /* 0x48 */
    int32_t tile_y;                          /* 0x4c */
    uint8_t unresolved_50_af[0x60];
} DsUnitInfo32;

typedef struct DsCharacter32 {
    int32_t active;                          /* 0x00 */
    uint8_t unresolved_04_0f[0x0c];
    int32_t unit_id;                         /* 0x10 */
    uint8_t unresolved_14_17[4];
    int32_t job_id;                          /* 0x18 */
    uint8_t unresolved_1c_6f[0x54];
    int32_t tile_x;                          /* 0x70 */
    int32_t tile_y;                          /* 0x74 */
    uint8_t unresolved_78_107[0x90];
    int32_t ai_state;                        /* 0x108 */
    uint8_t unresolved_10c_2df[0x1d4];
} DsCharacter32;

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
#endif
