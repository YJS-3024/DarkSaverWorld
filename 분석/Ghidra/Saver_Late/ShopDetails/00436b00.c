
int __thiscall FUN_00436b00(void *this,CString *param_1)

{
  CString *pCVar1;
  byte *pbVar2;
  byte *pbVar3;
  int iVar4;
  uint uVar5;
  uint uVar6;
  byte *pbVar7;
  int iVar8;
  int local_14 [2];
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  pCVar1 = param_1;
  local_4 = 0xffffffff;
  local_8 = &LAB_0047d9f8;
  local_c = ExceptionList;
  if (*(int *)((int)this + 4) == 0) {
    return 0;
  }
  ExceptionList = &local_c;
  CString::CString((CString *)&param_1,param_1);
  local_4 = 0;
  uVar6 = *(int *)(*(int *)pCVar1 + -8) + 10;
  pbVar2 = operator_new(uVar6);
  pbVar3 = operator_new(uVar6);
  pbVar7 = pbVar2;
  for (uVar5 = uVar6 >> 2; uVar5 != 0; uVar5 = uVar5 - 1) {
    pbVar7[0] = 0;
    pbVar7[1] = 0;
    pbVar7[2] = 0;
    pbVar7[3] = 0;
    pbVar7 = pbVar7 + 4;
  }
  for (uVar5 = uVar6 & 3; uVar5 != 0; uVar5 = uVar5 - 1) {
    *pbVar7 = 0;
    pbVar7 = pbVar7 + 1;
  }
  pbVar7 = pbVar3;
  for (uVar5 = uVar6 >> 2; uVar5 != 0; uVar5 = uVar5 - 1) {
    pbVar7[0] = 0;
    pbVar7[1] = 0;
    pbVar7[2] = 0;
    pbVar7[3] = 0;
    pbVar7 = pbVar7 + 4;
  }
  local_14[0] = 0;
  for (uVar6 = uVar6 & 3; uVar6 != 0; uVar6 = uVar6 - 1) {
    *pbVar7 = 0;
    pbVar7 = pbVar7 + 1;
  }
  iVar8 = *(int *)((int)this + 0x14);
  iVar4 = thunk_FUN_0043fcc0(*(void **)((int)this + 4),(byte *)param_1);
  if (iVar8 < iVar4) {
    do {
      thunk_FUN_0043fdf0(*(void **)((int)this + 4),(byte *)param_1,pbVar2,pbVar3,iVar8);
      CStringList::AddTail((CStringList *)((int)this + 0x1c),(char *)pbVar2);
      CString::operator=((CString *)&param_1,(char *)pbVar3);
      iVar8 = *(int *)((int)this + 0x14);
      local_14[0] = local_14[0] + 1;
      iVar4 = thunk_FUN_0043fcc0(*(void **)((int)this + 4),(byte *)param_1);
    } while (iVar8 < iVar4);
  }
  CStringList::AddTail((CStringList *)((int)this + 0x1c),(CString *)&param_1);
  local_14[0] = local_14[0] + 1;
  if (*(int *)((int)this + 8) < *(int *)((int)this + 0x28)) {
    do {
      CStringList::RemoveHead((CStringList *)((int)this + 0x1c));
      CString::~CString((CString *)local_14);
    } while (*(int *)((int)this + 8) < *(int *)((int)this + 0x28));
  }
  operator_delete(pbVar2);
  operator_delete(pbVar3);
  local_4 = 0xffffffff;
  CString::~CString((CString *)&param_1);
  ExceptionList = local_c;
  return local_14[0];
}

