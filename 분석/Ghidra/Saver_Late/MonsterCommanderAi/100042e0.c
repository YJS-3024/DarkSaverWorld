
/* int __cdecl CommanderAIAction(int,int) */

int __cdecl CommanderAIAction(int param_1,int param_2)

{
  int iVar1;
  magicrecord *pmVar2;
  uint uVar3;
  int iVar4;
  int iVar5;
  NativeCharacter32 *pNVar6;
  uint uVar7;
  int iVar8;
  bool bVar9;
  int local_38;
  int local_34;
  int local_30;
  int *local_2c;
  int local_28;
  int local_24;
  int local_20;
  undefined2 local_1c [4];
  _PGSMAGIC local_14 [16];
  void *local_4;
  
                    /* 0x42e0  12  ?CommanderAIAction@@YAHHH@Z */
  local_38 = 0;
  if (g_RuntimeCharacters[param_1].active == 0) {
    return -1;
  }
  if ((*(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 0x10) == 0) {
    return -1;
  }
  if ((*(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 1) != 0) {
    return -1;
  }
  iVar1 = GetCmderAINum(param_1);
  if (iVar1 < 0) {
    g_RuntimeCharacters[param_1].aiState = 4;
    return -1;
  }
  iVar8 = iVar1 * 0x110;
  g_RuntimeCharacters[param_1].aiState = (&DAT_100d99a4)[iVar1 * 0x44];
  local_20 = rand();
  local_20 = local_20 % 100;
  if ((*(int *)&g_RuntimeCharacters[param_1].field_0x24 * 0x1e) / 100 <=
      *(int *)&g_RuntimeCharacters[param_1].field_0x20) {
    local_30 = 0;
    if (*(int *)(&DAT_100d99bc + iVar8) < 1) {
      return -1;
    }
    local_2c = (int *)(&DAT_100d99c4 + iVar8);
    pNVar6 = g_RuntimeCharacters;
    local_24 = 0;
    do {
      if (pNVar6[param_1].active == 0) {
        return -1;
      }
      if ((pNVar6[param_1].field_0x100 & 0x10) == 0) {
        return -1;
      }
      local_24 = local_24 + local_2c[-1];
      if (local_20 <= local_24) {
        iVar1 = *local_2c;
        if (iVar1 == 0) {
          if (*(int *)&pNVar6[param_1].field_0x20 <
              (*(int *)&pNVar6[param_1].field_0x24 * 100) / 100) goto LAB_1000465d;
          break;
        }
        if (iVar1 == 1) {
          if (*(int *)&pNVar6[param_1].field_0x1c == 5) {
            local_38 = FUN_10003860(local_1c,param_1,&local_34,*(int *)&pNVar6[param_1].field_0x30);
          }
          else {
            local_38 = FUN_10003c00(local_1c,param_1,&local_34,*(int *)&pNVar6[param_1].field_0x30);
          }
          if (local_34 < 0) {
            return -1;
          }
          break;
        }
        if (iVar1 != 2) break;
        iVar1 = local_2c[1];
        local_28 = iVar1;
        if (((-1 < iVar1) && ((pNVar6[param_1].field_0x104 & 0x80) == 0)) &&
           (pmVar2 = GetMagicTbl(iVar1), pNVar6 = g_RuntimeCharacters,
           *(int *)(pmVar2 + 4) <= *(int *)&g_RuntimeCharacters[param_1].field_0x28)) {
          uVar3 = *(uint *)(pmVar2 + 0x10) & 0x80000001;
          bVar9 = uVar3 == 0;
          if ((int)uVar3 < 0) {
            bVar9 = (uVar3 - 1 | 0xfffffffe) == 0xffffffff;
          }
          iVar4 = *(int *)(pmVar2 + 8);
          if (bVar9) {
            if (iVar4 < 4) {
              iVar4 = 3;
            }
            local_38 = FUN_10003860(local_1c,g_RuntimeCharacters[param_1].unitId,&local_34,iVar4);
          }
          else {
            if (iVar4 < 4) {
              iVar4 = 3;
            }
            local_38 = FUN_10003a40(local_1c,g_RuntimeCharacters[param_1].unitId,&local_34,iVar4);
          }
          pNVar6 = g_RuntimeCharacters;
          if (local_34 < 0) {
            local_38 = 0;
          }
          else if (local_38 != 0) {
            iVar8 = *(int *)(pmVar2 + 8);
            iVar4 = g_RuntimeCharacters[local_34].tileY;
            iVar5 = g_RuntimeCharacters[local_34].tileX;
            if (((iVar8 == -1) || (iVar8 == -3)) || (iVar8 == -4)) {
              local_24 = g_RuntimeCharacters[param_1].tileX;
              uVar3 = iVar4 - g_RuntimeCharacters[param_1].tileY;
              uVar7 = (int)uVar3 >> 0x1f;
              local_20 = (uVar3 ^ uVar7) - uVar7;
              uVar3 = iVar5 - local_24 >> 0x1f;
              iVar1 = local_28;
              if (local_20 < (int)((iVar5 - local_24 ^ uVar3) - uVar3)) {
                iVar5 = (local_24 < iVar5) - 2;
              }
              else {
                iVar5 = (iVar4 <= g_RuntimeCharacters[param_1].tileY) - 4;
              }
            }
            if ((0 < iVar4) &&
               (iVar1 = CheckMagicTarget(local_14,iVar1,param_1,iVar5,iVar4), iVar1 != 0)) {
              FUN_10005090((short *)local_14,0);
              if (local_4 != (void *)0x0) {
                operator_delete(local_4);
              }
              *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x60 = 0;
              goto LAB_10004673;
            }
            break;
          }
        }
      }
      local_30 = local_30 + 1;
      local_2c = local_2c + 3;
    } while (local_30 < *(int *)(&DAT_100d99bc + iVar8));
    if (local_38 == 0) {
      return -1;
    }
    if (local_38 == 1) {
      FUN_10005240(local_1c);
      goto LAB_10004663;
    }
  }
LAB_1000465d:
  FUN_100051e0((short)param_1);
LAB_10004663:
  *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x60 = 0;
LAB_10004673:
  *(uint *)&g_RuntimeCharacters[param_1].field_0x100 =
       *(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 0xffffffef;
  return -1;
}


/* incoming refs
10014014 DATA
Entry Point EXTERNAL
100049ed UNCONDITIONAL_CALL
10004920 UNCONDITIONAL_CALL
CALLER 10004810 CommanderAIMove
  10014018 DATA
  Entry Point EXTERNAL
  100049d5 UNCONDITIONAL_CALL
CALLER 10004990 ExecuteCommanderAI
  10014020 DATA
  Entry Point EXTERNAL
  10002457 UNCONDITIONAL_CALL
*/
