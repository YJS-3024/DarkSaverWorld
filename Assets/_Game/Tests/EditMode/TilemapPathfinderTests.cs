using System.Collections.Generic;
using DarkSaver.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkSaver.Tests
{
    public sealed class TilemapPathfinderTests
    {
        private GameObject gridObject;
        private Tilemap ground;
        private Tilemap terrain;
        private Tile floorTile;
        private Tile wallTile;

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("Pathfinding Test Grid", typeof(Grid));
            ground = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer))
                .GetComponent<Tilemap>();
            terrain = new GameObject("TerrainAttributes", typeof(Tilemap), typeof(TilemapRenderer))
                .GetComponent<Tilemap>();
            ground.transform.SetParent(gridObject.transform);
            terrain.transform.SetParent(gridObject.transform);
            floorTile = ScriptableObject.CreateInstance<Tile>();
            floorTile.colliderType = Tile.ColliderType.None;
            wallTile = ScriptableObject.CreateInstance<Tile>();
            wallTile.colliderType = Tile.ColliderType.Grid;
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
                ground.SetTile(new Vector3Int(x, y), floorTile);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(floorTile);
            Object.DestroyImmediate(wallTile);
        }

        [Test]
        public void FindsDirectDiagonalPath()
        {
            var waypoints = new List<Vector3>();

            Assert.IsTrue(TilemapPathfinder.TryFind(ground, terrain,
                ground.GetCellCenterWorld(new Vector3Int(0, 0)),
                ground.GetCellCenterWorld(new Vector3Int(1, 1)), waypoints));
            Assert.AreEqual(1, waypoints.Count);
            Assert.AreEqual(new Vector3Int(1, 1), ground.WorldToCell(waypoints[0]));
        }

        [Test]
        public void DiagonalDoesNotCutBlockedCorner()
        {
            terrain.SetTile(new Vector3Int(1, 0), wallTile);
            var waypoints = new List<Vector3>();

            Assert.IsTrue(TilemapPathfinder.TryFind(ground, terrain,
                ground.GetCellCenterWorld(new Vector3Int(0, 0)),
                ground.GetCellCenterWorld(new Vector3Int(1, 1)), waypoints));
            Assert.AreEqual(2, waypoints.Count);
            Assert.AreEqual(new Vector3Int(0, 1), ground.WorldToCell(waypoints[0]));
        }

        [Test]
        public void FindsDetourAroundBlockedTile()
        {
            terrain.SetTile(new Vector3Int(1, 1), wallTile);
            var waypoints = new List<Vector3>();

            var found = TilemapPathfinder.TryFind(ground, terrain,
                ground.GetCellCenterWorld(new Vector3Int(0, 1)),
                ground.GetCellCenterWorld(new Vector3Int(2, 1)), waypoints);

            Assert.IsTrue(found);
            Assert.AreEqual(4, waypoints.Count);
            foreach (var waypoint in waypoints)
                Assert.AreNotEqual(new Vector3Int(1, 1), ground.WorldToCell(waypoint));
        }

        [Test]
        public void RejectsBlockedDestination()
        {
            terrain.SetTile(new Vector3Int(1, 1), wallTile);
            var waypoints = new List<Vector3>();

            Assert.IsFalse(TilemapPathfinder.TryFind(ground, terrain,
                ground.GetCellCenterWorld(new Vector3Int(0, 1)),
                ground.GetCellCenterWorld(new Vector3Int(1, 1)), waypoints));
        }
    }
}
