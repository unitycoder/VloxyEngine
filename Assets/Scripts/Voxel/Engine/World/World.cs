﻿using System;
using System.Collections.Generic;

using CodeBlaze.Voxel.Engine.Chunk;
using CodeBlaze.Voxel.Engine.Meshing.Coordinator;
using CodeBlaze.Voxel.Engine.Settings;

using UnityEngine;

namespace CodeBlaze.Voxel.Engine.World {

    public class World<B> : MonoBehaviour where B : IBlock {

        [SerializeField] private Transform _focus;
        [SerializeField] private VoxelSettings _settings;

        public ChunkPool<B> ChunkPool { get; private set; }
        protected MeshBuildCoordinator<B> BuildCoordinator;
        
        protected Dictionary<Vector3Int, Chunk<B>> Chunks;
        protected Vector3Int FocusChunkCoord;

        private WorldSettings _worldSettings;

        #region Virtual

        protected virtual Func<VoxelProvider<B>> Provider() => () => new VoxelProvider<B>();
        protected virtual void WorldAwake() { }
        protected virtual void WorldStart() { }
        protected virtual void WorldUpdate() { }
        protected virtual void WorldChunkPoolUpdate() { }

        #endregion

        #region Unity

        private void Awake() {
            VoxelProvider<B>.Initialize(Provider(), _settings);

            _worldSettings = VoxelProvider<B>.Current.Settings.World;

            Chunks = new Dictionary<Vector3Int, Chunk<B>>();
            
            ChunkPool = VoxelProvider<B>.Current.ChunkPool(transform);
            BuildCoordinator = VoxelProvider<B>.Current.MeshBuildCoordinator(this);

            FocusChunkCoord = _focus != null
                ? GetChunkCoords(_focus.position)
                : Vector3Int.zero;
            
            WorldAwake();
        }

        private void Start() {
            for (int x = -_worldSettings.ChunkPageSize; x <= _worldSettings.ChunkPageSize; x++) {
                for (int z = -_worldSettings.ChunkPageSize; z <= _worldSettings.ChunkPageSize; z++) {
                    var pos = new Vector3Int(x, 0, z) * _worldSettings.ChunkSize;
                    Chunks.Add(pos, VoxelProvider<B>.Current.CreateChunk(pos));
                }
            }

            ChunkPoolUpdate();
            
            Debug.Log("[World][Start] Done");
            
            WorldStart();
        }

        private void Update() {
            var coords = GetChunkCoords(_focus.position);
            
            WorldUpdate();

            if (coords.x == FocusChunkCoord.x && coords.z == FocusChunkCoord.z) return;

            FocusChunkCoord = coords;
                
            // update
            ChunkPoolUpdate();
        }
        
        #endregion

        #region Neighbors
        
        public NeighborChunks<B> GetNeighbors(Chunk<B> chunk) {
            var position = chunk.Position;

            var px = position + Vector3Int.right * _worldSettings.ChunkSize;
            var py = position + Vector3Int.up * _worldSettings.ChunkSize;
            var pz = position + new Vector3Int(0, 0, 1) * _worldSettings.ChunkSize;
            var nx = position + Vector3Int.left * _worldSettings.ChunkSize;
            var ny = position + Vector3Int.down * _worldSettings.ChunkSize;
            var nz = position + new Vector3Int(0, 0, -1) * _worldSettings.ChunkSize;
            
            return new NeighborChunks<B> {
                ChunkPX = Chunks.ContainsKey(px) ? Chunks[px] : null,
                ChunkPY = Chunks.ContainsKey(py) ? Chunks[py] : null,
                ChunkPZ = Chunks.ContainsKey(pz) ? Chunks[pz] : null,
                ChunkNX = Chunks.ContainsKey(nx) ? Chunks[nx] : null,
                ChunkNY = Chunks.ContainsKey(ny) ? Chunks[ny] : null,
                ChunkNZ = Chunks.ContainsKey(nz) ? Chunks[nz] : null
            };
        }

        public Chunk<B> GetNeighborPX(Chunk<B> chunk) {
            var px = chunk.Position + Vector3Int.right * _worldSettings.ChunkSize;

            return Chunks.ContainsKey(px) ? Chunks[px] : null;
        }
        
        public Chunk<B> GetNeighborPY(Chunk<B> chunk) {
            var py = chunk.Position + Vector3Int.up * _worldSettings.ChunkSize;

            return Chunks.ContainsKey(py) ? Chunks[py] : null;
        }
        
        public Chunk<B> GetNeighborPZ(Chunk<B> chunk) {
            var pz = chunk.Position + new Vector3Int(0, 0, 1) * _worldSettings.ChunkSize;

            return Chunks.ContainsKey(pz) ? Chunks[pz] : null;
        }
        
        public Chunk<B> GetNeighborNX(Chunk<B> chunk) {
            var nx = chunk.Position + Vector3Int.left * _worldSettings.ChunkSize;

            return Chunks.ContainsKey(nx) ? Chunks[nx] : null;
        }
        
        public Chunk<B> GetNeighborNY(Chunk<B> chunk) {
            var ny = chunk.Position + Vector3Int.down * _worldSettings.ChunkSize;

            return Chunks.ContainsKey(ny) ? Chunks[ny] : null;
        }
        
        public Chunk<B> GetNeighborNZ(Chunk<B> chunk) {
            var nz = chunk.Position + new Vector3Int(0, 0, -1) * _worldSettings.ChunkSize;

            return Chunks.ContainsKey(nz) ? Chunks[nz] : null;
        }
        
        #endregion

        #region Utils

        public Vector3Int GetChunkCoords(Vector3 Position) {
            var pos = Vector3Int.FloorToInt(Position);

            var x = pos.x - pos.x % _worldSettings.ChunkSize.x;
            var y = 0; //pos.y - pos.y % WorldSettings.ChunkSize.y;
            var z = pos.z - pos.z % _worldSettings.ChunkSize.z;

            x = pos.x < 0 ? x - _worldSettings.ChunkSize.x : x;
            //y = pos.y < 0 ? y - WorldSettings.ChunkSize.y : y;
            z = pos.z < 0 ? z - _worldSettings.ChunkSize.z : z;
            
            return new Vector3Int(x,y,z);
        }
        
        public Vector3Int GetChunkCoords(Vector3Int Position) {
            var x = Position.x - Position.x % _worldSettings.ChunkSize.x;
            var y = 0;//Position.y - Position.y % WorldSettings.ChunkSize.y;
            var z = Position.z - Position.z % _worldSettings.ChunkSize.z;
            
            return new Vector3Int(x,y,z);
        }

        #endregion

        #region Private
        private void ChunkPoolUpdate() {
            foreach (var x in ChunkPool.Update(GetChunkCoords(_focus.transform.position))) {
                BuildCoordinator.Add(Chunks[x]);
            }

            BuildCoordinator.Process();

            WorldChunkPoolUpdate();
        }
        #endregion

    }

}