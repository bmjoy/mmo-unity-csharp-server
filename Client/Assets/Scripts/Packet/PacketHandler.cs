﻿using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

class PacketHandler
{
	public static void S_EnterGameHandler(PacketSession session, IMessage packet)
	{
		S_EnterGame enterGamePacket = packet as S_EnterGame;

		// 플레이어 객체 자체를 여기서 생성 또는
		// ObjectManager의 Add 내부에서 플레이어 객체를 생성
		Managers.Object.Add(enterGamePacket.Player, myPlayer : true);
	}

	public static void S_LeaveGameHandler(PacketSession session, IMessage packet)
	{
		S_LeaveGame leaveGamePacket = packet as S_LeaveGame;
		Managers.Object.Clear();
	}

	// 다른 유저들의 정보를 받음
	public static void S_SpawnHandler(PacketSession session, IMessage packet)
	{
		S_Spawn spawnPacket = packet as S_Spawn;

        foreach (ObjectInfo obj in spawnPacket.Objects)
        {
			Managers.Object.Add(obj, myPlayer: false);
        }
	}

	public static void S_DespawnHandler(PacketSession session, IMessage packet)
	{
		S_Despawn despawnPacket = packet as S_Despawn;

		foreach (int id in despawnPacket.ObjectIds)
		{
			Managers.Object.Remove(id);
		}
	}

	// 이동패킷
	public static void S_MoveHandler(PacketSession session, IMessage packet)
	{
		S_Move movePacket = packet as S_Move;

		GameObject go = Managers.Object.FindById(movePacket.ObjectId);
		if (go == null)
			return;

		// 내 이동은 이미 클라에서 처리했는데, 굳이 서버에서 콜백을 받아다가 덮어쓸 이유는 없지않을까?
		// 난 이미 이동했지만 서버는 직전 좌표를 던져줘서 나를 강제로 이전 좌표에 이동시킬수가 있다.
		// 조작중인 플레이어의 이동은 전적으로 클라이언트에 의지한다 서버는 통보만 받음
		if (Managers.Object.MyPlayer.Id == movePacket.ObjectId)
			return;

		// 정보를 고치기 위해 CreatureController에 접근
		BaseController bc = go.GetComponent<BaseController>();
		if (bc == null)
			return; // or crash 

		bc.PosInfo = movePacket.PosInfo;
	}

	public static void S_SkillHandler(PacketSession session, IMessage packet)
	{
		S_Skill skillPacket = packet as S_Skill;

		// 여기서 찾은 PlayerId가 꼭 나라는 보장은 없다. 스킬은 아무나 쓰니깐
		GameObject go = Managers.Object.FindById(skillPacket.ObjectId);
		if (go == null)
			return;

		// 정보를 고치기 위해 CreatureController에 접근
		CreatureController pc = go.GetComponent<CreatureController>();
		if (pc != null)
		{
			pc.UseSkill(skillPacket.Info.SkillId);
		}
	}

    public static void S_ChangeHpHandler(PacketSession session, IMessage packet)
    {
		S_ChangeHp changePacket = packet as S_ChangeHp;

		// 여기서 찾은 PlayerId가 꼭 나라는 보장은 없다. 스킬은 아무나 쓰니깐
		GameObject go = Managers.Object.FindById(changePacket.ObjectId);
		if (go == null)
			return;

		CreatureController cc = go.GetComponent<CreatureController>();
		if (cc != null)
		{
			// 체력을 깎자
			cc.Hp = changePacket.Hp; // ui 갱신도 같이 됨
			// UI 갱신
			Debug.Log($"ChangeHP : {changePacket.Hp}");
		}
	}

	public static void S_DieHandler(PacketSession session, IMessage packet)
	{
		S_Die diePacket = packet as S_Die;

		// 여기서 찾은 PlayerId가 꼭 나라는 보장은 없다. 스킬은 아무나 쓰니깐
		GameObject go = Managers.Object.FindById(diePacket.ObjectId);
		if (go == null)
			return;

		CreatureController cc = go.GetComponent<CreatureController>();
		if (cc != null)
		{
			cc.Hp = 0;
			cc.OnDead(); // 해당 크리처를 죽임
		}
	}
}
