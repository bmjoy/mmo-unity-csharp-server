using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class GameObject
    {
        // 공용으로 사용할 GameObject 타입 생성 -> 뭐에 쓰는놈인지 구분
        public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
        public int Id
        {
            get { return Info.ObjectId; }
            set { Info.ObjectId = value; }
        }

        // Room이나 Info의 get, set에 lock을 걸면 괜찮지 않나 싶은데
        // 위의 것들을 get 하는순간 참조를 가져오는거기 때문에 잠궈봤자 그거 무시하고 접근가능
        // 걍 null 체크하고 조심하자
        public GameRoom Room { get; set; } // 들어가있는 게임룸

        public ObjectInfo Info { get; set; } = new ObjectInfo();
        // PosInfo는 자주 쓸것같아서 따로 뺐다
        public PositionInfo PosInfo { get; private set; } = new PositionInfo();
        public StatInfo Stat { get; private set; } = new StatInfo();

        public float Speed // 자주 쓸 것 같으니 열어둠
        {
            get { return Stat.Speed; }
            set { Stat.Speed = value; }
        }

        public GameObject()
        {
            // new ObjectInfo, PositionInfo 해주는게 여기보다 일찍 실행되는갑네?
            Info.PosInfo = PosInfo;
            Info.StatInfo = Stat;
        }

        public Vector2Int CellPos
        {
            get
            {
                return new Vector2Int(Info.PosInfo.PosX, Info.PosInfo.PosY);
            }

            set
            {
                PosInfo.PosX = value.x;
                PosInfo.PosY = value.y;
            }
        }

        public Vector2Int GetFrontCellPos()
        {
            // 내가 지금 진행하고 있는 방향 기준으로 즉시 반환
            return GetFrontCellPos(PosInfo.MoveDir);
        }

        // 내가 바라보는 방향의 바로 앞 Cell 포지션 (타격처리용)
        public Vector2Int GetFrontCellPos(MoveDir dir)
        {
            // lastDir 이라는 개념이 여긴 없으니 일단 dir을 받아옴
            Vector2Int cellPos = CellPos;

            // 내가 타격직전 마지막으로 바라보는곳 기준으로
            switch (dir)
            {
                case MoveDir.Up:
                    cellPos += Vector2Int.up;
                    break;
                case MoveDir.Down:
                    cellPos += Vector2Int.down;
                    break;
                case MoveDir.Left:
                    cellPos += Vector2Int.left;
                    break;
                case MoveDir.Right:
                    cellPos += Vector2Int.right;
                    break;
            }

            return cellPos;
        }

        // 데미지, 공격자(어그로 시스템, 공격자 보상)
        // 여러명이 동시에 친다면?
        public virtual void OnDamaged(GameObject attacker, int damage)
        {
            Stat.Hp = Math.Max(Stat.Hp - damage, 0); // -hp방지

            // 모두에게 알린다 -> BroadCasting Packet
            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Stat.Hp;
            Room.Broadcast(changePacket); // 방에 있는 모두에게 나 얻어맞았다고 전송

            if (Stat.Hp <= 0)
            {
                Stat.Hp = 0;
                OnDead(attacker);
            }
        }

        public virtual void OnDead(GameObject attacker)
        {

        }
    }
}
