/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x10007d00; source SHA256 08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3
 */

/* Original StartPosAndValidSpriteInit. Set four cached view coordinates to -100, delete the prior
   visible-sprite index buffer, and allocate a new index buffer of mapSpriteCount*4 bytes. This is
   map rendering cache initialization, not monster spawn placement.
   Source SHA256: 08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3 */

void __thiscall BBDX_MapManager::ResetVisibleSpriteCache(BBDX_MapManager *this)

{
  void *pvVar1;
  
                    /* 0x7d00  195  ?StartPosAndValidSpriteInit@BBDX_MapManager@@QAEXXZ */
  *(undefined4 *)(this + 0x70) = 0xffffff9c;
  *(undefined4 *)(this + 0x78) = 0xffffff9c;
  *(undefined4 *)(this + 0x74) = 0xffffff9c;
  *(undefined4 *)(this + 0x7c) = 0xffffff9c;
  if (*(void **)(this + 0x68) != (void *)0x0) {
    operator_delete(*(void **)(this + 0x68));
    *(undefined4 *)(this + 0x68) = 0;
  }
  pvVar1 = operator_new(*(int *)(*(int *)(this + 0x4c) + 0x38) << 2);
  *(void **)(this + 0x68) = pvVar1;
  return;
}

