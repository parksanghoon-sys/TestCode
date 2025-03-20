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

            // ���� Ȯ��
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
            int size = 2000; // 1024���� ū ũ��

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
            const int advancePerThread = 200; // �� ������� 200�� ����
            var tasks = new Task[numThreads];

            // Act
            for (int t = 0; t < numThreads; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++) // �� ������� 20�� 10�� ����
                    {
                        try
                        {
                            buffer.Advance(20);
                            Thread.Sleep(1); // ���� ��Ȳ �����
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // �ٸ� �����尡 �̹� �������� �� ����
                            break;
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            Assert.True(buffer.ReaderIndex <= 1000); // �ִ� 1000������ ���� ����
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

        //    // ��ġ ������ ���� ���ü� �÷���
        //    var positions = new ConcurrentDictionary<int, int>();
        //    var completedMessages = new ConcurrentBag<(int producerId, int messageId, byte[] content)>();

        //    // �޽��� ����/���� �۾�
        //    var producers = Enumerable.Range(0, producerCount)
        //        .Select(producerId => Task.Run(() =>
        //        {
        //            var random = new Random(producerId * 100);

        //            for (int i = 0; i < messagesPerProducer; i++)
        //            {
        //                // �޽��� ����
        //                byte[] message = new byte[messageSize];
        //                random.NextBytes(message);

        //                // ���ε༭ ID�� �޽��� ��ȣ�� �޽����� ���� (�ĺ���)
        //                BitConverter.GetBytes(producerId).CopyTo(message, 0);
        //                BitConverter.GetBytes(i).CopyTo(message, 4);

        //                // ���ۿ� �� ��ġ ��� (����������)
        //                int position = Interlocked.Add(ref positions[0], messageSize) - messageSize;

        //                // ���ۿ� �޽��� ����
        //                if (position + messageSize <= bufferSize)
        //                {
        //                    lock (buffer) // ���� ���� �۾����� ��ȣ
        //                    {
        //                        buffer.Write(message, position);
        //                    }
        //                }

        //                // �ణ�� ���� (�� �������� �ó�����)
        //                Thread.Sleep(random.Next(1, 5));
        //            }
        //        }))
        //        .ToArray();

        //    // ��� ���ε༭�� �Ϸ�� ������ ���
        //    await Task.WhenAll(producers);

        //    // ���ۿ��� �޽��� �б� �۾�
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

        //                    // �޽������� ���ε༭ ID�� �޽��� ��ȣ ����
        //                    int producerId = BitConverter.ToInt32(content, 0);
        //                    int messageId = BitConverter.ToInt32(content, 4);

        //                    // ó���� �޽��� ��Ͽ� �߰�
        //                    completedMessages.Add((producerId, messageId, content));
        //                }
        //            }
        //        }))
        //        .ToArray();

        //    // ��� �����Ӱ� �Ϸ�� ������ ���
        //    await Task.WhenAll(consumers);

        //    // Assert - ��� �޽����� �ùٸ��� ó���Ǿ����� Ȯ��
        //    Assert.Equal(producerCount * messagesPerProducer, completedMessages.Count);

        //    // �ߺ� �޽��� üũ
        //    var uniqueMessages = completedMessages
        //        .Select(m => (m.producerId, m.messageId))
        //        .Distinct()
        //        .Count();

        //    // �ߺ� ���� ��� �޽����� ó���Ǿ����� Ȯ��
        //    Assert.Equal(producerCount * messagesPerProducer, uniqueMessages);

        //    // �� ���ε༭�� �޽��� �� Ȯ��
        //    foreach (var producerId in Enumerable.Range(0, producerCount))
        //    {
        //        var producerMessages = completedMessages
        //            .Where(m => m.producerId == producerId)
        //            .ToList();

        //        Assert.Equal(messagesPerProducer, producerMessages.Count);

        //        // �޽��� ��ȣ ���Ӽ� Ȯ��
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
            // �۾� ����
         
            // ������ �۾� ID�� ����ϱ� ���� �÷���
            var completedOperations = new ConcurrentBag<(int threadId, int operationId, OperationType type)>();
            // ���� �����忡�� ���ÿ� �۾� ����
            var tasks = Enumerable.Range(0, threadCount)
                .Select(threadId => Task.Run(() =>
                {
                    var random = new Random(threadId * 1000);

                    for (int i = 0; i < operationCount; i++)
                    {
                        try
                        {
                            // ������ �۾� ���� (����, �б�, ����, ����)
                            var operation = (OperationType)random.Next(4);

                            switch (operation)
                            {
                                case OperationType.Write:
                                    // ������ ũ���� ������ ����
                                    int writeSize = random.Next(100, 1000);
                                    byte[] data = new byte[writeSize];
                                    random.NextBytes(data);

                                    // ����� ������ ID�� �۾� ID ����
                                    BitConverter.GetBytes(threadId).CopyTo(data, 0);
                                    BitConverter.GetBytes(i).CopyTo(data, 4);

                                    // ���ۿ� ���� �õ�
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
                                        // ����� ���� (���� ��� �ʰ�, ��)
                                    }
                                    break;

                                case OperationType.Read:
                                    // ������ ���� �б�
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
                                        // ����� ����
                                    }
                                    break;

                                case OperationType.Advance:
                                    // ���� �õ�
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
                                        // ����� ����
                                    }
                                    break;

                                case OperationType.Reset:
                                    // ���� ���� �õ� (�ٸ� �۾����� �� ����)
                                    if (random.Next(10) == 0)
                                    {
                                        buffer.Reset();
                                        completedOperations.Add((threadId, i, operation));
                                    }
                                    break;
                            }

                            // ������ ���� ������ ���� ��Ȳ �����
                            Thread.Sleep(random.Next(0, 2));
                        }
                        catch (ObjectDisposedException)
                        {
                            // �ٸ� �����忡�� ���۰� ������ ���
                            break;
                        }
                    }
                }))
                .ToArray();

            // ��� �۾��� �Ϸ�� ������ ���
            await Task.WhenAll(tasks);

            // Assert
            Console.WriteLine($"�Ϸ�� �۾� ��: {completedOperations.Count}");

            // �� �����庰 �۾� ���
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
                Console.WriteLine($"������ {stats.ThreadId}: " +
                                 $"�� {stats.TotalOperations}�� �۾�, " +
                                 $"����: {stats.WriteOps}, " +
                                 $"�б�: {stats.ReadOps}, " +
                                 $"����: {stats.AdvanceOps}, " +
                                 $"����: {stats.ResetOps}");
            }

            // ���۰� ���� ��� �������� Ȯ�� (�޸� �ջ� ����)
            Assert.False(buffer.IsDisposed);

            // �⺻ �۾��� ������ �������� Ȯ��
            try
            {
                buffer.Reset();
                buffer.Write(new byte[100], 0);
                buffer.ReadMemory(new Range(0, 50));
                buffer.Advance(50);
            }
            catch (Exception ex)
            {
                Assert.Fail($"��Ʈ���� �׽�Ʈ �� �⺻ �۾� ����: {ex.Message}");
            }
        }


        [Fact]
        public void MemoryUsage_ShouldNotLeak()
        {
            // Arrange
            const int iterations = 1000;
            const int bufferSize = 1024 * 1024; // 1MB

            // �ʱ� �޸� ��뷮 Ȯ��
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long initialMemory = GC.GetTotalMemory(true);

            // Act - ���� ���� ������ ����
            for (int i = 0; i < iterations; i++)
            {
                using (var buffer = new RentedBuffer<byte>(bufferSize))
                {
                    // ������ �۾� ����
                    buffer.Write(new byte[1024], 0);
                    buffer.Advance(512);
                    var _ = buffer.ReadMemory(new Range(0, 512));
                }
            }

            // �޸� ����
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long finalMemory = GC.GetTotalMemory(true);

            // Assert - �޸� ��뷮 Ȯ��
            long memoryDifference = finalMemory - initialMemory;
            Console.WriteLine($"Memory usage difference: {memoryDifference / 1024} KB");

            // �ణ�� �޸� ���̴� ��� (���� Ǯ������ ����)
            Assert.True(memoryDifference < bufferSize,
                $"Memory leak detected: {memoryDifference / 1024} KB");


        }

    }
}