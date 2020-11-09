using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

class PacketHandler
{
	public static void C_MoveHandler(PacketSession session, IMessage packet)
	{
		C_Move movePacket = packet as C_Move;
		ClientSession clientSession = session as ClientSession;

		//Console.WriteLine($"C_Move ({movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosY})");

		// lock 처리 안하고 멀티쓰레드 방어

		Player player = clientSession.MyPlayer; // 내가 꺼내오는 시점의 플레이어를 따로 변수로 뺌
		if (player == null)
			return;

		// 아래부터는 MyPlayer가 null 아니라고 확신하고 작성한 코드들이지만
		// 멀티쓰레드일때는 보장할수가 없다.
		// 그래서 플레이어가 내가 꺼내오는 시점에서 있다면? 따로 변수로 빼놓고 쓰자

		// 만약 다른 쓰레드에서 LeaveGame() 해버리면 null이 아니라고 체크하고 들어가도
		// 하단에서 터질수가 있음
		// 그니깐 플레이어처럼 내가 체크하는 시점에서 따로 빼놓음
		GameRoom room = player.Room;
		if (room == null)
			return;

		// JobQueue 방식을 사용했기 때문에 이동패킷에 대한 응답에 딜레이가 생겼음 (room.HandleMove)
		// 그런데 내가 조작하는 플레이어는 이미 이동을 해버렸는데 서버 응답은 즉시가 아님
		// 클라이언트의 S_MoveHandler을 보면 서버 응답을 받고 내가 조작하는 플레이어의 상태까지 변경해버리기 때문에
		// 위에 응답 딜레이와 합쳐져서 부자연스러운 움직임을 보이게 됨, 
		// 난 이미 이동했지만 서버는 직전 좌표를 던져줘서 나를 강제로 이동시킬수가 있다.
		room.Push(room.HandleMove, player, movePacket); // 이동패킷 처리를 안전하게
	}

    public static void C_SkillHandler(PacketSession session, IMessage packet)
    {
		C_Skill skillPacket = packet as C_Skill;
		ClientSession clientSession = session as ClientSession;

		Player player = clientSession.MyPlayer; 
		if (player == null)
			return;

		GameRoom room = player.Room;
		if (room == null)
			return;

		room.Push(room.HandleSkill, player, skillPacket);
	}
}
