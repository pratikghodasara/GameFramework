﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameFramework {
    public class GameMode : GameModeBase {
        public enum MatchState {
            WaitingToStart,
            InProgress,
            WaitingPostMatch,
            LeavingMap
        }

        public MatchState matchState {
            get;
            internal set;
        }

        public bool hasMatchStarted {
            get { return matchState == MatchState.InProgress; }
        }

        public bool hasMatchEnded {
            get { return matchState == MatchState.WaitingPostMatch; }
        }

        public List<Pawn> players {
            get;
            internal set;
        }

        Queue<(float, PlayerController)> _respawnQueue = new Queue<(float, PlayerController)>();

        public GameMode(ServerGame server) : base(server) {
            matchState = MatchState.WaitingToStart;
            players = new List<Pawn>();
            HandleMatchIsWaitingToStart();
        }

        public void StartMatch() {
            if (matchState != MatchState.WaitingToStart) {
                Debug.LogWarning("StartMatch called in wrong MatchState " + matchState);
                return;
            }

            Debug.Log("[Server][Game] <b>Match starting...</b>");

            matchState = MatchState.InProgress;
            HandleMatchHasStarted();
        }

        public void EndMatch() {
            if (matchState != MatchState.InProgress) {
                Debug.LogWarning("EndMatch called in wrong MatchState " + matchState);
                return;
            }

            matchState = MatchState.WaitingPostMatch;
            HandleMatchHasEnded();

            Debug.Log("[Server][Game] <b>Match has ended</b>");
        }

        public override void StartToLeaveMap() {
            if (matchState == MatchState.InProgress) {
                EndMatch();
            }

            if (matchState != MatchState.WaitingPostMatch) {
                Debug.LogWarning("StartToLeaveMap called in wrong MatchState " + matchState);
                return;
            }

            matchState = MatchState.LeavingMap;
            HandleLeavingMap();
        }

        public override void Update() {
            switch (matchState) {
                case MatchState.WaitingToStart:
                    if (ReadyToStartMatch()) {
                        StartMatch();
                    }
                    break;

                case MatchState.InProgress:
                    foreach (var pc in server.world.playerControllers) {
                        if (pc.pawn == null && !_respawnQueue.Any(pair => pair.Item2 == pc)) {
                            _respawnQueue.Enqueue((Time.time + 3, pc));
                        }
                    }

                    if (_respawnQueue.Count > 0) {
                        var timeControllerPair = _respawnQueue.Peek();
                        var respawnPlayer = Time.time >= timeControllerPair.Item1;
                        if (respawnPlayer) {
                            _respawnQueue.Dequeue();
                            SpawnPlayer(timeControllerPair.Item2);
                        }
                    }

                    if (ReadyToEndMatch()) {
                        EndMatch();
                    }
                    break;
            }
        }

        public override void HandleNewPlayer(PlayerController pc) {
            if (hasMatchStarted) {
                SpawnPlayer(pc);
            }
        }

        protected virtual bool ReadyToStartMatch() {
            return server.server.connections.Count > 0;
        }

        protected virtual bool ReadyToEndMatch() {
            return server.server.connections.Count == 0;
        }

        protected virtual void HandleMatchIsWaitingToStart() {
        }

        protected virtual void HandleMatchHasStarted() {
            foreach (var pc in server.world.playerControllers) {
                SpawnPlayer(pc);
            }
        }

        protected virtual void HandleMatchHasEnded() {
        }

        protected virtual void HandleLeavingMap() {
        }

        protected virtual void HandlePlayerSpawned(Pawn player) {
        }

        protected virtual void SpawnPlayer(PlayerController pc) {
            Debug.Log("[Server][Game] <b>Spawning player</b> <i>" + pc.connection + "</i>");

            var prefab = GetPlayerPrefab(pc);
            var go = server.server.replicaManager.InstantiateReplica(prefab);

            var spawnPose = GetPlayerSpawnPosition();

            var character = go.GetComponent<Character>();
            character.movement.Teleport(spawnPose.position, spawnPose.rotation);

            players.Add(character);

            HandlePlayerSpawned(character);

            pc.Possess(character);
        }

        protected virtual GameObject GetPlayerPrefab(PlayerController pc) {
            throw new NotImplementedException();
        }

        protected virtual Pose GetPlayerSpawnPosition() {
            if (PlayerSpawn.all.Count == 0)
                return Pose.identity;

            var spawn = PlayerSpawn.all[UnityEngine.Random.Range(0, PlayerSpawn.all.Count)];

            var spawnPosition = spawn.GetRandomizedPosition();
            return new Pose(spawnPosition, spawn.transform.rotation);
        }
    }
}