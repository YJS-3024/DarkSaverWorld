
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* struct soldierlist const * __cdecl GetSoldierList(int *,int *) */

soldierlist * __cdecl GetSoldierList(int *param_1,int *param_2)

{
  undefined4 uVar1;
  int iVar2;
  int iVar3;
  int *piVar4;
  int *piVar5;
  int *piVar6;
  undefined4 *puVar7;
  int iVar8;
  int iVar9;
  int *local_88;
  int local_84;
  int *local_80;
  int local_7c [11];
  int local_50 [10];
  int local_28 [10];
  
  iVar8 = 0;
  piVar5 = local_28;
                    /* 0x2850  10  ?GetSoldierList@@YAPBUsoldierlist@@PAH0@Z */
  for (iVar3 = 10; iVar3 != 0; iVar3 = iVar3 + -1) {
    *piVar5 = *param_2;
    param_2 = param_2 + 1;
    piVar5 = piVar5 + 1;
  }
  piVar5 = local_28;
  piVar6 = local_50;
  for (iVar3 = 10; iVar3 != 0; iVar3 = iVar3 + -1) {
    *piVar6 = *param_1;
    param_1 = param_1 + 1;
    piVar6 = piVar6 + 1;
  }
  piVar6 = local_7c;
  local_88 = (int *)0x8;
  do {
    iVar3 = *piVar5;
    if ((19999 < iVar3) && (iVar2 = FUN_10002820(iVar3), -1 < iVar2)) {
      iVar2 = 0;
      if (0 < iVar8) {
        piVar4 = local_7c;
        do {
          if (*piVar4 == iVar3) goto LAB_100028c3;
          iVar2 = iVar2 + 1;
          piVar4 = piVar4 + 1;
        } while (iVar2 < iVar8);
      }
      *piVar6 = iVar3;
      iVar8 = iVar8 + 1;
      piVar6 = piVar6 + 1;
    }
LAB_100028c3:
    piVar5 = piVar5 + 1;
    local_88 = (int *)((int)local_88 + -1);
  } while (local_88 != (int *)0x0);
  local_80 = local_7c + iVar8;
  local_88 = local_50;
  local_84 = 7;
  do {
    iVar3 = *local_88;
    if ((((0 < iVar3) && (iVar2 = iVar3 * 0x298, *(int *)(&DAT_1007bde0 + iVar2) == iVar3)) &&
        (*(int *)(&DAT_1007be50 + iVar2) < 0)) &&
       (iVar3 = *(int *)(&DAT_1007be10 + iVar2), 19999 < iVar3)) {
      iVar2 = 0;
      if (0 < iVar8) {
        piVar5 = local_7c;
        do {
          if (*piVar5 / 100 == iVar3 / 100) {
            if (local_7c[iVar2] % 100 < iVar3 % 100) {
              local_7c[iVar2] = iVar3;
            }
            goto LAB_1000296d;
          }
          iVar2 = iVar2 + 1;
          piVar5 = piVar5 + 1;
        } while (iVar2 < iVar8);
      }
      iVar8 = iVar8 + 1;
      *local_80 = iVar3;
      local_80 = local_80 + 1;
    }
LAB_1000296d:
    local_88 = local_88 + 1;
    local_84 = local_84 + -1;
  } while (local_84 != 0);
  local_88 = local_50;
  local_84 = 7;
LAB_10002997:
  iVar3 = *local_88 * 0x298;
  if (((*(int *)(&DAT_1007bde0 + iVar3) == *local_88) &&
      (iVar2 = *(int *)(&DAT_1007be50 + iVar3), iVar2 != 0)) &&
     (19999 < *(int *)(&DAT_1007be10 + iVar3))) {
    if (iVar2 < 0) {
      iVar2 = -iVar2;
    }
    iVar9 = 0;
    if (0 < iVar8) {
      piVar5 = local_7c;
      do {
        if (*piVar5 / 100 == *(int *)(&DAT_1007be10 + iVar3) / 100) {
          piVar5 = local_7c + iVar9;
          iVar3 = 7;
          *piVar5 = local_7c[iVar9] + iVar2;
          goto LAB_10002a59;
        }
        iVar9 = iVar9 + 1;
        piVar5 = piVar5 + 1;
      } while (iVar9 < iVar8);
    }
  }
  goto LAB_10002a6e;
  while( true ) {
    iVar3 = iVar3 + -1;
    *piVar5 = iVar2 + -1;
    if (iVar3 < 0) break;
LAB_10002a59:
    iVar2 = *piVar5;
    iVar9 = FUN_10002820(iVar2);
    if (-1 < iVar9) break;
  }
LAB_10002a6e:
  local_88 = local_88 + 1;
  local_84 = local_84 + -1;
  if (local_84 == 0) {
    iVar3 = 0;
    if (0 < iVar8) {
      local_88 = local_7c;
      local_80 = (int *)iVar8;
      do {
        iVar8 = FUN_10002820(*local_88);
        if (-1 < iVar8) {
          local_84 = (&DAT_10005604)[iVar8 * 0x61];
          if (0 < local_84) {
            puVar7 = &DAT_100052b8 + iVar3 * 2;
            piVar5 = &DAT_10005608 + iVar8 * 0x61;
            do {
              iVar2 = 0;
              if (0x100052b8 < (int)puVar7) {
                piVar6 = &DAT_100052b4;
                do {
                  if (*piVar6 == *piVar5) {
                    if ((int)(&DAT_100052b8)[iVar2 * 2] < *(int *)(&DAT_10005680 + iVar8 * 0x184)) {
                      (&DAT_100052b8)[iVar2 * 2] = *(int *)(&DAT_10005680 + iVar8 * 0x184);
                    }
                    goto LAB_10002b05;
                  }
                  iVar2 = iVar2 + 1;
                  piVar6 = piVar6 + 2;
                } while (iVar2 < iVar3);
              }
              uVar1 = *(undefined4 *)(&DAT_10005680 + iVar8 * 0x184);
              puVar7[-1] = *piVar5;
              *puVar7 = uVar1;
              iVar3 = iVar3 + 1;
              puVar7 = puVar7 + 2;
LAB_10002b05:
              piVar5 = piVar5 + 1;
              local_84 = local_84 + -1;
            } while (local_84 != 0);
          }
        }
        local_88 = local_88 + 1;
        local_80 = (int *)((int)local_80 + -1);
      } while (local_80 != (int *)0x0);
    }
    _DAT_100052b0 = iVar3;
    return (soldierlist *)&DAT_100052b0;
  }
  goto LAB_10002997;
}

