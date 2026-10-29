using System.Threading.Tasks;

namespace BuyBidBlow.Systems.Account
{
    // UI가 아는 유일한 계정 기능. 지금은 AccountService(DB 직접 연결),
    // 서버 경유로 바꿀 때는 이 인터페이스를 구현한 클라이언트(예: Mirror 메시지 송신)로 교체한다.
    // 결과는 호출한 스레드(Unity 메인 스레드)로 돌아온다.
    public interface IAccountService
    {
        Task<AccountResult> SignUpAsync(string loginId, string password, string nickname);
        Task<AccountResult> SignInAsync(string loginId, string password);
    }

    // 저장 인터페이스. DB 종류가 바뀌면 이것만 새로 구현한다.
    // 메서드는 동기이며 AccountService가 메인 스레드 밖에서 호출한다. 실패는 예외로 알린다.
    public interface IAccountRepository
    {
        // 없으면 null.
        AccountRecord FindByLoginId(string loginId);
        // 아이디가 이미 있으면 false.
        bool TryInsert(string loginId, string nickname, string passwordHash, out long accountId);
        void MarkSignedIn(long accountId);
    }
}
