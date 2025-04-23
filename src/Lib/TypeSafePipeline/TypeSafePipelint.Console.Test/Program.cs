using TypeSafePipelint.Console.Test.Domain.CommandHandlers;
using TypeSafePipelint.Console.Test.Domain.Commands;
using TypeSafePipelint.Console.Test.Domain.Models;
using TypeSafePipelint.Console.Test;
using TypeSafePipelint.Console.Test.Infra;
using TypeSafePipeline.lib.Pipeline;
using TypeSafePipeline.lib.ResponseMatcher;
using TypeSafePipelint.Console.Test.Domain.Adapt;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        //byte[] bytes = new byte[4] { 0x12, 0x13, 0x01, 0x15 };
        //var reverseBytes = bytes.Reverse();

        //var result = BitConverter.ToUInt32(bytes.ToArray(), 0);
        //var result1 = BitConverter.ToUInt32(reverseBytes.ToArray(), 0);

        //Console.WriteLine($"General : {result}, Reverse {result1}");
        // 인프라 컴포넌트 설정
         var logger = new ConsoleLogger();

        // 메디에이터 설정
        var mediator = new Mediator(logger);
        mediator.RegisterHandler<CreateUserCommand, User>(new CreateUserCommandHandler());
        mediator.RegisterHandler<CreateAdminCommand, Admin>(new CreateAdminCommandHandler());

        Console.WriteLine("===== 파이프라인 패턴 사용 예제 =====");

        // User 명령에 대한 파이프라인 설정
        var userPipeline = new Pipeline<CreateUserCommand, User>()
            .AddHandler(new LoggingHandler<CreateUserCommand, User>(logger))
            .AddHandler(new ValidationHandler<CreateUserCommand, User>(new  EmailValidator()));

        // 유효한 명령 실행
        var validCommand = new CreateUserCommand("홍길동", "hong@example.com");
        var validResult = await userPipeline.ExecuteAsync(validCommand);

        var userMatch = new ResponseMatcher<User>(validResult);

        userMatch.Match(
            onSuccess: user =>
            {
                Console.WriteLine($"사용자 생성 성공: ID={user.Id}, 이름={user.Name}, 이메일={user.Email}");
                return true;
            },
            onFailure: (message,ex) =>
            {
                Console.WriteLine($"오류: {message}");
                if (ex != null) Console.WriteLine($"예외: {ex.Message}");
                return false;
            }
        );

        // 유효하지 않은 명령 실행
        var invalidCommand = new CreateUserCommand("", "invalid-email");
        var invalidResult = await userPipeline.ExecuteAsync(invalidCommand);

        var invalidMatcher = new ResponseMatcher<User>(invalidResult);
        invalidMatcher.Match(
            onSuccess: user => {
                Console.WriteLine($"사용자 생성 성공: ID={user.Id}, 이름={user.Name}, 이메일={user.Email}");
                return true;
            },
            onFailure: (message, ex) => {
                Console.WriteLine($"오류: {message}");
                if (ex != null) Console.WriteLine($"예외: {ex.Message}");
                return false;
            }
        );

        Console.WriteLine("\n===== 어댑터 패턴 사용 예제 =====");

        // 어댑터 생성
        var userToPersonAdapter = new UserToPersonAdapter();
        var adminToUserAdapter = new AdminToUserCommandAdapter();

        // 관리자 명령을 사용자 명령으로 변환하여 처리
        var adminCommand = new CreateAdminCommand("관리자", "admin@example.com", "시스템관리자");
        var adaptedUserCommand = adminToUserAdapter.Adapt(adminCommand);

        var adaptedResult = await userPipeline.ExecuteAsync((CreateUserCommand)adaptedUserCommand);
        var adaptedMatcher = new ResponseMatcher<User>(adaptedResult);

        adaptedMatcher.Match(
            onSuccess: user => {
                Console.WriteLine($"어댑터를 통한 관리자->사용자 변환 성공: 이름={user.Name}, 이메일={user.Email}");
                return true;
            },
            onFailure: (message, ex) => {
                Console.WriteLine($"어댑터 오류: {message}");
                return false;
            }
        );


        Console.WriteLine("\n===== 메디에이터 패턴 사용 예제 =====");

        var userCommandResult = await mediator.SendAsync<User>(validCommand);
        var mediatorMatcher = new ResponseMatcher<User>(userCommandResult);

        mediatorMatcher.Match(
            onSuccess: user => {
                Console.WriteLine($"메디에이터 처리 성공: ID={user.Id}, 이름={user.Name}, 이메일={user.Email}");
                return true;
            },
            onFailure: (message, ex) => {
                Console.WriteLine($"메디에이터 오류: {message}");
                return false;
            }
        );
        
        var adminCommandResult = await mediator.SendAsync<Admin>(adminCommand);
        var adminMatcher = new ResponseMatcher<Admin>(adminCommandResult);

        adminMatcher.Match(
            onSuccess: admin => {
                Console.WriteLine($"관리자 생성 성공: ID={admin.Id}, 이름={admin.Name}, 역할={admin.Role}");
                return true;
            },
            onFailure: (message, ex) => {
                Console.WriteLine($"관리자 생성 오류: {message}");
                return false;
            }
        );
    }
}