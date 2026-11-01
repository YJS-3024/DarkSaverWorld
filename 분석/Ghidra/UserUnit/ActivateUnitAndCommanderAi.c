/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x100015a0
 * Evidence: ev_45f5857423819e88337ff09165ca8072dd6b8f86b574180d7f4aa3dd55a1075a
 */

/* void __cdecl setmonsterai(int) */

void __cdecl ActivateUnitAndCommanderAi(int param_1)

{
  int *piVar1;
  
                    /* Original export setmonsterai. For the selected active unit and its decimal
                       commander floor(unit/10)*10, write AI state +0x108=2 when the original
                       eligibility fields are nonzero. This activates AI; it does not load a monster
                       roster or determine job/count. */
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

