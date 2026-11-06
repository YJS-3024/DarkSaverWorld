
/* void __cdecl SetTickMenu(struct _PGSTICKMENU) */

void __cdecl SetTickMenu(undefined4 param_1,ushort param_2)

{
  uint uVar1;
  int iVar2;
  
                    /* 0xc740  80  ?SetTickMenu@@YAXU_PGSTICKMENU@@@Z */
  iVar2 = (int)(short)param_1;
  if (g_RuntimeCharacters[iVar2].active != 0) {
    *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x10c = 0;
    if ((param_2 & 1) == 0) {
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c & 0xfffffffd;
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x100 & 0xffffffef;
    }
    else {
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c | 2;
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x100 | 0x10;
    }
    *(uint *)&g_RuntimeCharacters[iVar2].field_0x100 = uVar1;
    if ((param_2 & 2) == 0) {
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c & 0xffffffef;
    }
    else {
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c | 0x10;
    }
    if ((param_2 & 4) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c & 0xfffffff7;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c | 8;
    }
    *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c = uVar1;
    if ((param_2 & 8) == 0) {
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c & 0xffffffdf;
    }
    else {
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c | 0x20;
    }
    if ((param_2 & 0x10) == 0) {
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c & 0xfffffffe;
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x100 =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x100 & 0xffffffdf;
    }
    else {
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c | 1;
      *(uint *)&g_RuntimeCharacters[iVar2].field_0x100 =
           *(uint *)&g_RuntimeCharacters[iVar2].field_0x100 | 0x20;
    }
    if ((param_2 & 0x20) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c & 0xfffffffb;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c | 4;
    }
    *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c = uVar1;
    if ((param_2 & 0x40) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c & 0xffffffbf;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c | 0x40;
    }
    *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c = uVar1;
    if ((param_2 & 0x80) == 0) {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c & 0xffffff7f;
    }
    else {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c | 0x80;
    }
    *(uint *)&g_RuntimeCharacters[iVar2].field_0x10c = uVar1;
    uVar1 = (param_1._2_2_ < 0) - 1 & (int)param_1._2_2_;
    if ((int)*(uint *)&g_RuntimeCharacters[iVar2].field_0x5c < (int)uVar1) {
      uVar1 = *(uint *)&g_RuntimeCharacters[iVar2].field_0x5c;
    }
    *(uint *)&g_RuntimeCharacters[iVar2].field_0x60 = uVar1;
    if ((((*(int *)&g_RuntimeCharacters[iVar2].field_0x64 == DAT_1003d540) && (iVar2 % 10 != 0)) &&
        (g_RuntimeCharacters[(iVar2 / 10) * 10].aiState != 1)) &&
       ((g_RuntimeCharacters[(iVar2 / 10) * 10].aiState != 0 &&
        ((g_RuntimeCharacters[iVar2].field_0x10c & 3) != 0)))) {
      AIStart(g_RuntimeCharacters[iVar2].unitId);
    }
  }
  return;
}


/* incoming refs
10014124 DATA
Entry Point EXTERNAL
*/
