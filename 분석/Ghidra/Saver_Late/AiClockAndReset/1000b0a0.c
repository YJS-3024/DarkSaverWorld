
/* Original export resetcharacter(unitinfo*). Apply the supplied unit snapshot into the runtime
   character array at unit*0x2e0. Copies unit/job/x/y and other fields; this is not a complete
   replacement implementation. Called by received records and scripted actor creation.
   Source SHA256: 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b */

int __cdecl ApplyUnitSnapshot(NativeUnitInfo32 *snapshot)

{
  undefined1 *puVar1;
  int iVar2;
  undefined4 *puVar3;
  CPlex *this;
  int iVar4;
  int iVar5;
  bool bVar6;
  NativeCharacter32 *pNVar7;
  NativeUnitInfo32 *pNVar8;
  int iVar9;
  int iVar10;
  int local_10;
  int local_c;
  
  pNVar8 = snapshot;
                    /* 0xb0a0  101  ?resetcharacter@@YAHPAUunitinfo@@@Z */
  pNVar7 = g_RuntimeCharacters;
  iVar2 = snapshot->unitId;
  bVar6 = false;
  local_c = 0;
  local_10 = 0;
  if ((g_RuntimeCharacters[iVar2].active != 0) &&
     ((g_RuntimeCharacters[iVar2].field_0x100 & 1) != 0)) {
    local_c = *(int *)&g_RuntimeCharacters[iVar2].field_0x28c;
    local_10 = *(int *)&g_RuntimeCharacters[iVar2].field_0x290;
    bVar6 = true;
  }
  for (puVar3 = *(undefined4 **)&g_RuntimeCharacters[iVar2].field_0x130; puVar3 != (undefined4 *)0x0
      ; puVar3 = (undefined4 *)*puVar3) {
  }
  this = *(CPlex **)&g_RuntimeCharacters[iVar2].field_0x140;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x138 = 0;
  *(undefined4 *)&pNVar7[iVar2].field_0x13c = 0;
  *(undefined4 *)&pNVar7[iVar2].field_0x134 = 0;
  *(undefined4 *)&pNVar7[iVar2].field_0x130 = 0;
  CPlex::FreeDataChain(this);
  *(undefined4 *)&pNVar7[iVar2].field_0x140 = 0;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x11c = 0xffffffff;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x120 = 0xffffffff;
  g_RuntimeCharacters[iVar2].active = 1;
  iVar4 = g_RuntimeCharacters[iVar2].tileX;
  iVar5 = g_RuntimeCharacters[iVar2].tileY;
  *(int *)&g_RuntimeCharacters[iVar2].field_0x64 = iVar2 / 0x32;
  g_RuntimeCharacters[iVar2].unitId = snapshot->unitId;
  g_RuntimeCharacters[iVar2].jobId = snapshot->jobId;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x68 = *(undefined4 *)&snapshot->field_0x8;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x14 = *(undefined4 *)&snapshot->field_0xc;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x8 = *(undefined4 *)&snapshot->field_0x10;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0xc = *(undefined4 *)&snapshot->field_0x14;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x1c = *(undefined4 *)&snapshot->field_0x18;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x24 = *(undefined4 *)&snapshot->field_0x1c;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x20 = *(undefined4 *)&snapshot->field_0x20;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x2c = *(undefined4 *)&snapshot->field_0x24;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x28 = *(undefined4 *)&snapshot->field_0x28;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x34 = *(undefined4 *)&snapshot->field_0x2c;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x38 = *(undefined4 *)&snapshot->field_0x30;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x3c = *(undefined4 *)&snapshot->field_0x34;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x40 = *(undefined4 *)&snapshot->field_0x38;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x44 = *(undefined4 *)&snapshot->field_0x3c;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x48 = *(undefined4 *)&snapshot->field_0x40;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x4c = *(undefined4 *)&snapshot->field_0x44;
  g_RuntimeCharacters[iVar2].tileX = snapshot->tileX;
  g_RuntimeCharacters[iVar2].tileY = snapshot->tileY;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x5c = *(undefined4 *)&snapshot->field_0x58;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x64 = *(undefined4 *)&snapshot->field_0x60;
  puVar1 = &snapshot->field_0xac;
  snapshot = (NativeUnitInfo32 *)&snapshot->field_0x64;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x30 = *(undefined4 *)puVar1;
  iVar10 = 0;
  do {
    iVar9 = iVar10;
    if (snapshot->unitId == -1) break;
    iVar9 = iVar10 + 1;
    *(int *)(&g_RuntimeCharacters[iVar2].field_0xbc + iVar10 * 4) = snapshot->unitId;
    snapshot = (NativeUnitInfo32 *)&snapshot->jobId;
    iVar10 = iVar9;
  } while (iVar9 < 8);
  snapshot = (NativeUnitInfo32 *)&pNVar8->field_0x84;
  *(int *)&g_RuntimeCharacters[iVar2].field_0xb8 = iVar9;
  iVar10 = 0;
  do {
    iVar9 = iVar10;
    if (snapshot->unitId == -1) break;
    iVar9 = iVar10 + 1;
    *(int *)(&g_RuntimeCharacters[iVar2].field_0xe0 + iVar10 * 4) = snapshot->unitId;
    snapshot = (NativeUnitInfo32 *)&snapshot->jobId;
    iVar10 = iVar9;
  } while (iVar9 < 8);
  *(int *)&g_RuntimeCharacters[iVar2].field_0xdc = iVar9;
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x30 = *(undefined4 *)&pNVar8->field_0xac;
  CString::operator=((CString *)&g_RuntimeCharacters[iVar2].field_0x114,
                     (CString *)&pNVar8->field_0xa4);
  CString::operator=((CString *)&g_RuntimeCharacters[iVar2].field_0x118,
                     (CString *)&pNVar8->field_0xa8);
  if (DAT_1010b3d8 == 0) {
    *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x100 = 0;
    *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x104 = 0;
  }
  *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x10c = *(undefined4 *)&pNVar8->field_0x54;
  iVar9 = 100;
  iVar10 = iVar2 * 0x2e0 + 0x14c;
  do {
    iVar9 = iVar9 + -1;
    *(undefined4 *)((int)&g_RuntimeCharacters->active + iVar10) = 0;
    pNVar7 = g_RuntimeCharacters;
    iVar10 = iVar10 + 4;
  } while (iVar9 != 0);
  (&DAT_100e42a0)[g_RuntimeCharacters[iVar2].tileY + g_RuntimeCharacters[iVar2].tileX * 200] = iVar2
  ;
  *(undefined4 *)&pNVar7[iVar2].field_0x60 = *(undefined4 *)&pNVar8->field_0x5c;
  if ((g_RuntimeCharacters[iVar2].active == 0) ||
     (*(int *)&g_RuntimeCharacters[iVar2].field_0x4 == 0)) {
    g_RuntimeCharacters[iVar2].active = 1;
    *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x4 = 1;
    *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x100 = 0x30;
    g_RuntimeCharacters[iVar2].aiState = 1;
  }
  MagicFlag(iVar2,*(ulong *)&pNVar8->field_0x50);
  if (bVar6) {
    g_RuntimeCharacters[iVar2].tileX = iVar4;
    g_RuntimeCharacters[iVar2].tileY = iVar5;
    MoveChar(iVar2,local_c,local_10);
  }
  if (iVar2 < 400) {
    SetAIArea(g_RuntimeCharacters[iVar2].tileX,g_RuntimeCharacters[iVar2].tileY);
  }
  if (*(int *)&g_RuntimeCharacters[iVar2].field_0x68 < 0) {
    *(int *)&g_RuntimeCharacters[iVar2].field_0x68 = g_RuntimeCharacters[iVar2].jobId;
  }
  loadcharbmp(*(int *)&g_RuntimeCharacters[iVar2].field_0x68,iVar2);
  return iVar2;
}


/* incoming refs
10014178 DATA
Entry Point EXTERNAL
1000ed67 UNCONDITIONAL_CALL
CALLER 1000ea80 SpawnActorFromScriptArguments
  10014160 DATA
  Entry Point EXTERNAL
*/
