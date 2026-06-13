*PROJECT AGENT RULES
-이 문서는 현재 프로젝트에서 작업하는 AI 에이전트(Antigravity, Gemini, MCP Agent 등)가 반드시 준수해야 하는 전역 규칙이다.


*Architecture Rules
-반드시 MVP 패턴 사용


*Resource Rules
-반드시 Addressables 사용

-금지:
Resources 폴더 사용
Resources.Load 사용
-모든 리소스 로드는 ResourceManager를 통해 수행한다.


*Addressables Rules
-Addressable Key 규칙:
Feature_Type_Name

예시:
Mining_Block_Stone
Combat_Monster_Rat
UI_Popup_Upgrade



*Save System Rules
-저장 방식:
JSON

-저장 위치:
Application.persistentDataPath

-금지:
PlayerPrefs


*Combat
-전투는 터치 기반
-몬스터 터치 시 전투
-플레이어 터치로 공격



*UI Rules
-모든 UI는 Popup 구조 우선
-화면 비율은 1080*1920을 사용
-UI추가시 텍스트가 추가된다면 Localization Rules을 적용하여 추가

예시:
UpgradePopup
ShopPopup
SettingPopup

-UI 로직은 Presenter에서 처리한다.

*Localization Rules
-모든 텍스트는 English, Korean을 기본 적용
-기본 언어는 기기 기본언어를 바탕으로 Korean외 모든 언어는 English를 기본언어로 설정
-폰트 리소스는 Addressables로 관리
-TMP Font Asset은 Dynamic Atlas 사용

-Character Set:
Unicode

-포함 범위:
- 한글
- 영어
- 숫자
- 특수문자


*Development Rules
-새 기능 추가 시:
MVP 구조 유지
Addressables 등록 고려
Feature 폴더 구조 유지
Save 영향 여부 검토
Scene 추가 금지

*Development Rules
-새 기능 추가 시:
MVP 구조 유지
Addressables 등록 고려
Feature 폴더 구조 유지
Save 영향 여부 검토
Scene 추가 금지



*AI Agent Instructions
-새로운 코드 작성 시:
기존 구조를 우선 재사용
MVP 패턴 유지
Addressables 사용
Feature 기반 구조 유지
SaveManager 연동 여부 검토

구조를 위반하는 구현은 생성하지 않는다.

