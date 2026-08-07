using System;
class P { static void Main() {
    try {
        Console.WriteLine(Convert.ToInt32("0x1000", 16));
    } catch (Exception e) {
        Console.WriteLine("ERROR: " + e.Message);
    }
} }
