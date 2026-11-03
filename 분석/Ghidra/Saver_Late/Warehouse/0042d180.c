
void __thiscall FUN_0042d180(void *this,undefined2 *param_1)

{
  char cVar1;
  byte bVar2;
  short sVar3;
  bool bVar4;
  undefined3 extraout_var;
  undefined1 *puVar5;
  undefined3 extraout_var_00;
  int *this_00;
  void *pvVar6;
  short *psVar7;
  short *psVar8;
  int iVar9;
  int iVar10;
  char *pcVar11;
  undefined2 *puVar12;
  undefined4 *puVar13;
  byte *pbVar14;
  CString *this_01;
  uint uVar15;
  void *unaff_ESI;
  CString *unaff_EDI;
  int local_1c;
  CString local_18 [4];
  int local_14;
  uint local_10;
  void *pvStack_c;
  undefined1 *puStack_8;
  uint local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047d298;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_18);
  local_4 = 0;
  local_1c = 0;
  psVar8 = param_1 + 1;
  switch(*param_1) {
  case 0x20:
    puVar5 = (undefined1 *)thunk_FUN_0043c420((int)psVar8,&local_1c);
    param_1 = (undefined2 *)CONCAT31(param_1._1_3_,*puVar5);
    pcVar11 = operator_new(0xff);
    thunk_FUN_0043c450((int)psVar8,&local_1c,(undefined4 *)pcVar11,(uint)param_1 & 0xff);
    bVar4 = thunk_FUN_0042fe90(this,(int)psVar8,&local_1c,pcVar11);
    if (CONCAT31(extraout_var,bVar4) != 0) {
      psVar8 = (short *)thunk_FUN_0043c3f0((int)psVar8,&local_1c);
      sVar3 = *psVar8;
      CString::operator=((CString *)(DAT_004952a0 + 0x1a0),pcVar11);
      if (sVar3 == 1) {
        (**(code **)(**(int **)((int)this + 0x150) + 4))(0x37,1);
        operator_delete(pcVar11);
        break;
      }
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x25);
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x30);
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x22);
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x22);
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x3f);
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x3e);
      thunk_FUN_0043c090(1,(char *)0x0,pcVar11,pcVar11);
    }
    operator_delete(pcVar11);
    break;
  case 0x21:
    puVar5 = (undefined1 *)thunk_FUN_0043c420((int)psVar8,&local_1c);
    param_1 = (undefined2 *)CONCAT31(param_1._1_3_,*puVar5);
    pcVar11 = operator_new(0xff);
    thunk_FUN_0043c450((int)psVar8,&local_1c,(undefined4 *)pcVar11,(uint)param_1 & 0xff);
    bVar4 = thunk_FUN_0042fe90(this,(int)psVar8,&local_1c,pcVar11);
    if (((CONCAT31(extraout_var_00,bVar4) != 0) && (*(int **)((int)this + 0x150) != (int *)0x0)) &&
       (iVar9 = (**(code **)(**(int **)((int)this + 0x150) + 4))(0x17,1), iVar9 != 0)) {
      pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x17);
      if (pvVar6 != (void *)0x0) {
        thunk_FUN_0045fe30(pvVar6,pcVar11,unaff_EDI);
      }
      CString::operator=((CString *)(DAT_004952a0 + 0x1a0),pcVar11);
    }
    operator_delete(pcVar11);
    break;
  case 0x22:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x18,1);
    }
    break;
  case 0x23:
    thunk_FUN_0042dcc0(this,(int)psVar8);
    break;
  case 0x24:
    if ((DAT_00497c2c == 0) && (*(int **)((int)this + 0x150) != (int *)0x0)) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x1d,1);
    }
    break;
  case 0x25:
    if ((DAT_00497c2c == 0) && (*(int **)((int)this + 0x150) != (int *)0x0)) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x1e,1);
    }
    break;
  case 0x26:
    if ((DAT_00497c2c == 0) && (*(int **)((int)this + 0x150) != (int *)0x0)) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x1f,1);
    }
    break;
  case 0x27:
    if ((DAT_00497c2c == 0) && (*(int **)((int)this + 0x150) != (int *)0x0)) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x20,1);
    }
    break;
  case 0x29:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x30,1);
      pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x30);
      if (pvVar6 != (void *)0x0) {
        thunk_FUN_0044cce0(pvVar6,(int)psVar8);
      }
    }
    break;
  case 0x2b:
    if (*(int *)((int)this + 0x150) == 0) break;
    CString::CString((CString *)&param_1);
    local_4._0_1_ = 2;
    local_14 = 0;
    pbVar14 = (byte *)thunk_FUN_0043c420((int)psVar8,&local_14);
    bVar2 = *pbVar14;
    local_10 = CONCAT31(local_10._1_3_,bVar2);
    pcVar11 = operator_new(bVar2 + 1);
    thunk_FUN_0043c450((int)psVar8,&local_14,(undefined4 *)pcVar11,(uint)bVar2);
    CString::operator=((CString *)&param_1,pcVar11);
    operator_delete(pcVar11);
    thunk_FUN_00476870(*(int **)((int)this + 0x150));
    local_4 = (uint)local_4._1_3_;
    this_01 = (CString *)&param_1;
    goto LAB_0042d8be;
  case 0x2d:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x28,1);
      this_00 = (int *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x28);
      if (this_00 != (int *)0x0) {
        thunk_FUN_00462b90(this_00,(int)*psVar8);
        (**(code **)(*this_00 + 0x44))();
      }
    }
    break;
  case 0x31:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x2f,1);
      iVar9 = thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x2f);
      puVar13 = (undefined4 *)thunk_FUN_0043c4c0((int)psVar8,&local_1c);
      if (iVar9 != 0) {
        *(undefined4 *)(iVar9 + 0xb4) = *puVar13;
      }
    }
    break;
  case 0x32:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x22,1);
    }
    break;
  case 0x33:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x25,1);
      pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x25);
      if (pvVar6 != (void *)0x0) {
        thunk_FUN_00471640(pvVar6,*psVar8);
      }
    }
    break;
  case 0x37:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x3b,1);
      pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x3b);
      if (pvVar6 != (void *)0x0) {
        thunk_FUN_00455050(pvVar6,(int)psVar8);
      }
    }
    break;
  case 0x3a:
    if (*(int *)((int)this + 0x150) != 0) {
      psVar7 = (short *)thunk_FUN_0043c3f0((int)psVar8,&local_1c);
      sVar3 = *psVar7;
      psVar8 = (short *)thunk_FUN_0043c3f0((int)psVar8,&local_1c);
      thunk_FUN_00476e50(*(void **)((int)this + 0x150),(int)sVar3,(int)*psVar8,(int)unaff_EDI,
                         unaff_ESI);
    }
    break;
  case 0x44:
    if (*(int *)((int)this + 0x150) != 0) {
      DAT_00497c2c = 1;
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x43);
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x12);
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x15);
      thunk_FUN_004777f0(*(void **)((int)this + 0x150),0x14);
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x3e,1);
      pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x3e);
      if (pvVar6 != (void *)0x0) {
        thunk_FUN_0046d770(pvVar6,(int)psVar8);
      }
      DAT_00497c2c = 1;
    }
    break;
  case 0x45:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x3f,1);
      iVar9 = thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x3f);
      param_1 = (undefined2 *)0x0;
      puVar13 = (undefined4 *)thunk_FUN_0043c4c0((int)psVar8,(int *)&param_1);
      if (iVar9 != 0) {
        *(undefined4 *)(iVar9 + 0xb4) = *puVar13;
        iVar10 = thunk_FUN_0042f5c0(this);
        if (iVar10 != 0) {
          *(int *)(iVar9 + 0xb8) = (int)*(short *)(iVar10 + 0x3a);
        }
      }
    }
    break;
  case 0x46:
    if (*(int **)((int)this + 0x150) == (int *)0x0) break;
    (**(code **)(**(int **)((int)this + 0x150) + 4))(0x40,1);
    pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x40);
    param_1 = (undefined2 *)0x0;
    pcVar11 = (char *)thunk_FUN_0043c420((int)psVar8,(int *)&param_1);
    cVar1 = *pcVar11;
    local_10 = CONCAT31(local_10._1_3_,cVar1);
    CString::CString((CString *)&local_14);
    local_4 = CONCAT31(local_4._1_3_,1);
    if (cVar1 != '\0') {
      uVar15 = local_10 & 0xff;
      pcVar11 = operator_new(uVar15 + 1);
      thunk_FUN_0043c450((int)psVar8,(int *)&param_1,(undefined4 *)pcVar11,uVar15);
      CString::operator=((CString *)&local_14,pcVar11);
      operator_delete(pcVar11);
    }
    puVar12 = (undefined2 *)thunk_FUN_0043c3f0((int)psVar8,(int *)&param_1);
    if (pvVar6 != (void *)0x0) {
      thunk_FUN_0046a110(pvVar6,(CString *)&local_14,*puVar12);
    }
    local_4 = local_4 >> 8;
    this_01 = (CString *)&local_14;
