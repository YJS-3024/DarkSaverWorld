
void FUN_10002a60(void)

{
  byte bVar1;
  undefined4 *puVar2;
  byte *pbVar3;
  int iVar4;
  undefined4 uVar5;
  byte *unaff_EBX;
  int iVar6;
  byte *pbVar7;
  int *piVar8;
  int iVar9;
  bool bVar10;
  CString local_20 [4];
  int *local_1c;
  undefined1 local_18 [4];
  int local_14;
  int local_10;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_10014de0;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_10);
  local_4 = 0;
  local_1c = &DAT_1001bde0;
  iVar6 = 0x1a4;
  do {
    piVar8 = local_1c;
    local_14 = *local_1c;
    CString::operator=((CString *)&local_10,(CString *)(local_1c + 1));
    iVar9 = local_14;
    if (*(int *)(local_10 + -8) != 0) {
      if (local_14 == 0) {
        CString::CString(local_20,'<',1);
        local_4 = CONCAT31(local_4._1_3_,1);
        puVar2 = (undefined4 *)CString::Mid((CString *)&local_10,(int)local_18,0xc);
        pbVar3 = (byte *)*puVar2;
        pbVar7 = unaff_EBX;
        do {
          bVar1 = *pbVar3;
          bVar10 = bVar1 < *pbVar7;
          if (bVar1 != *pbVar7) {
LAB_10002b19:
            iVar4 = (1 - (uint)bVar10) - (uint)(bVar10 != 0);
            goto LAB_10002b1e;
          }
          if (bVar1 == 0) break;
          bVar1 = pbVar3[1];
          bVar10 = bVar1 < pbVar7[1];
          if (bVar1 != pbVar7[1]) goto LAB_10002b19;
          pbVar3 = pbVar3 + 2;
          pbVar7 = pbVar7 + 2;
        } while (bVar1 != 0);
        iVar4 = 0;
LAB_10002b1e:
        CString::~CString((CString *)&local_1c);
        local_8 = (undefined1 *)((uint)local_8 & 0xffffff00);
        CString::~CString((CString *)&stack0xffffffdc);
        if (iVar4 == 0) {
          iVar9 = 7;
        }
      }
      switch(iVar9) {
      default:
        uVar5 = 0xffffff;
        break;
      case 2:
        uVar5 = 0x40ffff;
        break;
      case 3:
        uVar5 = 0xff00;
        break;
      case 4:
      case 7:
        uVar5 = 0xffff00;
        break;
      case 5:
        uVar5 = 0xc0c0c0;
      }
      (*DAT_1001c450)(DAT_1001c470,0x23,iVar6,local_10,uVar5,0);
      piVar8 = local_1c;
    }
    iVar6 = iVar6 + -0x14;
    local_1c = piVar8 + 2;
    if (iVar6 < 0x141) {
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_10);
      ExceptionList = pvStack_c;
      return;
    }
  } while( true );
}


/* incoming refs
10003baa UNCONDITIONAL_CALL
CALLER 10003840 Update
  10018aa0 DATA
  Entry Point EXTERNAL
*/
