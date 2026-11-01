/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x1000b0a0
 * Evidence: ev_bd6a6b819cc44f428c5134c4eee91fd6eeb34b3d736fd80affb99285de667446
 */

/* int __cdecl resetcharacter(struct unitinfo *) */

int __cdecl ApplyUnitSnapshot(unitinfo *param_1)

{
  unitinfo *puVar1;
  int iVar2;
  undefined4 *puVar3;
  undefined4 uVar4;
  undefined4 uVar5;
  bool bVar6;
  int iVar7;
  unitinfo *puVar8;
  int iVar9;
  int iVar10;
  int iVar11;
  int local_10;
  int local_c;
  
                    /* Original export resetcharacter(unitinfo*). Apply the supplied unit snapshot
                       into the runtime character array at unit*0x2e0. Copies unit/job/x/y and other
                       fields; this is not a complete replacement implementation. Called by received
                       records and scripted actor creation. */
  puVar8 = param_1;
  iVar2 = *(int *)param_1;
  bVar6 = false;
  local_c = 0;
  local_10 = 0;
  iVar11 = iVar2 * 0x2e0;
  iVar9 = iVar11 + DAT_1010b3a0;
  if ((*(int *)(iVar11 + DAT_1010b3a0) != 0) && ((*(byte *)(iVar9 + 0x100) & 1) != 0)) {
    local_c = *(int *)(iVar9 + 0x28c);
    local_10 = *(int *)(iVar9 + 0x290);
    bVar6 = true;
  }
  for (puVar3 = *(undefined4 **)(iVar9 + 0x130); puVar3 != (undefined4 *)0x0;
      puVar3 = (undefined4 *)*puVar3) {
  }
  *(undefined4 *)(iVar9 + 0x138) = 0;
  *(undefined4 *)(iVar9 + 0x13c) = 0;
  *(undefined4 *)(iVar9 + 0x134) = 0;
  *(undefined4 *)(iVar9 + 0x130) = 0;
  CPlex::FreeDataChain(*(CPlex **)(iVar9 + 0x140));
  *(undefined4 *)(iVar9 + 0x140) = 0;
  *(undefined4 *)(iVar11 + 0x11c + DAT_1010b3a0) = 0xffffffff;
  *(undefined4 *)(iVar11 + 0x120 + DAT_1010b3a0) = 0xffffffff;
  *(undefined4 *)(iVar11 + DAT_1010b3a0) = 1;
  uVar4 = *(undefined4 *)(iVar11 + 0x70 + DAT_1010b3a0);
  uVar5 = *(undefined4 *)(iVar11 + 0x74 + DAT_1010b3a0);
  *(int *)(iVar11 + 100 + DAT_1010b3a0) = iVar2 / 0x32;
  *(undefined4 *)(iVar11 + 0x10 + DAT_1010b3a0) = *(undefined4 *)param_1;
  *(undefined4 *)(iVar11 + 0x18 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 4);
  *(undefined4 *)(iVar11 + 0x68 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 8);
  *(undefined4 *)(iVar11 + 0x14 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0xc);
  *(undefined4 *)(iVar11 + 8 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x10);
  *(undefined4 *)(iVar11 + 0xc + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x14);
  *(undefined4 *)(iVar11 + 0x1c + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x18);
  *(undefined4 *)(iVar11 + 0x24 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x1c);
  *(undefined4 *)(iVar11 + 0x20 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x20);
  *(undefined4 *)(iVar11 + 0x2c + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x24);
  *(undefined4 *)(iVar11 + 0x28 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x28);
  *(undefined4 *)(iVar11 + 0x34 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x2c);
  *(undefined4 *)(iVar11 + 0x38 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x30);
  *(undefined4 *)(iVar11 + 0x3c + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x34);
  *(undefined4 *)(iVar11 + 0x40 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x38);
  *(undefined4 *)(iVar11 + 0x44 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x3c);
  *(undefined4 *)(iVar11 + 0x48 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x40);
  *(undefined4 *)(iVar11 + 0x4c + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x44);
  *(undefined4 *)(iVar11 + 0x70 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x48);
  *(undefined4 *)(iVar11 + 0x74 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x4c);
  *(undefined4 *)(iVar11 + 0x5c + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x58);
  *(undefined4 *)(iVar11 + 100 + DAT_1010b3a0) = *(undefined4 *)(param_1 + 0x60);
  puVar1 = param_1 + 0xac;
  param_1 = param_1 + 100;
  *(undefined4 *)(iVar11 + 0x30 + DAT_1010b3a0) = *(undefined4 *)puVar1;
  iVar9 = 0;
  do {
    if (*(int *)param_1 == -1) break;
    iVar10 = iVar9 + iVar2 * 0xb8;
    iVar9 = iVar9 + 1;
    *(int *)(DAT_1010b3a0 + 0xbc + iVar10 * 4) = *(int *)param_1;
    param_1 = param_1 + 4;
  } while (iVar9 < 8);
  param_1 = puVar8 + 0x84;
  *(int *)(iVar11 + 0xb8 + DAT_1010b3a0) = iVar9;
  iVar9 = 0;
  do {
    if (*(int *)param_1 == -1) break;
    iVar10 = iVar9 + iVar2 * 0xb8;
    iVar9 = iVar9 + 1;
    *(int *)(DAT_1010b3a0 + 0xe0 + iVar10 * 4) = *(int *)param_1;
    param_1 = param_1 + 4;
  } while (iVar9 < 8);
  *(int *)(iVar11 + 0xdc + DAT_1010b3a0) = iVar9;
  *(undefined4 *)(iVar11 + 0x30 + DAT_1010b3a0) = *(undefined4 *)(puVar8 + 0xac);
  CString::operator=((CString *)(iVar11 + 0x114 + DAT_1010b3a0),(CString *)(puVar8 + 0xa4));
  CString::operator=((CString *)(iVar11 + 0x118 + DAT_1010b3a0),(CString *)(puVar8 + 0xa8));
  if (DAT_1010b3d8 == 0) {
    *(undefined4 *)(iVar11 + 0x100 + DAT_1010b3a0) = 0;
    *(undefined4 *)(iVar11 + 0x104 + DAT_1010b3a0) = 0;
  }
  *(undefined4 *)(iVar11 + 0x10c + DAT_1010b3a0) = *(undefined4 *)(puVar8 + 0x54);
  iVar10 = 100;
  iVar9 = iVar11 + 0x14c;
  do {
    iVar10 = iVar10 + -1;
    *(undefined4 *)(iVar9 + DAT_1010b3a0) = 0;
    iVar7 = DAT_1010b3a0;
    iVar9 = iVar9 + 4;
  } while (iVar10 != 0);
  (&DAT_100e42a0)
  [*(int *)(iVar11 + 0x74 + DAT_1010b3a0) + *(int *)(iVar11 + 0x70 + DAT_1010b3a0) * 200] = iVar2;
  *(undefined4 *)(iVar11 + 0x60 + iVar7) = *(undefined4 *)(puVar8 + 0x5c);
  if ((*(int *)(iVar11 + DAT_1010b3a0) == 0) || (*(int *)(iVar11 + 4 + DAT_1010b3a0) == 0)) {
    *(undefined4 *)(iVar11 + DAT_1010b3a0) = 1;
    *(undefined4 *)(iVar11 + 4 + DAT_1010b3a0) = 1;
    *(undefined4 *)(iVar11 + 0x100 + DAT_1010b3a0) = 0x30;
    *(undefined4 *)(iVar11 + 0x108 + DAT_1010b3a0) = 1;
  }
  MagicFlag(iVar2,*(ulong *)(puVar8 + 0x50));
  if (bVar6) {
    *(undefined4 *)(iVar11 + 0x70 + DAT_1010b3a0) = uVar4;
    *(undefined4 *)(iVar11 + 0x74 + DAT_1010b3a0) = uVar5;
    MoveChar(iVar2,local_c,local_10);
  }
  if (iVar2 < 400) {
    SetAIArea(*(int *)(iVar11 + 0x70 + DAT_1010b3a0),*(int *)(iVar11 + 0x74 + DAT_1010b3a0));
  }
  if (*(int *)(iVar11 + 0x68 + DAT_1010b3a0) < 0) {
    *(undefined4 *)(iVar11 + 0x68 + DAT_1010b3a0) = *(undefined4 *)(iVar11 + 0x18 + DAT_1010b3a0);
  }
  loadcharbmp(*(int *)(iVar11 + 0x68 + DAT_1010b3a0),iVar2);
  return iVar2;
}

