using System;
using System.Collections.Generic;

namespace EcomMMS.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }
        public List<string> Errors { get; private set; } = new List<string>();

        private Result(bool isSuccess, T? data = default, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Success(T data)
        {
            return new Result<T>(true, data);
        }

        public static Result<T> Failure(string errorMessage)
        {
            return new Result<T>(false, errorMessage: errorMessage);
        }

        public static Result<T> Failure(List<string> errors)
        {
            var result = new Result<T>(false);
            result.Errors.AddRange(errors);
            return result;
        }

        public static Result<T> Failure(string errorMessage, List<string> errors)
        {
            var result = new Result<T>(false, errorMessage: errorMessage);
            result.Errors.AddRange(errors);
            return result;
        }
    }

    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string? ErrorMessage { get; private set; }
        public List<string> Errors { get; private set; } = new List<string>();

        private Result(bool isSuccess, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static Result Success()
        {
            return new Result(true);
        }

        public static Result Failure(string errorMessage)
        {
            return new Result(false, errorMessage);
        }

        public static Result Failure(List<string> errors)
        {
            var result = new Result(false);
            result.Errors.AddRange(errors);
            return result;
        }

        public static Result Failure(string errorMessage, List<string> errors)
        {
            var result = new Result(false, errorMessage);
            result.Errors.AddRange(errors);
            return result;
        }
    }
} 