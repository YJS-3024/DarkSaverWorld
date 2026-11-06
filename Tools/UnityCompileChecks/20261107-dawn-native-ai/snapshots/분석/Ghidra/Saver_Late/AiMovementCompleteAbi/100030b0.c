
/* class CPoint __cdecl MoveAttack(int,int,int,int) */

int * __cdecl MoveAttack(int *output,int unit,int centerX,int centerY,int radius)

{
  uint uVar1;
  uint uVar2;
  int iVar3;
  int iVar4;
  int *piVar5;
  int iVar6;
  uint uVar7;
  uint uVar8;
  int iVar9;
  int iVar10;
  int *local_450;
  int *local_44c;
  int local_448;
  int local_444;
  int local_440;
  int local_43c;
  undefined2 local_438 [4];
  int local_430 [268];
  
                    /* 0x30b0  60  ?MoveAttack@@YA?AVCPoint@@HHHH@Z */
  local_440 = -1;
  local_43c = -1;
  uVar1 = g_RuntimeCharacters[unit].tileX - centerX;
  uVar7 = (int)uVar1 >> 0x1f;
  uVar2 = g_RuntimeCharacters[unit].tileY - centerY;
  uVar8 = (int)uVar2 >> 0x1f;
  if ((int)(((uVar1 ^ uVar7) - uVar7) + ((uVar2 ^ uVar8) - uVar8)) <= radius) {
    iVar9 = *(int *)&g_RuntimeCharacters[unit].field_0x30;
    if (*(int *)&g_RuntimeCharacters[unit].field_0x1c == 5) {
      iVar3 = FUN_10003860(local_438,unit,&local_448,iVar9);
    }
    else {
      iVar3 = FUN_10003c00(local_438,unit,&local_448,iVar9);
    }
    if ((iVar3 != 0) && (-1 < local_448)) {
      *output = -1;
      output[1] = -1;
      return output;
    }
    ClearTa(&DAT_100b28a0);
    ClearTa(&DAT_100646a0);
    SetArea(centerX,centerY,iVar9 + 1 + radius,&DAT_100b28a0);
    iVar9 = 0;
    if (0 < DAT_100e4298) {
      local_450 = &DAT_100b28a0;
      do {
        iVar3 = 0;
        piVar5 = local_450;
        if (0 < DAT_100e429c) {
          do {
            if (((-1 < *piVar5) && (iVar4 = mainmapgetchar(iVar9,iVar3), -1 < iVar4)) &&
               (iVar4 = ally3(iVar4,unit), iVar4 == 0)) {
              iVar4 = radius;
              if (0xb < radius) {
                iVar4 = 0xc;
              }
              SetArea(iVar9,iVar3,iVar4,&DAT_100646a0);
            }
            iVar3 = iVar3 + 1;
            piVar5 = piVar5 + 1;
          } while (iVar3 < DAT_100e429c);
        }
        iVar9 = iVar9 + 1;
        local_450 = local_450 + 200;
      } while (iVar9 < DAT_100e4298);
    }
    ClearTa(&DAT_100b28a0);
    SetArea(centerX,centerY,radius + -1,&DAT_100b28a0);
    iVar9 = 0;
    local_444 = -1;
    if (0 < DAT_100e4298) {
      iVar4 = 0;
      iVar3 = DAT_100e4298;
      do {
        iVar10 = 0;
        iVar6 = iVar4;
        if (0 < DAT_100e429c) {
          do {
            if ((*(int *)((int)&DAT_1008b7a0 + iVar6) < 0) ||
               (*(int *)((int)&DAT_100b28a0 + iVar6) < 0)) {
              *(undefined4 *)((int)&DAT_100646a0 + iVar6) = 0xffffffff;
            }
            else if (((-1 < *(int *)((int)&DAT_100646a0 + iVar6)) &&
                     ((iVar3 = mainmapgetchar(iVar9,iVar10), iVar3 < 0 ||
                      (iVar3 = mainmapgetchar(iVar9,iVar10), iVar3 == unit)))) &&
                    (local_444 < *(int *)((int)&DAT_100646a0 + iVar6))) {
              local_444 = *(int *)((int)&DAT_100646a0 + iVar6);
            }
            iVar10 = iVar10 + 1;
            iVar3 = DAT_100e4298;
            iVar6 = iVar6 + 4;
          } while (iVar10 < DAT_100e429c);
        }
        iVar9 = iVar9 + 1;
        iVar4 = iVar4 + 800;
      } while (iVar9 < iVar3);
      if (local_444 != -1) {
        iVar4 = 0;
        iVar9 = 0;
        if (0 < iVar3) {
          local_450 = &DAT_100646a0;
          iVar6 = DAT_100e429c;
          do {
            iVar10 = 0;
            if (0 < iVar6) {
              piVar5 = local_430 + iVar4 * 2 + 1;
              local_44c = local_450;
              do {
                if ((*local_44c == local_444) && (iVar3 = mainmapgetchar(iVar9,iVar10), iVar3 < 0))
                {
                  piVar5[-1] = iVar9;
                  *piVar5 = iVar10;
                  iVar4 = iVar4 + 1;
                  piVar5 = piVar5 + 2;
                }
                iVar10 = iVar10 + 1;
                local_44c = local_44c + 1;
                iVar6 = DAT_100e429c;
                iVar3 = DAT_100e4298;
              } while (iVar10 < DAT_100e429c);
            }
            iVar9 = iVar9 + 1;
            local_450 = local_450 + 200;
          } while (iVar9 < iVar3);
          if (iVar4 != 0) {
            iVar9 = 99;
            if (0 < iVar4) {
              piVar5 = local_430;
              do {
                if ((int)(&DAT_1008b7a0)[piVar5[1] + *piVar5 * 200] < iVar9) {
                  local_440 = *piVar5;
                  iVar9 = (&DAT_1008b7a0)[piVar5[1] + *piVar5 * 200];
                  local_43c = piVar5[1];
                }
                piVar5 = piVar5 + 2;
                iVar4 = iVar4 + -1;
              } while (iVar4 != 0);
            }
            *output = local_440;
            output[1] = local_43c;
            return output;
          }
        }
        *output = -1;
        output[1] = -1;
        return output;
      }
    }
    if (*(int *)&g_RuntimeCharacters[unit].field_0x20 <
        *(int *)&g_RuntimeCharacters[unit].field_0x24) {
      *(undefined4 *)&g_RuntimeCharacters[unit].field_0x1fc = 99;
    }
  }
  piVar5 = FUN_10002ee0(&local_440,unit,centerX,centerY,radius);
  *output = *piVar5;
  output[1] = piVar5[1];
  return output;
}


/* incoming refs
100140d4 DATA
Entry Point EXTERNAL
100035a5 UNCONDITIONAL_CALL
CALLER 10003480 GetMovePos
  1001408c DATA
  Entry Point EXTERNAL
  10003725 UNCONDITIONAL_CALL
  10003771 UNCONDITIONAL_CALL
  100037b3 UNCONDITIONAL_CALL
  100037f0 UNCONDITIONAL_CALL
  100048b8 UNCONDITIONAL_CALL
*/
