
void __thiscall FUN_0046bf20(void *this,int param_1)

{
  char cVar1;
  char *pcVar2;
  int iVar3;
  uint uVar4;
  uint uVar5;
  CString *this_00;
  char *pcVar6;
  char *pcVar7;
  char *pcVar8;
  
  if (param_1 == 0) {
    CString::Empty((CString *)((int)this + 0x324));
    return;
  }
  pcVar2 = operator_new(0xff);
  pcVar8 = pcVar2;
  if (*(char *)(param_1 + 0x94) == '\0') {
    uVar4 = 0xffffffff;
    pcVar6 = &DAT_0049076c;
    do {
      pcVar7 = pcVar6;
      if (uVar4 == 0) break;
      uVar4 = uVar4 - 1;
      pcVar7 = pcVar6 + 1;
      cVar1 = *pcVar6;
      pcVar6 = pcVar7;
    } while (cVar1 != '\0');
    uVar4 = ~uVar4;
    pcVar6 = pcVar7 + -uVar4;
    for (uVar5 = uVar4 >> 2; uVar5 != 0; uVar5 = uVar5 - 1) {
      *(undefined4 *)pcVar8 = *(undefined4 *)pcVar6;
      pcVar6 = pcVar6 + 4;
      pcVar8 = pcVar8 + 4;
    }
  }
  else {
    uVar4 = 0xffffffff;
    pcVar6 = (char *)(param_1 + 0x94);
    do {
      pcVar7 = pcVar6;
      if (uVar4 == 0) break;
      uVar4 = uVar4 - 1;
      pcVar7 = pcVar6 + 1;
      cVar1 = *pcVar6;
      pcVar6 = pcVar7;
    } while (cVar1 != '\0');
    uVar4 = ~uVar4;
    pcVar6 = pcVar7 + -uVar4;
    for (uVar5 = uVar4 >> 2; uVar5 != 0; uVar5 = uVar5 - 1) {
      *(undefined4 *)pcVar8 = *(undefined4 *)pcVar6;
      pcVar6 = pcVar6 + 4;
      pcVar8 = pcVar8 + 4;
    }
  }
  for (uVar4 = uVar4 & 3; uVar4 != 0; uVar4 = uVar4 - 1) {
    *pcVar8 = *pcVar6;
    pcVar6 = pcVar6 + 1;
    pcVar8 = pcVar8 + 1;
  }
  iVar3 = (**(code **)(**(int **)(*(int *)((int)this + 0xa0) + 0x48) + 4))();
  if (iVar3 != 1) {
    CString::operator=((CString *)((int)this + 0x324),pcVar2);
    operator_delete(pcVar2);
    return;
  }
  CString::Format(this_00,(char *)((int)this + 0x324));
  operator_delete(pcVar2);
  return;
}

