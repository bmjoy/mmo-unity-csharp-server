using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    // 공용매니저가 있는게 좋다
    public class ObjectManager
    {
        public static ObjectManager Instance { get; } = new ObjectManager();

        object _lock = new object();
        // 이건 플레이어끼리 귓속말 한다거나 그럴때 쓰려고 만든거고
        // 플레이어 이외의 오브젝트들은 GameRoom에서 관리할거임
        Dictionary<int, Player> _players = new Dictionary<int, Player>();

        // [UNUSED(1)][TYPE(7)][ID(24)] 맨앞 1비트는 부호다
        // [ ........ | ........ | ........ | ........ ]
        int _counter = 0;

        public T Add<T>() where T : GameObject, new()
        {
            T gameObject = new T();

            lock (_lock)
            {
                // ID 발급
                gameObject.Id = GenerateId(gameObject.ObjectType);

                if(gameObject.ObjectType == GameObjectType.Player)
                {
                    _players.Add(gameObject.Id, gameObject as Player);
                }
            }

            return gameObject;
        }

        int GenerateId(GameObjectType type)
        {
            lock (_lock)
            {
                // type값을 int(32비트) 안에서 24칸 이동한 위치에 대입 
                // 그 후 id값(_counter)와 or연산하면 id값도 입력 된 int값이 반환
                return ((int)type << 24) | (_counter++);
            }
        }

        public bool Remove(int objectId)
        {
            GameObjectType objectType = GetObjectTypeById(objectId);

            lock (_lock)
            {
                if(objectType == GameObjectType.Player)
                    return _players.Remove(objectId);
            }

            // 플레이어 빼면 여기서는 다른 오브젝트들을 모른다. -> 못지움
            return false;
        }

        public static GameObjectType GetObjectTypeById(int id)
        {
            // id값 비트를 맨 앞 7칸까지로 끌고오고
            // 나머지 부분들을 0x7F과 and연산해서 0으로 밀어버림
            // 0111 1111 = 0x7F = 127
            int type = (id >> 24) & 0x7F; // GameObjectType에 대입하게 위해 id에서 추출
            return (GameObjectType)type;
        }

        public Player Find(int objectId)
        {
            GameObjectType objectType = GetObjectTypeById(objectId);

            lock (_lock)
            {
                if(objectType == GameObjectType.Player)
                {
                    Player player = null;
                    if (_players.TryGetValue(objectId, out player))
                        return player;
                }
            }
            
            // lock 위치하고는 딱히 관계 없다, 안에 있어도 됨
            return null; // 플레이어 빼면 저장된 정보가 없음
        }
    }
}
