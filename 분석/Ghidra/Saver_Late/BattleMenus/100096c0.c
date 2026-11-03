
/* WARNING: Removing unreachable block (ram,0x10009875) */

void __thiscall FUN_100096c0(void *this,int param_1,undefined4 *param_2)

{
  int iVar1;
  character *pcVar2;
  magicrecord *pmVar3;
  LPRECT lprc;
  void **ppvVar4;
  undefined4 *puVar5;
  int local_50;
  void *local_48 [18];
  
  local_48[3] = (void *)0x20;
  local_48[6] = (void *)0x20;
  local_48[8] = (void *)0x20;
  local_48[0xb] = (void *)0x20;
  local_48[0xe] = (void *)0x20;
  local_48[0xf] = (void *)0x20;
  local_48[0] = (void *)0x0;
  local_48[1] = (void *)0xffffffe0;
  local_48[2] = (void *)0x0;
  local_48[4] = (void *)0xffffffe0;
  local_48[5] = (void *)0x0;
  local_48[7] = (void *)0x0;
  local_48[9] = (void *)0xffffffe0;
  local_48[10] = (void *)0xffffffe0;
  local_48[0xc] = (void *)0xffffffe0;
  local_48[0xd] = (void *)0xffffffe0;
  local_48[0x10] = (void *)0x0;
  local_48[0x11] = (void *)0x0;
  *(int *)((int)this + 0xc) = param_1;
  pcVar2 = GetUnitInfoP(param_1);
  *(undefined4 *)((int)this + 0x3c) = DAT_1001c47c;
  iVar1 = *(int *)(pcVar2 + 0xdc);
  *(int *)((int)this + 0x10) = iVar1;
  local_50 = 0;
  if (0 < iVar1) {
    pcVar2 = pcVar2 + 0xe0;
    ppvVar4 = local_48;
    puVar5 = (undefined4 *)((int)this + 0x1a8);
    lprc = (LPRECT)((int)this + 0xe0);
    do {
      iVar1 = *(int *)pcVar2;
      if (((iVar1 < 20000) && (9999 < iVar1)) || ((iVar1 < 32000 && (29999 < iVar1)))) {
        pmVar3 = GetMagicTbl(iVar1);
        SetRect(lprc + -10,(int)*ppvVar4,(int)ppvVar4[1],(int)*ppvVar4 + 0x20,(int)ppvVar4[1] + 0x20
               );
        SetRect(lprc,*(int *)(pmVar3 + 0x20),*(int *)(pmVar3 + 0x24),*(int *)(pmVar3 + 0x20) + 0x20,
                *(int *)(pmVar3 + 0x24) + 0x20);
        CString::Format((CString *)(puVar5 + -10),(char *)(puVar5 + -10));
        *puVar5 = 0x5ff;
        local_50 = local_50 + 1;
        puVar5[10] = *param_2;
        lprc = lprc + 1;
        puVar5 = puVar5 + 1;
        ppvVar4 = ppvVar4 + 2;
        this = local_48[0];
      }
      local_50 = local_50 + 1;
      pcVar2 = pcVar2 + 4;
    } while (local_50 < *(int *)((int)this + 0x10));
  }
  *(undefined4 *)((int)this + 0x10) = 0;
  CString::CString((CString *)&stack0xffffff98,&DAT_1001a3c4);
  FUN_10008790(DAT_1006a708,0x28,0x82,0x231,0x41);
  FUN_10008d20();
  return;
}

