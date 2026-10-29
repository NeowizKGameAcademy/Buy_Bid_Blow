using BuyBidBlow.Systems.Account;

namespace BuyBidBlow.Sandbox.Login
{
    // 계정 기능 구현을 고르는 유일한 곳. UI는 IAccountService만 안다.
    //  - 지금: 내 MariaDB에 직접 연결(접속 정보는 LoginDb.json).
    //  - 서버 담당 MariaDB로 옮길 때: LoginDb.json 값만 바꾼다.
    //  - 서버 경유로 바꿀 때: 이 메서드가 IAccountService를 구현한 서버 요청용 클라이언트를 돌려주게 하고,
    //    서버에서는 new AccountService(new MariaDbAccountRepository(...))를 그대로 쓴다.
    public static class AccountBackend
    {
        // 메인 스레드에서 호출한다.
        public static IAccountService Create(out string target, out string error)
        {
            target = ""; error = "";
            if (!LoginDbSettings.TryLoad(out var settings, out error)) return null;
            try
            {
                target = settings.Describe();
                return new AccountService(new MariaDbAccountRepository(settings));
            }
            catch (System.Exception e) { error = "DB 설정이 올바르지 않습니다: " + e.Message; return null; }
        }
    }
}
