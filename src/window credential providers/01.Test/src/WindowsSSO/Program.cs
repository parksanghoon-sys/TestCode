using System;
using System.DirectoryServices.AccountManagement; // Active Directory 관련 작업을 위한 네임스페이스
using System.Security.Principal; // Windows 보안 주체(사용자, 그룹) 관련 작업
using System.Runtime.InteropServices; // Win32 API 호출을 위한 네임스페이스
using System.Text;
using System.Net; // 네트워크 자격증명 관련 작업
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Linq;

namespace WindowsSSO
{
    /// <summary>
    /// Windows 기본 인증을 이용한 SSO 구현 클래스
    /// 현재 로그인된 Windows 사용자의 정보를 가져오고 검증하는 기능을 제공
    /// </summary>
    public class WindowsAuthenticationSSO
    {
        /// <summary>
        /// 현재 로그인된 Windows 사용자의 상세 정보를 출력하는 메소드
        /// WindowsIdentity.GetCurrent()를 사용하여 현재 프로세스를 실행중인 사용자의 정보를 가져옴
        /// </summary>
        public static void GetCurrentUserInfo()
        {
            try
            {
                // WindowsIdentity.GetCurrent(): 현재 스레드/프로세스의 Windows 사용자 정보를 가져옴
                // 이는 Windows 로그인 세션과 직접 연결되어 있음
                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                Console.WriteLine("=== Windows 사용자 기본 정보 ===");

                // Name: 도메인\사용자명 형태로 반환 (예: DOMAIN\johndoe 또는 COMPUTER\johndoe)
                Console.WriteLine($"사용자명: {identity.Name}");

                // AuthenticationType: 인증 방식을 나타냄 (예: Kerberos, NTLM, Negotiate)
                Console.WriteLine($"인증 타입: {identity.AuthenticationType}");

                // Token: Windows 액세스 토큰의 핸들값 - 실제 Windows 보안 토큰을 가리킴
                Console.WriteLine($"토큰 핸들: {identity.Token}");

                // IsAuthenticated: 사용자가 성공적으로 인증되었는지 여부
                Console.WriteLine($"인증 상태: {identity.IsAuthenticated}");

                // User.Value: 사용자의 SID(Security Identifier) - Windows에서 사용자를 고유하게 식별하는 값
                Console.WriteLine($"사용자 SID: {identity.User?.Value ?? "N/A"}");

                // IsSystem: 시스템 계정인지 확인
                Console.WriteLine($"시스템 계정 여부: {identity.IsSystem}");

                // IsGuest: 게스트 계정인지 확인  
                Console.WriteLine($"게스트 계정 여부: {identity.IsGuest}");

                // IsAnonymous: 익명 사용자인지 확인
                Console.WriteLine($"익명 사용자 여부: {identity.IsAnonymous}");

                Console.WriteLine("\n=== 사용자 그룹 정보 ===");

                // Groups: 사용자가 속한 모든 Windows 그룹의 SID 목록
                // 도메인 그룹, 로컬 그룹, 내장 그룹 등이 모두 포함됨
                if (identity.Groups != null)
                {
                    Console.WriteLine($"총 그룹 수: {identity.Groups.Count}");

                    foreach (IdentityReference group in identity.Groups)
                    {
                        try
                        {
                            // SID를 사람이 읽을 수 있는 이름으로 변환
                            // 예: S-1-5-32-544 -> BUILTIN\Administrators
                            NTAccount ntAccount = (NTAccount)group.Translate(typeof(NTAccount));
                            Console.WriteLine($"  - {ntAccount.Value}");
                        }
                        catch (Exception ex)
                        {
                            // 변환에 실패하면 SID 그대로 출력 (고아 SID이거나 접근 권한이 없는 경우)
                            Console.WriteLine($"  - {group.Value} (이름 변환 실패: {ex.Message})");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("그룹 정보를 가져올 수 없습니다.");
                }

                // 추가적인 보안 정보 출력
                Console.WriteLine("\n=== 추가 보안 정보 ===");

                // 현재 사용자가 관리자 권한을 가지고 있는지 확인
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                Console.WriteLine($"관리자 권한: {isAdmin}");

                // 일반적인 Windows 내장 역할 확인
                Console.WriteLine($"사용자 그룹 멤버: {principal.IsInRole(WindowsBuiltInRole.User)}");
                Console.WriteLine($"파워 사용자: {principal.IsInRole(WindowsBuiltInRole.PowerUser)}");
                Console.WriteLine($"백업 운영자: {principal.IsInRole(WindowsBuiltInRole.BackupOperator)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"사용자 정보 조회 중 오류 발생: {ex.Message}");
                Console.WriteLine($"스택 추적: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 현재 사용자가 유효하게 인증되었는지 검증하는 메소드
        /// SSO 구현에서 가장 기본적인 검증 로직
        /// </summary>
        /// <returns>인증 성공 시 true, 실패 시 false</returns>
        public static bool ValidateCurrentUser()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                // null 체크와 인증 상태 동시 확인
                // identity가 null이거나 IsAuthenticated가 false면 인증 실패로 간주
                bool isValid = identity != null && identity.IsAuthenticated;

                Console.WriteLine($"사용자 검증 결과: {(isValid ? "성공" : "실패")}");

                if (isValid)
                {
                    Console.WriteLine($"검증된 사용자: {identity.Name}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"사용자 검증 중 오류 발생: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Active Directory를 이용한 고급 SSO 기능 구현 클래스
    /// AD에서 사용자 정보를 조회하고 도메인 인증을 수행하는 기능 제공
    /// </summary>
    public class ActiveDirectorySSO
    {
        /// <summary>
        /// Active Directory에서 현재 사용자의 상세 정보를 가져오는 메소드
        /// Windows 기본 정보보다 더 많은 비즈니스 관련 정보를 제공 (이메일, 부서 등)
        /// </summary>
        /// <param name="domain">특정 도메인 지정 (null이면 현재 도메인 사용)</param>
        /// <returns>사용자 정보 객체 또는 null</returns>
        public static UserInfo GetUserInfoFromAD(string domain = null)
        {
            Console.WriteLine("\n=== Active Directory 사용자 정보 조회 ===");

            try
            {
                // PrincipalContext: AD와 연결하기 위한 컨텍스트 객체
                // ContextType.Domain: 도메인 환경에서 작업한다는 의미
                PrincipalContext context;
                if (string.IsNullOrEmpty(domain))
                {
                    // 도메인을 지정하지 않으면 현재 컴퓨터가 속한 도메인 사용
                    context = new PrincipalContext(ContextType.Machine);
                    Console.WriteLine("현재 도메인에서 사용자 정보 조회 중...");
                }
                else
                {
                    // 특정 도메인 지정
                    context = new PrincipalContext(ContextType.Domain, domain);
                    Console.WriteLine($"지정된 도메인 '{domain}'에서 사용자 정보 조회 중...");
                }

                // UserPrincipal.Current: 현재 로그인한 사용자의 AD 정보를 가져옴
                // 이는 AD의 실제 사용자 객체에 해당하며, 풍부한 속성 정보를 제공
                UserPrincipal user = UserPrincipal.Current;

                if (user != null)
                {
                    Console.WriteLine($"AD에서 사용자 '{user.SamAccountName}' 정보를 성공적으로 조회했습니다.");

                    var userInfo = new UserInfo
                    {
                        // SamAccountName: Windows 로그인에 사용되는 계정명 (도메인 제외)
                        Username = user.SamAccountName,

                        // DisplayName: AD에 저장된 사용자의 표시 이름 (한글명 등)
                        DisplayName = user.DisplayName,

                        // EmailAddress: AD에 저장된 이메일 주소
                        Email = user.EmailAddress,

                        // Description: 사용자 설명 필드 (부서 정보 등이 저장되기도 함)
                        Department = user.Description,

                        // LastLogon: 마지막 로그온 시간
                        LastLogon = user.LastLogon,

                        // Enabled: 계정 활성화 여부
                        IsEnabled = user.Enabled ?? false,

                        // DistinguishedName: AD에서 사용자의 전체 경로
                        DistinguishedName = user.DistinguishedName,

                        // UserPrincipalName: 사용자의 UPN (예: user@domain.com)
                        UserPrincipalName = user.UserPrincipalName
                    };

                    // 추가 AD 속성 조회 시도
                    try
                    {
                        // 사용자가 속한 그룹 정보 조회
                        var groups = user.GetAuthorizationGroups();
                        userInfo.Groups = groups.Select(g => g.Name).ToList();
                        Console.WriteLine($"사용자가 속한 그룹 수: {userInfo.Groups.Count}");
                    }
                    catch (Exception groupEx)
                    {
                        Console.WriteLine($"그룹 정보 조회 실패: {groupEx.Message}");
                        userInfo.Groups = new List<string>();
                    }

                    return userInfo;
                }
                else
                {
                    Console.WriteLine("AD에서 현재 사용자를 찾을 수 없습니다.");
                    return null;
                }
            }
            catch (PrincipalServerDownException ex)
            {
                Console.WriteLine($"도메인 컨트롤러에 연결할 수 없습니다: {ex.Message}");
                return null;
            }         
            catch (Exception ex)
            {
                Console.WriteLine($"AD 사용자 정보 조회 실패: {ex.Message}");
                Console.WriteLine($"상세 오류: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 사용자명과 비밀번호로 도메인 인증을 수행하는 메소드
        /// SSO가 아닌 일반적인 로그인 검증에 사용
        /// </summary>
        /// <param name="username">사용자명</param>
        /// <param name="password">비밀번호</param>
        /// <param name="domain">도메인 (선택사항)</param>
        /// <returns>인증 성공 시 true</returns>
        public static bool AuthenticateUser(string username, string password, string domain = null)
        {
            Console.WriteLine($"\n=== 도메인 사용자 인증: {username} ===");

            try
            {
                PrincipalContext context;
                if (string.IsNullOrEmpty(domain))
                {
                    context = new PrincipalContext(ContextType.Domain);
                    Console.WriteLine("현재 도메인에서 인증 시도 중...");
                }
                else
                {
                    context = new PrincipalContext(ContextType.Domain, domain);
                    Console.WriteLine($"도메인 '{domain}'에서 인증 시도 중...");
                }

                // ValidateCredentials: AD에 실제로 사용자명/비밀번호를 검증 요청
                // 이는 실제 도메인 컨트롤러와 통신하여 인증을 수행
                bool isAuthenticated = context.ValidateCredentials(username, password);

                Console.WriteLine($"인증 결과: {(isAuthenticated ? "성공" : "실패")}");

                return isAuthenticated;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"사용자 인증 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 특정 사용자의 그룹 멤버십을 조회하는 메소드
        /// 권한 관리 시스템에서 사용자의 역할을 확인하는데 사용
        /// </summary>
        /// <param name="username">조회할 사용자명</param>
        /// <returns>그룹 이름 목록</returns>
        public static List<string> GetUserGroups(string username)
        {
            Console.WriteLine($"\n=== 사용자 '{username}' 그룹 조회 ===");

            var groups = new List<string>();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(context, username);
                    if (user != null)
                    {
                        // GetAuthorizationGroups(): 사용자가 직접/간접적으로 속한 모든 그룹 반환
                        // 중첩된 그룹 멤버십도 모두 포함
                        var authGroups = user.GetAuthorizationGroups();

                        foreach (var group in authGroups)
                        {
                            groups.Add(group.Name);
                            Console.WriteLine($"  - {group.Name}");
                        }

                        Console.WriteLine($"총 {groups.Count}개의 그룹에 속해있습니다.");
                    }
                    else
                    {
                        Console.WriteLine("사용자를 찾을 수 없습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"그룹 조회 실패: {ex.Message}");
            }

            return groups;
        }
    }

    /// <summary>
    /// SSPI(Security Support Provider Interface)를 이용한 Windows 인증 구현
    /// Win32 API를 직접 호출하여 저수준 인증 토큰을 다루는 고급 기능
    /// </summary>
    public class SSPIAuthentication
    {
        // Win32 API 함수들 - secur32.dll에서 제공하는 보안 함수들

        /// <summary>
        /// 보안 자격증명 핸들을 획득하는 Win32 API
        /// Kerberos나 NTLM 등의 인증 패키지에 대한 자격증명을 가져옴
        /// </summary>
        [DllImport("secur32.dll", SetLastError = true)]
        static extern int AcquireCredentialsHandle(
            string pszPrincipal,        // 주체 이름 (null이면 현재 로그온 세션 사용)
            string pszPackage,          // 보안 패키지 이름 ("Negotiate", "Kerberos", "NTLM" 등)
            int fCredentialUse,         // 자격증명 사용 방향 (INBOUND=1, OUTBOUND=2)
            IntPtr pvLogonID,           // 로그온 세션 ID (null이면 현재 세션)
            IntPtr pAuthData,           // 인증 데이터 (null이면 기본값 사용)
            IntPtr pGetKeyFn,           // 키 획득 콜백 함수
            IntPtr pvGetKeyArgument,    // 콜백 함수 인자
            ref SecHandle phCredential, // 출력: 자격증명 핸들
            ref TimeStamp ptsExpiry);   // 출력: 만료 시간

        /// <summary>
        /// 보안 컨텍스트를 초기화하는 Win32 API
        /// 실제 인증 토큰을 생성하고 교환하는 과정의 핵심
        /// </summary>
        [DllImport("secur32.dll", SetLastError = true)]
        static extern int InitializeSecurityContext(
            ref SecHandle phCredential,    // 자격증명 핸들
            IntPtr phContext,              // 기존 컨텍스트 (첫 호출시 null)
            string pszTargetName,          // 대상 서비스 이름
            int fContextReq,               // 컨텍스트 요구사항 플래그
            int Reserved1,                 // 예약됨 (0)
            int TargetDataRep,             // 대상 데이터 표현
            IntPtr pInput,                 // 입력 토큰
            int Reserved2,                 // 예약됨 (0)  
            out SecHandle phNewContext,    // 출력: 새 컨텍스트 핸들
            out SecBufferDesc pOutput,     // 출력: 보안 토큰
            out uint pfContextAttr,        // 출력: 컨텍스트 속성
            out TimeStamp ptsExpiry);      // 출력: 만료 시간

        /// <summary>
        /// 보안 핸들 구조체 - Windows 보안 객체에 대한 핸들을 나타냄
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SecHandle
        {
            public IntPtr dwLower;  // 핸들의 하위 부분
            public IntPtr dwUpper;  // 핸들의 상위 부분
        }

        /// <summary>
        /// 시간 스탬프 구조체 - 64비트 시간값을 32비트 두 개로 분할
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TimeStamp
        {
            public uint dwLowDateTime;   // 시간의 하위 32비트
            public uint dwHighDateTime;  // 시간의 상위 32비트
        }

        /// <summary>
        /// 보안 버퍼 설명자 - 보안 토큰 데이터를 담는 버퍼 구조
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SecBufferDesc
        {
            public int ulVersion;    // 버전 정보
            public int cBuffers;     // 버퍼 개수
            public IntPtr pBuffers;  // 버퍼 배열 포인터
        }

        /// <summary>
        /// SSPI를 이용하여 Negotiate 인증 토큰을 생성하는 메소드
        /// 이 토큰은 HTTP Authorization 헤더나 다른 인증 시나리오에서 사용 가능
        /// </summary>
        /// <returns>성공 시 토큰 정보, 실패 시 null</returns>
        public static string GetNegotiateToken()
        {
            Console.WriteLine("\n=== SSPI Negotiate 토큰 생성 ===");

            SecHandle credential = new SecHandle();
            TimeStamp expiry = new TimeStamp();

            try
            {
                // "Negotiate" 패키지로 자격증명 획득
                // Negotiate는 Kerberos와 NTLM 중 적절한 것을 자동 선택
                int result = AcquireCredentialsHandle(
                    null,           // 현재 로그온 세션 사용
                    "Negotiate",    // Negotiate 보안 패키지 사용
                    2,              // SECPKG_CRED_OUTBOUND - 클라이언트 자격증명
                    IntPtr.Zero,    // 현재 로그온 세션 ID 사용
                    IntPtr.Zero,    // 기본 인증 데이터 사용
                    IntPtr.Zero,    // 키 획득 콜백 없음
                    IntPtr.Zero,    // 콜백 인자 없음
                    ref credential, // 출력: 자격증명 핸들
                    ref expiry);    // 출력: 만료 시간

                Console.WriteLine($"AcquireCredentialsHandle 결과 코드: 0x{result:X8}");

                if (result == 0) // SEC_E_OK (성공)
                {
                    Console.WriteLine("SSPI 자격증명 획득 성공");
                    Console.WriteLine($"자격증명 핸들: Lower=0x{credential.dwLower:X}, Upper=0x{credential.dwUpper:X}");

                    // 만료 시간 계산 (FILETIME을 DateTime으로 변환)
                    long fileTime = ((long)expiry.dwHighDateTime << 32) | expiry.dwLowDateTime;

                    try
                    {
                        if (fileTime > 0 && fileTime < DateTime.MaxValue.ToFileTime())
                        {
                            DateTime expiryDateTime = DateTime.FromFileTime(fileTime);
                            Console.WriteLine($"자격증명 만료 시간: {expiryDateTime}");
                        }
                        else
                        {
                            Console.WriteLine("만료 시간 없음 또는 무효");
                        }
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        Console.WriteLine("FILETIME → DateTime 변환 실패: " + ex.Message);
                    }

                    return "SSPI Negotiate 토큰을 성공적으로 획득했습니다.";
                }
                else
                {
                    // 일반적인 오류 코드들과 의미
                    string errorMessage = result switch
                    {
                        unchecked((int)0x80090300) => "SEC_E_INSUFFICIENT_MEMORY - 메모리 부족",
                        unchecked((int)0x80090301) => "SEC_E_INVALID_HANDLE - 잘못된 핸들",
                        unchecked((int)0x80090302) => "SEC_E_UNSUPPORTED_FUNCTION - 지원되지 않는 기능",
                        unchecked((int)0x80090303) => "SEC_E_TARGET_UNKNOWN - 알 수 없는 대상",
                        unchecked((int)0x80090304) => "SEC_E_INTERNAL_ERROR - 내부 오류",
                        unchecked((int)0x80090305) => "SEC_E_SECPKG_NOT_FOUND - 보안 패키지를 찾을 수 없음",
                        unchecked((int)0x80090308) => "SEC_E_INVALID_TOKEN - 잘못된 토큰",
                        unchecked((int)0x8009030C) => "SEC_E_LOGON_DENIED - 로그온 거부",
                        _ => "알 수 없는 오류"
                    };

                    Console.WriteLine($"SSPI 자격증명 획득 실패: {errorMessage} (0x{result:X8})");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSPI 호출 중 예외 발생: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 현재 시스템의 SSPI 패키지 정보를 조회하는 메소드
        /// 디버깅이나 시스템 진단에 유용
        /// </summary>
        public static void EnumerateSecurityPackages()
        {
            Console.WriteLine("\n=== 사용 가능한 보안 패키지 ===");

            // 실제 구현에서는 EnumerateSecurityPackages API를 호출해야 하지만
            // 여기서는 일반적인 패키지들을 나열
            string[] commonPackages = {
                "Negotiate",    // Kerberos/NTLM 자동 선택
                "Kerberos",     // Kerberos v5 인증
                "NTLM",         // NTLM 인증
                "Digest",       // HTTP Digest 인증
                "Schannel",     // SSL/TLS 인증
                "Microsoft Unified Security Protocol Provider"
            };

            foreach (string package in commonPackages)
            {
                Console.WriteLine($"  - {package}");
            }
        }
    }

    /// <summary>
    /// HTTP 클라이언트에서 Windows 인증을 사용하는 예제 클래스
    /// 웹 서비스 호출 시 현재 사용자의 자격증명을 자동으로 전달
    /// </summary>
    public class HttpWindowsAuth
    {
        /// <summary>
        /// WebClient를 사용하여 Windows 인증으로 웹 서비스 호출
        /// 레거시 방식이지만 간단하고 직관적
        /// </summary>
        /// <param name="url">호출할 웹 서비스 URL</param>
        /// <returns>서버 응답 문자열</returns>
        public static async System.Threading.Tasks.Task<string> CallWebServiceWithSSO(string url)
        {
            Console.WriteLine($"\n=== WebClient로 Windows 인증 호출: {url} ===");

            try
            {                
                using (var client = new WebClient())
                {
                    // DefaultCredentials: 현재 로그인한 사용자의 자격증명 사용
                    // 이는 Kerberos 티켓이나 NTLM 해시를 자동으로 전송
                    client.Credentials = CredentialCache.DefaultCredentials;

                    Console.WriteLine("기본 자격증명(DefaultCredentials) 사용 설정");

                    // 또는 명시적으로 네트워크 자격증명 사용 가능
                    // client.Credentials = CredentialCache.DefaultNetworkCredentials;

                    // 추가 헤더 설정 예제
                    client.Headers.Add("User-Agent", "Windows-SSO-Test-Client/1.0");

                    Console.WriteLine("웹 서비스 호출 중...");
                    string response = await client.DownloadStringTaskAsync(url);

                    Console.WriteLine($"응답 받음: {response.Length} 문자");
                    return response;
                }
            }
            catch (WebException webEx)
            {
                // HTTP 상태 코드별 처리
                if (webEx.Response is HttpWebResponse httpResponse)
                {
                    Console.WriteLine($"HTTP 오류: {httpResponse.StatusCode} ({(int)httpResponse.StatusCode})");

                    switch (httpResponse.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            Console.WriteLine("401 Unauthorized - Windows 인증이 필요하거나 자격증명이 잘못되었습니다.");
                            break;
                        case HttpStatusCode.Forbidden:
                            Console.WriteLine("403 Forbidden - 인증은 되었지만 접근 권한이 없습니다.");
                            break;
                        case HttpStatusCode.NotFound:
                            Console.WriteLine("404 Not Found - 요청한 리소스를 찾을 수 없습니다.");
                            break;
                    }
                }
                Console.WriteLine($"웹 예외: {webEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"웹 서비스 호출 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// HttpClient를 사용하여 Windows 인증으로 웹 서비스 호출
        /// 현대적이고 권장되는 방식
        /// </summary>
        /// <param name="url">호출할 웹 서비스 URL</param>
        /// <returns>서버 응답 문자열</returns>
        public static async System.Threading.Tasks.Task<string> CallWebServiceWithHttpClient(string url)
        {
            Console.WriteLine($"\n=== HttpClient로 Windows 인증 호출: {url} ===");

            try
            {
                // HttpClientHandler 설정으로 Windows 인증 활성화
                var handler = new HttpClientHandler()
                {
                    UseDefaultCredentials = true,  // 현재 사용자 자격증명 자동 사용
                    PreAuthenticate = true,        // 첫 요청부터 인증 헤더 포함

                    // 프록시 설정 (필요한 경우)
                    UseProxy = false,

                    // 쿠키 처리 설정
                    UseCookies = true
                };

                using (var client = new System.Net.Http.HttpClient(handler))
                {
                    // 타임아웃 설정
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // 기본 헤더 설정
                    client.DefaultRequestHeaders.Add("User-Agent", "HttpClient-Windows-SSO/1.0");

                    Console.WriteLine("HttpClient 설정 완료, 요청 전송 중...");

                    var response = await client.GetAsync(url);

                    Console.WriteLine($"HTTP 상태 코드: {response.StatusCode}");
                    Console.WriteLine($"응답 헤더:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"응답 내용 길이: {content.Length} 문자");
                        return content;
                    }
                    else
                    {
                        Console.WriteLine($"HTTP 오류: {response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }
                }
            }
            catch (System.Net.Http.HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP 요청 예외: {httpEx.Message}");
                return null;
            }
            catch (TaskCanceledException timeoutEx)
            {
                Console.WriteLine($"요청 타임아웃: {timeoutEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HttpClient 호출 실패: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 사용자 정보를 담는 데이터 클래스
    /// Active Directory에서 조회한 사용자 정보를 구조화하여 저장
    /// </summary>
    public class UserInfo
    {
        /// <summary>Windows 로그인 사용자명 (SAM Account Name)</summary>
        public string Username { get; set; }

        /// <summary>사용자의 표시 이름 (Display Name)</summary>
        public string DisplayName { get; set; }

        /// <summary>이메일 주소</summary>
        public string Email { get; set; }

        /// <summary>부서 또는 설명</summary>
        public string Department { get; set; }

        /// <summary>마지막 로그온 시간</summary>
        public DateTime? LastLogon { get; set; }

        /// <summary>계정 활성화 상태</summary>
        public bool IsEnabled { get; set; }

        /// <summary>Active Directory의 고유 이름 (Distinguished Name)</summary>
        public string DistinguishedName { get; set; }

        /// <summary>사용자 주체 이름 (User Principal Name)</summary>
        public string UserPrincipalName { get; set; }

        /// <summary>사용자가 속한 그룹 목록</summary>
        public List<string> Groups { get; set; } = new List<string>();

        /// <summary>
        /// 사용자 정보를 읽기 쉬운 형태로 포맷팅
        /// </summary>
        /// <returns>포맷된 사용자 정보 문자열</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== 사용자 정보 ===");
            sb.AppendLine($"사용자명: {Username ?? "N/A"}");
            sb.AppendLine($"표시 이름: {DisplayName ?? "N/A"}");
            sb.AppendLine($"이메일: {Email ?? "N/A"}");
            sb.AppendLine($"부서: {Department ?? "N/A"}");
            sb.AppendLine($"마지막 로그인: {LastLogon?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");
            sb.AppendLine($"계정 활성화: {IsEnabled}");
            sb.AppendLine($"UPN: {UserPrincipalName ?? "N/A"}");
            sb.AppendLine($"DN: {DistinguishedName ?? "N/A"}");

            if (Groups != null && Groups.Count > 0)
            {
                sb.AppendLine($"그룹 ({Groups.Count}개):");
                foreach (string group in Groups)
                {
                    sb.AppendLine($"  - {group}");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// SSO 관련 기능을 통합 관리하는 매니저 클래스
    /// 다른 클래스들의 기능을 조합하여 완전한 SSO 솔루션 제공
    /// </summary>
    public class SSOManager
    {
        /// <summary>
        /// 현재 사용자가 인증되었는지 확인하는 메소드
        /// SSO의 가장 기본적인 기능
        /// </summary>
        /// <returns>인증 상태</returns>
        public static bool IsUserAuthenticated()
        {
            Console.WriteLine("\n=== SSO 사용자 인증 상태 확인 ===");
            bool result = WindowsAuthenticationSSO.ValidateCurrentUser();
            Console.WriteLine($"최종 인증 상태: {(result ? "인증됨" : "인증되지 않음")}");
            return result;
        }

        /// <summary>
        /// 현재 사용자의 통합 정보를 가져오는 메소드
        /// Windows Identity와 Active Directory 정보를 조합
        /// </summary>
        /// <returns>사용자 정보 객체</returns>
        public static UserInfo GetCurrentUserInfo()
        {
            Console.WriteLine("\n=== SSO 사용자 정보 조회 ===");

            if (!IsUserAuthenticated())
            {
                Console.WriteLine("사용자가 인증되지 않아 정보를 조회할 수 없습니다.");
                return null;
            }

            // 1단계: AD에서 상세 정보 조회 시도
            Console.WriteLine("1단계: Active Directory에서 사용자 정보 조회 시도...");
            var userInfo = ActiveDirectorySSO.GetUserInfoFromAD();
            if (userInfo != null)
            {
                Console.WriteLine("AD에서 사용자 정보를 성공적으로 가져왔습니다.");
                return userInfo;
            }

            // 2단계: AD 조회 실패 시 Windows Identity에서 기본 정보 가져오기
            Console.WriteLine("2단계: AD 조회 실패, Windows Identity에서 기본 정보 조회...");
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var fallbackUserInfo = new UserInfo
                {
                    Username = identity.Name,
                    DisplayName = identity.Name,
                    IsEnabled = identity.IsAuthenticated,
                    Groups = new List<string>()
                };

                // 그룹 정보 추가 (Windows Identity에서)
                if (identity.Groups != null)
                {
                    foreach (IdentityReference group in identity.Groups)
                    {
                        try
                        {
                            NTAccount ntAccount = (NTAccount)group.Translate(typeof(NTAccount));
                            fallbackUserInfo.Groups.Add(ntAccount.Value);
                        }
                        catch
                        {
                            fallbackUserInfo.Groups.Add(group.Value);
                        }
                    }
                }

                Console.WriteLine("Windows Identity에서 기본 정보를 가져왔습니다.");
                return fallbackUserInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Windows Identity 정보 조회도 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 현재 사용자가 특정 역할(그룹)에 속하는지 확인
        /// 권한 기반 접근 제어(RBAC)의 핵심 기능
        /// </summary>
        /// <param name="roleName">확인할 역할/그룹 이름</param>
        /// <returns>역할 보유 여부</returns>
        public static bool HasRole(string roleName)
        {
            Console.WriteLine($"\n=== 역할 확인: {roleName} ===");

            try
            {
                // WindowsPrincipal을 사용하여 역할 확인
                // 이는 Windows 그룹 멤버십을 직접 검사
                WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                bool hasRole = principal.IsInRole(roleName);

                Console.WriteLine($"역할 '{roleName}' 보유 여부: {hasRole}");

                // 추가: 역할 이름의 다양한 형태로 재시도
                if (!hasRole)
                {
                    // 도메인\그룹 형태로 재시도
                    string domainRole = $"{Environment.UserDomainName}\\{roleName}";
                    hasRole = principal.IsInRole(domainRole);
                    Console.WriteLine($"도메인 형태 '{domainRole}' 확인 결과: {hasRole}");
                }

                return hasRole;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"역할 확인 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 사용자가 관리자 권한을 가지고 있는지 확인
        /// 가장 일반적으로 사용되는 권한 확인 기능
        /// </summary>
        /// <returns>관리자 권한 여부</returns>
        public static bool IsAdmin()
        {
            Console.WriteLine("\n=== 관리자 권한 확인 ===");

            try
            {
                WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());

                // WindowsBuiltInRole.Administrator: Windows 내장 관리자 역할
                // 이는 로컬 Administrators 그룹 멤버십을 확인
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                Console.WriteLine($"관리자 권한 보유: {isAdmin}");

                // 추가 정보: UAC(User Account Control) 상태 확인
                if (isAdmin)
                {
                    Console.WriteLine("주의: UAC가 활성화된 경우 실제 관리자 권한 사용을 위해 프로세스 승격이 필요할 수 있습니다.");
                }

                return isAdmin;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"관리자 권한 확인 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 시스템 환경 정보를 출력하는 유틸리티 메소드
        /// SSO 문제 진단에 유용한 정보들을 제공
        /// </summary>
        public static void PrintSystemInfo()
        {
            Console.WriteLine("\n=== 시스템 환경 정보 ===");

            try
            {
                Console.WriteLine($"컴퓨터명: {Environment.MachineName}");
                Console.WriteLine($"사용자 도메인: {Environment.UserDomainName}");
                Console.WriteLine($"현재 사용자: {Environment.UserName}");
                Console.WriteLine($"OS 버전: {Environment.OSVersion}");
                Console.WriteLine($"프로세서 수: {Environment.ProcessorCount}");
                Console.WriteLine($"64비트 OS: {Environment.Is64BitOperatingSystem}");
                Console.WriteLine($"64비트 프로세스: {Environment.Is64BitProcess}");
                Console.WriteLine($"CLR 버전: {Environment.Version}");

                // 도메인 조인 상태 확인
                bool isDomainJoined = Environment.UserDomainName != Environment.MachineName;
                Console.WriteLine($"도메인 조인 상태: {(isDomainJoined ? "조인됨" : "워크그룹")}");

                // 현재 시간 (Kerberos 인증에서 시간 동기화가 중요)
                Console.WriteLine($"현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"UTC 시간: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"시스템 정보 조회 중 오류: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 메인 테스트 프로그램 클래스
    /// 모든 SSO 기능을 순차적으로 테스트하고 결과를 출력
    /// </summary>
    class Program
    {
        /// <summary>
        /// 프로그램의 진입점
        /// 각 SSO 기능을 단계별로 테스트하고 결과를 상세히 출력
        /// </summary>
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("====================================================");
            Console.WriteLine("=== Windows Credential Provider SSO 테스트 프로그램 ===");
            Console.WriteLine("====================================================");
            Console.WriteLine($"테스트 시작 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // 0. 시스템 환경 정보 출력
                SSOManager.PrintSystemInfo();

                // 1. 기본 Windows 인증 테스트
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("1. Windows 기본 인증 정보 테스트");
                Console.WriteLine(new string('=', 60));
                WindowsAuthenticationSSO.GetCurrentUserInfo();

                // 2. SSO 매니저를 통한 통합 테스트
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("2. SSO 매니저 통합 테스트");
                Console.WriteLine(new string('=', 60));

                if (SSOManager.IsUserAuthenticated())
                {
                    var userInfo = SSOManager.GetCurrentUserInfo();
                    if (userInfo != null)
                    {
                        Console.WriteLine(userInfo.ToString());
                    }

                    // 권한 테스트
                    Console.WriteLine("\n--- 권한 테스트 ---");
                    Console.WriteLine($"관리자 권한: {SSOManager.IsAdmin()}");
                    Console.WriteLine($"Users 그룹 멤버: {SSOManager.HasRole("Users")}");
                    Console.WriteLine($"Administrators 그룹 멤버: {SSOManager.HasRole("Administrators")}");
                    Console.WriteLine($"Domain Users 그룹 멤버: {SSOManager.HasRole("Domain Users")}");
                }
                else
                {
                    Console.WriteLine("❌ 사용자가 인증되지 않았습니다.");
                }

                // 3. Active Directory 상세 테스트
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("3. Active Directory 상세 테스트");
                Console.WriteLine(new string('=', 60));

                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                var adUserInfo = ActiveDirectorySSO.GetUserInfoFromAD(Environment.UserDomainName);
                if (adUserInfo != null)
                {
                    Console.WriteLine("✅ AD 사용자 정보 조회 성공");
                    Console.WriteLine(adUserInfo.ToString());

                    // 사용자 그룹 정보 별도 조회
                    var groups = ActiveDirectorySSO.GetUserGroups(adUserInfo.Username);
                    Console.WriteLine($"AD 그룹 조회 결과: {groups.Count}개 그룹");
                }
                else
                {
                    Console.WriteLine("❌ AD 사용자 정보를 가져올 수 없습니다.");
                    Console.WriteLine("   - 도메인에 조인되지 않았거나");
                    Console.WriteLine("   - 도메인 컨트롤러에 접근할 수 없거나");
                    Console.WriteLine("   - 네트워크 연결에 문제가 있을 수 있습니다.");
                }

                // 4. SSPI 저수준 인증 테스트
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("4. SSPI 저수준 인증 테스트");
                Console.WriteLine(new string('=', 60));

                // 사용 가능한 보안 패키지 나열
                SSPIAuthentication.EnumerateSecurityPackages();

                // Negotiate 토큰 생성 테스트
                string token = SSPIAuthentication.GetNegotiateToken();
                if (token != null)
                {
                    Console.WriteLine("✅ SSPI 토큰 생성 성공");
                    Console.WriteLine(token);
                }
                else
                {
                    Console.WriteLine("❌ SSPI 토큰 생성 실패");
                }

                // 5. HTTP 클라이언트 Windows 인증 테스트
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("5. HTTP Windows 인증 테스트");
                Console.WriteLine(new string('=', 60));

                // 테스트용 URL들 (실제 환경에 맞게 변경 필요)
                string[] testUrls = {
                    "http://localhost/api/test",              // 로컬 IIS 테스트
                    "https://intranet.company.com/api/auth",  // 회사 인트라넷
                    "http://sharepoint.company.com/_api/web"  // SharePoint API
                };

                foreach (string testUrl in testUrls)
                {
                    Console.WriteLine($"\n--- 테스트 URL: {testUrl} ---");
                    Console.WriteLine("⚠️  실제 테스트를 위해서는 Windows 인증을 지원하는 웹 서비스가 필요합니다.");
                    Console.WriteLine("   IIS에서 Windows Authentication을 활성화하고 Anonymous Authentication을 비활성화해야 합니다.");

                    // 실제 호출은 주석 처리 (테스트 환경에 따라 활성화)
                    /*
                    try
                    {
                        Console.WriteLine("WebClient 방식 테스트 중...");
                        string response1 = await HttpWindowsAuth.CallWebServiceWithSSO(testUrl);
                        if (response1 != null)
                        {
                            Console.WriteLine($"✅ WebClient 호출 성공: {response1.Substring(0, Math.Min(100, response1.Length))}...");
                        }
                        
                        Console.WriteLine("HttpClient 방식 테스트 중...");
                        string response2 = await HttpWindowsAuth.CallWebServiceWithHttpClient(testUrl);
                        if (response2 != null)
                        {
                            Console.WriteLine($"✅ HttpClient 호출 성공: {response2.Substring(0, Math.Min(100, response2.Length))}...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ HTTP 테스트 실패: {ex.Message}");
                    }
                    */
                }

                // 6. 종합 결과 및 권장사항
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("6. 테스트 결과 요약 및 권장사항");
                Console.WriteLine(new string('=', 60));

                Console.WriteLine("✅ 완료된 테스트:");
                Console.WriteLine("   - Windows 기본 인증 정보 조회");
                Console.WriteLine("   - 사용자 권한 및 그룹 멤버십 확인");
                Console.WriteLine("   - SSPI 토큰 생성");

                if (adUserInfo != null)
                {
                    Console.WriteLine("   - Active Directory 사용자 정보 조회");
                }

                Console.WriteLine("\n📋 SSO 구현을 위한 권장사항:");
                Console.WriteLine("   1. IIS에서 Windows Authentication 활성화");
                Console.WriteLine("   2. web.config에서 <authentication mode=\"Windows\" /> 설정");
                Console.WriteLine("   3. 브라우저에서 자동 로그온 설정 (Intranet 영역)");
                Console.WriteLine("   4. Kerberos 사용을 위한 SPN(Service Principal Name) 등록");
                Console.WriteLine("   5. 클라이언트와 서버 간 시간 동기화");
                Console.WriteLine("   6. 방화벽에서 필요한 포트 개방 (Kerberos: 88, LDAP: 389/636)");

                Console.WriteLine("\n🔧 문제 해결 팁:");
                Console.WriteLine("   - 401 오류 지속: SPN 설정 및 시간 동기화 확인");
                Console.WriteLine("   - 느린 인증: DNS 설정 및 도메인 컨트롤러 연결 최적화");
                Console.WriteLine("   - 그룹 정보 누락: 사용자 권한 및 AD 스키마 확인");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 테스트 중 예상치 못한 오류 발생:");
                Console.WriteLine($"   오류 메시지: {ex.Message}");
                Console.WriteLine($"   오류 위치: {ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("테스트 완료");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine($"테스트 종료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}