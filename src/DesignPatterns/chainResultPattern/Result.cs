﻿
// Result 패턴 - 성공/실패 결과 처리
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T Data { get; private set; }
    public string ErrorMessage { get; private set; }

    private Result(bool isSuccess, T data, string errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T data) => new Result<T>(true, data, null);
    public static Result<T> Failure(string errorMessage) => new Result<T>(false, default, errorMessage);
}
