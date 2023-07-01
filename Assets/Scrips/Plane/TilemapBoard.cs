using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Tilemaps;


public class TilemapBoard : MonoBehaviour
{
    public int CellMaxWidth => tilemapBoard?.cellBounds.size.x ?? -1;
    public int CellMaxHeight => tilemapBoard?.cellBounds.size.y ?? -1;

    private List<Tilemap> _tilemapList;

    private Tilemap tilemapBoard;
    private Tilemap tilemapBlock;

    public Vector3 MaxSize => tilemapBoard.localBounds.max;
    public Vector3 MinSize => tilemapBoard.localBounds.min;
    public Vector3 Center => tilemapBoard.localBounds.center;

    private void Awake()
    {
        if (_tilemapList is null)
        {
            _tilemapList = GetComponentsInChildren<Tilemap>().ToList();

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
        }
    }

    public PlanePathNode[,] InitPlane()
    {
        if (tilemapBoard is null)
            return new PlanePathNode[0,0];
        
        var bounds = tilemapBoard.cellBounds;
        var pathNodes = new PlanePathNode[bounds.size.x, bounds.size.y];

        for (int y = bounds.yMin, posY = 0; y < bounds.yMax; y++, posY++)
        {
            for (int x = bounds.xMin, posX = 0; x < bounds.xMax; x++, posX++)
            {
                var node = new PlanePathNode(x, y)
                {
                    indexX = posX,
                    indexY = posY,
                    costTotal = int.MaxValue,
                    pParent = null,
                    centerPos = tilemapBoard.CellToWorld(new Vector3Int(x, y, 0)),
                    isMoveAble = !tilemapBlock.HasTile(new Vector3Int(x, y, 0))
                };

                pathNodes[posX, posY] = node;
            }
        }

        return pathNodes;
    }


    public Vector3Int GetPlanePosWorld(Vector3 worldPos)
    {
        if (tilemapBoard is null)
            return Vector3Int.zero;

        return tilemapBoard.WorldToCell(worldPos);
    }

    public bool GetIsMove(Vector3Int pos)
    {
        return !tilemapBlock.HasTile(pos);
    }
}
