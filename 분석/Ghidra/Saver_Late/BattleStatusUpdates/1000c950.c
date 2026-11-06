
/* int __cdecl MagicFlag(int,unsigned long) */

int __cdecl MagicFlag(int param_1,ulong param_2)

{
  uint uVar1;
  
                    /* 0xc950  59  ?MagicFlag@@YAHHK@Z */
  if (g_RuntimeCharacters[param_1].active != 0) {
    *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x104 = 0;
    if ((param_2 & 1) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xffffffbf;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x40;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 2) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xffffffdf;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x20;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 4) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xfffffffb;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 4;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 8) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xffffff7f;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x80;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x10) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xffffffef;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x10;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x20) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xfffffff7;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 8;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x40) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xfffffffe;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 1;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x80) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xfffffeff;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x100;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x100) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xfffffdff;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x200;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x200) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xfffffbff;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x400;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x400) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xfffff7ff;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x800;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x800) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xffffefff;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x1000;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x1000) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xffffdfff;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x2000;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x2000) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xffffbfff;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x4000;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x4000) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xfffffffd;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 2;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 = uVar1;
    if ((param_2 & 0x8000) != 0) {
      *(uint *)&g_RuntimeCharacters[param_1].field_0x104 =
           *(uint *)&g_RuntimeCharacters[param_1].field_0x104 | 0x8000;
      return param_1;
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x104 =
         *(uint *)&g_RuntimeCharacters[param_1].field_0x104 & 0xffff7fff;
  }
  return param_1;
}


/* incoming refs
100140d0 DATA
Entry Point EXTERNAL
1000b444 UNCONDITIONAL_CALL
CALLER 1000b0a0 ApplyUnitSnapshot
  10014178 DATA
  Entry Point EXTERNAL
  1000ed67 UNCONDITIONAL_CALL
*/
