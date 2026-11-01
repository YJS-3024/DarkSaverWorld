/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x10002550
 * Evidence: ev_d28615d8e813514642726bc4c6a3bed0dd86e014b7671db15a87e56ce82d2325
 */

/* int __cdecl ReadSetFiles(int) */

int __cdecl LoadCommonGameTables(int param_1)

{
  uint uVar1;
  uint uVar2;
  uint uVar3;
  uint uVar4;
  
                    /* Original export ReadSetFiles(options). Read common tile/item/ability-related
                       tables according to original flags. The name setunit does not establish a
                       scenarioNN.set/NN.pos roster loader. */
  uVar1 = ReadTileAttr(s_tileattr_atr_10005294);
  uVar2 = FUN_10001420(param_1);
  uVar3 = FUN_10001a10(param_1);
  uVar4 = FUN_10001ed0();
  return uVar1 & 1 & uVar2 & uVar3 & uVar4;
}

