
void __thiscall FUN_00422500(void *this,CDDSurface *param_1,int param_2,int param_3)

{
  char *pcVar1;
  byte bVar2;
  LONG LVar3;
  undefined4 uVar4;
  __POSITION *p_Var5;
  int iVar6;
  byte *pbVar7;
  char *pcVar8;
  BOOL BVar9;
  int iVar10;
  int *this_00;
  undefined4 *puVar11;
  byte *pbVar12;
  char *pcVar13;
  _DDSURFACEDESC2 *p_Var14;
  undefined4 *puVar15;
  bool bVar16;
  int iVar17;
  undefined4 auStack_360 [30];
  undefined4 uStack_2e8;
  char cStack_2b5;
  uint local_2b4;
  byte *local_2b0;
  tagRECT local_2ac;
  int local_29c;
  RECT local_298;
  tagRECT local_288;
  int local_278;
  int local_274;
  int local_270;
  char *local_26c;
  byte *local_268;
  int local_264;
  int local_260;
  char *local_25c;
  byte *local_258;
  CString local_254 [4];
  int local_250;
  int local_24c;
  char *local_248;
  byte *local_244 [2];
  tagRECT local_23c;
  tagRECT local_22c;
  undefined1 local_21c [8];
  char *local_214;
  byte *local_210;
  CString local_20c [4];
  undefined1 local_208 [4];
  __POSITION *local_204;
  undefined4 *local_200;
  _DDSURFACEDESC2 local_1fc [36];
  void *local_1d8;
  undefined4 auStack_180 [31];
  undefined4 auStack_104 [31];
  undefined4 local_88 [31];
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0047c981;
  local_c = ExceptionList;
  local_288.left = *(int *)((int)this + 0x38);
  local_288.top = *(int *)((int)this + 0x3c);
  local_288.right = *(int *)((int)this + 0x40);
  local_288.bottom = *(LONG *)((int)this + 0x44);
  ExceptionList = &local_c;
  OffsetRect(&local_288,param_2,param_3);
  puVar11 = (undefined4 *)0x0;
  if (*(int *)((int)this + 4) != 0) {
    CRect::NormalizeRect((CRect *)&local_288);
    CString::CString((CString *)&local_2b0);
    iVar6 = *(int *)((int)this + 0x4c);
    iVar10 = 0;
    local_4 = 0;
    if (iVar6 < 2) {
      if (0 < iVar6) {
        iVar10 = 1;
      }
    }
    else {
      iVar10 = 2;
    }
    local_278 = *(int *)((int)this + 0x28);
    p_Var5 = CStringList::FindIndex((CStringList *)((int)this + 0x1c),iVar6 - iVar10);
    iVar6 = *(int *)((int)this + 0x4c) - iVar10;
    if ((iVar6 < *(int *)((int)this + 100)) && (-1 < iVar6)) {
      puVar11 = *(undefined4 **)((int)this + 0x5c);
      for (; iVar6 != 0; iVar6 = iVar6 + -1) {
        puVar11 = (undefined4 *)*puVar11;
      }
    }
    CDDSurface::GetDescription(param_1,local_1fc);
    local_1d8 = CDDSurface::Lock(param_1);
    local_29c = *(int *)((int)this + 0x4c) - iVar10;
    if (local_29c < local_278) {
      do {
        if (p_Var5 == (__POSITION *)0x0) break;
        local_200 = (undefined4 *)*puVar11;
        local_204 = *(__POSITION **)p_Var5;
        iVar6 = puVar11[2];
        CString::operator=((CString *)&local_2b0,(CString *)(p_Var5 + 8));
        if (iVar6 == 9) {
          CString::CString(local_254,'>',1);
          local_4 = CONCAT31(local_4._1_3_,1);
          puVar11 = (undefined4 *)CString::Left((CString *)&local_2b0,(int)local_208);
          pbVar7 = (byte *)*puVar11;
          pbVar12 = local_258;
          do {
            bVar2 = *pbVar7;
            bVar16 = bVar2 < *pbVar12;
            if (bVar2 != *pbVar12) {
LAB_004226ad:
              iVar6 = (1 - (uint)bVar16) - (uint)(bVar16 != 0);
              goto LAB_004226b2;
            }
            if (bVar2 == 0) break;
            bVar2 = pbVar7[1];
            bVar16 = bVar2 < pbVar12[1];
            if (bVar2 != pbVar12[1]) goto LAB_004226ad;
            pbVar7 = pbVar7 + 2;
            pbVar12 = pbVar12 + 2;
          } while (bVar2 != 0);
          iVar6 = 0;
LAB_004226b2:
          CString::~CString(local_20c);
          local_8 = (undefined1 *)((uint)local_8 & 0xffffff00);
          CString::~CString((CString *)&local_258);
          if (iVar6 != 0) {
            CString::CString((CString *)local_244,'<',1);
            local_8 = (undefined1 *)CONCAT31(local_8._1_3_,2);
            puVar11 = (undefined4 *)CString::Left((CString *)&local_2b4,(int)&local_210);
            pcVar8 = (char *)*puVar11;
            pcVar13 = local_248;
            do {
              if (((*pcVar8 != *pcVar13) || (*pcVar8 == '\0')) ||
                 (pcVar1 = pcVar8 + 1, *pcVar1 != pcVar13[1])) break;
              pcVar8 = pcVar8 + 2;
              pcVar13 = pcVar13 + 2;
            } while (*pcVar1 != '\0');
            CString::~CString((CString *)&local_214);
            local_c = (void *)((uint)local_c & 0xffffff00);
            CString::~CString((CString *)&local_248);
          }
          if (*(int *)((int)this + 0x4c) <= local_29c) {
            uStack_2e8 = 0x4227ab;
            SetRect(&local_2ac,local_288.left,
                    (local_29c - *(int *)((int)this + 0x4c)) *
                    (*(int *)((int)this + 0x48) + *(int *)((int)this + 0x10)) + local_288.top,
                    local_288.right,0);
            local_2ac.bottom =
                 *(int *)((int)this + 0x48) + local_2ac.top + *(int *)((int)this + 0x10);
            CRect::NormalizeRect((CRect *)&local_2ac);
            local_298.left = local_2ac.left;
            local_298.top = local_2ac.top;
            local_298.right = local_2ac.right;
            local_298.bottom = local_2ac.bottom;
            IntersectRect(&local_298,&local_298,&local_288);
            BVar9 = EqualRect(&local_298,&local_2ac);
            pbVar7 = local_2b0;
            if (BVar9 == 0) break;
            if (cStack_2b5 == '>') {
              this_00 = *(int **)((int)this + 4);
              local_274 = local_2ac.left;
              p_Var14 = local_1fc;
              puVar11 = auStack_104;
              for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
                *puVar11 = *(undefined4 *)p_Var14;
                p_Var14 = p_Var14 + 4;
                puVar11 = puVar11 + 1;
              }
              local_22c.left = local_2ac.left;
              local_2b4 = CONCAT22(local_2b4._2_2_,*(undefined2 *)((int)this + 0x76));
              local_270 = local_2ac.top;
              local_26c = (char *)local_2ac.right;
              local_268 = (byte *)local_2ac.bottom;
              local_22c.top = local_2ac.top;
              local_22c.right = local_2ac.right;
              local_22c.bottom = local_2ac.bottom;
              OffsetRect(&local_22c,1,1);
              LVar3 = local_22c.top;
              if (*this_00 == 0) {
                puVar11 = auStack_104;
                puVar15 = auStack_360;
                for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
                  *puVar15 = *puVar11;
                  puVar11 = puVar11 + 1;
                  puVar15 = puVar15 + 1;
                }
                thunk_FUN_0043ea00(this_00,local_22c.left,LVar3,local_22c.right,local_22c.bottom,
                                   pbVar7,0);
                if (*this_00 == 0) {
                  puVar11 = auStack_104;
                  puVar15 = auStack_360;
                  for (iVar6 = 0x1f; pbVar12 = local_268, iVar10 = local_274, iVar17 = local_270,
                      pcVar8 = local_26c, iVar6 != 0; iVar6 = iVar6 + -1) {
                    *puVar15 = *puVar11;
                    puVar11 = puVar11 + 1;
                    puVar15 = puVar15 + 1;
                  }
LAB_00422daf:
                  thunk_FUN_0043ea00(this_00,iVar10,iVar17,(int)pcVar8,(int)pbVar12,pbVar7,local_2b4
                                    );
                  goto LAB_00422db9;
                }
              }
              puVar11 = auStack_104;
              puVar15 = auStack_360;
              for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
                *puVar15 = *puVar11;
                puVar11 = puVar11 + 1;
                puVar15 = puVar15 + 1;
              }
              thunk_FUN_0043e390(this_00,local_274,local_270,(int)local_26c,(int)local_268,pbVar7,
                                 local_2b4);
            }
            else {
              this_00 = *(int **)((int)this + 4);
              local_264 = local_2ac.left;
              p_Var14 = local_1fc;
              puVar11 = auStack_180;
              for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
                *puVar11 = *(undefined4 *)p_Var14;
                p_Var14 = p_Var14 + 4;
                puVar11 = puVar11 + 1;
              }
              local_21c._0_4_ = local_2ac.left;
              local_2b4 = CONCAT22(local_2b4._2_2_,*(undefined2 *)((int)this + 0x7c));
              local_260 = local_2ac.top;
              local_25c = (char *)local_2ac.right;
              local_258 = (byte *)local_2ac.bottom;
              local_21c._4_4_ = local_2ac.top;
              local_214 = (char *)local_2ac.right;
              local_210 = (byte *)local_2ac.bottom;
              OffsetRect((LPRECT)local_21c,1,1);
              uVar4 = local_21c._4_4_;
              if (*this_00 == 0) {
                puVar11 = auStack_180;
                puVar15 = auStack_360;
                for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
                  *puVar15 = *puVar11;
                  puVar11 = puVar11 + 1;
                  puVar15 = puVar15 + 1;
                }
                thunk_FUN_0043ea00(this_00,local_21c._0_4_,uVar4,(int)local_214,(int)local_210,
                                   pbVar7,0);
                if (*this_00 == 0) {
                  puVar11 = auStack_180;
                  puVar15 = auStack_360;
                  for (iVar6 = 0x1f; pbVar12 = local_258, iVar10 = local_264, iVar17 = local_260,
                      pcVar8 = local_25c, iVar6 != 0; iVar6 = iVar6 + -1) {
                    *puVar15 = *puVar11;
                    puVar11 = puVar11 + 1;
                    puVar15 = puVar15 + 1;
                  }
                  goto LAB_00422daf;
                }
              }
              puVar11 = auStack_180;
              puVar15 = auStack_360;
              for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
                *puVar15 = *puVar11;
                puVar11 = puVar11 + 1;
                puVar15 = puVar15 + 1;
              }
              thunk_FUN_0043e390(this_00,local_264,local_260,(int)local_25c,(int)local_258,pbVar7,
                                 local_2b4);
            }
          }
        }
        else if (*(int *)((int)this + 0x4c) <= local_29c) {
          uStack_2e8 = 0x422ba0;
          SetRect(&local_2ac,local_288.left,
                  (local_29c - *(int *)((int)this + 0x4c)) *
                  (*(int *)((int)this + 0x48) + *(int *)((int)this + 0x10)) + local_288.top,
                  local_288.right,0);
          local_2ac.bottom = *(int *)((int)this + 0x48) + local_2ac.top + *(int *)((int)this + 0x10)
          ;
          CRect::NormalizeRect((CRect *)&local_2ac);
          local_298.left = local_2ac.left;
          local_298.top = local_2ac.top;
          local_298.right = local_2ac.right;
          local_298.bottom = local_2ac.bottom;
          IntersectRect(&local_298,&local_298,&local_288);
          BVar9 = EqualRect(&local_298,&local_2ac);
          pbVar7 = local_2b0;
          if (BVar9 == 0) break;
          local_250 = local_2ac.left;
          p_Var14 = local_1fc;
          puVar11 = local_88;
          for (iVar10 = 0x1f; iVar10 != 0; iVar10 = iVar10 + -1) {
            *puVar11 = *(undefined4 *)p_Var14;
            p_Var14 = p_Var14 + 4;
            puVar11 = puVar11 + 1;
          }
          this_00 = *(int **)((int)this + 4);
          local_23c.left = local_2ac.left;
          local_2b4 = CONCAT22(local_2b4._2_2_,*(undefined2 *)((int)this + iVar6 * 2 + 0x74));
          local_24c = local_2ac.top;
          local_248 = (char *)local_2ac.right;
          local_244[0] = (byte *)local_2ac.bottom;
          local_23c.top = local_2ac.top;
          local_23c.right = local_2ac.right;
          local_23c.bottom = local_2ac.bottom;
          OffsetRect(&local_23c,1,1);
          LVar3 = local_23c.top;
          if (*this_00 == 0) {
            puVar11 = local_88;
            puVar15 = auStack_360;
            for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
              *puVar15 = *puVar11;
              puVar11 = puVar11 + 1;
              puVar15 = puVar15 + 1;
            }
            thunk_FUN_0043ea00(this_00,local_23c.left,LVar3,local_23c.right,local_23c.bottom,pbVar7,
                               0);
            if (*this_00 == 0) {
              puVar11 = local_88;
              puVar15 = auStack_360;
              for (iVar6 = 0x1f; pbVar12 = local_244[0], iVar10 = local_250, iVar17 = local_24c,
                  pcVar8 = local_248, iVar6 != 0; iVar6 = iVar6 + -1) {
                *puVar15 = *puVar11;
                puVar11 = puVar11 + 1;
                puVar15 = puVar15 + 1;
              }
              goto LAB_00422daf;
            }
          }
          puVar11 = local_88;
          puVar15 = auStack_360;
          for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
            *puVar15 = *puVar11;
            puVar11 = puVar11 + 1;
            puVar15 = puVar15 + 1;
          }
          thunk_FUN_0043e390(this_00,local_250,local_24c,(int)local_248,(int)local_244[0],pbVar7,
                             local_2b4);
        }
LAB_00422db9:
        local_29c = local_29c + 1;
        puVar11 = local_200;
        p_Var5 = local_204;
      } while (local_29c < local_278);
    }
    CDDSurface::Unlock(param_1);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_2b0);
  }
  ExceptionList = local_c;
  return;
}

