using System;
using System.Threading.Tasks;

namespace BuyBidBlow.Systems.Account
{
    // 계정 규칙(검증·해시)과 저장소를 묶는다. DB 입출력과 해시 계산은 메인 스레드 밖에서 한다.
    // 서버 경유로 바꿀 때도 서버 쪽에서 이 클래스를 그대로 쓴다.
    public sealed class AccountService : IAccountService
    {
        readonly IAccountRepository repository;

        public AccountService(IAccountRepository repository) { this.repository = repository ?? throw new ArgumentNullException(nameof(repository)); }

        public Task<AccountResult> SignUpAsync(string loginId, string password, string nickname)
        {
            if (!AccountRules.TryLoginId(loginId, out var id, out var error)
                || !AccountRules.TryPassword(password, out error)
                || !AccountRules.TryNickname(nickname, out var name, out error))
                return Task.FromResult(AccountResult.Fail(AccountError.InvalidInput, error));

            return Run(() =>
            {
                var hash = PasswordHasher.Hash(password);
                if (!repository.TryInsert(id, name, hash, out long accountId))
                    return AccountResult.Fail(AccountError.DuplicateLoginId, "이미 사용 중인 아이디입니다.");
                return AccountResult.Ok(new AccountProfile(accountId, id, name), "가입 완료! 이제 로그인하세요.");
            });
        }

        public Task<AccountResult> SignInAsync(string loginId, string password)
        {
            var id = (loginId ?? "").Trim().ToLowerInvariant();
            if (id.Length == 0 || string.IsNullOrEmpty(password))
                return Task.FromResult(AccountResult.Fail(AccountError.InvalidInput, "아이디와 비밀번호를 입력하세요."));

            return Run(() =>
            {
                var record = repository.FindByLoginId(id);
                if (record == null || !PasswordHasher.Verify(password, record.PasswordHash))
                    return AccountResult.Fail(AccountError.WrongCredentials, "아이디 또는 비밀번호가 맞지 않습니다.");
                repository.MarkSignedIn(record.AccountId);
                return AccountResult.Ok(new AccountProfile(record.AccountId, record.LoginId, record.Nickname), $"환영합니다, {record.Nickname}님!");
            });
        }

        static async Task<AccountResult> Run(Func<AccountResult> work)
        {
            try { return await Task.Run(work); }
            catch (Exception e) { return AccountResult.Fail(AccountError.Unavailable, "계정 DB에 연결하지 못했습니다. 잠시 후 다시 시도하세요.", e.GetType().Name + ": " + e.Message); }
        }
    }
}
