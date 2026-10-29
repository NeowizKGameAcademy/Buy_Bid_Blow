using BuyBidBlow.Systems.Room;

namespace BuyBidBlow.Systems.Account
{
    // 입력 검증. 아이디는 소문자로 정규화해 저장·비교한다.
    public static class AccountRules
    {
        public const int LoginIdMin = 4, LoginIdMax = 16;
        public const int PasswordMin = 6, PasswordMax = 64;

        public static bool IsLoginIdChar(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

        public static bool TryLoginId(string input, out string loginId, out string error)
        {
            loginId = (input ?? "").Trim().ToLowerInvariant(); error = "";
            if (loginId.Length < LoginIdMin || loginId.Length > LoginIdMax) { error = $"아이디는 {LoginIdMin}~{LoginIdMax}자로 입력하세요."; return false; }
            foreach (char c in loginId) if (!IsLoginIdChar(c)) { error = "아이디는 영문·숫자·밑줄(_)만 쓸 수 있습니다."; return false; }
            return true;
        }

        public static bool TryPassword(string password, out string error)
        {
            error = "";
            if (string.IsNullOrEmpty(password) || password.Length < PasswordMin || password.Length > PasswordMax) { error = $"비밀번호는 {PasswordMin}~{PasswordMax}자로 입력하세요."; return false; }
            foreach (char c in password) if (char.IsControl(c) || char.IsWhiteSpace(c)) { error = "비밀번호에는 공백을 쓸 수 없습니다."; return false; }
            return true;
        }

        // 로비와 같은 닉네임 규칙(Nickname.TryNormalize, 1~12글자)을 쓴다.
        public static bool TryNickname(string input, out string nickname, out string error)
        {
            error = "";
            if (Nickname.TryNormalize(input, out nickname)) return true;
            error = $"닉네임은 1~{Nickname.MaxLength}글자로 입력하세요. (<, > 제외)"; return false;
        }
    }
}
