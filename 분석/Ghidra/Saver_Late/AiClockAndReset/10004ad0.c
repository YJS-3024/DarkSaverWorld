
void __cdecl FUN_10004ad0(int param_1,int param_2,int param_3,int param_4)

{
  int iVar1;
  int iVar2;
  int iVar3;
  int iVar4;
  undefined4 *puVar5;
  int iVar6;
  uint uVar7;
  
  iVar4 = param_3 + 1;
  uVar7 = -iVar4;
  if (-iVar4 == iVar4 || (int)uVar7 < iVar4) {
    iVar6 = uVar7 + param_2;
    do {
      if ((-1 < iVar6) && (iVar6 < DAT_100e429c)) {
        iVar1 = (uVar7 ^ (int)uVar7 >> 0x1f) - ((int)uVar7 >> 0x1f);
        iVar3 = iVar4 - iVar1;
        iVar1 = iVar1 - iVar4;
        if (iVar1 <= iVar3) {
          iVar2 = param_1 + iVar1;
          iVar1 = (iVar3 - iVar1) + 1;
          puVar5 = (undefined4 *)(param_4 + (uVar7 + iVar2 * 200 + param_2) * 4);
          do {
            if ((-1 < iVar2) && (iVar2 < DAT_100e4298)) {
              *puVar5 = 1;
            }
            puVar5 = puVar5 + 200;
            iVar2 = iVar2 + 1;
            iVar1 = iVar1 + -1;
          } while (iVar1 != 0);
        }
      }
      uVar7 = uVar7 + 1;
      iVar6 = iVar6 + 1;
    } while ((int)uVar7 <= iVar4);
  }
  return;
}


/* incoming refs
10004b71 UNCONDITIONAL_CALL
CALLER 10004b60 SetAIArea
  100140ec DATA
  Entry Point EXTERNAL
  1000b49a UNCONDITIONAL_CALL
  1001040f UNCONDITIONAL_CALL
*/
