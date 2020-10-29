using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    // 공용매니저가 있는게 좋다
    public class PlayerManager
    {
        public static PlayerManager Instance { get; } = new PlayerManager();

        object _lock = new object();
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        int _playerId = 1; // 비트플레그로 쓰는 경우가 많다
        // 32개의 비트에서 앞에 4개는 플레이어 타입을 넣고.. 나머지는 또 다른정보로 채운다.

        public Player Add()
        {
            Player player = new Player();

            // 동일한 roomId가 생성되지 않게 하기위해 lock
            lock (_lock)
            {
                player.Info.PlayerId = _playerId;
                _players.Add(_playerId, player);
                _playerId++;
            }

            return player;
        }

        public bool Remove(int playerId)
        {
            lock (_lock)
            {
                return _players.Remove(playerId);
            }
        }

        public Player Find(int playerId)
        {
            lock (_lock)
            {
                Player player = null;
                if (_players.TryGetValue(playerId, out player))
                    return player;

                return null;
            }
        }
    }

}
