
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* struct magicrecord const * __cdecl GetMagicTbl(int) */

magicrecord * __cdecl GetMagicTbl(int param_1)

{
  char cVar1;
  int *piVar2;
  int iVar3;
  uint uVar4;
  uint uVar5;
  int iVar6;
  char *pcVar7;
  char *pcVar8;
  
                    /* 0x2610  8  ?GetMagicTbl@@YAPBUmagicrecord@@H@Z */
  if (DAT_10269850 == 0) {
    LoadMagicDefinitionTables();
  }
  if (DAT_10005020 == param_1) {
    return (magicrecord *)&DAT_10005388;
  }
  iVar3 = 0;
  piVar2 = &DAT_1002dbe0;
  while (*piVar2 != param_1) {
    piVar2 = piVar2 + 0x50;
    iVar3 = iVar3 + 1;
    if (0x1007bca0 < (int)piVar2) {
      _DAT_10005388 = 0xffffffff;
      return (magicrecord *)&DAT_10005388;
    }
  }
  if (iVar3 < 0) {
    _DAT_10005388 = 0xffffffff;
    return (magicrecord *)&DAT_10005388;
  }
  DAT_10005020 = param_1;
  iVar6 = iVar3 * 0x140;
  _DAT_10005388 = (&DAT_1002dbe0)[iVar3 * 0x50];
  _DAT_1000538c = *(undefined4 *)(&DAT_1002dcf0 + iVar6);
  _DAT_10005390 = *(undefined4 *)(&DAT_1002dce8 + iVar6);
  _DAT_10005394 = *(undefined4 *)(&DAT_1002dcec + iVar6);
  _DAT_10005398 = *(undefined4 *)(&DAT_1002dcf8 + iVar6);
  _DAT_1000539c = *(undefined4 *)(&DAT_1002dcfc + iVar6);
  _DAT_100054f8 = *(undefined4 *)(&DAT_1002dce4 + iVar6);
  _DAT_100053a0 = *(undefined4 *)(&DAT_1002dd00 + iVar6);
  _DAT_100053a4 = *(undefined4 *)(&DAT_1002dd04 + iVar6);
  _DAT_100054f4 = *(undefined4 *)(&DAT_1002dd08 + iVar6);
  uVar4 = 0xffffffff;
  pcVar7 = &DAT_1002dbe4 + iVar6;
  do {
    pcVar8 = pcVar7;
    if (uVar4 == 0) break;
    uVar4 = uVar4 - 1;
    pcVar8 = pcVar7 + 1;
    cVar1 = *pcVar7;
    pcVar7 = pcVar8;
  } while (cVar1 != '\0');
  uVar4 = ~uVar4;
  pcVar7 = pcVar8 + -uVar4;
  pcVar8 = (char *)&DAT_100053f4;
  for (uVar5 = uVar4 >> 2; uVar5 != 0; uVar5 = uVar5 - 1) {
    *(undefined4 *)pcVar8 = *(undefined4 *)pcVar7;
    pcVar7 = pcVar7 + 4;
    pcVar8 = pcVar8 + 4;
  }
  _DAT_100053a8 = *(undefined4 *)(&DAT_1002dd18 + iVar6);
  for (uVar4 = uVar4 & 3; uVar4 != 0; uVar4 = uVar4 - 1) {
    *pcVar8 = *pcVar7;
    pcVar7 = pcVar7 + 1;
    pcVar8 = pcVar8 + 1;
  }
  _DAT_100054fc = *(undefined4 *)(&DAT_1002dd10 + iVar6);
  _DAT_100053ac = *(undefined4 *)(&DAT_1002dd1c + iVar6);
  iVar3 = *(int *)(&DAT_1002dd14 + iVar6) * 0x148;
  _DAT_100053b0 = *(undefined4 *)(&DAT_10021cb4 + iVar3);
  _DAT_100053b4 = *(undefined4 *)(&DAT_10021cb8 + iVar3);
  _DAT_100053b8 = *(undefined4 *)(&DAT_10021cbc + iVar3);
  _DAT_100053bc = *(undefined4 *)(&DAT_10021cc0 + iVar3);
  _DAT_100053c0 = *(undefined4 *)(&DAT_10021cc4 + iVar3);
  _DAT_100053c4 = *(undefined4 *)(&DAT_10021cc8 + iVar3);
  _DAT_100053e0 = *(undefined4 *)(&DAT_10021ce4 + iVar3);
  _DAT_100053c8 = *(undefined4 *)(&DAT_10021ccc + iVar3);
  _DAT_100053cc = *(undefined4 *)(&DAT_10021cd0 + iVar3);
  _DAT_100053d0 = *(undefined4 *)(&DAT_10021cd4 + iVar3);
  _DAT_100053d4 = *(undefined4 *)(&DAT_10021cd8 + iVar3);
  _DAT_100053d8 = *(undefined4 *)(&DAT_10021cdc + iVar3);
  _DAT_100053dc = *(undefined4 *)(&DAT_10021ce0 + iVar3);
  _DAT_100053e4 = *(undefined4 *)(&DAT_10021ce8 + iVar3);
  _DAT_100053e8 = *(undefined4 *)(&DAT_10021cec + iVar3);
  _DAT_100053ec = *(undefined4 *)(&DAT_10021cf0 + iVar3);
  _DAT_100053f0 = *(undefined4 *)(&DAT_10021cf4 + iVar3);
  return (magicrecord *)&DAT_10005388;
}


/* incoming refs
100045b4 DATA
Entry Point EXTERNAL
*/
