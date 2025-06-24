#
# Github 작업 합치기 관련
1. 작업 시작 전 Main Branch에서 최신화 업데이트
2. 그 후 본인 Branch에 Main Branch Merge
3. 작업 완료 후 본인 Branch에서 오류가 있나 없나 확인
4. 3번 까지의 작업이 완료 되었다면 PR (Pull Request)

__★★★★★ 모든 유니티 작업은 개인 Branch에서 작업할것 !! ★★★★★__


#
# DroneProject 폴더 구분
    1. PythonWebServer => ★ HTTP 웹 서버 전용 폴더 ★
    
    2.UnitySimulation => ★ 유니티 프로젝트 파일 관련 폴더 ★

# DroneProject HTTP 통신 서버 실행 방법
1. PowerShell ** 관리자 권한으로 실행 **
2. 다음 명령어 입력 + Enter

    => Set-ExecutionPolicy RemoteSigned -Scope CurrentUser


3. PowerShell 그냥 실행
4. cd C:\(본인 깃 파일 로컬 저장소 주소 입력)\PythonWebServer 로 이동
5. .\venv\Scripts\activate 입력 + Enter

    => 성공적으로 활성화가 되면 프롬프트 맨 앞에 (venv) 라고 표시가 됨.

    => ex) (venv) PS C:\DroneControlProject\PythonWebServer>


6. 가상환경이 실행된 상태에서 app.py 입력 + Enter
7. 터미널 창에 http://127.0.0.1:5000" 과 같은 메시지가 나타나고, 오류 없이 실행 중인지 확인.

8. 본인 PC에서는 127.0.0.1:(포트번호) 입력해서 접속 가능
9. 다른 PC에서 접속을 원할 시 http://(본인 IP 주소):(포트번호) 이런식으로 주소를 입력해야 연결됨.

</details>

<detail>
    <summary><b>🚀 깃 참고</b></summary>

[노션 폴더 정리 링크](https://sable-beard-26b.notion.site/Unity-Python-208fbf84667880368c81d891d256744b?source=copy_link)

</details>