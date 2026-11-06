
void __cdecl FUN_1000ddc0(int param_1)

{
  char cVar1;
  short sVar2;
  short sVar3;
  int iVar4;
  short *psVar5;
  char *pcVar6;
  int iVar7;
  undefined4 *puVar8;
  int iVar9;
  int local_4;
  
  iVar4 = param_1;
  local_4 = 0;
  psVar5 = (short *)FUN_1000c540(param_1,&local_4);
  sVar2 = *psVar5;
  if (0 < sVar2) {
    puVar8 = &DAT_1006a8a0;
    do {
      iVar9 = 10;
      do {
        *puVar8 = 0;
        puVar8 = puVar8 + 3;
        iVar9 = iVar9 + -1;
      } while (iVar9 != 0);
    } while ((int)puVar8 < 0x1006ad50);
    param_1 = (int)sVar2;
    if (0 < param_1) {
      do {
        psVar5 = (short *)FUN_1000c540(iVar4,&local_4);
        sVar2 = *psVar5;
        psVar5 = (short *)FUN_1000c540(iVar4,&local_4);
        sVar3 = *psVar5;
        pcVar6 = (char *)FUN_1000c560(iVar4,&local_4);
        cVar1 = *pcVar6;
        iVar9 = (int)sVar2 % 10 + (((int)sVar2 / 10) % 5) * 10;
        iVar7 = iVar9 * 0xc;
        *(int *)(&DAT_1006a89c + iVar7) = (int)sVar3;
        (&DAT_1006a8a0)[iVar9 * 3] = 1;
        if (cVar1 == '\x01') {
          *(undefined4 *)(&DAT_1006a8a4 + iVar7) = 0;
        }
        else {
          *(undefined4 *)(&DAT_1006a8a4 + iVar7) = 1;
        }
        param_1 = param_1 + -1;
      } while (param_1 != 0);
    }
  }
  return;
}


/* incoming refs
1000eca3 UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
