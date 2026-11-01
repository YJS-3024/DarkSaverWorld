/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x10007d00
 * Evidence: ev_026ea85466b9c0f3b81a7926348c1536c8f2943e86aaa4702ebb10ff0ae6a0ae
 */

/* public: void __thiscall BBDX_MapManager::StartPosAndValidSpriteInit(void) */

void __thiscall BBDX_MapManager::ResetVisibleSpriteCache(BBDX_MapManager *this)

{
  void *pvVar1;
  
                    /* Original StartPosAndValidSpriteInit. Set four cached view coordinates to
                       -100, delete the prior visible-sprite index buffer, and allocate a new index
                       buffer of mapSpriteCount*4 bytes. This is map rendering cache initialization,
                       not monster spawn placement. */
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

