using System;

public class BitManipulator
{
    /// <summary>
    /// 바이트 배열의 특정 비트들을 주어진 값으로 설정합니다.
    /// </summary>
    /// <param name="data">수정할 바이트 배열</param>
    /// <param name="startBit">시작 비트 위치 (0부터 시작)</param>
    /// <param name="bitCount">설정할 비트 수</param>
    /// <param name="value">쓸 값</param>
    public static void SetBits(byte[] data, int startBit, int bitCount, int value)
    {
        // 값이 지정된 비트 수에 맞는지 확인합니다
        // 예: 3비트에는 0~7(2^3-1)까지만 저장 가능
        int maxValue = (1 << bitCount) - 1;
        if (value > maxValue)
        {
            throw new ArgumentException($"값 {value}은(는) {bitCount}비트에 저장하기에 너무 큽니다 (최대값: {maxValue})");
        }

        // 영향을 받는 바이트 인덱스를 계산합니다
        // 예: 10번 비트는 1번 바이트의 2번 비트입니다 (10/8 = 1)
        int startByte = startBit / 8;
        int endByte = (startBit + bitCount - 1) / 8;

        // 배열 범위를 벗어나는지 확인합니다
        if (endByte >= data.Length)
        {
            throw new ArgumentException("작업이 배열 경계를 초과합니다");
        }

        // 첫 번째 바이트 내의 시작 비트 위치를 계산합니다
        // 예: 10번 비트는 1번 바이트의 2번 비트입니다 (10%8 = 2)
        int startBitInByte = startBit % 8;

        // 모든 비트가 단일 바이트 내에 있는 경우 처리
        if (startByte == endByte)
        {
            // 설정하려는 비트를 지우기 위한 마스크 생성
            // 예: 3비트를 설정하려면, (1<<3)-1 = 7 (111), 이를 왼쪽으로 이동
            byte mask = (byte)(((1 << bitCount) - 1) << startBitInByte);

            // 비트 지우기 (마스크의 비트 반전 후 AND 연산)
            data[startByte] &= (byte)~mask;

            // 비트 설정 (값을 시작 위치만큼 왼쪽으로 이동 후 OR 연산)
            data[startByte] |= (byte)(value << startBitInByte);
            return;
        }

        // 비트가 여러 바이트에 걸쳐 있는 경우 처리

        // 첫 번째 바이트 처리
        // 첫 번째 바이트에서 사용 가능한 비트 수 계산 (8 - 시작 위치)
        int bitsInFirstByte = 8 - startBitInByte;
        byte maskFirstByte = (byte)(((1 << bitsInFirstByte) - 1) << startBitInByte);

        // 첫 번째 바이트의 비트 지우기
        data[startByte] &= (byte)~maskFirstByte;

        // 첫 번째 바이트에 비트 설정
        // 값에서 첫 번째 바이트에 해당하는 비트만 추출하여 설정
        data[startByte] |= (byte)((value & ((1 << bitsInFirstByte) - 1)) << startBitInByte);

        // 설정해야 할 남은 비트 계산
        int remainingBits = bitCount - bitsInFirstByte;
        value >>= bitsInFirstByte;  // 이미 처리한 비트 제거

        // 중간 바이트 처리 (있는 경우)
        for (int i = startByte + 1; i < endByte; i++)
        {
            // 각 바이트는 값의 8비트씩 처리
            data[i] = (byte)(value & 0xFF);
            value >>= 8;  // 다음 8비트로 이동
            remainingBits -= 8;
        }

        // 마지막 바이트 처리 (첫 번째 바이트와 다른 경우)
        if (remainingBits > 0)
        {
            // 마지막 바이트에 설정할 비트 수에 맞는 마스크 생성
            byte maskLastByte = (byte)((1 << remainingBits) - 1);

            // 마지막 바이트의 비트 지우기
            data[endByte] &= (byte)~maskLastByte;

            // 마지막 바이트에 비트 설정
            data[endByte] |= (byte)(value & maskLastByte);
        }
    }

