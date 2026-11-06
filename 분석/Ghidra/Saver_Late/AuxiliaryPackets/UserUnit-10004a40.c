
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* void __cdecl SetAllyInfo(struct nallytype,int) */

void __cdecl SetAllyInfo(short param_1)

{
  int iVar1;
  short *psVar2;
  undefined4 *puVar3;
  undefined4 *puVar4;
  undefined4 in_stack_0000005c;
  undefined4 local_58;
  short local_54 [42];
  
  puVar3 = &local_58;
  puVar4 = (undefined4 *)register0x00000010;
                    /* 0x4a40  67  ?SetAllyInfo@@YAXUnallytype@@H@Z */
  for (iVar1 = 0x15; puVar4 = puVar4 + 1, iVar1 != 0; iVar1 = iVar1 + -1) {
    *puVar3 = *puVar4;
    puVar3 = puVar3 + 1;
  }
  *(undefined2 *)puVar3 = *(undefined2 *)puVar4;
  DAT_1003d540 = in_stack_0000005c;
  puVar4 = &DAT_1003d54c;
  for (iVar1 = 0x15; iVar1 != 0; iVar1 = iVar1 + -1) {
    *puVar4 = 1;
    puVar4 = puVar4 + 1;
  }
  if (-1 < param_1) {
    psVar2 = local_54;
    iVar1 = param_1 + 1;
    do {
      _DAT_1003d548 = in_stack_0000005c;
      if (*psVar2 == 1) {
        (&DAT_1003d54c)[psVar2[-1]] = 2;
      }
      else if (*psVar2 == 2) {
        (&DAT_1003d54c)[psVar2[-1]] = 4;
      }
      psVar2 = psVar2 + 2;
      iVar1 = iVar1 + -1;
    } while (iVar1 != 0);
  }
  return;
}


/* incoming refs
100140f0 DATA
Entry Point EXTERNAL
*/
