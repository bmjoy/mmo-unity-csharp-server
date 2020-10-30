using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Game;

namespace Server
{
	public class ClientSession : PacketSession
	{
		// 현재 ClientSession에 소속된 플레이어가 누군지 알고있으면 편하다.
		public Player MyPlayer { get; set; }
		public int SessionId { get; set; }		

		public void Send(IMessage packet)
        {
			// 패킷 이름 추출 
			// ex)S_Chat 식으로, enum에 있는 id는 SChat 이런식임 -> 변환
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
			// c#의 리플렉션을 이용하면
			// string인 msgName으로 enum인 MsgId중에 msgName과 같은 이름을 가진 MsgId를 찾을수있다.
			// 만약 MsgId 안에서 찾을 수 없으면 걍 터지니깐 에러처리를 하든지 냅둬서 무조건 찾아내게 하든지
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);

			ushort size = (ushort)packet.CalculateSize(); // packet 사이즈 계산

			// Session.cs 를 보면 전송할 데이터 + 아래와 같은 추가정보를 넣는다.
			// [size(2)] : 얼만큼 파싱을 할지
			// [packetId(2)] : 변환할 클래스
			// 각각 2바이트씩 필요하므로 위에서 구한 사이즈에 4바이트를 더해줘야 한다
			byte[] sendBuffer = new byte[size + 4];
			// BitConverter.GetBytes의 경우 구현이 내부에서 또 byte배열을 할당하기 때문에 문제가 있다.
			// -> 비트연산을 이용해서 직접 넣으면 되긴 함 
			Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));

			// 메시지의 id를 인자로 받은 packet의 이름을 통해 얻어옴
			// 미리 id와 패킷의 이름 사이에 규칙을 만들었기 때문에 가능 (S_Chat,SChat)
			Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort)); // sendBuffer 위치는 2
			
			// packet을 버퍼에 넣자
			Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size); // sendBuffer 위치는 4

			Send(new ArraySegment<byte>(sendBuffer));
		}

		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			// 클라이언트가 서버에 접속성공
			// DB를 긁어 플레이어 정보를 가져와서 클라에 보내주고..
			MyPlayer = PlayerManager.Instance.Add();
            {
				MyPlayer.Info.Name = $"Player_{MyPlayer.Info.PlayerId}";
				MyPlayer.Info.PosInfo.State = CreatureState.Idle;
				MyPlayer.Info.PosInfo.MoveDir = MoveDir.None;
				MyPlayer.Info.PosInfo.PosX = 0;
				MyPlayer.Info.PosInfo.PosY = 0;
				MyPlayer.Session = this;
            }

			// 지금 방이 1번방밖에 없다
			RoomManager.Instance.Find(1).EnterGame(MyPlayer);
		}

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			// PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			RoomManager.Instance.Find(1).LeaveGame(MyPlayer.Info.PlayerId); // 게임룸에서 퇴장

			SessionManager.Instance.Remove(this);

			Console.WriteLine($"OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}
	}
}
