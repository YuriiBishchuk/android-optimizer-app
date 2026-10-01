using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main()
    {
        var regex = new Regex(@"analytics|telemetry|tracker|acr|adservice|recommend|promo|demo|retail|partnercustomizer|printspooler|nearby\.halfsheet|feedback|federated|personalization", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Console.WriteLine("IsMatch nearby.halfsheet: " + regex.IsMatch("nearby.halfsheet"));
        Console.WriteLine("IsMatch com.example.nearby.halfsheet: " + regex.IsMatch("com.example.nearby.halfsheet"));
        Console.WriteLine("IsMatch acrstuff: " + regex.IsMatch("acrstuff"));
        Console.WriteLine("IsMatch com.example.acrstuff: " + regex.IsMatch("com.example.acrstuff"));
    }
}
