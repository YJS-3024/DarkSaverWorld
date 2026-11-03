
undefined4 __thiscall FUN_00473e30(void *this,int param_1,int param_2)

{
  char cVar1;
  int iVar2;
  void *pvVar3;
  int *piVar4;
  int iVar5;
  uint uVar6;
  char *pcVar7;
  char local_20 [20];
  void *local_c;
  undefined *puStack_8;
  int local_4;
  
  iVar5 = param_1;
  local_4 = 0xffffffff;
  puStack_8 = &DAT_00480f77;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  iVar2 = thunk_FUN_00475e40(this,param_1);
  if (iVar2 != 0) {
    ExceptionList = local_c;
    return 0;
  }
  CString::CString((CString *)&param_1);
  local_4 = 0;
  switch(iVar5) {
  case 0x11:
    CString::operator=((CString *)&param_1,s_scene1_lsc_00491254);
    pvVar3 = operator_new(0xbc);
    local_4._0_1_ = 1;
    if (pvVar3 == (void *)0x0) {
LAB_0047512e:
      local_4 = (uint)local_4._1_3_ << 8;
      piVar4 = (int *)0x0;
    }
    else {
      piVar4 = thunk_FUN_00470540(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,this,
                                  *(int *)((int)this + 0x48));
      local_4 = (uint)local_4._1_3_ << 8;
    }
    break;
  case 0x12:
    CString::operator=((CString *)&param_1,s_scene2_lsc_00491244);
    pvVar3 = operator_new(0x17c);
    local_4._0_1_ = 2;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00464bb0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(int *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x13:
    CString::operator=((CString *)&param_1,s_scene3_lsc_00491234);
    pvVar3 = operator_new(0xd8);
    local_4._0_1_ = 3;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00453aa0(pvVar3,*(int *)((int)this + 0x44),(CString *)&param_1,this);
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x14:
    CString::operator=((CString *)&param_1,s_scene4_lsc_00491224);
    pvVar3 = operator_new(0x32c);
    local_4._0_1_ = 4;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0046a300(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x15:
    CString::operator=((CString *)&param_1,s_scene6_lsc_00491214);
    pvVar3 = operator_new(0xe0);
    local_4._0_1_ = 5;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00457050(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  default:
    goto switchD_00473e7b_caseD_16;
  case 0x17:
    CString::operator=((CString *)&param_1,s_scene12_lsc_00491204);
    pvVar3 = operator_new(0x10c);
    local_4._0_1_ = 6;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045ee00(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x18:
    CString::operator=((CString *)&param_1,s_scene9_lsc_004911f4);
    pvVar3 = operator_new(0x194);
    local_4._0_1_ = 7;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0046f180(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x19:
    CString::operator=((CString *)&param_1,s_scene13_lsc_004911e4);
    pvVar3 = operator_new(0xf0);
    local_4._0_1_ = 8;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00471790(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x1a:
    CString::operator=((CString *)&param_1,s_scene14_lsc_004911d4);
    pvVar3 = operator_new(0xd4);
    local_4._0_1_ = 9;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_004698c0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x1b:
    CString::operator=((CString *)&param_1,s_scene15_lsc_004911c4);
    pvVar3 = operator_new(200);
    local_4._0_1_ = 10;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00459c80(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x1c:
    CString::operator=((CString *)&param_1,s_scene7_lsc_004911b4);
    pvVar3 = operator_new(0xb8);
    local_4._0_1_ = 0xb;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045da60(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x1d:
    CString::operator=((CString *)&param_1,s_scene10_lsc_004911a4);
    pvVar3 = operator_new(0x36c);
    local_4._0_1_ = 0xc;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044ba30(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x1e:
    CString::operator=((CString *)&param_1,s_scene10_lsc_004911a4);
    pvVar3 = operator_new(0x36c);
    local_4._0_1_ = 0xd;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044bb30(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x1f:
    CString::operator=((CString *)&param_1,s_scene10_lsc_004911a4);
    pvVar3 = operator_new(0x36c);
    local_4._0_1_ = 0xe;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044bc30(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x20:
    CString::operator=((CString *)&param_1,s_scene10_lsc_004911a4);
    pvVar3 = operator_new(0x36c);
    local_4._0_1_ = 0xf;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044bd30(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x21:
    CString::operator=((CString *)&param_1,s_scene19_lsc_00491194);
    pvVar3 = operator_new(0xd4);
    local_4._0_1_ = 0x10;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045e700(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(BB_CompressDllMng **)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x22:
    CString::operator=((CString *)&param_1,s_scene18_lsc_00491184);
    pvVar3 = operator_new(0xd8);
    local_4._0_1_ = 0x11;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00445070(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x23:
    CString::operator=((CString *)&param_1,s_scene17_lsc_00491174);
    pvVar3 = operator_new(0xd0);
    local_4._0_1_ = 0x12;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0046ea00(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x24:
    CString::operator=((CString *)&param_1,s_scene5_lsc_00491164);
    pvVar3 = operator_new(0xec);
    local_4._0_1_ = 0x13;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00446870(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x25:
    CString::operator=((CString *)&param_1,s_scene20_lsc_00491154);
    pvVar3 = operator_new(0x37c);
    local_4._0_1_ = 0x14;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00470c90(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x26:
    CString::operator=((CString *)&param_1,s_scene21_lsc_00491144);
    pvVar3 = operator_new(0x178);
    local_4._0_1_ = 0x15;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00468900(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x27:
    CString::operator=((CString *)&param_1,s_scene22_lsc_00491134);
    pvVar3 = operator_new(0x184);
    local_4._0_1_ = 0x16;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0046e150(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x28:
    CString::operator=((CString *)&param_1,s_scene23_lsc_0049020c);
    pvVar3 = operator_new(0x444);
    local_4._0_1_ = 0x17;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00460c80(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x29:
    CString::operator=((CString *)&param_1,s_scene25_lsc_00491124);
    pvVar3 = operator_new(0xf8);
    local_4._0_1_ = 0x18;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_004474d0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x2a:
    CString::operator=((CString *)&param_1,s_scene26_lsc_00491114);
    pvVar3 = operator_new(0xec);
    local_4._0_1_ = 0x19;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00448950(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x2b:
    CString::operator=((CString *)&param_1,s_scene27_lsc_00491104);
    pvVar3 = operator_new(0xcc);
    local_4._0_1_ = 0x1a;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00444720(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x2c:
    CString::operator=((CString *)&param_1,s_scene28_lsc_004910f4);
    pvVar3 = operator_new(0xbc);
    local_4._0_1_ = 0x1b;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00467b60(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x2d:
    CString::operator=((CString *)&param_1,s_scene24_lsc_004910e4);
    pvVar3 = operator_new(0x14c);
    local_4._0_1_ = 0x1c;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045bcd0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x2e:
    CString::operator=((CString *)&param_1,s_scene36_lsc_004910d4);
    pvVar3 = operator_new(0xc0);
    local_4._0_1_ = 0x1d;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0046e4e0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x2f:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(0xb8);
    local_4._0_1_ = 0x1e;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045d630(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x30:
    CString::operator=((CString *)&param_1,s_scene10_lsc_004911a4);
    pvVar3 = operator_new(0x36c);
    local_4._0_1_ = 0x1f;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044cc10(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x31:
    CString::operator=((CString *)&param_1,s_scene31_lsc_004910a4);
    pvVar3 = operator_new(0xe0);
    local_4._0_1_ = 0x21;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045aae0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x32:
    CString::operator=((CString *)&param_1,s_scene30_lsc_004910b4);
    pvVar3 = operator_new(0xd4);
    local_4._0_1_ = 0x20;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045d030(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x33:
    CString::operator=((CString *)&param_1,s_scene32_lsc_00491094);
    pvVar3 = operator_new(0xe4);
    local_4._0_1_ = 0x22;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00446460(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x34:
    CString::operator=((CString *)&param_1,s_scene33_lsc_00491084);
    pvVar3 = operator_new(0xf0);
    local_4._0_1_ = 0x23;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045a3b0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x35:
    CString::operator=((CString *)&param_1,s_scene34_lsc_00491074);
    pvVar3 = operator_new(0xd4);
    local_4._0_1_ = 0x24;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00445840(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x36:
    CString::operator=((CString *)&param_1,s_scene35_lsc_00491064);
    pvVar3 = operator_new(0xe0);
    local_4._0_1_ = 0x25;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00472a10(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x37:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(0xb4);
    local_4._0_1_ = 0x26;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00444c40(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x38:
    CString::operator=((CString *)&param_1,s_scene37_lsc_0049021c);
    pvVar3 = operator_new(0xd4);
    local_4._0_1_ = 0x27;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_004680c0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x39:
    CString::operator=((CString *)&param_1,s_scene38_lsc_00491054);
    pvVar3 = operator_new(0xc0);
    local_4._0_1_ = 0x28;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00472440(pvVar3,*(CDirectDraw **)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x3a:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(0xb8);
    local_4._0_1_ = 0x29;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045e1a0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x3b:
    CString::operator=((CString *)&param_1,s_scene39_lsc_00491044);
    pvVar3 = operator_new(0x1b0);
    local_4._0_1_ = 0x2a;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00454950(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x3c:
    CString::operator=((CString *)&param_1,s_scene38_lsc_00491054);
    pvVar3 = operator_new(0xc0);
    local_4._0_1_ = 0x2b;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00459070(pvVar3,*(CDirectDraw **)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x3d:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(0xc0);
    local_4._0_1_ = 0x2c;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00459800(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x3e:
    CString::operator=((CString *)&param_1,s_scene40_lsc_00491034);
    pvVar3 = operator_new(0x210);
    local_4._0_1_ = 0x2d;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0046c290(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x3f:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(0xbc);
    local_4._0_1_ = 0x2e;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_004528b0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x40:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(0xc0);
    local_4._0_1_ = 0x2f;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00469e20(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x41:
    CString::operator=((CString *)&param_1,s_scene41_lsc_00491024);
    pvVar3 = operator_new(0xbc);
    local_4._0_1_ = 0x30;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045cb90(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x42:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(0xbc);
    local_4._0_1_ = 0x31;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00458af0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x43:
    CString::operator=((CString *)&param_1,s_scene42_lsc_00491014);
    pvVar3 = operator_new(0x10c);
    local_4._0_1_ = 0x32;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00465e10(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x44:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(0xbc);
    local_4._0_1_ = 0x33;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_004607b0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x45:
    CString::operator=((CString *)&param_1,s_scene43_lsc_00491004);
    pvVar3 = operator_new(0xe8);
    local_4._0_1_ = 0x34;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0045b600(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x46:
    CString::operator=((CString *)&param_1,s_scene44_lsc_00490ff4);
    pvVar3 = operator_new(0x334);
    local_4._0_1_ = 0x35;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044f640(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(int *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x47:
    CString::operator=((CString *)&param_1,s_scene45_lsc_00490fe4);
    pvVar3 = operator_new(0xb8);
    local_4._0_1_ = 0x36;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_00452570(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x49:
    CString::operator=((CString *)&param_1,s_scene29_lsc_004910c4);
    pvVar3 = operator_new(200);
    local_4._0_1_ = 0x3c;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_004495f0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x4a:
    CString::operator=((CString *)&param_1,s_scene46_lsc_00490fd4);
    pvVar3 = operator_new(0x418);
    local_4._0_1_ = 0x37;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044be30(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x4b:
    CString::operator=((CString *)&param_1,s_scene46_lsc_00490fd4);
    pvVar3 = operator_new(0x418);
    local_4._0_1_ = 0x3b;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044ce10(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x4c:
    CString::operator=((CString *)&param_1,s_scene46_lsc_00490fd4);
    pvVar3 = operator_new(0x418);
    local_4._0_1_ = 0x38;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044c1a0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x4d:
    CString::operator=((CString *)&param_1,s_scene46_lsc_00490fd4);
    pvVar3 = operator_new(0x418);
    local_4._0_1_ = 0x39;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044c510(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x4e:
    CString::operator=((CString *)&param_1,s_scene46_lsc_00490fd4);
    pvVar3 = operator_new(0x418);
    local_4._0_1_ = 0x3a;
    if (pvVar3 == (void *)0x0) goto LAB_0047512e;
    piVar4 = thunk_FUN_0044c8a0(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                (int)this,*(undefined4 *)((int)this + 0x4c),
                                *(undefined4 *)((int)this + 0x50));
    local_4 = (uint)local_4._1_3_ << 8;
    break;
  case 0x51:
    CString::operator=((CString *)&param_1,s_scene48_lsc_00490fc4);
    pvVar3 = operator_new(0xbc);
    local_4._0_1_ = 0x3d;
    if (pvVar3 == (void *)0x0) {
      piVar4 = (int *)0x0;
    }
    else {
      piVar4 = thunk_FUN_00467090(pvVar3,*(undefined4 *)((int)this + 0x44),(CString *)&param_1,
                                  (int)this,*(undefined4 *)((int)this + 0x4c),
                                  *(undefined4 *)((int)this + 0x50));
    }
    local_4 = (uint)local_4._1_3_ << 8;
    if (*(int *)((int)this + 4) != 0) {
      *(undefined4 *)((int)this + 4) = 0;
      sprintf(local_20,s__c_c_c_s_00490fac,0x7a,5,9,&DAT_00490fb8);
      uVar6 = 0xffffffff;
      pcVar7 = local_20;
      do {
        if (uVar6 == 0) break;
        uVar6 = uVar6 - 1;
        cVar1 = *pcVar7;
        pcVar7 = pcVar7 + 1;
      } while (cVar1 != '\0');
      thunk_FUN_004132d0(DAT_004952a0,~uVar6 - 1,local_20);
    }
  }
  if (piVar4 != (int *)0x0) {
    iVar5 = thunk_FUN_00434ea0(piVar4);
    if (iVar5 != 0) {
      if (param_2 == 0) {
        (**(code **)(*piVar4 + 0x40))(0);
      }
      if (((*(int *)((int)this + 0x40) == -1) ||
          (*(int *)(*(int *)(*(int *)((int)this + 0x18) + *(int *)((int)this + 0x40) * 4) + 0x90) ==
           0)) || (piVar4[0x24] != 0)) {
        CPtrArray::SetAtGrow((CPtrArray *)((int)this + 0x14),*(int *)((int)this + 0x1c),piVar4);
        *(int *)((int)this + 0x3c) = *(int *)((int)this + 0x1c);
        *(int *)((int)this + 0x40) = *(int *)((int)this + 0x1c) + -1;
        local_4 = 0xffffffff;
        CString::~CString((CString *)&param_1);
        ExceptionList = local_c;
        return 1;
      }
      (**(code **)*piVar4)(1);
      goto LAB_00475256;
    }
    (**(code **)*piVar4)(1);
  }
switchD_00473e7b_caseD_16:
LAB_00475256:
  local_4 = 0xffffffff;
  CString::~CString((CString *)&param_1);
  ExceptionList = local_c;
  return 0;
}

