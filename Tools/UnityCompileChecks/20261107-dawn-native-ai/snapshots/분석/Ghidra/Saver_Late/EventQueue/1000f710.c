
void __thiscall FUN_1000f710(void *this,undefined4 param_1)

{
  int iVar1;
  CString *pCVar2;
  CString *this_00;
  int iVar3;
  int local_4;
  
  iVar3 = (int)(short)param_1;
  if ((&DAT_1006c120)[iVar3 * 0x193] != 0) {
    this_00 = (CString *)((int)this + 0x54);
    pCVar2 = (CString *)(&DAT_1006c130 + iVar3 * 0x193);
    local_4 = 399;
    do {
      CString::operator=(this_00,pCVar2);
      pCVar2 = pCVar2 + 4;
      this_00 = this_00 + 4;
      local_4 = local_4 + -1;
    } while (local_4 != 0);
    iVar1 = (int)param_1._2_2_;
    *(int *)((int)this + 0x10) = iVar3;
    *(int *)((int)this + 0x14) = iVar1;
    if (iVar1 == -1) {
      *(undefined4 *)((int)this + 0x18) = 0xffffffff;
    }
    else {
      iVar3 = GetTeamID(iVar1);
      *(int *)((int)this + 0x18) = iVar3;
    }
    *(undefined4 *)((int)this + 0xc) = 1;
    *(undefined4 *)((int)this + 0x1c) = 0;
    *(undefined4 *)((int)this + 0x34) = 0;
    *(undefined4 *)((int)this + 0x20) = 0;
    *(undefined4 *)((int)this + 0x2c) = 0xffffffff;
    *(undefined4 *)((int)this + 0x24) = 0xffffffff;
    *(undefined4 *)((int)this + 4) = 1;
  }
  return;
}


/* incoming refs
1000f0c3 UNCONDITIONAL_CALL
1000dd75 UNCONDITIONAL_CALL
CALLER 1000dcf0 FUN_1000dcf0
  1000ec89 UNCONDITIONAL_CALL
CALLER 1000f070 FUN_1000f070
  10013cd8 UNCONDITIONAL_CALL
  1000d8d9 UNCONDITIONAL_CALL
*/
