
void FUN_00413350(undefined4 param_1,uint param_2,int param_3,int param_4,char param_5,short param_6
                 ,undefined4 param_7,undefined4 param_8,int param_9,undefined4 param_10,
                 undefined4 param_11)

{
  byte bVar1;
  short sVar2;
  short sVar3;
  void *pvVar4;
  bool bVar5;
  char cVar6;
  uint uVar7;
  undefined **ppuVar8;
  int iVar9;
  short *psVar10;
  int *piVar11;
  undefined3 extraout_var;
  undefined1 *puVar12;
  undefined4 *puVar13;
  byte *pbVar14;
  char *pcVar15;
  AFX_MODULE_STATE *pAVar16;
  void *this;
  uint uVar17;
  int iVar18;
  uint uVar19;
  undefined4 unaff_EBX;
  undefined4 unaff_EBP;
  undefined4 unaff_ESI;
  undefined4 unaff_EDI;
  undefined **ppuVar20;
  undefined4 *puVar21;
  undefined4 unaff_retaddr;
  short in_stack_0000004e;
  int in_stack_0000025c;
  short in_stack_00000260;
  short in_stack_00000262;
  short in_stack_00000264;
  short in_stack_00000266;
  short in_stack_00000268;
  char in_stack_00000a74;
  void *in_stack_00003974;
  int in_stack_00003984;
  void *pvVar22;
  undefined1 *local_8;
  uint local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0047bccb;
  pvVar22 = ExceptionList;
  ExceptionList = &stack0xfffffff4;
  FUN_00479c00();
  uVar7 = *(int *)(in_stack_00003984 + 0x10) - *(int *)(in_stack_00003984 + 0xc);
  if ((int)uVar7 < 0) {
    uVar7 = uVar7 + *(int *)(in_stack_00003984 + 4);
  }
  ppuVar8 = operator_new(uVar7);
  if (*(int *)((int)this + 0x108) == 0) {
    uVar7 = *(int *)(in_stack_00003984 + 0x10) - *(int *)(in_stack_00003984 + 0xc);
    if ((int)uVar7 < 0) {
      uVar7 = uVar7 + *(int *)(in_stack_00003984 + 4);
    }
    local_4 = *(uint *)(in_stack_00003984 + 0xc);
    uVar19 = *(int *)(in_stack_00003984 + 4) - local_4;
    if ((int)uVar19 < (int)uVar7) {
      uVar7 = uVar7 - uVar19;
      puVar13 = (undefined4 *)(local_4 + *(int *)(in_stack_00003984 + 8));
      ppuVar20 = ppuVar8;
      for (uVar17 = uVar19 >> 2; uVar17 != 0; uVar17 = uVar17 - 1) {
        *ppuVar20 = (undefined *)*puVar13;
        puVar13 = puVar13 + 1;
        ppuVar20 = ppuVar20 + 1;
      }
      for (uVar17 = uVar19 & 3; uVar17 != 0; uVar17 = uVar17 - 1) {
        *(undefined1 *)ppuVar20 = *(undefined1 *)puVar13;
        puVar13 = (undefined4 *)((int)puVar13 + 1);
        ppuVar20 = (undefined **)((int)ppuVar20 + 1);
      }
      puVar13 = *(undefined4 **)(in_stack_00003984 + 8);
      puVar21 = (undefined4 *)(uVar19 + (int)ppuVar8);
      for (uVar17 = uVar7 >> 2; uVar17 != 0; uVar17 = uVar17 - 1) {
        *puVar21 = *puVar13;
        puVar13 = puVar13 + 1;
        puVar21 = puVar21 + 1;
      }
      for (uVar19 = uVar7 & 3; uVar19 != 0; uVar19 = uVar19 - 1) {
        *(undefined1 *)puVar21 = *(undefined1 *)puVar13;
        puVar13 = (undefined4 *)((int)puVar13 + 1);
        puVar21 = (undefined4 *)((int)puVar21 + 1);
      }
      *(uint *)(in_stack_00003984 + 0xc) = uVar7;
      local_4 = uVar7;
    }
    else {
      puVar13 = (undefined4 *)(*(int *)(in_stack_00003984 + 8) + local_4);
      ppuVar20 = ppuVar8;
      for (uVar19 = uVar7 >> 2; uVar19 != 0; uVar19 = uVar19 - 1) {
        *ppuVar20 = (undefined *)*puVar13;
        puVar13 = puVar13 + 1;
        ppuVar20 = ppuVar20 + 1;
      }
      for (uVar19 = uVar7 & 3; uVar19 != 0; uVar19 = uVar19 - 1) {
        *(undefined1 *)ppuVar20 = *(undefined1 *)puVar13;
        puVar13 = (undefined4 *)((int)puVar13 + 1);
        ppuVar20 = (undefined **)((int)ppuVar20 + 1);
      }
      iVar9 = *(int *)(in_stack_00003984 + 0xc) + uVar7;
      *(int *)(in_stack_00003984 + 0xc) = iVar9;
      if (iVar9 == *(int *)(in_stack_00003984 + 4)) {
        *(undefined4 *)(in_stack_00003984 + 0xc) = 0;
      }
    }
    if (*(char *)ppuVar8 == '&') {
      iVar9 = thunk_FUN_0043afe0(*(int *)((int)this + 200));
      if (iVar9 != 0) {
        operator_delete(ppuVar8);
        iVar9 = (*(int **)((int)this + 200))[5];
        if (iVar9 == 1) {
          *(undefined4 *)((int)this + 0xd8) = 1;
          ExceptionList = in_stack_00003974;
          return;
        }
        if (iVar9 != 2) {
          ExceptionList = in_stack_00003974;
          return;
        }
        (**(code **)(**(int **)((int)this + 200) + 0x38))();
        *(undefined2 *)((int)this + 0xc0) = 2;
        if (*(int **)((int)this + 0x1c0) == (int *)0x0) {
          ExceptionList = in_stack_00003974;
          return;
        }
        (**(code **)(**(int **)((int)this + 0x1c0) + 0xc))();
        ExceptionList = in_stack_00003974;
        return;
      }
      (**(code **)(**(int **)(*(int *)((int)this + 0x120) + 8) + 0x28))();
      if (*(short *)((int)this + 0xc0) != 2) {
        AfxMessageBox(&DAT_0048f120,0,0);
      }
      PostMessageA(*(HWND *)((int)this + 0x20),0x466,1,0);
    }
    else {
      pvVar4 = *(void **)((int)this + 0x15c);
      if ((pvVar4 != (void *)0x0) && (*(int *)((int)pvVar4 + 0x1c) != -1)) {
        if (*(int *)((int)pvVar4 + 4) == 0) {
          iVar9 = thunk_FUN_0041df00(pvVar4,(undefined1 *)ppuVar8);
        }
        else {
          iVar9 = thunk_FUN_0041dd10(pvVar4,(undefined1 *)ppuVar8);
        }
        if (iVar9 != 0) goto LAB_00413cda;
      }
      switch(*(undefined1 *)ppuVar8) {
      case 0x32:
        puVar13 = (undefined4 *)thunk_FUN_0043c870((undefined4 *)&stack0x00000c94,(int)ppuVar8 + 1);
        puVar21 = (undefined4 *)&stack0x00000670;
        for (iVar9 = 0x43; iVar9 != 0; iVar9 = iVar9 + -1) {
          *puVar21 = *puVar13;
          puVar13 = puVar13 + 1;
          puVar21 = puVar21 + 1;
        }
        *(undefined2 *)puVar21 = *(undefined2 *)puVar13;
        puVar13 = (undefined4 *)&stack0x00000670;
        puVar21 = (undefined4 *)&stack0x0000026c;
        for (iVar9 = 0x43; iVar9 != 0; iVar9 = iVar9 + -1) {
          *puVar21 = *puVar13;
          puVar13 = puVar13 + 1;
          puVar21 = puVar21 + 1;
        }
        pvVar4 = *(void **)((int)this + 0xdc);
        *(undefined2 *)puVar21 = *(undefined2 *)puVar13;
        thunk_FUN_004276a0(pvVar4,(short *)&stack0x0000026c);
        break;
      case 0x34:
        local_4 = 1;
        puVar12 = (undefined1 *)thunk_FUN_0043c420((int)ppuVar8,(int *)&local_4);
        pvVar22 = (void *)CONCAT13(*puVar12,(int3)pvVar22);
        do {
          puVar13 = (undefined4 *)
                    thunk_FUN_0043c9b0((undefined4 *)&stack0x00000fc4,(int)ppuVar8,(int *)&local_4);
          puVar21 = (undefined4 *)&stack0x0000026c;
          for (iVar9 = 0xb5; iVar9 != 0; iVar9 = iVar9 + -1) {
            *puVar21 = *puVar13;
            puVar13 = puVar13 + 1;
            puVar21 = puVar21 + 1;
          }
          puVar13 = (undefined4 *)&stack0x0000026c;
          puVar21 = (undefined4 *)&stack0x00000670;
          for (iVar9 = 0xb5; iVar9 != 0; iVar9 = iVar9 + -1) {
            *puVar21 = *puVar13;
            puVar13 = puVar13 + 1;
            puVar21 = puVar21 + 1;
          }
          thunk_FUN_00427840(*(void **)((int)this + 0xdc),(short *)&stack0x00000670);
          cVar6 = (char)((uint)pvVar22 >> 0x18) + -1;
          pvVar22 = (void *)CONCAT13(cVar6,(int3)pvVar22);
        } while (cVar6 != '\0');
        thunk_FUN_004316c0(*(int *)((int)this + 0x170));
        DAT_00497bf0 = 1;
        break;
      case 0x36:
        puVar13 = (undefined4 *)thunk_FUN_0043ced0((short *)&stack0x0000156c,(int)ppuVar8 + 1);
        puVar21 = (undefined4 *)&stack0x0000026c;
        for (iVar9 = 0x100; iVar9 != 0; iVar9 = iVar9 + -1) {
          *puVar21 = *puVar13;
          puVar13 = puVar13 + 1;
          puVar21 = puVar21 + 1;
        }
        *(undefined2 *)puVar21 = *(undefined2 *)puVar13;
        puVar13 = (undefined4 *)&stack0x0000026c;
        puVar21 = (undefined4 *)&stack0x00000670;
        for (iVar9 = 0x100; iVar9 != 0; iVar9 = iVar9 + -1) {
          *puVar21 = *puVar13;
          puVar13 = puVar13 + 1;
          puVar21 = puVar21 + 1;
        }
        pvVar4 = *(void **)((int)this + 0xdc);
        *(undefined2 *)puVar21 = *(undefined2 *)puVar13;
        thunk_FUN_00427890(pvVar4,(short *)&stack0x00000670);
        break;
      case 0x38:
        puVar13 = (undefined4 *)thunk_FUN_0043cfa0((short *)&stack0x00000a74,(int)ppuVar8 + 1);
        puVar21 = (undefined4 *)&stack0x0000026c;
        for (iVar9 = 0x80; iVar9 != 0; iVar9 = iVar9 + -1) {
          *puVar21 = *puVar13;
          puVar13 = puVar13 + 1;
          puVar21 = puVar21 + 1;
        }
        *(undefined2 *)puVar21 = *(undefined2 *)puVar13;
        puVar13 = (undefined4 *)&stack0x0000026c;
        puVar21 = (undefined4 *)&stack0x00000670;
        for (iVar9 = 0x80; iVar9 != 0; iVar9 = iVar9 + -1) {
          *puVar21 = *puVar13;
          puVar13 = puVar13 + 1;
          puVar21 = puVar21 + 1;
        }
        pvVar4 = *(void **)((int)this + 0xdc);
        *(undefined2 *)puVar21 = *(undefined2 *)puVar13;
        thunk_FUN_00427970(pvVar4,(short *)&stack0x00000670);
        break;
      case 0x3a:
        param_2 = thunk_FUN_0043cf70((int)ppuVar8 + 1);
        thunk_FUN_00427950(*(void **)((int)this + 0xdc),&param_2);
        break;
      case 0x5b:
        thunk_FUN_00414640(this);
        break;
      case 0x6f:
        local_8 = (undefined1 *)0x1;
        pbVar14 = (byte *)thunk_FUN_0043c420((int)ppuVar8,(int *)&local_8);
        bVar1 = *pbVar14;
        param_2 = CONCAT31(param_2._1_3_,bVar1);
        pcVar15 = operator_new(bVar1 + 1);
        CString::CString((CString *)&stack0x00000000);
        thunk_FUN_0043c450((int)ppuVar8,(int *)&local_8,(undefined4 *)pcVar15,(uint)bVar1);
        CString::operator=((CString *)&stack0x00000000,pcVar15);
        operator_delete(pcVar15);
        iVar9 = 5;
        pcVar15 = operator_new(5);
        local_8 = (undefined1 *)0x0;
        uVar7 = thunk_FUN_0043beb0(unaff_retaddr,iVar9);
        thunk_FUN_0043b9a0((int)pcVar15,(int *)&local_8,0x6f);
        thunk_FUN_0043ba20((int)pcVar15,(int *)&local_8,uVar7);
        thunk_FUN_004132d0(this,(int)local_8,pcVar15);
        operator_delete(pcVar15);
        CString::~CString((CString *)&stack0x00000000);
        break;
      case 0x91:
        if ((((*(int **)((int)this + 0xf0) != (int *)0x0) &&
             (iVar9 = (**(code **)(**(int **)((int)this + 0xf0) + 4))(), iVar9 == 1)) &&
            (iVar9 = (**(code **)(**(int **)((int)this + 0xf0) + 0x9c))(), iVar9 != 0)) &&
           (*(int *)((int)this + 0x144) == 0)) goto LAB_00413cda;
        break;
      case 0xcf:
        local_8 = (undefined1 *)0x1;
        psVar10 = (short *)thunk_FUN_0043c3f0((int)ppuVar8,(int *)&local_8);
        param_2 = (uint)*psVar10;
        thunk_FUN_0043b5b0(0x4950f8);
        psVar10 = (short *)thunk_FUN_0043c3f0((int)ppuVar8,(int *)&local_8);
        sVar2 = *psVar10;
        psVar10 = (short *)thunk_FUN_0043c3f0((int)ppuVar8,(int *)&local_8);
        sVar3 = *psVar10;
        piVar11 = (int *)thunk_FUN_0043c4c0((int)ppuVar8,(int *)&local_8);
        param_4 = *piVar11;
        local_4 = (uint)sVar2;
        puVar13 = (undefined4 *)(local_8 + (int)ppuVar8);
        puVar21 = (undefined4 *)&stack0x00001970;
        for (uVar7 = local_4 >> 2; uVar7 != 0; uVar7 = uVar7 - 1) {
          *puVar21 = *puVar13;
          puVar13 = puVar13 + 1;
          puVar21 = puVar21 + 1;
        }
        for (uVar7 = local_4 & 3; uVar7 != 0; uVar7 = uVar7 - 1) {
          *(undefined1 *)puVar21 = *(undefined1 *)puVar13;
          puVar13 = (undefined4 *)((int)puVar13 + 1);
          puVar21 = (undefined4 *)((int)puVar21 + 1);
        }
        local_8 = local_8 + local_4;
        thunk_FUN_0043b6d0(&DAT_004950f8,local_4,(int)sVar3);
        puVar13 = (undefined4 *)&stack0x00001970;
        puVar21 = DAT_00495114;
        for (uVar7 = local_4 >> 2; uVar7 != 0; uVar7 = uVar7 - 1) {
          *puVar21 = *puVar13;
          puVar13 = puVar13 + 1;
          puVar21 = puVar21 + 1;
        }
        for (uVar7 = local_4 & 3; uVar7 != 0; uVar7 = uVar7 - 1) {
          *(undefined1 *)puVar21 = *(undefined1 *)puVar13;
          puVar13 = (undefined4 *)((int)puVar13 + 1);
          puVar21 = (undefined4 *)((int)puVar21 + 1);
        }
        bVar5 = thunk_FUN_0043b7b0(0x4950f8);
        iVar9 = DAT_00495118;
        if (CONCAT31(extraout_var,bVar5) == 0) {
          thunk_FUN_0043b7f0(0x4950f8);
          goto LAB_00413cda;
        }
        if (DAT_00495130 != 0) {
          thunk_FUN_0043b7f0(0x4950f8);
          goto LAB_00413cda;
        }
        if (DAT_0049510c != param_4) {
          thunk_FUN_0043b7f0(0x4950f8);
          goto LAB_00413cda;
        }
        if (sVar3 != DAT_00495128) {
          thunk_FUN_0043b7f0(0x4950f8);
          goto LAB_00413cda;
        }
        local_8 = (undefined1 *)0x0;
        param_3 = 0;
        if (0 < (int)param_2) {
          local_4 = param_2;
          do {
            puVar12 = (undefined1 *)thunk_FUN_0043c420(iVar9,&param_3);
            pvVar22 = (void *)CONCAT13(*puVar12,(int3)pvVar22);
            do {
              puVar13 = (undefined4 *)
                        thunk_FUN_0043c9b0((undefined4 *)&stack0x00001298,iVar9,&param_3);
              puVar21 = (undefined4 *)&stack0x0000026c;
              for (iVar18 = 0xb5; iVar18 != 0; iVar18 = iVar18 + -1) {
                *puVar21 = *puVar13;
                puVar13 = puVar13 + 1;
                puVar21 = puVar21 + 1;
              }
              puVar13 = (undefined4 *)&stack0x0000026c;
              puVar21 = (undefined4 *)&stack0x00000670;
              for (iVar18 = 0xb5; iVar18 != 0; iVar18 = iVar18 + -1) {
                *puVar21 = *puVar13;
                puVar13 = puVar13 + 1;
                puVar21 = puVar21 + 1;
              }
              thunk_FUN_00427840(*(void **)((int)this + 0xdc),(short *)&stack0x00000670);
              cVar6 = (char)((uint)pvVar22 >> 0x18) + -1;
              pvVar22 = (void *)CONCAT13(cVar6,(int3)pvVar22);
            } while (cVar6 != '\0');
            local_4 = local_4 - 1;
          } while (local_4 != 0);
        }
        DAT_00497bf0 = 1;
        thunk_FUN_0043b7f0(0x4950f8);
      }
      if ((*(int *)((int)this + 0x144) == 0) || (*(int **)((int)this + 0x148) == (int *)0x0)) {
        switch(*(undefined1 *)ppuVar8) {
        case 0x11:
          thunk_FUN_00415ba0(ppuVar8,unaff_EDI,unaff_ESI,unaff_EBP,unaff_EBX,pvVar22);
          thunk_FUN_004165f0();
          break;
        default:
          if (*(int **)((int)this + 0xf0) != (int *)0x0) {
            (**(code **)(**(int **)((int)this + 0xf0) + 0x9c))();
          }
          break;
        case 0x22:
          if (*(int *)((int)this + 0xd8) == 0) {
            if (*(int **)((int)this + 0xf0) != (int *)0x0) {
              (**(code **)(**(int **)((int)this + 0xf0) + 0x9c))();
            }
          }
          else if (*(char *)((int)ppuVar8 + 1) == '\x01') {
            pAVar16 = AfxGetModuleState();
            *(undefined2 *)(*(int *)(pAVar16 + 4) + 0xd2) = *(undefined2 *)((int)ppuVar8 + 3);
            pAVar16 = AfxGetModuleState();
            thunk_FUN_00418c80(*(CWinApp **)(pAVar16 + 4));
            thunk_FUN_004132a0();
          }
          break;
        case 0x2b:
          (**(code **)(**(int **)((int)this + 0xf0) + 0x88))();
          thunk_FUN_00414210(this,(int)ppuVar8);
          break;
        case 0x2d:
          thunk_FUN_004159a0(this,(int)ppuVar8);
          break;
        case 0x53:
          (**(code **)(**(int **)((int)this + 0xf0) + 0x88))();
          puVar13 = (undefined4 *)
                    thunk_FUN_0043d060((undefined4 *)&stack0x00000030,(int)ppuVar8 + 1);
          pcVar15 = &param_5;
          for (iVar9 = 7; iVar9 != 0; iVar9 = iVar9 + -1) {
            *(undefined4 *)pcVar15 = *puVar13;
            puVar13 = puVar13 + 1;
            pcVar15 = pcVar15 + 4;
          }
          *(undefined4 *)((int)this + 0x19c) = 0;
          if (param_5 == '\x01') {
            userclearstatusall();
            PostMessageA(*(HWND *)((int)this + 0x20),0x403,(int)param_6,0);
            *(int *)((int)this + 0x1a4) = param_9;
            if (param_9 != 0) {
              *(int *)((int)this + 0x1a8) = (int)(short)param_10;
              *(int *)((int)this + 0x1ac) = (int)param_10._2_2_;
              *(int *)((int)this + 0x1b0) = (int)(short)param_11;
              *(int *)((int)this + 0x1b4) = (int)param_11._2_2_;
            }
            *(undefined4 *)((int)this + 0x1b8) = 1;
          }
          break;
        case 0x55:
          (**(code **)(**(int **)((int)this + 0xf0) + 0x88))();
          puVar13 = (undefined4 *)
                    thunk_FUN_0043d210((undefined4 *)&stack0x00000da4,(int)ppuVar8 + 1);
          puVar21 = (undefined4 *)&stack0x00000a74;
          for (iVar9 = 0x88; iVar9 != 0; iVar9 = iVar9 + -1) {
            *puVar21 = *puVar13;
            puVar13 = puVar13 + 1;
            puVar21 = puVar21 + 1;
          }
          puVar13 = (undefined4 *)&stack0x00000a74;
          puVar21 = (undefined4 *)&stack0x0000004c;
          for (iVar9 = 0x88; iVar9 != 0; iVar9 = iVar9 + -1) {
            *puVar21 = *puVar13;
            puVar13 = puVar13 + 1;
            puVar21 = puVar21 + 1;
          }
          if (in_stack_00000a74 == '\x01') {
            CString::operator=((CString *)((int)this + 0x1a0),&stack0x0000005b);
            userclearstatusall();
            PostMessageA(*(HWND *)((int)this + 0x20),0x403,(int)in_stack_0000004e,0);
            *(int *)((int)this + 0x1a4) = in_stack_0000025c;
            if (in_stack_0000025c != 0) {
              *(int *)((int)this + 0x1a8) = (int)in_stack_00000260;
              *(int *)((int)this + 0x1ac) = (int)in_stack_00000262;
              *(int *)((int)this + 0x1b0) = (int)in_stack_00000264;
              *(int *)((int)this + 0x1b4) = (int)in_stack_00000266;
            }
            *(int *)((int)this + 0x1b8) = (int)in_stack_00000268;
          }
          break;
        case 0x7b:
          if (*(char *)((int)ppuVar8 + 1) == '\b') {
            if ((*(int *)(*(int *)((int)this + 0xf0) + 0x124) == 0) ||
               (*(int *)((int)this + 0xd8) != 0)) {
              *(undefined4 *)((int)this + 0xd8) = 0;
              thunk_FUN_00411850(this,-1,-1);
            }
            else {
              thunk_FUN_00412460(this,2);
            }
          }
          break;
        case 0x7c:
          thunk_FUN_00414490(this,(undefined2 *)((int)ppuVar8 + 1));
        }
      }
      else {
        (**(code **)(**(int **)((int)this + 0x148) + 0x9c))();
      }
    }
  }
LAB_00413cda:
  operator_delete(ppuVar8);
  ExceptionList = in_stack_00003974;
  return;
}

