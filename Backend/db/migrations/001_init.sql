-- 계정·재화·상점·인벤토리·경기 기록 초기 스키마 (MariaDB)

CREATE TABLE accounts (
    id          BIGINT      NOT NULL AUTO_INCREMENT PRIMARY KEY,
    nickname    VARCHAR(32) NOT NULL,
    created_at  DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);

-- 외부 ID(steam, eos, googleplay)와 계정 연결. 한 계정에 여러 외부 ID를 붙일 수 있다.
CREATE TABLE account_identities (
    provider     VARCHAR(16) NOT NULL,
    external_id  VARCHAR(64) NOT NULL,
    account_id   BIGINT      NOT NULL,
    created_at   DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (provider, external_id),
    FOREIGN KEY (account_id) REFERENCES accounts (id)
);

-- 현재 잔액. 변동은 반드시 wallet_ledger와 같은 트랜잭션에서 갱신한다.
CREATE TABLE wallets (
    account_id  BIGINT NOT NULL PRIMARY KEY,
    balance     BIGINT NOT NULL DEFAULT 0 CHECK (balance >= 0),
    FOREIGN KEY (account_id) REFERENCES accounts (id)
);

-- 재화 변동 원장. 같은 요청 ID는 한 번만 처리된다.
CREATE TABLE wallet_ledger (
    id          BIGINT      NOT NULL AUTO_INCREMENT PRIMARY KEY,
    account_id  BIGINT      NOT NULL,
    amount      BIGINT      NOT NULL,
    reason      VARCHAR(32) NOT NULL,   -- purchase, match_reward, admin ...
    request_id  CHAR(36)    NOT NULL,
    created_at  DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UNIQUE (account_id, request_id),
    FOREIGN KEY (account_id) REFERENCES accounts (id)
);

-- 상품 카탈로그
CREATE TABLE items (
    id       VARCHAR(64) NOT NULL PRIMARY KEY,
    kind     VARCHAR(16) NOT NULL,      -- costume, emote ...
    price    BIGINT      NOT NULL CHECK (price >= 0),
    enabled  BOOLEAN     NOT NULL DEFAULT TRUE
);

CREATE TABLE inventory (
    account_id   BIGINT      NOT NULL,
    item_id      VARCHAR(64) NOT NULL,
    quantity     INT         NOT NULL DEFAULT 1 CHECK (quantity >= 0),
    acquired_at  DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (account_id, item_id),
    FOREIGN KEY (account_id) REFERENCES accounts (id),
    FOREIGN KEY (item_id) REFERENCES items (id)
);

-- 구매 기록. 잔액 차감·인벤토리 지급·원장 기록과 한 트랜잭션으로 남긴다.
CREATE TABLE purchases (
    id          BIGINT      NOT NULL AUTO_INCREMENT PRIMARY KEY,
    account_id  BIGINT      NOT NULL,
    item_id     VARCHAR(64) NOT NULL,
    price       BIGINT      NOT NULL,
    request_id  CHAR(36)    NOT NULL,
    created_at  DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UNIQUE (account_id, request_id),
    FOREIGN KEY (account_id) REFERENCES accounts (id),
    FOREIGN KEY (item_id) REFERENCES items (id)
);

-- 호스트가 보고한 경기 결과. 경기 ID가 같으면 중복 보고로 보고 무시한다.
CREATE TABLE matches (
    id               CHAR(36)    NOT NULL PRIMARY KEY,
    host_account_id  BIGINT      NOT NULL,
    reported_at      DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    FOREIGN KEY (host_account_id) REFERENCES accounts (id)
);

CREATE TABLE match_participants (
    match_id      CHAR(36) NOT NULL,
    account_id    BIGINT   NOT NULL,
    rank_no       INT      NOT NULL,
    final_assets  BIGINT   NOT NULL,
    reward        BIGINT   NOT NULL DEFAULT 0,  -- 순위와 무관한 참가 보상
    PRIMARY KEY (match_id, account_id),
    FOREIGN KEY (match_id) REFERENCES matches (id),
    FOREIGN KEY (account_id) REFERENCES accounts (id)
);
