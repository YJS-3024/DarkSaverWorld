/* Ghidra decompilation; not recovered original source. Evidence: ev_a5a941df49459c9cb312069835b24a8226c81d23b970724f42d5823654cd5823 */

/* void __cdecl setmonsterai(int) */

void __cdecl setmonsterai(int param_1)

{
  int *piVar1;
  
                    /* 0x15a0  109  ?setmonsterai@@YAXH@Z */
  piVar1 = (int *)(DAT_1010b3a0 + param_1 * 0x2e0);
  if ((*piVar1 != 0) && (piVar1[1] != 0)) {
    piVar1[0x42] = 2;
  }
  piVar1 = (int *)(DAT_1010b3a0 + (param_1 / 10) * 0x1cc0);
  if ((*piVar1 != 0) && (piVar1[1] != 0)) {
    piVar1[0x42] = 2;
  }
  return;
}

