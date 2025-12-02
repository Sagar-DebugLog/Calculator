using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Calculator : MonoBehaviour
{
    public Text displayText;
    public Text displayOperator;
    public Text displayResult;
    private int CurrentNumber;
    private string tempNumberHolder = "";
    private string expression = "";

    void Awake()
    {
        TryAutoAssignUIText();
    }

    void Start()
    {
        UpdateDisplay();
        if (displayResult != null) displayResult.text = "0";
    }

    public void OnClickButton_1() { CurrentNumber = 1; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_2() { CurrentNumber = 2; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_3() { CurrentNumber = 3; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_4() { CurrentNumber = 4; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_5() { CurrentNumber = 5; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_6() { CurrentNumber = 6; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_7() { CurrentNumber = 7; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_8() { CurrentNumber = 8; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_9() { CurrentNumber = 9; SetDisplayInput(CurrentNumber); }
    public void OnClickButton_0() { CurrentNumber = 0; SetDisplayInput(CurrentNumber); }

    public void OnClickButton_dot()
    {
        if (string.IsNullOrEmpty(tempNumberHolder)) tempNumberHolder = "0.";
        else if (!tempNumberHolder.Contains(".")) tempNumberHolder += ".";
        UpdateDisplay();
    }

    public void OnClickButton_delete()
    {
        if (!string.IsNullOrEmpty(tempNumberHolder))
        {
            tempNumberHolder = tempNumberHolder.Length > 1 ? tempNumberHolder.Remove(tempNumberHolder.Length - 1) : "";
        }
        else if (!string.IsNullOrEmpty(expression))
        {
            expression = expression.Remove(expression.Length - 1);
        }
        UpdateDisplay();
        TryEvaluatePartial();
    }

    public void OnClickButton_division()
    {
        CommitNumberThenOperator('/');
        if (displayOperator != null) displayOperator.text = "/";
        OnCheckOperator("/");
    }

    public void OnClickButton_multiply()
    {
        CommitNumberThenOperator('*');
        if (displayOperator != null) displayOperator.text = "*";
        OnCheckOperator("*");
    }

    public void OnClickButton_subtraction()
    {
        if (string.IsNullOrEmpty(expression) && string.IsNullOrEmpty(tempNumberHolder))
        {
            tempNumberHolder = "-";
            UpdateDisplay();
            return;
        }
        CommitNumberThenOperator('-');
        if (displayOperator != null) displayOperator.text = "-";
        OnCheckOperator("-");
    }

    public void OnClickButton_addition()
    {
        CommitNumberThenOperator('+');
        if (displayOperator != null) displayOperator.text = "+";
        OnCheckOperator("+");
    }

    public void OnClickButton_equal()
    {
        if (!string.IsNullOrEmpty(tempNumberHolder))
        {
            expression += tempNumberHolder;
            tempNumberHolder = "";
        }
        if (string.IsNullOrEmpty(expression)) return;
        try
        {
            var tokens = Tokenize(expression);
            var rpn = ShuntingYard(tokens);
            double val = EvaluateRPN(rpn);
            if (displayResult != null) displayResult.text = val.ToString();
            if (displayOperator != null) displayOperator.text = "=";
            expression = val.ToString();
            tempNumberHolder = "";
        }
        catch
        {
            if (displayResult != null) displayResult.text = "Error";
            if (displayOperator != null) displayOperator.text = "=";
            expression = "";
            tempNumberHolder = "";
        }
        UpdateDisplay();
    }

    public void OnClickButton_AC()
    {
        if (displayOperator != null) displayOperator.text = "AC";
        tempNumberHolder = "";
        expression = "";
        if (displayResult != null) displayResult.text = "0";
        UpdateDisplay();
    }

    public void SetDisplayInput(int Value)
    {
        if (tempNumberHolder == "0") tempNumberHolder = Value.ToString();
        else tempNumberHolder += Value.ToString();
        UpdateDisplay();
    }

    public void OnCheckOperator(string operators)
    {
        Debug.Log("Operator: " + operators);
    }

    private void CommitNumberThenOperator(char op)
    {
        if (!string.IsNullOrEmpty(tempNumberHolder))
        {
            expression += tempNumberHolder;
            tempNumberHolder = "";
        }
        if (string.IsNullOrEmpty(expression)) return;
        char last = expression[expression.Length - 1];
        if ("+-*/".IndexOf(last) >= 0)
        {
            expression = expression.Remove(expression.Length - 1) + op;
        }
        else
        {
            expression += op;
        }
        UpdateDisplay();
        TryEvaluatePartial();
    }

    private void UpdateDisplay()
    {
        string combined = (string.IsNullOrEmpty(expression) ? "" : expression) + (string.IsNullOrEmpty(tempNumberHolder) ? "" : tempNumberHolder);
        if (string.IsNullOrEmpty(combined)) combined = "0";
        if (displayText != null) displayText.text = combined;
    }

    private void TryEvaluatePartial()
    {
        string exprToEval = expression;
        if (string.IsNullOrEmpty(exprToEval)) return;
        if ("+-*/".IndexOf(exprToEval[exprToEval.Length - 1]) >= 0) exprToEval = exprToEval.Remove(exprToEval.Length - 1);
        if (string.IsNullOrEmpty(exprToEval)) return;
        try
        {
            var tokens = Tokenize(exprToEval);
            var rpn = ShuntingYard(tokens);
            double val = EvaluateRPN(rpn);
            if (displayResult != null) displayResult.text = val.ToString();
        }
        catch
        {
            if (displayResult != null) displayResult.text = "Error";
        }
    }

    private void TryAutoAssignUIText()
    {
        if (displayText != null && displayOperator != null && displayResult != null) return;
        var texts = FindObjectsOfType<Text>();
        foreach (var t in texts)
        {
            string n = t.gameObject.name.ToLower();
            if (displayText == null && (n.Contains("displaytext") || n.Contains("display") || n.Contains("text"))) displayText = t;
            else if (displayOperator == null && (n.Contains("operator") || n.Contains("op"))) displayOperator = t;
            else if (displayResult == null && (n.Contains("result") || n.Contains("res"))) displayResult = t;
        }
        int idx = 0;
        if (displayText == null && texts.Length > idx) displayText = texts[idx++];
        if (displayOperator == null && texts.Length > idx) displayOperator = texts[idx++];
        if (displayResult == null && texts.Length > idx) displayResult = texts[idx++];
    }

    private enum TokenType { Number, Operator, LeftParen, RightParen }
    private struct Token { public TokenType type; public string text; public Token(TokenType t, string s) { type = t; text = s; } }

    private List<Token> Tokenize(string expr)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < expr.Length)
        {
            char c = expr[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.')) i++;
                tokens.Add(new Token(TokenType.Number, expr.Substring(start, i - start)));
                continue;
            }
            if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                if (c == '-' && (tokens.Count == 0 || tokens[tokens.Count - 1].type == TokenType.Operator || tokens[tokens.Count - 1].type == TokenType.LeftParen))
                {
                    int start = i;
                    i++;
                    while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.')) i++;
                    tokens.Add(new Token(TokenType.Number, expr.Substring(start, i - start)));
                    continue;
                }
                tokens.Add(new Token(TokenType.Operator, c.ToString()));
                i++;
                continue;
            }
            if (c == '(') { tokens.Add(new Token(TokenType.LeftParen, "(")); i++; continue; }
            if (c == ')') { tokens.Add(new Token(TokenType.RightParen, ")")); i++; continue; }
            i++;
        }
        return tokens;
    }

    private int Precedence(string op)
    {
        if (op == "+" || op == "-") return 1;
        if (op == "*" || op == "/") return 2;
        return 0;
    }

    private List<Token> ShuntingYard(List<Token> tokens)
    {
        var output = new List<Token>();
        var ops = new Stack<Token>();
        foreach (var t in tokens)
        {
            if (t.type == TokenType.Number) output.Add(t);
            else if (t.type == TokenType.Operator)
            {
                while (ops.Count > 0 && ops.Peek().type == TokenType.Operator && Precedence(ops.Peek().text) >= Precedence(t.text)) output.Add(ops.Pop());
                ops.Push(t);
            }
            else if (t.type == TokenType.LeftParen) ops.Push(t);
            else if (t.type == TokenType.RightParen)
            {
                while (ops.Count > 0 && ops.Peek().type != TokenType.LeftParen) output.Add(ops.Pop());
                if (ops.Count > 0 && ops.Peek().type == TokenType.LeftParen) ops.Pop();
            }
        }
        while (ops.Count > 0) output.Add(ops.Pop());
        return output;
    }

    private double EvaluateRPN(List<Token> rpn)
    {
        var st = new Stack<double>();
        foreach (var t in rpn)
        {
            if (t.type == TokenType.Number)
            {
                if (!double.TryParse(t.text, out double v)) throw new Exception("Parse error");
                st.Push(v);
            }
            else if (t.type == TokenType.Operator)
            {
                if (st.Count < 2) throw new Exception("Insufficient values");
                double b = st.Pop();
                double a = st.Pop();
                double r = 0;
                switch (t.text)
                {
                    case "+": r = a + b; break;
                    case "-": r = a - b; break;
                    case "*": r = a * b; break;
                    case "/":
                        if (Math.Abs(b) < 1e-12) throw new Exception("Division by zero");
                        r = a / b; break;
                }
                st.Push(r);
            }
        }
        if (st.Count != 1) throw new Exception("Bad expression");
        return st.Pop();
    }
}