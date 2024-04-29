using System;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        string calc = "1+2*(5*2/(1+2))";
        string postfix = InfixToPostfix(calc);
        Console.WriteLine("Postfix expression: " + postfix);

        // 후위 표기법을 계산
        double result = EvaluatePostfix(postfix);
        Console.WriteLine("Result: " + result);

    }
    static int GetPriority(char op)
    {
        switch (op)
        {
            case '+':
            case '-':
                return 1;
            case '*':
            case '/':
                return 2;
            default:
                return 0;
        }
    }
    static string InfixToPostfix(string infix)
    {
        string postfix = "";
        Stack<char> stack = new Stack<char>();

        foreach (char c in infix)
        {
            if (char.IsDigit(c))
            {
                postfix += c;
            }
            else if (c == '(')
            {
                stack.Push(c);
            }
            else if (c == ')')
            {
                while (stack.Count > 0 && stack.Peek() != '(')
                {
                    postfix += stack.Pop();
                }
                stack.Pop(); // '(' 제거
            }
            else
            {
                while (stack.Count > 0 && GetPriority(stack.Peek()) >= GetPriority(c))
                {
                    postfix += stack.Pop();
                }
                stack.Push(c);
            }
        }

        while (stack.Count > 0)
        {
            postfix += stack.Pop();
        }

        return postfix;
    }
    static double EvaluatePostfix(string postfix)
    {
        Stack<double> stack = new Stack<double>();

        foreach (char c in postfix)
        {
            if (char.IsDigit(c))
            {
                stack.Push(double.Parse(c.ToString()));
            }
            else
            {
                double operand2 = stack.Pop();
                double operand1 = stack.Pop();
                switch (c)
                {
                    case '+':
                        stack.Push(operand1 + operand2);
                        break;
                    case '-':
                        stack.Push(operand1 - operand2);
                        break;
                    case '*':
                        stack.Push(operand1 * operand2);
                        break;
                    case '/':
                        stack.Push(operand1 / operand2);
                        break;
                }
            }
        }

        return stack.Pop();
    }
}