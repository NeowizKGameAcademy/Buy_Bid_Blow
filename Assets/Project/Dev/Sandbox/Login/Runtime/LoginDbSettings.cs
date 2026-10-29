using System;
using System.IO;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Login
{
    // DB 접속 정보. 비밀번호가 들어가므로 저장소(Assets) 밖에 둔다.
    //  - 에디터: <프로젝트>/UserSettings/BuyBidBlow/LoginDb.json (UserSettings는 .gitignore 대상)
    //  - 빌드: Application.persistentDataPath/LoginDb.json
    // 서버 담당 DB로 옮길 때는 이 파일 값만 바꾼다. 형식 예시는 Dev/Sandbox/Login/LoginDb.sample.json.
    [Serializable]
    public sealed class LoginDbSettings
    {
        public string host = "127.0.0.1";
        public int port = 3306;
        public string database = "buybidblow";
        public string user = "root";
        public string password = "";
        public string table = "accounts";
        // 테이블이 없으면 만든다. 서버 담당 DB처럼 스키마를 따로 관리하면 false.
        public bool autoCreateTable = true;
        public int connectTimeoutSeconds = 5;
        // None / Preferred / Required. 내 PC의 MariaDB는 None, 원격 서버 DB는 서버 담당 안내에 따른다.
        public string sslMode = "None";

        public const string FileName = "LoginDb.json";

        // 메인 스레드에서 호출한다(Application 경로 사용).
        public static string FilePath => Application.isEditor
            ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UserSettings", "BuyBidBlow", FileName))
            : Path.Combine(Application.persistentDataPath, FileName);

        // 파일이 없으면 기본값으로 만들어 두고 false를 돌려준다.
        public static bool TryLoad(out LoginDbSettings settings, out string error)
        {
            var path = FilePath; settings = null; error = "";
            try
            {
                if (!File.Exists(path))
                {
                    WriteTemplate(path);
                    error = $"DB 설정 파일을 만들었습니다. 접속 정보를 채운 뒤 다시 시도하세요.\n{path}";
                    return false;
                }
                settings = JsonUtility.FromJson<LoginDbSettings>(File.ReadAllText(path));
                if (settings == null || string.IsNullOrWhiteSpace(settings.host) || string.IsNullOrWhiteSpace(settings.database))
                {
                    error = $"DB 설정 파일에 host·database가 비어 있습니다.\n{path}"; settings = null; return false;
                }
                return true;
            }
            catch (Exception e) { error = $"DB 설정 파일을 읽지 못했습니다: {e.Message}\n{path}"; return false; }
        }

        public static void WriteTemplate(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(new LoginDbSettings(), true));
        }

        // 화면·로그 표시용(비밀번호 제외).
        public string Describe() => $"{user}@{host}:{port}/{database}.{table}";
    }
}
