/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x10002550; source SHA256 71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca
 */

/* Original export ReadSetFiles(options). Read common tile/item/ability-related tables according to
   original flags. The name setunit does not establish a scenarioNN.set/NN.pos roster loader.
   Source SHA256: 71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca */

int __cdecl LoadCommonGameTables(int param_1)

{
  uint uVar1;
  uint uVar2;
  uint uVar3;
  uint uVar4;
  
                    /* 0x2550  14  ?ReadSetFiles@@YAHH@Z */
  uVar1 = LoadTileAttributeTable(s_tileattr_atr_10005294);
  uVar2 = LoadAbilityDefinitionTables(param_1);
  uVar3 = LoadItemDefinitionTables(param_1);
  uVar4 = LoadMagicDefinitionTables();
  return uVar1 & 1 & uVar2 & uVar3 & uVar4;
}

