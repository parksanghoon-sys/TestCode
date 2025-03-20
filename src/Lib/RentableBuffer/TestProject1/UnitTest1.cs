using RentableBuffer;
using System.Collections.Concurrent;
using System;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    public class RentedBufferTests
    {
        [Fact]
        public void Create_ReadOnlyBuffer_ShouldInitializeCorrectly()
        {
            // Arrange
            byte[] sourceData = Encoding.UTF8.GetBytes("Test Data");

            // Act
            using var buffer = new RentedBuffer<byte>(sourceData);

            // Assert
            Assert.Equal(sourceData.Length, buffer.MaxCapacity);
            Assert.Equal(0, buffer.ReaderIndex);
            Assert.Equal(sourceData.Length, buffer.WriterIndex);
            Assert.Equal(0, buffer.WritableBytes);
            Assert.Equal(sourceData.Length, buffer.ReadableBytes);
            Assert.True(buffer.IsReadOnly);
            Assert.False(buffer.IsWritable);
            Assert.True(buffer.IsReadable);
            Assert.False(buffer.IsDisposed);
            Assert.False(buffer.IsEndOfBuffer);

            // 내용 확인
            for (int i = 0; i < sourceData.Length; i++)
            {
                Assert.Equal(sourceData[i], buffer[i]);
            }
        }
        [Fact]
        public void Write_WithFlush_ShouldUpdateReaderIndex()
        {
            // Arrange
            using var buffer = new RentedBuffer<byte>(100);
            byte[] data = Encoding.UTF8.GetBytes("Hello World");

            // Act
            buffer.Write(data, isFlush: true);

            // Assert
            Assert.Equal(data.Length, buffer.WriterIndex);
            Assert.Equal(data.Length, buffer.ReaderIndex);
            Assert.Equal(0, buffer.ReadableBytes);
            Assert.True(buffer.IsEndOfBuffer);
        }
        [Fact]
        public void ArrayPoolAllocator_LargeBuffer_ShouldRentAndReturn()
        {
            // Arrange
            int size = 2000; // 1024보다 큰 크기

            // Act
            var array = ArrayPoolAllocator<byte>.Rent(size);

            // Assert
            Assert.NotNull(array);
            Assert.True(array.Length >= size);

            // Clean up
            ArrayPoolAllocator<byte>.Return(array, true);
        }
        [Fact]
        public void ConcurrentAdvance_ShouldBeThreadSafe()
        {
            // Arrange
            byte[] data = new byte[1000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }

            using var buffer = new RentedBuffer<byte>(data);
            const int numThreads = 5;
            const int advancePerThread = 200; // 각 스레드는 200씩 진행
            var tasks = new Task[numThreads];

            // Act
            for (int t = 0; t < numThreads; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++) // 각 스레드는 20씩 10번 진행
                    {
                        try
                        {
                            buffer.Advance(20);
                            Thread.Sleep(1); // 경합 상황 만들기
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // 다른 스레드가 이미 진행했을 수 있음
                            break;
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            Assert.True(buffer.ReaderIndex <= 1000); // 최대 1000까지만 진행 가능
            Assert.True(buffer.IsEndOfBuffer || buffer.ReaderIndex == 1000);
        }
        //[Fact]
        //public async Task ParallelOperations_WriteThenRead_ShouldHandleCorrectly()
        //{
        //    // Arrange
        //    const int bufferSize = 10 * 1024 * 1024; // 10MB
        //    const int producerCount = 8;
        //    const int consumerCount = 4;
        //    const int messagesPerProducer = 100;
        //    const int messageSize = 10 * 1024; // 10KB per message

        //    using var buffer = new RentedBuffer<byte>(bufferSize);

        //    // 위치 추적을 위한 동시성 컬렉션
        //    var positions = new ConcurrentDictionary<int, int>();
        //    var completedMessages = new ConcurrentBag<(int producerId, int messageId, byte[] content)>();

        //    // 메시지 생성/쓰기 작업
        //    var producers = Enumerable.Range(0, producerCount)
        //        .Select(producerId => Task.Run(() =>
        //        {
        //            var random = new Random(producerId * 100);

        //            for (int i = 0; i < messagesPerProducer; i++)
        //            {
        //                // 메시지 생성
        //                byte[] message = new byte[messageSize];
        //                random.NextBytes(message);

        //                // 프로듀서 ID와 메시지 번호를 메시지에 포함 (식별용)
        //                BitConverter.GetBytes(producerId).CopyTo(message, 0);
        //                BitConverter.GetBytes(i).CopyTo(message, 4);

        //                // 버퍼에 쓸 위치 계산 (원자적으로)
        //                int position = Interlocked.Add(ref positions[0], messageSize) - messageSize;

        //                // 버퍼에 메시지 쓰기
        //                if (position + messageSize <= bufferSize)
        //                {
        //                    lock (buffer) // 동시 쓰기 작업에서 보호
        //                    {
        //                        buffer.Write(message, position);
        //                    }
        //                }

        //                // 약간의 지연 (더 현실적인 시나리오)
        //                Thread.Sleep(random.Next(1, 5));
        //            }
        //        }))
        //        .ToArray();

        //    // 모든 프로듀서가 완료될 때까지 대기
        //    await Task.WhenAll(producers);

        //    // 버퍼에서 메시지 읽기 작업
        //    var consumers = Enumerable.Range(0, consumerCount)
        //        .Select(consumerId => Task.Run(() =>
        //        {
        //            int totalMessages = producerCount * messagesPerProducer;
        //            int messagesPerConsumer = totalMessages / consumerCount;
        //            int startMessage = consumerId * messagesPerConsumer;
        //            int endMessage = (consumerId == consumerCount - 1)
        //                ? totalMessages
        //                : startMessage + messagesPerConsumer;

        //            for (int msgIndex = startMessage; msgIndex < endMessage; msgIndex++)
        //            {
        //                int position = msgIndex * messageSize;
        //                if (position + messageSize <= bufferSize)
        //                {
        //                    var messageData = buffer.ReadMemory(new Range(position, position + messageSize));
        //                    byte[] content = messageData.ToArray();

        //                    // 메시지에서 프로듀서 ID와 메시지 번호 추출
        //                    int producerId = BitConverter.ToInt32(content, 0);
        //                    int messageId = BitConverter.ToInt32(content, 4);

        //                    // 처리된 메시지 목록에 추가
        //                    completedMessages.Add((producerId, messageId, content));
        //                }
        //            }
        //        }))
        //        .ToArray();

        //    // 모든 컨슈머가 완료될 때까지 대기
        //    await Task.WhenAll(consumers);

        //    // Assert - 모든 메시지가 올바르게 처리되었는지 확인
        //    Assert.Equal(producerCount * messagesPerProducer, completedMessages.Count);

        //    // 중복 메시지 체크
        //    var uniqueMessages = completedMessages
        //        .Select(m => (m.producerId, m.messageId))
        //        .Distinct()
        //        .Count();

        //    // 중복 없이 모든 메시지가 처리되었는지 확인
        //    Assert.Equal(producerCount * messagesPerProducer, uniqueMessages);

        //    // 각 프로듀서별 메시지 수 확인
        //    foreach (var producerId in Enumerable.Range(0, producerCount))
        //    {
        //        var producerMessages = completedMessages
        //            .Where(m => m.producerId == producerId)
        //            .ToList();

        //        Assert.Equal(messagesPerProducer, producerMessages.Count);

        //        // 메시지 번호 연속성 확인
        //        var messageIds = producerMessages
        //            .Select(m => m.messageId)
        //            .OrderBy(id => id)
        //            .ToList();

        //        Assert.Equal(
        //            Enumerable.Range(0, messagesPerProducer),
        //            messageIds);
        //    }
        //}
        enum OperationType { Write, Read, Advance, Reset }
        [Fact]
        public async Task StressTest_MultipleThreadsReadWriteAdvance_ShouldNotCorruptBuffer()
        {
            // Arrange
            const int bufferSize = 1 * 1024 * 1024; // 1MB
            const int operationCount = 1000;
            const int threadCount = 16;

            using var buffer = new RentedBuffer<byte>(bufferSize);
            // 작업 유형
         
            // 각각의 작업 ID를 기록하기 위한 컬렉션
            var completedOperations = new ConcurrentBag<(int threadId, int operationId, OperationType type)>();
            // 여러 스레드에서 동시에 작업 수행
            var tasks = Enumerable.Range(0, threadCount)
                .Select(threadId => Task.Run(() =>
                {
                    var random = new Random(threadId * 1000);

                    for (int i = 0; i < operationCount; i++)
                    {
                        try
                        {
                            // 무작위 작업 선택 (쓰기, 읽기, 진행, 리셋)
                            var operation = (OperationType)random.Next(4);

                            switch (operation)
                            {
                                case OperationType.Write:
                                    // 무작위 크기의 데이터 생성
                                    int writeSize = random.Next(100, 1000);
                                    byte[] data = new byte[writeSize];
                                    random.NextBytes(data);

                                    // 헤더에 스레드 ID와 작업 ID 포함
                                    BitConverter.GetBytes(threadId).CopyTo(data, 0);
                                    BitConverter.GetBytes(i).CopyTo(data, 4);

                                    // 버퍼에 쓰기 시도
                                    try
                                    {
                                        int position = random.Next(0, Math.Max(1, bufferSize - writeSize));
                                        buffer.Write(data, position);
                                        completedOperations.Add((threadId, i, operation));
                                    }
                                    catch (Exception ex) when (
                                        ex is ArgumentOutOfRangeException ||
                                        ex is InvalidOperationException)
                                    {
                                        // 예상된 예외 (버퍼 경계 초과, 등)
                                    }
                                    break;

                                case OperationType.Read:
                                    // 무작위 범위 읽기
                                    try
                                    {
                                        int readSize = random.Next(10, 1000);
                                        int startPos = random.Next(0, Math.Max(1, bufferSize - readSize));
                                        var memory = buffer.ReadMemory(new Range(startPos, startPos + readSize));
                                        completedOperations.Add((threadId, i, operation));
                                    }
                                    catch (Exception ex) when (
                                        ex is ArgumentOutOfRangeException ||
                                        ex is IndexOutOfRangeException)
                                    {
                                        // 예상된 예외
                                    }
                                    break;

                                case OperationType.Advance:
                                    // 진행 시도
                                    try
                                    {
                                        int advanceSize = random.Next(1, 100);
                                        buffer.Advance(advanceSize);
                                        completedOperations.Add((threadId, i, operation));
                                    }
                                    catch (Exception ex) when (
                                        ex is ArgumentOutOfRangeException ||
                                        ex is InvalidOperationException)
                                    {
                                        // 예상된 예외
                                    }
                                    break;

                                case OperationType.Reset:
                                    // 가끔 리셋 시도 (다른 작업보다 덜 자주)
                                    if (random.Next(10) == 0)
                                    {
                                        buffer.Reset();
                                        completedOperations.Add((threadId, i, operation));
                                    }
                                    break;
                            }

                            // 지연을 통해 스레드 경합 상황 만들기
                            Thread.Sleep(random.Next(0, 2));
                        }
                        catch (ObjectDisposedException)
                        {
                            // 다른 스레드에서 버퍼가 해제된 경우
                            break;
                        }
                    }
                }))
                .ToArray();

            // 모든 작업이 완료될 때까지 대기
            await Task.WhenAll(tasks);

            // Assert
            Console.WriteLine($"완료된 작업 수: {completedOperations.Count}");

            // 각 스레드별 작업 통계
            var statsByThread = completedOperations
                .GroupBy(op => op.threadId)
                .Select(g => new
                {
                    ThreadId = g.Key,
                    TotalOperations = g.Count(),
                    WriteOps = g.Count(op => op.type == OperationType.Write),
                    ReadOps = g.Count(op => op.type == OperationType.Read),
                    AdvanceOps = g.Count(op => op.type == OperationType.Advance),
                    ResetOps = g.Count(op => op.type == OperationType.Reset)
                })
                .OrderBy(s => s.ThreadId)
                .ToList();

            foreach (var stats in statsByThread)
            {
                Console.WriteLine($"스레드 {stats.ThreadId}: " +
                                 $"총 {stats.TotalOperations}개 작업, " +
                                 $"쓰기: {stats.WriteOps}, " +
                                 $"읽기: {stats.ReadOps}, " +
                                 $"진행: {stats.AdvanceOps}, " +
                                 $"리셋: {stats.ResetOps}");
            }

            // 버퍼가 아직 사용 가능한지 확인 (메모리 손상 없음)
            Assert.False(buffer.IsDisposed);

            // 기본 작업이 여전히 가능한지 확인
            try
            {
                buffer.Reset();
                buffer.Write(new byte[100], 0);
                buffer.ReadMemory(new Range(0, 50));
                buffer.Advance(50);
            }
            catch (Exception ex)
            {
                Assert.Fail($"스트레스 테스트 후 기본 작업 실패: {ex.Message}");
            }
        }


        [Fact]
        public void MemoryUsage_ShouldNotLeak()
        {
            // Arrange
            const int iterations = 1000;
            const int bufferSize = 1024 * 1024; // 1MB

            // 초기 메모리 사용량 확인
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long initialMemory = GC.GetTotalMemory(true);

            // Act - 많은 버퍼 생성과 해제
            for (int i = 0; i < iterations; i++)
            {
                using (var buffer = new RentedBuffer<byte>(bufferSize))
                {
                    // 간단한 작업 수행
                    buffer.Write(new byte[1024], 0);
                    buffer.Advance(512);
                    var _ = buffer.ReadMemory(new Range(0, 512));
                }
            }

            // 메모리 정리
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long finalMemory = GC.GetTotalMemory(true);

            // Assert - 메모리 사용량 확인
            long memoryDifference = finalMemory - initialMemory;
            Console.WriteLine($"Memory usage difference: {memoryDifference / 1024} KB");

            // 약간의 메모리 차이는 허용 (내부 풀링으로 인해)
            Assert.True(memoryDifference < bufferSize,
                $"Memory leak detected: {memoryDifference / 1024} KB");


        }

    }
}