# Backend

BuyBidBlow의 아웃게임 API 서버. 계정·재화·상점·인벤토리·경기 기록·랭킹을 관리한다. 설계 결정은 [Docs/Decisions.md](../Docs/Decisions.md)의 백엔드·경제 판정 위치·재화 항목을 따른다.

- 스택: ASP.NET Core(.NET 10) + MariaDB, DB 연결은 MySqlConnector
- 운영: AWS Lightsail 한 대에 API와 MariaDB를 함께 설치
- 경기 중 입찰·거래·정산은 Mirror 호스트(방장 PC)가 판정한다. 백엔드는 호스트가 보고한 경기 결과를 기록하고 참가 보상만 지급한다.

## 폴더

```
Backend/
├─ BuyBidBlow.Backend.slnx
├─ compose.yaml                ← 로컬 개발용 MariaDB
├─ db/migrations/              ← 번호순 SQL. 적용된 파일은 고치지 않고 새 번호로 추가한다
└─ src/BuyBidBlow.Api/
   ├─ Program.cs               ← 진입점, 서비스 등록, 엔드포인트 연결
   ├─ Accounts/                ← 외부 ID(Steam·EOS·Google Play) 검증 → 계정 → JWT
   ├─ Wallet/                  ← 잔액, 변동 원장
   ├─ Shop/                    ← 상품 목록, 구매
   ├─ Inventory/               ← 보유 아이템
   ├─ Matches/                 ← 경기 결과 기록, 참가 보상
   ├─ Rankings/
   └─ Common/                  ← 인증, 요청 ID 등 여러 기능이 쓰는 코드
```

- 기능 폴더는 그 기능을 구현할 때 만든다. 한 기능의 엔드포인트·서비스·DB 접근 코드는 그 폴더에 함께 두고, 파일 종류(`Controllers`·`Models`)로 나누지 않는다.
- 네임스페이스는 `BuyBidBlow.Backend.{기능}`이다.
- Unity와 주고받는 요청·응답 DTO는 `Assets/Project/Backend/Contracts`에 두고 이 프로젝트가 같은 소스를 컴파일한다(도입 시).

## 규칙

- 재화는 백엔드만 바꾼다. 잔액(`wallets`)과 원장(`wallet_ledger`)은 같은 트랜잭션에서 갱신한다.
- 재화를 바꾸는 요청은 클라이언트가 만든 요청 ID(UUID)를 받고, `(account_id, request_id)` UNIQUE로 중복 처리를 막는다. 중복 키 오류는 이미 처리된 요청으로 보고 기존 결과를 돌려준다.
- 구매는 잔액 확인·차감·인벤토리 지급·구매 기록을 한 트랜잭션으로 처리한다.
- 경기 결과는 `matches.id`(경기 ID) UNIQUE로 중복 보고를 막는다. 보상은 순위와 무관한 참가 보상만 지급하고 일일 상한을 둔다.

## 로컬 실행

필요: .NET 10 SDK, Docker(또는 직접 설치한 MariaDB)

```bash
docker compose up -d
dotnet run --project src/BuyBidBlow.Api
```

- `appsettings.Development.json`의 접속 정보는 로컬 전용이다. 운영 접속 정보는 커밋하지 않는 `appsettings.Local.json` 또는 환경변수 `ConnectionStrings__Main`으로 넣는다.
- 확인: `GET /health`(서버), `GET /health/db`(DB 연결)
