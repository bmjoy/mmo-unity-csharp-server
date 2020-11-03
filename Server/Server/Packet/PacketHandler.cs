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

		Console.WriteLine($"C_Move ({movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosY})");

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

		room.HandleMove(player, movePacket); // 이동패킷 처리를 안전하게
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

		room.HandleSkill(player, skillPacket);
	}
}
