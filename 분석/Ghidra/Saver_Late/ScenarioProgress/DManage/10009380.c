
/* public: void __thiscall BBDX_MapManager::SpriteChange(int,int,unsigned char,unsigned
   char,unsigned char,unsigned char) */

void __thiscall
BBDX_MapManager::SpriteChange
          (BBDX_MapManager *this,int param_1,int param_2,uchar param_3,uchar param_4,uchar param_5,
          uchar param_6)

{
  int iVar1;
  int iVar2;
  int *piVar3;
  int iVar4;
  int iVar5;
  int iVar6;
  int iVar7;
  undefined3 in_stack_0000000d;
  undefined3 in_stack_00000011;
  undefined3 in_stack_00000019;
  undefined4 local_8 [2];
  
  iVar5 = param_2;
                    /* 0x9380  193  ?SpriteChange@BBDX_MapManager@@QAEXHHEEEE@Z */
  iVar6 = 0;
  iVar1 = *(int *)(*(int *)(this + 0x4c) + 0x38);
  if (0 < iVar1) {
    while (((iVar2 = BB_Map::GetSpriteData(*(BB_Map **)(this + 0x4c),iVar6,(SPRITE **)&param_2),
            iVar2 == 0 || (param_1 != *(int *)(param_2 + 4))) || (iVar5 != *(int *)(param_2 + 8))))
    {
      iVar6 = iVar6 + 1;
      if (iVar1 <= iVar6) {
        return;
      }
    }
    if ((_param_3 & 0xff) != 0xffffffff) {
      *(char *)param_2 = (char)_param_3;
    }
    *(uchar *)(param_2 + 0xc) = param_4;
    *(uchar *)(param_2 + 0xd) = param_5;
    piVar3 = (int *)FUN_1000a600(*(void **)(*(int *)(this + 0x3c) + (uint)*(byte *)param_2 * 4),
                                 local_8,(uint)*(byte *)(param_2 + 0xc));
    iVar1 = piVar3[1];
    iVar5 = *(int *)(param_2 + 4);
    iVar6 = *(int *)(param_2 + 8);
    iVar2 = ((int)(*piVar3 + (*piVar3 >> 0x1f & 0x1fU)) >> 5) + iVar5;
    iVar4 = param_2;
    _param_4 = iVar2;
    if (iVar5 < iVar2) {
      do {
        iVar7 = *(int *)(iVar4 + 8);
        for (; iVar7 < iVar6 + ((int)(iVar1 + (iVar1 >> 0x1f & 0x1fU)) >> 5); iVar7 = iVar7 + 1) {
          TileAttrChange(this,iVar5,iVar7,(uchar)_param_6);
          iVar2 = _param_4;
          iVar4 = param_2;
        }
        iVar5 = iVar5 + 1;
      } while (iVar5 < iVar2);
    }
  }
  return;
}

