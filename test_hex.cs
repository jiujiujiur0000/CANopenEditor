using System;
class Program {
    static void Main() {
        string keyString = "10";
        if (byte.TryParse(keyString, out byte key))
        {
            Console.WriteLine("Parsed "10" as decimal: " + key + ", hex output: 0x" + key.ToString("X1"));
        }
    }
}
