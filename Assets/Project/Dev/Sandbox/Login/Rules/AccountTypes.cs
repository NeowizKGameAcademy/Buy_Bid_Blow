namespace BuyBidBlow.Systems.Account
{
    public enum AccountError { None, InvalidInput, DuplicateLoginId, WrongCredentials, Unavailable }

    // 로그인에 성공한 계정. 비밀번호 해시는 담지 않는다.
    public sealed class AccountProfile
    {
        public readonly long AccountId;
        public readonly string LoginId;
        public readonly string Nickname;
        public AccountProfile(long accountId, string loginId, string nickname) { AccountId = accountId; LoginId = loginId; Nickname = nickname; }
    }

    // 저장소에서 읽은 행 그대로. 서비스 안에서만 쓴다.
    public sealed class AccountRecord
    {
        public readonly long AccountId;
        public readonly string LoginId;
        public readonly string Nickname;
        public readonly string PasswordHash;
        public AccountRecord(long accountId, string loginId, string nickname, string passwordHash) { AccountId = accountId; LoginId = loginId; Nickname = nickname; PasswordHash = passwordHash; }
    }

    public readonly struct AccountResult
    {
        public readonly AccountError Error;
        // 플레이어에게 보여줄 문장.
        public readonly string Message;
        // 개발자 로그용(예외 내용). 화면에는 띄우지 않는다.
        public readonly string Detail;
        public readonly AccountProfile Profile;
        public bool Success => Error == AccountError.None;

        AccountResult(AccountError error, string message, string detail, AccountProfile profile) { Error = error; Message = message; Detail = detail; Profile = profile; }
        public static AccountResult Ok(AccountProfile profile, string message = "") => new AccountResult(AccountError.None, message, "", profile);
        public static AccountResult Fail(AccountError error, string message, string detail = "") => new AccountResult(error, message, detail, null);
    }
}
