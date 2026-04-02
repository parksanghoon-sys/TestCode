using ReadableRingChainSample.Abstractions;
using ReadableRingChainSample.Core;
using ReadableRingChainSample.Domain;
using ReadableRingChainSample.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReadableRingChainSampleTest
{
    public sealed class DeviceScenario
    {
        private readonly ICommandTransport<DeviceCommand, DeviceResponse> _transport;

        public DeviceScenario(ICommandTransport<DeviceCommand, DeviceResponse> transport)
        {
            _transport = transport;
        }

        public ChainRunner<DeviceSessionState> Build(IAppLogger logger)
        {
            return new ChainRunner<DeviceSessionState>(logger)
                .Add(CreateHelloStep())
                .Add(CreateAuthStep())
                .Add(CreateGetDataStep());
        }

        private IChainStep<DeviceSessionState> CreateHelloStep()
        {
            return new CommandStep<DeviceSessionState, DeviceCommand, DeviceResponse>("HELLO")
                .BuildCommand(BuildHelloCommandAsync)
                .SendBy(SendAsync)
                .ReceiveBy(ReceiveAsync)
                .ValidateBy(ValidateHelloResponse)
                .UpdateStateBy(UpdateHelloState)
                .GoTo((_, _, _) => "AUTH")
                .WithRetry(1)
                .WithTimeout(TimeSpan.FromSeconds(3));
        }

        private IChainStep<DeviceSessionState> CreateAuthStep()
        {
            return new CommandStep<DeviceSessionState, DeviceCommand, DeviceResponse>("AUTH")
                .BuildCommand(BuildAuthCommandAsync)
                .SendBy(SendAsync)
                .ReceiveBy(ReceiveAsync)
                .ValidateBy(ValidateAuthResponse)
                .UpdateStateBy(UpdateAuthState)
                .GoTo((_, _, _) => "GET_DATA")
                .WithRetry(2)
                .WithTimeout(TimeSpan.FromSeconds(3));
        }

        private IChainStep<DeviceSessionState> CreateGetDataStep()
        {
            return new CommandStep<DeviceSessionState, DeviceCommand, DeviceResponse>("GET_DATA")
                .BuildCommand(BuildGetDataCommandAsync)
                .SendBy(SendAsync)
                .ReceiveBy(ReceiveAsync)
                .ValidateBy(ValidateGetDataResponse)
                .UpdateStateBy(UpdateGetDataState)
                .CompleteWhen((_, _, response) => response.Code == "DATA_ACK")
                .GoTo((_, _, _) => null)
                .WithRetry(1)
                .WithTimeout(TimeSpan.FromSeconds(3));
        }

        private Task<Result> SendAsync(
            ChainContext<DeviceSessionState> context,
            DeviceCommand command,
            CancellationToken cancellationToken)
        {
            return _transport.SendAsync(command, cancellationToken);
        }

        private Task<Result<DeviceResponse>> ReceiveAsync(
            ChainContext<DeviceSessionState> context,
            DeviceCommand command,
            CancellationToken cancellationToken)
        {
            return _transport.ReceiveAsync(cancellationToken);
        }

        private Task<Result<DeviceCommand>> BuildHelloCommandAsync(
            ChainContext<DeviceSessionState> context,
            CancellationToken cancellationToken)
        {
            var command = new DeviceCommand("HELLO", $"DEVICE={context.State.DeviceId}");
            return Task.FromResult(Result.Success(command));
        }

        private Task<Result<DeviceCommand>> BuildAuthCommandAsync(
            ChainContext<DeviceSessionState> context,
            CancellationToken cancellationToken)
        {
            if (!context.State.Handshaked)
            {
                return Task.FromResult(
                    Result<DeviceCommand>.Failure("NOT_HANDSHAKED", "HELLO step must succeed before AUTH."));
            }

            var command = new DeviceCommand("AUTH", "USER=admin;PASSWORD=1234");
            return Task.FromResult(Result.Success(command));
        }

        private Task<Result<DeviceCommand>> BuildGetDataCommandAsync(
            ChainContext<DeviceSessionState> context,
            CancellationToken cancellationToken)
        {
            if (!context.State.Authenticated || string.IsNullOrWhiteSpace(context.State.Token))
            {
                return Task.FromResult(
                    Result<DeviceCommand>.Failure("NOT_AUTHENTICATED", "AUTH step must succeed before GET_DATA."));
            }

            var command = new DeviceCommand("GET_DATA", $"TOKEN={context.State.Token}");
            return Task.FromResult(Result.Success(command));
        }

        private Result ValidateHelloResponse(
            ChainContext<DeviceSessionState> context,
            DeviceCommand command,
            DeviceResponse response)
        {
            if (!response.IsOk)
                return Result.Failure("HELLO_NACK", "HELLO response is not ok.");

            if (!string.Equals(response.Code, "HELLO_ACK", StringComparison.OrdinalIgnoreCase))
                return Result.Failure("HELLO_INVALID", $"Unexpected response code: {response.Code}");

            return Result.Success();
        }

        private Result ValidateAuthResponse(
            ChainContext<DeviceSessionState> context,
            DeviceCommand command,
            DeviceResponse response)
        {
            if (!response.IsOk)
                return Result.Failure("AUTH_NACK", "AUTH response is not ok.");

            if (!string.Equals(response.Code, "AUTH_ACK", StringComparison.OrdinalIgnoreCase))
                return Result.Failure("AUTH_INVALID", $"Unexpected response code: {response.Code}");

            if (!response.Payload.StartsWith("TOKEN:", StringComparison.OrdinalIgnoreCase))
                return Result.Failure("TOKEN_MISSING", "Token payload not found.");

            return Result.Success();
        }

        private Result ValidateGetDataResponse(
            ChainContext<DeviceSessionState> context,
            DeviceCommand command,
            DeviceResponse response)
        {
            if (!response.IsOk)
                return Result.Failure("DATA_NACK", "GET_DATA response is not ok.");

            if (!string.Equals(response.Code, "DATA_ACK", StringComparison.OrdinalIgnoreCase))
                return Result.Failure("DATA_INVALID", $"Unexpected response code: {response.Code}");

            if (string.IsNullOrWhiteSpace(response.Payload))
                return Result.Failure("DATA_EMPTY", "Response payload is empty.");

            return Result.Success();
        }

        private DeviceSessionState UpdateHelloState(
            ChainContext<DeviceSessionState> context,
            DeviceCommand command,
            DeviceResponse response)
        {
            return context.State with
            {
                Handshaked = true,
                Logs = AppendLog(context.State.Logs, $"HELLO success: {response.Payload}")
            };
        }

        private DeviceSessionState UpdateAuthState(
            ChainContext<DeviceSessionState> context,
            DeviceCommand command,
            DeviceResponse response)
        {
            var token = response.Payload["TOKEN:".Length..];

            return context.State with
            {
                Authenticated = true,
                Token = token,
                Logs = AppendLog(context.State.Logs, $"AUTH success: token={token}")
            };
        }

        private DeviceSessionState UpdateGetDataState(
            ChainContext<DeviceSessionState> context,
            DeviceCommand command,
            DeviceResponse response)
        {
            return context.State with
            {
                Data = response.Payload,
                Logs = AppendLog(context.State.Logs, $"GET_DATA success: {response.Payload}")
            };
        }

        private static IReadOnlyList<string> AppendLog(IReadOnlyList<string> logs, string newLog)
        {
            var list = new List<string>(logs) { newLog };
            return list;
        }
    }
}
