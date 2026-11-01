/* Ghidra decompilation; not recovered original source. Evidence: ev_6d11f8f22a423bd635ba837466c3ac2b892fd2b6b63218dd0fa7bfe525b71288 */

/* int __cdecl ReadSetFiles(int) */

int __cdecl ReadSetFiles(int param_1)

{
  uint uVar1;
  uint uVar2;
  uint uVar3;
  uint uVar4;
  
                    /* 0x2550  14  ?ReadSetFiles@@YAHH@Z */
  uVar1 = ReadTileAttr(s_tileattr_atr_10005294);
  uVar2 = FUN_10001420(param_1);
  uVar3 = FUN_10001a10(param_1);
  uVar4 = FUN_10001ed0();
  return uVar1 & 1 & uVar2 & uVar3 & uVar4;
}