    /// <summary>
    /// 바이트 배열에서 특정 비트들의 값을 가져옵니다.
    /// </summary>
    /// <param name="data">읽을 바이트 배열</param>
    /// <param name="startBit">시작 비트 위치 (0부터 시작)</param>
    /// <param name="bitCount">읽을 비트 수</param>
    /// <returns>지정된 비트가 나타내는 값</returns>
    public static int GetBits(byte[] data, int startBit, int bitCount)
    {
        // 영향을 받는 바이트 인덱스를 계산합니다
        int startByte = startBit / 8;
        int endByte = (startBit + bitCount - 1) / 8;

        // 배열 범위를 벗어나는지 확인합니다
        if (endByte >= data.Length)
        {
            throw new ArgumentException("작업이 배열 경계를 초과합니다");
        }

        // 첫 번째 바이트 내의 시작 비트 위치를 계산합니다
        int startBitInByte = startBit % 8;

        // 모든 비트가 단일 바이트 내에 있는 경우 처리
        if (startByte == endByte)
        {
            // 원하는 비트만 추출하기 위한 마스크 생성 후 해당 위치의 비트 추출
            byte mask = (byte)(((1 << bitCount) - 1) << startBitInByte);
            return (data[startByte] & mask) >> startBitInByte;
        }

        // 비트가 여러 바이트에 걸쳐 있는 경우 처리
        int result = 0;  // 결과값 저장할 변수
        int currentBitPosition = 0;  // 현재 결과의 비트 위치

        // 첫 번째 바이트 처리
        int bitsInFirstByte = 8 - startBitInByte;  // 첫 번째 바이트에서 사용 가능한 비트 수
        byte maskFirstByte = (byte)(((1 << bitsInFirstByte) - 1) << startBitInByte);
        // 첫 번째 바이트에서 비트 추출하여 결과에 추가
        result |= ((data[startByte] & maskFirstByte) >> startBitInByte);
        currentBitPosition += bitsInFirstByte;

        // 중간 바이트 처리 (있는 경우)
        for (int i = startByte + 1; i < endByte; i++)
        {
            // 각 바이트의 모든 비트를 추출하여 결과에 올바른 위치에 추가
            result |= (data[i] << currentBitPosition);
            currentBitPosition += 8;  // 다음 바이트로 이동
        }

        // 마지막 바이트 처리 (첫 번째 바이트와 다른 경우)
        int remainingBits = bitCount - currentBitPosition;  // 남은 비트 수 계산
        if (remainingBits > 0)
        {
            // 마지막 바이트에서 읽을 비트 수에 맞는 마스크 생성
            byte maskLastByte = (byte)((1 << remainingBits) - 1);
            // 마지막 바이트에서 비트 추출하여 결과에 올바른 위치에 추가
            result |= ((data[endByte] & maskLastByte) << currentBitPosition);
        }

        return result;
    }

    /// <summary>
    /// 바이트 배열을 이진 문자열로 변환합니다.
    /// </summary>
    /// <param name="data">변환할 바이트 배열</param>
    /// <returns>각 비트를 표현하는 문자열 (예: "01010101 10101010")</returns>
    public static string ToBinaryString(byte[] data)
    {
        // 결과 문자열을 저장할 StringBuilder
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            // 한 바이트의 각 비트를 문자열로 변환
            for (int bit = 7; bit >= 0; bit--)
            {
                // 특정 비트가 설정되어 있는지 확인 (0 또는 1)
                bool isSet = (data[i] & (1 << bit)) != 0;
                sb.Append(isSet ? '1' : '0');
            }

            // 바이트 사이에 공백 추가 (마지막 바이트 제외)
            if (i < data.Length - 1)
            {
                sb.Append(' ');
            }
        }

        return sb.ToString();
    }

    // 사용 예시
    public static void Example()
    {
        // 3바이트 크기의 바이트 배열 생성
        byte[] data = new byte[3];
        Console.WriteLine($"초기 상태 (비트): {ToBinaryString(data)}");

        // 예시 1: 0-2번 비트에 값 2(이진수: 010) 설정
        SetBits(data, 0, 3, 2);
        Console.WriteLine($"0-2번 비트에 2 설정 후 (16진수): {BitConverter.ToString(data)}");
        Console.WriteLine($"0-2번 비트에 2 설정 후 (비트): {ToBinaryString(data)}");
        Console.WriteLine($"0-2번 비트 값 읽기: {GetBits(data, 0, 3)}");
        Console.WriteLine();

        // 예시 2: 5-6번 비트에 값 1(이진수: 01) 설정
        SetBits(data, 5, 2, 1);
        Console.WriteLine($"5-6번 비트에 1 설정 후 (16진수): {BitConverter.ToString(data)}");
        Console.WriteLine($"5-6번 비트에 1 설정 후 (비트): {ToBinaryString(data)}");
        Console.WriteLine($"5-6번 비트 값 읽기: {GetBits(data, 5, 2)}");
        Console.WriteLine();

        // 바이트 경계를 넘는 예시
        SetBits(data, 7, 9, 257); // 이진수: 100000001 (0번 바이트에서 1번 바이트로 넘어감)
        Console.WriteLine($"7-15번 비트에 257 설정 후 (16진수): {BitConverter.ToString(data)}");
        Console.WriteLine($"7-15번 비트에 257 설정 후 (비트): {ToBinaryString(data)}");
        Console.WriteLine($"7-15번 비트 값 읽기: {GetBits(data, 7, 9)}");
    }
}