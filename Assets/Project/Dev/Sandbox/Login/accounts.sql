-- BuyBidBlow 로그인 Sandbox 계정 테이블 (MariaDB 10.x 이상)
-- MariaDbAccountRepository.EnsureSchema와 같은 정의다. 바꾸면 둘 다 고친다.
-- 서버 담당 DB에 옮길 때는 이 파일로 테이블을 만들고 LoginDb.json의 autoCreateTable을 false로 둔다.

CREATE DATABASE IF NOT EXISTS buybidblow CHARACTER SET utf8mb4;
USE buybidblow;

CREATE TABLE IF NOT EXISTS accounts (
    account_id    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    login_id      VARCHAR(32)  NOT NULL,              -- 소문자 영문·숫자·_ 4~16자
    nickname      VARCHAR(64)  NOT NULL,              -- 로비 닉네임 규칙(1~12글자)
    password_hash VARCHAR(255) NOT NULL,              -- pbkdf2_sha256$반복수$salt$hash
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at DATETIME NULL,
    UNIQUE KEY uq_login_id (login_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 게임 전용 계정(권장: root 대신 사용). 비밀번호는 바꿔서 쓴다.
-- CREATE USER IF NOT EXISTS 'bbb_login'@'localhost' IDENTIFIED BY '비밀번호';
-- CREATE USER IF NOT EXISTS 'bbb_login'@'%' IDENTIFIED BY '비밀번호';   -- 다른 PC에서 접속할 때
-- GRANT SELECT, INSERT, UPDATE, CREATE ON buybidblow.* TO 'bbb_login'@'localhost';
-- GRANT SELECT, INSERT, UPDATE, CREATE ON buybidblow.* TO 'bbb_login'@'%';
-- FLUSH PRIVILEGES;

-- 저장 확인
-- SELECT account_id, login_id, nickname, LEFT(password_hash, 24) AS hash, created_at, last_login_at FROM accounts;
