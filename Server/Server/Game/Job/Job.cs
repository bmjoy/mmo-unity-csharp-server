using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    // 이 프로젝트의 Job의 개념 정립
    public interface IJob
    {
        void Execute();
    }
    // 클라이언트의 요청을 캡슐화 하는 클래스, 주방의 주문내역
    public class Job : IJob
    {
        Action _action; // 어떠한 함수를 delegate식으로 저장

        public Job(Action action)
        {
            _action = action;
        }

        public void Execute()
        {
            _action.Invoke();
        }
    }

    public class Job<T1> : IJob
    {
        Action<T1> _action; // 어떠한 함수를 delegate식으로 저장
        T1 _t1;

        public Job(Action<T1> action, T1 t1)
        {
            _action = action;
            _t1 = t1;
        }

        public void Execute()
        {
            _action.Invoke(_t1); // C++로 치면 함수자
        }
    }

    public class Job<T1, T2> : IJob
    {
        Action<T1, T2> _action; // 어떠한 함수를 delegate식으로 저장
        T1 _t1;
        T2 _t2;

        public Job(Action<T1, T2> action, T1 t1, T2 t2)
        {
            _action = action;
            _t1 = t1;
            _t2 = t2;
        }

        public void Execute()
        {
            _action.Invoke(_t1, _t2); // C++로 치면 함수자
        }
    }
    
    // 노가다 같지만 Action 구현도 이런식임
    public class Job<T1, T2, T3> : IJob
    {
        Action<T1, T2, T3> _action; // 어떠한 함수를 delegate식으로 저장
        T1 _t1;
        T2 _t2;
        T3 _t3;

        public Job(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3)
        {
            _action = action;
            _t1 = t1;
            _t2 = t2;
            _t3 = t3;
        }

        public void Execute()
        {
            _action.Invoke(_t1, _t2, _t3); // C++로 치면 함수자
        }
    }
}
