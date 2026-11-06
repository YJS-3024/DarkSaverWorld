
/* int __cdecl CheckMagicTarget(struct _PGSMAGIC &,int,int,int,int) */

int __cdecl CheckMagicTarget(_PGSMAGIC *param_1,int param_2,int param_3,int param_4,int param_5)

{
  undefined2 uVar1;
  int iVar2;
  bool bVar3;
  magicrecord *pmVar4;
  int iVar5;
  void *pvVar6;
  int iVar7;
  int iVar8;
  int iVar9;
  int iVar10;
  NativeCharacter32 *pNVar11;
  int *piVar12;
  undefined2 local_8;
  undefined2 local_4;
  
  iVar7 = param_5;
                    /* 0x1d60  10  ?CheckMagicTarget@@YAHAAU_PGSMAGIC@@HHHH@Z */
  iVar5 = param_3;
  pmVar4 = GetMagicTbl(param_2);
  if (*(int *)(pmVar4 + 0x14) == 8) {
    iVar5 = mainmapgetchar(param_4,param_5);
    if (-1 < iVar5) {
      uVar1 = *(undefined2 *)pmVar4;
      *(undefined2 *)(param_1 + 2) = (undefined2)param_3;
      *(undefined2 *)(param_1 + 4) = 0xffff;
      *(undefined2 *)(param_1 + 6) = 0xffff;
      *(undefined2 *)param_1 = uVar1;
      pvVar6 = operator_new(0x12);
      *(void **)(param_1 + 0x10) = pvVar6;
      param_1[0xc] = (_PGSMAGIC)0x0;
      iVar10 = 0;
      iVar7 = (iVar5 / 10) * 0x1cc0;
      pNVar11 = g_RuntimeCharacters;
      do {
        if ((*(int *)((int)&pNVar11->active + iVar7) != 0) &&
           (*(int *)(&pNVar11->field_0x4 + iVar7) != 0)) {
          *(short *)(*(int *)(param_1 + 0x10) + (uint)(byte)param_1[0xc] * 2) =
               (short)iVar10 + (short)(iVar5 / 10) * 10;
          param_1[0xc] = (_PGSMAGIC)((char)param_1[0xc] + '\x01');
          pNVar11 = g_RuntimeCharacters;
        }
        iVar10 = iVar10 + 1;
        iVar7 = iVar7 + 0x2e0;
      } while (iVar10 < 9);
      if (param_1[0xc] == (_PGSMAGIC)0x0) {
        operator_delete(*(void **)(param_1 + 0x10));
        *(undefined4 *)(param_1 + 0x10) = 0;
      }
      return 1;
    }
  }
  else {
    ClearTa(&DAT_100162a0);
    iVar10 = *(int *)(pmVar4 + 8);
    bVar3 = false;
    if (((iVar10 == -1) || (iVar10 == -3)) || (iVar10 == -4)) {
      if (param_4 == -4) {
        iVar8 = -4;
      }
      else if (param_4 == -3) {
        iVar8 = -3;
      }
      else {
        iVar8 = (param_4 != -2) - 2;
      }
      iVar9 = g_RuntimeCharacters[param_3].tileX;
      iVar2 = g_RuntimeCharacters[param_3].tileY;
      if (iVar10 == -1) {
        setarea5(iVar9,iVar2,*(int *)(pmVar4 + 0xc),&DAT_100162a0,iVar8);
      }
      if (*(int *)(pmVar4 + 8) == -3) {
        setarea7(iVar9,iVar2,*(int *)(pmVar4 + 0xc),&DAT_100162a0,iVar8);
      }
      if (*(int *)(pmVar4 + 8) == -4) {
        setareaa(iVar9,iVar2,*(int *)(pmVar4 + 0xc),&DAT_100162a0,iVar8);
      }
      bVar3 = true;
    }
    else {
      iVar9 = param_5;
      iVar8 = param_4;
      if (iVar10 < 1) {
        iVar8 = g_RuntimeCharacters[param_3].tileX;
        iVar9 = g_RuntimeCharacters[param_3].tileY;
      }
      if (iVar10 == -6) {
        setareac(iVar8,iVar9,param_4,param_5,&DAT_100162a0);
      }
      else if (iVar10 == -5) {
        setarea8(iVar8,iVar9,*(int *)(pmVar4 + 0xc),&DAT_100162a0);
      }
      else if (iVar10 == -2) {
        setarea6(iVar8,iVar9,*(int *)(pmVar4 + 0xc),&DAT_100162a0);
      }
      else {
        SetArea(iVar8,iVar9,*(uint *)(pmVar4 + 0xc) & ((int)*(uint *)(pmVar4 + 0xc) < 1) - 1,
                &DAT_100162a0);
      }
    }
    param_3 = 0;
    if (0 < DAT_100e4298) {
      param_5 = (int)&DAT_100162a0;
      do {
        iVar10 = 0;
        piVar12 = (int *)param_5;
        if (0 < DAT_100e429c) {
          do {
            if (0 < *piVar12) {
              iVar8 = mainmapgetchar(param_3,iVar10);
              *piVar12 = -1;
              if (-1 < iVar8) {
                if (iVar5 == -3) {
                  iVar5 = 700;
LAB_1000204c:
                  if ((((byte)pmVar4[0x10] & 1) != 0) && (iVar9 = ally3(iVar5,iVar8), iVar9 != 0)) {
                    *piVar12 = 1;
                    bVar3 = true;
                  }
                  if ((((byte)pmVar4[0x10] & 2) == 0) || (iVar8 = ally4(iVar5,iVar8), iVar8 == 0))
                  goto LAB_10002098;
                }
                else {
                  if (iVar5 == -2) {
                    iVar5 = DAT_1003d540 * 0x32;
                  }
                  if (iVar5 != -1) goto LAB_1000204c;
                }
                *piVar12 = 1;
                bVar3 = true;
              }
            }
LAB_10002098:
            iVar10 = iVar10 + 1;
            piVar12 = piVar12 + 1;
          } while (iVar10 < DAT_100e429c);
        }
        param_3 = param_3 + 1;
        param_5 = param_5 + 800;
      } while (param_3 < DAT_100e4298);
    }
    if (bVar3) {
      local_8 = (undefined2)param_4;
      local_4 = (undefined2)iVar7;
      *(undefined2 *)param_1 = *(undefined2 *)pmVar4;
      *(short *)(param_1 + 2) = (short)iVar5;
      *(undefined2 *)(param_1 + 4) = local_8;
      *(undefined2 *)(param_1 + 6) = local_4;
      iVar5 = 0;
      iVar7 = 0;
      param_3 = 0;
      if (0 < DAT_100e4298) {
        param_2 = (int)&DAT_100162a0;
        iVar10 = DAT_100e429c;
        do {
          iVar8 = 0;
          piVar12 = (int *)param_2;
          if (0 < iVar10) {
            do {
              iVar5 = mainmapgetchar(iVar7,iVar8);
              if ((((0 < *piVar12) && (-1 < iVar5)) && (g_RuntimeCharacters[iVar5].active != 0)) &&
                 (*(int *)&g_RuntimeCharacters[iVar5].field_0x4 != 0)) {
                param_3 = param_3 + 1;
              }
              iVar8 = iVar8 + 1;
              piVar12 = piVar12 + 1;
              iVar10 = DAT_100e429c;
              iVar5 = param_3;
            } while (iVar8 < DAT_100e429c);
          }
          iVar7 = iVar7 + 1;
          param_2 = param_2 + 800;
        } while (iVar7 < DAT_100e4298);
        if (iVar5 != 0) {
          param_1[0xc] = SUB41(iVar5,0);
          pvVar6 = operator_new(iVar5 * 2);
          *(void **)(param_1 + 0x10) = pvVar6;
          iVar5 = 0;
          iVar7 = 0;
          if (0 < DAT_100e4298) {
            param_3 = (int)&DAT_100162a0;
            iVar10 = DAT_100e429c;
            do {
              iVar8 = 0;
              piVar12 = (int *)param_3;
              if (0 < iVar10) {
                do {
                  iVar10 = mainmapgetchar(iVar7,iVar8);
                  if (((0 < *piVar12) && (-1 < iVar10)) &&
                     ((g_RuntimeCharacters[iVar10].active != 0 &&
                      (*(int *)&g_RuntimeCharacters[iVar10].field_0x4 != 0)))) {
                    iVar5 = iVar5 + 1;
                    *(short *)(*(int *)(param_1 + 0x10) + -2 + iVar5 * 2) = (short)iVar10;
                  }
                  iVar8 = iVar8 + 1;
                  piVar12 = piVar12 + 1;
                  iVar10 = DAT_100e429c;
                } while (iVar8 < DAT_100e429c);
              }
              iVar7 = iVar7 + 1;
              param_3 = param_3 + 800;
            } while (iVar7 < DAT_100e4298);
          }
          ClearTa(&DAT_100162a0);
          return 1;
        }
      }
    }
    ClearTa(&DAT_100162a0);
  }
  return 0;
}


/* incoming refs
1001400c DATA
Entry Point EXTERNAL
10004619 UNCONDITIONAL_CALL
CALLER 100042e0 CommanderAIAction
  10014014 DATA
  Entry Point EXTERNAL
  100049ed UNCONDITIONAL_CALL
  10004920 UNCONDITIONAL_CALL
*/
