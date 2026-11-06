
int * __cdecl FUN_10002ee0(int *output,int unit,int centerX,int centerY,int radius)

{
  int iVar1;
  int iVar2;
  int iVar3;
  int *piVar4;
  int iVar5;
  int iVar6;
  int *local_440;
  int local_43c;
  int *local_438;
  int local_434;
  int local_430 [268];
  
  SetArea(centerX,centerY,0x31,&DAT_100b28a0);
  iVar5 = -1;
  iVar3 = 0;
  local_434 = -1;
  if (0 < DAT_100e4298) {
    local_440 = (int *)0x0;
    iVar1 = DAT_100e4298;
    do {
      iVar2 = 0;
      piVar4 = local_440;
      if (0 < DAT_100e429c) {
        do {
          if (*(int *)((int)&DAT_1008b7a0 + (int)piVar4) < 0) {
            *(undefined4 *)((int)&DAT_100b28a0 + (int)piVar4) = 0xffffffff;
          }
          else {
            iVar1 = mainmapgetchar(iVar3,iVar2);
            if ((iVar1 < 0) && (iVar1 = *(int *)((int)&DAT_100b28a0 + (int)piVar4), iVar5 < iVar1))
            {
              iVar5 = iVar1;
              local_434 = iVar1;
            }
          }
          iVar2 = iVar2 + 1;
          piVar4 = (int *)((int)piVar4 + 4);
          iVar1 = DAT_100e4298;
        } while (iVar2 < DAT_100e429c);
      }
      iVar3 = iVar3 + 1;
      local_440 = (int *)((int)local_440 + 800);
    } while (iVar3 < iVar1);
    if (iVar5 != -1) {
      iVar3 = 0;
      iVar5 = 0;
      if (0 < iVar1) {
        local_440 = &DAT_100b28a0;
        iVar2 = DAT_100e429c;
        do {
          iVar6 = 0;
          if (0 < iVar2) {
            piVar4 = local_430 + iVar3 * 2 + 1;
            local_438 = local_440;
            do {
              if ((*local_438 == local_434) && (iVar1 = mainmapgetchar(iVar5,iVar6), iVar1 < 0)) {
                piVar4[-1] = iVar5;
                *piVar4 = iVar6;
                iVar3 = iVar3 + 1;
                piVar4 = piVar4 + 2;
              }
              iVar6 = iVar6 + 1;
              local_438 = local_438 + 1;
              iVar2 = DAT_100e429c;
              iVar1 = DAT_100e4298;
            } while (iVar6 < DAT_100e429c);
          }
          iVar5 = iVar5 + 1;
          local_440 = local_440 + 200;
        } while (iVar5 < iVar1);
        if (iVar3 != 0) {
          iVar5 = 99;
          if (iVar3 < 1) {
            *output = (int)local_440;
            output[1] = local_43c;
            return output;
          }
          piVar4 = local_430;
          do {
            if ((int)(&DAT_1008b7a0)[piVar4[1] + *piVar4 * 200] < iVar5) {
              local_440 = (int *)*piVar4;
              iVar5 = (&DAT_1008b7a0)[piVar4[1] + *piVar4 * 200];
              local_43c = piVar4[1];
            }
            piVar4 = piVar4 + 2;
            iVar3 = iVar3 + -1;
          } while (iVar3 != 0);
          *output = (int)local_440;
          output[1] = local_43c;
          return output;
        }
      }
      *output = -1;
      output[1] = -1;
      return output;
    }
  }
  *output = -1;
  output[1] = -1;
  return output;
}


/* incoming refs
1000351d UNCONDITIONAL_CALL
10003587 UNCONDITIONAL_CALL
10003364 UNCONDITIONAL_CALL
CALLER 10003480 GetMovePos
  1001408c DATA
  Entry Point EXTERNAL
  10003725 UNCONDITIONAL_CALL
  10003771 UNCONDITIONAL_CALL
  100037b3 UNCONDITIONAL_CALL
  100037f0 UNCONDITIONAL_CALL
  100048b8 UNCONDITIONAL_CALL
CALLER 100030b0 MoveAttack
  100140d4 DATA
  Entry Point EXTERNAL
  100035a5 UNCONDITIONAL_CALL
*/