LAB_0042d8be:
    local_4 = local_4 << 8;
    CString::~CString(this_01);
    break;
  case 0x47:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x41,1);
      pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x41);
      param_1 = (undefined2 *)0x0;
      puVar13 = (undefined4 *)thunk_FUN_0043c4c0((int)psVar8,(int *)&param_1);
      if (pvVar6 != (void *)0x0) {
        thunk_FUN_0045cdf0(pvVar6,*puVar13);
      }
    }
    break;
  case 0x48:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x42,1);
      pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x42);
      param_1 = (undefined2 *)0x0;
      if (pvVar6 != (void *)0x0) {
        thunk_FUN_00458e40(pvVar6,(int)psVar8,(int *)&param_1);
      }
    }
    break;
  case 0xd2:
  case 0xd3:
  case 0xd4:
  case 0xd5:
    if (*(int **)((int)this + 0x150) != (int *)0x0) {
      (**(code **)(**(int **)((int)this + 0x150) + 4))(0x4b,1);
      pvVar6 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x4b);
      if (pvVar6 != (void *)0x0) {
        thunk_FUN_0044cee0(pvVar6,(int)psVar8);
      }
    }
  }
  local_4 = 0xffffffff;
  CString::~CString(local_18);
  ExceptionList = pvStack_c;
  return;
}

