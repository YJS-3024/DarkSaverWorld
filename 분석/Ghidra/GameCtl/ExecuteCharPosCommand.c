/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x100105a0
 * Evidence: ev_c0ebc1b7786daa542d19c5ee6919890b85c57ceed838e7d0763b68e214bca774
 */

void ExecuteCharPosCommand(void)

{
  CString *pCVar1;
  int iVar2;
  undefined4 *puVar3;
  int extraout_ECX;
  undefined4 extraout_ECX_00;
  int extraout_ECX_01;
  undefined4 extraout_ECX_02;
  CString *this;
  char *unaff_EBX;
  char *unaff_ESI;
  undefined4 uVar4;
  CString local_1c [4];
  char *local_18;
  CString local_14 [4];
  undefined1 *local_10;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
                    /* Parse the scene CHARPOS arguments, resolve commander aliases and construct
                       the string passed to UserUnit.dmSetChar. Scripted actors must remain separate
                       from an initial battle roster. */
  local_8 = &LAB_100158a8;
  local_c = ExceptionList;
  local_4 = 0;
  ExceptionList = &local_c;
  CString::CString(local_14);
  local_4._0_1_ = 1;
  CString::CString(local_1c);
  local_4._0_1_ = 2;
  CString::CString((CString *)&local_18);
  local_10 = &stack0xffffffd4;
  local_4._0_1_ = 3;
  iVar2 = extraout_ECX;
  CString::CString((CString *)&stack0xffffffd4,(CString *)&stack0x00000004);
  pCVar1 = FUN_1000f110(&local_10,iVar2);
  local_4._0_1_ = 4;
  CString::operator=(local_1c,pCVar1);
  local_4._0_1_ = 3;
  CString::~CString((CString *)&local_10);
  local_10 = &stack0xffffffd4;
  uVar4 = extraout_ECX_00;
  CString::CString((CString *)&stack0xffffffd4,(CString *)&stack0x00000004);
  pCVar1 = FUN_1000f310(&local_10,uVar4);
  local_4._0_1_ = 5;
  CString::operator=((CString *)&local_18,pCVar1);
  local_4 = CONCAT31(local_4._1_3_,3);
  CString::~CString((CString *)&local_10);
  pCVar1 = (CString *)CString::Left(local_1c,(int)&local_10);
  local_8._0_1_ = 6;
  CString::operator=((CString *)&local_18,pCVar1);
  local_8 = (undefined1 *)CONCAT31(local_8._1_3_,3);
  CString::~CString(local_14);
  iVar2 = _stricmp(local_18,&DAT_1001a560);
  if (iVar2 == 0) {
    puVar3 = (undefined4 *)CString::Mid((CString *)&stack0xffffffe0,(int)local_14,3);
    iVar2 = FUN_1000f040((char *)*puVar3,0);
    CString::~CString((CString *)&local_18);
    iVar2 = iVar2 * 0x33;
  }
  else {
    atoi(unaff_EBX);
    iVar2 = extraout_ECX_01;
  }
  local_18 = &stack0xffffffcc;
  CString::CString((CString *)&stack0xffffffcc,(CString *)&stack0xffffffe0);
  pCVar1 = FUN_1000f110(&local_18,iVar2);
  local_c._0_1_ = 7;
  CString::operator=((CString *)&stack0xffffffdc,pCVar1);
  local_c._0_1_ = 3;
  CString::~CString((CString *)&local_18);
  local_18 = &stack0xffffffcc;
  uVar4 = extraout_ECX_02;
  CString::CString((CString *)&stack0xffffffcc,(CString *)&stack0xffffffe0);
  pCVar1 = FUN_1000f310(&local_18,uVar4);
  local_c._0_1_ = 8;
  CString::operator=((CString *)&stack0xffffffe0,pCVar1);
  local_c = (void *)CONCAT31(local_c._1_3_,3);
  CString::~CString((CString *)&local_18);
  atoi(unaff_ESI);
  CString::Format(this,&stack0xffffffdc);
  dmSetChar(unaff_EBX);
  local_8._0_1_ = 2;
  CString::~CString(local_1c);
  local_8._0_1_ = 1;
  CString::~CString((CString *)&stack0xffffffe0);
  local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
  CString::~CString((CString *)&local_18);
  local_8 = (undefined1 *)0xffffffff;
  CString::~CString((CString *)&stack0x00000000);
  ExceptionList = local_10;
  return;
}

