﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManewryMorskie
{
    public class PlayerManager : IEnumerable<Player>
    {
        private readonly TurnCounter turnCommander;
        private readonly int queueOffset;

        public PlayerManager(TurnCounter turnCommander, Player bottomPlayer, Player topPlayer)
        {
            this.turnCommander = turnCommander;
            BottomPlayer = bottomPlayer;
            TopPlayer = topPlayer;

            queueOffset = new Random().Next(2);
        }

        public Player TopPlayer { get; private set; }
        public Player BottomPlayer { get; private set; }
        public Player CurrentPlayer => GetPlayerOfTurn(turnCommander.TurnNumber);
        public Player GetPlayerOfTurn(int turnNumber) => turnNumber % 2 == queueOffset ? BottomPlayer : TopPlayer;

        public IEnumerable<Player> GetOpositePlayers(Player current) => this.Where(p => current != p);
        public IEnumerable<Player> GetOpositePlayers() => this.Where(p => CurrentPlayer != p);

        public Player GetOpositePlayer(Player current) => this.First(p => current != p);
        public Player GetOpositePlayer() => this.First(p => CurrentPlayer != p);

        public async Task WriteToPlayers(Player current, string msgToCurrent, string msgToOthers, MessageType messageType = MessageType.Standard)
        {
            await current.UserInterface.DisplayMessage(msgToCurrent, messageType);

            foreach (Player other in GetOpositePlayers(current))
                await other.UserInterface.DisplayMessage(msgToOthers, messageType);
        }

        public async Task WriteToPlayers(string msgToCurrent, string msgToOthers, MessageType messageType = MessageType.Standard) 
            => await WriteToPlayers(CurrentPlayer, msgToCurrent, msgToOthers, messageType);

        public async Task WriteToPlayers(string msgToAll, MessageType messageType = MessageType.Standard) 
            => await WriteToPlayers(CurrentPlayer, msgToAll, msgToAll, messageType);

        public IEnumerable<IUserInterface> UniqueInferfaces => new HashSet<IUserInterface>(this.Select(p => p.UserInterface));

        public IEnumerator<Player> GetEnumerator()
        {
            yield return BottomPlayer;
            yield return TopPlayer;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
