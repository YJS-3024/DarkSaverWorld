/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x100015a0; source SHA256 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b
 */

/* Original export setmonsterai. For the selected active unit and its decimal commander
   floor(unit/10)*10, write AI state +0x108=2 when the original eligibility fields are nonzero. This
   activates AI; it does not load a monster roster or determine job/count.
   Source SHA256: 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b */

void __cdecl ActivateUnitAndCommanderAi(int param_1)

{
  NativeCharacter32 *pNVar1;
  
                    /* 0x15a0  109  ?setmonsterai@@YAXH@Z */
  pNVar1 = g_RuntimeCharacters + param_1;
  if ((pNVar1->active != 0) && (*(int *)&pNVar1->field_0x4 != 0)) {
    pNVar1->aiState = 2;
  }
  pNVar1 = g_RuntimeCharacters + (param_1 / 10) * 10;
  if ((pNVar1->active != 0) && (*(int *)&pNVar1->field_0x4 != 0)) {
    pNVar1->aiState = 2;
  }
  return;
}

