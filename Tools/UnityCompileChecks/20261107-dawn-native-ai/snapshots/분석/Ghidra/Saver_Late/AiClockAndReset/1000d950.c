
/* void __cdecl MoveChar(int,int,int) */

void __cdecl MoveChar(int param_1,int param_2,int param_3)

{
  undefined4 *puVar1;
  CPlex *this;
  NativeCharacter32 *pNVar2;
  int iVar3;
  int iVar4;
  int iVar5;
  int local_4;
  
                    /* 0xd950  61  ?MoveChar@@YAXHHH@Z */
  ClearTa((int *)&DAT_101324ec);
  if ((param_2 < 1) && (param_3 < 1)) {
    return;
  }
  if (g_RuntimeCharacters[param_1].active == 0) {
    return;
  }
  *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x264 = 0;
  if (((g_RuntimeCharacters[param_1].field_0x100 & 1) != 0) &&
     (*(int *)&g_RuntimeCharacters[param_1].field_0x11c != 0)) {
    *(int *)&g_RuntimeCharacters[param_1].field_0x120 = param_1;
    *(int *)&g_RuntimeCharacters[param_1].field_0x124 = param_2 * 2;
    *(int *)&g_RuntimeCharacters[param_1].field_0x128 = param_3 * 2;
    return;
  }
  iVar5 = g_RuntimeCharacters[param_1].tileY;
  iVar4 = g_RuntimeCharacters[param_1].tileX;
  mainmapsetchar(iVar4,iVar5,-1);
  pNVar2 = g_RuntimeCharacters;
  if (*(int *)&g_RuntimeCharacters[param_1].field_0x138 == 0) goto LAB_1000dab3;
  iVar3 = *(int *)&g_RuntimeCharacters[param_1].field_0x11c;
  if (iVar3 == 1) {
LAB_1000da60:
    iVar4 = iVar4 + -1;
  }
  else if (iVar3 == 5) {
    iVar4 = iVar4 + -1;
    iVar5 = iVar5 + -1;
  }
  else if (iVar3 == 2) {
    iVar4 = iVar4 + 1;
  }
  else if (iVar3 == 8) {
    iVar4 = iVar4 + 1;
    iVar5 = iVar5 + 1;
  }
  else if (iVar3 == 3) {
    iVar5 = iVar5 + -1;
  }
  else if (iVar3 == 7) {
    iVar5 = iVar5 + -1;
    iVar4 = iVar4 + 1;
  }
  else if (iVar3 == 4) {
    iVar5 = iVar5 + 1;
  }
  else if (iVar3 == 6) {
    iVar5 = iVar5 + 1;
    goto LAB_1000da60;
  }
  for (puVar1 = *(undefined4 **)&g_RuntimeCharacters[param_1].field_0x130;
      puVar1 != (undefined4 *)0x0; puVar1 = (undefined4 *)*puVar1) {
  }
  this = *(CPlex **)&g_RuntimeCharacters[param_1].field_0x140;
  *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x138 = 0;
  *(undefined4 *)&pNVar2[param_1].field_0x13c = 0;
  *(undefined4 *)&pNVar2[param_1].field_0x134 = 0;
  *(undefined4 *)&pNVar2[param_1].field_0x130 = 0;
  CPlex::FreeDataChain(this);
  *(undefined4 *)&pNVar2[param_1].field_0x140 = 0;
  *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x11c = 0xffffffff;
LAB_1000dab3:
  g_RuntimeCharacters[param_1].tileX = iVar4;
  g_RuntimeCharacters[param_1].tileY = iVar5;
  *(int *)&g_RuntimeCharacters[param_1].field_0x28c = param_2;
  *(int *)&g_RuntimeCharacters[param_1].field_0x290 = param_3;
  *(int *)&g_RuntimeCharacters[param_1].field_0x2a4 = iVar4;
  *(int *)&g_RuntimeCharacters[param_1].field_0x2a8 = iVar5;
  SetArea2(iVar4,iVar5,*(int *)&g_RuntimeCharacters[param_1].field_0x3c + 7,param_1,
           *(int *)&g_RuntimeCharacters[param_1].field_0x1c,(int *)&DAT_101324ec);
  mainmapsetchar(iVar4,iVar5,param_1);
  local_4 = 0;
  if ((-1 < *(int *)(&DAT_101324ec + (param_3 + param_2 * 200) * 4)) &&
     (iVar3 = FUN_1000d2e0(param_2,param_3,param_1,iVar4,iVar5,0x101324ec,&local_4), iVar3 != 0)) {
    FUN_1000d740((undefined4 *)&g_RuntimeCharacters[param_1].field_0x12c);
    *(uint *)&g_RuntimeCharacters[param_1].field_0x100 =
         *(uint *)&g_RuntimeCharacters[param_1].field_0x100 | 1;
    *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x16c = 0xffffffff;
    *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x170 = 0xffffffff;
    *(int *)&g_RuntimeCharacters[param_1].field_0x29c = local_4 * 4;
    *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x11c = 0;
    *(int *)(&g_RuntimeCharacters[param_1].field_0x268 +
            *(int *)&g_RuntimeCharacters[param_1].field_0x264 * 8) = param_2;
    *(int *)(&g_RuntimeCharacters[param_1].field_0x26c +
            *(int *)&g_RuntimeCharacters[param_1].field_0x264 * 8) = param_3;
    return;
  }
  g_RuntimeCharacters[param_1].tileX = param_2;
  g_RuntimeCharacters[param_1].tileY = param_3;
  mainmapsetchar(iVar4,iVar5,-1);
  mainmapsetchar(param_2,param_3,param_1);
  return;
}


/* incoming refs
100140d8 DATA
Entry Point EXTERNAL
1000b47b UNCONDITIONAL_CALL
100104c6 UNCONDITIONAL_CALL
CALLER 1000b0a0 ApplyUnitSnapshot
  10014178 DATA
  Entry Point EXTERNAL
  1000ed67 UNCONDITIONAL_CALL
CALLER 10010210 FUN_10010210
  10010fb9 UNCONDITIONAL_CALL
*/
