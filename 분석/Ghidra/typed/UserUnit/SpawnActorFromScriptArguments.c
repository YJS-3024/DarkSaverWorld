/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x1000ea80; source SHA256 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b
 */

/* Original export dmSetChar(char*). Parse scripted actor identity, resolve the ability/picture
   record, build a temporary unitinfo, and call resetcharacter. This path must not be conflated with
   initial battle deployment.
   Source SHA256: 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b */

void __cdecl SpawnActorFromScriptArguments(char *param_1)

{
  CString *pCVar1;
  undefined4 *puVar2;
  int iVar3;
  int iVar4;
  abilityrecord *paVar5;
  undefined4 uVar6;
  int iVar7;
  char *local_e4;
  char *local_e0;
  CString local_dc [4];
  CString local_d8 [4];
  CString local_d4 [4];
  int local_d0;
  undefined4 local_cc;
  undefined4 local_c8;
  CString local_c4 [4];
  undefined1 local_c0 [168];
  CString local_18 [8];
  void *local_10;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
                    /* 0xea80  95  ?dmSetChar@@YAXPAD@Z */
  local_4 = 0xffffffff;
  local_8 = &LAB_10011f19;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_d8);
  local_4 = 0;
  CString::CString(local_d4);
  local_4._0_1_ = 1;
  CString::CString(local_dc);
  local_4._0_1_ = 2;
  CString::CString((CString *)&local_e0);
  local_4._0_1_ = 3;
  CString::operator=(local_d8,param_1);
  pCVar1 = (CString *)GetStrHead((char *)&local_e4);
  local_4._0_1_ = 4;
  CString::operator=(local_dc,pCVar1);
  local_4._0_1_ = 3;
  CString::~CString((CString *)&local_e4);
  pCVar1 = (CString *)GetStrTails((char *)&local_e4);
  local_4._0_1_ = 5;
  CString::operator=((CString *)&local_e0,pCVar1);
  local_4 = CONCAT31(local_4._1_3_,3);
  CString::~CString((CString *)&local_e4);
  pCVar1 = (CString *)CString::Mid(local_dc,(int)&local_e4,1);
  local_8._0_1_ = 6;
  CString::operator=(local_d8,pCVar1);
  local_8._0_1_ = 3;
  CString::~CString((CString *)&stack0xffffff18);
  puVar2 = (undefined4 *)GetStrHead((char *)local_d4);
  local_8._0_1_ = 7;
  iVar3 = FUN_1000f4e0((char *)*puVar2,-1);
  local_8._0_1_ = 3;
  CString::~CString(local_d4);
  pCVar1 = (CString *)GetStrHead((char *)local_d4);
  local_8._0_1_ = 8;
  CString::operator=((CString *)&local_e0,pCVar1);
  local_8._0_1_ = 3;
  CString::~CString(local_d4);
  pCVar1 = (CString *)GetStrTails((char *)local_c4);
  local_8._0_1_ = 9;
  CString::operator=((CString *)&local_e4,pCVar1);
  local_8._0_1_ = 3;
  CString::~CString(local_c4);
  iVar4 = FUN_1000f4e0(local_e0,-1);
  if ((iVar3 < 0) || (paVar5 = GetCharAbility(iVar4), paVar5 == (abilityrecord *)0x0)) {
    local_8._0_1_ = 2;
    CString::~CString((CString *)&local_e4);
    local_8._0_1_ = 1;
    CString::~CString((CString *)&local_e0);
    local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
    CString::~CString(local_d8);
  }
  else {
    iVar7 = iVar4;
    if (*(int *)paVar5 == iVar4) {
      iVar7 = *(int *)(paVar5 + 4);
    }
    if ((iVar4 < 100) || (299 < iVar4)) {
      uVar6 = *(undefined4 *)(paVar5 + 0x58);
    }
    else {
      uVar6 = 0;
    }
    local_d0 = 99;
    sscanf(local_e4,s__d__d__d_10016218,&local_c8,&local_cc,&local_d0);
    if (local_d0 == 99) {
      local_d0 = -1;
    }
    *(int *)&g_RuntimeCharacters[iVar3].field_0x2dc = local_d0;
    CString::CString((CString *)(local_c0 + 0xa4));
    local_8._0_1_ = 10;
    CString::CString(local_18);
    local_8._0_1_ = 0xb;
    BuildScriptUnitSnapshot
              ((undefined4 *)local_c0,iVar3,iVar4,iVar7,uVar6,local_c8,local_cc,iVar3 / 0x32);
    ApplyUnitSnapshot((NativeUnitInfo32 *)local_c0);
    local_8._0_1_ = 0xc;
    CString::~CString(local_18);
    local_8._0_1_ = 3;
    CString::~CString((CString *)(local_c0 + 0xa4));
    local_8._0_1_ = 2;
    CString::~CString((CString *)&local_e4);
    local_8._0_1_ = 1;
    CString::~CString((CString *)&local_e0);
    local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
    CString::~CString(local_d8);
  }
  local_8 = (undefined1 *)0xffffffff;
  CString::~CString(local_dc);
  ExceptionList = local_10;
  return;
}

