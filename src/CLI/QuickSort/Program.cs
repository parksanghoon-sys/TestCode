using System;

class QuickSort
{
    static void Main(string[] args)
    {
        int[] array = { 9, 7, 5, 11, 12, 2, 14, 3, 10, 6 };

        Console.WriteLine("Original array:");
        PrintArray(array);

        QuickSortAlgorithm(array, 0, array.Length - 1);

        Console.WriteLine("\nSorted array:");
        PrintArray(array);
    }

    static void QuickSortAlgorithm(int[] array, int left, int right)
    {
        if (left < right)
        {
            int pivot = Partition(array, left, right);

            if (pivot > 1)
                QuickSortAlgorithm(array, left, pivot - 1);

            if (pivot + 1 < right)
                QuickSortAlgorithm(array, pivot + 1, right);
        }
    }

    static int Partition(int[] array, int left, int right)
    {
        int pivot = array[left];
        while (true)
        {
            while (array[left] < pivot)
                left++;

            while (array[right] > pivot)
                right--;

            if (left < right)
            {
                if (array[left] == array[right]) return right;

                int temp = array[left];
                array[left] = array[right];
                array[right] = temp;
            }
            else
            {
                return right;
            }
        }
    }

    static void PrintArray(int[] arr)
    {
        foreach (var item in arr)
        {
            Console.Write(item + " ");
        }
        Console.WriteLine();
    }
}
