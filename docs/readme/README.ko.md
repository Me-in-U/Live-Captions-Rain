[English](../../README.md) | [한국어](README.ko.md)

# Falling Words

Falling Words는 Windows Live Captions 단어를 물리 기반 오버레이로 떨어뜨리는 Windows WPF 앱입니다. 캡션 단어가 선택한 모니터 위에서 떨어지고, 화면 바닥에 쌓이며, 선택적으로 보이는 데스크톱 창 상단에도 쌓일 수 있습니다.

## 스크린샷

### 설정

![Falling Words 설정 창](../assets/screenshots/settings.png)

## 주요 기능

- Windows Live Captions에서 단어를 읽습니다.
- 캡션 단어를 투명 데스크톱 오버레이에 떨어뜨립니다.
- 선택 모니터 모드와 전체 모니터 모드를 지원합니다.
- 기본적으로 클릭 통과 상태이며, 선택적으로 클릭 상호작용을 켤 수 있습니다.
- Box2D 물리 엔진으로 중력, 쌓임, 바람, 창 상단 발판을 처리합니다.
- 보이는 데스크톱 창을 감지하고, 실제로 보이는 상단 모서리만 쌓임 발판으로 사용합니다.
- 높은 곳에서 떨어진 단어는 충돌 시 분리될 수 있습니다.
- 한글을 고려한 단어 조각화를 지원합니다.
- 글꼴, 크기, 굵기, 채움, 외곽선, 색상, 불투명도, 그림자를 설정할 수 있습니다.
- 랜덤, 왼쪽에서 오른쪽, 오른쪽에서 왼쪽, 중앙 위주 생성 모드를 제공합니다.
- 좌우 움직임과 약한 상승 흐름이 포함된 자연스러운 랜덤 바람을 적용합니다.
- 닫기 버튼을 누르면 트레이로 최소화됩니다.
- Windows 표시 언어에 따라 한국어/영어 UI를 사용합니다.

## 요구사항

- Windows 11
- Windows Live Captions
- 개발 빌드용 .NET 8 SDK

## 빠른 시작

1. Windows Live Captions를 켜거나 Falling Words가 실행하도록 둡니다.
2. `FallingWords.exe`를 실행합니다.
3. 대상 모니터를 선택합니다.
4. 텍스트 스타일과 물리 옵션을 조정합니다.
5. **켜기**를 누릅니다.
6. 설정 창을 닫으면 트레이에서 계속 실행됩니다.

설정 파일 위치:

```text
%APPDATA%\FallingWords\settings.json
```

## 빌드

```powershell
dotnet restore
dotnet build FallingWords.sln
dotnet test FallingWords.sln
dotnet publish src\FallingWords\FallingWords.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

생성된 `artifacts`, `bin`, `obj` 출력물은 커밋하지 않습니다.

## 저장소 구조

- `src\FallingWords` - WPF 앱, 설정 창, 트레이, 오버레이, Live Captions 어댑터, Win32 interop, 렌더링 제어
- `src\FallingWords.Core` - 캡션 처리, 설정, 지역화, 물리 헬퍼, 바람, 창 발판 계산, 단어 분리 로직
- `tests\FallingWords.Tests` - 캡션, 설정, 지역화, 물리, 창 필터링, 바람, 생성, 텍스트 분리 테스트
- `docs` - 프로젝트 문서와 스크린샷
- `artifacts` - 로컬 빌드 결과와 검증 스크린샷. git에서 제외됩니다.

## 개인정보

Falling Words는 오디오를 녹음하지 않고 캡션 텍스트를 원격 서비스로 보내지 않습니다. 앱은 Windows Live Captions에 이미 표시된 텍스트만 읽습니다. 설정은 `%APPDATA%\FallingWords` 아래에 로컬로 저장됩니다.

## 알려진 제한사항

- Windows Live Captions UI가 변경되면 어댑터 수정이 필요할 수 있습니다.
- 텍스트 충돌은 안정성을 위해 사각형 물리 바디를 사용합니다.
- 일부 셸, 게임, 보호된 창, GPU 오버레이 창은 사용 가능한 위치 정보를 제공하지 않을 수 있습니다.
- 전체화면 또는 화면 상단에 붙은 창은 단어 발판이 아니라 가림 영역으로 처리됩니다.
- Windows 11을 지원 대상으로 합니다.

## 크레딧

- [SakiRinn/LiveCaptions-Translator](https://github.com/SakiRinn/LiveCaptions-Translator)
- [Me-in-U/LiveDialogue-Translator](https://github.com/Me-in-U/LiveDialogue-Translator)
- [Box2D.NET](https://www.nuget.org/packages/Box2D.NET)
- [Interop.UIAutomationClient](https://www.nuget.org/packages/Interop.UIAutomationClient)
- [WPF-UI](https://www.nuget.org/packages/WPF-UI)

## 라이선스

Apache-2.0. [LICENSE](../../LICENSE)를 참고하세요.
