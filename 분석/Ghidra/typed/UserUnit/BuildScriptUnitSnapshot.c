/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x1000ee60; source SHA256 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b
 */

/* Build the temporary unitinfo used by dmSetChar. Observed offsets: unit+0x00, job+0x04, native
   tile X+0x48, Y+0x4c. Other fields retain their original meaning/unknown status.
   Source SHA256: 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b */

void __cdecl
BuildScriptUnitSnapshot
          (undefined4 *param_1,undefined4 param_2,undefined4 param_3,undefined4 param_4,
          undefined4 param_5,undefined4 param_6,undefined4 param_7,undefined4 param_8)

{
  param_1[1] = param_3;
  param_1[6] = param_5;
  param_1[2] = param_4;
  param_1[7] = 99;
  param_1[8] = 99;
  param_1[9] = 99;
  param_1[10] = 99;
  param_1[0xb] = 99;
  param_1[0xc] = 99;
  *param_1 = param_2;
  param_1[0x12] = param_6;
  param_1[0x13] = param_7;
  param_1[3] = 0;
  param_1[4] = 0;
  param_1[5] = 0;
  param_1[0xd] = 0xc;
  param_1[0xe] = 0;
  param_1[0xf] = 0;
  param_1[0x10] = 0;
  param_1[0x11] = 0;
  param_1[0x14] = 0;
  param_1[0x15] = 0xffffffff;
  param_1[0x16] = 10;
  param_1[0x17] = 1;
  param_1[0x18] = param_8;
  CString::Empty((CString *)(param_1 + 0x29));
  CString::Empty((CString *)(param_1 + 0x2a));
  param_1[0x2b] = 1;
  return;
}

