
/* public: void __thiscall BBDX_MapManager::TileChange(int,int,unsigned char,unsigned char,unsigned
   char,unsigned char,unsigned char) */

void __thiscall
BBDX_MapManager::TileChange
          (BBDX_MapManager *this,int param_1,int param_2,uchar param_3,uchar param_4,uchar param_5,
          uchar param_6,uchar param_7)

{
  TILE *pTVar1;
  
                    /* 0x9340  202  ?TileChange@BBDX_MapManager@@QAEXHHEEEEE@Z */
  pTVar1 = BB_Map::GetTilePtr(*(BB_Map **)(this + 0x4c),&param_1,&param_2);
  if (pTVar1 != (TILE *)0x0) {
    *pTVar1 = (TILE)param_3;
    pTVar1[1] = (TILE)param_4;
    pTVar1[2] = (TILE)param_5;
    pTVar1[3] = (TILE)param_6;
    pTVar1[4] = (TILE)param_7;
  }
  return;
}

