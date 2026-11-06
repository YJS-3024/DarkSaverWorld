
undefined4 __cdecl FUN_100035e0(int *param_1,undefined4 param_2)

{
  uint uVar1;
  uint uVar2;
  int iVar3;
  clock_t cVar4;
  int *piVar5;
  int iVar6;
  uint uVar7;
  uint uVar8;
  int local_28;
  int local_24;
  int local_20 [2];
  _PGSMOVECHARPATH local_18 [24];
  
  iVar6 = (int)param_1 / 10;
  if ((g_RuntimeCharacters[iVar6 * 10].active == 0) ||
     (*(int *)&g_RuntimeCharacters[iVar6 * 10].field_0x4 == 0)) {
    piVar5 = GetMovePos(local_20,(int)param_1,&param_2,g_RuntimeCharacters[(int)param_1].tileX,
                        g_RuntimeCharacters[(int)param_1].tileY,0x14,2);
    local_28 = *piVar5;
    local_24 = piVar5[1];
  }
  else {
    uVar1 = g_RuntimeCharacters[iVar6 * 10].tileY - g_RuntimeCharacters[(int)param_1].tileY;
    uVar7 = (int)uVar1 >> 0x1f;
    uVar2 = g_RuntimeCharacters[iVar6 * 10].tileX - g_RuntimeCharacters[(int)param_1].tileX;
    uVar8 = (int)uVar2 >> 0x1f;
    if ((int)(((uVar1 ^ uVar7) - uVar7) + ((uVar2 ^ uVar8) - uVar8)) <=
        *(int *)&g_RuntimeCharacters[iVar6 * 10].field_0x4c) {
      *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 =
           *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 & 0xffffffdf;
      iVar3 = FUN_100040a0((undefined2 *)&local_28,(int)param_1);
      if (iVar3 == 0) {
        *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 =
             *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 & 0xffffffef;
      }
      else if (((iVar3 != 2) || ((g_RuntimeCharacters[iVar6 * 10].aiState & 8) == 0)) ||
              (cVar4 = clock(),
              cVar4 - *(int *)&g_RuntimeCharacters[(int)param_1].field_0x298 < 3000)) {
        FUN_10003f70((int)param_1);
        return 0;
      }
    }
    cVar4 = clock();
    *(clock_t *)&g_RuntimeCharacters[(int)param_1].field_0x298 = cVar4;
    if ((g_RuntimeCharacters[iVar6 * 10].aiState & 2) != 0) {
      iVar3 = *(int *)&g_RuntimeCharacters[iVar6 * 10].field_0x4c;
      if (iVar3 < 3) {
        iVar3 = 2;
      }
      piVar5 = GetMovePos(local_20,(int)param_1,&param_2,g_RuntimeCharacters[iVar6 * 10].tileX,
                          g_RuntimeCharacters[iVar6 * 10].tileY,iVar3,1);
      local_28 = *piVar5;
      local_24 = piVar5[1];
    }
    if ((g_RuntimeCharacters[iVar6 * 10].aiState & 4) != 0) {
      iVar3 = *(int *)&g_RuntimeCharacters[iVar6 * 10].field_0x4c;
      if (iVar3 < 3) {
        iVar3 = 2;
      }
      piVar5 = GetMovePos(local_20,(int)param_1,&param_2,g_RuntimeCharacters[iVar6 * 10].tileX,
                          g_RuntimeCharacters[iVar6 * 10].tileY,iVar3,2);
      local_28 = *piVar5;
      local_24 = piVar5[1];
    }
    if ((g_RuntimeCharacters[iVar6 * 10].aiState & 8) != 0) {
      iVar3 = *(int *)&g_RuntimeCharacters[iVar6 * 10].field_0x4c;
      if (iVar3 < 3) {
        iVar3 = 2;
      }
      piVar5 = GetMovePos(local_20,(int)param_1,&param_2,g_RuntimeCharacters[iVar6 * 10].tileX,
                          g_RuntimeCharacters[iVar6 * 10].tileY,iVar3,3);
      local_28 = *piVar5;
      local_24 = piVar5[1];
    }
  }
  if (((-1 < local_28) && (-1 < local_24)) &&
     ((g_RuntimeCharacters[(int)param_1].tileX != local_28 ||
      (g_RuntimeCharacters[(int)param_1].tileY != local_24)))) {
    iVar6 = MoveCharPath(local_18,(int)param_1,local_28,local_24);
    if (iVar6 != 0) {
      FUN_10004ff0((undefined2 *)local_18);
    }
    *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 =
         *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 & 0xffffffcf;
  }
  FUN_10003f70((int)param_1);
  return 0xffffffff;
}


/* incoming refs
10004282 UNCONDITIONAL_CALL
CALLER 100041f0 FUN_100041f0
  10002465 UNCONDITIONAL_CALL
*/
