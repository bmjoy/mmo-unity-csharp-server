using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Server.DB
{
    // Entity임을 알리기 위해 클래스명 끝에 Db를 붙였다
    // 데이터모델, 아이템이나 콘텐츠
    // AccountDb 1 : n PlayerDb
    [Table("Account")]
    public class AccountDb
    {
        public int AccountDbId { get; set; } // Primary Key (PK), 끝에 Id를 붙이면 PK로 인식
        public string AccountName { get; set; } // unique, index
        public ICollection<PlayerDb> Players { get; set; }
    }

    [Table("Player")]
    public class PlayerDb
    {
        public int PlayerDbId { get; set; }
        public string PlayerName { get; set; }  
        public AccountDb Account { get; set; }
    }
}
