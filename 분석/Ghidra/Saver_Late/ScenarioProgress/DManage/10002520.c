
/* public: static int __cdecl CAlphaProcess::AlphaBlit(class CDDSurface *,class CPoint,unsigned
   long,class CAlphaMask *) */

int __cdecl
CAlphaProcess::AlphaBlit
          (CDDSurface *param_1,uint param_2,undefined4 param_3,ushort *param_4,int *param_5)

{
  ushort uVar1;
  int iVar2;
  int iVar3;
  int *piVar4;
  uint uVar5;
  void *pvVar6;
  uint uVar7;
  byte *pbVar8;
  uint uVar9;
  ushort *puVar10;
  ushort uVar11;
  ushort *puVar12;
  uint local_54;
  uint local_50;
  int local_4c;
  ushort *local_48;
  ushort *local_44;
  byte local_40;
  undefined3 uStack_3f;
  uint local_3c;
  undefined4 local_38;
  uint local_34;
  int local_30;
  int local_2c;
  int local_28;
  int local_24;
  int local_20;
  int local_14;
  undefined1 local_10 [16];
  
                    /* 0x2520  50
                       ?AlphaBlit@CAlphaProcess@@SAHPAVCDDSurface@@VCPoint@@KPAVCAlphaMask@@@Z */
  local_30 = 0;
  local_2c = 0;
  local_28 = param_5[1];
  local_24 = param_5[2];
  piVar4 = (int *)GetAdjustedRect(local_10,param_1,param_2,param_3,&local_30);
  local_20 = *piVar4;
  iVar2 = piVar4[1];
  iVar3 = piVar4[2];
  local_14 = piVar4[3];
  uVar5 = DDColorMatch(*(int **)(param_1 + 0x84),(COLORREF)param_4);
  if (uVar5 != 0) {
    pvVar6 = CDDSurface::Lock(param_1,(CRect *)0x0,1);
    if (pvVar6 == (void *)0x0) {
      return 0;
    }
    CDDSurface::GetRGBMasks(param_1,&local_3c,&param_2,&local_54);
    uVar9 = iVar3 - local_20;
    local_4c = local_14 - iVar2;
    local_34 = uVar9;
    local_48 = operator_new(uVar9 * 2);
    param_4 = (ushort *)(iVar2 * *(int *)(param_1 + 0x18) + *(int *)(param_1 + 0x2c) + local_20 * 2)
    ;
    uVar11 = *(short *)(param_1 + 0x18) + (short)uVar9 * -2;
    local_38 = CONCAT22((short)((uint)param_1 >> 0x10),uVar11);
    pbVar8 = (byte *)(param_5[3] * local_2c + *param_5 + local_30);
    do {
      puVar10 = param_4;
      puVar12 = local_48;
      for (uVar7 = (uVar9 & 0x7fffffff) >> 1; uVar7 != 0; uVar7 = uVar7 - 1) {
        *(undefined4 *)puVar12 = *(undefined4 *)puVar10;
        puVar10 = puVar10 + 2;
        puVar12 = puVar12 + 2;
      }
      for (uVar7 = uVar9 * 2 & 3; local_44 = local_48, local_50 = uVar9, uVar7 != 0;
          uVar7 = uVar7 - 1) {
        *(char *)puVar12 = (char)*puVar10;
        puVar10 = (ushort *)((int)puVar10 + 1);
        puVar12 = (ushort *)((int)puVar12 + 1);
      }
      do {
        uVar1 = *local_44;
        uVar7 = (uint)uVar1;
        local_44 = local_44 + 1;
        _local_40 = CONCAT31(uStack_3f,*pbVar8);
        uVar9 = (uint)*pbVar8;
        pbVar8 = pbVar8 + 1;
        *param_4 = (short)(((local_3c & uVar5) - (uVar7 & local_3c)) * uVar9 >> 8) +
                   ((ushort)local_3c & uVar1) & (ushort)local_3c |
                   (short)(((param_2 & uVar5) - (uVar7 & param_2)) * uVar9 >> 8) +
                   ((ushort)param_2 & uVar1) & (ushort)param_2 |
                   (short)(((local_54 & uVar5) - (uVar7 & local_54)) * uVar9 >> 8) +
                   ((ushort)local_54 & uVar1) & (ushort)local_54;
        param_4 = param_4 + 1;
        local_50 = local_50 - 1;
      } while (local_50 != 0);
      param_4 = (ushort *)((int)param_4 + (uint)uVar11);
      pbVar8 = pbVar8 + (param_5[3] - local_34);
      local_4c = local_4c + -1;
      uVar9 = local_34;
    } while (local_4c != 0);
    CDDSurface::Unlock(param_1,(CRect *)0x0);
    operator_delete(local_48);
  }
  return 1;
}

