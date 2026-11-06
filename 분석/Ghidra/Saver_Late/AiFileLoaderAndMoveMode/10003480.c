
/* class CPoint __cdecl GetMovePos(int,int &,int,int,int,int) */

int * __cdecl
GetMovePos(int *output,int unit,int *cursor,int centerX,int centerY,int radius,int mode)

{
  int *piVar1;
  undefined1 *puVar2;
  int local_28;
  int local_24;
  undefined4 local_20 [2];
  undefined1 local_18 [8];
  undefined4 local_10 [2];
  undefined1 local_8 [8];
  
                    /* 0x3480  42  ?GetMovePos@@YA?AVCPoint@@HAAHHHHH@Z */
  local_28 = -1;
  local_24 = -1;
  ClearTa(&DAT_100b28a0);
  SetArea(centerX,centerY,radius,&DAT_100b28a0);
  GetAttrTable3(centerX,centerY);
  ClearTa(&DAT_1008b7a0);
  SetArea2(g_RuntimeCharacters[unit].tileX,g_RuntimeCharacters[unit].tileY,
           *(int *)&g_RuntimeCharacters[unit].field_0x3c,unit,
           *(int *)&g_RuntimeCharacters[unit].field_0x1c,&DAT_1008b7a0);
  if (mode == 1) {
    if ((*(int *)&g_RuntimeCharacters[unit].field_0x24 * 0x32) / 100 <=
        *(int *)&g_RuntimeCharacters[unit].field_0x20) {
      puVar2 = local_8;
      goto LAB_100035a4;
    }
    piVar1 = (int *)FUN_10002ee0(local_10,unit,centerX,centerY);
  }
  else {
    if (mode != 2) {
      if (mode == 3) {
        piVar1 = (int *)FUN_10002ee0(local_20,unit,centerX,centerY);
        local_28 = *piVar1;
        local_24 = piVar1[1];
        *cursor = -1;
      }
      goto LAB_100035c4;
    }
    puVar2 = local_18;
LAB_100035a4:
    piVar1 = (int *)MoveAttack((int)puVar2,unit,centerX,centerY);
  }
  local_28 = *piVar1;
  local_24 = piVar1[1];
  *cursor = -1;
LAB_100035c4:
  *output = local_28;
  output[1] = local_24;
  return output;
}


/* incoming refs
1001408c DATA
Entry Point EXTERNAL
10003725 UNCONDITIONAL_CALL
10003771 UNCONDITIONAL_CALL
100037b3 UNCONDITIONAL_CALL
100037f0 UNCONDITIONAL_CALL
100048b8 UNCONDITIONAL_CALL
CALLER 10004810 CommanderAIMove
  10014018 DATA
  Entry Point EXTERNAL
  100049d5 UNCONDITIONAL_CALL
CALLER 100035e0 FUN_100035e0
  10004282 UNCONDITIONAL_CALL
*/
