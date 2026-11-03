
/* WARNING: Heritage AFTER dead removal. Example location: s0xffffffb8 : 0x1000961e */
/* WARNING: Restarted to delay deadcode elimination for space: stack */
/* WARNING: Exceeded maximum restarts with more pending */

void __thiscall FUN_10009520(void *this,int param_1)

{
  int iVar1;
  character *pcVar2;
  magicrecord *pmVar3;
  LPRECT lprc;
  undefined4 *puVar4;
  void *local_4c;
  undefined4 *local_48;
  
  local_48 = (undefined4 *)0x0;
  *(int *)((int)this + 0xc) = param_1;
  pcVar2 = GetUnitInfoP(param_1);
  *(undefined4 *)((int)this + 0x3c) = DAT_1001c47c;
  iVar1 = *(int *)(pcVar2 + 0xb8);
  *(int *)((int)this + 0x10) = iVar1;
  if (0 < iVar1) {
    if (0 < iVar1) {
      puVar4 = (undefined4 *)((int)this + 0x1a8);
      pcVar2 = pcVar2 + 0xbc;
      lprc = (LPRECT)((int)this + 0xe0);
      local_4c = this;
      do {
        pmVar3 = GetMagicTbl(*(int *)pcVar2);
        SetRect(lprc + -10,0,-0x20,0x20,0);
        SetRect(lprc,*(int *)(pmVar3 + 0x20),*(int *)(pmVar3 + 0x24),*(int *)(pmVar3 + 0x20) + 0x20,
                *(int *)(pmVar3 + 0x24) + 0x20);
        CString::Format((CString *)(puVar4 + -10),(char *)(puVar4 + -10));
        *puVar4 = 0x5ff;
        puVar4[10] = *(int *)pcVar2;
        local_4c = (void *)((int)local_4c + 1);
        lprc = lprc + 1;
        puVar4 = puVar4 + 1;
        pcVar2 = pcVar2 + 4;
        this = local_48;
      } while ((int)local_4c < iRam00000010);
    }
    *(undefined4 *)((int)this + 4) = 0;
    *(undefined4 *)this = 1;
    return;
  }
  CString::CString((CString *)&stack0xffffff9c,&DAT_1001a3ac);
  FUN_10008790(DAT_1006a708,0x28,0x82,0x231,0x41);
  FUN_10008d20();
  return;
}

