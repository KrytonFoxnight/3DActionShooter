namespace Player.State
{
    // 생사 축만 표현한다.
    // 공격 중 / 경직 같은 일시 상태는 배타적이지 않으므로(예: 슈퍼아머 - 공격 중 피격) 여기에 넣지 않는다.
    public enum PlayerStateType
    {
        Alive,
        Dead,
    }
}
