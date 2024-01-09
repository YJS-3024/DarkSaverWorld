using System.Linq;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Tilemaps;


public partial class TilemapManager : MonoSingleton<TilemapManager>
{
    public int CellMaxWidth => tilemapBoard?.cellBounds.size.x ?? -1;
    public int CellMaxHeight => tilemapBoard?.cellBounds.size.y ?? -1;

    private List<Tilemap> _tilemapList;
    private PlanePathNode[,] _planePathNodes;

    private Tilemap tilemapBoard;
    private Tilemap tilemapBlock;

    public Vector3 MaxSize => tilemapBoard?.localBounds.max ?? Vector3.zero;
    public Vector3 MinSize => tilemapBoard?.localBounds.min ?? Vector3.zero;
    public Vector3 Center => tilemapBoard?.localBounds.center ?? Vector3.zero;

    protected override void Destroy()
    {
        
    }

    public override bool Initialize()
    {
        Reset();

        return true;
    }

    public void Reset()
    {
        if (_tilemapList is null || 
            _tilemapList.Count == 0)
        {
            _tilemapList = FindObjectsOfType<Tilemap>().ToList();

            tilemapBoard = _tilemapList.FirstOrDefault(x=>x.gameObject.layer == (int)eLayer.Field_Board);
             if (tilemapBoard != null)
            {
                tilemapBoard.CompressBounds();
            }

            tilemapBlock = _tilemapList.FirstOrDefault(x=>x.gameObject.layer == (int)eLayer.Field_Block);
            if (tilemapBlock != null)
            {
                tilemapBlock.CompressBounds();
            }


            if (tilemapBoard != null)
            {
                var bounds = tilemapBoard.cellBounds;
                _planePathNodes = new PlanePathNode[bounds.size.x, bounds.size.y];

                for (int y = bounds.yMin, posY = 0; y < bounds.yMax; y++, posY++)
                {
                    for (int x = bounds.xMin, posX = 0; x < bounds.xMax; x++, posX++)
                    {
                        var pos = new Vector3Int(x, y, 0);
                        var node = new PlanePathNode(x, y)
                        {
                            indexX = posX,
                            indexY = posY,
                            costTotal = int.MaxValue,
                            pParent = null,
                            centerPos = tilemapBoard.CellToWorld(pos),
                            isMoveAble = !tilemapBlock?.HasTile(pos) ?? true,
                        };

                        _planePathNodes[posX, posY] = node;
                    }
                }
            }
        }
    }

    public void Clear()
    {
        tilemapBoard.ClearAllTiles();
        tilemapBoard = null;

        _planePathNodes = null;
    }

    public PlanePathNode GetNode(int x, int y)
    {
        return _planePathNodes[x, y];
    }

    public PlanePathNode GetNode_WorldPos(Vector3 pos)
    {
        if (tilemapBoard is null)
            return null;

        var vec3Int = tilemapBoard.WorldToCell(pos);
        foreach (var node in _planePathNodes)
        {
            if (node.centerPos.x.Equals(vec3Int.x) &&
                node.centerPos.y.Equals(vec3Int.y))
            {
                return node;
            }
        }

        return null;
    }

    public bool IsMove(int posX, int posY)
    {
        var posInt = new Vector3Int(posX, posY, 0);
        return tilemapBoard.HasTile(posInt) && !tilemapBlock.HasTile(posInt);
    }

    /// <summary>
    /// 해당위치에 캐릭터가 서있는가
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool IsStandChar(Vector3 pos)
    {
        return PlayerManager.I.MainPlayer.GetNodePos == pos;
    }

    public bool IsStandEnemy(Vector3 pos)
    {
        return EnemyManager.I.GetIsEnemy(pos);
    }
}
