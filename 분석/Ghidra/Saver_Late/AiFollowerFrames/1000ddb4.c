
undefined4 __cdecl
FUN_1000dd30(undefined2 *param_1,int param_2,uint param_3,int param_4,int param_5,uint param_6,
            int param_7,int *param_8)

{
  undefined4 *puVar1;
  CPlex *this;
  int iVar2;
  NativeCharacter32 *pNVar3;
  int iVar4;
  int iVar5;
  uint uVar6;
  undefined2 *puVar7;
  int iVar8;
  int iVar9;
  undefined2 uVar10;
  int iVar11;
  int iVar12;
  undefined2 *puVar13;
  undefined4 uVar14;
  int local_30;
  int local_2c;
  int local_24;
  int local_20;
  int local_1c;
  CString local_14 [4];
  int local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10011e48;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_14);
  *param_1 = (short)param_4;
  pNVar3 = g_RuntimeCharacters;
  local_4 = 0;
  for (puVar1 = *(undefined4 **)&g_RuntimeCharacters[param_4].field_0x130;
      puVar1 != (undefined4 *)0x0; puVar1 = (undefined4 *)*puVar1) {
  }
  this = *(CPlex **)&g_RuntimeCharacters[param_4].field_0x140;
  *(undefined4 *)&g_RuntimeCharacters[param_4].field_0x138 = 0;
  *(undefined4 *)&pNVar3[param_4].field_0x13c = 0;
  *(undefined4 *)&pNVar3[param_4].field_0x134 = 0;
  *(undefined4 *)&pNVar3[param_4].field_0x130 = 0;
  CPlex::FreeDataChain(this);
  *(undefined4 *)&pNVar3[param_4].field_0x140 = 0;
  *(undefined4 *)&g_RuntimeCharacters[param_4].field_0x11c = 0xffffffff;
  FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,9);
  if ((((param_2 < 0) || ((int)param_3 < 0)) || (DAT_100e4298 <= param_2)) ||
     ((DAT_100e429c <= (int)param_3 || (*(int *)(param_7 + (param_3 + param_2 * 200) * 4) < 0)))) {
    local_4 = 0xffffffff;
    CString::~CString(local_14);
    ExceptionList = local_c;
    return 0;
  }
  iVar12 = 1;
  local_2c = param_3 + 1;
  param_1[1] = (short)param_2;
  local_30 = param_2 + 1;
  param_1[2] = (short)param_3;
  puVar7 = param_1 + 4;
  puVar13 = param_1 + 3;
  iVar11 = param_2;
  param_2 = param_2 * 200;
  do {
    local_10 = param_2 + param_3;
    iVar2 = *(int *)(param_7 + local_10 * 4);
    if (iVar11 < 1) {
      local_1c = -1;
    }
    else {
      local_1c = *(int *)(param_7 + -800 + local_10 * 4);
    }
    if (local_30 < DAT_100e4298) {
      local_20 = *(int *)(param_7 + 800 + (param_2 + param_3) * 4);
    }
    else {
      local_20 = -1;
    }
    if ((int)param_3 < 1) {
      local_24 = -1;
    }
    else {
      local_24 = *(int *)(param_7 + -4 + local_10 * 4);
    }
    if (local_2c < DAT_100e429c) {
      iVar8 = *(int *)(param_7 + 4 + local_10 * 4);
    }
    else {
      iVar8 = -1;
    }
    iVar4 = iVar2;
    if (iVar2 < iVar8) {
      iVar4 = iVar8;
    }
    iVar5 = local_24;
    if ((local_24 <= iVar4) && (iVar5 = iVar2, iVar2 < iVar8)) {
      iVar5 = iVar8;
    }
    iVar4 = local_20;
    if (local_20 <= iVar5) {
      iVar5 = iVar2;
      if (iVar2 < iVar8) {
        iVar5 = iVar8;
      }
      iVar4 = local_24;
      if ((local_24 <= iVar5) && (iVar4 = iVar2, iVar2 < iVar8)) {
        iVar4 = iVar8;
      }
    }
    iVar5 = local_1c;
    if (local_1c <= iVar4) {
      iVar4 = iVar2;
      if (iVar2 < iVar8) {
        iVar4 = iVar8;
      }
      iVar9 = local_24;
      if ((local_24 <= iVar4) && (iVar9 = iVar2, iVar2 < iVar8)) {
        iVar9 = iVar8;
      }
      iVar5 = local_20;
      if (local_20 <= iVar9) {
        iVar4 = iVar2;
        if (iVar2 < iVar8) {
          iVar4 = iVar8;
        }
        iVar5 = local_24;
        if ((local_24 <= iVar4) && (iVar5 = iVar2, iVar2 < iVar8)) {
          iVar5 = iVar8;
        }
      }
    }
    *(undefined4 *)(param_7 + local_10 * 4) = 0xffffffc0;
    uVar10 = (undefined2)iVar11;
    if (DAT_10016170 == 0) {
      if (iVar5 == local_1c) {
        FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,2);
        uVar6 = param_3 & 0xffff;
        iVar11 = iVar11 + -1;
        local_30 = local_30 + -1;
        param_2 = param_2 + -200;
        *puVar13 = (short)iVar11;
LAB_1000e1e1:
        *puVar7 = (short)uVar6;
        goto joined_r0x1000e1ee;
      }
      if (iVar5 == local_20) {
        FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,1);
        iVar11 = iVar11 + 1;
        local_30 = local_30 + 1;
        param_2 = param_2 + 200;
        *puVar13 = (short)iVar11;
        *puVar7 = (undefined2)param_3;
        goto joined_r0x1000e1ee;
      }
      if (iVar5 == local_24) {
        FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,4);
        param_3 = param_3 - 1;
        local_2c = local_2c + -1;
        *puVar13 = uVar10;
        *puVar7 = (short)param_3;
        iVar12 = iVar12 + 1;
        puVar13 = puVar13 + 2;
        puVar7 = puVar7 + 2;
        if (iVar12 < 5) goto LAB_1000e220;
        goto LAB_1000e286;
      }
      if (iVar5 == iVar8) {
        FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,3);
        uVar6 = param_3 + 1;
        local_2c = local_2c + 1;
        *puVar13 = uVar10;
        param_3 = uVar6;
        goto LAB_1000e1e1;
      }
      if (((iVar5 == iVar2) && (iVar11 == param_5)) && (param_3 == param_6)) {
        uVar14 = 1;
        if (iVar12 < 5) {
          iVar11 = 5 - iVar12;
          puVar7 = param_1 + iVar12 * 2 + 2;
          do {
            puVar7[-1] = 0xffff;
            *puVar7 = 0xffff;
            puVar7 = puVar7 + 2;
            iVar11 = iVar11 + -1;
          } while (iVar11 != 0);
        }
        goto LAB_1000e2ba;
      }
    }
    else {
      if (iVar5 == local_24) {
        FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,4);
        uVar6 = param_3 - 1;
        local_2c = local_2c + -1;
        *puVar13 = uVar10;
        param_3 = uVar6;
      }
      else if (iVar5 == iVar8) {
        FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,3);
        uVar6 = param_3 + 1;
        local_2c = local_2c + 1;
        *puVar13 = uVar10;
        param_3 = uVar6;
      }
      else {
        if (iVar5 == local_1c) {
          FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,2);
          iVar11 = iVar11 + -1;
          local_30 = local_30 + -1;
          param_2 = param_2 + -200;
          *puVar13 = (short)iVar11;
          *puVar7 = (undefined2)param_3;
          goto joined_r0x1000e1ee;
        }
        if (iVar5 != local_20) {
          if (((iVar5 != iVar2) || (iVar11 != param_5)) || (param_3 != param_6)) goto LAB_1000e220;
          uVar14 = 1;
          if (iVar12 < 5) {
            puVar7 = param_1 + iVar12 * 2 + 2;
            iVar12 = 5 - iVar12;
            do {
              puVar7[-1] = 0xffff;
              *puVar7 = 0xffff;
              puVar7 = puVar7 + 2;
              iVar12 = iVar12 + -1;
            } while (iVar12 != 0);
          }
          goto LAB_1000e2ba;
        }
        FUN_1000f810((int)&g_RuntimeCharacters[param_4].field_0x12c,1);
        uVar6 = param_3 & 0xffff;
        iVar11 = iVar11 + 1;
        local_30 = local_30 + 1;
        param_2 = param_2 + 200;
        *puVar13 = (short)iVar11;
      }
      *puVar7 = (short)uVar6;
joined_r0x1000e1ee:
      puVar7 = puVar7 + 2;
      puVar13 = puVar13 + 2;
      iVar12 = iVar12 + 1;
      if (4 < iVar12) {
LAB_1000e286:
        uVar14 = 1;
        goto LAB_1000e2ba;
      }
    }
LAB_1000e220:
    DAT_10016170 = (uint)(DAT_10016170 == 0);
    *param_8 = *param_8 + 1;
    if (((param_2 < 0) || ((int)param_3 < 0)) ||
       ((DAT_100e4298 <= iVar11 || ((DAT_100e429c <= (int)param_3 || (iVar2 < 0)))))) {
      uVar14 = 0;
LAB_1000e2ba:
      local_4 = 0xffffffff;
      CString::~CString(local_14);
      ExceptionList = local_c;
      return uVar14;
    }
  } while( true );
}


/* incoming refs
1000e3cb UNCONDITIONAL_CALL
CALLER 1000e310 MoveCharPath
  100140dc DATA
  Entry Point EXTERNAL
  10003822 UNCONDITIONAL_CALL
  100048f8 UNCONDITIONAL_CALL
*/
