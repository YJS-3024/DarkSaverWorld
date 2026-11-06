
/* void __cdecl IncTick(void) */

void __cdecl IncTick(void)

{
  int iVar1;
  NativeCharacter32 *pNVar2;
  
                    /* 0xc6d0  53  ?IncTick@@YAXXZ */
  iVar1 = 0;
  pNVar2 = g_RuntimeCharacters;
  do {
    if ((*(int *)((int)&pNVar2->active + iVar1) != 0) && (*(int *)(&pNVar2->field_0x4 + iVar1) != 0)
       ) {
      *(int *)(&pNVar2->field_0x60 + iVar1) = *(int *)(&pNVar2->field_0x60 + iVar1) + 1;
      pNVar2 = g_RuntimeCharacters;
      if (*(int *)(&g_RuntimeCharacters->field_0x5c + iVar1) <
          *(int *)(&g_RuntimeCharacters->field_0x60 + iVar1)) {
        *(int *)(&g_RuntimeCharacters->field_0x60 + iVar1) =
             *(int *)(&g_RuntimeCharacters->field_0x5c + iVar1);
        pNVar2 = g_RuntimeCharacters;
      }
    }
    iVar1 = iVar1 + 0x2e0;
  } while (iVar1 < 0x21b100);
  return;
}


/* incoming refs
100140b8 DATA
Entry Point EXTERNAL
*/
