# 🛸 드론 자율 임무 및 웹 관제 시스템 (TeamProject)

### **Unity와 Python을 이용한 드론 자율 임무 및 웹 관제 시스템 프로젝트입니다.**

---

## 📚 목차 (Table of Contents)

1.  [**Git 협업 규칙 (Git Workflow)**](#-git-협업-규칙-git-workflow)
2.  [**사전 준비 (Windows 사용자)**](#-사전-준비-windows-사용자--최초-1회)
3.  [**파이썬 웹 서버 설정**](#-파이썬-웹-서버-설정-setup-python-web-server)
4.  [**프로젝트 실행 방법**](#️-실행-방법)
5.  [**폴더 구조**](#-폴더-구조-folder-structure)
6.  [**참고 링크**](#-참고-링크-links)

---

## 🌳 Git 협업 규칙 (Git Workflow)

> **★ 모든 작업은 개인 브랜치(Branch)에서 진행해주세요! (예: `JWK` 브랜치) ★**

### ✨ **핵심 원칙 3가지**

1.  **내 브랜치에서만 작업하기**: `main` 브랜치에 직접 커밋(Commit)하는 것은 절대 금지입니다.
2.  **작업 전에는 항상 최신화하기**: 내 작업을 시작하기 전, 항상 `main` 브랜치의 최신 내용을 내 브랜치로 먼저 가져와야 합니다.
3.  **PR은 소통의 시작**: Pull Request(PR)는 코드 리뷰와 소통을 위한 것입니다. PR을 올린 후에는 꼭 리뷰를 요청해주세요.

---

### **🚀 1. 작업 시작 전 (매번)**

> **목표: 내 개인 브랜치를 팀의 최신 작업 내용(`main`)과 동기화합니다.**

1.  **`main` 브랜치 최신화**
    * **GitHub Desktop**: `Current branch`를 `main`으로 변경하고, **`Pull origin`** 버튼을 클릭하여 최신 내용을 가져옵니다.

2.  **내 개인 브랜치로 이동**
    * **GitHub Desktop**: `Current branch`를 다시 내 개인 브랜치(예: `BGT`, `JWK`)로 변경합니다.

3.  **`main` 내용을 내 브랜치로 가져오기 (Merge)**
    * **매우 중요**: 이 과정을 통해 다른 팀원의 작업 내용을 내 브랜치에 먼저 반영하여, 나중에 발생할 큰 충돌을 미리 방지할 수 있습니다.
    * **GitHub Desktop**: 상단 메뉴에서 **`Branch > Update from main`**을 클릭합니다.
    * **❓ 다른 사람의 커밋이 보여요!**: 이 과정이 끝나면 내 브랜치의 `History` 탭에 다른 팀원들의 커밋이 보이는 것이 **정상입니다.** "팀의 최신 이력을 내 브랜치가 모두 반영했다"는 의미이니 안심하세요.
    * **💥 만약 충돌(Conflict)이 발생하면?**: 충돌 해결 메시지가 나타나면, 당황하지 말고 해당 파일을 열어 충돌 부분을 수정한 후 커밋을 완료해야 합니다. (이 단계에서 미리 해결하는 것이 훨씬 쉽습니다!)

4.  **✨ 새 작업 시작!**
    * 이제 내 브랜치는 팀의 모든 최신 내용을 담고 있습니다. 여기서부터 새로운 기능 개발이나 수정을 시작합니다.

---

### **✅ 2. 작업 완료 후**

> **목표: 내가 작업한 내용을 팀원들과 공유하기 위해 `main` 브랜치에 반영을 요청합니다.**

1.  **내 작업 내용 커밋 (Commit)**
    * **GitHub Desktop**: 왼쪽 `Changes` 탭에서 내가 변경한 파일들을 확인합니다.
    * 하단에 **어떤 작업을 했는지 명확하게** 커밋 메시지를 작성한 후, **`Commit to [내 브랜치]`** 버튼을 클릭합니다.

2.  **GitHub 서버에 업로드 (Push)**
    * **GitHub Desktop**: **`Push origin`** 버튼을 클릭하여 내 컴퓨터에 저장된 커밋들을 GitHub 저장소로 업로드합니다.

3.  **Pull Request (PR) 생성**
    * `Push`가 완료되면 GitHub Desktop에 나타나는 **`Create Pull Request`** 버튼을 클릭합니다.
    * PR의 **제목과 설명**을 다른 팀원들이 이해하기 쉽게 작성합니다. (어떤 작업을 왜 했는지, 어떻게 테스트하면 되는지 등)
    * 오른쪽의 **Reviewers** 메뉴에서 리뷰를 요청할 팀원을 지정하고, PR 생성을 완료한 뒤 리뷰를 요청했다고 알려주세요!

---

## ⚙️ 사전 준비 (Windows 사용자 / 최초 1회)

> 프로젝트를 처음 시작하기 전, PowerShell 스크립트를 실행할 수 있도록 아래 설정을 딱 한 번만 진행해주세요.

### **PowerShell 실행 정책 변경**

1.  **PowerShell을 "관리자 권한으로 실행"**:
    * Windows 검색창에 `powershell`을 입력한 후, **'Windows PowerShell'** 에 마우스 오른쪽 버튼을 클릭하여 **'관리자 권한으로 실행'**을 선택합니다.
2.  **명령어 입력**:
    * 관리자 권한으로 열린 PowerShell 창에 아래 명령어를 그대로 복사하여 붙여넣고 `Enter` 키를 누릅니다.
    ```powershell
    Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
    ```
3.  **변경 수락**:
    * 실행 정책을 변경할지 묻는 메시지가 나타나면 `Y`를 입력하고 `Enter` 키를 누릅니다.
    * 창을 닫으면 설정이 완료됩니다.

---

## ⚙️ 파이썬 웹 서버 설정 (Setup Python Web Server)

1.  **터미널을 열고 프로젝트 내의 `PythonWebServer` 폴더로 이동합니다.**
    * 예시: `cd C:\path\to\your\cloned\folder\TeamProject\PythonWebServer`

2.  **가상 환경 생성 (최초 한 번만)**
    ```bash
    python -m venv venv
    ```

3.  **가상 환경 활성화**
    * **PowerShell**: `.\venv\Scripts\activate`
    * **CMD**: `venv\Scripts\activate`
    * 성공하면 프롬프트의 맨 앞에 `(venv)` 가 표시됩니다.

4.  **필요한 라이브러리 설치**
    ```bash
    pip install Flask Flask-SocketIO eventlet
    ```

---

## ▶️ 실행 방법

1.  **웹 서버 실행**
    * 터미널을 열고 `TeamProject/PythonWebServer` 폴더로 이동 후 가상 환경을 활성화합니다.
    * 다음 명령어로 서버를 실행합니다.
        ```bash
        python app.py
        ```
    * 터미널에 `... wsgi starting up on http://0.0.0.0:5000` 메시지가 보이면 서버가 정상적으로 실행 중인 것입니다.

2.  **Unity 시뮬레이션 실행**
    * Unity 에디터에서 `TeamProject`를 열고, 상단의 플레이(▶) 버튼을 누릅니다.
    * Unity 콘솔 창에 `[Unity] WebSocket Connected!` 메시지가 나타나는지 확인합니다.

3.  **웹 관제 UI 접속**
    * 웹 브라우저를 열고 다음 주소로 접속합니다.
    * `http://127.0.0.1:5000`

---

## 📂 폴더 구조 (Folder Structure)

* `TeamProject/` : Unity 프로젝트의 핵심 파일들(Assets, Packages, ProjectSettings 등)이 위치합니다.
* `TeamProject/PythonWebServer/` : HTTP 웹 서버의 모든 소스 코드(Python, HTML, CSS, JS)가 포함된 전용 폴더입니다.
* `1_PLC File/` : 작업한 PLC 파일을 저장하는 폴더입니다.
* `2_CATIA File/` : 작업한 CATIA 파일 (CATPART, CATPRODUCT 등)을 저장하는 폴더입니다.

---

## 🚀 참고 링크 (Links)

* [노션 폴더 정리페이지 링크](https://sable-beard-26b.notion.site/Unity-Python-208fbf84667880368c81d891d256744b?source=copy_link)
